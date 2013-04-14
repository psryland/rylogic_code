//********************************
// Geometry
//  Copyright © Rylogic Ltd 2013
//********************************
#pragma once
#ifndef PR_GEOMETRY_LINE_H
#define PR_GEOMETRY_LINE_H

#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/maths/maths.h"
#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for an
		// array of 'num_lines' lines. (Lines given as start point, end point pairs)
		template <typename Tvr, typename Tir>
		void LineSize(std::size_t num_lines, pr::Range<Tvr>& vrange, pr::Range<Tir>& irange)
		{
			vrange.set(0, 2 * num_lines);
			irange.set(0, 2 * num_lines);
		}

		// Generate lines from an array of start point, end point pairs.
		// 'num_lines' is the number of start/end point pairs in the following arrays
		// 'points' is the input array of start and end points for lines.
		// 'num_colours' should be either, 0, 1, or num_lines * 2
		// 'colours' is an input array of colour values, a pointer to a single colour, or null.
		// 'out_verts' is an output iterator to receive the [vert,colour] data
		// 'out_indices' is an output iterator to receive the index data
		// 'ibase' is a base index offset to apply to the index data
		template <typename TVertIter, typename TIdxIter>
		Props Lines(std::size_t num_lines, v4 const* points, std::size_t num_colours, Colour32 const* colours, TVertIter out_verts, TIdxIter out_indices, pr::uint16 ibase = 0)
		{
			ColourRepeater col(colours, num_colours, 2*num_lines, Colour32White);

			Props props;
			v4 const* v_in = points;
			TVertIter v_out = out_verts;
			TIdxIter i_out = out_indices;
			for (std::size_t i = 0; i != num_lines; ++i, ibase += 2)
			{
				auto& v0 = *v_in++;
				auto& v1 = *v_in++;
				Colour32 c0 = *col++;
				Colour32 c1 = *col++;

				SetPC(*v_out++, v0, c0);
				SetPC(*v_out++, v1, c1);

				*i_out++ = ibase + 0;
				*i_out++ = ibase + 1;

				// Grow the bounding box
				pr::Encompase(props.m_bbox, v0);
				pr::Encompase(props.m_bbox, v1);
			}
			props.m_has_alpha = col.m_alpha;
			return props;
		}

		// Create lines using a single colour
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		inline Props Lines(std::size_t num_lines ,TVertCIter points ,Colour32 colour ,TVertIter out_verts ,TIdxIter out_indices ,pr::uint16 ibase = 0)
		{
			return Lines(num_lines ,points ,1 ,&colour ,out_verts ,out_indices, ibase);
		}

		// Create lines using collections of points and directions
		template <typename TVertCIter, typename TColCIter, typename TVertIter, typename TIdxIter>
		inline Props LinesD(std::size_t num_lines ,TVertCIter points ,TVertCIter directions ,std::size_t num_colours ,TColCIter colours ,TVertIter out_verts, TIdxIter out_indices ,pr::uint16 ibase = 0)
		{
			std::vector<v4> buf(num_lines * 2);
			auto dst = begin(buf);
			for (std::size_t i = 0; i != num_lines; ++i, ++points, ++directions)
			{
				*dst++ = *points;
				*dst++ = *points+ *directions;
			}
			return Lines(num_lines, &buf[0], num_colours, colours, out_verts, out_indices, ibase);
		}

		// Create lines using collections of points and directions and a single colour
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		inline Props LinesD(std::size_t num_lines ,TVertCIter points ,TVertCIter directions ,Colour32 colour ,TVertIter out_verts, TIdxIter out_indices ,pr::uint16 ibase = 0)
		{
			return LinesD(num_lines ,points ,directions ,1 ,&colour ,out_verts ,out_indices ,ibase);
		}

	}
}

#endif
