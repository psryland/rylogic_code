// Fluid
#include "src/fluid_simulation.h"
#include "src/particles.h"

namespace pr::fluid
{
	FluidSimulation::FluidSimulation()
		: m_particles(1000)
	{
		std::default_random_engine rng;
		for (auto& p : m_particles.m_positions)
		{
			p = v3::Random(rng, v3(0, 1, 0), 1.0f).w1();
		}
	}
	
	int FluidSimulation::ParticleCount() const
	{
		return m_particles.ParticleCount();
	}

	void FluidSimulation::Step(float dt)
	{
		(void)dt;
	}
	
}
