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
		struct RigidBody
		{
			// World space position of the rigid bodies' centre of mass
			m4x4 m_o2w;

			// The external forces and torques applied to this body (in world space)
			v8f m_force;

			// Mass properties
			m3x4  m_os_inertia_tensor;        // The object space inertia tensor
			m3x4  m_os_inv_inertia_tensor;    // The object space inverse inertia tensor
			m3x4  m_ws_inv_inertia_tensor;    // The world space inverse inertia tensor. Calculated per step
			kg_t  m_mass;                     // The mass of the object
			float m_inv_mass;                 // The inverse mass

			// Collision shape
			Shape const* m_shape;

			template <typename TShape, typename = enable_if_shape<TShape>> RigidBody(TShape const* shape)
				:m_o2w()
				,m_force()
				,m_mass(1.0_kg)
				,m_shape(&shape->m_base)
			{}
		};
	}
}
