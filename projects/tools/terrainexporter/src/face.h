//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"
#include "terrainexporter/line2d.h"

namespace pr
{
	namespace terrain
	{
		struct Face
		{
			pr::v4				m_original_vertex[3];	// The original vertices of this face (in region space).
			pr::FRect			m_bounds;				// The bounds of this face (in region space).
			pr::uint			m_index[3];				// The indices of the vertices of this face
			pr::uint			m_material_index;		// The material id for the face
			pr::uint			m_surface_flags;		// Per face surface information
			Edge const*			m_edges[3];				// Pointers to the edges of this face
			Vertex const*		m_vertices[3];			// Pointers to the vertices of this face (vertices in region space)
			pr::Plane const*	m_plane;				// A pointer to the plane used to represent this face
			pr::uint			m_face_id;				// A unique id for this face
			mutable pr::uint	m_tree_id;				// The unique id of the tree that this face is in during BSP tree building

			pr::uint			EdgeIndex(Edge const& edge) const;
			Line2d				Line(pr::uint edge_index) const;
			pr::v4				MidPoint() const;
		};

		// Predicate for sorting face pointers into a std::set.
		// This is used to guarantee the set order 
		inline bool Pred::operator () (Face const* lhs, Face const* rhs) const
		{
			return (lhs && rhs) ? lhs->m_face_id < rhs->m_face_id : lhs < rhs;
		}

	}//namespace terrain
}//namespace pr

