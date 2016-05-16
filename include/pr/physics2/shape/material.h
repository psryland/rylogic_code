//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr
{
	namespace physics
	{
		struct Material
		{
			kg_p_m³_t m_density;           // Material density in kg/m^3
			float m_static_friction;       // Co-efficient of static friction: 0 = no friction, 1 = infinite friction
			float m_dynamic_friction;      // Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
			float m_rolling_friction;      // Co-efficient of rolling friction: 0 = no friction, 1 = infinite friction 
			float m_elasticity;            // Co-efficient of elasticity aka restitution: 0 = inelastic, 1 = completely elastic.f
			float m_tangential_elasticity; // Co-efficient of tangential elasticity: -1.0f = bounces forward (frictionless), 0.0f = bounces up, 1.0f = bounces back
			float m_torsional_elasticity;  // Co-efficient of torsional elasticity: -1.0f = normal ang momentum unchanged (frictionless), 0.0f = normal ang momentum goes to zero, 1.0f = normal ang momentum reversed

			Material(
				kg_p_m³_t density           = 1000.0f,
				float static_friction       = 0.5f,
				float dynamic_friction      = 0.5f,
				float rolling_friction      = 0.5f,
				float elasticity            = 0.5f,
				float tangential_elasticity = 0.5f,
				float torsional_elasticity  = 0.5f)
				:m_density              (density              )
				,m_static_friction      (static_friction      )
				,m_dynamic_friction     (dynamic_friction     )
				,m_rolling_friction     (rolling_friction     )
				,m_elasticity           (elasticity           )
				,m_tangential_elasticity(tangential_elasticity)
				,m_torsional_elasticity (torsional_elasticity )
			{}
		};
	}
}