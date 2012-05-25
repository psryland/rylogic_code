//******************************************************************
// Geosphere
//  Copyright © Rylogic Ltd 2002
//******************************************************************
#pragma once
#ifndef PR_GEOMETRY_GEOSPHERE_H
#define PR_GEOMETRY_GEOSPHERE_H

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
			namespace geosphere
			{
				struct Child
				{
					pr::uint16 m_other_parent;
					pr::uint16 m_child;
				};
				typedef pr::Array<Child>  TChild;
				typedef pr::Array<TChild> TVertexLookupCont;
				
				// A struct to hold all of the generation data
				struct CreateGeosphereData
				{
					TVertexLookupCont m_vertex_lookup;
					pr::TVertCont*    m_vertex;
					pr::TFaceCont*    m_face;
					float             m_radius;
					uint              m_divisions;
				};
				
				// Create a vertex and add it to the vertex container
				template <typename T> pr::uint16 AddVertex(v4 const& norm, v2 const& uv, CreateGeosphereData& data)
				{
					PR_ASSERT(PR_DBG_GEOM, IsNormal3(norm), "");
					
					// Add the vertex
					pr::Vert vertex;
					vertex.m_normal     = norm;
					vertex.m_vertex     = data.m_radius * vertex.m_normal; vertex.m_vertex.w = 1.0f;
					vertex.m_colour     = Colour32White;
					vertex.m_tex_vertex = uv;
					data.m_vertex       ->push_back(vertex);
					
					// Add an entry in the lookup table
					data.m_vertex_lookup.push_back(TChild());
					data.m_vertex_lookup.back().reserve(Max(3 << (data.m_divisions - 2), 3));
					return (pr::uint16)(data.m_vertex->size() - 1);
				}
				
				// Get the vertex that has these two vertices as parents
				template <typename T> pr::uint16 GetVertex(pr::uint16 parent1, pr::uint16 parent2, CreateGeosphereData& data)
				{
					// Try and find 'parent2' in 'data.m_vertex_lookup[parent1]' or 'parent1' in 'data.m_vertex_lookup[parent2]'
					for (TChild::const_iterator i = data.m_vertex_lookup[parent1].begin(), i_end = data.m_vertex_lookup[parent1].end(); i != i_end; ++i)
					{
						if (i->m_other_parent == parent2) return i->m_child;
					}
					for (TChild::const_iterator i = data.m_vertex_lookup[parent2].begin(), i_end = data.m_vertex_lookup[parent2].end(); i != i_end; ++i)
					{
						if (i->m_other_parent == parent1) return i->m_child;
					}
					
					// If it wasn't found add a vertex
					pr::Vert& vert_a = (*data.m_vertex)[parent1].m_tex_vertex.x < (*data.m_vertex)[parent2].m_tex_vertex.x ? (*data.m_vertex)[parent1] : (*data.m_vertex)[parent2];
					pr::Vert& vert_b = (*data.m_vertex)[parent1].m_tex_vertex.x < (*data.m_vertex)[parent2].m_tex_vertex.x ? (*data.m_vertex)[parent2] : (*data.m_vertex)[parent1];
					v4 norm = GetNormal3(vert_a.m_normal + vert_b.m_normal);
					v2 uv = v2::make(ATan2Positive(norm.y, norm.x) / maths::tau, (1.0f - norm.z) * 0.5f);
					if (!(FGtrEql(uv.x, vert_a.m_tex_vertex.x) && FLessEql(uv.x, vert_b.m_tex_vertex.x))) { uv.x += 1.0f; }
					PR_ASSERT(PR_DBG_GEOM, FGtrEql(uv.x, vert_a.m_tex_vertex.x) && FLessEql(uv.x, vert_b.m_tex_vertex.x), "");
					pr::uint16 new_vert_index = AddVertex<T>(norm, uv, data); // Remember this modifies data.m_vertex_lookup
					
					// Add the entry to the parent with the least number of children
					if (data.m_vertex_lookup[parent1].size() < data.m_vertex_lookup[parent2].size())
					{
						Child child = {parent2, new_vert_index};
						data.m_vertex_lookup[parent1].push_back(child);
					}
					else
					{
						Child child = {parent1, new_vert_index};
						data.m_vertex_lookup[parent2].push_back(child);
					}
					
					return new_vert_index;
				}
				
				// Recursively add a face
				template <typename T> void AddFace(pr::uint16 V00, pr::uint16 V11, pr::uint16 V22, uint level, CreateGeosphereData& data)
				{
					PR_ASSERT(PR_DBG_GEOM, V00 < data.m_vertex->size(), "");
					PR_ASSERT(PR_DBG_GEOM, V11 < data.m_vertex->size(), "");
					PR_ASSERT(PR_DBG_GEOM, V22 < data.m_vertex->size(), "");
					if (level == data.m_divisions)
					{
						pr::Face face;
						face.m_flags         = 0;
						face.m_mat_index     = 0;
						face.m_vert_index[0] = V00;
						face.m_vert_index[1] = V11;
						face.m_vert_index[2] = V22;
						data.m_face->push_back(face);
					}
					else
					{
						pr::uint16 V01 = GetVertex<T>(V00, V11, data);
						pr::uint16 V12 = GetVertex<T>(V11, V22, data);
						pr::uint16 V20 = GetVertex<T>(V22, V00, data);
						AddFace<T>(V00, V01, V20, level + 1, data);
						AddFace<T>(V01, V11, V12, level + 1, data);
						AddFace<T>(V20, V12, V22, level + 1, data);
						AddFace<T>(V01, V12, V20, level + 1, data);
					}
				}
				
				// Create an Icosahedron and recursively subdivide the triangles
				template <typename T> void CreateIcosahedron(CreateGeosphereData& data)
				{
					const float A       = 2.0f / (1.0f + maths::phi * maths::phi);
					const float H1      =  1.0f - A;
					const float H2      = -1.0f + A;
					const float R       = Sqrt(1.0f - H1 * H1);
					const float dAng    = maths::tau / 5.0f;
					
					// Add the vertices
					float ang1 = 0.0f, ang2 = maths::tau / 10.0f;
					float ua = 0.0f, ub = 0.0f;
					for (uint w = 0; w != 6; ++w, ang1 += dAng, ang2 += dAng)
					{
						v4 norm_a = v4::make(R * Cos(ang1), R * Sin(ang1), H1, 0.0f);
						v4 norm_b = v4::make(R * Cos(ang2), R * Sin(ang2), H2, 0.0f);
						float u_a = ATan2Positive(norm_a.y, norm_a.x) / maths::tau; ua = u_a + (u_a < ua);
						float u_b = ATan2Positive(norm_b.y, norm_b.x) / maths::tau; ub = u_b + (u_b < ub);
						AddVertex<T>(v4ZAxis, v2::make(ua, 0.0f), data);
						AddVertex<T>(norm_a  , v2::make(ua, (1.0f - norm_a.z) * 0.5f), data);
						AddVertex<T>(norm_b  , v2::make(ub, (1.0f - norm_b.z) * 0.5f), data);
						AddVertex<T>(-v4ZAxis, v2::make(ub, 1.0f), data);
					}
					
					// Add the faces
					for (pr::uint16 i = 0; i != 5; ++i)
					{
						AddFace<T>(i *4 + 0, i*4 + 1, (1+i)*4 + 1, 0, data);
						AddFace<T>(i *4 + 1, i*4 + 2, (1+i)*4 + 1, 0, data);
						AddFace<T>((1+i)*4 + 1, i*4 + 2, (1+i)*4 + 2, 0, data);
						AddFace<T>(i *4 + 2, i*4 + 3, (1+i)*4 + 2, 0, data);
					}
				}
				
				// Return the number of verts and faces needed for a geosphere subdivided 'divisions' times
				inline uint VertCount(uint divisions)   { return 3 + 10 * Pow2(2 * divisions) + 11 * Pow2(divisions); }
				inline uint FaceCount(uint divisions)   { return 10 * Pow2(2 * divisions + 1); }
				
				// Generate a geosphere
				template <typename T> void Generate(Geometry& geometry, float radius, uint divisions)
				{
					// Create the mesh
					geometry.m_frame.clear();
					geometry.m_name = "Geosphere";
					geometry.m_frame.push_back(Frame());
					geometry.m_frame[0].m_name = "Geosphere";
					geometry.m_frame[0].m_transform = m4x4Identity;
					Mesh& pr_mesh = geometry.m_frame[0].m_mesh;
					pr_mesh.m_geom_type = pr::geom::EVNCT;
					pr_mesh.m_material.push_back(DefaultPRMaterial());
					
					// Predict the number of vertices and reserve a buffer
					uint num_vertices = VertCount(divisions);
					uint num_faces    = FaceCount(divisions);
					
					CreateGeosphereData data;
					data.m_vertex       = &pr_mesh.m_vertex;
					data.m_face         = &pr_mesh.m_face;
					data.m_radius       = radius;
					data.m_divisions    = divisions;
					data.m_vertex_lookup.reserve(num_vertices);
					data.m_vertex      ->reserve(num_vertices);
					data.m_face        ->reserve(num_faces);
					CreateIcosahedron<T>(data);
					
					PR_ASSERT(PR_DBG_GEOM, (uint)pr_mesh.m_vertex.size() == num_vertices, "");
					PR_ASSERT(PR_DBG_GEOM, (uint)pr_mesh.m_face.size()   == num_faces   , "");
				}
			}
		}
		
		// Generate a geosphere
		inline uint GeosphereVertCount(uint divisions)  { return impl::geosphere::VertCount(divisions); }
		inline uint GeosphereFaceCount(uint divisions)  { return impl::geosphere::FaceCount(divisions); }
		inline void GenerateGeosphere(Geometry& geometry, float radius, uint divisions) { return impl::geosphere::Generate<void>(geometry, radius, divisions); }
	}
}

#endif//PR_GEOMETRY_GEOSPHERE_H