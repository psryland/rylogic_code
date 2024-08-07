// Fluid
#include "src/bucket_collision.h"
#include "src/particle.h"

namespace pr::fluid
{
	BucketCollision::BucketCollision()
		: m_hwidth(1.0f)
		, m_hheight(0.5f)
		, m_ceiling(2.0f)
		, m_restitution(0.95f, 1.0f)
	{}
	
	// The approximate volume (in m^3 or m^2 depending on Dimensions) occupied by the particles under normal conditions
	float BucketCollision::Volume() const
	{
		if constexpr (Dimensions == 2)
		{
			return (2 * m_hwidth) * (2 * m_hheight);
		}
		if constexpr (Dimensions == 3)
		{
			return (2 * m_hwidth) * (2 * m_hwidth) * (2 * m_hheight);
		}
	}

	// Generate a distribution of positions within the boundary
	void BucketCollision::Fill(EFillStyle style, int count, float radius, std::function<void(v4 const&)> points) const
	{
		(void)radius;
		switch (style)
		{
			case EFillStyle::Point:
			{
				for (int i = 0; i != count; ++i)
					points(v4(0, 0, 0, 1));

				break;
			}
			case EFillStyle::Random:
			{
				auto const margin = 0.95f;
				auto hw = m_hwidth * margin;
				auto hh = m_hheight * margin;

				// Uniform distribution over the volume
				std::default_random_engine rng;
				for (int i = 0; i != count; ++i)
				{
					auto pos = v3::Random(rng, v3(-hw, -hh, -hw), v3(+hw, +hh, +hw)).w1();
					if constexpr (Dimensions == 2) pos.z = 0;
					points(pos);
				}
				break;
			}
			case EFillStyle::Lattice:
			{
				auto const margin = 0.95f;
				auto hw = m_hwidth * margin;
				auto hh = m_hheight * margin;

				if constexpr (Dimensions == 2)
				{
					// Want to spread N particles evenly over the volume.
					// Area is 2*hwidth * 2*hheight
					// Want to find 'step' such that:
					//   (2*hwidth / step) * (2*hheight / step) = N
					// => step = sqrt((2*hwidth * 2*hheight) / N)
					auto step = Sqrt((2 * hw * 2 * hh) / count);

					auto x = -hw + step/2;
					auto y = -hh + step/2;
					for (int i = 0; i != count; ++i)
					{
						points(v4(x, y, 0, 1));

						x += step;
						if (x > hw) { x = -hw + step/2; y += step; }
					}
				}
				if constexpr (Dimensions == 3)
				{
					// Want to spread N particles evenly over the volume.
					// Volume is 2*hwidth * 2*hwidth * 2*hheight
					// Want to find 'step' such that:
					//  (2*hwidth/step) * (2*hwidth/step) * (2*hheight/step) = N
					// => step = cubert((2*hwidth * 2*hwidth * 2*hheight) / N)
					auto step = Cubert((2 * hw * 2 * hh * 2 * hw) / count);

					auto x = -hw + step/2;
					auto y = -hh + step/2;
					auto z = -hw + step/2;
					for (int i = 0; i != count; ++i)
					{
						points(v4(x, y, z, 1));

						x += step;
						if (x > hw) { x = -hw + step/2; z += step; }
						if (z > hw) { z = -hw + step/2; y += step; }
					}
				}
				break;
			}
			case EFillStyle::Grid:
			{
				auto const margin = 1.0f;//0.95f;
				auto hw = m_hwidth * margin;
				auto hh = m_hheight * margin;
				auto step = 0.1f;

				if constexpr (Dimensions == 2)
				{
					auto x = -hw + step / 2.0f;
					auto y = -hh + step / 2.0f;
					for (int i = 0; i != count; ++i)
					{
						points(v4(x, y, 0, 1));

						x += step;
						if (x > hw) { x = -hw + step/2; y += step; }
					}
				}
				break;
			}
		}
	}

	// Apply collision resolution with the container boundary
	BucketCollision::Dynamics BucketCollision::ResolveCollision(Particle const& particle, float radius, float dt) const
	{
		(void)radius;

		// The particle velocity
		auto vel = particle.m_vel;

		// The vector to the next position of the particle
		auto ray = particle.m_vel * dt;

		// The Walls of the container
		v4 const Walls[] =
		{
			v4( 0, 1, 0, m_hheight),
			v4(-1, 0, 0, m_hwidth),
			v4(+1, 0, 0, m_hwidth),
			v4( 0, 0, -1, m_hwidth),
			v4( 0, 0, +1, m_hwidth),
			v4( 0,-1, 0, m_hheight), // lid
		};

		// Reflect the ray off all walls of the boundary
		auto pos = particle.m_pos;

		// Conserve energy: mgh + 0.5mv^2. Set 'm' == 1
		auto nrg0 = particle.m_pos.y + 0.5f * Dot(vel, vel);

		// Repeat until ray consumed
		for (;;)
		{
			// Find the nearest intercept
			auto t = 1.0f;
			auto idx = -1;
			for (auto i = 0; i != _countof(Walls); ++i)
			{
				auto& wall = Walls[i];

				// Ignore if the particle is moving away from the wall.
				// 'step' is the length of the projection of 'ray' onto 'wall'
				auto step = Dot(ray, wall);
				if (step >= 0)
					continue;

				// The distance to the wall
				auto dist = Dot(pos, wall);
				if (dist < 0 || dist >= -step)
					continue;

				auto t1 = -dist / step;
				if (t1 < 0 || t1 > t)
					continue;

				t = t1;
				idx = i;
			}

			// Advance the point to the intercept
			pos += ray * t;
			ray = (1 - t) * ray;

			// Stop if no intercept found
			if (idx == -1)
				break;

			// Get the normal and tangential part of 'ray' relative to the wall
			auto ray_n = -Dot(ray, Walls[idx]) * Walls[idx].w0();
			auto ray_t = ray + ray_n;

			// Reflect the ray + apply restitution
			ray = ray_n * m_restitution.x + ray_t * m_restitution.y;

			// Update the velocity
			auto vel_n = -Dot(vel, Walls[idx]) * Walls[idx].w0();
			auto vel_t = vel + vel_n;
			vel = vel_n * m_restitution.x + vel_t * m_restitution.y;
		}

		// Conserve energy: mgh + 0.5mv^2. Set 'm' == 1
		auto nrg1 = pos.y + 0.5f * Dot(vel, vel);
		(void)nrg1, nrg0;
		//assert(nrg0 >= nrg1);

		if constexpr (Dimensions == 2)
		{
			pos.z = 0;
			vel.z = 0;
		}

		return { pos, vel };
	}
}
