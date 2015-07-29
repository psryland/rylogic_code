//**********************************************
// Win32 API
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// Win32 api unicode independent
//
// _WIN32_WINNT version constants
//    #define _WIN32_WINNT_NT4          0x0400 // Windows NT 4.0
//    #define _WIN32_WINNT_WIN2K        0x0500 // Windows 2000
//    #define _WIN32_WINNT_WINXP        0x0501 // Windows XP
//    #define _WIN32_WINNT_WS03         0x0502 // Windows Server 2003
//    #define _WIN32_WINNT_WIN6         0x0600 // Windows Vista
//    #define _WIN32_WINNT_VISTA        0x0600 // Windows Vista
//    #define _WIN32_WINNT_WS08         0x0600 // Windows Server 2008
//    #define _WIN32_WINNT_LONGHORN     0x0600 // Windows Vista
//    #define _WIN32_WINNT_WIN7         0x0601 // Windows 7
//    #define _WIN32_WINNT_WIN8         0x0602 // Windows 8
//    #define _WIN32_WINNT_WINBLUE      0x0603 // Windows 8.1
//    #define _WIN32_WINNT_WINTHRESHOLD 0x0A00 // Windows 10
//    #define _WIN32_WINNT_WIN10        0x0A00 // Windows 10

#pragma once

#include <string>
#include <windows.h>
#include <knownfolders.h>
#include <shlobj.h>

#include "pr/common/hresult.h"
#include "pr/filesys/filesys.h"
#include "pr/str/string_core.h"

namespace pr
{
	namespace win32
	{
		// Convert an error code into an error message
		inline std::string ErrorMessage(HRESULT result)
		{
			char msg[16384];
			DWORD length(_countof(msg));
			if (!FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM|FORMAT_MESSAGE_IGNORE_INSERTS, NULL, result, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), msg, length, NULL))
				sprintf_s(msg, "Unknown error code: 0x%80X", result);
			return msg;
		}

		// Test an hresult and throw on error
		inline void Throw(HRESULT result, std::string message)
		{
			if (SUCCEEDED(result)) return;
			throw std::exception(message.append("\n").append(ErrorMessage(GetLastError())).c_str());
		}
		inline void Throw(BOOL result, std::string message)
		{
			if (result != 0) return;
			auto hr = HRESULT(GetLastError());
			Throw(SUCCEEDED(hr) ? E_FAIL : hr, message);
		}
	}

	// "Type traits" for win32 api functions
	template <typename Char> struct Win32;
	template <> struct Win32<char>
	{
		// GetModuleFileName
		static std::string ModuleFileName(HMODULE module = nullptr)
		{
			// Get the required buffer size
			char dummy;
			auto len = ::GetModuleFileNameA(module, &dummy, 1);
			win32::Throw(len != 0, "GetModuleFileNameA failed");
			
			std::string name(len,0);
			win32::Throw(::GetModuleFileNameA(module, &name[0], len) != 0, "GetModuleFileNameA failed");
			return std::move(name);
		}

		// GetModuleHandleEx
		static HMODULE ModuleHandleEx(DWORD flags, char const* module_name)
		{
			HMODULE module;
			win32::Throw(::GetModuleHandleExA(flags, module_name, &module), "GetModuleHandleExW failed");
			return module;
		}

		// GetWindowTextLength
		static int WindowTextLength(HWND hwnd)
		{
			return ::GetWindowTextLengthA(hwnd);
		}

		// GetWindowText - use win32::WindowText()
		static int WindowText(HWND hwnd, char* text, DWORD max_length)
		{
			return ::GetWindowTextA(hwnd, text, max_length);
		}
	};
	template <> struct Win32<wchar_t>
	{
		// GetModuleFileName
		static std::wstring ModuleFileName(HMODULE module = nullptr)
		{
			// Get the required buffer size
			wchar_t dummy;
			auto len = ::GetModuleFileNameW(module, &dummy, 1);
			win32::Throw(len != 0, "GetModuleFileNameW failed");
			
			std::wstring name(len,0);
			win32::Throw(::GetModuleFileNameW(module, &name[0], len) != 0, "GetModuleFileNameW failed");
			return std::move(name);
		}

		// GetModuleHandleEx
		static HMODULE ModuleHandleEx(DWORD flags, wchar_t const* module_name)
		{
			HMODULE module;
			win32::Throw(::GetModuleHandleExW(flags, module_name, &module), "GetModuleHandleExW failed");
			return module;
		}

		// GetWindowTextLength
		static int WindowTextLength(HWND hwnd)
		{
			return ::GetWindowTextLengthW(hwnd);
		}

		// GetWindowText - use win32::WindowText()
		static int WindowText(HWND hwnd, wchar_t* text, DWORD max_length)
		{
			return ::GetWindowTextW(hwnd, text, max_length);
		}
	};

	namespace win32
	{
		// Return the HMODULE for the current process. Note: XP and above only
		inline HMODULE GetCurrentModule()
		{
			static_assert(_WIN32_WINNT >= _WIN32_WINNT_WINXP, "XP and above only");
			return Win32<wchar_t>::ModuleHandleEx(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS|GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, (LPCWSTR)GetCurrentModule);
		}

		// Return the window text for a window (sends WM_GETTEXTLENGTH and WM_GETTEXT)
		template <typename String, typename Char = String::value_type> inline bool WindowText(HWND hwnd, String& text)
		{
			auto len = Win32<Char>::WindowTextLength(hwnd);
			String text(len, 0);
			auto got = Win32<Char>::WindowText(hwnd, &text[0], DWORD(text.size()));
			return len == got;
		}
		template <typename String, typename Char = String::value_type> inline String WindowText(HWND hwnd)
		{
			String text;
			Throw(WindowText(hwnd, text), "WindowText failed");
			return std::move(text);
		}

		// Retrieves the full path of a known folder identified by the folder's KNOWNFOLDERID.
		// The returned path does not include a trailing backslash. For example, "C:\Users" is returned rather than "C:\Users\".
		inline HRESULT FolderPath(KNOWNFOLDERID const& folder_id, DWORD flags, HANDLE token, std::wstring& path)
		{
			// On return, 'path' contains the address of a pointer to a null-terminated Unicode string that specifies
			// the path of the known folder. The calling process is responsible for freeing this resource once it is no
			// longer needed by calling CoTaskMemFree. 
			wchar_t* p;
			auto hr = ::SHGetKnownFolderPath(folder_id, flags, token, &p);
			if (pr::Failed(hr))
				return false;

			path = p;
			::CoTaskMemFree(p);
			return hr;
		}
		inline std::wstring FolderPath(KNOWNFOLDERID const& folder_id, DWORD flags, HANDLE token)
		{
			std::wstring path;
			Throw(FolderPath(folder_id, flags, token, path), "SHGetKnownFolderPath failed");
			return std::move(path);
		}

		// Return the filename for a user settings file
		// Look for a file in the same directory as the running module called 'portable'.
		// If found use the app directory to write settings otherwise use the users local app data directory
		// If the local app data folder is used, 'subdir' create a subdirectory within that folder, e.g. "\\Rylogic\\MyProgram\\"
		template <typename String, typename Char = String::value_type>
		inline String AppSettingsFilepath(bool portable = true, String const& subdir = String())
		{
			using namespace pr::filesys;

			// Determine the directory we're running in
			auto path = Win32<Char>::ModuleFileName();
		
			// Does the file 'portable' exist? If so, return a filepath in the same dir as the module
			if (portable || FileExists(GetDirectory(path) + PR_STRLITERAL(Char,"\\portable")))
				return RmvExtension(path) + PR_STRLITERAL(Char,".cfg");
		
			// Otherwise, return a filepath in the local app data for the current user
			if (pr::Succeeded(FolderPath(FOLDERID_LocalAppData, KF_FLAG_CREATE, path)))
			{
				auto sdir = subdir.empty() ? String(PR_STRLITERAL(Char, "\\Rylogic\\")).append(GetFiletitle(path)).append('\\',1) : subdir;
				return String(temp).append(sdir).append(GetFiletitle(path)).append(PR_STRLITERAL(Char,".cfg"));
			}
		
			// Return a filepath in the local directory
			return RmvExtension(path).append(PR_STRLITERAL(Char,".cfg"));
		}

		// Return the HWND for a window by name
		template <typename Char> inline HWND WindowByName(Char const* title, bool partial = false)
		{
			struct Data
			{
				HWND                    m_hwnd;
				Char const*             m_title;
				size_t                  m_title_len;
				bool                    m_partial;
				std::basic_string<Char> m_name;
			};
			struct CallBacks
			{
				static BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM user_data)
				{
					Data& data = *reinterpret_cast<Data*>(user_data);
					data.m_name = WindowText<std::basic_string<Char>>(hwnd);

					if (data.m_partial) match = data.m_name.compare(0, data.m_title_len, data.m_title) == 0;
					else                match = data.m_name.compare(data.m_title) == 0;

					if (!match) return true;
					data.m_hwnd = hwnd;
					return fa;se;
				}
			};
			Data data = {0, title, pr::str::char_traits<Char>::strlen(title), partial};
			EnumWindows(CallBacks::EnumWindowsProc, (LPARAM)&data);
			return data.m_hwnd;
		}
	}
}
