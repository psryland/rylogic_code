//*****************************************************************************************
// LineDrawer
//  Copyright (c) Rylogic Ltd 2009
//*****************************************************************************************
#pragma once
#include "linedrawer/main/forward.h"

namespace ldr
{
	// A interface for classes that handle user input
	struct IInputHandler
	{
		using ENavBtn = pr::camera::ENavBtn;

		virtual ~IInputHandler() {}
		IInputHandler() {}
		IInputHandler(IInputHandler const&) = delete;

		// Called when input focus is given or removed. Implementers should use
		// LostInputFocus() to abort any control operations in progress.
		virtual void GainInputFocus(IInputHandler* gained_from) = 0;
		virtual void LostInputFocus(IInputHandler* lost_to) = 0;

		// Keyboard input.
		// Return true if the key was handled and should not be
		// passed to anything else that might want the key event.
		virtual bool KeyInput(UINT vk_key, bool down, UINT flags, UINT repeats) = 0;

		// Mouse input.
		// 'pos_ns' is the normalised screen space position of the mouse
		//   i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
		// 'button_state' is the state of the mouse buttons (pr::camera::ENavKey)
		// 'start_or_end' is true on mouse down/up
		// Returns true if the scene needs refreshing
		virtual bool MouseInput(pr::v2 const& pos_ns, ENavBtn button_state, bool start_or_end) = 0;
		virtual bool MouseClick(pr::v2 const& pos_ns, ENavBtn button_state) = 0;
		virtual bool MouseWheel(pr::v2 const& pos_ns, float delta) = 0;
	};
}
