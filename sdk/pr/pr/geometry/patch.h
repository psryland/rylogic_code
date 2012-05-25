//******************************************************************
// Patch
//  Copyright © Rylogic Ltd 2002
//******************************************************************
// Create a rectangular patch with texture coordinates
//	geometry - the geometry object to return
//	patch_origin		- the coordinate of the left, top edge of the patch
//	patch_dimension		- the overall width/height of the patch
//	patch_divisions		- how many quads to create over the width/height of the patch
//	texture_origin		- the texture coordinate to start with at the top/left corner of the patch
//	texture_dimension	- the width/height of the texture on the patch.
//						  If this is less than the dimension of the patch then the texture coords will repeat
//
//	The returned patch will look like:
//  0,0 ---------------------> patch_x
//   | +-----+-----+-----+-----+
//   | | 1  /| 3  /| 5  /| 7  /|
//   | |  /  |  /  |  /  |  /  |
//   | |/  2 |/  4 |/  6 |/  8 |
//   | +-----+-----+-----+-----+
//   | | 9  /| 11 /| 13 /| 15 /|
//   | |  /  |  /  |  /  |  /  |
//   | |/ 10 |/ 12 |/ 14 |/ 16 |
//   V +-----+-----+-----+-----+
// patch_y
//
// Vertex order:
// 1-----2-----3----4
// |     |     |    |
// 5-----6-----7----8
// |     |     |    |
//
// Face vertex order:
//  1----3,4---
//  |   / |
//  | /   | /
//  2,5---6---
//  |     |

#pragma once
#ifndef PR_GEOMETRY_PATCH_H
#define PR_GEOMETRY_PATCH_H

#include "pr/maths/maths.h"
#include "pr/common/array.h"
#include "pr/common/assert.h"
#include "pr/geometry/geometry.h"

namespace pr
{
	namespace geometry
	{
		namespace impl
		{
			namespace patch
			{
				// Generate the patch
				template <typename T> void Generate(Geometry& geometry, v2 const& origin, v2 const& dimensions, iv2 const& divisions, v2 const& tex_origin, v2 const& tex_dim)
				{
					PR_ASSERT(PR_DBG_GEOM, divisions.x != 0 && divisions.y != 0);

					geometry.m_frame.clear();
					geometry.m_name = "Patch";
					geometry.m_frame.push_back(pr::Frame());
					geometry.m_frame[0].m_name = "Patch";
					geometry.m_frame[0].m_transform = m4x4Identity;
					pr::Mesh& pr_mesh = geometry.m_frame[0].m_mesh;
					pr_mesh.m_geom_type = pr::geom::EVNT;
					pr_mesh.m_material.push_back(pr::DefaultPRMaterial());

					// Create the vertices
					pr_mesh.m_vertex.reserve((divisions.x + 1) * (divisions.y + 1));
					pr::v2 vertex       = origin;
					pr::v2 texture      = tex_origin;
					pr::v2 vertex_step  = dimensions / divisions;
					for (int h = 0; h <= divisions.y; ++h)
					{
						for (int w = 0; w <= divisions.x; ++w)
						{
							pr::Vert v;
							v.m_vertex     = pr::v4::make(vertex, 0.0f, 1.0f);
							v.m_normal     = pr::v4ZAxis;
							v.m_colour     = pr::Colour32Black;
							v.m_tex_vertex = texture / tex_dim;
							pr_mesh.m_vertex.push_back(v);

							vertex.x += vertex_step.x;
							texture.x = Fmod(texture.x + vertex_step.x, tex_dim.x);
						}
						vertex.x  = origin.x;	
						texture.y = origin.x;
						vertex.y += vertex_step.y;
						texture.y = Fmod(texture.y + vertex_step.y, tex_dim.y);
					}

					// Create the faces
					pr_mesh.m_face.reserve(divisions.x * divisions.y * 2);
					for (pr::uint16 h = 0; h < (pr::uint16)divisions.y; ++h)
					{
						pr::uint16 row = (pr::uint16)((divisions.x + 1) * h);
						for( pr::uint16 w = 0; w < (pr::uint16)divisions.x; ++w )
						{
							pr::uint16 col = row + w;
							
							pr::Face f;
							f.m_flags = 0;
							f.m_mat_index = 0;

							f.m_vert_index[0] = col;
							f.m_vert_index[1] = col + (pr::uint16)divisions.x + 1;
							f.m_vert_index[2] = col + 1;
							pr_mesh.m_face.push_back(f);

							f.m_vert_index[0] = col + 1;
							f.m_vert_index[1] = col + (pr::uint16)divisions.x + 1;
							f.m_vert_index[2] = col + (pr::uint16)divisions.x + 2;
							pr_mesh.m_face.push_back(f);
						}
					}
				}
			}
		}

		// Generate an NxM patch of triangles
		// 'origin' is the top/left corner of the patch
		// 'dimensions' is the width/height of the patch
		// 'divisions' is the number of quads across the width/height of the patch. NOTE: num_verts_across = divisions + 1
		// 'tex_origin' is the texture coord of the top/left corner
		// 'tex_dim' is the size of the texture over the patch
		void GeneratePatch(Geometry& geometry, v2 const& origin, v2 const& dimensions, iv2 const& divisions, v2 const& tex_origin, v2 const& tex_dim)	{ return impl::patch::Generate<void>(geometry, origin, dimensions, divisions, tex_origin, tex_dim); }
		void GeneratePatch(Geometry& geometry, v2 const& origin, v2 const& dimensions, iv2 const& divisions)											{ return GeneratePatch(geometry, origin, dimensions, divisions, pr::v2Zero, pr::v2::make(1.0f,1.0f)); }
	}
}

#endif
