//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/macros/enum.h"
#include "pr/geometry/common.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/stl.h"

namespace pr
{
	namespace geometry
	{
		// Supported model file formats
		#define PR_ENUM(x)\
			x(Unknown , ""    , = 0)\
			x(P3D     , "p3d" , = 1)/* PR3D */\
			x(Max3DS  , "3ds" , = 2)/* 3D Studio Max */\
			x(STL     , "stl" , = 3)/* Stereolithography CAD model */
		PR_DEFINE_ENUM3(EModelFileFormat, PR_ENUM);
		#undef PR_ENUM
		
		// Determine the model file format from the filepath
		template <typename Char>
		inline EModelFileFormat GetModelFormat(Char const* filepath)
		{
			struct L {
				static void splitpath(char const* fullpath, char* drive, size_t drive_size, char* dir, size_t dir_size, char* fname, size_t fname_size, char* extn, size_t extn_size)
				{
					::_splitpath_s(fullpath, drive, drive_size, dir, dir_size, fname, fname_size, extn, extn_size);
				}
				static void splitpath(wchar_t const* fullpath, wchar_t* drive, size_t drive_size, wchar_t* dir, size_t dir_size, wchar_t* fname, size_t fname_size, wchar_t* extn, size_t extn_size)
				{
					::_wsplitpath_s(fullpath, drive, drive_size, dir, dir_size, fname, fname_size, extn, extn_size);
				}
			};

			// Read the file extension
			Char extn[_MAX_EXT] = {}, *ext = &extn[0];
			L::splitpath(filepath, nullptr, 0, nullptr, 0, nullptr, 0, extn, _countof(extn));
			while (*ext != 0 && *ext == '.') ++ext;

			EModelFileFormat format;
			return EModelFileFormat_::TryParse(format, ext, false) ? format : EModelFileFormat::Unknown;
		}
	}
}
