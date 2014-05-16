//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR3_IMPL_H
#define PR_MATHS_VECTOR3_IMPL_H

#include "pr/maths/vector3.h"

namespace pr
{
	inline v2 v3::vec2(int i0, int i1) const
	{
		return pr::v2::make(ToArray()[i0], ToArray()[i1]);
	}
	inline v3& Zero(v3& v)
	{
		return v = pr::v3Zero;
	}
	inline bool IsFinite(v3 const& v)
	{
		return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z);
	}
	inline bool IsFinite(v3 const& v, float max_value)
	{
		return IsFinite(v.x, max_value) && IsFinite(v.y, max_value) && IsFinite(v.z, max_value);
	}
	inline int SmallestElement2(v3 const& v)
	{
		return SmallestElement2(cast_v2(v));
	}
	inline int SmallestElement3(v3 const& v)
	{
		int i = (v.y > v.z) + 1;
		return (v.x > v[i]) * i;
	}
	inline int LargestElement2(v3 const& v)
	{
		return LargestElement2(cast_v2(v));
	}
	inline int LargestElement3(v3 const& v)
	{
		int i = (v.y < v.z) + 1;
		return (v.x < v[i]) * i;
	}
	inline v3 Abs(v3 const& v)
	{
		return v3::make(Abs(v.x), Abs(v.y), Abs(v.z));
	}
	inline v3 Trunc(v3 const& v)
	{
		return v3::make(Trunc(v.x), Trunc(v.y), Trunc(v.z));
	}
	inline v3 Frac(v3 const& v)
	{
		return v3::make(Frac(v.x), Frac(v.y), Frac(v.z));
	}
	inline v3 Sqr(v3 const& v)
	{
		return v3::make(Sqr(v.x), Sqr(v.y), Sqr(v.z));
	}
	inline float Dot3(v3 const& lhs, v3 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
	}
	inline v3 Cross3(v3 const& lhs, v3 const& rhs)
	{
		return v3::make(lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x);
	}
	inline float Triple3(v3 const& a, v3 const& b, v3 const& c)
	{
		return Dot3(a, Cross3(b, c));
	}
	inline v3 Quantise(v3 const& v, int pow2)
	{
		return v3::make(Quantise(v.x, pow2), Quantise(v.y, pow2), Quantise(v.z, pow2));
	}
	inline v3 Lerp(v3 const& src, v3 const& dest, float frac)
	{
		return src + frac * (dest - src);
	}
	inline v3 SLerp3(v3 const& src, v3 const& dest, float frac)
	{
		float s_len = Length3(src), d_len = Length3(dest);
		return (s_len + frac*(d_len - s_len)) * Normalise3(src + frac*(dest - src));
	}
	inline float CosAngle3(v3 const& lhs, v3 const& rhs)
	{
		// Return the cosine of the angle between two vectors
		assert(!IsZero3(lhs) && !IsZero3(rhs) && "CosAngle undefined for zero vectors");
		return Clamp(Dot3(lhs,rhs) / Sqrt(Length3Sq(lhs) * Length3Sq(rhs)), -1.0f, 1.0f);
	}
	inline v3 Permute3(v3 const& v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return v3::make(v.y, v.z, v.x);
		case 2:  return v3::make(v.z, v.x, v.y);
		}
	}
}

#endif
