//**********************************************
// Win32 API
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// Win32 api unicode independent

#pragma once

#include <string>
#include <windows.h>

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
		static std::string ModuleFileName(HMODULE module)
		{
			// Get the required buffer size
			char dummy;
			auto len = ::GetModuleFileNameA(module, &dummy, 1);
			win32::Throw(len != 0, "GetModuleFileNameA failed");
			
			std::string name(len,0);
			win32::Throw(::GetModuleFileNameA(module, &name[0], len) != 0, "GetModuleFileNameA failed");
			return std::move(name);
		}
	};
	template <> struct Win32<wchar_t>
	{
		// GetModuleFileName
		static std::wstring ModuleFileName(HMODULE module)
		{
			// Get the required buffer size
			wchar_t dummy;
			auto len = ::GetModuleFileNameW(module, &dummy, 1);
			win32::Throw(len != 0, "GetModuleFileNameW failed");
			
			std::wstring name(len,0);
			win32::Throw(::GetModuleFileNameW(module, &name[0], len) != 0, "GetModuleFileNameW failed");
			return std::move(name);
		}
	};
}
