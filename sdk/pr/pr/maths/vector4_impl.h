//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR4_IMPL_H
#define PR_MATHS_VECTOR4_IMPL_H

#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"

namespace pr
{
	inline v4& v4::operator = (iv4 const& rhs)
	{
		return set(float(rhs.x), float(rhs.y), float(rhs.z), float(rhs.w));
	}
	inline v2 v4::vec2(int i0, int i1) const
	{
		return pr::v2::make(ToArray()[i0], ToArray()[i1]);
	}
	inline v3 v4::vec3(int i0, int i1, int i2) const
	{
		return pr::v3::make(ToArray()[i0], ToArray()[i1], ToArray()[i2]);
	}
	inline v4& Zero(v4& v)
	{
		return v = pr::v4Zero;
	}
	inline bool IsFinite(v4 const& v)
	{
		return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z) && IsFinite(v.w);
	}
	inline bool  IsFinite(v4 const& v, float max_value)
	{
		return IsFinite(v.x, max_value) && IsFinite(v.y, max_value) && IsFinite(v.z, max_value) && IsFinite(v.w, max_value);
	}
	inline int SmallestElement2(v4 const& v)
	{
		return SmallestElement2(cast_v2(v));
	}
	inline int SmallestElement3(v4 const& v)
	{
		return SmallestElement3(cast_v3(v));
	}
	inline int SmallestElement4(v4 const& v)
	{
		int i = (v.x > v.y), j = (v.z > v.w) + 2;
		return (v[i] > v[j]) ? j : i;
	}
	inline int LargestElement2(v4 const& v)
	{
		return LargestElement2(cast_v2(v));
	}
	inline int LargestElement3(v4 const& v)
	{
		return LargestElement3(cast_v3(v));
	}
	inline int LargestElement4(v4 const& v)
	{
		int i = (v.x < v.y), j = (v.z < v.w) + 2;
		return (v[i] < v[j]) ? j : i;
	}
	inline v4 Abs(v4 const& v)
	{
		return v4::make(Abs(v.x), Abs(v.y), Abs(v.z), Abs(v.w));
	}
	inline v4 Trunc(v4 const& v)
	{
		return v4::make(Trunc(v.x), Trunc(v.y), Trunc(v.z), Trunc(v.w));
	}
	inline v4 Frac(v4 const& v)
	{
		return v4::make(Frac(v.x), Frac(v.y), Frac(v.z), Frac(v.w));
	}
	inline v4 Sqr(v4 const& v)
	{
		return v4::make(Sqr(v.x), Sqr(v.y), Sqr(v.z), Sqr(v.w));
	}
	inline float Dot3(v4 const& lhs, v4 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
	}
	inline float Dot4(v4 const& lhs, v4 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
	}
	inline v4 Cross3(v4 const& lhs, v4 const& rhs)
	{
		return v4::make(lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x, 0.0f);
	}
	inline float Triple3(v4 const& a, v4 const& b, v4 const& c)
	{
		return Dot3(a, Cross3(b, c));
	}
	inline v4 Quantise(v4 const& v, int pow2)
	{
		return v4::make(Quantise(v.x, pow2), Quantise(v.y, pow2), Quantise(v.z, pow2), Quantise(v.w, pow2));
	}
	inline v4 Lerp(v4 const& src, v4 const& dest, float frac)
	{
		return src + frac * (dest - src);
	}
	inline v4 SLerp3(v4 const& src, v4 const& dest, float frac)
	{
		v4 vec = src;
		cast_v3(vec) = SLerp3(cast_v3(src), cast_v3(dest), frac);
		return vec;
	}
	
	// returns +1 if all are positive, -1 if all are negative, or 0 if there's a mixture
	inline int SignCombined3(v4 const& v)
	{
		int p = (v.x>0.0f) + (v.y>0.0f) + (v.z>0.0f);
		int n = (v.x<0.0f) + (v.y<0.0f) + (v.z<0.0f);
		return (p == 3) - (n == 3);
	}
	// returns +1 if all are positive, -1 if all are negative, or 0 if there's a mixture
	inline int SignCombined4(v4 const& v)
	{
		int p = (v.x>0.0f) + (v.y>0.0f) + (v.z>0.0f) + (v.w>0.0f);
		int n = (v.x<0.0f) + (v.y<0.0f) + (v.z<0.0f) + (v.w<0.0f);
		return (p == 4) - (n == 4);
	}
	inline bool Parallel(v4 const& v0, v4 const& v1, float tol)
	{
		return Length3Sq(Cross3(v0, v1)) <= tol;
	}
	inline v4 CreateNotParallelTo(v4 const& v)
	{
		bool x_aligned = Abs(v.x) > Abs(v.y) && Abs(v.x) > Abs(v.z);
		return v4::make(static_cast<float>(!x_aligned), 0.0f, static_cast<float>(x_aligned), v.w);
	}
	inline v4 Perpendicular(v4 const& v)
	{
		PR_ASSERT(PR_DBG_MATHS, !IsZero3(v), "Cannot make a perpendicular to a zero vector");
		v4 vec = Cross3(v, CreateNotParallelTo(v));
		vec *= Length3(v) / Length3(vec);
		return vec;
	}
	inline uint Octant(v4 const& v)
	{
		// Returns a 3 bit bitmask of the octant the vector is in where X = 0x1, Y = 0x2, Z = 0x4    
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1) | ((v.z >= 0.0f) << 2);
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	inline v4 RotationVectorApprox(m3x3 const& from, m3x3 const& to)
	{
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(from) && IsOrthonormal(to), "This only works for orthonormal matrices");
		m3x3 cpm_x_i2wR = to - from;
		m3x3 w2iR = GetTranspose(from);
		m3x3 cpm = cpm_x_i2wR * w2iR;
		return v4::make(cpm.y.z, cpm.z.x, cpm.x.y, 0.0f);
	}
	inline v4 RotationVectorApprox(m4x4 const& from, m4x4 const& to)
	{
		PR_ASSERT(PR_DBG_MATHS, IsOrthonormal(from) && IsOrthonormal(to), "This only works for orthonormal matrices");

		m4x4 cpm_x_i2wR = to - from;
		m4x4 w2iR = GetTranspose3x3(from); Zero(w2iR.pos);
		m4x4 cpm = cpm_x_i2wR * w2iR;
		return v4::make(cpm.y.z, cpm.z.x, cpm.x.y, 0.0f);
	}
	inline float CosAngle3(v4 const& lhs, v4 const& rhs)
	{
		return CosAngle3(cast_v3(lhs), cast_v3(rhs));
	}
}

#endif
