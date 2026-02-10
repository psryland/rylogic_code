//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once
#include "../forward.h"

namespace pr::colision
{
	enum class EResult
	{
		Success = 0,
		Failed = 0x80000000,
		InvalidSettings,
		MaterialIndexOutOfRange,
		VolumeTooSmall,
	};

	// Result testing
	inline bool Failed   (EResult result) { return result  < 0; }
	inline bool Succeeded(EResult result) { return result >= 0; }
	inline void Verify   (EResult result) { (void)result; PR_ASSERT(1, Succeeded(result), "Verify failure"); }

}
