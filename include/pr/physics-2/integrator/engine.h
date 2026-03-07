//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics-2/forward.h"
#include "pr/physics-2/broadphase/ibroadphase.h"
#include "pr/physics-2/material/imaterials.h"
#include "pr/physics-2/rigid_body/rigid_body.h"
#include "pr/physics-2/integrator/contact.h"
#include "pr/physics-2/integrator/integrator.h"
#include "pr/physics-2/integrator/impulse.h"

namespace pr::physics
{
	// ToDo:
	//  - Make use of sub step collision time
	//  - Use spatial vectors for impulse restitution
	//  - Optimise the impulse restitution function

	// A container object that groups the parts of a physics system together.
	// IBroadphase provides spatial overlap queries (e.g. brute-force, sweep-and-prune).
	// IMaterials maps material ID pairs to combined material properties (friction, elasticity).
	// The broadphase and material map are owned externally and passed by reference.
	struct Engine
	{
		IBroadphase& m_broadphase;
		IMaterials& m_materials;

		// Raised after collision detection, but before resolution.
		// Subscribers can inspect, modify, add, or remove contacts before impulses are applied.
		EventHandler<Engine&, std::vector<Contact>&> PostCollisionDetection;

		Engine(IBroadphase& bp, IMaterials& mats)
			: m_broadphase(bp)
			, m_materials(mats)
			, PostCollisionDetection()
		{}

		// Evolve the physics objects forward in time and resolve any collisions.
		// The simulation pipeline is: Evolve → Broad Phase → Narrow Phase → Resolve.
		// Step() is a template so it can iterate containers of RigidBody-derived types
		// (e.g. std::span<Body>) without slicing. The heavy collision work is in DetectAndResolve().
		template <typename TRigidBodyCont>
		void Step(float dt, TRigidBodyCont& bodies)
		{
			// Before here, callers should have set forces on the rigid bodies (including gravity).

			// Advance all bodies to 't + dt' using Störmer-Verlet symplectic integration.
			// After this, body positions reflect the new time but may overlap.
			// Static bodies (infinite mass) are skipped — they have no forces, no
			// velocity, and evolving them would only accumulate numerical drift.
			for (auto& body : bodies)
			{
				if (body.Mass() >= InfiniteMass * 0.5f) continue;
				Evolve(body, dt);
			}

			// Broad phase → narrow phase → resolve (implemented in engine.cpp)
			DetectAndResolve(dt);
		}

	private:

		// Broad phase overlap query → narrow phase collision detection → impulse resolution.
		// Implemented in engine.cpp to keep DirectX/GPU dependencies out of the header.
		void DetectAndResolve(float dt);

		// Narrow phase collision detection.
		// Tests whether the two bodies in 'c' are geometrically in contact using GJK/SAT.
		bool NarrowPhaseCollision(float dt, Contact& c);

		// Calculate and apply the restitution impulse to resolve a collision.
		void ResolveCollision(Contact& c);
	};
}
