//**************************************************************
// FindFiles
//  Copyright (c) Rylogic 2010
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

#pragma once

#include <string>
#include <filesystem>
#include <windows.h>

namespace pr::filesys
{
	// Helper for iterating over files in a directory
	// Note: Consider using std::filesystem::directory_iterator
	struct FindFiles :WIN32_FIND_DATAW
	{
	private:
		std::filesystem::path const m_root; // The directory to find files within
		std::filesystem::path m_filepath;   // The found full file path
		wchar_t const* m_file_masks;        // A semicolon separated list of file masks
		HANDLE m_handle;                    // The find files handle
		BOOL m_more;                        // true if there are more files to get

	public:
		// 'root' is the directory to search for files in.
		// 'file_masks' is a semicolon separated, null terminated, list of file masks.
		FindFiles(std::filesystem::path const& root, wchar_t const* file_masks)
			:WIN32_FIND_DATAW()
			,m_root(root)
			,m_filepath()
			,m_file_masks(file_masks)
			,m_handle(INVALID_HANDLE_VALUE)
			,m_more(false)
		{
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
				if (m_handle != INVALID_HANDLE_VALUE)
				{
					m_more = FindNextFileW(m_handle, this);
					last_error = GetLastError();
				}
				if (m_more) break;

				// Check that 'next' failed only because there were no more files
				if (!m_more && last_error != ERROR_NO_MORE_FILES)
					throw std::exception("Error occurred while searching for files");

				// If there are no more file masks, we're done
				if (*m_file_masks == 0)
					break;

				// Find the next file mask,
				auto p = m_file_masks;
				for (; *p != ';' && *p != 0; ++p) {}

				// Reset the root search path and append the file mask
				auto search = m_root / std::filesystem::path(m_file_masks, p);

				// Advance the file mask pointer
				m_file_masks = p + (*p == ';');

				// Close the previous find and open a new find
				if (m_handle != INVALID_HANDLE_VALUE)
				{
					FindClose(m_handle);
					m_handle = INVALID_HANDLE_VALUE;
				}
				m_handle = FindFirstFileW(search.c_str(), this);
				last_error = GetLastError();

				m_more = m_handle != INVALID_HANDLE_VALUE;
				if (!m_more && last_error != ERROR_FILE_NOT_FOUND)
					throw std::exception("Error occurred while searching for files");
			}
			while (!m_more);
			m_filepath = m_root / cFileName;
		}

		// Return true if the last file has be found
		bool done() const
		{
			return m_more == 0;
		}

		// Return the full pathname of the found file
		wchar_t const* fullpath() const
		{
			return m_filepath.c_str();
		}

		// Return the full pathname (as a String)
		std::filesystem::path const& fullpath2() const
		{
			return m_filepath;
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr::filesys
{
	PRUnitTest(FindFilesTests)
	{
		using namespace std::filesystem;

		auto dir = path(__FILE__).parent_path();
		auto root = canonical(dir / ".." / ".." / ".." / "projects" / "unittests" / "src");
		if (!exists(root))
		{
			PR_CHECK(false && "FindFiles test failed, root directory not found", true);
			return;
		}

		bool found_cpp = false, found_h = false;
		for (pr::filesys::FindFiles ff(root, L"*.cpp;*.h"); !ff.done(); ff.next())
		{
			found_cpp |= ff.fullpath2().extension().compare(".cpp") == 0;
			found_h   |= ff.fullpath2().extension().compare(".h") == 0;
		}
		PR_CHECK(found_cpp,true);
		PR_CHECK(found_h,true);
	}
}
#endif
