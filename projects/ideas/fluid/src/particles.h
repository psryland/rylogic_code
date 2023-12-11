// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct Particle
	{
		v4 m_pos;
		v4 m_vel;
	};

	struct Particles
	{
		pr::vector<Particle> m_particles;
		float m_radius;

		Particles(int count, float radius)
			:m_particles(count)
			,m_radius(radius)
		{}

		// The number of simulated particles
		int ParticleCount() const
		{
			return isize(m_particles);
		}

		auto begin() -> decltype(m_particles.begin())
		{
			return m_particles.begin();
		}
		auto end() -> decltype(m_particles.end())
		{
			return m_particles.end();
		}
		auto begin() const -> decltype(m_particles.begin())
		{
			return m_particles.begin();
		}
		auto end() const -> decltype(m_particles.end())
		{
			return m_particles.end();
		}
	};
}
