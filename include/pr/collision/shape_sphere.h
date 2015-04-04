//*********************************************
// Collision
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once

#include "pr/collision/shape.h"

namespace pr
{
	namespace collision
	{
		struct ShapeSphere
		{
			Shape m_base;
			float m_radius;

			operator Shape const&() const { return m_base; }
			operator Shape& ()            { return m_base; }

			static ShapeSphere make(float radius, m4x4 const& s2p = pr::m4x4Identity, uint flags = 0)
			{
				return ShapeSphere{Shape::make(EShape::Sphere, sizeof(ShapeSphere), s2p, flags), radius};
			}
			static ShapeSphere make(pr::BSphere const& sph, uint flags = 0)
			{
				return ShapeSphere{Shape::make(EShape::Sphere, sizeof(ShapeSphere), pr::m4x4Identity, flags), sph.Radius()};
			}
		};

		// Traits
		template <> struct shape_traits<ShapeSphere>
		{
			static EShape const eshape = EShape::Sphere;
			static char const* name() { return "ShapeSphere"; }
		};
	}
}
