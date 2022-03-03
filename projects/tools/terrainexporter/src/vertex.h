//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"

namespace pr
{
	namespace terrain
	{
		struct Vertex
		{
			pr::v4 m_position;	// The quantised vertex position
		};

		inline bool operator == (Vertex const& lhs, Vertex const& rhs)
		{
			return	lhs.m_position == rhs.m_position;
		}
		inline bool operator != (Vertex const& lhs, Vertex const& rhs)
		{
			return !(lhs == rhs);
		}
		inline bool operator < (Vertex const& lhs, Vertex const& rhs)
		{
			if (lhs.m_position.x != rhs.m_position.x)	return lhs.m_position.x < rhs.m_position.x;
			if (lhs.m_position.y != rhs.m_position.y)	return lhs.m_position.y < rhs.m_position.y;
														return lhs.m_position.z < rhs.m_position.z;
		}
		inline bool operator > (Vertex const& lhs, Vertex const& rhs)
		{
			if (lhs.m_position.x != rhs.m_position.x)	return lhs.m_position.x > rhs.m_position.x;
			if (lhs.m_position.y != rhs.m_position.y)	return lhs.m_position.y > rhs.m_position.y;
														return lhs.m_position.z > rhs.m_position.z;
		}
	}//namespace terrain
}//namespace pr
