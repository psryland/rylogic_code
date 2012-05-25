//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_LINE3_H
#define PR_MATHS_LINE3_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	struct Line3
	{
		v4 m_point;
		v4 m_line;
		
		Line3& set(v4 const& point, v4 const& line)  { m_point = point; m_line = line; return *this; }
		v4     start() const                         { return m_point; }
		v4     end() const                           { return m_point + m_line; }
		v4     normal() const                        { return GetNormal3(m_line); }
		v4     operator()(float t) const             { return m_point + t * m_line; }
		
		static Line3 make(v4 const& point, v4 const& line) { Line3 l; return l.set(point, line); }
	};
	
	Line3 const Line3Zero  = {v4Origin, v4Zero};
	
	// Assignment operators
	inline Line3& operator += (Line3& lhs, v4 const& vec) { lhs.m_line += vec; return lhs; }
	inline Line3& operator -= (Line3& lhs, v4 const& vec) { lhs.m_line -= vec; return lhs; }
	inline Line3& operator *= (Line3& lhs, float s)       { lhs.m_line *= s; return lhs; }
	
	// Binary operators
	inline Line3 operator + (Line3 const& lhs, v4 const& vec) { Line3 l = lhs; return l += vec; }
	inline Line3 operator - (Line3 const& lhs, v4 const& vec) { Line3 l = lhs; return l -= vec; }
	inline Line3 operator * (Line3 const& lhs, float s)       { Line3 l = lhs; return l *= s; }
	inline Line3 operator * (float s, Line3 const& rhs)       { Line3 l = rhs; return l *= s; }
	inline Line3 operator * (m4x4 const& m, Line3 const& rhs) { return Line3::make(m * rhs.m_point, m * rhs.m_line); }
	
	// Unary operators
	inline Line3 operator + (Line3 const& line) { return line; }
	inline Line3 operator - (Line3 const& line) { return Line3::make(line.m_point, -line.m_line); }
	
	// Equality operators
	inline bool operator == (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (Line3 const& lhs, Line3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	
	// Functions
	float   Length3Sq(Line3 const& l);
	float   Length3(Line3 const& l);
}

#endif
