//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once
#include "pr/physics-2/forward.h"
#include "pr/physics-2/shape/inertia.h"
#include "pr/physics-2/rigid_body/rigid_body.h"
#include "pr/physics-2/rigid_body/rigid_body_dynamics.h"

namespace pr::physics
{
	// ToDo:
	//  - Make use of sub step collision time

	// A container object that groups the parts of a physics system together.
	// IBroadphase provides spatial overlap queries (e.g. brute-force, sweep-and-prune).
	// IMaterials maps material ID pairs to combined material properties (friction, elasticity).
	// The broadphase and material map are owned externally and passed by reference.
	struct Engine
	{
	private:

		// GPU device and command queue wrapper, shared by the integrator and collision detector.
		GpuPtr m_gpu;

		// GPU integrator (opaque)
		GpuIntegratorPtr m_gpu_integrator;

		// GPU broadphase (opaque)
		GpuSortAndSweepPtr m_gpu_sort_and_sweep;

		// GPU collision detector (opaque)
		GpuCollisionDetectorPtr m_gpu_collision_detector;

		// Material map for looking up combined material properties during collision resolution.
		IMaterials& m_materials;

		// Buffers for preparing GPU data
		CachePtr m_cache;

		// Staging buffer for packing body dynamics
		std::vector<RigidBodyDynamics> m_rb_dynamics;
		std::vector<IntegrateOutput> m_integrate_output;
		std::vector<IntegrateAABB> m_integrate_aabbs;

		// Recycled buffer of rigid body pointers
		std::vector<RigidBody*> m_cache_body_ptrs;

	public:

		Engine(IMaterials& mats, ID3D12Device4* existing_device = nullptr);

		// Get/Set whether the GPU is used for integration and collision detection.
		bool UseGpu() const;
		void UseGpu(bool use_gpu);

		// Access the broadphase for registering bodies and enumerating overlapping pairs.
		IBroadphase& Broadphase() const;

		// Raised after collision detection, but before resolution.
		// Subscribers can inspect, modify, add, or remove contacts before impulses are applied.
		struct PostCollisionDetectionArgs { std::span<RbContact> m_contacts; };
		EventHandler<Engine&, PostCollisionDetectionArgs> PostCollisionDetection;

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

		// Deferred readback of bodies from the GPU after the collision pipeline.
		void ReadbackBodies();

	public:

		// Evolve the physics objects forward in time and resolve any collisions.
		void Step(float dt, std::span<RigidBody*> bodies);
		void Step(float dt, RigidBodyRange auto&& bodies)
		{
			m_cache_body_ptrs.resize(0);
			for (auto& body : bodies)
				m_cache_body_ptrs.push_back(&body);

			Step(dt, m_cache_body_ptrs);
		}

	private:

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
