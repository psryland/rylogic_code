//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

#include <pix3.h>
#pragma comment(lib, "WinPixEventRuntime.lib")

namespace pr::rdr12
{
	struct PixEvent
	{
		// Notes:
		//  - Command list overloads need to be called on active (i.e. not closed) command lists
		ID3D12GraphicsCommandList* m_cmd_list;
		bool m_active;

		PixEvent(pr::Colour32 colour, wchar_t const* message)
			:m_cmd_list()
			,m_active()
		{
			colour.a = 0xff;
			PIXBeginEvent(colour.argb, message);
			m_active = true;
		}
		PixEvent(ID3D12GraphicsCommandList* cmd_list, pr::Colour32 colour, wchar_t const* message)
			:m_cmd_list(cmd_list)
			,m_active()
		{
			PIXBeginEvent(cmd_list, colour.argb, message);
			m_active = true;
		}
		PixEvent(PixEvent&& rhs) noexcept
			:m_cmd_list(rhs.m_cmd_list)
			,m_active(rhs.m_active)
		{
			rhs.m_active = false;
		}
		PixEvent(PixEvent const& rhs) = delete;
		PixEvent& operator=(PixEvent&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			std::swap(m_cmd_list, rhs.m_cmd_list);
			std::swap(m_active, rhs.m_active);
			return *this;
		}
		PixEvent& operator=(PixEvent const& rhs) = delete;
		~PixEvent()
		{
			if (!m_active) return;
			if (m_cmd_list)
				PIXEndEvent(m_cmd_list);
			else
				PIXEndEvent();
		}
	};
}
/* todo
*/