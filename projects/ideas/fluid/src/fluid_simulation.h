// Fluid
#pragma once
#include "src/forward.h"
#include "src/particle.h"
#include "src/probe.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		rdr12::Renderer* m_rdr;               // The renderer instance to use to run the compute shader
		rdr12::ComputeJob m_job;              // Manages running the compute shader steps
		rdr12::ComputeStep m_cs_densities;    // Calculate the density at each particle position
		rdr12::ComputeStep m_cs_apply_forces; // Calculate the forces acting on each particle position
		rdr12::ComputeStep m_cs_integrate;    // Update the particle positions and apply collision
		D3DPtr<ID3D12Resource> m_r_particles; // The vertex buffer of the particles (includes position/colour/norm(velocity))
		D3DPtr<ID3D12Resource> m_r_collision; // A buffer of collision primitives





		using Bucket = pr::vector<Particle>;
		using Densities = pr::vector<float>;

		v4 m_gravity;                   // Down
		Bucket m_particles;             // The particles being simulated
		Densities m_densities;          // The cached density at each particle position
		IBoundaryCollision* m_boundary; // The container collision for the fluid
		SpatialPartition* m_spatial;    // Spatial partitioning of the particles
		IExternalForces* m_external;    // External forces acting on the fluid
		float m_thermal_noise;          // Random noise	
		float const m_radius;           // The radius of influence of a particle
		float m_density0;               // The expected density of the fluid
		float m_mass;                   // The mass of each particle
		int64_t m_count;                // The number of particles


		FluidSimulation(rdr12::Renderer& rdr, int particle_count, float particle_radius, IBoundaryCollision& boundary, SpatialPartition& spatial, IExternalForces& external);

		// The number of simulated particles
		int ParticleCount() const;

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

		// Calculates the fluid density at 'position'
		float DensityAt(v4_cref position) const;
		float DensityAt(size_t index) const;

		// Calculate the pressure gradient at 'position'
		v4 PressureAt(v4_cref position, std::optional<size_t> index) const;

		// Calculate the viscosity at 'position'
		v4 ViscosityAt(v4_cref position, std::optional<size_t> index) const;

	private:

		// Get the compute shader constants for "fluid" compute shaders
		struct cbFluid FluidConstants(float dt = 0.0f) const;

		// Get the compute shader constants for "collision" compute shaders
		struct cbCollision CollisionConstants(float dt = 0.0f) const;

		// Create the D3D Resources
		void CreateBuffers(int particle_count);

		// Create the compute steps for the fluid simulation
		void CreateComputeSteps();

		// Advance the particles in time
		void Integrate();

		// Apply forces to each particle
		void ApplyForces();

		// Calculate the fluid density at the particle locations
		void CalculateDensities();

		// Convert the particles buffer to a compute resource or a vertex buffer
		void ParticleBufferAsUAV(bool for_compute);
	};
}
