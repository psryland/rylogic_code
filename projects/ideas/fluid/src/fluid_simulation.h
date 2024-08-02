// Fluid
#pragma once
#include "src/forward.h"
#include "src/particle.h"
#include "src/probe.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		using Bucket = pr::vector<Particle>;
		using Densities = pr::vector<float>;

		v4 m_gravity;                   // Down
		Bucket m_particles;             // The particles being simulated
		Densities m_densities;          // The cached density at each particle position
		IBoundaryCollision* m_boundary; // The container collision for the fluid
		ISpatialPartition* m_spatial;   // Spatial partitioning of the particles
		IExternalForces* m_external;    // External forces acting on the fluid
		float m_thermal_noise;          // Random noise	
		float m_radius;                 // The radius of influence of a particle
		float m_density0;               // The expected density of the fluid
		float m_mass;                   // The mass of each particle

		FluidSimulation(int particle_count, float particle_radius, IBoundaryCollision& boundary, ISpatialPartition& spatial, IExternalForces& external);

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

		// Update the cache of density values at the particle locations
		void CacheDensities();
	};
}
