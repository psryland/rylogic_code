// Fluid
#include "src/bucket_collision.h"
#include "src/particle.h"

namespace pr::fluid
{
	BucketCollision::BucketCollision()
		: m_hwidth(1.0f)
		, m_ceiling(5.0f)
		, m_restitution(0.3f)
	{}

	// Distribute the particles within the boundary
	void BucketCollision::Fill(std::span<Particle> particles, float radius) const
	{
		const bool Random = true;
		const bool Lattice = false;

		if constexpr (Random)
		{
			std::default_random_engine rng;
			for (auto& particle : particles)
			{
				// Uniform distribution over the volume
				particle.m_pos = v3::Random(rng, v3(-m_hwidth, radius, -m_hwidth), v3(+m_hwidth, +2*m_hwidth - radius, +m_hwidth)).w1();
				particle.m_vel = v4::Zero();
				if constexpr (Dimensions == 2)
					particle.m_pos.z = 0;
			}
		}

		if constexpr (Lattice)
		{
			// The number of particles on an edge of the volume/area
			int const N = s_cast<int>(Pow<double>(isize(particles), 1.0 / Dimensions));

			float const sep = 0.01f; // 10cm
			float const halfW = 0.5f * N * sep;
			for (int i = 0, iend = isize(particles); i < iend; ++i)
			{
				auto& particle = particles[i];
				particle.m_vel = v4::Zero();
				if constexpr (Dimensions == 2)
				{
					particle.m_pos = v4(
						sep * (i % N) - halfW,
						sep * (i / N) + sep + radius,
						0,
						1);
				}
				if constexpr (Dimensions == 3)
				{
					particle.m_pos = v4(
						sep * ((i / 1) % N) - halfW,
						sep * ((i / N) % N) + sep + radius,
						sep * ((i / N * N) % N) - halfW,
						1);
				}
			}
		}
	}

	// Apply collision resolution with the container boundary
	BucketCollision::Dynamics BucketCollision::ResolveCollision(Particle const& particle, float radius, float dt) const
	{
#if false
		//// Reflect the ray off all walls of the boundary
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
#endif
		// The particle velocity
		auto vel = particle.m_vel;

		// The vector to the next position of the particle
		auto ray = particle.m_vel * dt;

		// The un-collided next position of the particle
		auto pos = particle.m_pos + ray;

		// Bottom
		if (pos.y < radius && ray.y < 0)
		{
			// The point of intercept along 'vel'
			auto t = (particle.m_pos.y - radius) / -ray.y;
			assert(t >= 0.0f && t <= 1.0f);

			// Advance to the intercept
			pos = particle.m_pos + ray * t;
			ray *= (1 - t);

			// Reflect the ray
			ray.y = -ray.y * m_restitution;

			// Advance to the end of the ray
			pos += ray;

			// Reflect the velocity
			vel.y = -vel.y * m_restitution;
		}

		// Top
		if (pos.y > m_ceiling - radius && vel.y > 0)
		{
			// Reflect the velocity
			vel.y = -vel.y;
		}

		// Sides
		if (Abs(pos.x) > m_hwidth && Sign(vel.x) == Sign(pos.x))
		{
			vel.x = -vel.x;
		}
		if (Abs(pos.z) > m_hwidth && Sign(vel.z) == Sign(pos.z))
		{
			vel.z = -vel.z;
		}

		if constexpr (Dimensions == 2)
		{
			pos.z = 0;
			vel.z = 0;
		}

		return { pos, vel };
	}
}
