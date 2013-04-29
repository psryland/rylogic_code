//******************************************************************
// Quad
//  Copyright © Rylogic Ltd 2013
//******************************************************************
// Create a rectangular patch with texture coordinates
//  geometry - the geometry object to return
//  patch_origin      - the coordinate of the left, top edge of the patch
//  patch_dimension   - the overall width/height of the patch
//  patch_divisions   - how many quads to create over the width/height of the patch
//  texture_origin    - the texture coordinate to start with at the top/left corner of the patch
//  texture_dimension - the width/height of the texture on the patch.
//                      If this is less than the dimension of the patch then the texture coords will repeat
//
// The returned patch will look like:
//  0,0 ---------------------> quad_x
//   | +-----+-----+-----+-----+
//   | | 0  /| 2  /| 4  /| 6  /|
//   | |  /  |  /  |  /  |  /  |
//   | |/  1 |/  3 |/  5 |/  7 |
//   | +-----+-----+-----+-----+
//   | | 8  /| 10 /| 12 /| 14 /|
//   | |  /  |  /  |  /  |  /  |
//   | |/  9 |/ 11 |/ 13 |/ 15 |
//   V +-----+-----+-----+-----+
// quad_z
//
// Vertex order:
// 0-----1-----2----3
// |     |     |    |
// 4-----5-----6----7
// |     |     |    |
//
// Face vertex order:
//  0----2,3---
//  |   / |
//  | /   | /
//  1,4---5---
//  |     |

#pragma once
#ifndef PR_GEOMETRY_QUAD_H
#define PR_GEOMETRY_QUAD_H

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and indices needed to hold geometry for a quad/patch
		template <typename Tvr, typename Tir>
		void QuadSize(iv2 const& divisions, Tvr& vcount, Tir& icount)
		{
			vcount = value_cast<Tvr>(1 * (divisions.x + 2) * (divisions.y + 2));
			icount = value_cast<Tir>(6 * (divisions.x + 1) * (divisions.y + 1));
		}

		// Generate an NxM patch of triangles
		// 'origin' is the top/left corner of the patch
		// 'quad_x' is the length and direction of the quad_x axis (see above)
		// 'quad_z' is the length and direction of the quad_z axis (see above)
		// 'divisions' is the number of times to divide the width/height of the quad. NOTE: num_verts_across = divisions.x + 2
		// 'colour' is a colour for the whole quad
		// 'tex_origin' is the texture coordinate of the top/left corner
		// 'tex_dim' is the normalised size of the texture over the quad
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& origin, v4 const& quad_x, v4 const& quad_z, iv2 const& divisions, Colour32 colour, v2 const& tex_origin, v2 const& tex_dim, TVertIter v_out, TIdxIter i_out)
		{
			// Create the vertices
			v4 norm = GetNormal3IfNonZero(Cross3(quad_z,quad_x));
			v4 step_x = quad_x / (divisions.x + 1);
			v4 step_y = quad_z / (divisions.y + 1);
			for (int h = 0, hend = divisions.y + 2; h != hend; ++h)
			{
				v4 vert = origin + float(h) * step_y;
				v2 uv   = v2::make(tex_origin.x, tex_origin.y + tex_dim.y*h/(hend - 1));
				for (int w = 0, wend = divisions.x + 2; w != wend; ++w, vert += step_x, uv.x += tex_dim.x/(wend - 1))
					SetPCNT(*v_out++, vert, colour, norm, uv);
			}

			// Create the faces
			int verts_per_row = divisions.x + 2;
			typedef decltype(impl::remove_ref(*i_out)) VIdx;
			for (int h = 0, hend = divisions.y+1; h != hend; ++h)
			{
				int row = h * verts_per_row;
				for (int w = 0, wend = divisions.x+1; w != wend; ++w)
				{
					int col = row + w;
					*i_out++ = value_cast<VIdx>(col);
					*i_out++ = value_cast<VIdx>(col + verts_per_row);
					*i_out++ = value_cast<VIdx>(col + 1);
					
					*i_out++ = value_cast<VIdx>(col + 1);
					*i_out++ = value_cast<VIdx>(col + verts_per_row);
					*i_out++ = value_cast<VIdx>(col + verts_per_row + 1);
				}
			}

			BoundingBox bbox = BBoxReset;
			pr::Encompase(bbox, origin);
			pr::Encompase(bbox, origin + quad_x + quad_z);

			Props props;
			props.m_bbox = bbox;
			props.m_has_alpha = colour.a() != 0xFF;
			return props;
		}

		// Create a quad with a texture mapped over the whole surface
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& origin, v4 const& quad_x, v4 const& quad_z, iv2 const& divisions, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(origin, quad_x, quad_z, divisions, colour, v2Zero, v2One, v_out, i_out);
		}

		// Create a simple quad, centred on the origin with a normal along the y axis, with a texture mapped over the whole surface
		template <typename TVertIter, typename TIdxIter>
		Props Quad(float width, float height, iv2 const& divisions, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			v4 origin = v4::make(-0.5f * width, 0.f, -0.5f * height, 1.0f);
			v4 quad_x = width  * v4XAxis;
			v4 quad_z = height * v4ZAxis;
			return Quad(origin, quad_x, quad_z, divisions, colour, v_out, i_out);
		}

		// Create a quad centred on an arbitrary position with a normal in the given direction.
		// 'centre' is the mid-point of the quad
		// 'forward' is the normal direction of the quad (not necessarily normalised)
		// 'top' is the up direction of the quad. Can be zero (defaults to -zaxis, then -xaxis), doesn't need to be orthogonal to 'forward'
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions, Colour32 colour, v2 const& tex_origin, v2 const& tex_dim, TVertIter v_out, TIdxIter i_out)
		{
			v4 fwd = !IsZero3(forward) ? forward :  pr::v4YAxis;
			v4 up  = !IsZero3(top)     ? top     : -pr::v4ZAxis;
			if (Parallel(up, fwd)) up = -pr::v4XAxis;

			v4 quad_x = width  * GetNormal3(Cross3(up, fwd));
			v4 quad_z = height * GetNormal3(Cross3(quad_x, fwd));
			v4 origin = centre - 0.5f * quad_x - 0.5f * quad_z;
			return Quad(origin, quad_x, quad_z, divisions, colour, tex_origin, tex_dim, v_out, i_out);
		}
	}
}

#endif
