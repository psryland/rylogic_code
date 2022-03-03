//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/face.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/vertex.h"

using namespace pr;
using namespace pr::terrain;

// Returns the index of 'edge' within this face
uint Face::EdgeIndex(Edge const& edge) const
{
	PR_ASSERT(PR_DBG_TERRAIN, edge.m_Lface == this || edge.m_Rface == this, "'edge' does not belong to this face");
	for (uint i = 0; i != 3; ++i)
	{
		if (m_edges[i] == &edge) return i;
	}
	PR_ASSERT(PR_DBG_TERRAIN, false, "Edge index not found");
	return 0;
}

// Return a 2d line representing an edge of this face
Line2d Face::Line(uint i) const
{
	return Line2d(m_vertices[i]->m_position, m_vertices[(i + 1) % 3]->m_position - m_vertices[i]->m_position);
}

// Return a point in the centre of this face
v4 Face::MidPoint() const
{
	return (m_vertices[0]->m_position + m_vertices[1]->m_position + m_vertices[2]->m_position) / 3.0f;
}
