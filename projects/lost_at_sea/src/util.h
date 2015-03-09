//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************

#pragma once

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/settings.h"

namespace las
{
	// Return the directory that this app is running in
	inline wstring ModuleDir()
	{
		wchar_t temp[MAX_PATH]; GetModuleFileNameW(0, temp, MAX_PATH);
		return pr::filesys::GetDirectory<wstring>(temp);
	}
	
	// Return true if the app is running as a portable app
	inline bool IsPortable()
	{
		// Look for a file called "portable" in the same directory as the main app
		return pr::filesys::FileExists(pr::filesys::CombinePath<wstring>(ModuleDir(), L"portable"));
	}
	
	// Return the path to the settings file
	inline std::string SettingsPath()
	{
		auto path = pr::filesys::CombinePath<wstring>(ModuleDir(), L"settings.xml");
		return pr::To<std::string>(path);
		//wstring dir = ModuleDir();
		//
		//// If portable use a local settings file
		//if (IsPortable())
		//	return pr::filesys::CombinePath<wstring>(dir, L"settings.cfg");
		//
		//// Otherwise find the user app data folder
		//wchar_t temp[MAX_PATH];
		//if (pr::Failed(SHGetFolderPathW(hwnd, CSIDL_LOCAL_APPDATA, 0, CSIDL_FLAG_CREATE, temp)))
		//	throw Exception(EResult::SettingsPathNotFound, "The settings path could not be created");
		//
		//wstring path = temp;
		//path += L"\\";
		//path += pr::To<wstring>(AppVendor());
		//path += L"\\";
		//path += pr::To<wstring>(AppTitle());
		//path += L"\\settings.cfg";
		//return path;
	}

	// Return the full path
	inline wstring DataPath(wstring const& relpath)
	{
		auto dir = ModuleDir();
		return pr::filesys::CombinePath<wstring>(dir, relpath);
	}
}
