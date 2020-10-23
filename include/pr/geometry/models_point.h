//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once
#include "pr/geometry/common.h"

namespace pr::geometry
{
	// Returns the number of verts and number of indices needed to hold geometry for an array of 'num_point' points.
	constexpr BufSizes PointSize(int num_points)
	{
		return
		{
			num_points,
			num_points,
		};
	}

	// Generate points from an array of points.
	// 'num_points' is the number of point in the following arrays
	// 'points' is the input array of points.
	// 'num_colours' should be either, 0, 1, or num_points
	// 'colours' is an input array of colour values, a pointer to a single colour, or null.
	// 'out_verts' is an output iterator to receive the [vert,colour] data
	// 'out_indices' is an output iterator to receive the index data
	template <typename VOut, typename IOut>
	Props Points(int num_points, v4 const* points, int num_colours, Colour32 const* colours, VOut vout, IOut iout)
	{
		Props props;
		props.m_geom = EGeom::Vert | (num_colours ? EGeom::Colr : EGeom::None) | EGeom::Tex0; // UVs are added in the geometry shader

		// Colour iterator
		auto col = CreateRepeater(colours, num_colours, num_points, Colour32White);
		auto cc = [&](Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

		// Bounding box
		auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

		v4 const* v_in = points; int index = 0;
		for (int i = 0; i != num_points; ++i)
		{
			vout(bb(*v_in++), cc(*col++));
			iout(index++);
		}

		return props;
	}
}
