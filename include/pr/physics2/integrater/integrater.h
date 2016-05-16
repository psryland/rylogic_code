//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr
{
	namespace physics
	{
		// Evolve a rigid body forward in time. 
		// This advances the position/orientation of the provided physics object by 'elapsed_seconds'
		// This function assumes the 'm_force' member of the rigid bodies has been set, so all constraint
		// forces, gravity, etc should have been applied before calling this function. On exit, the force
		// member is reset to 0.
		template <typename RB, typename = enable_if_rb<RB>>
		void Evolve(RB& rb, double elapsed_seconds)
		{
			// Equation of Motion: f = d(Iv)/dt = I*a + v x Iv
			// f = net force acting
			// I = inertia tensor
			// v = velocity
			// Iv = momentum
			// a = acceleration
		}
	}
}
