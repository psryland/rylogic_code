//******************************************************************
// Mesh Tools
//  Copyright © Rylogic Ltd 2002
//******************************************************************

#ifndef PR_GEOMETRY_MESH_TOOLS_H
#define PR_GEOMETRY_MESH_TOOLS_H
#pragma once

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
			namespace mesh_tools
			{
				// Generate normals for a mesh
				template <typename T> void GenerateNormals(pr::Mesh& mesh)
				{
					// Initialise all of the normals to zero
					uint num_vertices = static_cast<uint>(mesh.m_vertex.size());
					pr::Vert* vb = &mesh.m_vertex[0];
					for (uint v = 0; v < num_vertices; ++v)
					{ 
						Zero(vb->m_normal);
						++vb;
					}

					// For each face, calculate a face normal and add it to each of the vertex normals
					uint num_faces = static_cast<uint>(mesh.m_face.size());
					for( uint f = 0; f < num_faces; ++f )
					{
						pr::Face& face = mesh.m_face[f];

						// Calculate a face normal
						pr::Vert& V0 = mesh.m_vertex[face.m_vert_index[0]];
						pr::Vert& V1 = mesh.m_vertex[face.m_vert_index[1]];
						pr::Vert& V2 = mesh.m_vertex[face.m_vert_index[2]];
						pr::v4 face_norm = pr::Cross3(V1.m_vertex - V0.m_vertex, V2.m_vertex - V0.m_vertex);
						Normalise3(face_norm);

						// Add the face normal to each vertex that references the face
						V0.m_normal += face_norm;
						V1.m_normal += face_norm;
						V2.m_normal += face_norm;
					}

					// Normalise all of the vertex normals
					vb = &mesh.m_vertex[0];
					for (uint v = 0; v < num_vertices; ++v)
					{ 
						Normalise3IfNonZero(vb->m_normal);
						++vb;
					}
				}

				// Calculate the bounding box for a mesh
				inline pr::BoundingBox GetBoundingBox(pr::Mesh const& mesh)
				{
					// Enclose all of the vertices
					pr::BoundingBox bbox = pr::BBoxReset;
					for (pr::TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
						pr::Encompase(bbox, v->m_vertex);
					return bbox;
				}

				// Modify a mesh to be just verts and faces
				namespace reduce_mesh
				{
					typedef pr::Array<v4>			TVert;
					typedef pr::Array<std::size_t>	TIdx;
					struct Dict
					{
						TVert*	m_verts;
						TIdx*	m_remap;
						TIdx*	m_lookup;
						Dict(TVert*	verts, TIdx* remap, TIdx* lookup) : m_verts(verts), m_remap(remap), m_lookup(lookup) {}
						bool operator() (std::size_t lhs, v4 const& rhs)		{ return (*m_verts)[lhs] < rhs; }
						bool operator() (v4 const& lhs, std::size_t rhs)		{ return lhs < (*m_verts)[rhs]; }
						bool operator() (std::size_t lhs, std::size_t rhs)		{ return (*m_verts)[lhs] < (*m_verts)[rhs]; }
						void Add(v4 const& v)
						{
							v4 quant_v = Quantise(v, 1 << 12);
							TIdx::iterator in = std::lower_bound(m_lookup->begin(), m_lookup->end(), quant_v, *this);
							if( in == m_lookup->end() || (*m_verts)[*in] != v )
							{
								std::size_t idx = m_verts->size();
								m_verts->push_back(quant_v);
								m_lookup->insert(in, idx);
								m_remap->push_back(idx);
							}
							else
							{
								m_remap->push_back(*in);
							}
						}
					};
				}//namespace reduce_mesh

				template <typename T> void ReduceMesh(pr::Mesh& mesh)
				{
					uint num_vertices	= static_cast<uint>(mesh.m_vertex.size());
					reduce_mesh::TVert verts;	verts.reserve(num_vertices);
					reduce_mesh::TIdx  remap;	remap.reserve(num_vertices);
					reduce_mesh::TIdx  lookup;	lookup.reserve(num_vertices);
					reduce_mesh::Dict dict(&verts, &remap, &lookup);
					
					// Quantise the verts and add them to the dictionary
					for( uint i = 0; i != num_vertices; ++i )
						dict.Add(mesh.m_vertex[i].m_vertex);

					// Replace the verts in the mesh with the reduced set
					mesh.m_vertex.resize(0);
					pr::Vert vert; vert.set(v4Zero, v4Zero, Colour32White, v2Zero);
					for( reduce_mesh::TVert::const_iterator v = verts.begin(), v_end = verts.end(); v != v_end; ++v )
					{
						vert.m_vertex = *v;	
						mesh.m_vertex.push_back(vert);
					}

					// Update the face indices
					for( pr::TFaceCont::iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f )
					{
						f->m_vert_index[0] = static_cast<pr::uint16>(remap[f->m_vert_index[0]]);
						f->m_vert_index[1] = static_cast<pr::uint16>(remap[f->m_vert_index[1]]);
						f->m_vert_index[2] = static_cast<pr::uint16>(remap[f->m_vert_index[2]]);
					}
				}
			}
		}

		// Generate normals for a mesh using the directions of the surrounding faces
		inline void GenerateNormals(pr::Mesh& mesh)	{ return impl::mesh_tools::GenerateNormals<void>(mesh); }

		// Calculate the bounding box for a mesh
		inline pr::BoundingBox GetBoundingBox(pr::Mesh const& mesh) { return impl::mesh_tools::GetBoundingBox(mesh); }
		
		// Modify a mesh to be just verts and faces, i.e remove redundant verts when texture coords are not needed
		inline void ReduceMesh(pr::Mesh& mesh) { return impl::mesh_tools::ReduceMesh<void>(mesh); }
	}
}

#endif//PR_GEOMETRY_MESH_TOOLS_H