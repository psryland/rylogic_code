//******************************************************************
// Quad
//  Copyright (c) Rylogic Ltd 2013
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
		// Returns the number of verts and indices needed to hold geometry for a quad
		template <typename Tvr, typename Tir>
		void QuadSize(size_t num_quads, Tvr& vcount, Tir& icount)
		{
			vcount = value_cast<Tvr>(4 * num_quads);
			icount = value_cast<Tir>(6 * num_quads);
		}
		
		// Returns the number of verts and indices needed to hold geometry for a quad strip
		template <typename Tvr, typename Tir>
		void QuadStripSize(size_t num_quads, Tvr& vcount, Tir& icount)
		{
			// A quad plus corner per quad
			vcount = value_cast<Tvr>(4 * num_quads);
			icount = value_cast<Tir>(4 * num_quads);
		}

		// Generate quads from sets of four points
		// Point Order: (bottom to top 'S')
		//  -x, -y = 0
		//  +x, -y = 1
		//  -x, +y = 2
		//  +x, +y = 3
		// 'num_quads' is the number of sets of 4 points pointed to by 'verts'
		// 'verts' is the input array of corner points for the quads
		// 'num_colours' should be either 0, 1, num_quads, or num_quads*4 representing; no colour, 1 colour for all, 1 colour per quad, or 1 colour per quad vertex
		// 't2q' is a tranform to apply to the standard texture coordinates 0,0 -> 1,1
		// 'out_verts' is an output iterator to receive the [vert,norm,colour,tex] data
		// 'out_indices' is an output iterator to receive the index data
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(size_t num_quads, TVertCIter verts, size_t num_colours, Colour32 const* colours, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			// Helper function for generating normals
			auto norm = [](v4 const& a, v4 const& b, v4 const& c) { return Normalise3IfNonZero(Cross3(a - b, c - b)); };

			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, num_quads * 4, Colour32White);

			// Texture coords
			v2 t00 = (t2q * v4::make(0.000f, 0.000f, 0.0f, 1.0f)).xy;
			v2 t01 = (t2q * v4::make(0.000f, 0.999f, 0.0f, 1.0f)).xy;
			v2 t10 = (t2q * v4::make(0.999f, 0.000f, 0.0f, 1.0f)).xy;
			v2 t11 = (t2q * v4::make(0.999f, 0.999f, 0.0f, 1.0f)).xy;

			pr::BBox bbox = pr::BBoxReset;
			for (std::size_t i = 0; i != num_quads; ++i)
			{
				v4 v0 = *verts++;
				v4 v1 = *verts++;
				v4 v2 = *verts++;
				v4 v3 = *verts++;
				Colour32 c0 = *col++;
				Colour32 c1 = *col++;
				Colour32 c2 = *col++;
				Colour32 c3 = *col++;

				// Set verts
				SetPCNT(*v_out++, v0, c0, norm(v1,v0,v2), t01);
				SetPCNT(*v_out++, v1, c1, norm(v3,v1,v0), t11);
				SetPCNT(*v_out++, v2, c2, norm(v0,v2,v3), t00);
				SetPCNT(*v_out++, v3, c3, norm(v2,v3,v1), t10);

				pr::Encompass(bbox, v0);
				pr::Encompass(bbox, v1);
				pr::Encompass(bbox, v2);
				pr::Encompass(bbox, v3);

				// Set faces
				std::size_t ibase = i * 6;
				typedef decltype(impl::remove_ref(*i_out)) VIdx;
				*i_out++ = value_cast<VIdx>(ibase);
				*i_out++ = value_cast<VIdx>(ibase + 1);
				*i_out++ = value_cast<VIdx>(ibase + 2);
				*i_out++ = value_cast<VIdx>(ibase + 2);
				*i_out++ = value_cast<VIdx>(ibase + 1);
				*i_out++ = value_cast<VIdx>(ibase + 3);
			}

			Props props;
			props.m_geom = EGeom::Vert | (colours != 0 ? EGeom::Colr : 0) | EGeom::Norm | EGeom::Tex0;
			props.m_bbox = bbox;
			props.m_has_alpha = col.m_alpha;
			return props;
		}

		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(size_t num_quads, TVertCIter verts, size_t num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(num_quads, verts, num_colours, colours, m4x4Identity, v_out, i_out);
		}

		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(size_t num_quads, TVertCIter verts, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(num_quads, verts, 0, 0, m4x4Identity, v_out, i_out);
		}

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
		// 't2q' is a tranform to apply to the standard texture coordinates 0,0 -> 1,1
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& origin, v4 const& quad_x, v4 const& quad_z, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			// Create the vertices
			v4 norm = Normalise3IfNonZero(Cross3(quad_z,quad_x));
			v4 step_x = quad_x / (divisions.x + 1);
			v4 step_y = quad_z / (divisions.y + 1);
			v2 uvbase = (t2q * v4Origin).xy;
			v2 du     = (t2q * v4XAxis).xy;
			v2 dv     = (t2q * v4YAxis).xy;
			for (int h = 0, hend = divisions.y + 2; h != hend; ++h, uvbase += dv)
			{
				v4 vert = origin + float(h) * step_y;
				v2 uv   = uvbase;
				for (int w = 0, wend = divisions.x + 2; w != wend; ++w, vert += step_x, uv += du)
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

			Props props;
			props.m_geom = EGeom::Vert|EGeom::Colr|EGeom::Norm|EGeom::Tex0;
			props.m_bbox = pr::BBoxMake({origin, origin + quad_x + quad_z});
			props.m_has_alpha = colour.a() != 0xFF;
			return props;
		}

		// Create a quad with a texture mapped over the whole surface
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& origin, v4 const& quad_x, v4 const& quad_z, iv2 const& divisions, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(origin, quad_x, quad_z, divisions, colour, m4x4Identity, v_out, i_out);
		}

		// Create a simple quad, centred on the origin with a normal along the y axis, with a texture mapped over the whole surface
		template <typename TVertIter, typename TIdxIter>
		Props Quad(float width, float height, iv2 const& divisions, Colour32 colour, TVertIter v_out, TIdxIter i_out)
		{
			v4 origin = v4::make(-0.5f * width, 0.f, -0.5f * height, 1.0f);
			v4 quad_x = width  * v4XAxis;
			v4 quad_z = height * v4ZAxis;
			return Quad(origin, quad_x, quad_z, divisions, colour, m4x4Identity, v_out, i_out);
		}

		// Create a quad centred on an arbitrary position with a normal in the given direction.
		// 'centre' is the mid-point of the quad
		// 'forward' is the normal direction of the quad (not necessarily normalised)
		// 'top' is the up direction of the quad. Can be zero (defaults to -zaxis, then -xaxis), doesn't need to be orthogonal to 'forward'
		// 't2q' is a tranform to apply to the standard texture coordinates 0,0 -> 1,1
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			v4 fwd = !IsZero3(forward) ? forward :  pr::v4YAxis;
			v4 up  = !IsZero3(top)     ? top     : -pr::v4ZAxis;
			if (Parallel(up, fwd)) up = -pr::v4XAxis;

			v4 quad_x = width  * Normalise3(Cross3(up, fwd));
			v4 quad_z = height * Normalise3(Cross3(quad_x, fwd));
			v4 origin = centre - 0.5f * quad_x - 0.5f * quad_z;
			return Quad(origin, quad_x, quad_z, divisions, colour, t2q, v_out, i_out);
		}

		// Generate a strip of quads centred on a line of verts.
		// 'num_quads' is the number quads in the strip (num_quads == num_verts - 1)
		// 'verts' is the input array of line verts
		// 'width' is the tranverse width of the quad strip (not half width)
		// 'normal' is the normal of the first vertex. After that, normals on same side are used
		// 'num_colours' should be either 0, 1, num_quads+1 representing; no colour, 1 colour for all, 1 colour per vertex pair
		// 'v_out' is an output iterator to receive the [vert,colour,norm,tex] data
		// 'i_out' is an output iterator to receive the index data
		template <typename TVertCIter, typename TNormCIter, typename TVertIter, typename TIdxIter>
		Props QuadStrip(size_t num_quads, TVertCIter verts, float width, size_t num_normals, TNormCIter normals, size_t num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			typedef decltype(impl::remove_ref(*i_out)) VIdx;

			if (num_quads < 1) return Props();
			auto num_verts = num_quads + 1;

			// Colour iterator wrapper
			ColourRepeater col(colours, num_colours, num_verts, Colour32White);

			// Normal iterator wrapper
			auto norm = pr::CreateRepeater(normals, num_normals, num_verts, pr::v4ZAxis);

			// Texture coords (note: 1D texture)
			v2 const t00 = v2::make(0.000f, 0.000f);
			v2 const t10 = v2::make(0.999f, 0.000f);
			
			pr::BBox bbox = pr::BBoxReset;
			auto bb = [&](v4 const& v) { pr::Encompass(bbox, v); return v; };

			VIdx index = 0;
			auto hwidth = width * 0.5f;
			v4       v0, v1 = *verts++, v2 = *verts++;
			v4       n0, n1 = *norm++ , n2 = *norm++ ;
			Colour32 c0, c1 = *col++  , c2 = *col++  ;

			// Create the first pair of verts
			v4 bi = pr::Normalise3(pr::Cross3(n1, v2 - v1), pr::Perpendicular(n1));
			SetPCNT(*v_out++, bb(v1 + bi*hwidth), c1, n1, t00); *i_out++ = index++;
			SetPCNT(*v_out++, bb(v1 - bi*hwidth), c1, n1, t10); *i_out++ = index++;

			for (size_t i = 0; i != num_quads - 1; ++i)
			{
				v0 = v1; v1 = v2; v2 = *verts++;
				n0 = n1; n1 = n2; n2 = *norm++;
				c0 = c1; c1 = c2; c2 = *col++;

				auto d0 = v1 - v0;
				auto d1 = v2 - v1;
				auto b0 = pr::Normalise3(pr::Cross3(n1, d0), pr::Perpendicular(n1));
				auto b1 = pr::Normalise3(pr::Cross3(n1, d1), pr::Perpendicular(n1));
				bi = pr::Normalise3(b0 + b1, bi); // The bisector at v1
				// Note: bi always points to the left of d0 and d1
			
				// Find the distance, t, along d0 to the inside corner vert
				// let t = 1 - u, where u is the distance back along d0 from v1
				// x = dot(d0,bi)/|d0| = the length of bi along d0
				// y = dot(b0,bi)      = the perpendicular distance of bi from d0
				// let w = x/|d0| = dot(d0,bi)/|d0|² => x = w*|d0|
				// x/y = |d0|/Y = similar triangles
				//   => Y = |d0|*y/x = y/w = |d0|²*dot(b0,bi)/dot(d0,bi)
				// for u >= 1; Y <= hwidth
				//   => y/w <= hwidth
				//   => y <= hwidth*w
				// u = 1 - t = X/|d0| = parametric value back from v1 where the perpendicular distance is hwidth
				//   X/hwidth = x/y => X = hwidth*w*|d0|/y
				//   => X/|d0| = hwidth*w/y
				//   => t = 1 - hwidth*w/y
				auto d0_sq = Length3Sq(d0);
				auto d1_sq = Length3Sq(d1);
				auto w0 = Abs(Dot3(d0,bi)) / d0_sq;
				auto w1 = Abs(Dot3(d1,bi)) / d1_sq;
				auto y = Dot3(b0,bi); // == Dot3(b1,bi);
				auto u0 = y <= hwidth*w0 ? 1.0f : hwidth*w0/y; // Cannot be a div/0 because w0,w1 are positive-semi-definite.
				auto u1 = y <= hwidth*w1 ? 1.0f : hwidth*w1/y;
				if (Dot3(d0,bi) >= 0.0f) // line turns to the right
				{
					auto inner = u0*pr::Sqrt(d0_sq) > u1*pr::Sqrt(d1_sq) // Pick the maximum distance from v1
						? v1 - u0*d0 - b0*hwidth
						: v1 + u1*d1 - b1*hwidth;

					// Finish the previous quad
					SetPCNT(*v_out++, bb(v1 + b0*hwidth), c1, n1, t00); *i_out++ = index++;
					SetPCNT(*v_out++, bb(inner)         , c1, n1, t10); *i_out++ = index++;

					// Start the next quad
					SetPCNT(*v_out++, bb(v1 + b1*hwidth), c1, n1, t00); *i_out++ = index++;
					SetPCNT(*v_out++, bb(inner)         , c1, n1, t10); *i_out++ = index++;
				}
				else // line turns to the left
				{
					auto inner = u0*pr::Sqrt(d0_sq) > u1*pr::Sqrt(d1_sq) // Pick the maximum distance from v1
						? v1 - u0*d0 + b0*hwidth
						: v1 + u1*d1 + b1*hwidth;

					// Finish the previous quad
					SetPCNT(*v_out++, bb(inner)         , c1, n1, t10); *i_out++ = index++;
					SetPCNT(*v_out++, bb(v1 - b0*hwidth), c1, n1, t00); *i_out++ = index++;
					
					// Start the next quad
					SetPCNT(*v_out++, bb(inner)         , c1, n1, t10); *i_out++ = index++;
					SetPCNT(*v_out++, bb(v1 - b1*hwidth), c1, n1, t00); *i_out++ = index++;
				}
			}

			// Finish the previous quad
			bi = pr::Normalise3(pr::Cross3(n2, v2 - v1), pr::Perpendicular(n2));
			SetPCNT(*v_out++, bb(v2 + bi*hwidth), c2, n2, t00); *i_out++ = index++;
			SetPCNT(*v_out++, bb(v2 - bi*hwidth), c2, n2, t10); *i_out++ = index++;

			Props props;
			props.m_geom = EGeom::Vert | (colours != 0 ? EGeom::Colr : 0) | EGeom::Norm | EGeom::Tex0;
			props.m_bbox = bbox;
			props.m_has_alpha = col.m_alpha;
			return props;
		}
	}
}

#endif
