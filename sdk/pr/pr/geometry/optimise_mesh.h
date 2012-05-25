//******************************************************************
// Optimise Mesh
//  Copyright © Rylogic Ltd 2002
//******************************************************************

#ifndef PR_GEOMETRY_OPTIMISE_MESH_H
#define PR_GEOMETRY_OPTIMISE_MESH_H
#pragma once

#include <d3d9.h>
#include <d3dx9.h>
#include "pr/maths/maths.h"
#include "pr/common/hresult.h"
#include "pr/common/array.h"
#include "pr/common/assert.h"
#include "pr/geometry/geometry.h"

namespace pr
{
	namespace geometry
	{
		namespace impl
		{
			namespace optimise_mesh
			{
				// Use the D3DX functions to optimise 'mesh'
				template <typename T> void OptimiseMesh(pr::Mesh& mesh)
				{
					uint num_faces		= static_cast<uint>(mesh.m_face.size());
					uint num_vertices	= static_cast<uint>(mesh.m_vertex.size());
					PR_ASSERT(PR_DBG_GEOM, num_vertices <= static_cast<pr::uint16>(~0), "More than 65535 vertices");

					typedef std::vector<DWORD> TIdx;

					// Make an array of indices
					TIdx	indices(num_faces * 3, 0);
					DWORD*	index = &indices[0];
					for (TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f)
					{
						*index++ = f->m_vert_index[0];
						*index++ = f->m_vert_index[1];
						*index++ = f->m_vert_index[2];
					}

					// Optimise the face order
					TIdx remap_faces(num_faces);
					if (Failed(D3DXOptimizeFaces(&indices[0], num_faces, num_vertices, TRUE, &remap_faces[0])))
					{
						PR_ASSERT(PR_DBG_GEOM, false, "D3DXOptimizeFaces failed in void pr::geometry::OptimiseMesh(Mesh& mesh)");
						return;
					}

					{// Apply the remap to the faces
						TFaceCont copy = mesh.m_face;
						index = &indices[0];
						Face* face = &mesh.m_face[0];
						for( uint f = 0; f != num_faces; ++f, ++face )
						{
							*face = copy[remap_faces[f]];
							*index++ = face->m_vert_index[0];
							*index++ = face->m_vert_index[1];
							*index++ = face->m_vert_index[2];
						}
					}

					// Optimise the vertex order
					TIdx remap_verts(num_vertices);
					TIdx unmap_verts(num_vertices);
					if (Failed(D3DXOptimizeVertices(&indices[0], num_faces, num_vertices, TRUE, &remap_verts[0])))
					{
						PR_ASSERT(PR_DBG_GEOM, false, "D3DXOptimizeVertices failed in void pr::geometry::OptimiseMesh(Mesh& mesh)");
						return;
					}
					
					// Get the new vertex count
					while( num_vertices && remap_verts[num_vertices - 1] == 0xFFFFFFFF )
						--num_vertices;

					{// Remap the vertices
						TVertCont copy = mesh.m_vertex;
						mesh.m_vertex.resize(num_vertices);
						Vert* vert = &mesh.m_vertex[0];
						uint v;
						for( v = 0; v != num_vertices; ++v, ++vert )
						{
							*vert = copy[remap_verts[v]];
							unmap_verts[remap_verts[v]] = v;
						}
					}

					// Update the indices
					for( TFaceCont::iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f )
					{
						f->m_vert_index[0] = static_cast<pr::uint16>(unmap_verts[f->m_vert_index[0]]);
						f->m_vert_index[1] = static_cast<pr::uint16>(unmap_verts[f->m_vert_index[1]]);
						f->m_vert_index[2] = static_cast<pr::uint16>(unmap_verts[f->m_vert_index[2]]);
					}
				}
			}
		}

		// Use the D3DX functions to optimise 'mesh'
		inline void OptimiseMesh(pr::Mesh& mesh) { return impl::optimise_mesh::OptimiseMesh<void>(mesh); }
	}
}

#endif//PR_GEOMETRY_OPTIMISE_MESH_H