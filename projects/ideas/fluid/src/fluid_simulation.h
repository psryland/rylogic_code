// Fluid
#pragma once
#include "pr/maths/maths.h"
#include "src/particles.h"

namespace pr::fluid
{
	struct FluidSimulation
	{
		Particles m_particles;

		FluidSimulation();
		int ParticleCount() const;
		void Step(float dt);
	};
}
