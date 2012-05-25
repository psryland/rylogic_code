//**************************************************************
// Recurse Directory
//  Copyright © Rylogic Ltd 2010
//**************************************************************
// Usage:
//	bool EnumDirectories(const char* path, void* context)
//	{
//		for (pr::filesys::FindFiles ff(path, "*.*"); !ff.done(); ff.next())
//		{
//			std::string filepath = ff.fullpath();
//			// Do stuff to file
//		}
//		return true;
//	}
//	bool EnumFiles(const char* file_path, void* context)
//	{
//		// Do stuff to file
//		return true; // For more files please
//	}
//	void main(void)
//	{
//		std::string directory_path = "C:\windows";
//		RecurseDirectory(directory_path, EnumDirectories, 0);
//		RecurseFiles(directory_path, EnumFiles, "*.x", 0);
//	}

#ifndef PR_FILESYS_RECURSE_DIRECTORY_H
#define PR_FILESYS_RECURSE_DIRECTORY_H

#include <stdio.h>
#include <windows.h>
#include <string>
#include "pr/filesys/findfiles.h"
#include "pr/common/assert.h"

namespace pr
{
	namespace filesys
	{
		typedef bool (*PathCB)(const char* pathname, void* context);

		// Recursively enumerate directories below and including 'path'
		template <typename String> inline bool RecurseDirectory(String const& path, PathCB EnumDirectories, void* context)
		{
			// Enum this directory
			if (!EnumDirectories(path, context)) { return false; }
			
			// Recurse the directories in this directory
			// Append a mask to the dir path
			for (FindFiles<String> ff(path, "*"); !ff.done(); ff.next())
			{
				// Only consider directories
				if ((ff.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY)
					continue;
				
				// Ignore the special directories
				if (strcmp(ff.cFileName, ".") == 0 || strcmp(ff.cFileName, "..") == 0)
					continue;
				
				// Recurse into subdirectories
				if (!RecurseDirectory(ff.fullpath2(), EnumDirectories, context))
					return false;
			}
			return true;
		}
		
		// Recursively enumerate files within and below 'path'
		// 'file_masks' is a semicolon separated, null terminated, list of file masks
		template <typename String> inline bool RecurseFiles(String const& path, PathCB EnumFiles, const char* file_masks, void* context, PathCB SkipDir = 0)
		{
			// Find the files in this directory
			for (FindFiles<String> ff(path, file_masks); !ff.done(); ff.next())
			{
				if (!EnumFiles(ff.fullpath(), context))
					return false;
			}
			
			// Recurse into the directories with 'path'
			for (FindFiles<String> ff(path, "*"); !ff.done(); ff.next())
			{
				// Ignore non directories
				if ((ff.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY)
					continue;
				
				// Ignore special directories
				if (strcmp(ff.cFileName, ".") == 0 || strcmp(ff.cFileName, "..") == 0)
					continue;
				
				// Allow callers to exclude specific directories
				// Directories will not have trailing '\' characters
				if (SkipDir != 0 && SkipDir(ff.fullpath(), context))
					continue;
				
				// Enumerate the files in this directory
				if (!RecurseFiles(ff.fullpath2(), EnumFiles, file_masks, context, SkipDir))
					return false;
			}
			return true;
		}
	}
}

#endif
