//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

#include "pr/physics2/forward.h"

namespace pr::physics
{
	// Mass properties of a collision shape
	struct MassProperties
	{
		// Notes:
		//  This class is used as a return type when calculating inertia
		//  for Shapes. It's not used in the RigidBody object.
		//  All values are in model space.

		// Normalised object space inertia, expressed at the object centre of mass.
		m3x4 m_os_unit_inertia;

		// Offset to the centre of mass from the model space origin (note: w = 0)
		v4 m_centre_of_mass;

		// Mass
		float m_mass;

		MassProperties()
			:m_os_unit_inertia(v4{maths::float_inf}, v4{maths::float_inf}, v4{maths::float_inf})
			,m_centre_of_mass(v4{})
			,m_mass(maths::float_inf)
		{}
		MassProperties(m3_cref<> os_unit_inertia, v4_cref<> centre_of_mass, float mass)
			:m_os_unit_inertia(os_unit_inertia)
			,m_centre_of_mass(centre_of_mass)
			,m_mass(mass)
		{}
	};
}
