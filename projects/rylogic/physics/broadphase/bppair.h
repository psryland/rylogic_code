//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

// This object is used to indicate two entities are overlapping in the broadphase

#pragma once
#ifndef PR_BROADPHASE_PAIR_H
#define PR_BROADPHASE_PAIR_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		// These are the objects returned from the broadphase collision detection
		struct BPPair
		{
			BPEntity const* m_objectA;
			union {
			BPEntity const* m_objectB;
			void const*     m_objB_void;
			};
		};
	}
}

#endif
