//*******************************************************************************
// Terrain Exporter
//  Copyright © Rylogic Ltd 2009
//*******************************************************************************
#pragma once
#include "pr/maths/maths.h"
#include "terrainexporter/forward.h"
#include "terrainexporter/lineeqn.h"

namespace pr
{
	namespace terrain
	{
		struct Line2d
		{
			pr::v4	m_point;	// The start point of the line
			pr::v4	m_edge;		// The direction and length of the line
			float	m_t0;		// Parametric value for the start of the line
			float	m_t1;		// Parametric value for the end of the line

			Line2d()
			:m_point(pr::v4Origin)
			,m_edge (pr::v4Zero)
			,m_t0   (0.0f)
			,m_t1   (0.0f)
			{}

			Line2d(pr::v4 const& point, pr::v4 const& edge, float t0 = 0.0f, float t1 = 1.0f)
			:m_point(point)
			,m_edge(edge)
			,m_t0(t0)
			,m_t1(t1)
			{
				m_point[1] = 0.0f;
				m_edge [1] = 0.0f;
			}

			pr::v4	Start() const						{ return m_point + m_edge * m_t0; }
			pr::v4	End() const							{ return m_point + m_edge * m_t1; }
			pr::v4	Vector() const						{ return End() - Start(); }
			pr::v4	Normal() const						{ return GetNormal3(m_edge); }
			pr::v4	Midpoint() const					{ return m_point + m_edge * (0.5f * (m_t0 + m_t1)); }
			LineEqn	Eqn() const							{ return LineEqn(m_point, m_edge); }
			float	Length() const						{ return (m_t1 - m_t0) * Length3(m_edge); }

			// Returns the smallest signed distance from 'point' to this line
			float Distance(pr::v4 const& point) const
			{
				pr::v4 norm = pr::v4::make(m_edge.z, 0.0f, -m_edge.x, 0.0f);	// 90 degree CCW rotation
				float norm_length = Length3(norm);
				if( norm_length == 0.0f )	return Length3(point - Start());
				else						return Dot3(norm, point - m_point) / norm_length;
			}
		
			// Returns a copy of the line rotation by 90 degree CCW
			Line2d ccw90() const
			{
				Line2d result(m_point, pr::v4::make(m_edge.z, 0.0f, -m_edge.x, 0.0f));
				result.m_t0 = m_t0;
				result.m_t1 = m_t1;
				return result;
			}
		};

		// Unary minus
		inline Line2d operator -(Line2d const& line)
		{
			Line2d neg_line;
			neg_line.m_point	= line.m_point + line.m_edge;
			neg_line.m_edge		= -line.m_edge;
			neg_line.m_t0		= line.m_t1;
			neg_line.m_t1		= line.m_t0;
			return neg_line;
		}

	}//namespace terrain
}//namespace pr

