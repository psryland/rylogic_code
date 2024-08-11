// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		rdr12::Renderer* m_rdr;               // The renderer instance to use to run the compute shader
		ComputeJob m_job;                     // Manages running the compute shader steps
		ComputeStep m_cs_densities;           // Calculate the density at each particle position
		ComputeStep m_cs_apply_forces;        // Calculate the forces acting on each particle position
		ComputeStep m_cs_apply_probe;         // Apply forces from the probe
		ComputeStep m_cs_colour;              // Apply colours to the particles
		ComputeStep m_cs_debugging;           // Debugging CS function
		D3DPtr<ID3D12Resource> m_r_particles; // The buffer of the particles (includes position/colour/norm(velocity))
		SpatialPartition m_spatial;           // Spatial partitioning of the particles
		ParticleCollision m_collision;        // The collision resolution for the fluid
		int m_frame;                          // Frame counter

		struct ParamsData
		{
			int NumParticles = 0;             // The number of particles
			float ParticleRadius = 0.1f;      // The radius of influence for each particle
			int CellCount = 1021;             // The number of grid cells in the spatial partition
			float GridScale = 10.0f;          // The scale factor for the spatial partition grid

			v4 Gravity = { 0, -9.8f, 0, 0 };  // The acceleration due to gravity

			float Mass = 1.0f;                // The particle mass
			float DensityToPressure = 100.0f; // The conversion factor from density to pressure
			float Density0 = 0.0f;            // The baseline density
			float Viscosity = 10.0f;          // The viscosity scaler

			float ThermalDiffusion = 0.01f; // The thermal diffusion rate
			float TimeStep = 0.0f;          // Particle position prediction
			int RandomSeed = 0;             // Seed value for the RNG
		} Params;
		struct ColoursData
		{
			// The colour scale to use
			Colour Colours[4] = {
				Colour(0xFF0000A0),
				Colour(0xFFFF0000),
				Colour(0xFFFFFF00),
				Colour(0xFFFFFFFF),
			};
			v2 Range = { 0, 1 };

			// Colouring scheme
			uint32_t VelocityBased : 1 = 0;
			uint32_t DensityBased : 1 = 0;
			uint32_t WithinProbe : 1 = 0;
		} Colours;
		struct ProbeData
		{
			v4 Position = { 0,0,0,1 };
			Colour Colour = pr::Colour(0xFFFFFF00);
			float Radius = 0.1f;
			float Force = 0.0f;
		} Probe;

		FluidSimulation(rdr12::Renderer& rdr, ParamsData const& params, std::span<Particle const> particle_init_data, std::span<CollisionPrim const> collision_init_data);

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

		// Update the particle colours without stepping the simulation
		void UpdateColours();

		// Read the particle positions from the particle buffer
		void ReadParticles(std::span<Particle> particles);

	private:

		// Calculate the fluid density at the particle locations
		void CalculateDensities(ComputeJob& job, float dt);

		// Apply forces to each particle
		void ApplyForces(ComputeJob& job, float dt);

		// Apply colours to the particles
		void ColourParticles(ComputeJob& job);

		// Convert the particles buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(ComputeJob& job, bool for_compute);

		// Run the debugging function
		void Debugging(ComputeJob& job) const;
	};
}
