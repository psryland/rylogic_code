//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"

namespace pr::physics
{
	// A description of contact between 'objA' and 'objB'
	struct Contact :collision::Contact
	{
		// Notes:
		//  - Collision detection is performed in objA space to prevent floating point accuracy issues.
		//  - The results of the collision; 'axis' and 'point', are in 'objA' space.

		// Transform from B to A space
		m4x4 m_b2a;

		// The relative velocity of objB in objA space (measured at objA 0,0,0)
		v8m m_velocity;

		// The objects that are colliding, defining who is 'A' and 'B'
		RigidBody const* m_objA;
		RigidBody const* m_objB;
		
		// The combined material properties of the two colliding objects
		Material m_mat;
		
		// The parametric value for the time of the collision
		float m_time;

		Contact() = default;
		Contact(RigidBody const& objA, RigidBody const& objB)
			:m_b2a(InvertFast(objA.O2W()) * objB.O2W())
			,m_velocity()
			,m_objA(&objA)
			,m_objB(&objB)
			,m_mat()
			,m_time()
		{}
	};
}
