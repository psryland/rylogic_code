//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2016
//*********************************************
#pragma once

// Mass properties

#include "pr/physics2/forward.h"

namespace pr
{
	namespace physics
	{
		// Mass properties for a rigid body
		struct MassProperties
		{
			// Object space inertia tensor
			m3x4 m_os_inertia_tensor;

			// Offset to the object space centre of mass from the model space origin (note: w = 0)
			v4 m_centre_of_mass;

			// Mass in kg
			kg_t m_mass;

			MassProperties() = default;
			MassProperties(m3x4 const& os_inertia_tensor, v4 const& centre_of_mass, kg_t mass)
				:m_os_inertia_tensor(os_inertia_tensor)
				,m_centre_of_mass(centre_of_mass)
				,m_mass(mass)
			{}
		};

		// Direction for translating an inertia tensor
		enum class ETranslateType
		{
			TowardCoM,
			AwayFromCoM 
		};

		// Transform an inertia tensor using the parallel axis theorem.
		// 'offset' is the distance from (or toward) the centre of mass (determined by 'translate_type')
		// 'inertia' and 'offset' must be in the same frame.
		inline void ParallelAxisTranslateInertia(m3x4& inertia, v4 const& offset, float mass, ETranslateType translate_type)
		{
			if (translate_type == ETranslateType::TowardCoM)
				mass = -mass;

			for (auto i = 0; i != 3; ++i)
			{
				for (auto j = i; j != 3; ++j)
				{
					if (i == j)
					{
						// For the diagonal elements:
						///  I = Io + md^2 (away from CoM), Io = I - md^2 (toward CoM)
						/// 'd' is the perpendicular component of 'offset'
						auto i1 = (i + 1) % 3;
						auto i2 = (i + 2) % 3;
						inertia[i][i] += mass * (offset[i1] * offset[i1] + offset[i2] * offset[i2]);
					}
					else
					{
						// For off diagonal elements:
						///  Ixy = Ioxy + mdxdy  (away from CoM), Io = I - mdxdy (toward CoM)
						///  Ixz = Ioxz + mdxdz  (away from CoM), Io = I - mdxdz (toward CoM)
						///  Iyz = Ioyz + mdydz  (away from CoM), Io = I - mdydz (toward CoM)
						auto delta = mass * (offset[i] * offset[j]);
						inertia[i][j] += delta;
						inertia[j][i] += delta;
					}
				}
			}
		}
	}
}
