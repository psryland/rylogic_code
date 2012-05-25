//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "terrainexporter/forward.h"
#include "terrainexporter/debug.h"

namespace pr
{
	namespace terrain
	{
		struct LineEqn
		{
			float m_a;
			float m_b;
			float m_c;

			// Construct a 2D line from a point and edge
			LineEqn(pr::v4 const& point, pr::v4 const& edge)
			{
				// Derivation of line equation co-efficients
				//       Z = mX + k, where m = dZ / dX
				// => dX.Z = dZ.X + k
				//    dZ.X - dX.Z + k = 0
				// => aX + bZ + c = 0, where a = dZ, b = -dX, c = k
				m_a = edge[2];
				m_b = -edge[0];

				pr::v4 s = point;
				pr::v4 e = point + edge;

				// Find an average value for 'c' by using the start and end points of 'edge'
				// Note: aX + bZ = -c, therefore (c1 + c2)/2 = -c
				float c1 = m_a * s[0] + m_b * s[2];
				float c2 = m_a * e[0] + m_b * e[2];
				m_c = -(c1 + c2) / 2.0f;
			}
		
			// Normalise the line equation constants
			void Normalise()
			{
				// Normalise 'a', 'b', and 'c'
				PR_ASSERT(PR_DBG_TERRAIN, !(m_a == 0.0f && m_b == 0.0f && m_c == 0.0f), "");
				float scale = 1.0f / Sqrt(m_a*m_a + m_b*m_b + m_c*m_c);
				m_a *= scale;
				m_b *= scale;
				m_c *= scale;
			}

			// > 0 means left, < 0 means right
			float Evaluate(float x, float z) const
			{
				return m_a * x + m_b * z + m_c;
			}
			float Evaluate(pr::v4 const& point) const
			{
				return Evaluate(point.x, point.z);
			}
		};
	}//namespace terrain
}//namespace pr


