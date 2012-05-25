//******************************************************************
// Skin
//  Copyright © Rylogic Ltd 2002
//******************************************************************

#ifndef PR_GEOMETRY_SKIN_H
#define PR_GEOMETRY_SKIN_H
#pragma once

#include "pr/maths/maths.h"
#include "pr/maths/convexhull.h"
#include "pr/common/alloca.h"
#include "pr/common/assert.h"
#include "pr/geometry/geometry.h"

namespace pr
{
	namespace geometry
	{
		namespace impl
		{
			namespace skin
			{
				// Generate a mesh representing the convex hull of a set of verts
				void GenerateSkin(pr::Geometry& geometry, pr::v4 const* vertex_begin, pr::v4 const* vertex_end)
				{
					// Create the mesh
					geometry.m_frame.clear();
					geometry.m_name = "Skin";
					geometry.m_frame.push_back(Frame());
					geometry.m_frame[0].m_name = "Skin";
					geometry.m_frame[0].m_transform = m4x4Identity;

					pr::Mesh& pr_mesh = geometry.m_frame[0].m_mesh;
					pr_mesh.m_geom_type = pr::geom::EVNC;
					pr_mesh.m_material.push_back(pr::DefaultPRMaterial());

					std::size_t vertex_count = vertex_end - vertex_begin;

					// Generate the faces
					switch (vertex_count)
					{
					case 0: return;
					case 1:
						pr_mesh.m_vertex.push_back(pr::Vert::make(*vertex_begin, v4ZAxis, Colour32White, v2Zero));
						pr_mesh.m_face.push_back(pr::Face::make(0, 0, 0, 0, 0));
						return;
					case 2:
						pr_mesh.m_vertex.push_back(pr::Vert::make(*(vertex_begin+0), v4ZAxis, Colour32White, v2Zero));
						pr_mesh.m_vertex.push_back(pr::Vert::make(*(vertex_begin+1), v4ZAxis, Colour32White, v2Zero));
						pr_mesh.m_face.push_back(pr::Face::make(0, 1, 0, 0, 0));
						return;
					default:
						{
							// Create an index buffer
							std::size_t vert_count = static_cast<std::size_t>(vertex_end - vertex_begin);
							std::size_t face_count = 2 * (vert_count - 2);
							pr::uint16* index = PR_ALLOCA_POD(pr::uint16, vert_count);
							pr::uint16* face  = PR_ALLOCA_POD(pr::uint16, 3 * face_count);
							for (pr::uint16 i = 0; i != vert_count; ++i) index[i] = i;

							// Find the convex hull
							pr::ConvexHull(vertex_begin, index, index + vert_count, face, face + 3*face_count, vert_count, face_count);

							// Copy the verts into the geometry
							pr_mesh.m_vertex.resize(vert_count);
							TVertCont::iterator pr_v = pr_mesh.m_vertex.begin();
							for (pr::uint16 const* i = index, *i_end = i + vert_count; i != i_end; ++i, ++pr_v)
							{
								pr_v->m_vertex = vertex_begin[*i];
							}

							// Create the faces
							pr_mesh.m_face.resize(face_count);
							pr::TFaceCont::iterator pr_f = pr_mesh.m_face.begin();
							for (pr::uint16 const* f = face, *f_end = f + face_count*3; f != f_end; f += 3, ++pr_f)
							{
								pr_f->set(*(f+0), *(f+1), *(f+2), 0, 0);
							}
						}
						return;
					}
				}
			}
		}

		// Generate a mesh of triangles around a set of verts
		void GenerateSkin(pr::Geometry& geometry, v4 const* vertex, std::size_t num_verts)	{ return impl::skin::GenerateSkin(geometry, vertex, vertex + num_verts); }
		void GenerateSkin(pr::Geometry& geometry, v4 const* vert_begin, v4 const* vert_end)	{ return impl::skin::GenerateSkin(geometry, vert_begin, vert_end); }
	}
}

#endif//PR_GEOMETRY_SKIN_H