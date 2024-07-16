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
		float m_mass;                   // The mass of each particle
		float m_radius;                 // The radius of influence of a particle
		float m_density0;               // The expected density of the fluid

		FluidSimulation(IBoundaryCollision& boundary, ISpatialPartition& spatial);

		// The number of simulated particles
		int ParticleCount() const;

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

		// CalculateTs the fluid density at 'position'
		float DensityAt(v4_cref position) const;

		// Calculate the pressure gradient at 'position'
		v4 PressureAt(v4_cref position, std::optional<size_t> index) const;

	};
}
