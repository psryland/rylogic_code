//**************************************************************
// Recurse Directory
//  Copyright (c) Rylogic Ltd 2010
//**************************************************************
#pragma once

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
		template <typename PathCB>
		bool RecurseDirectory(std::wstring path, PathCB EnumDirectories)
		{
			// Enum this directory
			if (!EnumDirectories(path))
				return false;

			// Recurse the directories in this directory
			// Append a mask to the dir path
			for (FindFiles ff(path, "*"); !ff.done(); ff.next())
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
		template <typename PathCB, typename SkipDirCB>
		bool RecurseFiles(std::wstring path, PathCB EnumFiles, wchar_t const* file_masks, SkipDirCB SkipDir)
		{
			// Find the files in this directory
			for (FindFiles ff(path, file_masks); !ff.done(); ff.next())
			{
				if (!EnumFiles(ff.fullpath2()))
					return false;
			}

			// Recurse into the directories with 'path'
			for (FindFiles ff(path, L"*"); !ff.done(); ff.next())
			{
				// Ignore non directories
				if ((ff.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY) != FILE_ATTRIBUTE_DIRECTORY)
					continue;

				// Ignore special directories
				if (wcscmp(ff.cFileName, L".") == 0 || wcscmp(ff.cFileName, L"..") == 0)
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
		template <typename PathCB>
		bool RecurseFiles(std::wstring path, PathCB EnumFiles, wchar_t const* file_masks)
		{
			return RecurseFiles(path, EnumFiles, file_masks, [](std::wstring const&){ return false; });
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
			auto EnumFiles = [&](std::wstring path)
				{
					auto extn = pr::filesys::GetExtension(path);
					if      (extn.compare(L"cpp") == 0) ++found[0];
					else if (extn.compare(L"c")   == 0) ++found[1];
					else if (extn.compare(L"h")   == 0) ++found[2];
					else                                ++found[3];
					return true;
				};

			wchar_t curr_dir[MAX_PATH];
			GetCurrentDirectoryW(_countof(curr_dir), curr_dir);

			auto root = pr::filesys::CombinePath<std::wstring>(curr_dir, L"..\\projects\\unittests");
			if (!pr::filesys::DirectoryExists(root))
			{
				PR_CHECK(false && "Recurse directory test failed, root directory not found", true);
				return;
			}

			PR_CHECK(pr::filesys::RecurseFiles(root, EnumFiles, L"*.cpp;*.c"), true);
			PR_CHECK(pr::filesys::RecurseFiles(root, EnumFiles, L"*.h;*.py"), true);
			PR_CHECK(found[0] == 1, true);
			PR_CHECK(found[1] == 0, true);
			PR_CHECK(found[2] == 2, true);
			PR_CHECK(found[3] == 2, true);
		}
	}
}
#endif

