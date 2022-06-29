//*******************************************************************************
// Terrain Exporter
//  Copyright (c) Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"

namespace pr
{
	namespace terrain
	{
		struct Edge
		{
			pr::uint		m_index0;			// The index of the start vertex for the edge
			pr::uint		m_index1;			// The index of the end vertex for the edge
			Vertex const*	m_vertex0;			// A pointer to the start vertex for the edge
			Vertex const*	m_vertex1;			// A pointer to the end vertex for the edge
			Face const*		m_Lface;			// The face on the left of the edge
			Face const*		m_Rface;			// The face on the right of the edge
			mutable bool	m_contributes;		// True if this edge contributes to the terrain data. Used during tree building
			pr::uint		m_edge_number;		// The edge number within the mesh. Helpful for debugging

			LineEqn Eqn() const;
			Line2d	Line() const;
			pr::v4	Direction() const;
			Face const* GetOtherFace(Face const* face) const;
		};

		inline bool operator == (Edge const& lhs, Edge const& rhs)
		{
			return	(lhs.m_index0 == rhs.m_index0 && lhs.m_index1 == rhs.m_index1) ||
					(lhs.m_index0 == rhs.m_index1 && lhs.m_index1 == rhs.m_index0);
		}
		inline bool operator <  (Edge const& lhs, Edge const& rhs)
		{
			// Always sort using the smallest index first
			pr::uint lhs_index0 = (lhs.m_index0 < lhs.m_index1) ? (lhs.m_index0) : (lhs.m_index1);
			pr::uint lhs_index1 = (lhs.m_index0 < lhs.m_index1) ? (lhs.m_index1) : (lhs.m_index0);
			pr::uint rhs_index0 = (rhs.m_index0 < rhs.m_index1) ? (rhs.m_index0) : (rhs.m_index1);
			pr::uint rhs_index1 = (rhs.m_index0 < rhs.m_index1) ? (rhs.m_index1) : (rhs.m_index0);
			return (lhs_index0 != rhs_index0) ? lhs_index0 < rhs_index0 : lhs_index1 < rhs_index1;
		}

		// Predicate for sorting edge pointers	
		inline bool Pred::operator () (Edge const* lhs, Edge const* rhs) const
		{
			return (lhs && rhs) ? lhs->m_edge_number < rhs->m_edge_number : lhs < rhs;
		}

	}//namespace terrain
}//namespace pr

