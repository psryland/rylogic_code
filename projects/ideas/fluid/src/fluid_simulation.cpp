// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"
#include "src/ispatial_partition.h"
#include "src/iboundary_collision.h"
#include "src/iexternal_forces.h"

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

	FluidSimulation::FluidSimulation(int particle_count, IBoundaryCollision& boundary, ISpatialPartition& spatial, IExternalForces& external)
		: m_gravity(0, -9.8f, 0, 0)
		, m_particles(particle_count)
		, m_densities(m_particles.size())
		, m_boundary(&boundary)
		, m_spatial(&spatial)
		, m_external(&external)
		, m_thermal_noise(0.001f)
		, m_radius(0.1f)
		, m_density0(Dimensions == 3 ? 1000.0f : 10.0f) // kg/m^3 (3d), kg/m^2 (2d)
		, m_mass(m_density0 * m_boundary->Volume() / isize(m_particles)) // kg
	{
		// Distribute the particles
		m_boundary->Fill(EFillStyle::Random, m_particles, m_radius);

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
		// Evolve the particles forward in time
		std::default_random_engine rng;
		for (auto& particle : m_particles)
		{
			auto pidx = m_particles.index(particle);

			// Use Leapfrog integration to predict the next particle position
			auto pos1 = particle.m_pos + particle.m_vel * dt / 2;

			// Sum up all sources of acceleration
			auto accel = v4::Zero();

			// Get the force experienced by the particle due to pressure
			auto pressure = PressureAt(pos1, pidx);
			accel += pressure / DensityAt(pidx);

			// Get the viscosity force experienced by the particle
			auto viscosity = ViscosityAt(pos1, pidx);
			accel += viscosity;

			// External forces
			auto external = m_external->ForceAt(*this, pos1, pidx);
			accel += external / DensityAt(pidx);
			
			// Gravity
			static Tweakable<float, "Gravity"> Gravity = 0.0f;
			accel += Gravity * m_gravity;

			// Check for valid acceleration
			if constexpr (Dimensions == 2) accel.z = 0;
			assert(accel.w == 0);

			//static Tweakable<float, "Drag"> Drag = 0.99f;
			//particle.m_vel *= Drag;

			// Integrate the particle dynamics
			particle.m_vel += accel * dt;
			
			// Collision restitution with the boundary
			auto [pos, vel] = m_boundary->ResolveCollision(particle, m_radius, dt);

			// Apply new position and velocity
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
		auto density = 0.0f;

		// Find all particles within the particle radius of 'position'
		m_spatial->Find(position, m_radius, m_particles, [&](auto const&, float dist_sq)
		{
			auto dist = Sqrt(dist_sq);
			auto influence = Particle::InfluenceAt(dist, m_radius);
			density += influence * m_mass;
		});

		return density;
	}
	float FluidSimulation::DensityAt(size_t index) const
	{
		return m_densities[index];
	}

	// Calculate the pressure gradient at 'position'
	v4 FluidSimulation::PressureAt(v4_cref position, std::optional<size_t> index) const
	{
		auto nett_pressure = v4::Zero();
		m_spatial->Find(position, m_radius, m_particles, [&](auto const& particle, float dist_sq)
		{
			auto idx = m_particles.index(particle);
			if (index && *index == idx)
				return;

			// The distance from 'position' to 'particle'
			auto dist = Sqrt(dist_sq);

			// Get the influence due to 'particle' at 'dist'
			auto influence = Particle::dInfluenceAt(dist, m_radius);

			// Get the direction from 'particle' to 'position'
			auto direction = position - particle.m_pos;
			direction = dist > maths::tinyf ? (direction / dist) : v4::RandomN(g_rng(), 0);
			if constexpr (Dimensions == 2) { direction.z = 0; }

			// We need to simulate the force due to pressure being applied to both particles (idx and index).
			// A simple way to do this is to average the pressure between the two particles. Since pressure is
			// a linear function of density, we can use the average density.
			auto density = index
				? (DensityAt(idx) + DensityAt(*index)) / 2.0f
				: DensityAt(idx);

			// Convert the density to a pressure (P = k * (rho - rho0))
			//static float C = 7.0f;
			static Tweakable<float, "DensityToPressure"> DensityToPressure = 7.0f;
			static Tweakable<float, "Density0"> Density0 = 0.0f;
			auto pressure = DensityToPressure * (density - Density0); //m_density0

			// Get the pressure gradient at 'position' due to 'particle'
			nett_pressure += (pressure * influence * m_mass / density) * direction;
		});

		return nett_pressure;
	}

	// Calculate the viscosity at 'position'
	v4 FluidSimulation::ViscosityAt(v4_cref position, std::optional<size_t> index) const
	{
		auto nett_viscosity = v4::Zero();
		m_spatial->Find(position, m_radius, m_particles, [&](auto const& particle, float dist_sq)
		{
			auto idx = m_particles.index(particle);
			if (index && *index == idx)
				return;

			// The distance from 'position' to 'particle'
			auto dist = Sqrt(dist_sq);

			// Get the influence due to 'particle' at 'dist'
			auto influence = Particle::dInfluenceAt(dist, m_radius);

			// Calculate the viscosity from the relative velocity of the particles
			auto visocity = index
				? (m_particles[idx].m_vel - m_particles[*index].m_vel)
				: v4::Zero();

			// Viscosity
			static Tweakable<float, "Viscosity"> Viscosity = 0.0f;
			nett_viscosity = Viscosity * influence * visocity;
		});

		return nett_viscosity;
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
