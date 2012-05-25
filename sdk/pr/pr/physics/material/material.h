//*********************************************
// Physics engine
//	(c)opyright Paul Ryland 2006
//*********************************************

#ifndef PR_PHYSICS_MATERIAL_H
#define PR_PHYSICS_MATERIAL_H
#pragma once

#include "pr/physics/types/forward.h"

namespace pr
{
	namespace ph
	{
		struct Material
		{
			float	m_density;				// Material density in kg/m^3
			float	m_static_friction;		// Co-efficient of static friction: 0 = no friction, 1 = infinite friction
			float	m_dynamic_friction;		// Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
			float	m_rolling_friction;		// Co-efficient of rolling friction: 0 = no friction, 1 = infinite friction 
			float	m_elasticity;			// Co-efficient of elasticity aka restitution: 0 = inelastic, 1 = completely elastic.f
			float	m_tangential_elasticity;// Co-efficient of tangential elasticity: -1.0f = bounces forward (frictionless), 0.0f = bounces up, 1.0f = bounces back
			float	m_tortional_elasticity;	// Co-efficient of tortional elasticity: -1.0f = normal ang mom unchanged (frictionless), 0.0f = normal ang mom goes to zero, 1.0f = normal ang mom reversed

			static Material make(
				float density,
				float static_friction,
				float dynamic_friction,
				float rolling_friction,
				float elasticity,
				float tangential_elasticity,
				float tortional_elasticity)
			{
				Material m =
				{
					density,
					static_friction,
					dynamic_friction,
					rolling_friction,
					elasticity,
					tangential_elasticity,
					tortional_elasticity
				};
				return m;
			}
		};
	}
}

#endif
