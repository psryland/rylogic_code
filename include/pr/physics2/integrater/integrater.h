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
			// f = net spatial force acting
			// I = spatial inertia tensor
			// v = spatial velocity
			// Iv = momentum
			// a = spatial acceleration
			//
			// f = I*a + v x Iv

			(void)rb,elapsed_seconds;

			// Reset the forces on the body to zero
			rb.m_force = v8f();
		}
	}
}
