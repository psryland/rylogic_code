//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for an
		// array of 'num_lines' lines. (Lines given as start point, end point pairs)
		template <typename Tvr, typename Tir>
		void LineSize(std::size_t num_lines, Tvr& vcount, Tir& icount)
		{
			vcount = checked_cast<Tvr>(2 * num_lines);
			icount = checked_cast<Tir>(2 * num_lines);
		}
		template <typename Tvr, typename Tir>
		void LineStripSize(std::size_t num_lines, Tvr& vcount, Tir& icount)
		{
			vcount = checked_cast<Tvr>(1 + num_lines);
			icount = checked_cast<Tir>(1 + num_lines);
		}

		// Generate lines from an array of start point, end point pairs.
		// 'num_lines' is the number of start/end point pairs in the following arrays
		// 'points' is the input array of start and end points for lines.
		// 'num_colours' should be either, 0, 1, or num_lines * 2
		// 'colours' is an input array of colour values, a pointer to a single colour, or null.
		// 'out_verts' is an output iterator to receive the [vert,colour] data
		// 'out_indices' is an output iterator to receive the index data
		template <typename TVertIter, typename TIdxIter>
		Props Lines(std::size_t num_lines, v4 const* points, std::size_t num_colours, Colour32 const* colours, TVertIter out_verts, TIdxIter out_indices)
		{
			typedef decltype(impl::remove_ref(*out_indices)) VIdx;
			Props props;
			props.m_geom = EGeom::Vert | (num_colours != 0 ? EGeom::Colr : 0);

			// Colour iterator
			auto col = pr::CreateRepeater(colours, num_colours, 2*num_lines, Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a != 0xff; return c; };

			// Bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			v4 const* v_in  = points;
			TVertIter v_out = out_verts;
			TIdxIter  i_out = out_indices;
			VIdx index = 0;

			for (std::size_t i = 0; i != num_lines; ++i)
			{
				SetPC(*v_out++, bb(*v_in++), cc(*col++)); *i_out++ = index++;
				SetPC(*v_out++, bb(*v_in++), cc(*col++)); *i_out++ = index++;
			}

			return props;
		}

		// Create lines using collections of points and directions
		template <typename TVertCIter, typename TColCIter, typename TVertIter, typename TIdxIter>
		inline Props LinesD(std::size_t num_lines, TVertCIter points, TVertCIter directions, std::size_t num_colours, TColCIter colours, TVertIter out_verts, TIdxIter out_indices)
		{
			std::vector<v4> buf(num_lines * 2);
			auto dst = begin(buf);
			for (std::size_t i = 0; i != num_lines; ++i, ++points, ++directions)
			{
				*dst++ = *points;
				*dst++ = *points + *directions;
			}
			return Lines(num_lines, &buf[0], num_colours, colours, out_verts, out_indices);
		}

		// Create a line strip
		template <typename TVertCIter, typename TColCIter, typename TVertIter, typename TIdxIter>
		inline Props LinesStrip(std::size_t num_lines, TVertCIter points, std::size_t num_colours, TColCIter colours, TVertIter out_verts, TIdxIter out_indices)
		{
			typedef decltype(impl::remove_ref(*out_indices)) VIdx;
			Props props;
			props.m_geom = EGeom::Vert | (num_colours != 0 ? EGeom::Colr : 0);

			// Colour iterator
			auto col = pr::CreateLerpRepeater(colours, num_colours, 1+num_lines, Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a != 0xff; return c; };

			// Bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			auto v_in  = points;
			auto v_out = out_verts;
			auto i_out = out_indices;
			VIdx index = 0;

			for (std::size_t i = 0; i != num_lines + 1; ++i)
			{
				SetPC(*v_out++, bb(*v_in++), cc(*col++));
				*i_out++ = index++;
			}

			return props;
		}
	}
}
