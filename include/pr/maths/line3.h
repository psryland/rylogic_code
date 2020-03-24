//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

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

		Line3() = default;
		Line3(v4 const& point, v4 const& line)
			:m_point(point)
			,m_line(line)
		{}

		// The origin of the line
		v4 start() const
		{
			return m_point;
		}

		// The end point of the line
		v4 end() const
		{
			return m_point + m_line;
		}

		// The normalised direction vector for the line
		v4 normal() const
		{
			return Normalise(m_line);
		}

		// The parametric position along the line
		v4 operator()(float t) const
		{
			return m_point + t * m_line;
		}

		#pragma region Operators
		friend Line3 operator + (Line3 const& line)
		{
			return line;
		}
		friend Line3 operator - (Line3 const& line)
		{
			return Line3(line.m_point, -line.m_line);
		}
		friend bool operator == (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) == 0;
		}
		friend bool operator != (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) != 0;
		}
		friend bool operator <  (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) < 0;
		}
		friend bool operator >  (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) > 0;
		}
		friend bool operator <= (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0;
		}
		friend bool operator >= (Line3 const& lhs, Line3 const& rhs)
		{
			return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0;
		}
		friend Line3& operator += (Line3& lhs, v4 const& vec)
		{
			lhs.m_line += vec;
			return lhs;
		}
		friend Line3& operator -= (Line3& lhs, v4 const& vec)
		{
			lhs.m_line -= vec;
			return lhs;
		}
		friend Line3& operator *= (Line3& lhs, float s)
		{
			lhs.m_line *= s;
			return lhs;
		}
		friend Line3 operator + (Line3 const& lhs, v4 const& vec)
		{
			auto l = lhs;
			return l += vec;
		}
		friend Line3 operator - (Line3 const& lhs, v4 const& vec)
		{
			auto l = lhs;
			return l -= vec;
		}
		friend Line3 operator * (Line3 const& lhs, float s)
		{
			auto l = lhs;
			return l *= s;
		}
		friend Line3 operator * (float s, Line3 const& rhs)
		{
			auto l = rhs;
			return l *= s;
		}
		friend Line3 operator * (m4x4 const& m, Line3 const& rhs)
		{
			return Line3(m * rhs.m_point, m * rhs.m_line);
		}
		#pragma endregion
	};
	static Line3 const Line3Zero  = {v4Origin, v4Zero};

	#pragma region Functions

	// Get the length of the line
	inline float LengthSq(Line3 const& l)
	{
		return LengthSq(l.m_line);
	}
	inline float Length(Line3 const& l)
	{
		return Length(l.m_line);
	}

	#pragma endregion
}

