//********************************
// Geometry
//  Copyright (c) Rylogic Ltd 2013
//********************************
#pragma once

#include "pr/geometry/common.h"
#include "pr/geometry/triangle.h"

namespace pr
{
	namespace geometry
	{
		// Return the model buffer requirements of an extrusion
		template <typename Tvr, typename Tir>
		void ExtrudeSize(int cs_count, int path_count, bool closed, bool smooth_cs, Tvr& vcount, Tir& icount)
		{
			assert(cs_count >= 3 && "Cross section must have 3 or more points");
			assert(path_count >= 2 && "Extrusion path must have at least 2 points");

			// - 2 lots of verts at the end caps so they can have outward facing normals.
			// - 'smooth_cs' means smooth normals around the wall of the tube. If false we
			// need to double each vertex around the cross section
			vcount = checked_cast<Tvr>(
				(closed ? cs_count * 2 : 0) +                   // Verts for the two end caps (separate so they can have outward normals)
				(path_count * cs_count * (smooth_cs ? 1 : 2)) + // Verts around each cross section (doubled if not smooth)
				0);
			icount = checked_cast<Tir>(
				(closed ? (cs_count - 2) : 0) * 3 * 2 + // The number of end cap faces x2 (two ends) * 3 (indices/face)
				(path_count - 1) * cs_count * 2 * 3 +   // The number of sections along the path * faces around each section (2 per cs vert) * 3 (indices/face)
				0);
		}

		// Generate a model from an extrusion of a 2d polygon
		// 'cs_count' - the number of verts in the 2d cross section (closed) polygon
		// 'cs' - the cross section points (expects v2 const*). CCW winding order
		// 'path_count' - the number of matrices in the extrusion path
		// 'path' - a function that supplies a stream of transforms describing the extrusion path. Z axis should be the path tangent.
		// 'num_colours - the number of colours pointed to by 'colours', can be equal to 0, 1, or path_count
		// 'colours' - the array of colours of length 'num_colours'
		template <typename TPath, typename TVertIter, typename TIdxIter>
		Props Extrude(
			int cs_count, v2 const* cs,
			int path_count, TPath path,
			bool closed,
			bool smooth_cs,
			int num_colours, Colour32 const* colours,
			TVertIter v_out, TIdxIter i_out)
		{
			// Don't bother handling acute angles, users can just insert really small
			// line segments between acute lines within the path
			using VIdx = typename std::remove_reference<decltype(*i_out)>::type;
			assert(path_count >= 2 && "Path must have at least 2 points");
			
			int vcount, icount;
			ExtrudeSize(cs_count, path_count, closed, smooth_cs, vcount, icount);

			Props props;
			props.m_geom = EGeom::Vert | EGeom::Colr | EGeom::Norm;

			// Colour iterator wrapper
			auto col = pr::CreateRepeater(colours, num_colours, path_count, num_colours != 0 ? colours[num_colours-1] : pr::Colour32White);
			auto cc = [&](pr::Colour32 c) { props.m_has_alpha |= c.a != 0xff; return c; };

			// Bounding box
			auto bb = [&](v4 const& v) { pr::Encompass(props.m_bbox, v); return v; };

			// Verts - create planes of cross sections at each path point
			auto xsection = MakeRing(cs, cs + cs_count);
			if (closed)
			{
				// Starting end cap
				auto ori = path(0);
				for (int x = 0; x != cs_count; ++x)
				{
					auto pt   = ori * v4(xsection[x], 0, 1.0f);
					auto norm = ori * -v4ZAxis;
					SetPCNT(*v_out++, bb(pt), cc(*col), norm, v2Zero);
					--vcount;
				}
			}
			for (int p = 0; p != path_count; ++p, ++col)
			{
				// Cross section verts for each segment of the path
				// Doubled if outward normals are not smooth
				v4 pt, norm;
				auto ori = path(p);
				if (smooth_cs)
				{
					for (int x = 0; x != cs_count; ++x)
					{
						pt = ori * v4(xsection[x], 0, 1.0f);
						norm = ori * v4(Normalise2(Rotate90CCW(xsection[x+1] - xsection[x-1]), v2Zero), 0, 0);
						SetPCNT(*v_out++, bb(pt), cc(*col), norm, v2Zero);
						--vcount;
					}
				}
				else
				{
					for (int x = 0; x != cs_count+1; ++x)
					{
						pt = ori * v4(xsection[x], 0, 1.0f);
						if (x != 0)
						{
							norm = ori * v4(Normalise2(Rotate90CCW(xsection[x] - xsection[x-1]), v2Zero), 0, 0);
							SetPCNT(*v_out++, bb(pt), cc(*col), norm, v2Zero);
							--vcount;
						}
						if (x != cs_count)
						{
							norm = ori * v4(Normalise2(Rotate90CCW(xsection[x+1] - xsection[x]), v2Zero), 0, 0);
							SetPCNT(*v_out++, bb(pt), cc(*col), norm, v2Zero);
							--vcount;
						}
					}
				}
			}
			if (closed)
			{
				// Closing end cap
				auto ori = path(path_count - 1);
				for (int x = 0; x != cs_count; ++x)
				{
					auto pt   = ori * v4(xsection[x], 0, 1.0f);
					auto norm = ori * +v4ZAxis;
					SetPCNT(*v_out++, bb(pt), cc(*col), norm, v2Zero);
					--vcount;
				}
			}
			assert(vcount == 0);

			// Triangulate the cross section to generate faces for the end caps.
			// Save the end cap faces so we can use them for the other end cap as well.
			pr::vector<int> cap_faces;
			if (closed)
			{
				// The cross section may not be convex.
				// Triangulate the end cap using non-convex polygon triangulation.
				TriangulatePolygon(cs, cs_count, [&](int i0, int i1, int i2)
				{
					cap_faces.push_back(i0);
					cap_faces.push_back(i1);
					cap_faces.push_back(i2);
				});
			}

			// Faces
			auto v = 0; // Offset to the first vertex for each segment

			// If closed, create the starting end cap
			if (closed)
			{
				for (int i = 0, iend = int(cap_faces.size()); i != iend; i += 3)
				{
					*i_out++ = static_cast<VIdx>(v + cap_faces[i  ]);
					*i_out++ = static_cast<VIdx>(v + cap_faces[i+2]);
					*i_out++ = static_cast<VIdx>(v + cap_faces[i+1]);
					icount -= 3;
				}
				v += cs_count;
			}

			// Faces along the tube walls
			auto v_per_segment = cs_count * (smooth_cs ? 1 : 2);
			for (int p = 0; p != path_count-1; ++p, v += v_per_segment)
			{
				for (int i = 0; i != v_per_segment; i += (smooth_cs ? 1 : 2))
				{
					auto j = smooth_cs ? (i + 1) % v_per_segment : (i + 1);
					*i_out++ = static_cast<VIdx>(v + i);
					*i_out++ = static_cast<VIdx>(v + j);
					*i_out++ = static_cast<VIdx>(v + j + v_per_segment);
					*i_out++ = static_cast<VIdx>(v + j + v_per_segment);
					*i_out++ = static_cast<VIdx>(v + i + v_per_segment);
					*i_out++ = static_cast<VIdx>(v + i);
					icount -= 6;
				}
			}

			// If closed, create the closing end cap
			if (closed)
			{
				v = cs_count + path_count * cs_count * (smooth_cs ? 1 : 2);
				for (int i = 0, iend = int(cap_faces.size()); i != iend; i += 3)
				{
					*i_out++ = static_cast<VIdx>(v + cap_faces[i  ]);
					*i_out++ = static_cast<VIdx>(v + cap_faces[i+1]);
					*i_out++ = static_cast<VIdx>(v + cap_faces[i+2]);
					icount -= 3;
				}
			}
			assert(icount == 0);

			return props;
		}
	}
}