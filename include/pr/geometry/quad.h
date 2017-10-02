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
//  0,0 ---------------------> quad_w
//   | +-----+-----+-----+-----+
//   | | 0  /| 2  /| 4  /| 6  /|
//   | |  /  |  /  |  /  |  /  |
//   | |/  1 |/  3 |/  5 |/  7 |
//   | +-----+-----+-----+-----+
//   | | 8  /| 10 /| 12 /| 14 /|
//   | |  /  |  /  |  /  |  /  |
//   | |/  9 |/ 11 |/ 13 |/ 15 |
//   V +-----+-----+-----+-----+
// quad_h
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

#include "pr/geometry/common.h"

namespace pr
{
	namespace geometry
	{
		// Returns the number of verts and indices needed to hold geometry for a quad
		template <typename Tvr, typename Tir>
		void QuadSize(int num_quads, Tvr& vcount, Tir& icount)
		{
			vcount = checked_cast<Tvr>(4 * num_quads);
			icount = checked_cast<Tir>(6 * num_quads);
		}

		// Returns the number of verts and indices needed to hold geometry for a quad/patch
		template <typename Tvr, typename Tir>
		void QuadSize(iv2 const& divisions, Tvr& vcount, Tir& icount)
		{
			vcount = checked_cast<Tvr>(1 * (divisions.x + 2) * (divisions.y + 2));
			icount = checked_cast<Tir>(6 * (divisions.x + 1) * (divisions.y + 1));
		}

		// Returns the number of verts and indices needed to hold geometry for a quad strip
		template <typename Tvr, typename Tir>
		void QuadStripSize(int num_quads, Tvr& vcount, Tir& icount)
		{
			// A quad plus corner per quad
			vcount = checked_cast<Tvr>(4 * num_quads);
			icount = checked_cast<Tir>(4 * num_quads);
		}

		// Generate quads from sets of four points
		// Point Order: (bottom to top 'S')
		//  -x, -y = 0  uv = 00
		//  +x, -y = 1  uv = 10
		//  -x, +y = 2  uv = 01
		//  +x, +y = 3  uv = 11
		// 'num_quads' is the number of sets of 4 points pointed to by 'verts'
		// 'verts' is the input array of corner points for the quads
		// 'num_colours' should be either 0, 1, num_quads, or num_quads*4 representing; no colour, 1 colour for all, 1 colour per quad, or 1 colour per quad vertex
		// 't2q' is a transform to apply to the standard texture coordinates 0,0 -> 1,1
		// 'out_verts' is an output iterator to receive the [vert,norm,colour,tex] data
		// 'out_indices' is an output iterator to receive the index data
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(int num_quads, TVertCIter verts, int num_colours, Colour32 const* colours, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			using VIdx = typename std::remove_reference<decltype(*i_out)>::type;

			Props props;
			props.m_geom = EGeom::Vert | (colours ? EGeom::Colr : EGeom::None) | EGeom::Norm | EGeom::Tex0;

			// Helper function for generating normals
			auto norm = [](v4 const& a, v4 const& b, v4 const& c) { return Normalise3(Cross3(a - b, c - b), v4Zero); };

			// Colour iterator wrapper
			auto col = pr::CreateRepeater(colours, num_colours, num_quads * 4, Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a != 0xff; return c; };

			// Bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			// Texture coords
			auto t00 = (t2q * v4(0.0f, 0.0f, 0.0f, 1.0f)).xy;
			auto t10 = (t2q * v4(1.0f, 0.0f, 0.0f, 1.0f)).xy;
			auto t01 = (t2q * v4(0.0f, 1.0f, 0.0f, 1.0f)).xy;
			auto t11 = (t2q * v4(1.0f, 1.0f, 0.0f, 1.0f)).xy;

			for (int i = 0; i != num_quads; ++i)
			{
				auto v0 = *verts++;
				auto v1 = *verts++;
				auto v2 = *verts++;
				auto v3 = *verts++;
				auto c0 = *col++;
				auto c1 = *col++;
				auto c2 = *col++;
				auto c3 = *col++;

				// Set verts
				SetPCNT(*v_out++, bb(v0), cc(c0), norm(v1,v0,v2), t00);
				SetPCNT(*v_out++, bb(v1), cc(c1), norm(v3,v1,v0), t10);
				SetPCNT(*v_out++, bb(v2), cc(c2), norm(v0,v2,v3), t01);
				SetPCNT(*v_out++, bb(v3), cc(c3), norm(v2,v3,v1), t11);

				// Set faces
				auto ibase = i * 4;
				*i_out++ = checked_cast<VIdx>(ibase);
				*i_out++ = checked_cast<VIdx>(ibase + 1);
				*i_out++ = checked_cast<VIdx>(ibase + 2);
				*i_out++ = checked_cast<VIdx>(ibase + 2);
				*i_out++ = checked_cast<VIdx>(ibase + 1);
				*i_out++ = checked_cast<VIdx>(ibase + 3);
			}

			return props;
		}
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(int num_quads, TVertCIter verts, int num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(num_quads, verts, num_colours, colours, m4x4Identity, v_out, i_out);
		}
		template <typename TVertCIter, typename TVertIter, typename TIdxIter>
		Props Quad(int num_quads, TVertCIter verts, TVertIter v_out, TIdxIter i_out)
		{
			return Quad(num_quads, verts, 0, 0, m4x4Identity, v_out, i_out);
		}

		// Generate an NxM patch of triangles
		// 'anchor' is the origin of the quad, (0,0) = centre, (-1,-1) = left,bottom, (+1,+1) = right,top, etc
		// 'quad_w' is the length and direction of the quad_w axis
		// 'quad_h' is the length and direction of the quad_h axis
		// 'divisions' is the number of times to divide the width/height of the quad. Note: num_verts_across = divisions.x + 2
		// 'colour' is a colour for the whole quad
		// 't2q' is a transform to apply to the standard texture coordinates 0,0 -> 1,1
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			using VIdx = typename std::remove_reference<decltype(*i_out)>::type;

			// Set the start point so that the model origin matches 'anchor'
			auto origin = v4Origin
				- 0.5f * (1.0f + anchor.x) * quad_w
				- 0.5f * (1.0f + anchor.y) * quad_h;
			auto norm = Normalise3(Cross3(quad_w, quad_h));
			auto step_x = quad_w / float(divisions.x + 1);
			auto step_y = quad_h / float(divisions.y + 1);

			// Texture coordinates
			auto uvbase = (t2q * v4Origin).xy;
			auto du     = (t2q * v4XAxis).xy;
			auto dv     = (t2q * v4YAxis).xy;

			// Create the vertices
			for (int h = 0, hend = divisions.y+2; h != hend; ++h, uvbase += dv)
			{
				auto uv = uvbase;
				auto vert = origin + float(h) * step_y;
				for (int w = 0, wend = divisions.x+2; w != wend; ++w, vert += step_x, uv += du)
					SetPCNT(*v_out++, vert, colour, norm, uv);
			}

			// Create the faces
			auto verts_per_row = divisions.x + 2;
			for (int h = 0, hend = divisions.y+1; h != hend; ++h)
			{
				auto row = h * verts_per_row;
				for (int w = 0, wend = divisions.x+1; w != wend; ++w)
				{
					auto col = row + w;
					*i_out++ = checked_cast<VIdx>(col);
					*i_out++ = checked_cast<VIdx>(col + 1);
					*i_out++ = checked_cast<VIdx>(col + verts_per_row);

					*i_out++ = checked_cast<VIdx>(col + verts_per_row);
					*i_out++ = checked_cast<VIdx>(col + 1);
					*i_out++ = checked_cast<VIdx>(col + 1 + verts_per_row);
				}
			}

			Props props;
			props.m_geom = EGeom::Vert|EGeom::Colr|EGeom::Norm|EGeom::Tex0;
			props.m_bbox = BBox::Make(origin, Abs(origin + quad_w + quad_h));
			props.m_has_alpha = colour.a != 0xFF;
			return props;
		}

		// Create a simple quad, with a normal along 'axis_id', with a texture mapped over the whole surface
		template <typename TVertIter, typename TIdxIter>
		Props Quad(AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			// X => Y = width, Z = Height
			// Y => Z = width, X = Height
			// Z => X = width, Y = Height
			v4 quad_w, quad_h;
			switch (axis_id) {
			case AxisId::PosX: quad_w = +width * v4YAxis; quad_h = +height * v4ZAxis; break;
			case AxisId::PosY: quad_w = +width * v4ZAxis; quad_h = +height * v4XAxis; break;
			case AxisId::PosZ: quad_w = +width * v4XAxis; quad_h = +height * v4YAxis; break;
			case AxisId::NegX: quad_w = -width * v4YAxis; quad_h = -height * v4ZAxis; break;
			case AxisId::NegY: quad_w = -width * v4ZAxis; quad_h = -height * v4XAxis; break;
			case AxisId::NegZ: quad_w = -width * v4XAxis; quad_h = -height * v4YAxis; break;
			}
			return Quad(anchor, quad_w, quad_h, divisions, colour, t2q, v_out, i_out);
		}

		// Create a quad centred on an arbitrary position with a normal in the given direction.
		// 'centre' is the mid-point of the quad
		// 'forward' is the normal direction of the quad (not necessarily normalised)
		// 'top' is the up direction of the quad. Can be zero (defaults to -ZAxis, then -XAxis), doesn't need to be orthogonal to 'forward'
		// 't2q' is a transform to apply to the standard texture coordinates 0,0 -> 1,1
		template <typename TVertIter, typename TIdxIter>
		Props Quad(v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, TVertIter v_out, TIdxIter i_out)
		{
			auto fwd = !IsZero3(forward) ? forward :  pr::v4YAxis;
			auto up  = !IsZero3(top)     ? top     : -pr::v4ZAxis;
			if (Parallel(up, fwd))
				up = -pr::v4XAxis;

			auto quad_w = width  * Normalise3(Cross3(up, fwd));
			auto quad_h = height * Normalise3(Cross3(fwd, quad_w));
			auto origin = centre - 0.5f * quad_w - 0.5f * quad_h;
			return Quad(origin, quad_w, quad_h, divisions, colour, t2q, v_out, i_out);
		}

		// Generate a strip of quads centred on a line of verts.
		// 'num_quads' is the number quads in the strip (num_quads == num_verts - 1)
		// 'verts' is the input array of line verts
		// 'width' is the transverse width of the quad strip (not half width)
		// 'num_normals' should be either 0, 1, num_quads+1 representing; no norm, 1 norm for all, 1 norm per vertex pair
		// 'normals' is the normal of the first vertex. After that, normals on same side are used
		// 'num_colours' should be either 0, 1, num_quads+1 representing; no colour, 1 colour for all, 1 colour per vertex pair
		// 'v_out' is an output iterator to receive the [vert,colour,norm,tex] data
		// 'i_out' is an output iterator to receive the index data
		template <typename TVertCIter, typename TNormCIter, typename TVertIter, typename TIdxIter>
		Props QuadStrip(int num_quads, TVertCIter verts, float width, int num_normals, TNormCIter normals, int num_colours, Colour32 const* colours, TVertIter v_out, TIdxIter i_out)
		{
			using VIdx = typename std::remove_reference<decltype(*i_out)>::type;

			Props props;
			props.m_geom = EGeom::Vert | (colours ? EGeom::Colr : EGeom::None) | EGeom::Norm | EGeom::Tex0;

			if (num_quads < 1) return Props();
			auto num_verts = num_quads + 1;

			// Colour iterator wrapper
			auto col = pr::CreateLerpRepeater(colours, num_colours, num_verts, Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a != 0xff; return c; };

			// Normal iterator wrapper
			auto norm = pr::CreateLerpRepeater(normals, num_normals, num_verts, pr::v4ZAxis);

			// Bounding box
			auto lwr = +v4Max;
			auto upr = -v4Max;
			auto bb = [&](v4 const& v) { lwr = Min(lwr,v); upr = Max(upr,v); return v; };

			// Texture coords (note: 1D texture)
			auto t00 = v2(0.0f, 0.0f);
			auto t10 = v2(1.0f, 0.0f);

			VIdx index = 0;
			auto hwidth = width * 0.5f;
			v4       v0, v1 = *verts++, v2 = *verts++;
			v4       n0, n1 = *norm++ , n2 = *norm++ ;
			Colour32 c0, c1 = *col++  , c2 = *col++  ;

			// Create the first pair of verts
			v4 bi = Normalise3(Cross3(n1, v2 - v1), pr::Perpendicular(n1));
			SetPCNT(*v_out++, bb(v1 + bi*hwidth), cc(c1), n1, t00); *i_out++ = index++;
			SetPCNT(*v_out++, bb(v1 - bi*hwidth), cc(c1), n1, t10); *i_out++ = index++;

			for (int i = 0; i != num_quads - 1; ++i)
			{
				v0 = v1; v1 = v2; v2 = *verts++;
				n0 = n1; n1 = n2; n2 = *norm++;
				c0 = c1; c1 = c2; c2 = *col++;

				auto d0 = v1 - v0;
				auto d1 = v2 - v1;
				auto b0 = Normalise3(Cross3(n1, d0), Perpendicular(n1));
				auto b1 = Normalise3(Cross3(n1, d1), Perpendicular(n1));
				bi = Normalise3(b0 + b1, bi); // The bisector at v1
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
					SetPCNT(*v_out++, bb(v1 + b0*hwidth), cc(c1), n1, t00); *i_out++ = index++;
					SetPCNT(*v_out++, bb(inner)         , cc(c1), n1, t10); *i_out++ = index++;

					// Start the next quad
					SetPCNT(*v_out++, bb(v1 + b1*hwidth), cc(c1), n1, t00); *i_out++ = index++;
					SetPCNT(*v_out++, bb(inner)         , cc(c1), n1, t10); *i_out++ = index++;
				}
				else // line turns to the left
				{
					auto inner = u0*pr::Sqrt(d0_sq) > u1*pr::Sqrt(d1_sq) // Pick the maximum distance from v1
						? v1 - u0*d0 + b0*hwidth
						: v1 + u1*d1 + b1*hwidth;

					// Finish the previous quad
					SetPCNT(*v_out++, bb(inner)         , cc(c1), n1, t10); *i_out++ = index++;
					SetPCNT(*v_out++, bb(v1 - b0*hwidth), cc(c1), n1, t00); *i_out++ = index++;
					
					// Start the next quad
					SetPCNT(*v_out++, bb(inner)         , cc(c1), n1, t10); *i_out++ = index++;
					SetPCNT(*v_out++, bb(v1 - b1*hwidth), cc(c1), n1, t00); *i_out++ = index++;
				}
			}

			// Finish the previous quad
			bi = Normalise3(Cross3(n2, v2 - v1), Perpendicular(n2));
			SetPCNT(*v_out++, bb(v2 + bi*hwidth), cc(c2), n2, t00); *i_out++ = index++;
			SetPCNT(*v_out++, bb(v2 - bi*hwidth), cc(c2), n2, t10); *i_out++ = index++;

			props.m_bbox = BBox::Make(lwr, upr);
			return props;
		}
	}
}
