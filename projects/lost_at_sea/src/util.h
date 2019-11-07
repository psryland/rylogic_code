//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************

#pragma once

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/settings.h"

namespace las
{
	// The directory that the app is running from
	std::filesystem::path ModuleDir()
	{
		return pr::win32::ExeDir();
	}

	// Return true if the app is running as a portable app
	inline bool IsPortable()
	{
		// Look for a file called "portable" in the same directory as the main app
		return std::filesystem::exists(ModuleDir() / L"portable");
	}
	
	// Return the path to the settings file
	inline std::filesystem::path SettingsPath()
	{
		return ModuleDir() / L"settings.xml";
	}

	// Return the full path
	inline std::filesystem::path DataPath(std::filesystem::path const& relpath)
	{
		return ModuleDir() / relpath;
	}
}
