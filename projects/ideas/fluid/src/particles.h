// Fluid
#pragma once
#include "pr/container/vector.h"
#include "pr/maths/maths.h"

namespace pr::fluid
{
	struct Particles
	{
		pr::vector<v4> m_positions;
		pr::vector<v4> m_velocties;

		explicit Particles(int count)
		{
			m_positions.resize(count);
			m_velocties.resize(count);
		}

		// The number of simulated particles
		int ParticleCount() const
		{
			return isize(m_positions);
		}

	};
}
