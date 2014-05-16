//******************************************************************
// Mesh Tools
//  Copyright (c) Rylogic Ltd 2002
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
						face_norm = Normalise3(face_norm);

						// Add the face normal to each vertex that references the face
						V0.m_normal += face_norm;
						V1.m_normal += face_norm;
						V2.m_normal += face_norm;
					}

					// Normalise all of the vertex normals
					vb = &mesh.m_vertex[0];
					for (uint v = 0; v < num_vertices; ++v)
					{
						vb->m_normal = Normalise3IfNonZero(vb->m_normal);
						++vb;
					}
				}

				// Calculate the bounding box for a mesh
				inline pr::BBox GetBoundingBox(pr::Mesh const& mesh)
				{
					// Enclose all of the vertices
					pr::BBox bbox = pr::BBoxReset;
					for (pr::TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
						pr::Encompass(bbox, v->m_vertex);
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
		inline pr::BBox GetBoundingBox(pr::Mesh const& mesh) { return impl::mesh_tools::GetBoundingBox(mesh); }

		// Modify a mesh to be just verts and faces, i.e remove redundant verts when texture coords are not needed
		inline void ReduceMesh(pr::Mesh& mesh) { return impl::mesh_tools::ReduceMesh<void>(mesh); }
	}
}

#endif

/*
// Generate normals for this model
// Assumes the locked region of the model contains a triangle list
void pr::rdr::model::GenerateNormals(MLock& mlock, Range const* v_range, Range const* i_range)
{
	if (!v_range) v_range = &mlock.m_vrange;
	if (!i_range) i_range = &mlock.m_irange;
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *v_range), "The provided vertex range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_ilock.m_range, *i_range), "The provided index range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, (i_range->size() % 3) == 0, "This function assumes the index range refers to a triangle list");
	PR_ASSERT(PR_DBG_RDR, vf::GetFormat(mlock.m_model->GetVertexType()) & vf::EFormat::Norm, "Vertices must have normals");

	// Initialise all of the normals to zero
	vf::iterator vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		Zero(vb->normal());

	Index* ib = mlock.m_ilock.m_ptr + i_range->m_begin;
	for (std::size_t f = 0, f_end = f + i_range->size()/3; f != f_end; ++f, ib += 3)
	{
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[0]), "Face index refers outside of the locked vertex range");
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[1]), "Face index refers outside of the locked vertex range");
		PR_ASSERT(PR_DBG_RDR, mlock.m_vlock.m_range.contains(ib[2]), "Face index refers outside of the locked vertex range");
		vf::RefVertex v0 = mlock.m_vlock.m_ptr[ib[0]];
		vf::RefVertex v1 = mlock.m_vlock.m_ptr[ib[1]];
		vf::RefVertex v2 = mlock.m_vlock.m_ptr[ib[2]];

		// Calculate a face normal
		v3 norm = Cross3(v1.vertex() - v0.vertex(), v2.vertex() - v0.vertex());
		norm = Normalise3IfNonZero(norm);

		// Add the normal to each vertex that references the face
		v0.normal() += norm;
		v1.normal() += norm;
		v2.normal() += norm;
	}

	// Normalise all of the normals
	vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		vb->normal() = Normalise3IfNonZero(vb->normal());
}
void pr::rdr::model::GenerateNormals(ModelPtr& model, Range const* v_range, Range const* i_range)
{
	MLock mlock(model);
	GenerateNormals(mlock, v_range, i_range);
}

// Set the vertex colours in a model
void pr::rdr::model::SetVertexColours(MLock& mlock, Colour32 colour, Range const* v_range)
{
	if (!v_range) v_range = &mlock.m_vrange;
	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *v_range), "The provided vertex range is not within the locked range");
	PR_ASSERT(PR_DBG_RDR, vf::GetFormat(mlock.m_model->GetVertexType()) & vf::EFormat::Diff, "Vertices must have colours");

	vf::iterator vb = mlock.m_vlock.m_ptr + v_range->m_begin;
	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
		vb->colour() = colour;
}

*/