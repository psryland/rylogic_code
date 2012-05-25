//*********************************************
// Physics engine
//  Copyright © Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PHYSICS_GLOBAL_FUNCTIONS_H
#define PR_PHYSICS_GLOBAL_FUNCTIONS_H

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		// Transform an inertia tensor using the parallel axis theorem.
		// 'offset' is the distance from (or toward) the centre of mass (determined by 'translate_type')
		// 'inertia' and 'offset' must be in the same frame.
		namespace ParallelAxisTranslate { enum Type { TowardCoM, AwayFromCoM }; }
		void ParallelAxisTranslateInertia(m3x3& inertia, pr::v4 const& offset, float mass, ParallelAxisTranslate::Type translate_type);
		
		// Create inertia tensors
		inline m3x3 InertiaTensorWS(m3x3 const& orientation, m3x3 const& os_inertia_tensor)
		{
			return orientation * os_inertia_tensor * GetTranspose(orientation);
		}
		inline m3x3 InvInertiaTensorWS(m3x3 const& orientation, m3x3 const& os_inv_inertia_tensor)
		{
			return orientation * os_inv_inertia_tensor * GetTranspose(orientation);
		}
	}
}

#endif
