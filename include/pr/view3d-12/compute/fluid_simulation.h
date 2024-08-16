//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/compute/gpu_job.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/compute/compute_step.h"
#include "pr/view3d-12/compute/spatial_partition.h"
#include "pr/view3d-12/compute/particle_collision.h"

namespace pr::rdr12::compute::fluid
{
	// Particle is designed to be compatible with Vert so that the
	// same buffer can be used for both particle and vertex data.
	struct Particle
	{
		v4 pos;
		v4 col;
		v4 vel;
		v3 acc;
		float mass;

		inline static constexpr wchar_t const* Layout =
			L"struct PosType "
			L"{ "
			L"	float4 pos; "
			L"	float4 col; "
			L"	float4 vel; "
			L"	float3 accel; "
			L"	float mass; "
			L"}";
	};
	static_assert(sizeof(Particle) == sizeof(Vert));
	static_assert(alignof(Particle) == alignof(Vert));
	static_assert(offsetof(Particle, pos) == offsetof(Vert, m_vert));
	static_assert(offsetof(Particle, col) == offsetof(Vert, m_diff));
	static_assert(offsetof(Particle, vel) == offsetof(Vert, m_norm));
	static_assert(offsetof(Particle, acc) == offsetof(Vert, m_tex0));

	template <int Dimensions, D3D12_COMMAND_LIST_TYPE QueueType = D3D12_COMMAND_LIST_TYPE_DIRECT>
	struct FluidSimulation
	{
		using GpuJob = GpuJob<QueueType>;
		using SpatialPartition = compute::spatial_partition::SpatialPartition;
		using ParticleCollision = compute::particle_collision::ParticleCollision;
		using CollisionPrim = compute::particle_collision::Prim;

		static constexpr int ThreadGroupSize = 1024;

		struct EReg
		{
			inline static constexpr ECBufReg Params = ECBufReg::b0;
			inline static constexpr ECBufReg Colours = ECBufReg::b1;
			inline static constexpr ECBufReg Probe = ECBufReg::b2;
			inline static constexpr ECBufReg Collision = ECBufReg::b3;
			inline static constexpr ECBufReg Map = ECBufReg::b3;
			inline static constexpr EUAVReg ParticlePositions = EUAVReg::u0;
			inline static constexpr EUAVReg Spatial = EUAVReg::u1;
			inline static constexpr EUAVReg IdxStart = EUAVReg::u2;
			inline static constexpr EUAVReg IdxCount = EUAVReg::u3;
			inline static constexpr EUAVReg CollisionPrimitives = EUAVReg::u4;
			inline static constexpr EUAVReg TexMap = EUAVReg::u5;
		};

		Renderer* m_rdr;                      // The renderer instance to use to run the compute shader
		ComputeStep m_cs_apply_forces;        // Calculate the forces acting on each particle position
		ComputeStep m_cs_apply_probe;         // Apply forces from the probe
		ComputeStep m_cs_colour;              // Apply colours to the particles
		ComputeStep m_cs_gen_map;             // Populate a texture with a map of a property
		ComputeStep m_cs_debugging;           // Debugging CS function
		D3DPtr<ID3D12Resource> m_r_particles; // The buffer of the particles (includes position/colour/norm(velocity))
		SpatialPartition m_spatial;           // Spatial partitioning of the particles
		ParticleCollision m_collision;        // The collision resolution for the fluid
		int m_frame;                          // Frame counter

		struct ParamsData
		{
			int NumParticles = 0;             // The number of particles
			int NumPrimitives = 0;            // The number of collision primitives
			float ParticleRadius = 0.1f;      // The radius of influence for each particle
			float TimeStep = 0.0f;            // Particle position prediction

			v4 Gravity = { 0, -9.8f, 0, 0 };  // The acceleration due to gravity

			float Mass = 1.0f;              // The particle mass
			float ForceScale = 10.0f;       // The conversion factor from density to pressure
			float Viscosity = 0.1f;         // The viscosity scaler
			float ThermalDiffusion = 0.01f; // The thermal diffusion rate

			float Attraction = 1.6f; // The attraction force factor. > 1 = more attraction
			float Falloff = 2.5;     // Controls the width of the centre peak.
			float GridScale = 10.0f; // The scale factor for the spatial partition grid
			int CellCount = 1021;    // The number of grid cells in the spatial partition
			int RandomSeed = 0;      // Seed value for the RNG
		} Params;
		struct ColoursData
		{
			// The colour scale to use
			Colour Colours[4] = {
				Colour(0xFF2D50AF),
				Colour(0xFFFF0000),
				Colour(0xFFFFFF00),
				Colour(0xFFFFFFFF),
			};
			v2 Range = { 0, 1 };

			// Colouring scheme
			uint32_t Scheme = 0; // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density
		} Colours;
		struct ProbeData
		{
			v4 Position = { 0,0,0,1 };
			Colour Colour = pr::Colour(0xFFFFFF00);
			float Radius = 0.1f;
			float Force = 0.0f;
			int Highlight = 0;
		} Probe;
		struct MapData
		{
			m4x4 MapToWorld = m4x4::Identity(); // Transform from map space to world space (including scale)
			iv2 MapTexDim = { 1,1 };            // The dimensions of the map texture
			int MapType = 0;                    // 0 = Pressure
		};

		FluidSimulation(Renderer& rdr, ParamsData const& params, std::span<Particle const> particle_init_data, std::span<CollisionPrim const> collision_init_data, EGpuFlush flush = EGpuFlush::Block)
			: m_rdr(&rdr)
			, m_cs_apply_forces()
			, m_cs_apply_probe()
			, m_cs_colour()
			, m_cs_gen_map()
			, m_cs_debugging()
			, m_r_particles()
			, m_spatial(rdr, params.CellCount, params.GridScale, Particle::Layout)
			, m_collision(rdr, Particle::Layout, collision_init_data, flush)
			, m_frame()
			, Params(params)
			, Colours()
			, Probe()
		{
			// Create the compute shaders
			CreateComputeSteps();

			// Create the particle buffer
			CreateParticleBuffer(particle_init_data, flush);
		}

		// Set the initial state of the simulation (spatial partition, colours, etc.)
		void Init(GpuJob& job)
		{
			// Make the particle buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Update the spatial partition
			m_spatial.Update(job, Params.NumParticles, m_r_particles, true);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);

			// Run the compute jobs
			job.Run();
		}

		// Advance the simulation forward in time by 'dt' seconds
		void Step(GpuJob& job, float dt)
		{
			++m_frame;
			Params.RandomSeed = m_frame;

			// Make the particle vertex buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Apply the forces to each particle
			ApplyForces(job, dt);

			// Set particle colours
			ColourParticles(job);

			// Integrate velocity and position (with collision)
			m_collision.RestingContact(job, dt, Params.NumParticles, m_r_particles);
			m_collision.Integrate(job, dt, Params.NumParticles, m_r_particles);

			// Update the spatial partitioning of the particles
			m_spatial.Update(job, Params.NumParticles, m_r_particles, false);

			// Test stuff
			//Debugging(job);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);

			// Run the compute jobs
			job.Run();
		}

		// Update the particle colours without stepping the simulation
		void UpdateColours(GpuJob& job)
		{
			// Make the particle vertex buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Set particle colours
			ColourParticles(job);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);

			// Run the compute jobs
			job.Run();
		}

		// Read the particle positions from the particle buffer
		void ReadParticles(GpuJob& job, std::span<Particle> particles)
		{
			if (particles.size() < Params.NumParticles)
				throw std::runtime_error("Insufficient space to read particles");

			BarrierBatch barriers(job.m_cmd_list);
			GpuReadbackBuffer::Allocation buf;

			barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();
			{
				buf = job.m_readback.Alloc(Params.NumParticles * sizeof(Particle), alignof(Particle));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_r_particles.get(), 0, buf.m_size);
			}
			barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			barriers.Commit();

			job.Run();

			memcpy(particles.data(), buf.ptr<Particle>(), sizeof(Particle) * Params.NumParticles);
		}

		// Create a map of some value over the map area
		void GenerateMap(GpuJob& job, Texture2DPtr tex_map, MapData const& map_data)
		{
			job.m_barriers.Transition(tex_map->m_res.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			job.m_barriers.UAV(tex_map->m_res.get());
			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.UAV(m_spatial.m_pos_index.get());
			job.m_barriers.UAV(m_spatial.m_idx_start.get());
			job.m_barriers.UAV(m_spatial.m_idx_count.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_gen_map.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_gen_map.m_sig.get());

			job.m_cmd_list.SetComputeRootConstantBufferView(0, job.m_upload.Add(Params, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true));
			job.m_cmd_list.SetComputeRootConstantBufferView(1, job.m_upload.Add(Colours, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true));
			job.m_cmd_list.SetComputeRootConstantBufferView(2, job.m_upload.Add(map_data, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true));

			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_pos_index->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(6, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootDescriptorTable(7, job.m_view_heap.Add(tex_map->m_uav));
			job.m_cmd_list.Dispatch(DispatchCount({ map_data.MapTexDim, 1 }, { 32, 32, 1 }));

			job.m_barriers.UAV(tex_map->m_res.get());
			job.m_barriers.Transition(tex_map->m_res.get(), D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE);
			job.m_barriers.Commit();

			job.Run();
		}

	private:

		// Create the buffer of particles
		void CreateParticleBuffer(std::span<Particle const> init_data, EGpuFlush flush)
		{
			auto desc = ResDesc::VBuf<Particle>(Params.NumParticles, init_data.data()).usage(EUsage::UnorderedAccess);
			m_r_particles = m_rdr->res().CreateResource(desc, "Fluid:ParticlePositions");
			m_rdr->res().FlushToGpu(flush); // Ensure resources are created and initialised
		}

		// Compile the compute shaders
		void CreateComputeSteps()
		{
			auto device = m_rdr->D3DDevice();
			auto compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"FLUID_SIMULATION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"POS_TYPE", Particle::Layout)
				.Define(L"SPATIAL_DIMENSIONS", std::to_wstring(Dimensions))
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Apply Forces
			{
				auto bytecode = compiler.EntryPoint(L"ApplyForces").Compile();
				m_cs_apply_forces.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<ParamsData>(EReg::Params)
					.Uav(EReg::ParticlePositions)
					.Uav(EReg::Spatial)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "Fluid:ApplyForcesSig");
				m_cs_apply_forces.m_pso = ComputePSO(m_cs_apply_forces.m_sig.get(), bytecode)
					.Create(device, "Fluid:ApplyForcesPSO");
			}

			// Apply Probe
			{
				auto bytecode = compiler.EntryPoint(L"ApplyProbe").Compile();
				m_cs_apply_probe.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<ParamsData>(EReg::Params)
					.U32<ProbeData>(EReg::Probe)
					.Uav(EReg::ParticlePositions)
					.Create(device, "Fluid:ApplyProbeSig");
				m_cs_apply_probe.m_pso = ComputePSO(m_cs_apply_probe.m_sig.get(), bytecode)
					.Create(device, "Fluid:ApplyProbePSO");
			}

			// Colour
			{
				auto bytecode = compiler.EntryPoint(L"ColourParticles").Compile();
				m_cs_colour.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<ParamsData>(EReg::Params)
					.U32<ColoursData>(EReg::Colours)
					.U32<ProbeData>(EReg::Probe)
					.Uav(EReg::ParticlePositions)
					.Uav(EReg::Spatial)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "Fluid:ColourParticlesSig");
				m_cs_colour.m_pso = ComputePSO(m_cs_colour.m_sig.get(), bytecode)
					.Create(device, "Fluid:ColourParticlesPSO");
			}

			// Generate Map
			{
				auto bytecode = compiler.EntryPoint(L"GenerateMap").Compile();
				m_cs_gen_map.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.CBuf(EReg::Params)
					.CBuf(EReg::Colours)
					.CBuf(EReg::Map)
					.Uav(EReg::ParticlePositions)
					.Uav(EReg::Spatial)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Uav(EReg::TexMap, 1)
					.Create(device, "Fluid:GenerateMapSig");
				m_cs_gen_map.m_pso = ComputePSO(m_cs_gen_map.m_sig.get(), bytecode)
					.Create(device, "Fluid:GenerateMapPSO");
			}

			// Debugging
			{
				auto bytecode = compiler.EntryPoint(L"Debugging").Compile();
				m_cs_debugging.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<ParamsData>(EReg::Params)
					.U32<ProbeData>(EReg::Probe)
					.Uav(EReg::ParticlePositions)
					.Uav(EReg::Spatial)
					.Uav(EReg::IdxStart)
					.Uav(EReg::IdxCount)
					.Create(device, "Fluid:DebuggingSig");
				m_cs_debugging.m_pso = ComputePSO(m_cs_debugging.m_sig.get(), bytecode)
					.Create(device, "Fluid:DebuggingPSO");
			}
		}

		// Apply forces to each particle
		void ApplyForces(GpuJob& job, float dt)
		{
			// Leap-frog half step
			Params.TimeStep = dt / 2;

			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.UAV(m_spatial.m_pos_index.get());
			job.m_barriers.UAV(m_spatial.m_idx_start.get());
			job.m_barriers.UAV(m_spatial.m_idx_count.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_apply_forces.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_apply_forces.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_pos_index->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ Params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			if (Probe.Force != 0)
			{
				job.m_barriers.UAV(m_r_particles.get());
				job.m_barriers.Commit();

				job.m_cmd_list.SetPipelineState(m_cs_apply_probe.m_pso.get());
				job.m_cmd_list.SetComputeRootSignature(m_cs_apply_probe.m_sig.get());
				job.m_cmd_list.SetComputeRoot32BitConstants(0, Params);
				job.m_cmd_list.SetComputeRoot32BitConstants(1, Probe);
				job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_r_particles->GetGPUVirtualAddress());
				job.m_cmd_list.Dispatch(DispatchCount({ Params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));
			}
		}

		// Apply colours to the particles
		void ColourParticles(GpuJob& job)
		{
			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.UAV(m_spatial.m_pos_index.get());
			job.m_barriers.UAV(m_spatial.m_idx_start.get());
			job.m_barriers.UAV(m_spatial.m_idx_count.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_colour.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_colour.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params);
			job.m_cmd_list.SetComputeRoot32BitConstants(1, Colours);
			job.m_cmd_list.SetComputeRoot32BitConstants(2, Probe);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_pos_index->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(6, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ Params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));
		}

		// Convert the particles buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(GpuJob& job, bool for_compute)
		{
			BarrierBatch barriers(job.m_cmd_list);
			barriers.Transition(m_r_particles.get(), for_compute
				? D3D12_RESOURCE_STATE_UNORDERED_ACCESS
				: D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
			barriers.Commit();
		}

		// Run the debugging function
		void Debugging(GpuJob& job) const
		{
			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.UAV(m_spatial.m_pos_index.get());
			job.m_barriers.UAV(m_spatial.m_idx_start.get());
			job.m_barriers.UAV(m_spatial.m_idx_count.get());
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_debugging.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_debugging.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params);
			job.m_cmd_list.SetComputeRoot32BitConstants(1, Probe);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_pos_index->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ 1, 1, 1 }, { 1, 1, 1 }));
		}
	};
}
