//*******************************************************************
//
//	Physics Materials
//
//*******************************************************************
//	struct Material
//	{
//		float	m_density;				// Material density in kg/m^3
//		float	m_static_friction;		// Co-efficient of static friction: 0 = no friction, 1 = infinite friction
//		float	m_dynamic_friction;		// Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
//		float	m_elasticity;			// Co-efficient of elasticity aka restitution: 0 = inelastic, 1 = completely elastic.
//		float	m_tangential_elasticity;// Co-efficient of tangential elasticity: -1.0f = bounces forward (frictionless), 0.0f = bounces up, 1.0f = bounces back
//	};

#include "Stdafx.h"
#include "PhysicsLab_LDPI/PhysicsMaterials.h"

ph::Material g_physics_materials[] =
{
	{1.0f, 0.0f, 0.0f, 1.0f, -1.0f}
};

uint g_max_physics_materials = sizeof(g_physics_materials) / sizeof(ph::Material);