//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"
#include "pr/terrain/terrain.h"
#include "terrainexporter/face.h"
#include "terrainexporter/debug.h"

namespace pr
{
	namespace terrain
	{
		struct LeafEx
		{
			Leaf			m_leaf;			// The leaf that will go into the final data
			Face const*		m_face;			// A face that this leaf represents
			int				m_index;		// The index of this leaf within the list of leaves in a bsp tree
			PR_EXPAND(PR_DBG_TERRAIN, TFaceCPtrSet m_faces;)	// The faces that this leaf represents (they should all be equivalent)
			
			LeafEx() {}
			LeafEx(int index, Face const* face)
			:m_index(index)
			,m_face(face)
			{
				if( face )
				{
					m_leaf.SetPlane(*face->m_plane);
					m_leaf.SetMaterialId(face->m_material_index);
					m_leaf.SetSurfaceFlags(0);
				}
				else
				{
					m_leaf.SetPlane(pr::v4::make(0.0f, 1.0f, 0.0f, -pr::terrain::DefaultHeight()));
					m_leaf.SetMaterialId(0);
					m_leaf.SetSurfaceFlags(0);
				}
				m_leaf.pad = 0;
				PR_EXPAND(PR_DBG_TERRAIN, m_faces.insert(face));
			}
		};
	}//namespace terrain
}//namespace pr

