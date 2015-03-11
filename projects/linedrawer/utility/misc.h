//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once

#include "linedrawer/main/forward.h"
#include "pr/gui/misc.h"
#include "pr/gui/menu_helper.h"

namespace ldr
{
	// Status message priority buffer
	struct StatusPri
	{
		DWORD m_last_update;
		int   m_priority;
		DWORD m_min_display_time_ms;
		CFont m_normal_font;
		CFont m_bold_font;

		StatusPri()
		:m_last_update(0)
		,m_priority(0)
		,m_min_display_time_ms(0)
		{
			m_normal_font.CreatePointFont(80, "Sans Merif");
			m_bold_font.CreatePointFont(80, "Sans Merif", 0, true);
		}
	};

	// A interface for classes that handle user input
	struct IInputHandler
	{
		virtual ~IInputHandler() {}

		// Called when input focus is given or removed. Implementors should use
		// LostInputFocus() to abort any control operations in progress.
		virtual void GainInputFocus(IInputHandler* gained_from) = 0;
		virtual void LostInputFocus(IInputHandler* lost_to) = 0;

		// Keyboard input.
		// Return true if the key was handled and should not be
		// passed to anything else that might want the key event.
		virtual bool KeyInput(UINT vk_key, bool down, UINT flags, UINT repeats) = 0;

		// Mouse input.
		// 'pos_ns' is the normalised screen space position of the mouse
		//   i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal cartesian axes
		// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
		// 'start_or_end' is true on mouse down/up
		// Returns true if the camera has moved or objects in the scene have moved
		virtual void MouseInput(pr::v2 const& pos_ns, int button_state, bool start_or_end) = 0;
		virtual void MouseClick(pr::v2 const& pos_ns, int button_state) = 0;
		virtual void MouseWheel(pr::v2 const& pos_ns, float delta) = 0;
	};
}
