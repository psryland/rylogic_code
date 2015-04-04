//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR4_IMPL_H
#define PR_MATHS_VECTOR4_IMPL_H

#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x3.h"

namespace pr
{
	inline v4& v4::set(float x_)
	{
		#if PR_MATHS_USE_DIRECTMATH
		vec = DirectX::XMVectorReplicate(x_);
		#elif PR_MATHS_USE_INTRINSICS
		vec = _mm_set1_ps(x_);
		#else
		x = y = z = w = x_;
		#endif
		return *this;
	}
	inline v4& v4::set(float x_, float y_, float z_, float w_)
	{
		#if PR_MATHS_USE_DIRECTMATH
		vec = DirectX::XMVectorSet(x_, y_, z_, w_);
		#elif PR_MATHS_USE_INTRINSICS
		vec = _mm_set_ps(w_,z_,y_,x_);
		#else
		x = x_; y = y_; z = z_; w = w_;
		#endif
		return *this;
	}
	#if PR_MATHS_USE_INTRINSICS
	inline v4& v4::set(__m128 v)
	{
		vec = v;
		return *this;
	}
	#endif
	template <typename T> inline v4& v4::set(T const& v, float z_, float w_)
	{
		return set(GetXf(v), GetYf(v), z_, w_);
	}
	template <typename T> inline v4& v4::set(T const& v, float w_)
	{
		return set(GetXf(v), GetYf(v), GetZf(v), w_);
	}
	template <typename T> inline v4& v4::set(T const& v)
	{
		return set(GetXf(v), GetYf(v), GetZf(v), GetWf(v));
	}
	template <typename T> inline v4& v4::set(T const* v)
	{
		return set(AsReal(v[0]), AsReal(v[1]), AsReal(v[2]), AsReal(v[3]));
	}
	template <typename T> inline v4& v4::set(T const* v, float w_)
	{
		return set(AsReal(v[0]), AsReal(v[1]), AsReal(v[2]), w_);
	}

	inline v4 v4::w0() const { return v4::make(x, y, z, 0.0f); }
	inline v4 v4::w1() const { return v4::make(x, y, z, 1.0f); }

	inline v4::Array const& v4::ToArray() const       { return reinterpret_cast<Array const&>(*this); }
	inline v4::Array&       v4::ToArray()             { return reinterpret_cast<Array&>      (*this); }
	inline float const& v4::operator [] (int i) const { assert(i < 4); return ToArray()[i]; }
	inline float&       v4::operator [] (int i)       { assert(i < 4); return ToArray()[i]; }

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

	// Min/Max/Clamp
	inline v4 Max(v4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4::make(_mm_max_ps(lhs.vec, rhs.vec));
		#else
		return v4::make(Max(lhs.x,rhs.x), Max(lhs.y,rhs.y), Max(lhs.z,rhs.z), Max(lhs.w,rhs.w)); }
		#endif
	}
	inline v4 Min(v4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4::make(_mm_min_ps(lhs.vec,rhs.vec));
		#else
		return v4::make(Min(lhs.x,rhs.x), Min(lhs.y,rhs.y), Min(lhs.z,rhs.z), Min(lhs.w,rhs.w));
		#endif
	}
	inline v4 Clamp(v4 const& x, v4 const& mn, v4 const& mx)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4::make(_mm_max_ps(mn.vec, _mm_min_ps(mx.vec, x.vec)));
		#else
		return v4::make(Clamp(x.x,mn.x,mx.x), Clamp(x.y,mn.y,mx.y), Clamp(x.z,mn.z,mx.z), Clamp(x.w,mn.w,mx.w));
		#endif
	}
	inline v4 Clamp(v4 const& x, float mn, float mx)
	{
		v4 mn_ = {mn,mn,mn,mn};
		v4 mx_ = {mx,mx,mx,mx};
		return Clamp(x, mn_, mx_);
	}

	inline bool IsFinite(v4 const& v)
	{
		return IsFinite(v.x) && IsFinite(v.y) && IsFinite(v.z) && IsFinite(v.w);
	}
	inline bool IsFinite(v4 const& v, float max_value)
	{
		return IsFinite(v.x, max_value) && IsFinite(v.y, max_value) && IsFinite(v.z, max_value) && IsFinite(v.w, max_value);
	}
	inline int SmallestElement2(v4 const& v)
	{
		return SmallestElement2(v.xy);
	}
	inline int SmallestElement3(v4 const& v)
	{
		return SmallestElement3(v.xyz);
	}
	inline int SmallestElement4(v4 const& v)
	{
		int i = (v.x > v.y), j = (v.z > v.w) + 2;
		return (v[i] > v[j]) ? j : i;
	}
	inline int LargestElement2(v4 const& v)
	{
		return LargestElement2(v.xy);
	}
	inline int LargestElement3(v4 const& v)
	{
		return LargestElement3(v.xyz);
	}
	inline int LargestElement4(v4 const& v)
	{
		int i = (v.x < v.y), j = (v.z < v.w) + 2;
		return (v[i] < v[j]) ? j : i;
	}
	inline float Length2Sq(v4 const& v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0x31).m128_f32[0];
		#else
		return Len2Sq(v.x, v.y);
		#endif
	}
	inline float Length3Sq(v4 const& v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0x71).m128_f32[0];
		#else
		return Len3Sq(v.x, v.y, v.z);
		#endif
	}
	inline float Length4Sq(v4 const& v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0xF1).m128_f32[0];
		#else
		return Len4Sq(v.x, v.y, v.z, v.w);
		#endif
	}
	inline float Length2(v4 const& x)
	{
		return Sqrt(Length2Sq(x));
	}
	inline float Length3(v4 const& x)
	{
		return Sqrt(Length3Sq(x));
	}
	inline float Length4(v4 const& x)
	{
		return Sqrt(Length4Sq(x));
	}
	inline v4 Normalise3(v4 const& v)
	{
		#if PR_MATHS_USE_DIRECTMATH
		v4 vec; dxv4(vec) = DirectX::XMVector3Normalize(v.vec);
		return vec;
		#elif PR_MATHS_USE_INTRINSICS
		auto len = _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0x7F)); // _mm_rsqrt_ps isn't accurate enough
		return v4::make(_mm_div_ps(v.vec, len));
		#else
		return v / Length3(v);
		#endif
	}
	inline v4 Normalise4(v4 const& v)
	{
		#if PR_MATHS_USE_DIRECTMATH
		v4 vec; dxv4(vec) = DirectX::XMVector4Normalize(v.vec);
		return vec;
		#elif PR_MATHS_USE_INTRINSICS
		auto len = _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0xFF));
		return v4::make(_mm_div_ps(v.vec, len));
		#else
		return v / Length4(v);
		#endif
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
		#if PR_MATHS_USE_INTRINSICS
		return v4::make(_mm_mul_ps(v.vec, v.vec));
		#else
		return v4::make(Sqr(v.x), Sqr(v.y), Sqr(v.z), Sqr(v.w));
		#endif
	}
	inline float Dot3(v4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto r = _mm_dp_ps(lhs.vec, rhs.vec, 0x71);
		return r.m128_f32[0];
		#else
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
		#endif
	}
	inline float Dot4(v4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto r = _mm_dp_ps(lhs.vec, rhs.vec, 0xF1);
		return r.m128_f32[0];
		#else
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
		#endif
	}
	inline v4 Cross3(v4 const& lhs, v4 const& rhs)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4::make(_mm_sub_ps(
			_mm_mul_ps(_mm_shuffle_ps(lhs.vec, lhs.vec, _MM_SHUFFLE(3, 0, 2, 1)), _mm_shuffle_ps(rhs.vec, rhs.vec, _MM_SHUFFLE(3, 1, 0, 2))), 
			_mm_mul_ps(_mm_shuffle_ps(lhs.vec, lhs.vec, _MM_SHUFFLE(3, 1, 0, 2)), _mm_shuffle_ps(rhs.vec, rhs.vec, _MM_SHUFFLE(3, 0, 2, 1)))
			));
		#else
		return v4::make(lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x, 0.0f);
		#endif
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
		vec.xyz = SLerp3(src.xyz, dest.xyz, frac);
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
		assert(!IsZero3(v) && "Cannot make a perpendicular to a zero vector");
		v4 vec = Cross3(v, CreateNotParallelTo(v));
		vec *= Length3(v) / Length3(vec);
		return vec;
	}
	inline v4 Permute3(v4 const& v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return v4::make(v.y, v.z, v.x, v.w);
		case 2:  return v4::make(v.z, v.x, v.y, v.w);
		}
	}
	inline uint Octant(v4 const& v)
	{
		// Returns a 3 bit bitmask of the octant the vector is in where X = 0x1, Y = 0x2, Z = 0x4
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1) | ((v.z >= 0.0f) << 2);
	}

	// Return a vector representing the approximate rotation between two orthonormal transforms
	inline v4 RotationVectorApprox(m3x4 const& from, m3x4 const& to)
	{
		assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");
		m3x4 cpm_x_i2wR = to - from;
		m3x4 w2iR = Transpose3x3(from);
		m3x4 cpm = cpm_x_i2wR * w2iR;
		return v4::make(cpm.y.z, cpm.z.x, cpm.x.y, 0.0f);
	}
	inline v4 RotationVectorApprox(m4x4 const& from, m4x4 const& to)
	{
		assert(IsOrthonormal(from) && IsOrthonormal(to) && "This only works for orthonormal matrices");

		m4x4 cpm_x_i2wR = to - from;
		m4x4 w2iR = Transpose3x3_(from); w2iR.pos = v4Zero;
		m4x4 cpm = cpm_x_i2wR * w2iR;
		return v4::make(cpm.y.z, cpm.z.x, cpm.x.y, 0.0f);
	}
	inline float CosAngle3(v4 const& lhs, v4 const& rhs)
	{
		return CosAngle3(lhs.xyz, rhs.xyz);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_vector4)
		{
			#if PR_MATHS_USE_DIRECTMATH
			{
				v4 V0 = v4::make(1,2,3,4);
				DirectX::XMVECTORF32 VX0;
				VX0.v = dxv4(V0);
				PR_CHECK(V0.x, VX0.f[0]);
				PR_CHECK(V0.y, VX0.f[1]);
				PR_CHECK(V0.z, VX0.f[2]);
				PR_CHECK(V0.w, VX0.f[3]);
			}
			#endif
			{
				v4 a; a.set(1,2,-3,-4);
				PR_CHECK(a.x, +1);
				PR_CHECK(a.y, +2);
				PR_CHECK(a.z, -3);
				PR_CHECK(a.w, -4);
			}
			{
				v4 a = v4::make(3,-1,2,-4);
				v4 b = {-2,-1,4,2};
				PR_CHECK(Max(a,b), v4::make(3,-1,4,2));
				PR_CHECK(Min(a,b), v4::make(-2,-1,2,-4));
			}
			{
				v4 a = v4::make(3,-1,2,-4);
				PR_CHECK(Length2Sq(a), a.x*a.x + a.y*a.y);
				PR_CHECK(Length2(a), sqrt(Length2Sq(a)));
				PR_CHECK(Length3Sq(a), a.x*a.x + a.y*a.y + a.z*a.z);
				PR_CHECK(Length3(a), sqrt(Length3Sq(a)));
				PR_CHECK(Length4Sq(a), a.x*a.x + a.y*a.y + a.z*a.z + a.w*a.w);
				PR_CHECK(Length4(a), sqrt(Length4Sq(a)));
			}
			{
				v4 a = v4::make(3,-1,2,-4);
				v4 b = Normalise3(a);
				v4 c = Normalise4(a);
				PR_CHECK(Length3(b), 1.0f);
				PR_CHECK(b.w, a.w / Length3(a));
				PR_CHECK(sqrt(c.x*c.x + c.y*c.y + c.z*c.z + c.w*c.w), 1.0f);
				PR_CHECK(IsNormal3(a), false);
				PR_CHECK(IsNormal4(a), false);
				PR_CHECK(IsNormal3(b), true);
				PR_CHECK(IsNormal4(c), true);
			}
			{
				PR_CHECK(IsZero3(pr::v4::make(0,0,0,1)), true);
				PR_CHECK(IsZero4(pr::v4Zero), true);
				PR_CHECK(FEql3(pr::v4::make(1e-20f,0,0,1)     , pr::v4Zero), true);
				PR_CHECK(FEql4(pr::v4::make(1e-20f,0,0,1e-19f), pr::v4Zero), true);
			}
			{
				v4 a = {-2,  4,  2,  6};
				v4 b = { 3, -5,  2, -4};
				m4x4 a2b = CrossProductMatrix4x4(a);

				v4 c = Cross3(a,b);
				v4 d = a2b * b;
				PR_CHECK(FEql3(c,d), true);
			}
			{
				v4 a = {-2,  4,  2,  6};
				v4 b = { 3, -5,  2, -4};
				PR_CHECK(Dot4(a,b), -46);
				PR_CHECK(Dot3(a,b), -22);
			}
		}
	}
}
#endif

#endif
