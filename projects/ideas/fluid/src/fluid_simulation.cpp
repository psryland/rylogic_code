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
	//  - Density of water is 1000kg/m^3 = 1g/cm^3
	//  - Pressure of water at sea level = 101 kN/m^2
	//  - Hydrostatic pressure vs. depth: P = rho * g * h
	//
	// A particle represents a small unit of fluid. Given a volume and a number of particles,
	// the mass of each fluid unit is: mass = density * volume / number of particles.

	FluidSimulation::FluidSimulation(int particle_count, IBoundaryCollision& boundary, ISpatialPartition& spatial)
		: m_gravity(0, -9.8f, 0, 0)
		, m_particles(particle_count)
		, m_densities(m_particles.size())
		, m_boundary(&boundary)
		, m_spatial(&spatial)
		, m_thermal_noise(0.001f)
		, m_radius(0.1f)
		, m_density0(Dimensions == 3 ? 1000.0f : 10.0f) // kg/m^3 (3d), kg/m^2 (2d)
		, m_mass(m_density0 * m_boundary->Volume() / isize(m_particles)) // kg
	{
		// Distribute the particles
		m_boundary->Fill(EFillStyle::Point, m_particles, m_radius);

		// Update the spatial partitioning of the particles
		m_spatial->Update(m_particles);

		// Update the cache of density values at the particle locations
		CacheDensities();
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

		// Evolve the particles forward in time
		std::default_random_engine rng;
		for (auto& particle : m_particles)
		{
			auto pidx = m_particles.index(particle);

			// Get the force experienced by the particle due to pressure
			auto pressure = PressureAt(particle.m_pos, pidx);

			// Sum up all sources of acceleration
			auto accel = v4::Zero();
			accel += pressure / m_densities[pidx];
			//accel += m_thermal_noise * v4::RandomN(rng, 0);
			//accel += m_gravity;
			if (accel.w != 0)
				accel = accel;

			// Update velocity
			particle.m_vel += accel * dt;
			particle.m_vel *= 0.95f; // drag
			
			// Collision restitution with the boundary
			auto [pos, vel] = m_boundary->ResolveCollision(particle, m_radius, dt);

			// Integrate the particle dynamics
			particle.m_pos = pos;
			particle.m_vel = vel;
		}

		// Update the spatial partitioning of the particles
		m_spatial->Update(m_particles);

		// Update the cached densities at the particle positions
		CacheDensities();
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
		auto pressure = v4::Zero();
		m_spatial->Find(position, m_radius, m_particles, [&](auto const& particle, float dist_sq)
		{
			auto idx = m_particles.index(particle);
			if (index && *index == idx)
				return;

			// Get the direction from 'position' to 'particle'
			auto direction = particle.m_pos - position;
			auto dist = Sqrt(dist_sq);

			// Normalize the direction vector
			direction = dist > maths::tinyf ? (direction / dist) : v4::RandomN(g_rng(), 0);
			if constexpr (Dimensions == 2) { direction.z = 0; }

			// We need to simulate the force due to pressure being applied to
			// both particles (idx and index). Taking the average of the densities
			// at the two particle positions is kind of the same as applying the force
			// equal and opposite to both particles

			// Get the density at the particle position. Pressure is due to
			// a difference in density, so compare to the target density to get pressure
			
			auto density = index
				? (m_densities[idx] + m_densities[*index]) / 2.0f
				: m_densities[idx];

			//static tweakables::Tweakable<float, "DensityToPressure"> C = 7.0f;
			static float C = 7.0f;
			auto pres = C * (density - m_density0);

			// Get the pressure gradient at 'position' due to 'particle'
			auto influence = Particle::dInfluenceAt(dist, m_radius);
			pressure += (pres * influence * m_mass / density) * direction;
		});

		return pressure;
	}

	// Update the cache of density values at the particle locations
	void FluidSimulation::CacheDensities()
	{
		for (auto& particle : m_particles)
		{
			auto density = DensityAt(particle.m_pos);
			m_densities[m_particles.index(particle)] = density;
		}
	}
}
