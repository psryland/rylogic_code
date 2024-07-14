// Fluid
#pragma once
#include "src/forward.h"

namespace pr::fluid
{
	struct Particle
	{
		v4 m_pos;
		v4 m_vel;

		// The influence at 'distance' from a particle
		static float InfluenceAt(float distance, float radius);
	
		// The gradient of the influence at 'distance' from a particle
		static float dInfluenceAt(float distance, float radius);
	};
}
