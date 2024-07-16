// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"
#include "src/ispatial_partition.h"
#include "src/iboundary_collision.h"

namespace pr::fluid
{
	// Smooth Particle Dynamics:
	//  The value of some property 'A' at 'x' is the weighted sum of the values of 'A' at each particle
	//  A(x) = Sum_i A_i * (mass_i / density_i) * W(x - x_i)
	// 
	// Use SI units.
	// Density of water is 1000kg/m^3 = 1g/cm^3
	// So water should have a particle every 0.01m
	// Hydrostatic pressure vs. depth: P = rho * g * h

	FluidSimulation::FluidSimulation(IBoundaryCollision& boundary, ISpatialPartition& spatial)
		: m_gravity(0, -9.8f, 0, 0)
		, m_particles(30*30)
		, m_densities(m_particles.size())
		, m_boundary(&boundary)
		, m_spatial(&spatial)
		, m_mass(0.001f) // kg
		, m_density0(1.0f) // kg/m^3 or g/cm^3
		, m_radius(0.08f)
	{
		// Distribute the particles
		m_boundary->Fill(m_particles, m_radius);

		// Update the spatial partitioning of the particles
		m_spatial->Update(m_particles);
	}
	
	// The number of simulated particles
	int FluidSimulation::ParticleCount() const
	{
		return isize(m_particles);
	}

	// Advance the simulation forward in time by 'dt' seconds
	void FluidSimulation::Step(float dt)
	{
		//dt = 0;

		// Update the cached densities at the particle positions
		for (auto& particle : m_particles)
			m_densities[m_particles.index(particle)] = DensityAt(particle.m_pos);

		// Evolve the particles forward in time
		for (auto& particle : m_particles)
		{
			// Get the force experienced by the particle due to pressure
			auto pressure = PressureAt(particle.m_pos, m_particles.index(particle));

			// Sum up all sources of acceleration
			auto accel = m_gravity + pressure / m_densities[m_particles.index(particle)];

			// Update velocity
			particle.m_vel += accel * dt;
			particle.m_vel *= 0.9f; // drag
			
			// Collision restitution with the boundary
			auto [pos, vel] = m_boundary->ResolveCollision(particle, m_radius, dt);

			// Integrate the particle dynamics
			particle.m_pos = pos;
			particle.m_vel = vel;
		}

		// Update the spatial partitioning of the particles
		m_spatial->Update(m_particles);
	}

	// Calculates the fluid density at 'position'
	float FluidSimulation::DensityAt(v4_cref position) const
	{
		float density = 0;

		// Find all particles within the particle radius of 'position'
		m_spatial->Find(position, m_radius, m_particles, [&](auto const&, float dist_sq)
		{
			auto dist = Sqrt(dist_sq);
			auto influence = Particle::InfluenceAt(dist, m_radius);
			density += influence * m_mass;
		});

		return density;
	}

	// Calculate the pressure gradient at 'position'
	v4 FluidSimulation::PressureAt(v4_cref position, std::optional<size_t> index) const
	{
		v4 pressure = v4::Zero();
		m_spatial->Find(position, m_radius, m_particles, [&](auto const& particle, float dist_sq)
		{
			if (index && *index == m_particles.index(particle))
				return;

			// Get the direction from 'position' to 'particle'
			auto direction = particle.m_pos - position;
			auto dist = Sqrt(dist_sq);

			// Normalize the direction vector
			direction = dist != 0 ? (direction / dist) : v4::RandomN(g_rng(), 0);

			// Get the density at the particle position. Pressure is due to
			// a difference in density, so compare to the target density to get pressure
			auto density = m_densities[m_particles.index(particle)];
			static float C = 7.0f;
			auto pres = C * (density - m_density0);

			// Get the pressure gradient at 'position' due to 'particle'
			auto influence = Particle::dInfluenceAt(dist, m_radius);
			pressure += (pres * influence * m_mass / density) * direction;
		});

		return pressure;
	}
}
