// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"
#include "src/probe.h"

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
		inline static constexpr ECBufReg Params = ECBufReg::b0;
		inline static constexpr ECBufReg Colours = ECBufReg::b1;
		inline static constexpr ECBufReg Probe = ECBufReg::b2;
		inline static constexpr EUAVReg ParticlePositions = EUAVReg::u0;
		inline static constexpr EUAVReg Spatial = EUAVReg::u1;
		inline static constexpr EUAVReg IdxStart = EUAVReg::u2;
		inline static constexpr EUAVReg IdxCount = EUAVReg::u3;
	};

	FluidSimulation::FluidSimulation(rdr12::Renderer& rdr, ParamsData const& params, std::span<Particle const> particle_init_data, std::span<CollisionPrim const> collision_init_data)
		: m_rdr(&rdr)
		, m_job(rdr.D3DDevice(), "Fluid", 0xFFA83250)
		, m_cs_densities()
		, m_cs_apply_forces()
		, m_cs_apply_probe()
		, m_cs_colour()
		, m_cs_debugging()
		, m_r_particles()
		, m_spatial(rdr, params.CellCount, params.GridScale, Particle::Layout)
		, m_collision(rdr, Particle::Layout, collision_init_data)
		, m_frame()
		, Params(params)
		, Colours()
		, Probe()
	{
		auto device = rdr.D3DDevice();
		auto compiler = rdr12::ShaderCompiler{}
			.Source(resource::Read<char>(L"FLUID_HLSL", L"TEXT"))
			.Includes({ new ResourceIncludeHandler, true })
			.Define(L"POS_TYPE", Particle::Layout)
			.Define(L"SPATIAL_DIMENSIONS", std::to_wstring(Dimensions))
			.ShaderModel(L"cs_6_6")
			.Optimise();

		// Densities
		{
			auto bytecode = compiler.EntryPoint(L"DensityAtParticles").Compile();
			m_cs_densities.m_sig = RootSig(ERootSigFlags::ComputeOnly)
				.U32<ParamsData>(EReg::Params)
				.Uav(EReg::ParticlePositions)
				.Uav(EReg::Spatial)
				.Uav(EReg::IdxStart)
				.Uav(EReg::IdxCount)
				.Create(device, "Fluid:DensityAtParticlesSig");
			m_cs_densities.m_pso = ComputePSO(m_cs_densities.m_sig.get(), bytecode)
				.Create(device, "Fluid:DensityAtParticlesPSO");
		}

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

		// Debugging
		{
			//compiler.DebugInfo().Optimise(false).PDBOutput(L"E:\\dump\\Symbols");
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

		// Create the particle buffer
		{
			auto desc = ResDesc::VBuf<Particle>(Params.NumParticles, particle_init_data.data()).usage(EUsage::UnorderedAccess);
			m_r_particles = rdr.res().CreateResource(desc, "Fluid:ParticlePositions");
			rdr.res().FlushToGpu(true); // Ensure resources are created and initialised
		}

		// Make the particle buffer accessible in the compute shader
		ParticleBufferAsUAV(m_job, true);

		// Update the spatial partition
		m_spatial.Update(m_job, Params.NumParticles, m_r_particles, true);

		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(m_job, false);

		// Run the compute jobs
		m_job.Run();
	}

	// Advance the simulation forward in time by 'dt' seconds
	void FluidSimulation::Step(float dt)
	{
		Tweakable<v2, "Restitution"> Restitution = v2{ 1.0f, 1.0f };
		Tweakable<float, "Gravity"> Gravity = 0.1f;
		Tweakable<float, "Viscosity"> Viscosity = 10.0f;
		Tweakable<float, "DensityToPressure"> DensityToPressure = 100.0f;
		Tweakable<float, "Density0"> Density0 = 1.0f;
		Tweakable<float, "Mass"> Mass = 1.0f;
		Tweakable<float, "ThermalDiffusion"> ThermalDiffusion = 0.01f;
		Params.Gravity = v4(0, -9.8f, 0, 0) * Gravity;
		Params.Mass = Mass;
		Params.DensityToPressure = DensityToPressure;
		Params.Density0 = Density0;
		Params.Viscosity = Viscosity;
		Params.ThermalDiffusion = ThermalDiffusion;
		m_collision.Params.Restitution = Restitution;

		// todo:
		//  - Leap frog integration
		//  - better viscosity
		//  -3D

		++m_frame;
		Params.RandomSeed = m_frame;

		// Make the particle vertex buffer accessible in the compute shader
		ParticleBufferAsUAV(m_job, true);

		// Calculate the density values at each particle location
		CalculateDensities(m_job);

		// Apply the forces to each particle
		ApplyForces(m_job);

		// Integrate velocity and position (with collision)
		m_collision.Update(m_job, dt, Params.NumParticles, m_r_particles);

		// Update the spatial partitioning of the particles
		m_spatial.Update(m_job, Params.NumParticles, m_r_particles, false);

		// Set particle colours
		ColourParticles(m_job);

		//Debugging(m_job);

		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(m_job, false);

		// Run the compute jobs
		m_job.Run();
	}

	// Update the particle colours without stepping the simulation
	void FluidSimulation::UpdateColours()
	{
		// Make the particle vertex buffer accessible in the compute shader
		ParticleBufferAsUAV(m_job, true);

		// Set particle colours
		ColourParticles(m_job);

		// Make the particle buffer a vertex buffer again
		ParticleBufferAsUAV(m_job, false);

		// Run the compute jobs
		m_job.Run();
	}

	// Read the particle positions from the vertex buffer
	void FluidSimulation::ReadParticles(std::span<Particle> particles)
	{
		if (particles.size() < Params.NumParticles)
			throw std::runtime_error("Insufficient space to read particles");

		BarrierBatch barriers(m_job.m_cmd_list);
		GpuReadbackBuffer::Allocation buf;

		barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
		barriers.Commit();
		{
			buf = m_job.m_readback.Alloc(Params.NumParticles * sizeof(Particle), alignof(Particle));
			m_job.m_cmd_list.CopyBufferRegion(buf.m_res, buf.m_ofs, m_r_particles.get(), 0, buf.m_size);
		}
		barriers.Transition(m_r_particles.get(), D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		barriers.Commit();

		m_job.Run();

		memcpy(particles.data(), buf.ptr<Particle>(), sizeof(Particle) * Params.NumParticles);
	}

	// Update the cache of density values at the particle locations
	void FluidSimulation::CalculateDensities(ComputeJob& job) const
	{
		job.m_barriers.UAV(m_r_particles.get());
		job.m_barriers.UAV(m_spatial.m_pos_index.get());
		job.m_barriers.UAV(m_spatial.m_idx_start.get());
		job.m_barriers.UAV(m_spatial.m_idx_count.get());
		job.m_barriers.Commit();

		job.m_cmd_list.SetPipelineState(m_cs_densities.m_pso.get());
		job.m_cmd_list.SetComputeRootSignature(m_cs_densities.m_sig.get());
		job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
		job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_pos_index->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
		job.m_cmd_list.Dispatch(DispatchCount({Params.NumParticles, 1, 1}, PosCountDimension));
	}

	// Apply the force due to pressure for each particle
	void FluidSimulation::ApplyForces(ComputeJob& job) const
	{
		job.m_barriers.UAV(m_r_particles.get());
		job.m_barriers.UAV(m_spatial.m_pos_index.get());
		job.m_barriers.UAV(m_spatial.m_idx_start.get());
		job.m_barriers.UAV(m_spatial.m_idx_count.get());
		job.m_barriers.Commit();

		job.m_cmd_list.SetPipelineState(m_cs_apply_forces.m_pso.get());
		job.m_cmd_list.SetComputeRootSignature(m_cs_apply_forces.m_sig.get());
		job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
		job.m_cmd_list.SetComputeRootUnorderedAccessView(1, m_r_particles->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_spatial.m_pos_index->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_idx_start->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_count->GetGPUVirtualAddress());
		job.m_cmd_list.Dispatch(DispatchCount({Params.NumParticles, 1, 1}, PosCountDimension));

		if (Probe.Force != 0)
		{
			job.m_barriers.UAV(m_r_particles.get());
			job.m_barriers.Commit();
		
			job.m_cmd_list.SetPipelineState(m_cs_apply_probe.m_pso.get());
			job.m_cmd_list.SetComputeRootSignature(m_cs_apply_probe.m_sig.get());
			job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
			job.m_cmd_list.SetComputeRoot32BitConstants(1, Probe, 0);
			job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_r_particles->GetGPUVirtualAddress());
			job.m_cmd_list.Dispatch(DispatchCount({Params.NumParticles, 1, 1}, PosCountDimension));
		}
	}

	// Apply colours to the particles
	void FluidSimulation::ColourParticles(ComputeJob& job) const
	{
		job.m_barriers.UAV(m_r_particles.get());
		job.m_barriers.UAV(m_spatial.m_pos_index.get());
		job.m_barriers.UAV(m_spatial.m_idx_start.get());
		job.m_barriers.UAV(m_spatial.m_idx_count.get());
		job.m_barriers.Commit();

		job.m_cmd_list.SetPipelineState(m_cs_colour.m_pso.get());
		job.m_cmd_list.SetComputeRootSignature(m_cs_colour.m_sig.get());
		job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
		job.m_cmd_list.SetComputeRoot32BitConstants(1, Colours, 0);
		job.m_cmd_list.SetComputeRoot32BitConstants(2, Probe, 0);
		job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_r_particles->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_pos_index->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_spatial.m_idx_start->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(6, m_spatial.m_idx_count->GetGPUVirtualAddress());
		job.m_cmd_list.Dispatch(DispatchCount({Params.NumParticles, 1, 1}, PosCountDimension));
	}

	// Convert the particles buffer to a compute resource or a vertex buffer
	void FluidSimulation::ParticleBufferAsUAV(ComputeJob& job, bool for_compute) const
	{
		BarrierBatch barriers(job.m_cmd_list);
		barriers.Transition(m_r_particles.get(), for_compute
			? D3D12_RESOURCE_STATE_UNORDERED_ACCESS
			: D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER);
		barriers.Commit();
	}
	
	// Run the debugging function
	void FluidSimulation::Debugging(ComputeJob& job) const
	{
		job.m_cmd_list.SetPipelineState(m_cs_debugging.m_pso.get());
		job.m_cmd_list.SetComputeRootSignature(m_cs_debugging.m_sig.get());
		job.m_cmd_list.SetComputeRoot32BitConstants(0, Params, 0);
		job.m_cmd_list.SetComputeRoot32BitConstants(1, Probe, 0);
		job.m_cmd_list.SetComputeRootUnorderedAccessView(2, m_r_particles->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(3, m_spatial.m_pos_index->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(4, m_spatial.m_idx_start->GetGPUVirtualAddress());
		job.m_cmd_list.SetComputeRootUnorderedAccessView(5, m_spatial.m_idx_count->GetGPUVirtualAddress());
		job.m_cmd_list.Dispatch(DispatchCount({1, 1, 1}, {1, 1, 1}));
	}
}
