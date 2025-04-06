//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/ldraw/ldraw.h"

namespace pr::rdr12::ldraw
{
	enum class ECommandId : int
	{
		#define PR_ENUM_LDRAW_COMMANDS(x)\
		x(Invalid       , = HashI("Invalid"       ))\
		x(AddToScene    , = HashI("AddToScene"    )) /* <scene-id> */\
		x(CameraToWorld , = HashI("CameraToWorld" )) /* <scene-id> <o2w> */\
		x(CameraPosition, = HashI("CameraPosition")) /* <scene-id> <pos> */\
		x(ObjectToWorld , = HashI("ObjectToWorld" )) /* <object-name> <o2w> */\
		x(Render        , = HashI("Render"        )) /* <scene-id> */
		PR_ENUM_MEMBERS2(PR_ENUM_LDRAW_COMMANDS)
	};
	PR_ENUM_REFLECTION2(ECommandId, PR_ENUM_LDRAW_COMMANDS);

	// LDraw commands - These must be POD types
	struct alignas(16) Command_Invalid
	{
		ECommandId m_id;
		uint8_t pad[12];
	};
	struct alignas(16) Command_AddToScene
	{
		ECommandId m_id;
		int m_scene_id;
		uint8_t pad[8];
	};
	struct alignas(16) Command_CameraToWorld
	{
		ECommandId m_id;
		uint8_t pad[12];
		m4x4 m_c2w;
	};
	struct alignas(16) Command_CameraPosition
	{
		ECommandId m_id;
		uint8_t pad[12];
		v4 m_pos;
	};
	struct alignas(16) Command_ObjectToWorld
	{
		ECommandId m_id;
		char m_object_name[60];
		m4x4 m_o2w;
	};
	struct alignas(16) Command_Render
	{
		ECommandId m_id;
		int m_scene_id;
		uint8_t pad[8];
	};
}
