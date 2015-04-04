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
		struct ShapeBox
		{
			Shape m_base;
			v4    m_radius;

			operator Shape const&() const { return m_base; }
			operator Shape& ()            { return m_base; }

			static ShapeBox	make(v4 const& dim, m4x4 const& s2p = pr::m4x4Identity, uint flags = 0)
			{
				return ShapeBox{Shape::make(EShape::Box, sizeof(ShapeBox), s2p, flags), dim * 0.5f};
			}
			static ShapeBox	make(pr::BBox const& bbox, uint flags = 0)
			{
				return ShapeBox{Shape::make(EShape::Box, sizeof(ShapeBox), pr::m4x4Identity, flags), bbox.m_radius};
			}
			static ShapeBox	make(pr::OBox const& obox, uint flags = 0)
			{
				return ShapeBox{Shape::make(EShape::Box, sizeof(ShapeBox), pr::m4x4Identity, flags), obox.m_radius};
			}
		};

		// Traits
		template <> struct shape_traits<ShapeBox>
		{
			static const EShape eshape = EShape::Box;
			static char const* name() { return "ShapeBox"; }
		};
	}
}
