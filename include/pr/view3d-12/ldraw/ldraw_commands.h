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
		x(Invalid       , = HashI("Invalid"       ))\
		x(AddToScene    , = HashI("AddToScene"    )) /* <scene-id> */\
		x(CameraToWorld , = HashI("CameraToWorld" )) /* <scene-id> <o2w> */\
		x(CameraPosition, = HashI("CameraPosition")) /* <scene-id> <pos> */\
		x(ObjectToWorld , = HashI("ObjectToWorld" )) /* <object-name> <o2w> */\
		x(Render        , = HashI("Render"        )) /* <scene-id> */
	PR_DEFINE_ENUM2_BASE(ECommandId, PR_ENUM_LDRAW_COMMANDS, int);

	// An instruction to do something with ldraw objects
	struct Command
	{
		ECommandId m_id;
		byte_data<4> m_data;
	};

	// Process an ldraw command
	void ExecuteCommands(SourceBase& source, Context& context);
}
