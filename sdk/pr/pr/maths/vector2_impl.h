//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR2_IMPL_H
#define PR_MATHS_VECTOR2_IMPL_H

#include "pr/maths/vector2.h"

namespace pr
{
	inline v2& v2::operator = (iv2 const& rhs)
	{
		return set(float(rhs.x), float(rhs.y));
	}
	inline v2& Zero(v2& v)
	{
		return v = pr::v2Zero;
	}
	inline bool IsFinite(v2 const& v)
	{
		return IsFinite(v.x) && IsFinite(v.y);
	}
	inline bool IsFinite(v2 const& v, float max_value)
	{
		return IsFinite(v.x, max_value) && IsFinite(v.y, max_value);
	}
	inline int SmallestElement2(v2 const& v)
	{
		return v.x > v.y;
	}
	inline int LargestElement2(v2 const& v)
	{
		return v.x < v.y;
	}
	inline v2 Abs(v2 const& v)
	{
		return v2::make(Abs(v.x), Abs(v.y));
	}
	inline v2 Trunc(v2 const& v)
	{
		return v2::make(Trunc(v.x), Trunc(v.y));
	}
	inline v2 Frac(v2 const& v)
	{
		return v2::make(Frac(v.x), Frac(v.y));
	}
	inline v2 Sqr(v2 const& v)
	{
		return v2::make(Sqr(v.x), Sqr(v.y));
	}
	inline float Dot2(v2 const& lhs, v2 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}
	inline float Cross2(v2 const& lhs, v2 const& rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y; // Dot2(Rotate90CW(lhs), rhs)
	}
	inline v2 Rotate90CW(v2 const& v)
	{
		return v2::make(-v.y, v.x);
	}
	inline v2 Rotate90CCW(v2 const& v)
	{
		return v2::make(v.y, -v.x);
	}
	inline v2 Quantise(v2 const& v, int pow2)
	{
		return v2::make(Quantise(v.x, pow2), Quantise(v.y, pow2));
	}
	inline v2 Lerp(v2 const& src, v2 const& dest, float frac)
	{
		return src + frac * (dest - src);
	}
	inline v2 SLerp2(v2 const& src, v2 const& dest, float frac)
	{
		float s_len = Length2(src), d_len = Length2(dest);
		return (s_len + frac*(d_len - s_len)) * Normalise2(src + frac*(dest - src));
	}
	inline uint Quadrant(v2 const& v)
	{
		// Returns a 2 bit bitmask of the quadrant the vector is in where X = 0x1, Y = 0x2
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1);
	}
	inline int Sector(v2 const& vec, int sectors)
	{
		// Divide a circle into 'sectors' and return an index for the sector that 'v' is in
		return int(pr::ATan2Positive(vec.y, vec.x) * sectors / maths::tau);
	}
	inline float CosAngle2(v2 const& lhs, v2 const& rhs)
	{
		// Return the cosine of the angle between two vectors
		assert(!IsZero2(lhs) && !IsZero2(rhs) && "CosAngle undefined for zero vectors");
		return Clamp(Dot2(lhs,rhs) / Sqrt(Length2Sq(lhs) * Length2Sq(rhs)), -1.0f, 1.0f);
	}
}

#endif
