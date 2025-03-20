//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"

namespace pr::rdr12::ldraw
{
	#define PR_ENUM_LDRAW_COMMANDS(x)\
		x(Invalid, = HashI("Invalid"))\
		x(AddToScene, = HashI("AddToScene"))\
		x(Camera, = HashI("Camera"))\
		x(Transform, = HashI("Transform"))
	PR_DEFINE_ENUM2_BASE(ECommandId, PR_ENUM_LDRAW_COMMANDS, int);

	// An instruction to do something with ldraw objects
	struct Command
	{
		using CommandData = pr::vector<std::byte, sizeof(v4), false, alignof(v4)>;

		CommandData m_data;
		ECommandId m_id;
	};
}
