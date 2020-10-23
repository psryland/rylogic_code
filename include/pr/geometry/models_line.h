//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/geometry/common.h"

namespace pr::geometry
{
	// Returns the number of verts and number of indices needed to hold geometry for an
	// array of 'num_lines' lines. (Lines given as start point, end point pairs)
	constexpr BufSizes LineSize(int num_lines)
	{
		return
		{
			2 * num_lines,
			2 * num_lines,
		};
	}
	constexpr BufSizes LineStripSize(int num_lines)
	{
		return
		{
			1 + num_lines,
			1 + num_lines,
		};
	}

	// Generate lines from an array of start point, end point pairs.
	// 'num_lines' is the number of start/end point pairs in the following arrays
	// 'points' is the input array of start and end points for lines.
	// 'num_colours' should be either, 0, 1, or num_lines * 2
	// 'colours' is an input array of colour values, a pointer to a single colour, or null.
	// 'out_verts' is an output iterator to receive the [vert,colour] data
	// 'out_indices' is an output iterator to receive the index data
	template <typename VOut, typename IOut>
	Props Lines(int num_lines, v4 const* points, int num_colours, Colour32 const* colours, VOut vout, IOut iout)
	{
		Props props;
		props.m_geom = EGeom::Vert | (num_colours ? EGeom::Colr : EGeom::None);

		// Colour iterator
		auto col = CreateRepeater(colours, num_colours, 2*num_lines, Colour32White);
		auto cc = [&](Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

		// Bounding box
		auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

		int index = 0;
		for (int i = 0; i != num_lines; ++i)
		{
			vout(bb(*points++), cc(*col++));
			vout(bb(*points++), cc(*col++));
			iout(index++);
			iout(index++);
		}

		return props;
	}

	// Create lines using collections of points and directions
	template <typename TVertCIter, typename TColCIter, typename VOut, typename IOut>
	inline Props LinesD(int num_lines, TVertCIter points, TVertCIter directions, int num_colours, TColCIter colours, VOut vout, IOut iout)
	{
		Props props;
		props.m_geom = EGeom::Vert | (num_colours ? EGeom::Colr : EGeom::None);

		// Colour iterator
		auto col = CreateRepeater(colours, num_colours, 2*num_lines, Colour32White);
		auto cc = [&](Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

		// Bounding box
		auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

		int index = 0;
		for (int i = 0; i != num_lines; ++i, ++points, ++directions, ++col)
		{
			vout(bb(*points), cc(*col));
			vout(bb(*points + *directions), cc(*col));
			iout(index++);
			iout(index++);
		}

		return props;
	}

	// Create a line strip
	template <typename TVertCIter, typename TColCIter, typename VOut, typename IOut>
	inline Props LinesStrip(int num_lines, TVertCIter points, int num_colours, TColCIter colours, VOut vout, IOut iout)
	{
		Props props;
		props.m_geom = EGeom::Vert | (num_colours ? EGeom::Colr : EGeom::None);

		// Colour iterator
		auto col = CreateLerpRepeater(colours, num_colours, 1+num_lines, Colour32White);
		auto cc = [&](Colour32 c) { props.m_has_alpha |= HasAlpha(c); return c; };

		// Bounding box
		auto bb = [&](v4 const& v) { Grow(props.m_bbox, v); return v; };

		auto index = 0;
		for (int i = 0; i != num_lines + 1; ++i, ++points, ++col)
		{
			vout(bb(*points), cc(*col));
			iout(index++);
		}

		return props;
	}
}
