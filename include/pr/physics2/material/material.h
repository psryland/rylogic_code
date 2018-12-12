//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

namespace pr::physics
{
	struct Material
	{
		static int const NoID = -1;
		static int const DefaultID = 0;

		int m_id;                 // Unique id for this material
		float m_density;          // Material density in kg/m^3
		float m_friction_static;  // Co-efficient of static friction: 0 = no friction, 1 = infinite friction
		float m_friction_dynamic; // Co-efficient of dynamic friction: 0 = no friction, 1 = infinite friction
		float m_elasticity_norm;  // Co-efficient of elasticity in the collision normal direction. aka restitution: 0 = inelastic, 1 = completely elastic
		float m_elasticity_tang;  // Co-efficient of elasticity in the collision tangent direction: -1 = reverses tangential velocity, 0.0f = stops tangential velocity, 1.0f = frictionless

		Material(
			int id                 = DefaultID,
			float density          = 1000.0f,
			float friction_static  = 0.5f,
			float friction_dynamic = 0.5f,
			float elasticity_norm  = 1.0f,//0.5f,
			float elasticity_tang  = 0.0f)
			:m_id(id)
			,m_density(density)
			,m_friction_static(friction_static)
			,m_friction_dynamic(std::min(friction_static, friction_dynamic))
			,m_elasticity_norm(elasticity_norm)
			,m_elasticity_tang(elasticity_tang)
		{}
	};
}
