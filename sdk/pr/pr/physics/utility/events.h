//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_EVENTS_H
#define PR_PHYSICS_EVENTS_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		// A change to a rigid body event
		struct RBEvent
		{
			enum EType { EType_ShapeChanged };
			Rigidbody* m_rb;
			EType      m_type;
		};
	}
}

#endif
