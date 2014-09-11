//*******************************************************************
// A collection of window related functions
//  Copyright (c) Rylogic Ltd 2008
//*******************************************************************

#pragma once
#ifndef PR_WINDOW_FUNCTIONS_H
#define PR_WINDOW_FUNCTIONS_H

#ifndef _WIN32_WINNT
#define _WIN32_WINNT 0x0600
#elif _WIN32_WINNT < 0x0500
#error "_WIN32_WINNT >= 0x0500 required"
#endif

#include <tchar.h>
#include <string>
#include <knownfolders.h>
#include <shlobj.h>

namespace pr
{
	// Return the filename for a user settings file
	// Look for a file in the same directory as the running module called 'portable'.
	// If found use the app directory to write settings otherwise use the users local app data directory
	// If the local app data folder is used, 'subdir' create a subdirectory within that folder, e.g. "\\Rylogic\\MyProgram\\"
	template <typename String> inline String GetAppSettingsFilepath(HWND hwnd, bool portable = true, String const& subdir = "")
	{
		// Determine the directory we're running in
		char temp[MAX_PATH]; GetModuleFileNameA(0, temp, MAX_PATH);
		String path = temp;
		
		// Does the file 'portable' exist? If so, return a filepath in the same dir as the module
		if (portable || pr::filesys::FileExists(pr::filesys::GetDirectory(path) + "\\portable"))
			return pr::filesys::RmvExtension(path) + ".cfg";
		
		// Otherwise, return a filepath in the local app data for the current user
		if (pr::Succeeded(SHGetFolderPathA(hwnd, CSIDL_LOCAL_APPDATA, 0, CSIDL_FLAG_CREATE, temp)))
		{
			String sdir = subdir.empty() ? String("\\Rylogic\\")+pr::filesys::GetFiletitle(path)+"\\" : subdir;
			return String(temp) + sdir + pr::filesys::GetFiletitle(path) + ".cfg";
		}
		
		// Return a filepath in the local directory
		return pr::filesys::RmvExtension(path) + ".cfg";
	}
	
	// Return the HMODULE for the current process.
	// Note: XP and above only
	inline HMODULE GetCurrentModule()
	{
		HMODULE hModule = 0;
		GetModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCTSTR)GetCurrentModule, &hModule);
		return hModule;
	}
	
	// Return the HWND for a window by name
	inline HWND GetWindowByName(TCHAR const* title, bool partial = false)
	{
		struct Data { HWND m_hwnd; TCHAR const* m_title; bool m_partial; };
		struct CallBacks
		{
			static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM user_data)
			{
				Data& data = *reinterpret_cast<Data*>(user_data);

				bool match = false;
				TCHAR window_name[MAX_PATH]; GetWindowText(hwnd, window_name, MAX_PATH);
				if( data.m_partial )	match = _tcsncmp(window_name, data.m_title, _tcslen(data.m_title)) == 0;
				else					match = _tcscmp (window_name, data.m_title) == 0;
				if( !match ) return TRUE;
				data.m_hwnd = hwnd;
				return FALSE;
			}
		};
		Data data = {0, title, partial};
		EnumWindows(CallBacks::EnumWindowsProc, (LPARAM)&data);
		return data.m_hwnd;
	}
}

#endif//PR_WINDOW_FUNCTIONS_H
