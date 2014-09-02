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
		inline ModelFileInfo GetModelFileInfo(char const* filepath)
		{
			ModelFileInfo info = {};

			// Read the file extension
			char extn[_MAX_EXT] = {}, *ext = &extn[0];
			_splitpath(filepath, nullptr, nullptr, nullptr, extn);
			while (*ext != 0 && *ext == '.') ++ext;

			if (!EModelFileFormat::TryParse(info.m_format, ext, false))
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
