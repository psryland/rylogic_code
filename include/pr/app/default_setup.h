//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#include "pr/app/forward.h"

namespace pr::app
{
	// This type is a default and example of a set up object for the app.
	struct DefaultSetup
	{
		using RSettings = pr::rdr::RdrSettings;
		using WSettings = pr::rdr::WndSettings;

		// The Main object contains a user defined 'UserSettings' type which may be needed before
		// configuring the renderer. In order to construct the UserSettings instance a method with
		// the name 'UserSettings' is called with its return type provided to the user defined type.
		// The return type can be anything that the user defined settings type will accept.
		// e.g.
		//   Return an instance of the user defined type, to construct by copy constructor
		//   Return 'this' and allow the settings object to read members of this type
		//   Return a filepath that the settings can load from.
		//   Unfortunately it can't return void to pass to a parameterless constructor so use int:-/
		int UserSettings()
		{
			return 0;
		}

		// Return settings to configure the render
		RSettings RdrSettings()
		{
			return RSettings(GetModuleHandleW(nullptr), D3D11_CREATE_DEVICE_FLAG(0));
		}

		// Return settings for the render window
		WSettings RdrWindowSettings(HWND hwnd, iv2 const& client_area)
		{
			return WSettings(hwnd, TRUE, D3D11_CREATE_DEVICE_FLAG(0), client_area);
		}
	};
}
