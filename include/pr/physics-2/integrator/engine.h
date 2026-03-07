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
#include "pr/physics-2/integrator/gpu_integrator.h"

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
		// Compile-time switch for GPU vs CPU integration path.
		// When true, uses the GPU compute shader for Störmer-Verlet integration.
		// When false, uses the CPU fallback path (EvolveCPU on the dynamics buffer).
		// Set to false to disable GPU integration entirely (no D3D12 dependency).
		static constexpr bool UseGpu = false; // Start with CPU; flip to true once GPU path is validated

		IBroadphase& m_broadphase;
		IMaterials& m_materials;

		// GPU integrator (pimpl). Only created when UseGpu is true.
		// The full definition is in gpu_integrator.cpp — no view3d-12 types leak here.
		GpuIntegratorPtr m_gpu_integrator;

		// Raised after collision detection, but before resolution.
		// Subscribers can inspect, modify, add, or remove contacts before impulses are applied.
		EventHandler<Engine&, std::vector<Contact>&> PostCollisionDetection;

		Engine(IBroadphase& bp, IMaterials& mats)
			: m_broadphase(bp)
			, m_materials(mats)
			, m_gpu_integrator()
			, PostCollisionDetection()
		{}

		// Initialise GPU integration. Call this once, passing a D3D12 device pointer.
		// Only needed when UseGpu is true. The device can come from a Renderer or standalone Gpu.
		void InitGpu(ID3D12Device4* device, int max_bodies)
		{
			m_gpu_integrator = CreateGpuIntegrator(device, max_bodies);
		}

		// Evolve the physics objects forward in time and resolve any collisions.
		// The simulation pipeline is: Evolve → Broad Phase → Narrow Phase → Resolve.
		// Step() is a template so it can iterate containers of RigidBody-derived types
		// (e.g. std::span<Body>) without slicing. The heavy collision work is in DetectAndResolve().
		template <typename TRigidBodyCont>
		void Step(float dt, TRigidBodyCont& bodies)
		{
			// Before here, callers should have set forces on the rigid bodies (including gravity).

			// Count dynamic bodies and pack their state into the flat dynamics buffer.
			// Static bodies (infinite mass) are skipped — they have no forces, no
			// velocity, and evolving them would only accumulate numerical drift.
			std::vector<RigidBodyDynamics> dynamics;
			std::vector<int> dynamic_indices;  // Map: dynamics[i] ↔ bodies[dynamic_indices[i]]
			{
				int idx = 0;
				for (auto& body : bodies)
				{
					if (body.Mass() < InfiniteMass * 0.5f)
					{
						dynamics.push_back(PackDynamics(body));
						dynamic_indices.push_back(idx);
					}
					++idx;
				}
			}

			// Integrate all dynamic bodies using either GPU or CPU path.
			// Both paths operate on the same RigidBodyDynamics buffer format,
			// allowing A/B comparison for debugging.
			if constexpr (UseGpu)
			{
				// GPU compute shader path
				IntegrateGpu(dynamics, dt);
			}
			else
			{
				// CPU fallback path — same algorithm as the GPU shader
				for (auto& dyn : dynamics)
					EvolveCPU(dyn, dt);
			}

			// Unpack the integrated state back into the rigid bodies
			{
				int i = 0;
				for (auto dyn_idx : dynamic_indices)
				{
					auto body_it = std::next(std::begin(bodies), dyn_idx);
					UnpackDynamics(*body_it, dynamics[i]);
					++i;
				}
			}

			// Broad phase → narrow phase → resolve (implemented in engine.cpp)
			DetectAndResolve(dt);
		}

	private:

		// GPU integration dispatch (implemented in engine.cpp where GpuIntegrator is complete).
		void IntegrateGpu(std::span<RigidBodyDynamics> dynamics, float dt);

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
