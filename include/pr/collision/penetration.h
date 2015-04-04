//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once

#include "pr/maths/maths.h"

namespace pr
{
	// Collision detection penetration helpers
	struct IgnorePenetration
	{
		void operator()(v4 const&, float) {}
	};

	struct MinPenetration
	{
		v4    m_sep_axis;
		float m_penetration;

		MinPenetration()
			:m_sep_axis()
			,m_penetration(maths::float_max)
		{}

		// Add a penetration.
		// 'separating_axis' does not have to be normalised but 'penetration_depth' is
		// assumed to be in multiples of the sep_axis length
		void operator()(v4 const& sep_axis, float penetration_depth)
		{
			auto len = pr::Length3(sep_axis);
			auto pen = penetration_depth / len;
			if (pen >= m_penetration)
				return;

			m_sep_axis = sep_axis / len;
			m_penetration = pen;
		}
	};
}