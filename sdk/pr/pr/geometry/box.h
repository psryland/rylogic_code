//********************************
// Line geometry
//  Copyright � Rylogic Ltd 2006
//********************************
#pragma once
#ifndef PR_GEOMETRY_BOX_H
#define PR_GEOMETRY_BOX_H

#include "pr/common/colour.h"
#include "pr/common/range.h"
#include "pr/maths/maths.h"
#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and number of indices needed to hold geometry for an
		// array of 'num_boxes' boxes.
		template <typename Tvr, typename Tir>
		void BoxSize(std::size_t num_boxes, pr::Range<Tvr>& vrange, pr::Range<Tir>& irange)
		{
			vrange.set(0, 24 * num_boxes);
			irange.set(0, 36 * num_boxes);
		}

		// Generate boxes from an array of corners
		// Point Order:
		//  -x, -y, -z  = 0
		//  +x, -y, -z  = 1
		//  -x, +y, -z  = 2
		//  +x, +y, -z  = 3
		//  -x, -y, +z  = 4
		//  +x, -y, +z  = 5
		//  -x, +y, +z  = 6
		//  +x, +y, +z  = 7
		// 'num_boxes' is the number of boxes given in the 'points' array. (Should be 8 * its length)
		// 'points' is the input array of corner points for the boxes
		// 'num_colours' should be either, 0, 1, num_boxes, num_boxes*8 representing; no colour, 1 colour for all, 1 colour per box, or 1 colour per box vertex
		// 'colours' is an input array of colour values, a pointer to a single colour, or null.
		// 'out_verts' is an output iterator to receive the [vert,norm,colour,tex] data
		// 'out_indices' is an output iterator to receive the index data
		// 'ibase' is a base index offset to apply to the index data
		// The order of faces is +X,-X,+Y,-Y,+Z,-Z
		// The normals are outward facing
		// The texture coordinates set on the box have the 'walls' with Y as up.
		// On top (-x,+y,-z) is the top left corner, on the bottom (-x,-y,+z) is the top left corner
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Boxes(std::size_t num_boxes, TVertCIter points, std::size_t num_colours, Colour32 const* colours, TVertIter out_verts, TIdxIter out_indices, pr::uint16 ibase = 0)
		{
			int const vidx[] = 
			{
				7,5,1,3, // +X
				2,0,4,6, // -X
				2,6,7,3, // +Y
				4,0,1,5, // -Y
				6,4,5,7, // +Z
				3,1,0,2, // -Z
			};
			std::size_t vcount = sizeof(vidx)/sizeof(vidx[0]);
			pr::uint16 const indices[] =
			{
				0, 1, 2,  0, 2, 3,  //  0 -  6
				4, 5, 6,  4, 6, 7,  //  6 - 12
				8, 9,10,  8,10,11,  // 12 - 18
				12,13,14, 12,14,15, // 18 - 24
				16,17,18, 16,18,19, // 24 - 30
				20,21,22, 20,22,23, // 30 - 36
			};
			std::size_t const icount = sizeof(indices)/sizeof(indices[0]);

			// Helper function for generating normals
			auto norm = [](v4 const& a, v4 const& b, v4 const& c) { return GetNormal3IfNonZero(Cross3(c - b, a - b)); };

			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, 8*num_boxes, Colour32White);

			// Texture coords
			v2 t00 = v2::make(0.0f, 0.0f);
			v2 t01 = v2::make(0.0f, 1.0f);
			v2 t10 = v2::make(1.0f, 0.0f);
			v2 t11 = v2::make(1.0f, 1.0f);
			
			Props props;
			TVertCIter v_in = points;
			TVertIter v_out = out_verts;
			TIdxIter i_out = out_indices;
			for (std::size_t i = 0; i != num_boxes; ++i, ibase += 24)
			{
				// Read 8 points from the vertex and colour streams
				struct { v4 pt; Colour32 cl; } vert[8];
				for (std::size_t j = 0; j != 8; ++j)
				{
					vert[j].pt = *v_in; ++v_in;
					vert[j].cl = *col;  ++col;
					pr::Encompase(props.m_bbox, vert[j].pt);
				}

				// Set the verts
				int const* vi = vidx;
				for (std::size_t j = 0; j != vcount/4; ++j)
				{
					auto& a = vert[*vi++];
					auto& b = vert[*vi++];
					auto& c = vert[*vi++];
					auto& d = vert[*vi++];
					SetPCNT(*v_out++, a.pt, a.cl, norm(d.pt, a.pt, b.pt), t00);
					SetPCNT(*v_out++, b.pt, b.cl, norm(a.pt, b.pt, c.pt), t01);
					SetPCNT(*v_out++, c.pt, c.cl, norm(b.pt, c.pt, d.pt), t11);
					SetPCNT(*v_out++, d.pt, d.cl, norm(c.pt, d.pt, a.pt), t10);
				}

				// Set the faces
				pr::uint16 const* ii = indices;
				for (std::size_t j = 0; j != icount/6; ++j)
				{
					*i_out++ = ibase + *ii++;
					*i_out++ = ibase + *ii++;
					*i_out++ = ibase + *ii++;
					*i_out++ = ibase + *ii++;
					*i_out++ = ibase + *ii++;
					*i_out++ = ibase + *ii++;
				}
			}
			props.m_has_alpha = col.m_alpha;
			return props;
		}

		// Create a transformed box
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Boxes(std::size_t num_boxes, TVertCIter points, m4x4 const& o2w, std::size_t num_colours, Colour32 const* colours, TVertIter out_verts, TIdxIter out_indices, pr::uint16 ibase = 0)
		{
			if (o2w == m4x4Identity)
				return Boxes(num_boxes, points, num_colours, colours, out_verts, out_indices, ibase);

			// An iterator wrapper for applying a transform to 'points'
			Transformer<TVertCIter> tx(points, o2w);
			return Boxes(num_boxes, tx, num_colours, colours, out_verts, out_indices, ibase);
		}

		// Create a box with side half lengths = rad.x,rad.y,rad.z
		template <typename TVertIter, typename TIdxIter>
		Props Box(v4 const& rad, m4x4 const& o2w, Colour32 colour, TVertIter out_verts, TIdxIter out_indices, pr::uint16 ibase = 0)
		{
			v4 const pt[8] = 
				{
					{-rad.x, -rad.y, -rad.z, 1.0f},
					{+rad.x, -rad.y, -rad.z, 1.0f},
					{-rad.x, +rad.y, -rad.z, 1.0f},
					{+rad.x, +rad.y, -rad.z, 1.0f},
					{-rad.x, -rad.y,  rad.z, 1.0f},
					{+rad.x, -rad.y,  rad.z, 1.0f},
					{-rad.x, +rad.y,  rad.z, 1.0f},
					{+rad.x, +rad.y,  rad.z, 1.0f},
				};
			return Boxes(1, &pt[0], o2w, 1, &colour, out_verts, out_indices, ibase);
		}

		// Create boxes at each point in 'positions' with dimensions 'dim'
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props BoxList(std::size_t num_boxes, TVertCIter positions, v4 const& dim, std::size_t num_colours, Colour32 const* colours, TVertIter out_verts, TIdxIter out_indices, pr::uint16 ibase = 0)
		{
			TVertCIter pos = positions;
			std::vector<v4> points(8*num_boxes);
			v4* pt = &points[0];
			for (std::size_t i = 0; i != num_boxes; ++i, ++pos)
			{
				pt->set(pos->x - dim.x, pos->y - dim.y, pos->z - dim.z, 1.0f), ++pt;
				pt->set(pos->x + dim.x, pos->y - dim.y, pos->z - dim.z, 1.0f), ++pt;
				pt->set(pos->x - dim.x, pos->y + dim.y, pos->z - dim.z, 1.0f), ++pt;
				pt->set(pos->x + dim.x, pos->y + dim.y, pos->z - dim.z, 1.0f), ++pt;
				pt->set(pos->x - dim.x, pos->y - dim.y, pos->z + dim.z, 1.0f), ++pt;
				pt->set(pos->x + dim.x, pos->y - dim.y, pos->z + dim.z, 1.0f), ++pt;
				pt->set(pos->x - dim.x, pos->y + dim.y, pos->z + dim.z, 1.0f), ++pt;
				pt->set(pos->x + dim.x, pos->y + dim.y, pos->z + dim.z, 1.0f), ++pt;
			}
			return Boxes(num_boxes, &points[0], num_colours, colours, out_verts, out_indices, ibase);
		}
	}
}

#endif