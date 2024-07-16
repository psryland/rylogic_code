//********************************************************************
// Key State
//  Copyright (c) Rylogic Ltd 2009
//********************************************************************
// Helper functions for reading the state of a key
// Usage:
//	if( KeyState('A') ) { AAAAAA }
//
//	VK_SPACE, VK_SHIFT, VK_CONTROL
//	VK_LBUTTON, VK_MBUTTON, VK_RBUTTON
//	VK_LSHIFT, VK_RSHIFT, VK_LCONTROL, VK_RCONTROL 
//
#pragma once
#include <windows.h>

namespace pr
{
	// True if a virtual key is currently down
	inline bool KeyDown(int vk_key)
	{
		short key_state = GetAsyncKeyState(vk_key);
		return (key_state & 0x8000) != 0;
	}

	// True if a virtual key was pressed
	inline bool KeyPressed(int vk_key)
	{
		short key_state = GetAsyncKeyState(vk_key);
		return (key_state & 0x0001) != 0;
	}

	// True if there is an key down and up event
	inline bool KeyPress(int vk_key)
	{
		if   (!KeyDown(vk_key)) return false;
		while (KeyDown(vk_key)) Sleep(10);
		return true;
	}
}

