// Fluid
#include "src/fluid_simulation.h"
#include "src/particles.h"

namespace pr::fluid
{
	FluidSimulation::FluidSimulation()
		: m_particles(100, 0.05f)
		, m_spatial()
		, m_gravity(0, -9.8f, 0, 0)
		, m_restitution(0.3f)
	{
		std::default_random_engine rng;
		for (auto& particle : m_particles)
		{
			particle.m_pos = v3::Random(rng, v3(0, 1, 0), 0.5f).w1();
			particle.m_vel = v3::Random(rng, v3(0, 1, 0), 0.5f).w0();
		}

		m_spatial.Update(m_particles.m_particles);
	}
	
	int FluidSimulation::ParticleCount() const
	{
		return m_particles.ParticleCount();
	}

	void FluidSimulation::Step(float dt)
	{
		for (auto& particle : m_particles)
		{
			particle.m_vel += m_gravity * dt;

			// Predict the next position
			auto ray = particle.m_vel * dt;
			BoundaryCollision(particle, ray);
		}

		// Update the spatial partitioning of the particles
		m_spatial.Update(m_particles.m_particles);
	}

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
		if (particle.m_pos.y < m_particles.m_radius)
		{
			particle.m_pos.y =  m_particles.m_radius;
			particle.m_vel.y = -particle.m_vel.y * m_restitution;
		}
		if (particle.m_pos.x < -1)
		{
			//particle.m_pos.x = -1 + (particle.m_pos.x - -1);
			particle.m_vel.x = -particle.m_vel.x;
		}
		if (particle.m_pos.x > +1)
		{
			//particle.m_pos.x = +1 - (particle.m_pos.z - +1);
			particle.m_vel.x = -particle.m_vel.x;
		}
		if (particle.m_pos.z < -1)
		{
			//particle.m_pos.z = -1 - (particle.m_pos.z - -1);
			particle.m_vel.z = -particle.m_vel.z;
		}
		if (particle.m_pos.z > +1)
		{
			//particle.m_pos.z = +1 - (particle.m_pos.z - +1);
			particle.m_vel.z = -particle.m_vel.z;
		}
	}

	// Calculates the fluid density at 'position'
	float FluidSimulation::DensityAt(v4_cref position) const
	{
		constexpr auto SmoothingKernel = [](float radius, float distance) -> float
		{
			auto volume = maths::tauf * Pow(radius, 8.0f) / 2;
			auto v = Max(0.0f, Sqr(radius) - Sqr(distance));
			return Cube(v) / volume;
		};

		float density = 0;
		const float mass = 1;

		// Find all particles within the kernel radius
		m_spatial.Find(m_particles.m_particles, position, m_particles.m_radius, [&](auto const& particle)
		{
			auto dist = Length(particle.m_pos - position);
			auto influence = SmoothingKernel(m_particles.m_radius, dist);
			density += mass * influence;
		});

		return density;
	}

	// The influence at a distance of 'distance' from a particle with radius 'radius'
	float FluidSimulation::SmoothingKernel(float radius, float distance) const
	{
		if (distance >= radius)
			return 0.0f;
		//                                  /\
		// The square of an inverted cone: /  \ with height 1.
		auto volume = maths::tauf * Pow(radius, 4.0f) / 12;
		return Sqr(radius - distance) / volume;
	}
	
	// The derivative of the smoothing kernel
	float FluidSimulation::dSmoothingKernel(float radius, float distance) const
	{
		if (distance >= radius)
			return 0.0f;

		auto volume = maths::tauf * Pow(radius, 4.0f) / 24;
		return Sqr(distance - radius) / volume;
	}


}
