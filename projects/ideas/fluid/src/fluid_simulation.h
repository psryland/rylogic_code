// Fluid
#pragma once
#include "src/forward.h"
#include "src/particle.h"
#include "src/probe.h"
#include "src/spatial_partition.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		using Bucket = pr::vector<Particle>;
		using Densities = pr::vector<float>;

		v4 m_gravity;               // Down
		Bucket m_particles;         // The particles being simulated
		Densities m_densities;      // The cached density at each particle position
		SpatialPartition m_spatial; // Spatial partitioning of the particles
		float m_mass;               // The mass of each particle
		float m_density0;           // The expected density of the fluid
		float m_radius;             // The radius of influence of a particle
		float m_restitution;        // The coefficient of restitution of the particles

		FluidSimulation();

		// The number of simulated particles
		int ParticleCount() const;

		// Advance the simulation forward in time by 'dt' seconds
		void Step(float dt);

		// Apply collision resolution with the container boundary
		void BoundaryCollision(Particle& particle, v4_cref next) const;

		// CalculateTs the fluid density at 'position'
		float DensityAt(v4_cref position) const;

		// Calculate the pressure gradient at 'position'
		v4 PressureAt(v4_cref position, std::optional<size_t> index) const;

	};
}
