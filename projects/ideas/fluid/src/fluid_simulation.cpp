// Fluid
#include "src/fluid_simulation.h"
#include "src/particle.h"

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

	static const float Wall = 1.0f;
	static const float Ceiling = 5.0f;

	FluidSimulation::FluidSimulation()
		: m_gravity(0, -9.8f, 0, 0)
		, m_particles(30*30)
		, m_densities(m_particles.size())
		, m_spatial()
		, m_mass(0.001f) // kg
		, m_density0(1.0f) // kg/m^3 or g/cm^3
		, m_radius(0.08f)
		, m_restitution(0.3f)
	{
		if constexpr (1)
		{
			std::default_random_engine rng;
			for (auto& particle : m_particles)
			{
				particle.m_pos = v3::Random(rng, v3(-Wall, m_radius, -Wall), v3(+Wall, +2*Wall + m_radius, +Wall)).w1();
				particle.m_vel = v4::Zero();
				if constexpr (Dimensions == 2)
					particle.m_pos.z = 0;
			}
		}
		else
		{
			int const N = s_cast<int>(Pow<double>(isize(m_particles), 1.0 / Dimensions));

			float const sep = 0.01f; // 10cm
			float const halfW = 0.5f * N * sep;
			for (int i = 0, iend = isize(m_particles); i < iend; ++i)
			{
				auto& particle = m_particles[i];
				particle.m_vel = v4::Zero();
				if constexpr (Dimensions == 2)
				{
					particle.m_pos = v4(
						sep * (i % N) - halfW,
						sep * (i / N) + sep + m_radius,
						0,
						1);
				}
				if constexpr (Dimensions == 3)
				{
					particle.m_pos = v4(
						sep * ((i / 1) % N) - halfW,
						sep * ((i / N) % N) + sep + m_radius,
						sep * ((i / N * N) % N) - halfW,
						1);
				}
			}
		}

		// Update the spatial partitioning of the particles
		m_spatial.Update(m_particles);
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
			auto accel = pressure / m_densities[m_particles.index(particle)];
			particle.m_vel += accel * dt;

			// Fall under gravity
			//particle.m_vel += m_gravity * dt;

			// Drag
			particle.m_vel *= 0.9f;

			// Predict the next position
			auto ray = particle.m_vel * dt;
			BoundaryCollision(particle, ray);
			if constexpr (Dimensions == 2)
				particle.m_pos.z = 0;
		}

		// Update the spatial partitioning of the particles
		m_spatial.Update(m_particles);
	}

	// Calculates the fluid density at 'position'
	float FluidSimulation::DensityAt(v4_cref position) const
	{
		float density = 0;

		// Find all particles within the particle radius of 'position'
		m_spatial.Find(position, m_radius, m_particles, [&](auto const&, float dist_sq)
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
		m_spatial.Find(position, m_radius, m_particles, [&](auto const& particle, float dist_sq)
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

	// Apply collision resolution with the container boundary
	void FluidSimulation::BoundaryCollision(Particle& particle, v4_cref ray) const
	{
		//// Reflect the ray of all walls of the boundary
		//constexpr v4 Walls[] = { v4::YAxis(), v4(1, 0, 0, -1), v4(-1, 0, 0, 1), v4(0, 0, 1, -1), v4(0, 0, -1, +1) };
		//
		//// Reflect 'particle' off 'plane'
		//auto const Reflect = [this](Particle& particle, v4_cref ray, v4_cref plane)
		//{
		//	auto dist_above_plane = Dot(ray, plane);
		//	if (dist_above_plane > 0)
		//		return;

		//	// Conserve energy: mgh + 0.5mv^2. Set 'm' == 1
		//	auto nrg0 = Dot(ray, m_gravity) + 0.5f * Dot(ray, ray);

		//	// The distance to transport the particle to the surface of the plane
		//	auto dh = -dist_above_plane;
		//	
		//	// Conserve energy: mgh + 0.5mv^2. Set 'm' == 1
		//	auto nrg1 = Dot(ray, m_gravity) + 0.5f * Dot(ray, ray);
		//	assert(FEql(nrg0, nrg1));
		//	return;
		//};
		//
		//for (auto const& wall : Walls)
		//	Reflect(particle, ray, wall);

		//partical.m_pos = Reflect_RayToPlanes(particle.m_pos, ray, Walls);
		//Reflect_Direction(particle.m_pos, ray);

		particle.m_pos += ray;
		if (particle.m_pos.y < m_radius)
		{
			particle.m_pos.y = m_radius;
			particle.m_vel.y = -particle.m_vel.y * m_restitution;
		}
		if (particle.m_pos.y > Ceiling - m_radius)
		{
			particle.m_vel.y = -particle.m_vel.y;
		}
		if (particle.m_pos.x < -Wall)
		{
			//particle.m_pos.x = -1 + (particle.m_pos.x - -1);
			particle.m_vel.x = -particle.m_vel.x;
		}
		if (particle.m_pos.x > +Wall)
		{
			//particle.m_pos.x = +1 - (particle.m_pos.z - +1);
			particle.m_vel.x = -particle.m_vel.x;
		}
		if (particle.m_pos.z < -Wall)
		{
			//particle.m_pos.z = -1 - (particle.m_pos.z - -1);
			particle.m_vel.z = -particle.m_vel.z;
		}
		if (particle.m_pos.z > +Wall)
		{
			//particle.m_pos.z = +1 - (particle.m_pos.z - +1);
			particle.m_vel.z = -particle.m_vel.z;
		}
	}
}
