//*********************************
// GUID helper functions
//  Copyright © Rylogic Ltd 2006
//*********************************

#ifndef PR_GUID_H
#define PR_GUID_H

#include <windows.h>
#include <guiddef.h>
#include "pr/str/wstring.h"

// Required lib: rpcrt4.lib

namespace pr
{
	const GUID GUID_INVALID = { 0x00000000, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
	
	// Create a new GUID
	inline GUID GenerateGUID()
	{
		GUID guid;
		if (FAILED(CoCreateGuid(&guid))) return GUID_INVALID;
		return guid;
	}
	
	// Convert a GUID into a string
	inline std::string GUIDToString(GUID guid)
	{
		RPC_CSTR* str = 0;
		UuidToStringA(static_cast<UUID*>(&guid), str);
		std::string guid_str(reinterpret_cast<char const*>(str));
		RpcStringFreeA(str);
		return guid_str;
	}
}

#endif
