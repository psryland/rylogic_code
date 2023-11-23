//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#pragma comment(lib, "WinPixEventRuntime.lib")

namespace pr::rdr12
{
	struct PixEvent
	{
		ID3D12GraphicsCommandList* m_cmd_list;

		PixEvent(pr::Colour32 colour, char const* message)
			:m_cmd_list()
		{
			colour.a = 0xff;
			PIXBeginEvent(colour.argb, message);
		}
		PixEvent(ID3D12GraphicsCommandList* cmd_list, pr::Colour32 colour, char const* message)
			:m_cmd_list(cmd_list)
		{
			PIXBeginEvent(cmd_list, colour.argb, message);
		}
		~PixEvent()
		{
			if (m_cmd_list)
				PIXEndEvent(m_cmd_list);
			else
				PIXEndEvent();
		}
	};
}
/* todo
*/