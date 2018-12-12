//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/rigid_body/rigid_body.h"
#include "pr/physics2/integrator/contact.h"
#include "pr/physics2/integrator/integrator.h"
#include "pr/physics2/integrator/impulse.h"
#include "pr/physics2/material/material_map.h"
#include "pr/physics2/utility/ldraw.h"

namespace pr::physics
{
	// A container object that groups the parts of a physics system together
	template <typename TBroadphase, typename TMaterials = MaterialMap>
	struct Engine
	{
		TBroadphase m_broadphase;
		TMaterials m_materials;

		// Evolve the physics objects forward in time and resolve any collisions
		template <typename TRigidBodyCont>
		void Step(float dt, TRigidBodyCont& bodies)
		{
			// Before here, callers should have set forces on the rigid bodies (including gravity).
			// Todo: A lot of this could be done in parallel/pipelined...

			// Advance all bodies to 't + dt'
			for (auto& body : bodies)
				Evolve(body, dt);

			// Perform collision detection
			std::vector<Contact> collision_queue;
			m_broadphase.EnumOverlappingPairs([&](void*, RigidBody const& objA, RigidBody const& objB)
			{
				// Narrow phase collision detection
				auto c = Contact{objA, objB};
				if (NarrowPhaseCollision(dt, c))
				{
					Dump(c);
					collision_queue.push_back(c);
				}
			});

			// Sort the collisions by time
			// Todo: for parallel processing, collision queue should be broken up into islands of objects that affect each other.
			// Assign an increasing 'island' number to each Contact. If a preceding contact involves one of the same objects, use the lowest number
			std::sort(std::begin(collision_queue), std::end(collision_queue), [](auto& lhs, auto& rhs){ return lhs.m_time < rhs.m_time; });

			// Resolve collisions
			for (auto& c : collision_queue)
				ResolveCollision(c);
		}

		// Narrow phase collision detection
		// Returns true if 'objA' and 'objB' are in contact
		// 'c' is only valid if the function returns true.
		bool NarrowPhaseCollision(float dt, Contact& c)
		{
			// Notes:
			//  t0 = 't', t1 = 't + dt'. Objects are currently at t1.
			
			auto& objA = *c.m_objA;
			auto& objB = *c.m_objB;

			// Perform the narrow phase collision detection in 'objA' space to reduce floating point errors
			if (!collision::Collide(objA.Shape(), m4x4Identity, objB.Shape(), c.m_b2a, c))
				return false;

			// Get the relative velocity of the bodies.
			c.m_velocity = c.m_b2a * objB.VelocityOS() - objA.VelocityOS();

			// If the collision point is moving out of collision, ignore the collision.
			auto rel_vel_at_point = c.m_velocity.LinAt(c.m_point);
			if (Dot(rel_vel_at_point, c.m_axis) > 0)
				return false;

			// Get the combined material properties of the contact
			// Todo: in the previous implementation, micro velocity collisions changed the material properties.
			c.m_mat = m_materials(c.m_mat_idA, c.m_mat_idB);

			// Determine the parametric value for the time of the collision by estimating the A-space position
			// of 'c.m_point' at t0 assuming linear velocity (because it's faster and easier)
			auto point_at_t0 = c.m_point - dt * c.m_velocity.LinAt(c.m_point);
			auto distance = Abs(Dot(c.m_point - point_at_t0, c.m_axis)); // from 'point_at_t0' to 'point_at_t1' in the direction of 'c.m_axis'
			c.m_time = distance > c.m_depth ? c.m_depth / distance : 0.0f;
			c.m_time = 1.0f - Clamp(c.m_time, 0.0f, 1.0f);
		
			return true;
		}

		// Calculate and apply forces to resolve the contact between the objects in 'pair'
		// Contact values in 'pair' are expected to be in objA space.
		void ResolveCollision(Contact const& c)
		{
			// Algorithm:
			//  - Extrapolate back (using the current dynamics) to the time of collision
			//  - Resolve the collision with impulses
			//  - Extrapolate to the original 't + dt'
			// Notes:
			//  An impulse is an instantaneous change in momentum.

			// Const cast is needed here because collision detection is a read-only process
			// but now when want to change the objects that were detected as in collision.
			auto& objA = const_cast<RigidBody&>(*c.m_objA);
			auto& objB = const_cast<RigidBody&>(*c.m_objB);

			#if PR_DBG
			auto vel_beforeA = objA.VelocityWS();
			auto vel_beforeB = objB.VelocityWS();
			auto ke_beforeA = objA.KineticEnergy();
			auto ke_beforeB = objB.KineticEnergy();
			auto h_before = objA.MomentumWS() + objB.MomentumWS();
			#endif

			// Calculate the world space restitution impulse (measured at the collision point)
			auto impulse_pair = RestitutionImpulseWS(c);

			// Apply the impulse to the objects
			objA.MomentumOS(objA.MomentumOS() + impulse_pair.m_os_impulse_objA);
			objB.MomentumOS(objB.MomentumOS() + impulse_pair.m_os_impulse_objB);

			// Collisions should not add energy to the system and momentum should be conserved.
			#if PR_DBG
			auto vel_afterA = objA.VelocityWS();
			auto vel_afterB = objA.VelocityWS();
			auto ke_afterA  = objA.KineticEnergy();
			auto ke_afterB  = objB.KineticEnergy();
			auto ke_diff    = (ke_afterA + ke_afterB) - (ke_beforeA + ke_beforeB);
			auto h_after    = objA.MomentumWS() + objB.MomentumWS();
			auto h_ang_diff = Length(h_after.ang) - Length(h_before.ang);
			auto h_lin_diff = Length(h_after.lin) - Length(h_before.lin);
		//	assert("Collision caused an increase in angular momentum" && h_ang_diff <= 0);
		//	assert("Collision caused an increase in linear momentum" && h_lin_diff <= 0);
		//	assert("Collision caused an increase in K.E." && ke_diff <= 0);
			#endif
		}

		// Dump the collision scene to LDraw script
		void Dump(Contact const& c)
		{
			using namespace pr::ldr;

			std::string str;
			ldr::RigidBody(str, "ObjA", 0x80FF0000, *c.m_objA, 0.1f, ERigidBodyFlags::None, &m4x4Identity);
			ldr::RigidBody(str, "ObjB", 0x8000FF00, *c.m_objB, 0.1f, ERigidBodyFlags::None, &c.m_b2a);
			
			ldr::Box(str, "Contact", 0xFFFFFF00, 0.005f, c.m_point);
			ldr::SpatialVector(str, "Velocity", 0xFFFFFF00, (v8)c.m_velocity * 0.1f, c.m_point, 0.f);//1, 0.25f);

			ldr::Write(str, L"P:\\dump\\collision.ldr");
		}
	};
}
