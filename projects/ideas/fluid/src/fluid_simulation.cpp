// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"
#include "src/spatial_partition.h"
#include "src/iboundary_collision.h"
#include "src/iexternal_forces.h"

namespace pr::fluid
{
	// Smooth Particle Dynamics:
	//  The value of some property 'A' at 'x' is the weighted sum of the values of 'A' at each particle
	//  A(x) = Sum_i A_i * (mass_i / density_i) * W(x - x_i)
	// 
	// Use SI units.
	//  - Density of water is 1000kg/m^3 = 1g/cm^3
	//  - Pressure of water at sea level = 101 kN/m^2
	//  - Hydrostatic pressure vs. depth: P = rho * g * h
	//
	// A particle represents a small unit of fluid. Given a volume and a number of particles,
	// the mass of each fluid unit is: mass = density * volume / number of particles.

	static constexpr iv3 CellCountDimension(1024, 1, 1);
	static constexpr iv3 PosCountDimension(1024, 1, 1);

	struct EReg
	{
		inline static constexpr ECBufReg Constants = ECBufReg::b0;
		inline static constexpr EUAVReg ParticlePositions = EUAVReg::u0;
		inline static constexpr EUAVReg Primitives = EUAVReg::u1;
		inline static constexpr EUAVReg Spatial = EUAVReg::u1;
		inline static constexpr EUAVReg IdxStart = EUAVReg::u2;
		inline static constexpr EUAVReg IdxCount = EUAVReg::u3;
	};

	struct cbFluid
	{
		uint32_t NumParticles;   // The number of particles
		uint32_t CellCount;      // The number of grid cells in the spatial partition
		float GridScale;         // The scale factor for the spatial partition grid
		float Radius;            // The radius of influence for each particle
		v3 Gravity;              // The acceleration due to gravity
		float Mass;              // The particle mass
		float DensityToPressure; // The conversion factor from density to pressure
		float Density0;          // The baseline density
		float Viscosity;         // The viscosity scaler
		float dt;                // The time to advance each particle by
	};
	static constexpr int NumFluidConstants = sizeof(cbFluid) / sizeof(uint32_t);

	struct cbCollision
	{
		uint32_t NumParticles;  // The number of particles
		uint32_t NumPrimitives; // The number of primitives
		float TimeStep;         // The time to advance each particle by
		v2 Restitution;         // The coefficient of restitution (normal, tangential)
	};
	static constexpr int NumCollisionConstants = sizeof(cbCollision) / sizeof(uint32_t);

	FluidSimulation::FluidSimulation(rdr12::Renderer& rdr, int particle_count, float particle_radius, IBoundaryCollision& boundary, SpatialPartition& spatial, IExternalForces& external)
		: m_rdr(&rdr)
		, m_job(rdr.D3DDevice(), "Fluid", 0xFFA83250)
		, m_cs_densities()
		, m_cs_apply_forces()
		, m_cs_integrate()
		, m_r_particles()

		, m_gravity(0, -9.8f, 0, 0)
		, m_particles(particle_count)
		, m_densities(m_particles.size())
		, m_boundary(&boundary)
		, m_spatial(&spatial)
		, m_external(&external)
		, m_thermal_noise(0.001f)
		, m_radius(particle_radius)
		, m_density0(Dimensions == 3 ? 1000.0f : 10.0f) // kg/m^3 (3d), kg/m^2 (2d)
		, m_mass(m_density0 * m_boundary->Volume() / isize(m_particles)) // kg
		, m_count(particle_count)
	{
		// Initialise the D3D resources
		CreateBuffers(particle_count);

		// Create compute steps
		CreateComputeSteps();

		// Make the particle buffer accessible in the compute shader
		ParticleBufferAsUAV(true);

		// Update the spatial partition
		m_spatial->Update(m_job, m_count, m_r_particles, false);

		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(false);

		// Run the compute jobs
		m_job.Run();

#if 0
		// Distribute the particles
		auto i = 0;
		m_boundary->Fill(EFillStyle::Random, particle_count, m_radius, [&](auto& p) { m_particles[i++] = { p, v4::Zero() }; });

		// Update the spatial partitioning of the particles
		m_spatial->Update(m_particles);

		// Update the cache of density values at the particle locations
		CalculateDensities();
#endif
	}
	
	// The number of simulated particles
	int FluidSimulation::ParticleCount() const
	{
		return isize(m_particles);
	}

	// Advance the simulation forward in time by 'dt' seconds
	void FluidSimulation::Step(float dt)
	{
		// Make the particle buffer accessible in the compute shader
		ParticleBufferAsUAV(true);

		// Calculate the density values at each particle location
		CalculateDensities();

		// Add 'ApplyExternalForces'
		// Then integate and reset accel to 0

		// Apply the forces to each particle
		ApplyForces();

		// Integrate Velocities
		
		// Integrate Positions with collisions

#if 0

		// Evolve the particles forward in time
		std::default_random_engine rng;
		for (auto& particle : m_particles)
		{
			auto pidx = m_particles.index(particle);

			// Use Leapfrog integration to predict the next particle position
			auto pos1 = particle.m_pos + particle.m_vel * dt / 2;
			auto density = DensityAt(pidx);
			if (density < maths::tinyf)
				continue;

			// Sum up all sources of acceleration
			auto accel = v4::Zero();

			// Get the force experienced by the particle due to pressure
			auto pressure = PressureAt(pos1, pidx);
			accel += pressure / density;

			// Get the viscosity force experienced by the particle
			auto viscosity = ViscosityAt(pos1, pidx);
			accel += viscosity;

			// External forces
			auto external = m_external->ForceAt(*this, pos1, pidx);
			accel += external / density;
			
			// Gravity
			accel += Gravity * m_gravity;

			// Check for valid acceleration
			if constexpr (Dimensions == 2) accel.z = 0;
			assert(accel.w == 0);

			//static Tweakable<float, "Drag"> Drag = 0.99f;
			//particle.m_vel *= Drag;

			// Integrate the particle dynamics
			particle.m_vel += accel * dt;
			
			// Collision restitution with the boundary
			auto [pos, vel] = m_boundary->ResolveCollision(particle, m_radius, dt);

			// Apply new position and velocity
			particle.m_pos = pos;
			particle.m_vel = vel;
		}
#endif
		// Update the spatial partitioning of the particles
		m_spatial->Update(m_job, m_count, m_r_particles, false);
		
		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(false);

		// Run the compute jobs
		m_job.Run();
	}

	// Calculates the fluid density at 'position'
	float FluidSimulation::DensityAt(v4_cref position) const
	{
#if 0
		struct Ctx
		{
			FluidSimulation const* m_this;
			float density = 0.0f;

			static void Found(void* ctx, Particle const& particle, float dist_sq)
			{
				static_cast<Ctx*>(ctx)->DoFound(particle, dist_sq);
			}
			void DoFound(Particle const&, float dist_sq)
			{
				auto dist = Sqrt(dist_sq);
				auto influence = Particle::InfluenceAt(dist, m_this->m_radius);
				density += influence * m_this->m_mass;
			}
		} ctx = { this };

		// Find all particles within the particle radius of 'position'
		m_spatial->Find(position, m_radius, m_particles, {&Ctx::Found, &ctx});
		return ctx.density;
#endif
		return 0;
	}
	float FluidSimulation::DensityAt(size_t index) const
	{
		return m_densities[index];
	}

	// Calculate the pressure gradient at 'position'
	v4 FluidSimulation::PressureAt(v4_cref position, std::optional<size_t> index) const
	{
#if 0
		static Tweakable<float, "DensityToPressure"> DensityToPressure = 100.0f;
		static Tweakable<float, "Density0"> Density0 = 1.0f;

		struct Ctx
		{
			FluidSimulation const* m_this;
			v4 m_position;
			std::optional<size_t> m_index;
			v4 nett_pressure = v4::Zero();

			static void Found(void* ctx, Particle const& particle, float dist_sq)
			{
				return static_cast<Ctx*>(ctx)->DoFound(particle, dist_sq);
			}
			void DoFound(Particle const& particle, float dist_sq)
			{
				auto idx = m_this->m_particles.index(particle);
				if (m_index && *m_index == idx)
					return;

				// The distance from 'position' to 'particle'
				auto dist = Sqrt(dist_sq);

				// Get the influence due to 'particle' at 'dist'
				auto influence = Particle::dInfluenceAt(dist, m_this->m_radius);

				// Get the direction from 'particle' to 'position'
				auto direction = m_position - particle.m_pos;
				direction = dist > maths::tinyf ? (direction / dist) : v4::RandomN(g_rng(), 0);
				if constexpr (Dimensions == 2) { direction.z = 0; }

				// We need to simulate the force due to pressure being applied to both particles (idx and index).
				// A simple way to do this is to average the pressure between the two particles. Since pressure is
				// a linear function of density, we can use the average density.
				auto density = m_index
					? (m_this->DensityAt(idx) + m_this->DensityAt(*m_index)) / 2.0f
					: m_this->DensityAt(idx);

				// Convert the density to a pressure (P = k * (rho - rho0))
				//static float C = 7.0f;
				auto pressure = DensityToPressure * (density - Density0); //m_density0

				// Get the pressure gradient at 'position' due to 'particle'
				nett_pressure += (pressure * influence * m_this->m_mass / density) * direction;
			}
		} ctx = { this, position, index };

		m_spatial->Find(position, m_radius, m_particles, { &Ctx::Found, &ctx });
		return ctx.nett_pressure;
#endif
		return v4::Zero();
	}

	// Calculate the viscosity at 'position'
	v4 FluidSimulation::ViscosityAt(v4_cref position, std::optional<size_t> index) const
	{
#if 0
		static Tweakable<float, "Viscosity"> Viscosity = 10.0f;

		struct Ctx
		{
			FluidSimulation const* m_this;
			std::optional<size_t> m_index;
			v4 nett_viscosity = v4::Zero();

			static void Found(void* ctx, Particle const& particle, float dist_sq)
			{
				((Ctx*)ctx)->DoFound(particle, dist_sq);
			}
			void DoFound(Particle const& particle, float dist_sq)
			{
				auto idx = m_this->m_particles.index(particle);
				if (m_index && *m_index == idx)
					return;

				// The distance from 'position' to 'particle'
				auto dist = Sqrt(dist_sq);

				// Get the influence due to 'particle' at 'dist'
				auto influence = Particle::dInfluenceAt(dist, m_this->m_radius);

				// Calculate the viscosity from the relative velocity of the particles
				auto visocity = m_index
					? (m_this->m_particles[idx].m_vel - m_this->m_particles[*m_index].m_vel)
					: v4::Zero();

				// Viscosity
				nett_viscosity = Viscosity * influence * visocity;
			}
		} ctx = { this, index };

		m_spatial->Find(position, m_radius, m_particles, { &Ctx::Found, &ctx });
		return ctx.nett_viscosity;
#endif
		return v4::Zero();
	}

	// Get the compute shader constants
	cbFluid FluidSimulation::FluidConstants(float dt) const
	{
		Tweakable<float, "Gravity"> Gravity = 0.1f;
		Tweakable<float, "Viscosity"> Viscosity = 10.0f;
		Tweakable<float, "DensityToPressure"> DensityToPressure = 100.0f;
		Tweakable<float, "Density0"> Density0 = 1.0f;
		Tweakable<float, "Mass"> Mass = 1.0f;

		return cbFluid{
			.NumParticles = s_cast<uint32_t>(m_count),
			.CellCount = s_cast<uint32_t>(m_spatial->CellCount()),
			.GridScale = m_spatial->GridScale(),
			.Radius = m_radius,
			.Gravity = v3(0, -9.8f, 0) * Gravity,
			.Mass = m_mass,
			.DensityToPressure = DensityToPressure,
			.Density0 = Density0,
			.Viscosity = Viscosity,
			.dt = dt,
		};
	}

	// Get the compute shader constants for "collision" compute shaders
	cbCollision FluidSimulation::CollisionConstants(float dt) const
	{
		return cbCollision{
			.NumParticles = s_cast<uint32_t>(m_count),
			.NumPrimitives = 0,
			.TimeStep = dt,
			.Restitution = { 0.95f, 1.0f },
		};
	}

	// Create the D3D Resources
	void FluidSimulation::CreateBuffers(int particle_count)
	{
		// Particles
		{
			// Initialisation data
			std::vector<Vert> particles;
			particles.reserve(particle_count);
			m_boundary->Fill(EFillStyle::Lattice, particle_count, m_radius, [&](auto& p) {
				particles.push_back(Vert{ .m_vert = p, .m_diff = ColourWhite, .m_norm = {}, .m_tex0 = {}, .pad = {} });
			});

			// Renderer buffer
			auto desc = ResDesc::VBuf<Vert>(particle_count, particles.data()).usage(EUsage::UnorderedAccess);
			m_r_particles = m_rdr->res().CreateResource(desc, "Fluid:ParticlePositions");
		}

		// Ensure resources are created and initialised
		m_rdr->res().FlushToGpu(true);
	}

	// Create the compute steps for the fluid simulation
	void FluidSimulation::CreateComputeSteps()
	{
		auto include_handler = rdr12::ResourceIncludeHandler{};
		auto source = resource::Read<char>(L"FLUID_HLSL", L"TEXT");
		auto collision_source = resource::Read<char>(L"PARTICLE_COLLISION_HLSL", L"TEXT");
		auto spatial_dimensions = std::format(L"-DSPATIAL_DIMENSIONS={}", Dimensions);
		wchar_t const* args[] = { L"-E<placeholder>", spatial_dimensions.c_str(), L"-Tcs_6_6", L"-O3", L"-Zi" };

		// Densities
		{
			args[0] = L"-EDensityAtParticles";
			auto bytecode = CompileShader(source, args, &include_handler);
			m_cs_densities.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, NumFluidConstants)
				.Uav(EReg::ParticlePositions)
				.Uav(EReg::Spatial)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(m_rdr->D3DDevice(), "Fluid:DensityAtParticles");
			m_cs_densities.m_pso = ComputePSO(m_cs_densities.m_sig.get(), bytecode)
				.Create(m_rdr->D3DDevice(), "Fluid:DensityAtParticles");
		}
	
		// Apply Forces
		{
			args[0] = L"-EApplyForces";
			auto bytecode = CompileShader(source, args, &include_handler);
			m_cs_apply_forces.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, NumFluidConstants)
				.Uav(EReg::ParticlePositions)
				.Uav(EReg::Spatial)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(m_rdr->D3DDevice(), "Fluid:ApplyForces");
			m_cs_apply_forces.m_pso = ComputePSO(m_cs_apply_forces.m_sig.get(), bytecode)
				.Create(m_rdr->D3DDevice(), "Fluid:ApplyForces");
		}

		// Integrate
		//{
		//	args[0] = L"-EIntegrate";
		//	auto bytecode = CompileShader(source, args, &include_handler);
		//	m_cs_integrate.m_sig = RootSig(ERootSigFlags::ComputeOnly)
		//		.U32(EReg::Constants, NumFluidConstants)
		//		.Uav(EReg::ParticlePositions)
		//		.Uav(EReg::Primitives)
		//		.Create(m_rdr->D3DDevice(), "Fluid:Integrate");
		//	m_cs_integrate.m_pso = ComputePSO(m_cs_integrate.m_sig.get(), bytecode)
		//		.Create(m_rdr->D3DDevice(), "Fluid:Integrate");
		//}
	}

	// Advance the particles in time
	void FluidSimulation::Integrate()
	{
		auto constants = FluidConstants();
		m_job.m_cmd_list.SetPipelineState(m_cs_integrate.m_pso.get());
		m_job.m_cmd_list.SetComputeRootSignature(m_cs_integrate.m_sig.get());
		m_job.m_cmd_list.SetComputeRoot32BitConstants(0, NumFluidConstants, &constants, 0);
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_r_particles->GetGPUVirtualAddress());
		m_job.m_cmd_list.Dispatch(DispatchCount({s_cast<int>(m_count), 1, 1}, PosCountDimension));
	}

	// Apply the force due to pressure for each particle
	void FluidSimulation::ApplyForces()
	{
		auto constants = FluidConstants();
		m_job.m_cmd_list.SetPipelineState(m_cs_apply_forces.m_pso.get());
		m_job.m_cmd_list.SetComputeRootSignature(m_cs_apply_forces.m_sig.get());
		m_job.m_cmd_list.SetComputeRoot32BitConstants(0, NumFluidConstants, &constants, 0);
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial->m_pos_index->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial->m_idx_start->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial->m_idx_count->GetGPUVirtualAddress());
		m_job.m_cmd_list.Dispatch(DispatchCount({s_cast<int>(m_count), 1, 1}, PosCountDimension));
	}
	
	// Update the cache of density values at the particle locations
	void FluidSimulation::CalculateDensities()
	{
		auto constants = FluidConstants();
		m_job.m_cmd_list.SetPipelineState(m_cs_densities.m_pso.get());
		m_job.m_cmd_list.SetComputeRootSignature(m_cs_densities.m_sig.get());
		m_job.m_cmd_list.SetComputeRoot32BitConstants(0, NumFluidConstants, &constants, 0);
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial->m_pos_index->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial->m_idx_start->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial->m_idx_count->GetGPUVirtualAddress());
		m_job.m_cmd_list.Dispatch(DispatchCount({s_cast<int>(m_count), 1, 1}, PosCountDimension));

#if 0
		for (auto& particle : m_particles)
		{
			auto density = DensityAt(particle.m_pos);
			m_densities[m_particles.index(particle)] = density;
		}
#endif
	}

	// Convert the particles buffer to a compute resource or a vertex buffer
	void FluidSimulation::ParticleBufferAsUAV(bool for_compute)
	{
		BarrierBatch barriers(m_job.m_cmd_list);
		barriers.Transition(m_r_particles.get(), for_compute
			? D3D12_RESOURCE_STATE_UNORDERED_ACCESS
			: D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		barriers.Commit();
	}
}
