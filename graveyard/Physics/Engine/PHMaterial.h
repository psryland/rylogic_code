#ifndef PR_PHYSICS_MATERIAL_H
#define PR_PHYSICS_MATERIAL_H

namespace pr
{
	namespace ph
	{
		struct Material
		{
			float	m_density;				// Material density in kg/m^3
			float	m_static_friction;		// Co-efficient of static friction: 0 = no friction, 1 = infinite friction
			float	m_dynamic_friction;		// Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
			float	m_elasticity;			// Co-efficient of elasticity aka restitution: 0 = inelastic, 1 = completely elastic.f
			float	m_tangential_elasticity;// Co-efficient of tangential elasticity: -1.0f = bounces forward (frictionless), 0.0f = bounces up, 1.0f = bounces back
		};
	}//namespace ph
}//namespace pr

#endif//PR_PHYSICS_MATERIAL_H
