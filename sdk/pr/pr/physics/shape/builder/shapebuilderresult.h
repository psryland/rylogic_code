//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_SHAPE_BUILDER_RESULT_H
#define PR_PHYSICS_SHAPE_BUILDER_RESULT_H

#include "pr/common/assert.h"

namespace pr
{
	namespace ph
	{
		enum EResult
		{
			EResult_Success	= 0,
			EResult_Failed  = 0x80000000,
			EResult_InvalidSettings,
			EResult_MaterialIndexOutOfRange,
			EResult_VolumeTooSmall,
		};
	}

	// Result testing
	inline bool Failed   (ph::EResult result)	{ return result  < 0; }
	inline bool Succeeded(ph::EResult result)	{ return result >= 0; }
	inline void Verify   (ph::EResult result)	{ (void)result; PR_ASSERT(1, Succeeded(result), "Verify failure"); }

}

#endif

