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
#include "pr/view3d-12/compute/spatial_partition/spatial_partition.h"
#include "pr/view3d-12/compute/particle_collision/particle_collision.h"

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
			L"struct Particle "
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

	template <D3D12_COMMAND_LIST_TYPE QueueType = D3D12_COMMAND_LIST_TYPE_DIRECT>
	struct FluidSimulation
	{
		using GpuJob = GpuJob<QueueType>;
		using SpatialPartition = compute::spatial_partition::SpatialPartition;
		using ParticleCollision = compute::particle_collision::ParticleCollision;
		using CollisionPrim = compute::particle_collision::Prim;

		// Runtime data
		struct ColourData
		{
			Colour Spectrum[4] = {    // The colour spectrum to use
				Colour(0xFF2D50AF),
				Colour(0xFFFF0000),
				Colour(0xFFFFFF00),
				Colour(0xFFFFFFFF),
			};
			v2 Range = { 0, 1 }; // The range to interpolate the spectrum over
			int Scheme = 1;      // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density, 0x80000000 = Within Probe
			int Pad = 0;
		};
		struct ProbeData
		{
			v4 Position = { 0,0,0,1 };
			Colour Colour = pr::Colour(0xFFFFFF00);
			float Radius = 0.1f;
			float Force = 0.0f;
			int Highlight = 0;
		};
		struct MapData
		{
			m4x4 MapToWorld = m4x4::Identity(); // Transform from map space to world space (including scale)
			iv2 TexDim = { 1,1 };               // The dimensions of the map texture
			int Type = 0;                       // 0 = Pressure
		};
		struct ConfigData
		{
			struct
			{
				float Radius = 0.1f; // The radius of influence for each particle
				float Mass   = 1.0f; // The particle mass
				iv2 Pad      = {};
			} Particles;
			struct
			{
				v4 Gravity = { 0, -9.8f, 0, 0 }; // The acceleration due to gravity
				float ForceScale = 10.0f;        // The force scaling factor
				float Viscosity = 0.05f;         // The viscosity scaler
				float ThermalDiffusion = 0.01f;  // The thermal diffusion rate
				int Pad = 0;
			} Dyn;                          // The dynamics variables
			ColourData Colours;             // The colour data
			v4 CullPlane = { 0, 1, 0, +1 }; // The plane below which particles are culled
			int NumParticles = 0;           // The number of particles
			int ColouringScheme = 0;        // Colouring scheme - 0 = None, 1 = Velocity, 2 = Accel, 3 = Density
		};
		struct StepOutput
		{
			GpuReadbackBuffer::Allocation m_particles;
			GpuReadbackBuffer::Allocation m_cull_results;
			int m_particle_buffer_size;

			// The number of particles in the particle buffer (returned from Cull)
			int ParticleCount() const
			{
				auto ptr = m_cull_results.ptr<int>();
				return ptr ? ptr[0] : 0;
			}

			// Populate 'particles' with the particle data from the last step
			void ReadParticles(std::span<Particle> particles, int start) const
			{
				if (start < 0 || start + isize(particles) > m_particle_buffer_size)
					throw std::range_error("Invalid particle buffer range");
				if (isize(particles) != 0 && m_particles.ptr<Particle>() == nullptr)
					throw std::runtime_error("You must use 'readback' to access particle data in sys-memory");

				memcpy(particles.data(), m_particles.ptr<Particle>() + start, particles.size() * sizeof(Particle));
			}
		};

		// Initialisation data
		struct Setup
		{
			int ParticleCapacity;                            // The maximum number of particles to expect (to set buffer sizes)
			ConfigData Config;                               // Runtime configuration for the fluid simulation
			std::span<Particle const> ParticleInitData = {}; // Initialisation data for the particles
			std::span<float const> ForceProfileData = {};    // Normalised force vs. distance function for particles

			bool valid() const noexcept
			{
				return
					Config.NumParticles >= 0 && Config.NumParticles <= ParticleCapacity &&
					(ParticleInitData.empty() || isize(ParticleInitData) == Config.NumParticles);
			}
		};

		Renderer* m_rdr;                          // The renderer instance to use to run the compute shader
		ComputeStep m_cs_apply_forces;            // Calculate the forces acting on each particle position
		ComputeStep m_cs_apply_probe;             // Apply forces from the probe
		ComputeStep m_cs_cull_particles;          // Remove particles below a plane
		ComputeStep m_cs_colour;                  // Apply colours to the particles
		ComputeStep m_cs_gen_map;                 // Populate a texture with a map of a property
		D3DPtr<ID3D12Resource> m_r_particles;     // The buffer of the particles (includes position/colour/norm(velocity))
		D3DPtr<ID3D12Resource> m_r_force_profile; // The normalised force vs. distance function for particles
		D3DPtr<ID3D12Resource> m_r_output;        // The buffer that receives the output of the compute shader
		ParticleCollision m_collision;            // The collision resolution for the fluid
		SpatialPartition m_spatial;               // Spatial partitioning of the particles
		int m_force_profile_length;               // The length of the force profile buffer
		int m_capacity;                           // The maximum number of particles
		int m_frame;                              // Frame counter

		// Runtime configurable data
		ConfigData Config; // The configuration data for the fluid simulation
		StepOutput Output; // Read back data from the last executed step

		explicit FluidSimulation(Renderer& rdr)
			: m_rdr(&rdr)
			, m_cs_apply_forces()
			, m_cs_apply_probe()
			, m_cs_colour()
			, m_cs_gen_map()
			, m_r_particles()
			, m_r_force_profile()
			, m_r_output()
			, m_collision(rdr, Particle::Layout)
			, m_spatial(rdr, Particle::Layout)
			, m_force_profile_length()
			, m_capacity()
			, m_frame()
			, Config()
			, Output()
		{
			CreateComputeSteps(Particle::Layout);
		}

		// Set the initial state of the simulation (spatial partition, colours, etc.)
		void Init(GpuJob& job, Setup const& fs_setup, ParticleCollision::Setup const& pc_setup, SpatialPartition::Setup const& sp_setup, EGpuFlush flush = EGpuFlush::Block)
		{
			assert(fs_setup.valid());

			// Save the config
			Config = fs_setup.Config;

			// Create resource buffers
			CreateResourceBuffers(fs_setup);

			// Reset the collision primitives
			m_collision.Init(pc_setup, EGpuFlush::DontFlush);

			// Reset the spatial partitioning
			m_spatial.Init(sp_setup, EGpuFlush::DontFlush);

			// Ensure resources are created and initialised
			m_rdr->res().FlushToGpu(flush);

			// Make the particle buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Update the spatial partition
			m_spatial.Update(job, Config.NumParticles, m_r_particles, true);

			// Cull any particles that are initially out of bounds
			CullParticles(job);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);

			// Run the compute jobs
			job.Run();

			Config.NumParticles = Output.ParticleCount();
		}

		// Convert the particle buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(GpuJob& job, bool for_compute)
		{
			auto state = for_compute
				? D3D12_RESOURCE_STATE_UNORDERED_ACCESS
				: D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER;

			job.m_barriers.Transition(m_r_particles.get(), state);
		}

		// Advance the simulation forward in time by 'elapsed_s' seconds
		void Step(GpuJob& job, float elapsed_s, bool read_back)
		{
			++m_frame;
			Output = {};

			// No-op if there are no particles
			if (Config.NumParticles == 0)
				return;

			// Save the size of the particle buffer used in this step
			Output.m_particle_buffer_size = Config.NumParticles;

			// Make the particle vertex buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Apply forces to each particle
			ApplyForces(job, elapsed_s);

			// Integrate velocity and position (with collision)
			m_collision.RestingContact(job, elapsed_s, Config.NumParticles, Config.Particles.Radius, m_force_profile_length, m_r_particles, m_r_force_profile);
			m_collision.Integrate(job, elapsed_s, Config.NumParticles, Config.Particles.Radius, m_r_particles);

			// Update the spatial partitioning of the particles
			m_spatial.Update(job, Config.NumParticles, m_r_particles, false);

			// Cull any particles that are initially out of bounds
			CullParticles(job);

			// Read back the particle buffer
			if (read_back)
				Output.m_particles = ReadParticles(job, 0, Output.m_particle_buffer_size);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);
		}

		// Apply forces from a probe
		void ApplyProbeForces(GpuJob& job, ProbeData const& probe)
		{
			if (Config.NumParticles == 0)
				return;

			// Make the particle vertex buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Apply the probe forces
			ApplyProbe(job, probe);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);
		}

		// Update the particle colours without stepping the simulation
		void UpdateColours(GpuJob& job, ColourData const& colours)
		{
			if (Config.NumParticles == 0)
				return;

			// Make the particle vertex buffer accessible in the compute shader
			ParticleBufferAsUAV(job, true);

			// Set particle colours
			ColourParticles(job, colours);

			// Make the particle buffer a vertex buffer again
			ParticleBufferAsUAV(job, false);
		}

		// Read the particle positions from the particle buffer
		GpuReadbackBuffer::Allocation ReadParticles(GpuJob& job, int start, int count)
		{
			if (start + count < Config.NumParticles)
				throw std::range_error("Invalid particle buffer range");
			if (count == 0)
				return {};

			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "FluidSim::ReadParticles");
			GpuReadbackBuffer::Allocation buf;

			BarrierBatch barriers(job.m_cmd_list);
			auto previous_state = job.m_cmd_list.ResState(m_r_particles.get());
			barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();
			{
				// Allocate read back buffer space and read from the particle buffer
				buf = job.m_readback.Alloc(count * sizeof(Particle), alignof(Particle));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_r_particles.get(), start * sizeof(Particle), buf.m_size);

			}
			barriers.Transition(m_r_particles.get(), previous_state.Mip0State());
			barriers.Commit();

			PIXEndEvent(job.m_cmd_list.get());
			return buf;
		}

		// Write particles into the particle buffer
		void WriteParticles(GpuJob& job, int start, std::span<Particle const> particles)
		{
			if (particles.empty())
				return;

			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4988F2, "FluidSim::WriteParticles");

			BarrierBatch barriers(job.m_cmd_list);
			GpuUploadBuffer::Allocation buf;

			auto previous_state = job.m_cmd_list.ResState(m_r_particles.get());
			barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_COPY_DEST);
			barriers.Commit();
			{
				// Allocate upload buffer space and copy from 'particles' into the upload buffer, then into the particle buffer
				buf = job.m_upload.Alloc(isize(particles) * sizeof(Particle), alignof(Particle));
				memcpy(buf.ptr<Particle>(), particles.data(), particles.size() * sizeof(Particle));
				job.m_cmd_list.CopyBufferRegion(m_r_particles.get(), start * sizeof(Particle), buf.m_res, buf.m_ofs, buf.m_size);
			}
			barriers.Transition(m_r_particles.get(), previous_state.Mip0State());
			barriers.Commit();

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Create a map of some value over the map area
		void GenerateMap(GpuJob& job, Texture2DPtr tex_map, MapData const& map_data, ColourData const& colour_data)
		{
			DoGenerateMap(job, tex_map, map_data, colour_data);
		}

		enum class EForceProfileType
		{
			Type0,
			Type1,
			Type2,
			Type3,
		};

		// Generate the default force profile
		static void DefaultForceProfile(EForceProfileType type, std::initializer_list<float const> params, std::span<float> profile)
		{
			auto const* p = params.begin();
			for (int i = 0; i != isize(profile); ++i)
			{
				auto x = 1.0f * i / isize(profile);
				switch (type)
				{
					case EForceProfileType::Type0:
					{
						float a = -p[0] * x + 1;                 // a(x) = -m * x + 1
						float b = 2 * x * x * x - 3 * x * x + 1; // b(x) = 2x^3 - 3x^2 + 1
						profile[i] = a * b;
						break;
					}
					case EForceProfileType::Type1:
					{
						float a = -std::powf(x, p[0]) + 1;
						profile[i] = a;
						break;
					}
					case EForceProfileType::Type2:
					{
						float a = -x + 1;
						profile[i] = a;
						break;
					}
					case EForceProfileType::Type3:
					{
						float a = -0.28f + 1.0f / Sqr(x + 0.88f);
						profile[i] = a;
						break;
					}
				}
			}
		}

	private:

		static constexpr int ThreadGroupSize = 1024;
		using float4x4 = pr::m4x4;
		using float4 = pr::v4;
		using float2 = pr::v2;
		using int2 = pr::iv2;

		struct EReg
		{
			inline static constexpr ECBufReg Fluid = ECBufReg::b0;
			inline static constexpr ECBufReg Probe = ECBufReg::b0;
			inline static constexpr ECBufReg Cull = ECBufReg::b0;
			inline static constexpr ECBufReg Colours = ECBufReg::b0;
			inline static constexpr ECBufReg Map = ECBufReg::b0;

			inline static constexpr EUAVReg Particles = EUAVReg::u0;
			inline static constexpr EUAVReg Spatial = EUAVReg::u1;
			inline static constexpr EUAVReg IdxStart = EUAVReg::u2;
			inline static constexpr EUAVReg IdxCount = EUAVReg::u3;
			inline static constexpr ESRVReg ForceProfile = ESRVReg::t3;
			inline static constexpr EUAVReg Output = EUAVReg::u5;
			inline static constexpr EUAVReg TexMap = EUAVReg::u6;
		};

		struct cbFluidSim
		{
			int Dimensions;         // 2D or 3D simulation
			int NumParticles;       // The number of particles
			int CellCount;          // The number of grid cells in the spatial partition
			float GridScale;        // The scale factor for the spatial partition grid

			int RandomSeed;         // Seed value for the RNG
			float ParticleRadius;   // The radius of influence for each particle
			float TimeStep;         // Leap-frog time step
			float Mass;             // The particle mass

			float4 Gravity;         // The acceleration due to gravity

			float ForceScale;       // The force scaling factor
			float Viscosity;        // The viscosity scaler
			float ThermalDiffusion; // The thermal diffusion rate
			int ForceProfileLength; // The length of the force profile buffer
		};
		struct cbProbeData
		{
			float4 Position;      // The position of the probe
			float Radius;         // The radius of the probe
			float Force;          // The force that the probe applies
			int NumParticles;     // The number of particles in the 'm_particles' buffer
		};
		struct cbCullData
		{
			int NumParticles;       // The number of particles
			int CellCount;          // The number of grid cells in the spatial partition
		};
		struct cbColourData
		{
			float4 Spectrum[4];     // The colour scale to use
			float2 Range;           // Scales [0,1] to the colour range
			int Dimensions;         // 2D or 3D simulation
			int NumParticles;       // The number of particles in the 'm_particles' buffer
		
			int Scheme;             // 0 = None, 1 = Velocity, 2 = Accel, 3 = Density
			int CellCount;          // The number of grid cells in the spatial partition
			float GridScale;        // The scale factor for the spatial partition grid
			float ParticleRadius;   // The particle radius
		};
		struct cbMapData
		{
			float4x4 MapToWorld;    // Transform from map space to world space (including scale)
			float4 Spectrum[4];     // The colour scale to use
			float2 Range;           // Scales [0,1] to the colour range
			int2 TexDim;            // The dimensions of the map texture
			int Type;               // 0 = Pressure
			int Dimensions;         // 2D or 3D simulation
			int CellCount;          // The number of grid cells in the spatial partition
			float GridScale;        // The scale factor for the spatial partition grid
			float ParticleRadius;   // The particle radius
			float ParticleMass;     // The particle mass
			float ForceScale;       // The force scaling factor
			int ForceProfileLength; // The length of the force profile buffer
		};

		// Create constant buffer data for the fluid simulation
		cbFluidSim FluidSimCBuf(float time_step) const
		{
			return cbFluidSim{
				.Dimensions = m_collision.Config.SpatialDimensions,
				.NumParticles = Config.NumParticles,
				.CellCount = m_spatial.Config.CellCount,
				.GridScale = m_spatial.Config.GridScale,
				.RandomSeed = m_frame,
				.ParticleRadius = Config.Particles.Radius,
				.TimeStep = time_step,
				.Mass = Config.Particles.Mass,
				.Gravity = Config.Dyn.Gravity,
				.ForceScale = Config.Dyn.ForceScale,
				.Viscosity = Config.Dyn.Viscosity,
				.ThermalDiffusion = Config.Dyn.ThermalDiffusion,
				.ForceProfileLength = m_force_profile_length,
			};
		};
		cbProbeData ProbeCBuf(ProbeData const& probe) const
		{
			return cbProbeData{
				.Position = probe.Position,
				.Radius = probe.Radius,
				.Force = probe.Force,
				.NumParticles = Config.NumParticles,
			};
		}
		cbCullData CullCBuf() const
		{
			return cbCullData{
				.NumParticles = Config.NumParticles,
				.CellCount = m_spatial.Config.CellCount,
			};
		}
		cbColourData ColoursCBuf(ColourData const& colours) const
		{
			return cbColourData{
				.Spectrum = {
					To<v4>(colours.Spectrum[0]),
					To<v4>(colours.Spectrum[1]),
					To<v4>(colours.Spectrum[2]),
					To<v4>(colours.Spectrum[3]),
				},
				.Range = colours.Range,
				.Dimensions = m_collision.Config.SpatialDimensions,
				.NumParticles = Config.NumParticles,
				.Scheme = colours.Scheme,
				.CellCount = m_spatial.Config.CellCount,
				.GridScale = m_spatial.Config.GridScale,
				.ParticleRadius = Config.Particles.Radius,
			};
		}
		cbMapData MapCBuf(MapData const& map_data, ColourData const& colours) const
		{
			return cbMapData{
				.MapToWorld = map_data.MapToWorld,
				.Spectrum = { 
					To<v4>(colours.Spectrum[0]),
					To<v4>(colours.Spectrum[1]),
					To<v4>(colours.Spectrum[2]),
					To<v4>(colours.Spectrum[3]),
				},
				.Range = colours.Range,
				.TexDim = map_data.TexDim,
				.Type = map_data.Type,
				.Dimensions = m_collision.Config.SpatialDimensions,
				.CellCount = m_spatial.Config.CellCount,
				.GridScale = m_spatial.Config.GridScale,
				.ParticleRadius = Config.Particles.Radius,
				.ParticleMass = Config.Particles.Mass,
				.ForceScale = Config.Dyn.ForceScale,
				.ForceProfileLength = m_force_profile_length,
			};
		}

		// Compile the compute shaders
		void CreateComputeSteps(std::wstring_view particle_layout)
		{
			auto device = m_rdr->D3DDevice();
			ShaderCompiler compiler = ShaderCompiler{}
				.Source(resource::Read<char>(L"FLUID_SIMULATION_HLSL", L"TEXT"))
				.Includes({ new ResourceIncludeHandler, true })
				.Define(L"PARTICLE_TYPE", particle_layout)
				.ShaderModel(L"cs_6_6")
				.Optimise();

			// Apply Forces
			{
				auto bytecode = compiler.EntryPoint(L"ApplyForces").Compile();
				m_cs_apply_forces.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbFluidSim>(EReg::Fluid)
					.UAV(EReg::Particles)
					.UAV(EReg::Spatial)
					.UAV(EReg::IdxStart)
					.UAV(EReg::IdxCount)
					.SRV(EReg::ForceProfile)
					.Create(device, "Fluid:ApplyForcesSig");
				m_cs_apply_forces.m_pso = ComputePSO(m_cs_apply_forces.m_sig.get(), bytecode)
					.Create(device, "Fluid:ApplyForcesPSO");
			}

			// Apply Probe
			{
				auto bytecode = compiler.EntryPoint(L"ApplyProbe").Compile();
				m_cs_apply_probe.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbProbeData>(EReg::Probe)
					.UAV(EReg::Particles)
					.Create(device, "Fluid:ApplyProbeSig");
				m_cs_apply_probe.m_pso = ComputePSO(m_cs_apply_probe.m_sig.get(), bytecode)
					.Create(device, "Fluid:ApplyProbePSO");
			}

			// Cull particles
			{
				auto bytecode = compiler.EntryPoint(L"CullParticles").Compile();
				m_cs_cull_particles.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbCullData>(EReg::Cull)
					.UAV(EReg::Particles)
					.UAV(EReg::Spatial)
					.UAV(EReg::IdxStart)
					.UAV(EReg::IdxCount)
					.UAV(EReg::Output)
					.Create(device, "Fluid:CullParticlesSig");
				m_cs_cull_particles.m_pso = ComputePSO(m_cs_cull_particles.m_sig.get(), bytecode)
					.Create(device, "Fluid:CullParticlesPSO");
			}

			// Colour
			{
				auto bytecode = compiler.EntryPoint(L"ColourParticles").Compile();
				m_cs_colour.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.U32<cbColourData>(EReg::Colours)
					.UAV(EReg::Particles)
					.UAV(EReg::Spatial)
					.UAV(EReg::IdxStart)
					.UAV(EReg::IdxCount)
					.Create(device, "Fluid:ColourParticlesSig");
				m_cs_colour.m_pso = ComputePSO(m_cs_colour.m_sig.get(), bytecode)
					.Create(device, "Fluid:ColourParticlesPSO");
			}

			// Generate Map
			{
				auto bytecode = compiler.EntryPoint(L"GenerateMap").Compile();
				m_cs_gen_map.m_sig = RootSig(ERootSigFlags::ComputeOnly)
					.CBuf(EReg::Map)
					.UAV(EReg::Particles)
					.UAV(EReg::Spatial)
					.UAV(EReg::IdxStart)
					.UAV(EReg::IdxCount)
					.SRV(EReg::ForceProfile)
					.UAV(EReg::TexMap, 1)
					.Create(device, "Fluid:GenerateMapSig");
				m_cs_gen_map.m_pso = ComputePSO(m_cs_gen_map.m_sig.get(), bytecode)
					.Create(device, "Fluid:GenerateMapPSO");
			}
		}

		// Create the resource buffers
		void CreateResourceBuffers(Setup const& setup)
		{
			// Create the particle (vertex) buffer
			{
				ResDesc desc = ResDesc::VBuf<Particle>(setup.ParticleCapacity, setup.ParticleInitData)
					.usage(EUsage::UnorderedAccess);
				m_r_particles = m_rdr->res().CreateResource(desc, "Fluid:ParticlePositions");
				m_capacity = setup.ParticleCapacity;
			}

			// Create the force profile
			{
				// Create a default force profile if one isn't provided
				std::vector<float> force_profile_data;
				if (setup.ForceProfileData.empty())
				{
					force_profile_data.resize(100);
					DefaultForceProfile(FluidSimulation::EForceProfileType::Type0, { 1.4f }, force_profile_data);
				}

				auto profile = !setup.ForceProfileData.empty() ? setup.ForceProfileData : force_profile_data;
				ResDesc desc = ResDesc::Buf<float>(isize(profile), profile)
					.def_state(D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);

				m_r_force_profile = m_rdr->res().CreateResource(desc, "Fluid:ForceProfile");
				m_force_profile_length = isize(profile);
			}

			// Create the output buffer
			{
				ResDesc desc = ResDesc::Buf<int>(1, {})
					.def_state(D3D12_RESOURCE_STATE_UNORDERED_ACCESS)
					.usage(EUsage::UnorderedAccess);
				m_r_output = m_rdr->res().CreateResource(desc, "Fluid:Output");
			}
		}

		// Apply forces to each particle
		void ApplyForces(GpuJob& job, float time_step)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF3F75FF, "FluidSim::ApplyForces");

			auto cb_params = FluidSimCBuf(time_step);

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_apply_forces.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_apply_forces.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_spatial->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootShaderResourceView(5, m_r_force_profile->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(m_r_particles.get());

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Apply probe forces to the particles
		void ApplyProbe(GpuJob& job, ProbeData const& probe)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF4CFF4F, "FluidSim::ApplyProbe");

			auto cb_probe = ProbeCBuf(probe);

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_apply_probe.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_apply_probe.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_probe);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_probe.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(m_r_particles.get());

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Cull particles that fall out of the world
		void CullParticles(GpuJob& job)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFF993020, "FluidSim::CullParticles");

			auto cb_params = CullCBuf();

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_cull_particles.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_cull_particles.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_params);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_spatial->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_r_output->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_params.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.UAV(m_r_output.get());

			// Read back the number of particles
			job.m_barriers.Transition(m_r_output.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			job.m_barriers.Commit();
			{
				auto buf = job.m_readback.Alloc(sizeof(int), alignof(int));
				job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_r_output.get(), 0, sizeof(int));
				Output.m_cull_results = buf;
			}
			job.m_barriers.Transition(m_r_output.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Apply colours to the particles
		void ColourParticles(GpuJob& job, ColourData const& colours)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFFFB9BFF, "FluidSim::ColourParticles");

			auto cb_colours = ColoursCBuf(colours);

			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_colour.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_colour.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, cb_colours);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_spatial->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({ cb_colours.NumParticles, 1, 1 }, { ThreadGroupSize, 1, 1 }));

			job.m_barriers.UAV(m_r_particles.get());

			PIXEndEvent(job.m_cmd_list.get());
		}

		// Create a map of some value over the map area
		void DoGenerateMap(GpuJob& job, Texture2DPtr tex_map, MapData const& map_data, ColourData const& colour_data)
		{
			PIXBeginEvent(job.m_cmd_list.get(), 0xFFF0FF56, "FluidSim::ColourParticles");

			auto cb_map = MapCBuf(map_data, colour_data);

			job.m_barriers.Transition(tex_map->m_res.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			job.m_barriers.Commit();

			job.m_cmd_list.SetPipelineState(m_cs_gen_map.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_gen_map.m_sig.get());
			job.m_cmd_list.SetComputeRootConstantBufferView(0, job.m_upload.Add(cb_map, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, true));
			job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_spatial->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootShaderResourceView(5, m_r_force_profile->GetGPUVirtualAddress());
			job.m_cmd_list.SetComputeRootDescriptorTable(6, job.m_view_heap.Add(tex_map->m_uav));
			job.m_cmd_list.Dispatch(DispatchCount({ cb_map.TexDim, 1 }, { 32, 32, 1 }));

			job.m_barriers.UAV(tex_map->m_res.get());
			job.m_barriers.Transition(tex_map->m_res.get(), D3D12_RESOURCE_STATE_ALL_SHADER_RESOURCE);
			job.m_barriers.Commit();

			job.Run();

			PIXEndEvent(job.m_cmd_list.get());
		}
	};
}
