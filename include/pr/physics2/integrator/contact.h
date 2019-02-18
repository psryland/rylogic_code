//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"
#include "pr/physics2/utility/ldraw.h"

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

		// The collision point adjusted by the collision time
		v4 m_point_at_t;

		// The objects that are colliding, defining who is 'A' and 'B'
		RigidBody const* m_objA;
		RigidBody const* m_objB;
		
		// The combined material properties of the two colliding objects
		Material m_mat;

		// The relative time of the collision (in seconds). 0 = now, -dt = previous step. (used to order collision resolution)
		float m_time;

		Contact()
			:m_b2a()
			,m_velocity()
			,m_point_at_t()
			,m_objA()
			,m_objB()
			,m_mat()
			,m_time()
		{}
		Contact(RigidBody const& objA, RigidBody const& objB)
			:Contact()
		{
			m_objA = &objA;
			m_objB = &objB;
			update(0);
		}

		// Adjust the collision data to the given time
		void update(float dt)
		{
			// 'm_b2a' is the position/orientation of objB in objA space at 'time'
			// 'm_velocity' is value of objB's velocity vector field sampled at objA's origin.
			// 'm_point_at_t' is adjusted by half 'dt' because it is the average of the overlap.
			m_b2a = InvertFast(m_objA->O2W(dt)) * m_objB->O2W(dt);
			m_velocity = m_b2a * m_objB->VelocityOS() - m_objA->VelocityOS();
			m_point_at_t = m_point + 0.5f * dt * m_velocity.LinAt(m_point);
			m_time = dt;
		}
	};

	// Dump the collision scene to LDraw script
	template <typename = void> void Dump(Contact const& c)
	{
		std::string str;
		ldr::RigidBody(str, "ObjA", 0x80FF0000, *c.m_objA, ldr::ERigidBodyFlags::None, &m4x4Identity);
		ldr::RigidBody(str, "ObjB", 0x8000FF00, *c.m_objB, ldr::ERigidBodyFlags::None, &c.m_b2a);
		ldr::VectorField(str, "Velocity", 0xFFFFFF00, (v8)c.m_velocity * 0.1f, v4Origin, 2, 0.25f);
		ldr::Arrow(str, "Normal", 0xFFFFFFFF, ldr::EArrowType::Fwd, c.m_point_at_t, c.m_axis * 0.1f, 5);
		ldr::Box(str, "Contact", 0xFFFFFF00, 0.005f, c.m_point_at_t.w1());
		ldr::Write(str, L"\\dump\\collision.ldr");
	}
}
