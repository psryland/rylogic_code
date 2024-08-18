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
		Renderer* m_rdr;                     // The renderer instance to use to run the compute shader
		ComputeStep m_integrate;             // Integrate particles forward in time (with collision)
		ComputeStep m_resting_contact;       // Apply resting contact forces (call before integrate)
		D3DPtr<ID3D12Resource> m_primitives; // The primitives to collide with

		// Collision config
		struct ConfigData
		{
			int NumPrimitives = 0;           // The number of primitives
			float BoundaryThickness = 0.01f; // The distance at which boundary effects apply
			v2 Restitution = { 1.0f, 1.0f }; // The coefficient of restitution (normal, tangential)
		} Config;
		
		ParticleCollision(Renderer& rdr, std::wstring_view position_layout, int spatial_dimensions)
			: m_rdr(&rdr)
			, m_integrate()
			, m_resting_contact()
			, m_primitives()
			, Config()
		{
			CreateComputeSteps(position_layout, spatial_dimensions);
		}

		// (Re)Initialise the particle collision system
		void Init(ConfigData const& config, std::span<Prim const> init_data = {}, EGpuFlush flush = EGpuFlush::Block)
		{
			assert(init_data.empty() || isize(init_data) == config.NumPrimitives);
			Config = config;

			ResDesc desc = ResDesc::Buf(Config.NumPrimitives, sizeof(Prim), init_data.data(), alignof(Prim)).usage(EUsage::UnorderedAccess);
			m_primitives = m_rdr->res().CreateResource(desc, "ParticleCollision:Primitives");
			m_rdr->res().FlushToGpu(flush);
		}

		// Integrate the particle positions (with collision)
		void Integrate(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			DoIntegrate(job, dt, count, radius, particles);
		}

		// Apply resting contact forces
		void RestingContact(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			DoRestingContact(job, dt, count, radius, particles);
		}

	private:

		inline static constexpr int ThreadGroupSize = 1024;

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr EUAVReg Particles = EUAVReg::u0;
			inline static constexpr EUAVReg Primitives = EUAVReg::u1;
		};
		struct cbCollision
		{
			int NumParticles;        // The number of particles
			int NumPrimitives;       // The number of primitives
			v2 Restitution;          // The coefficient of restitution (normal, tangential)
			float ParticleRadius;    // The radius of volume that each particle represents
			float BoundaryThickness; // The thickness in with boundary effects are applied
			float TimeStep;          // The time to advance each particle by
		};

		// Create constant buffer data for the collision parameters
		cbCollision CollisionCBuf(float dt, int count, float radius)
		{
			return cbCollision {
				.NumParticles = count,
				.NumPrimitives = Config.NumPrimitives,
				.Restitution = Config.Restitution,
				.ParticleRadius = radius,
				.BoundaryThickness = Config.BoundaryThickness,
				.TimeStep = dt,
			};
		}

		// Create the compute steps
		void CreateComputeSteps(std::wstring_view position_layout, int spatial_dimensions)
		{
			auto device = m_rdr->D3DDevice();
			auto compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"PARTICLE_COLLISION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"POS_TYPE", position_layout)
				.Define(L"SPATIAL_DIMENSIONS", std::to_wstring(spatial_dimensions))
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Integrate
			{
				auto bytecode = compiler.EntryPoint(L"Integrate").Compile();
				m_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbCollision>(EReg::Constants)
					.Uav(EReg::Particles)
					.Uav(EReg::Primitives)
					.Create(device, "ParticleCollision:IntegrateSig");
				m_integrate.m_pso = ComputePSO(m_integrate.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:IntegratePSO");
			}

			// Resting contact
			{
				auto bytecode = compiler.EntryPoint(L"RestingContact").Compile();
				m_resting_contact.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbCollision>(EReg::Constants)
					.Uav(EReg::Particles)
					.Uav(EReg::Primitives)
					.Create(device, "ParticleCollision:RestingContactSig");
				m_resting_contact.m_pso = ComputePSO(m_resting_contact.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:RestingContactPSO");
			}
		}

		// Integrate the particle positions (with collision)
		void DoIntegrate(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "ParticleCollision::Integrate");

			auto cb_params = CollisionCBuf(dt, count, radius);

			job.m_barriers.UAV(particles.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_integrate.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_integrate.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			PIXEndEvent(job.m_cmd_list.get());
		}	

		// Apply resting contact forces
		void DoRestingContact(GraphicsJob& job, float dt, int count, float radius, D3DPtr<ID3D12Resource> particles)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "ParticleCollision::RestingContact");

			auto cb_params = CollisionCBuf(dt, count, radius);

			job.m_barriers.UAV(particles.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_resting_contact.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_resting_contact.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			PIXEndEvent(job.m_cmd_list.get());
		}
	};
}
