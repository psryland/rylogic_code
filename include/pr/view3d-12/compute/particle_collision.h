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
	enum class EPrimType
	{
		Plane = 0,
		Sphere = 1,
		Triangle = 2,
	};
	struct Prim
	{
		// Primitive data:
		union {
			v4 data[3];     // Data (as defined in the shader)
			v4 plane;       // xyz = normal, w = distance of origin above plane
			v4 sphere;      // xyz = center, w = radius
			v4 triangle[3]; // a, b, c
		} data;

		// flags.x = primitive type
		iv4 flags;
	};
	struct CollisionBuilder
	{
		std::vector<Prim> m_prims;
		ldr::Builder m_ldr;
		bool m_gen_ldr;

		CollisionBuilder(bool generate_ldraw_script = false)
			:m_prims()
			, m_ldr()
			, m_gen_ldr(generate_ldraw_script)
		{}
		CollisionBuilder(CollisionBuilder&&) = default;
		CollisionBuilder(CollisionBuilder const&) = delete;
		CollisionBuilder& operator=(CollisionBuilder&&) = default;
		CollisionBuilder& operator=(CollisionBuilder const&) = delete;
		CollisionBuilder& Plane(v4_cref plane, ldr::Name name = {}, ldr::Col colour = {}, v2 wh = { 1,1 }) // w is positive if the normal faces the origin
		{
			m_prims.push_back(Prim{
				.data = { .plane = plane },
				.flags = { s_cast<int>(EPrimType::Plane), 0, 0, 0 },
			});
			if (m_gen_ldr)
			{
				m_ldr.Plane(name, colour).plane(plane).wh(wh);
			}
			return *this;
		}
		CollisionBuilder& Sphere(v4_cref center, float radius, ldr::Name name = {}, ldr::Col colour = {})
		{
			m_prims.push_back(Prim{
				.data = { .sphere = v4(center.xyz, radius) },
				.flags = { s_cast<int>(EPrimType::Sphere), 0, 0, 0 },
			});
			if (m_gen_ldr)
			{
				m_ldr.Sphere(name, colour).r(radius).pos(center);
			}
			return *this;
		}
		CollisionBuilder& Triangle(v4_cref a, v4_cref b, v4_cref c, ldr::Name name = {}, ldr::Col colour = {})
		{
			m_prims.push_back(Prim{
				.data = { .triangle = { a, b, c } },
				.flags = { s_cast<int>(EPrimType::Triangle), 0, 0, 0 },
			});
			if (m_gen_ldr)
			{
				m_ldr.Triangle(name, colour).pt(a, b, c);
			}
			return *this;
		}
		std::span<Prim const> Primitives() const
		{
			return m_prims;
		}
		ldr::Builder& Ldr()
		{
			return m_ldr;
		}
	};

	struct ParticleCollision
	{
		inline static constexpr int ThreadGroupSize = 1024;

		struct EReg
		{
			inline static constexpr ECBufReg Constants = ECBufReg::b0;
			inline static constexpr EUAVReg Particles = EUAVReg::u0;
			inline static constexpr EUAVReg Primitives = EUAVReg::u1;
		};

		Renderer* m_rdr;                     // The renderer instance to use to run the compute shader
		ComputeStep m_integrate;             // Integrate particles forward in time (with collision)
		ComputeStep m_resting_contact;       // Apply resting contact forces (call before integrate)
		D3DPtr<ID3D12Resource> m_primitives; // The primitives to collide with

		// Shader parameters
		struct ParamsData
		{
			int NumParticles = 0;            // The number of particles
			int NumPrimitives = 0;           // The number of primitives
			v2 Restitution = { 1.0f, 1.0f }; // The coefficient of restitution (normal, tangential)
			float ParticleRadius = 0.1f;     // The radius of volume that each particle represents
			float BoundaryThickness = 0.01f; // The distance at which boundary effects apply
			float TimeStep = 0.0f;           // The time to advance each particle by
		} Params;
		
		ParticleCollision(Renderer& rdr, std::wstring_view position_layout, std::span<Prim const> init_data = {}, EGpuFlush flush = EGpuFlush::Block)
			: m_rdr(&rdr)
			, m_integrate()
			, m_resting_contact()
			, m_primitives()
			, Params()
		{
			auto device = rdr.D3DDevice();
			auto compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"PARTICLE_COLLISION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"POS_TYPE", position_layout)
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Integrate
			{
				auto bytecode = compiler.EntryPoint(L"Integrate").Compile();
				m_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<ParamsData>(EReg::Constants)
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
					.U32<ParamsData>(EReg::Constants)
					.Uav(EReg::Particles)
					.Uav(EReg::Primitives)
					.Create(device, "ParticleCollision:RestingContactSig");
				m_resting_contact.m_pso = ComputePSO(m_resting_contact.m_sig.get(), bytecode)
					.Create(device, "ParticleCollision:RestingContactPSO");
			}

			// Set default collision primitives
			SetCollisionPrimitives(init_data, flush);
		}

		/// <summary>Set the primitives that the particles will collide with</summary>
		void SetCollisionPrimitives(std::span<Prim const> primitives, EGpuFlush flush)
		{
			ResDesc desc = ResDesc::Buf(ssize(primitives), sizeof(Prim), primitives.data(), alignof(Prim)).usage(EUsage::UnorderedAccess);
			m_primitives = m_rdr->res().CreateResource(desc, "ParticleCollision:Primitives");
			m_rdr->res().FlushToGpu(flush);
			Params.NumPrimitives = isize(primitives);
		}

		// Integrate the particle positions (with collision)
		void Integrate(GraphicsJob& job, float dt, int count, D3DPtr<ID3D12Resource> particles)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "ParticleCollision::Integrate");

			Params.TimeStep = dt;
			Params.NumParticles = count;

			job.m_barriers.UAV(particles.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_integrate.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_integrate.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ count, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Apply resting contact forces
		void RestingContact(GraphicsJob& job, float dt, int count, D3DPtr<ID3D12Resource> particles)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "ParticleCollision::RestingContact");

			Params.TimeStep = dt;
			Params.NumParticles = count;

			job.m_barriers.UAV(particles.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_resting_contact.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_resting_contact.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_primitives->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ count, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			PIXEndEvent(job.m_cmd_list.get());
		}
	};
}
