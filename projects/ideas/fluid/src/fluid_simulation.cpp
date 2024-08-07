// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"

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
		inline static constexpr EUAVReg Spatial = EUAVReg::u1;
		inline static constexpr EUAVReg IdxStart = EUAVReg::u2;
		inline static constexpr EUAVReg IdxCount = EUAVReg::u3;
	};

	FluidSimulation::FluidSimulation(rdr12::Renderer& rdr, Constants const& constants, std::span<Vert const> init_data)
		: m_rdr(&rdr)
		, m_job(rdr.D3DDevice(), "Fluid", 0xFFA83250)
		, m_cs_densities()
		, m_cs_apply_forces()
		, m_r_particles()
		, m_spatial(rdr, constants.CellCount, constants.GridScale, ParticleLayout)
		, m_collision(rdr, ParticleLayout)
		, m_constants(constants)
	{
		auto include_handler = rdr12::ResourceIncludeHandler{};
		auto source = resource::Read<char>(L"FLUID_HLSL", L"TEXT");
		auto pos_type = std::format(L"-DPOS_TYPE={}", ParticleLayout);
		auto spatial_dimensions = std::format(L"-DSPATIAL_DIMENSIONS={}", Dimensions);
		wchar_t const* args[] = { L"-E<placeholder>", pos_type.c_str(), spatial_dimensions.c_str(), L"-Tcs_6_6", L"-O3", L"-Zi" };

		// Densities
		{
			args[0] = L"-EDensityAtParticles";
			auto bytecode = CompileShader(source, args, &include_handler);
			m_cs_densities.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, NumConstants)
				.Uav(EReg::ParticlePositions)
				.Uav(EReg::Spatial)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(rdr.D3DDevice(), "Fluid:DensityAtParticles");
			m_cs_densities.m_pso = ComputePSO(m_cs_densities.m_sig.get(), bytecode)
				.Create(rdr.D3DDevice(), "Fluid:DensityAtParticles");
		}
	
		// Apply Forces
		{
			args[0] = L"-EApplyForces";
			auto bytecode = CompileShader(source, args, &include_handler);
			m_cs_apply_forces.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32(EReg::Constants, NumConstants)
				.Uav(EReg::ParticlePositions)
				.Uav(EReg::Spatial)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(rdr.D3DDevice(), "Fluid:ApplyForces");
			m_cs_apply_forces.m_pso = ComputePSO(m_cs_apply_forces.m_sig.get(), bytecode)
				.Create(rdr.D3DDevice(), "Fluid:ApplyForces");
		}

		// Create the particle buffer
		{
			auto desc = ResDesc::VBuf<Vert>(m_constants.NumParticles, init_data.data()).usage(EUsage::UnorderedAccess);
			m_r_particles = rdr.res().CreateResource(desc, "Fluid:ParticlePositions");
			rdr.res().FlushToGpu(true); // Ensure resources are created and initialised
		}

		// Make the particle buffer accessible in the compute shader
		ParticleBufferAsUAV(true);

		// Update the spatial partition
		m_spatial.Update(m_job, m_constants.NumParticles, m_r_particles, false);

		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(false);

		// Run the compute jobs
		m_job.Run();
	}
	
	// Advance the simulation forward in time by 'dt' seconds
	void FluidSimulation::Step(float dt)
	{
		Tweakable<float, "Gravity"> Gravity = 0.1f;
		Tweakable<float, "Viscosity"> Viscosity = 10.0f;
		Tweakable<float, "DensityToPressure"> DensityToPressure = 100.0f;
		Tweakable<float, "Density0"> Density0 = 1.0f;
		Tweakable<float, "Mass"> Mass = 1.0f;
		m_constants.Gravity = v3(0, -9.8f, 0) * Gravity,
		m_constants.Mass = Mass,
		m_constants.DensityToPressure = DensityToPressure,
		m_constants.Density0 = Density0,
		m_constants.Viscosity = Viscosity,

		// Make the particle vertex buffer accessible in the compute shader
		ParticleBufferAsUAV(true);

		// Calculate the density values at each particle location
		CalculateDensities(m_job);

		// Apply the forces to each particle
		ApplyForces(m_job);

		// Integrate velocity and position (with collision)
		m_collision.Update(m_job, m_constants.NumParticles, m_r_particles);

		// Update the spatial partitioning of the particles
		m_spatial.Update(m_job, m_constants.NumParticles, m_r_particles, false);
		
		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(false);

		// Run the compute jobs
		m_job.Run();
	}

	// Apply the force due to pressure for each particle
	void FluidSimulation::ApplyForces(ComputeJob& job)
	{
		job.m_cmd_list.SetPipelineState(m_cs_apply_forces.m_pso.get());
		job.m_cmd_list.SetComputeRootSignature(m_cs_apply_forces.m_sig.get());
		job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
		job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_pos_index->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
		job.m_cmd_list.Dispatch(DispatchCount({m_constants.NumParticles, 1, 1}, PosCountDimension));
	}
	
	// Update the cache of density values at the particle locations
	void FluidSimulation::CalculateDensities(ComputeJob& job)
	{
		m_job.m_cmd_list.SetPipelineState(m_cs_densities.m_pso.get());
		m_job.m_cmd_list.SetComputeRootSignature(m_cs_densities.m_sig.get());
		m_job.m_cmd_list.SetComputeRoot32BitConstants(0, NumConstants, &m_constants, 0);
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_pos_index->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
		m_job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
		m_job.m_cmd_list.Dispatch(DispatchCount({m_constants.NumParticles, 1, 1}, PosCountDimension));
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
