//**************************************************************
// Recurse Directory
//  Copyright (c) Rylogic Ltd 2010
//**************************************************************
#pragma once

#include <string>
#include <filesystem>
#include <stdio.h>
#include <windows.h>
#include "pr/filesys/findfiles.h"

namespace pr::filesys
{
	// Recursively enumerate directories below and including 'path'
	// PathCB should have a signature: bool (*DirCB)(void*, std::filesystem::path pathname)
	// Returns false if 'DirCB' returns false, indicating that the search ended early.
	template <typename PathCB>
	bool EnumDirectories(std::filesystem::path const& path, PathCB DirCB, void* ctx)
	{
		// Enum this directory
		if (!DirCB(ctx, path))
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
			if (!EnumDirectories(ff.fullpath2(), DirCB, ctx))
				return false;
		}
		return true;
	}

	// Recursively enumerate files within and below 'path'
	// 'file_masks' is a semicolon separated, null terminated, list of file masks
	// PathCB should have a signature: bool (*file_cb)(void*, FindFiles const& ff)
	// SkipDirCB should have a signature: bool (*skip_cb)(void*, FindFiles const& ff)
	// Returns false if 'file_cb' returns false, indicating that the search ended early.
	template <typename PathCB, typename SkipDirCB>
	bool EnumFiles(std::filesystem::path const& path, wchar_t const* file_masks, PathCB file_cb, SkipDirCB skip_cb, void* ctx)
	{
		// Find the files in this directory
		for (FindFiles ff(path, file_masks); !ff.done(); ff.next())
		{
			if (!file_cb(ctx, ff))
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
			if (skip_cb(ctx, ff))
				continue;

			// Enumerate the files in this directory
			if (!EnumFiles(ff.fullpath2(), file_masks, file_cb, skip_cb, ctx))
				return false;
		}
		return true;
	}

	// Recursively enumerate files within and below 'path'
	// 'file_masks' is a semicolon separated, null terminated, list of file masks
	// PathCB should have a signature: bool (*file_cb)(FindFiles const& ff)
	template <typename PathCB>
	bool EnumFiles(std::filesystem::path const& path, wchar_t const* file_masks, PathCB file_cb, void* ctx)
	{
		return EnumFiles(path, file_masks, file_cb, [](void*, FindFiles const&){ return false; }, ctx);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/filesys.h"
namespace pr::filesys
{
	PRUnitTest(RecurseDirectoryTests)
	{
		using namespace std::filesystem;

		int found[4] = {}; // 0-*.cpp, 1-*.c, 2-*.h, 3-other
		auto file_cb = [&](void*, FindFiles const& ff)
		{
			auto extn = ff.fullpath2().extension();
			if      (extn.compare(".cpp") == 0) ++found[0];
			else if (extn.compare(".c")   == 0) ++found[1];
			else if (extn.compare(".h")   == 0) ++found[2];
			else                                ++found[3];
			return true;
		};

		auto dir = path(__FILE__).parent_path();
		auto root = canonical(dir / ".." / ".." / ".." / "projects" / "unittests" / "src");
		if (!exists(root))
		{
			PR_CHECK(false && "Recurse directory test failed, root directory not found", true);
			return;
		}

		PR_CHECK(pr::filesys::EnumFiles(root, L"*.cpp;*.c", file_cb, nullptr), true);
		PR_CHECK(pr::filesys::EnumFiles(root, L"*.h;*.py" , file_cb, nullptr), true);
		PR_CHECK(found[0] == 1, true);
		PR_CHECK(found[1] == 0, true);
		PR_CHECK(found[2] == 2, true);
		PR_CHECK(found[3] == 0, true);
	}
}
#endif

