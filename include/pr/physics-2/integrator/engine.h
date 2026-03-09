//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/physics-2/forward.h"
#include "pr/physics-2/shape/inertia.h"
#include "pr/physics-2/rigid_body/rigid_body.h"
#include "pr/physics-2/rigid_body/rigid_body_dynamics.h"
//#include "pr/physics-2/broadphase/ibroadphase.h"
//#include "pr/physics-2/material/imaterials.h"
//#include "pr/physics-2/integrator/contact.h"
//#include "pr/physics-2/integrator/impulse.h"
//#include "pr/physics-2/integrator/gpu_integrator.h"
//#include "pr/physics-2/collision/gpu_collision_detector.h"

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
	private:

		IBroadphase& m_broadphase;
		IMaterials& m_materials;

		// GPU device and command queue wrapper, shared by the integrator and collision detector.
		GpuPtr m_gpu;

		// GPU integrator (opaque)
		GpuIntegratorPtr m_gpu_integrator;

		// GPU collision detector (opaque)
		GpuCollisionDetectorPtr m_gpu_collision_detector;

		// Buffers for preparing GPU data
		using CachePtr = std::unique_ptr<struct EngineBufferCache, Deleter<EngineBufferCache>>;
		CachePtr m_cache;

		// Staging buffer for packing body dynamics
		std::vector<RigidBodyDynamics> m_rb_dynamics;
		std::vector<IntegrateOutput> m_integrate_output;

		// Runtime flag for GPU vs CPU integration. Set to true after calling InitGpu().
		// When true, integration is dispatched to the GPU compute shader.
		// When false, integration runs on the CPU via Evolve() (default).
		bool m_use_gpu;

	public:

		Engine(IBroadphase& bp, IMaterials& mats, ID3D12Device4* existing_device = nullptr);

		// Get/Set whether the GPU is used for integration and collision detection.
		bool UseGpu() const;
		void UseGpu(bool use_gpu);

		// Raised after collision detection, but before resolution.
		// Subscribers can inspect, modify, add, or remove contacts before impulses are applied.
		struct PostCollisionDetectionArgs { std::span<RbContact> m_contacts; };
		EventHandler<Engine&, PostCollisionDetectionArgs> PostCollisionDetection;

		// Evolve the physics objects forward in time and resolve any collisions.
		void Step(float dt, RigidBodyRange auto&& bodies)
		{
			// Before here, callers should have set forces on the rigid bodies (including gravity).
			// The simulation pipeline is: Evolve → Broad Phase → Narrow Phase → Resolve.
			// Step() is a template so it can iterate containers of RigidBody-derived types
			// (e.g. std::span<Body>) without slicing. The heavy collision work is in DetectAndResolve().
			m_rb_dynamics.resize(0);
			int i = 0;

			// GPU path: pack bodies into flat dynamics buffer → dispatch compute → unpack results.
			// CPU path: evolve each dynamic body directly using kick-drift-kick.
			for (auto& body : bodies)
			{
				if (body.Mass() >= 0.5f * InfiniteMass) continue;
				m_rb_dynamics.push_back(PackDynamics(body));
			}

			Integrate(m_rb_dynamics, dt);

			// Unpack the integrated state back into the rigid bodies
			for (auto& body : bodies)
			{
				if (body.Mass() >= 0.5f * InfiniteMass) continue;
				UnpackDynamics(body, m_rb_dynamics[i++]);
			}
			
			#if PR_DBG&&0
			CompareIntegrationPaths(dt, bodies);
			#endif

			// Detect and resolve collisions
			DetectAndResolveCollisions(dt);
		}

	private:

		// Integration dispatch
		void Integrate(std::span<RigidBodyDynamics> dynamics, float dt);

		// Broad phase overlap query → narrow phase collision detection → impulse resolution.
		void DetectAndResolveCollisions(float dt);

		// Narrow phase collision detection.
		// Tests whether the two bodies in 'c' are geometrically in contact using GJK/SAT.
		bool NarrowPhaseCollision(float dt, RbContact& c);

		// Calculate and apply the restitution impulse to resolve a collision.
		void ResolveCollision(RbContact& c);

		// Debug: stashed pre-integration state for A/B comparison between Evolve() and EvolveCPU().
		#if PR_DBG&&0
		std::vector<RigidBodyDynamics> m_compare_dynamics;
		std::vector<int> m_compare_indices;
		float m_compare_dt = 0;

		// A/B comparison: replay the last integration step through EvolveCPU and compare.
		// Uses the pre-integration state (stashed below) to run EvolveCPU independently.
		void CompareIntegrationPaths([[maybe_unused]] float dt, [[maybe_unused]] RigidBodyRange auto& bodies)
		{
			#if PR_DBG
			// Stash pre-integration state for comparison on the NEXT step.
			// On the first call, m_compare_dynamics is empty so we skip comparison.
			if (!m_compare_dynamics.empty())
			{
				// Run EvolveCPU on the stashed pre-integration dynamics
				for (auto& dyn : m_compare_dynamics)
					Evolve(dyn, m_compare_dt);

				// Compare EvolveCPU results with what Evolve() produced (now stored in bodies).
				// m_compare_indices maps dynamics[i] to bodies[idx].
				for (int i = 0; i != static_cast<int>(m_compare_dynamics.size()); ++i)
				{
					auto body_it = std::next(std::begin(bodies), m_compare_indices[i]);
					auto const& ref = *body_it; // Evolve() result
					auto const& gpu = m_compare_dynamics[i]; // EvolveCPU result

					// Compare transforms
					auto pos_err = Length(ref.O2W().pos - gpu.o2w.pos);
					auto rot_err = Length(ref.O2W().x - gpu.o2w.x)
					             + Length(ref.O2W().y - gpu.o2w.y)
					             + Length(ref.O2W().z - gpu.o2w.z);
					auto mom_ang_err = Length(ref.MomentumWS().ang - gpu.momentum_ang);
					auto mom_lin_err = Length(ref.MomentumWS().lin - gpu.momentum_lin);

					if (pos_err > 1e-4f || rot_err > 1e-4f || mom_ang_err > 1e-4f || mom_lin_err > 1e-4f)
					{
						auto f = fopen("dump\\evolve_compare.log", "a");
						if (f)
						{
							auto const& com = ref.InertiaInvOS().CoM();
							fprintf(f, "[MISMATCH] body=%d com=(%.4f,%.4f,%.4f) pos_err=%.6f rot_err=%.6f mom_ang_err=%.6f mom_lin_err=%.6f\n",
								m_compare_indices[i], com.x, com.y, com.z, pos_err, rot_err, mom_ang_err, mom_lin_err);
							fprintf(f, "  Evolve   pos=(%.6f,%.6f,%.6f) mom_ang=(%.6f,%.6f,%.6f) mom_lin=(%.6f,%.6f,%.6f)\n",
								ref.O2W().pos.x, ref.O2W().pos.y, ref.O2W().pos.z,
								ref.MomentumWS().ang.x, ref.MomentumWS().ang.y, ref.MomentumWS().ang.z,
								ref.MomentumWS().lin.x, ref.MomentumWS().lin.y, ref.MomentumWS().lin.z);
							fprintf(f, "  EvolveCPU pos=(%.6f,%.6f,%.6f) mom_ang=(%.6f,%.6f,%.6f) mom_lin=(%.6f,%.6f,%.6f)\n",
								gpu.o2w.pos.x, gpu.o2w.pos.y, gpu.o2w.pos.z,
								gpu.momentum_ang.x, gpu.momentum_ang.y, gpu.momentum_ang.z,
								gpu.momentum_lin.x, gpu.momentum_lin.y, gpu.momentum_lin.z);
							fclose(f);
						}
					}
				}
			}

			// Stash current pre-integration state for next step's comparison.
			// We pack dynamics NOW (before the next Evolve) so we have the same starting state.
			m_compare_dynamics.clear();
			m_compare_indices.clear();
			m_compare_dt = dt;
			{
				int idx = 0;
				for (auto& body : bodies)
				{
					if (body.Mass() < InfiniteMass * 0.5f)
					{
						m_compare_dynamics.push_back(PackDynamics(body));
						m_compare_indices.push_back(idx);
					}
					++idx;
				}
			}
			#endif
		}
		#endif
	};
}
