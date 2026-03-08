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
		static constexpr bool UseGpu = false;

		IBroadphase& m_broadphase;
		IMaterials& m_materials;

		// GPU integrator (pimpl). Only created when UseGpu is true.
		// The full definition is in gpu_integrator.cpp — no view3d-12 types leak here.
		GpuIntegratorPtr m_gpu_integrator;

		// Raised after collision detection, but before resolution.
		// Subscribers can inspect, modify, add, or remove contacts before impulses are applied.
		EventHandler<Engine&, std::vector<Contact>&> PostCollisionDetection;

		// Debug: stashed pre-integration state for A/B comparison between Evolve() and EvolveCPU().
		#if PR_DBG
		std::vector<RigidBodyDynamics> m_compare_dynamics;
		std::vector<int> m_compare_indices;
		float m_compare_dt = 0;
		#endif

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

			if constexpr (UseGpu)
			{
				// GPU path: pack bodies into flat dynamics buffer → dispatch compute → unpack results.
				// Static bodies (infinite mass) are skipped — they have no forces, no
				// velocity, and evolving them would only accumulate numerical drift.
				std::vector<RigidBodyDynamics> dynamics;
				std::vector<int> dynamic_indices;
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

				#ifdef PR_PHYSICS_GPU
				IntegrateGpu(dynamics, dt);
				#else
				// CPU fallback: process the dynamics buffer with EvolveCPU, which mirrors
				// the GPU compute shader algorithm. This validates the pack→evolve→unpack
				// pipeline without requiring a D3D12 device.
				for (auto& dyn : dynamics)
					EvolveCPU(dyn, dt);
				#endif

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
			}
			else
			{
				// CPU path: evolve each dynamic body directly using the original Evolve() function.
				// This is the reference implementation; the GPU/EvolveCPU path must match it.
				for (auto& body : bodies)
				{
					if (body.Mass() < InfiniteMass * 0.5f)
						Evolve(body, dt);
				}

				// Debug: A/B comparison between Evolve() (just applied) and EvolveCPU (buffer-based).
				// This validates that the pack→EvolveCPU→unpack pipeline produces identical results
				// to the reference path. Any delta here means the GPU shader will also be wrong.
				#if PR_DBG
				CompareIntegrationPaths(dt, bodies);
				#endif
			}

			// Broad phase → narrow phase → resolve (implemented in engine.cpp)
			DetectAndResolve(dt);
		}

	private:

		// A/B comparison: replay the last integration step through EvolveCPU and compare.
		// Uses the pre-integration state (stashed below) to run EvolveCPU independently.
		template <typename TRigidBodyCont>
		void CompareIntegrationPaths([[maybe_unused]] float dt, [[maybe_unused]] TRigidBodyCont& bodies)
		{
			#if PR_DBG
			// Stash pre-integration state for comparison on the NEXT step.
			// On the first call, m_compare_dynamics is empty so we skip comparison.
			if (!m_compare_dynamics.empty())
			{
				// Run EvolveCPU on the stashed pre-integration dynamics
				for (auto& dyn : m_compare_dynamics)
					EvolveCPU(dyn, m_compare_dt);

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
