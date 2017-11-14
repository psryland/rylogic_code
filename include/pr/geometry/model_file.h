//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/macros/enum.h"
#include "pr/geometry/common.h"
#include "pr/geometry/3ds.h"

namespace pr
{
	namespace geometry
	{
		// Supported model file formats
		#define PR_ENUM(x)\
			x(Unknown , ""    , = 0)\
			x(P3D     , "p3d" , = 1)/* PR3D */\
			x(Max3DS  , "3ds" , = 2)/* 3D Studio Max */
		PR_DEFINE_ENUM3(EModelFileFormat, PR_ENUM);
		#undef PR_ENUM

		struct ModelFileInfo
		{
			EModelFileFormat m_format;
			bool m_is_binary;
		};
		
		// Determine properties of a model file format from the filepath
		template <typename Char> inline ModelFileInfo GetModelFileInfo(Char const* filepath)
		{
			ModelFileInfo info = {};

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

			if (!EModelFileFormat_::TryParse(info.m_format, ext, false))
			{
				info.m_format = EModelFileFormat::Unknown;
				return info;
			}

			// Determine other info about the file format
			switch (info.m_format)
			{
			default: throw std::exception("Unknown model file format");
			case EModelFileFormat::P3D:
				info.m_is_binary = true;
				break;
			case EModelFileFormat::Max3DS:
				info.m_is_binary = true;
				break;
			}

			return info;
		}
	}
}
