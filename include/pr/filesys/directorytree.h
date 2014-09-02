//**********************************
// Directory Tree
//  Copyright (c) Rylogic Ltd 2007
//**********************************
#ifndef PR_DIRECTORY_TREE_H
#define PR_DIRECTORY_TREE_H

// Defines
// ;NOMINMAX 

#include <algorithm>
#include <windows.h>
#include "pr/common/predicate.h"
#include "pr/filesys/filesys.h"

namespace pr
{
	namespace filesys
	{
		enum ERecurse
		{
			ERecurse_Recurse,
			ERecurse_DontRecurse
		};

		struct DirTreeFile
		{
			std::string	m_name;		// The name of the file or directory
			uint        m_attrib;	// A combination of filesys::Attrib flags
		};
		inline bool operator == (const DirTreeFile& lhs, const std::string& rhs) { return str::EqualI(lhs.m_name, rhs); }
		typedef std::vector<DirTreeFile> TFileVec;
		
		struct DirTree;
		typedef std::vector<DirTree> TDirectoryVec;
		struct DirTree
		{
			std::string		m_name;		// The name of the directory
			uint			m_attrib;	// A combination of filesys::Attrib flags
			TFileVec		m_file;		// Files in this directory
			TDirectoryVec	m_sub_dir;	// Sub directories / folders
		};
		inline bool operator == (const DirTree& lhs, const std::string& rhs) { return str::EqualI(lhs.m_name, rhs); }
		typedef std::vector<std::string> TMasks; // Masks to use when building the tree

		namespace impl
		{
			// Build a directory tree
			template <typename T>
			void BuildDirectoryTree(std::string const& directory, ERecurse recurse, DirTree& root, TMasks const& masks)
			{
				root.m_name   = pr::filesys::StandardiseC(directory);
				root.m_attrib = pr::filesys::GetAttribs(directory);
				
				for( TMasks::const_iterator m = masks.begin(), m_end = masks.end(); m != m_end; ++m )
				{
					std::string find_mask = directory + "\\" + *m;

					// Find the files 
					WIN32_FIND_DATA find_data;
					HANDLE handle = FindFirstFile(find_mask.c_str(), &find_data);
					if( handle == INVALID_HANDLE_VALUE ) return;
					
					do
					{
						if( (find_data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) && recurse ==  ERecurse_Recurse )
						{
							if( strcmp(find_data.cFileName, ".") != 0 && strcmp(find_data.cFileName, "..") != 0 )
							{
								std::string sub_dir = directory + "\\" + std::string(find_data.cFileName);
								if( std::find(root.m_sub_dir.begin(), root.m_sub_dir.end(), sub_dir) == root.m_sub_dir.end() )
								{
									root.m_sub_dir.push_back(DirTree());
									BuildDirectoryTree<T>(sub_dir, recurse, root.m_sub_dir.back(), masks);
								}
							}
						}
						else
						{
							std::string filename = directory + "\\" + std::string(find_data.cFileName);
							if( std::find(root.m_file.begin(), root.m_file.end(), filename) == root.m_file.end() )
							{
								root.m_file.push_back(DirTreeFile());
								DirTreeFile& file = root.m_file.back();
								file.m_name   = pr::filesys::Standardise(filename);
								file.m_attrib = pr::filesys::GetAttribs(file.m_name);
							}
						}
					}
					while( FindNextFile(handle, &find_data) );
					FindClose(handle);
				}
			}
		}//namespace impl

		inline void BuildDirectoryTree(const std::string& directory, ERecurse recurse, DirTree& root)
		{
			TMasks masks; masks.push_back("*");
			return impl::BuildDirectoryTree<void>(directory, recurse, root, masks);
		}
		inline void BuildDirectoryTree(const std::string& directory, ERecurse recurse, DirTree& root, const TMasks& masks)
		{
			return impl::BuildDirectoryTree<void>(directory, recurse, root, masks);
		}

	}
}

#endif
