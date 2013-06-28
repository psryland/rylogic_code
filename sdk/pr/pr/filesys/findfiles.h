//**************************************************************
// FindFiles
//  (c)opyright Rylogic Limited 2010
//**************************************************************
// Scoped wrapper around the FindFirstFile/FindNextFile API
// Usage:
//	for (pr::filesys::FindFiles<> ff(dir, "*"); !ff.done(); ff.next())
//	{
//		// Ignore the special folders
//		if (strcmp(ff.cFileName, ".") == 0 || strcmp(ff.cFileName, "..") == 0)
//			continue;
//
//		// Directory...
//		if ((ff.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY)
//		{ ... }
//
//		std::string path = ff.fullpath();
//	}

#ifndef PR_FILESYS_FINDFILES
#define PR_FILESYS_FINDFILES

#include <string>
#include <windows.h>

namespace pr
{
	namespace filesys
	{
		// Helper for iterating over files in a directory
		template <typename String = std::string> struct FindFiles :WIN32_FIND_DATAA
		{
		private:
			String         m_root;       // The directory to find files within
			size_t         m_root_len;   // The length of the 'm_root' string
			char const*    m_file_masks; // A semicolon separated list of file masks
			HANDLE         m_handle;     // The find files handle
			BOOL           m_more;       // true if there are more files to get
			
		public:
			// 'root' is the directory to search for files in.
			// 'file_masks' is a semicolon separated, null terminated, list of file masks.
			FindFiles(String const& root, char const* file_masks)
			:WIN32_FIND_DATAA()
			,m_root(root)
			,m_root_len(root.size())
			,m_file_masks(file_masks)
			,m_handle(INVALID_HANDLE_VALUE)
			,m_more(false)
			{
				if (m_root.empty()) m_root.push_back('.');
				if (m_root[m_root_len-1] != '\\' && m_root[m_root_len-1] != '/') m_root.push_back('\\');
				m_root_len = m_root.size();
				next();
			}

			~FindFiles()
			{
				if (m_handle != INVALID_HANDLE_VALUE)
					FindClose(m_handle);
			}
			
			// Move to the next file that matches one of the file masks
			void next()
			{
				do
				{
					// If a find is already open, look for the next file
					DWORD last_error = ERROR_NO_MORE_FILES;
					if (m_handle != INVALID_HANDLE_VALUE) { m_more = FindNextFileA(m_handle, this); last_error = GetLastError(); }
					if (m_more) break;
					
					// Check that 'next' failed only because there were no more files
					if (!m_more && last_error != ERROR_NO_MORE_FILES)
						throw std::exception("Error occurred while searching for files");
					
					// Find the next file mask, if there are no more file masks, we're done
					if (*m_file_masks == 0) break;
					char const* p; for (p = m_file_masks+1; *p != ';' && *p != 0; ++p) {}
					m_root.resize(m_root_len);
					m_root.append(m_file_masks, p);
					m_file_masks = p + (*p == ';');
					
					// Close the previous find and open a new find
					if (m_handle != INVALID_HANDLE_VALUE) { FindClose(m_handle); m_handle = INVALID_HANDLE_VALUE; }
					m_handle = FindFirstFileA(m_root.c_str(), this); last_error = GetLastError();
					
					m_more = m_handle != INVALID_HANDLE_VALUE;
					if (!m_more && last_error != ERROR_FILE_NOT_FOUND)
						throw std::exception("Error occurred while searching for files");
				}
				while (!m_more);
				m_root.resize(m_root_len);
				m_root += cFileName;
			}
			
			// Return true if the last file has be found
			bool done() const
			{
				return m_more == 0;
			}
			
			// Return the full pathname of the found file
			char const* fullpath() const
			{
				return m_root.c_str(); // root has the file added after a call to 'next()'
			}
			
			// Return the full pathname (as a String)
			String const& fullpath2() const
			{
				return m_root;
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_filesys_findfiles)
		{
			bool found_cpp = false, found_h = false;
			std::string root = "Q:\\projects\\unittests";
			for (pr::filesys::FindFiles<> ff(root, "*.cpp;*.h"); !ff.done(); ff.next())
			{
				found_cpp |= pr::filesys::GetExtension<std::string>(ff.fullpath()).compare("cpp") == 0;
				found_h   |= pr::filesys::GetExtension<std::string>(ff.fullpath()).compare("h") == 0;
			}
			PR_CHECK(found_cpp,true);
			PR_CHECK(found_h,true);
		}
	}
}
#endif

#pragma warning(default:4355) // 'this' used in constructor

#endif
