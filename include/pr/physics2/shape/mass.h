//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"
#include "pr/physics2/shape/inertia.h"

namespace pr
{
	namespace physics
	{
		// Mass properties for a rigid body
		struct MassProperties
		{
			// Normalised object space inertia tensor. Multiply by mass to get the actual mass tensor
			m3x4 m_os_inertia_tensor;

			// Offset to the object space centre of mass from the model space origin (note: w = 0)
			v4 m_centre_of_mass;

			// Mass in kg
			kg_t m_mass;

			MassProperties() = default;
			MassProperties(m3_cref<> os_inertia_tensor, v4_cref<> centre_of_mass, kg_t mass)
				:m_os_inertia_tensor(os_inertia_tensor)
				,m_centre_of_mass(centre_of_mass)
				,m_mass(mass)
			{}
		};
	}
}
