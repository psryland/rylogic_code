//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_include_handler.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "pr/view3d-12/utility/barrier_batch.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12::compute::particle_collision
{
	struct ParticleCollision
	{
		inline static constexpr iv3 PosCountDimension = { 1024, 1, 1 };

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr EUAVReg Particles = EUAVReg::u0;
			inline static constexpr EUAVReg Primitives = EUAVReg::u1;
		};

		struct Constants
		{
			int NumParticles;  // The number of particles
			int NumPrimitives; // The number of primitives
			float TimeStep;    // The time to advance each particle by
			v2 Restitution;    // The coefficient of restitution (normal, tangential)
		};
		static constexpr int NumConstants = sizeof(Constants) / sizeof(uint32_t);

		enum class EPrimType
		{
			Plane = 0,
			Sphere = 1,
			Triangle = 2,
		};

		struct Prim
		{
			// [0:3] = primitive type
			uint32_t flags;

			// Primitive data:
			//  Plane: data[0] = normal, data[1].x = distance (positive if the normal faces the origin)
			//  Sphere: data[0] = center, data[1].x = radius
			//  Triangle: data = {a, b, c}
			union {
				struct { v3 normal; float distance; } plane;
				struct { v3 centre; float radius; } sphere;
				struct { v3 point[3]; } triangle;
			} data;
		};

		Renderer* m_rdr;                     // The renderer instance to use to run the compute shader
		ComputeStep m_integrate;             // Integrate particles forward in time (with collision)
		D3DPtr<ID3D12Resource> m_primitives; // The primitives to collide with
		Constants m_constants;
		
		ParticleCollision(Renderer& rdr, std::wstring_view position_layout)
			: m_rdr(&rdr)
			, m_integrate()
			, m_primitives()
		{
			auto device = rdr.D3DDevice();
			auto compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"PARTICLE_COLLISION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.EntryPoint(L"Integrate")
				.Define(L"POS_TYPE", position_layout)
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Integrate
			{
				auto bytecode = compiler.Compile();
				m_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32(EReg::Constants, NumConstants)
					.Uav(EReg::Particles)
					.Uav(EReg::Primitives)
					.Create(device, "ParticleCollision:Integrate");
				m_integrate.m_pso = ComputePSO(m_integrate.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:Integrate");
			}

			// Set default collision primitives
			{
				Prim plane = {
					.flags = (uint32_t)EPrimType::Plane,
					.data = {
						.plane = {
							.normal = { 0, 1, 0 },
							.distance = 0,
						},
					},
				};
				SetCollisionPrimitives({ &plane, 1 });
			}
		}

		/// <summary>Set the primitives that the particles will collide with</summary>
		void SetCollisionPrimitives(std::span<Prim const> primitives)
		{
			ResDesc desc = ResDesc::Buf(ssize(primitives), sizeof(Prim), primitives.data(), alignof(Prim)).usage(EUsage::UnorderedAccess);
			m_primitives = m_rdr->res().CreateResource(desc, "ParticleCollision:Primitives");
			m_rdr->res().FlushToGpu(true);
		}

		// Integrate the particle positions (with collision)
		void Update(ComputeJob& job, int count, D3DPtr<ID3D12Resource> particles) const
		{
			{
				job.m_cmd_list.SetPipelineState(m_integrate.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_integrate.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_primitives->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ count, 1, 1 }, PosCountDimension));
			}
		}
	};
}
