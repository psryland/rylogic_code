//*********************************
// GUID helper functions
//  Copyright © Rylogic Ltd 2006
//*********************************

#pragma once
#ifndef PR_GUID_H
#define PR_GUID_H

#include <windows.h>
#include <guiddef.h>
#include "pr/common/to.h"

// Required lib: rpcrt4.lib

namespace pr
{
	const GUID GUID_INVALID = { 0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	template <typename ToType> inline ToType To(GUID x) { static_assert(false, "No conversion from to this type available"); }

	// Create a new GUID
	inline GUID GenerateGUID()
	{
		GUID guid;
		if (FAILED(CoCreateGuid(&guid))) return GUID_INVALID;
		return guid;
	}

	// Convert a GUID into a string
	template <> inline std::string To<std::string>(GUID guid)
	{
		RPC_CSTR* str = 0;
		UuidToStringA(static_cast<UUID*>(&guid), str);
		std::string guid_str(reinterpret_cast<char const*>(str));
		RpcStringFreeA(str);
		return guid_str;
	}

	// Convert a GUID into a string
	template <> inline std::wstring To<std::wstring>(GUID guid)
	{
		RPC_WSTR* str = 0;
		UuidToStringW(static_cast<UUID*>(&guid), str);
		std::wstring guid_str(reinterpret_cast<wchar_t const*>(str));
		RpcStringFreeW(str);
		return guid_str;
	}
}

#endif
