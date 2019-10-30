//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include <filesystem>
#include "pr/geometry/common.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/stl.h"

namespace pr::geometry
{
	// Supported model file formats
	#define PR_ENUM(x)\
		x(Unknown , ""    , = 0) /**/\
		x(P3D     , ".p3d",    ) /* PR3D */\
		x(Max3DS  , ".3ds",    ) /* 3D Studio Max */\
		x(STL     , ".stl",    ) /* Stereolithography CAD model */
	PR_DEFINE_ENUM3(EModelFileFormat, PR_ENUM);
	#undef PR_ENUM
		
	// Determine the model file format from the filepath
	inline EModelFileFormat GetModelFormat(std::filesystem::path const& filepath)
	{
		// Read the file extension
		auto extn = filepath.extension();
		for (auto mff : Enum<EModelFileFormat>::Members())
		{
			if (!str::EqualI(extn.c_str(), Enum<EModelFileFormat>::ToStringW(mff))) continue;
			return mff;
		}
		return EModelFileFormat::Unknown;
	}
}
