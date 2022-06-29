//*******************************************************************************
// Terrain Exporter
//  Copyright (C) Rylogic Ltd 2009
//*******************************************************************************
#include "stdafx.h"
#include "terrainexporter/edge.h"
#include "terrainexporter/lineeqn.h"
#include "terrainexporter/line2d.h"
#include "terrainexporter/vertex.h"

using namespace pr;
using namespace pr::terrain;

// Convert this edge to a line equation
LineEqn Edge::Eqn() const
{
	return Line().Eqn();
}

// Convert this edge to a 2d line
Line2d Edge::Line() const
{
	return Line2d(m_vertex0->m_position, m_vertex1->m_position - m_vertex0->m_position);
}

// Return a direction vector that represents this edge
pr::v4 Edge::Direction() const
{
	return m_vertex1->m_position - m_vertex0->m_position;
}

// Return the face on the opposite side of the edge to 'face'
Face const* Edge::GetOtherFace(Face const* face) const
{
	PR_ASSERT(PR_DBG_TERRAIN, face == m_Lface || face == m_Rface, "");
	return face == m_Lface ? m_Rface : m_Lface;
}
