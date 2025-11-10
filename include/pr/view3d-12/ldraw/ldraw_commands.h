//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
#pragma once
#include "pr/view3d-12/ldraw/ldraw.h"

namespace pr::rdr12::ldraw
{
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
