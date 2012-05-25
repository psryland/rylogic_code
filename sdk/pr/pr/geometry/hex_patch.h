//******************************************************************
// Hex Patch
//  Copyright © Rylogic Ltd 2002
//******************************************************************
// Create a hexagonal patch with texture coordinates

#pragma once
#ifndef PR_GEOMETRY_HEX_PATCH_H
#define PR_GEOMETRY_HEX_PATCH_H

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
			namespace hex_patch
			{
				struct Data
				{
					pr::TVertCont*    m_vert;
					pr::TFaceCont*    m_face;
					float m_radius;
				};
				
				// Create a vertex and add it to the vertex container
				inline pr::uint16 AddVert(pr::v4 const& pt, Data& data)
				{
					// Add the vertex
					pr::Vert v;
					v.m_vertex     = pt;
					v.m_normal     = pr::v4YAxis;
					v.m_colour     = Colour32White;
					v.m_tex_vertex = pr::v2Zero;
					data.m_vert->push_back(v);
					return (pr::uint16)(data.m_vert->size() - 1);
				}
				
				// Create a face and add it to the face container
				inline void AddFace(int i0, int i1, int i2, Data& data)
				{
					pr::Face f;
					f.m_vert_index[0] = pr::uint16(i0);
					f.m_vert_index[1] = pr::uint16(i1);
					f.m_vert_index[2] = pr::uint16(i2);
					f.m_flags = 0;
					f.m_mat_index = 0;
					data.m_face->push_back(f);
				}
				
				// Create the starting hex
				inline void CreateHex(Data& data)
				{
					float r0 = data.m_radius, r1 = 8.660254e-1f * r0, r2 = 0.5f * r0;
					AddVert(pr::v4Origin, data);
					AddVert(pr::v4::make(0.0f, 0.0f, -r0, 1.0f), data);
					AddVert(pr::v4::make( -r1, 0.0f, -r2, 1.0f), data);
					AddVert(pr::v4::make( -r1, 0.0f, +r2, 1.0f), data);
					AddVert(pr::v4::make(0.0f, 0.0f, +r0, 1.0f), data);
					AddVert(pr::v4::make( +r1, 0.0f, +r2, 1.0f), data);
					AddVert(pr::v4::make( +r1, 0.0f, -r2, 1.0f), data);
					AddFace(0,1,2,data);
					AddFace(0,2,3,data);
					AddFace(0,3,4,data);
					AddFace(0,4,5,data);
					AddFace(0,5,6,data);
					AddFace(0,6,1,data);
				}
				
				// Generate the patch
				inline void Generate(Geometry& geometry)
				{
					// Create the mesh
					geometry.m_frame.clear();
					geometry.m_name = "HexPatch";
					geometry.m_frame.push_back(Frame());
					geometry.m_frame[0].m_name = "HexPatch";
					geometry.m_frame[0].m_transform = pr::m4x4Identity;
					Mesh& mesh = geometry.m_frame[0].m_mesh;
					mesh.m_geom_type = pr::geom::EVNC;
					mesh.m_material.push_back(DefaultPRMaterial());
					
					Data data;
					data.m_radius = 1.0f;
					data.m_vert = &mesh.m_vertex;
					data.m_face = &mesh.m_face;
					
					// Create the starting hex model
					CreateHex(data);
				}
			}
		}
		
		void GenerateHexPatch(Geometry& geometry) { return impl::hex_patch::Generate(geometry); }
	}
}

#endif
