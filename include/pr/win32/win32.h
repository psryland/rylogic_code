//**********************************************
// Win32 API
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// Win32 API Unicode independent
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

#include <sdkddkver.h>
#include <windows.h>
#include <knownfolders.h>
#include <shlobj.h>

#include "pr/common/fmt.h"
#include "pr/common/hresult.h"
#include "pr/filesys/filesys.h"
#include "pr/str/string_core.h"
#include "pr/str/string_util.h"

namespace pr
{
	// "Type traits" for win32 API functions
	template <typename Char> struct Win32;
	template <> struct Win32<char>
	{
		// GetModuleFileName
		static std::string ModuleFileName(HMODULE module = nullptr)
		{
			// Get the required buffer size
			char buf[_MAX_PATH];
			auto len = ::GetModuleFileNameA(module, &buf[0], _countof(buf));
			if (::GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				return std::string(buf, len);

			std::string name(len, 0);
			len = ::GetModuleFileNameA(module, &name[0], len);
			Throw(len == name.size(), "GetModuleFileNameW failed");
			return std::move(name);
		}

		// GetModuleHandleEx
		static HMODULE ModuleHandleEx(DWORD flags, char const* module_name)
		{
			HMODULE module;
			Throw(::GetModuleHandleExA(flags, module_name, &module), "GetModuleHandleExW failed");
			return module;
		}

		// ReplaceFile
		static bool FileReplace(char const* replacee, char const* replacer)
		{
			return ::ReplaceFileA(replacee, replacer, nullptr, REPLACEFILE_WRITE_THROUGH|REPLACEFILE_IGNORE_MERGE_ERRORS, nullptr, nullptr) != 0;
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
			wchar_t buf[_MAX_PATH];
			auto len = ::GetModuleFileNameW(module, &buf[0], _countof(buf));
			if (::GetLastError() != ERROR_INSUFFICIENT_BUFFER)
				return std::wstring(buf, len);

			std::wstring name(len, 0);
			len = ::GetModuleFileNameW(module, &name[0], len);
			Throw(len == name.size(), "GetModuleFileNameW failed");
			return std::move(name);
		}

		// GetModuleHandleEx
		static HMODULE ModuleHandleEx(DWORD flags, wchar_t const* module_name)
		{
			HMODULE module;
			Throw(::GetModuleHandleExW(flags, module_name, &module), "GetModuleHandleExW failed");
			return module;
		}

		// ReplaceFile
		static bool FileReplace(wchar_t const* replacee, wchar_t const* replacer)
		{
			return ::ReplaceFileW(replacee, replacer, nullptr, REPLACEFILE_WRITE_THROUGH|REPLACEFILE_IGNORE_MERGE_ERRORS, nullptr, nullptr) != 0;
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
		// Return the name of the currently running executable
		template <typename String, typename Char = String::value_type> inline String ExePath()
		{
			return std::move(String(Win32<Char>::ModuleFileName()));
		}
		template <typename String, typename Char = String::value_type> inline String ExeDir()
		{
			auto dir = pr::filesys::GetDirectory(ExePath<String>());
			return std::move(dir);
		}

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
				return hr;

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
		// If the local app data folder is used, 'subdir' creates a subdirectory within that folder, e.g. "\\Rylogic\\MyProgram\\"
		inline std::wstring AppSettingsFilepath(bool portable, std::wstring const& subdir = std::wstring())
		{
			using namespace pr::filesys;

			// Determine the directory we're running in
			auto module_path       = Win32<wchar_t>::ModuleFileName();
			auto module_dir        = GetDirectory(module_path);
			auto module_ftitle     = GetFiletitle(module_path);
			auto settings_filename = module_ftitle + L".cfg";

			// Does the file 'portable' exist? If so, return a filepath in the same dir as the module
			if (portable || FileExists(module_dir + L"\\portable"))
				return CombinePath(module_dir, settings_filename); // turn .\path\module.exe into .\path\module.cfg for settings
		
			// Otherwise, return a filepath in the local app data for the current user
			std::wstring local_app_data;
			if (pr::Succeeded(FolderPath(FOLDERID_LocalAppData, KF_FLAG_CREATE, nullptr, local_app_data)))
			{
				if (subdir.empty()) local_app_data.append(L"\\Rylogic\\").append(module_ftitle).append(L"\\");
				else                local_app_data.append(subdir);
				return CombinePath(local_app_data, settings_filename);
			}
		
			// Return a filepath in the local directory
			return CombinePath(module_path, settings_filename);
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

		// Load a dependent dll
		// 'dir' can use substitution values L".\\lib\\$(platform)\\$(config)\\"
		// 'Context' is used to ensure the module handle for the dll only exists once for each dll
		template <typename Context> inline HMODULE LoadDll(std::wstring const& dllname, std::wstring dir)
		{
			using namespace pr::filesys;

			static HMODULE module = nullptr;
			if (module != nullptr)
				return module;

			#ifdef NDEBUG
			bool const debug = false;
			#else
			bool const debug = true;
			#endif

			// Try the lib folder. Load the appropriate dll for the platform
			pr::str::Replace(dir, "$(platform)", sizeof(int) == 8 ? "x64" : "x86");
			pr::str::Replace(dir, "$(config)", debug ? "debug" : "release");
			auto exe_dir = ExeDir<std::wstring>();

			auto dllpath = CombinePath(exe_dir, dir, dllname);
			if (FileExists(dllpath))
			{
				module = ::LoadLibraryW(dllpath.c_str());
				if (module != nullptr)
					return module;
			}

			// Try the local directory
			dllpath = CombinePath(exe_dir, dllname);
			if (FileExists(dllpath))
			{
				module = ::LoadLibraryW(dllname.c_str());
				if (module != nullptr)
					return module;
			}

			throw std::exception(pr::FmtS("Failed to load dependency '%S'", dllname.c_str()));
		}
		template <typename Context> inline HMODULE LoadDll(std::wstring const& dllname)
		{
			return LoadDll<Context>(dllname, L".\\");
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::win32
{
	PRUnitTest(Win32Tests)
	{
		auto a = AppSettingsFilepath(false);
	}
}
#endif
