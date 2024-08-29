//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/compute/particle_collision/collision_builder.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_include_handler.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "pr/view3d-12/utility/barrier_batch.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12::compute::particle_collision
{
	struct ParticleCollision
	{
		// TODO:
		//  - ReadPrimitives/WritePrimitives methods for updating collision
		//  - Support primitives with dynamics (i.e. moving, with mass, accumulate force from particles)
		//  - Spatially partition collision

		enum class ECullMode :int
		{
			None,
			Plane,
			Sphere,
			SphereInside,
			Box,
			BoxInside,
			MASK = 0x7,
		};

		struct CullData
		{
			v4 Geom[2] = { v4::Zero(), v4::Zero() }; // The culling volume
			ECullMode Mode = ECullMode::None;        // The culling mode
		};
		struct ConfigData
		{
			int NumPrimitives = 0;           // The number of primitives to sort
			int SpatialDimensions = 3;       // The number of spatial dimensions
			float BoundaryThickness = 0.01f; // The distance at which boundary effects apply
			float BoundaryForce = 1.f;       // The repulsive force of the boundary
			v2 Restitution = { 1.0f, 1.0f }; // The coefficient of restitution (normal, tangential)
			CullData Culling;
		};
		struct Setup
		{
			int PrimitiveCapacity;                        // The maximum number of primitives
			ConfigData Config;                            // Runtime configuration for the particle collision
			std::span<Prim const> CollisionInitData = {}; // Initialisation data for the collision
			
			bool valid() const noexcept
			{
				return
					Config.NumPrimitives >= 0 && Config.NumPrimitives <= PrimitiveCapacity &&
					(CollisionInitData.empty() || isize(CollisionInitData) == Config.NumPrimitives);
			}
		};

		Renderer* m_rdr;                       // The renderer instance to use to run the compute shader
		ComputeStep m_integrate;               // Integrate particles forward in time (with collision)
		ComputeStep m_resting_contact;         // Apply resting contact forces (call before integrate)
		D3DPtr<ID3D12Resource> m_r_primitives; // The primitives to collide with
		int m_capacity;                        // The maximum space in the buffers

		// Collision config
		ConfigData Config;
		
		ParticleCollision(Renderer& rdr, std::wstring_view position_layout)
			: m_rdr(&rdr)
			, m_integrate()
			, m_resting_contact()
			, m_r_primitives()
			, m_capacity()
			, Config()
		{
			CreateComputeSteps(position_layout);
		}

		// (Re)Initialise the particle collision system
		void Init(Setup const& setup, EGpuFlush flush = EGpuFlush::Block)
		{
			assert(setup.valid());

			// Save the config
			Config = setup.Config;

			// Create the primitives buffer
			{
				ResDesc desc = ResDesc::Buf<Prim>(setup.PrimitiveCapacity, setup.CollisionInitData)
					.def_state(D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE)
					.usage(EUsage::UnorderedAccess);

				m_r_primitives = m_rdr->res().CreateResource(desc, "ParticleCollision:Primitives");
				m_capacity = setup.PrimitiveCapacity;
			}

			m_rdr->res().FlushToGpu(flush);
		}

		// Integrate the particle positions (with collision)
		void Integrate(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			if (count == 0)
				return;

			PIXBeginEvent(job.m_cmd_list.get(), 0xFF209932, "ParticleCollision::Integrate");
			DoIntegrate(job, dt, count, radius, particles);
			PIXEndEvent(job.m_cmd_list.get());
		}

		// Apply resting contact forces
		void RestingContact(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			if (count == 0)
				return;

			PIXBeginEvent(job.m_cmd_list.get(), 0xFF209932, "ParticleCollision::RestingContact");
			DoRestingContact(job, dt, count, radius, particles);
			PIXEndEvent(job.m_cmd_list.get());
		}

		// Generate the default force profile
		static void ForceProfile(float slope, std::span<float> profile)
		{
			for (int i = 0; i != isize(profile); ++i)
			{
				float x = 1.0f * i / isize(profile);
				float a = -slope * x + 1;                // a(x) = -slope * x + 1
				float b = 2 * x * x * x - 3 * x * x + 1; // b(x) = 2x^3 - 3x^2 + 1
				profile[i] = a * b;
			}
		}

	private:

		inline static constexpr int ThreadGroupSize = 1024;
		using float2 = pr::v2;

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr ECBufReg Cull = ECBufReg::b1;
			inline static constexpr EUAVReg Particles = EUAVReg::u0;
			inline static constexpr ESRVReg Primitives = ESRVReg::t0;
		};

		struct cbCollision
		{
			int NumParticles;        // The number of particles
			int NumPrimitives;       // The number of primitives
			int SpatialDimensions;   // The number of spatial dimension
			float TimeStep;          // The time to advance each particle by

			float ParticleRadius;    // The radius of volume that each particle represents
			float BoundaryThickness; // The thickness in with boundary effects are applied
			float BoundaryForce;     // The repulsive force of the boundary
			float pad;

			float2 Restitution;      // The coefficient of restitution (normal, tangential)

		};
		struct cbCull
		{
			v4 Cull[2];              // A plane, sphere, etc used to cull particles (set their positions to nan)
			int Flags;               // [3:0] = 0: No culling, 1: Sphere, 2: Plane, 3: Box
		};

		// Create constant buffer data for the collision parameters
		cbCollision CollisionCBuf(float dt, int count, float radius)
		{
			assert(Config.NumPrimitives >= 0 && Config.NumPrimitives <= m_capacity);
			return cbCollision {
				.NumParticles = count,
				.NumPrimitives = Config.NumPrimitives,
				.SpatialDimensions = Config.SpatialDimensions,
				.TimeStep = dt,
				.ParticleRadius = radius,
				.BoundaryThickness = Config.BoundaryThickness,
				.BoundaryForce = Config.BoundaryForce,
				.pad = 0,
				.Restitution = Config.Restitution,
			};
		}
		cbCull CullCBuf()
		{
			return cbCull{
				.Cull = { Config.Culling.Geom[0], Config.Culling.Geom[1] },
				.Flags = static_cast<int>(Config.Culling.Mode) & (int)ECullMode::MASK,
			};
		}

		// Create the compute steps
		void CreateComputeSteps(std::wstring_view position_layout)
		{
			auto device = m_rdr->D3DDevice();
			ShaderCompiler compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"PARTICLE_COLLISION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"PARTICLE_TYPE", position_layout)
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Integrate
			{
				auto bytecode = compiler.EntryPoint(L"Integrate").Compile();
				m_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbCollision>(EReg::Constants)
					.U32<cbCull>(EReg::Cull)
					.UAV(EReg::Particles)
					.SRV(EReg::Primitives)
					.Create(device, "ParticleCollision:IntegrateSig");
				m_integrate.m_pso = ComputePSO(m_integrate.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:IntegratePSO");
			}

			// Resting contact
			{
				auto bytecode = compiler.EntryPoint(L"RestingContact").Compile();
				m_resting_contact.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbCollision>(EReg::Constants)
					.UAV(EReg::Particles)
					.SRV(EReg::Primitives)
					.Create(device, "ParticleCollision:RestingContactSig");
				m_resting_contact.m_pso = ComputePSO(m_resting_contact.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:RestingContactPSO");
			}
		}

		// Integrate the particle positions (with collision)
		void DoIntegrate(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			auto cb_params = CollisionCBuf(dt, count, radius);
			auto cb_cull = CullCBuf();

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_integrate.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_integrate.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
			job.m_cmd_list.SetComputeRoot32BitConstants(1, cb_cull, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootShaderResourceView(3, m_r_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(particles.get());
		}

		// Apply resting contact forces
		void DoRestingContact(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			auto cb_params = CollisionCBuf(dt, count, radius);

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_resting_contact.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_resting_contact.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootShaderResourceView(2, m_r_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(particles.get());
		}
	};
}
