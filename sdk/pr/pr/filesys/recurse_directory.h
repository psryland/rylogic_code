//**************************************************************
// Recurse Directory
//  Copyright © Rylogic Ltd 2010
//**************************************************************
#pragma once
#ifndef PR_FILESYS_RECURSE_DIRECTORY_H
#define PR_FILESYS_RECURSE_DIRECTORY_H

#include <stdio.h>
#include <windows.h>
#include <string>
#include "pr/filesys/findfiles.h"

namespace pr
{
	namespace filesys
	{
		// Recursively enumerate directories below and including 'path'
		// PathCB should have a signature: bool (*EnumDirectories)(String pathname)
		// Returns false if 'EnumDirectories' returns false, indicating that the search ended early
		template <typename String, typename PathCB>
		bool RecurseDirectory(String path, PathCB EnumDirectories)
		{
			// Enum this directory
			if (!EnumDirectories(path))
				return false;

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
				if (!RecurseDirectory(ff.fullpath2(), EnumDirectories))
					return false;
			}
			return true;
		}

		// Recursively enumerate files within and below 'path'
		// 'file_masks' is a semicolon separated, null terminated, list of file masks
		// PathCB should have a signature: bool (*EnumFiles)(String pathname)
		// SkipDirCB should have a signature: bool (*SkipDir)(String pathname)
		// Returns false if 'EnumFiles' returns false, indicating that the search ended early
		template <typename String, typename PathCB, typename SkipDirCB>
		bool RecurseFiles(String path, PathCB EnumFiles, const char* file_masks, SkipDirCB SkipDir)
		{
			// Find the files in this directory
			for (FindFiles<String> ff(path, file_masks); !ff.done(); ff.next())
			{
				if (!EnumFiles(ff.fullpath2()))
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
				if (SkipDir(ff.fullpath2()))
					continue;

				// Enumerate the files in this directory
				if (!RecurseFiles(ff.fullpath2(), EnumFiles, file_masks, SkipDir))
					return false;
			}
			return true;
		}

		// Recursively enumerate files within and below 'path'
		// 'file_masks' is a semicolon separated, null terminated, list of file masks
		// PathCB should have a signature: bool (*EnumFiles)(String&& pathname)
		template <typename String, typename PathCB>
		bool RecurseFiles(String path, PathCB EnumFiles, const char* file_masks)
		{
			return RecurseFiles(path, EnumFiles, file_masks, [](String){ return false; });
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_filesys_recurse_directory)
		{
			int found[4] = {}; // 0-*.cpp, 1-*.c, 2-*.h, 3-other
			auto EnumFiles = [&](std::string path)
				{
					auto extn = pr::filesys::GetExtension(path);
					if      (extn.compare("cpp") == 0) ++found[0];
					else if (extn.compare("c")   == 0) ++found[1];
					else if (extn.compare("h")   == 0) ++found[2];
					else                               ++found[3];
					return true;
				};

			std::string root = "\\projects\\unittests";
			PR_CHECK(pr::filesys::RecurseFiles(root, EnumFiles, "*.cpp;*.c"), true);
			PR_CHECK(pr::filesys::RecurseFiles(root, EnumFiles, "*.h;*.py"), true);
			PR_CHECK(found[0] == 1, true);
			PR_CHECK(found[1] == 0, true);
			PR_CHECK(found[2] == 2, true);
			PR_CHECK(found[3] == 2, true);
		}
	}
}
#endif

#endif
