// Fluid
#pragma once
#include "src/forward.h"
#include "src/particle.h"
#include "src/probe.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		struct Constants
		{
			int NumParticles;        // The number of particles
			int CellCount;           // The number of grid cells in the spatial partition
			float GridScale;         // The scale factor for the spatial partition grid
			float Radius;            // The radius of influence for each particle
			v3 Gravity;              // The acceleration due to gravity
			float Mass;              // The particle mass
			float DensityToPressure; // The conversion factor from density to pressure
			float Density0;          // The baseline density
			float Viscosity;         // The viscosity scaler
		};
		inline static constexpr int NumConstants = sizeof(Constants) / sizeof(uint32_t);

		inline static constexpr wchar_t const* ParticleLayout =
			L"struct PosType "
			L"{ "
			L"	float4 pos; "
			L"	float4 col; "
			L"	float4 vel; "
			L"	float3 accel; "
			L"	float density; "
			L"}";

		rdr12::Renderer* m_rdr;               // The renderer instance to use to run the compute shader
		rdr12::ComputeJob m_job;              // Manages running the compute shader steps
		rdr12::ComputeStep m_cs_densities;    // Calculate the density at each particle position
		rdr12::ComputeStep m_cs_apply_forces; // Calculate the forces acting on each particle position
		D3DPtr<ID3D12Resource> m_r_particles; // The vertex buffer of the particles (includes position/colour/norm(velocity))
		SpatialPartition m_spatial;          // Spatial partitioning of the particles
		ParticleCollision m_collision;       // The collision resolution for the fluid
		Constants m_constants;

		FluidSimulation(rdr12::Renderer& rdr, Constants const& constants, std::span<Vert const> init_data);

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

	private:

		// Apply forces to each particle
		void ApplyForces(ComputeJob& job);

		// Calculate the fluid density at the particle locations
		void CalculateDensities(ComputeJob& job);

		// Convert the particles buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(bool for_compute);
	};
}
