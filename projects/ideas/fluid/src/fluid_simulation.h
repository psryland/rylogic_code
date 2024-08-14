// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		rdr12::Renderer* m_rdr;               // The renderer instance to use to run the compute shader
		GpuJob m_job;                         // Manages running the compute shader steps
		ComputeStep m_cs_densities;           // Calculate the density at each particle position
		ComputeStep m_cs_boundary_effects;    // Make adjustments to account for the boundaries
		ComputeStep m_cs_apply_forces;        // Calculate the forces acting on each particle position
		ComputeStep m_cs_apply_probe;         // Apply forces from the probe
		ComputeStep m_cs_colour;              // Apply colours to the particles
		ComputeStep m_cs_density_map;         // Populate a texture with a map of the density field
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

			float Mass = 1.0f;                // The particle mass
			float DensityToPressure = 100.0f; // The conversion factor from density to pressure
			float Density0 = 0.0f;            // The baseline density
			float Viscosity = 10.0f;          // The viscosity scaler

			float ThermalDiffusion = 0.01f; // The thermal diffusion rate
			float GridScale = 10.0f;          // The scale factor for the spatial partition grid
			int CellCount = 1021;             // The number of grid cells in the spatial partition
			int RandomSeed = 0;               // Seed value for the RNG
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
		struct MapData
		{
			m4x4 MapToWorld = m4x4::Identity(); // Transform from map space to world space (including scale)
			iv2 MapTexDim = { 1,1 };            // The dimensions of the map texture
		};

		FluidSimulation(rdr12::Renderer& rdr, ParamsData const& params, std::span<Particle const> particle_init_data, std::span<CollisionPrim const> collision_init_data);

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

		// Update the particle colours without stepping the simulation
		void UpdateColours();

		// Read the particle positions from the particle buffer
		void ReadParticles(std::span<Particle> particles);

		// Create a map of the density over the map area
		void GenerateDensityMap(rdr12::Texture2DPtr tex_map, MapData const& map_data);

	private:

		// Create the buffer of particles
		void CreateParticleBuffer(std::span<Particle const> init_data);

		// Compile the compute shaders
		void CreateComputeSteps();

		// Calculate the fluid density at the particle locations
		void CalculateDensities(GpuJob& job, float dt);

		// Apply boundary effect corrections
		void BoundaryEffects(GpuJob& job);

		// Apply forces to each particle
		void ApplyForces(GpuJob& job, float dt);

		// Apply colours to the particles
		void ColourParticles(GpuJob& job);

		// Convert the particles buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(GpuJob& job, bool for_compute);

		// Run the debugging function
		void Debugging(GpuJob& job) const;
	};
}
