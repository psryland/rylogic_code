//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR4_H
#define PR_MATHS_VECTOR4_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"
#include "pr/maths/ivector2.h"
#include "pr/maths/ivector4.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"

namespace pr
{
	__declspec(align(16)) struct v4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x,y,z,w; };
			#if PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec;
			#elif PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#endif
		};
		#pragma warning(pop)
		typedef float Array[4];

		v4&                       set(float x_);
		v4&                       set(float x_, float y_, float z_, float w_);
		template <typename T> v4& set(T const& v, float z_, float w_)         { x = GetXf(v); y = GetYf(v); z = z_; w = w_; return *this; }
		template <typename T> v4& set(T const& v, float w_)                   { x = GetXf(v); y = GetYf(v); z = GetZf(v); w = w_; return *this; }
		template <typename T> v4& set(T const& v)                             { x = GetXf(v); y = GetYf(v); z = GetZf(v); w = GetWf(v); return *this; }
		template <typename T> v4& set(T const* v)                             { x = AsReal(v[0]); y = AsReal(v[1]); z = AsReal(v[2]); w = AsReal(v[3]); return *this; }
		template <typename T> v4& set(T const* v, float w_)                   { x = AsReal(v[0]); y = AsReal(v[1]); z = AsReal(v[2]); w = w_; return *this; }
		v2 const&                 xy() const                                  { return reinterpret_cast<v2 const&>(x); }
		v2&                       xy()                                        { return reinterpret_cast<v2&>      (x); }
		v2 const&                 yz() const                                  { return reinterpret_cast<v2 const&>(y); }
		v2&                       yz()                                        { return reinterpret_cast<v2&>      (y); }
		v2 const&                 zw() const                                  { return reinterpret_cast<v2 const&>(z); }
		v2&                       zw()                                        { return reinterpret_cast<v2&>      (z); }
		v3 const&                 xyz() const                                 { return reinterpret_cast<v3 const&>(x); }
		v3&                       xyz()                                       { return reinterpret_cast<v3&>      (x); }
		v3 const&                 yzw() const                                 { return reinterpret_cast<v3 const&>(y); }
		v3&                       yzw()                                       { return reinterpret_cast<v3&>      (y); }
		v4                        w0() const                                  { pr::v4 v = *this; v.w = 0.0f; return v; }
		v4                        w1() const                                  { pr::v4 v = *this; v.w = 1.0f; return v; }
		Array const&              ToArray() const                             { return reinterpret_cast<Array const&>(*this); }
		Array&                    ToArray()                                   { return reinterpret_cast<Array&>      (*this); }
		float const&              operator [] (int i) const                   { assert(i < 4); return ToArray()[i]; }
		float&                    operator [] (int i)                         { assert(i < 4); return ToArray()[i]; }
		v4& operator = (iv4 const& rhs);
		v2  vec2(int i0, int i1) const;
		v3  vec3(int i0, int i1, int i2) const;

		static v4                       make(float x)                               { v4 vec; return vec.set(x); }
		static v4                       make(float x, float y, float z, float w)    { v4 vec; return vec.set(x, y, z, w); }
		template <typename T> static v4 make(T const& v, float z, float w)          { v4 vec; return vec.set(v, z, w); }
		template <typename T> static v4 make(T const& v, float w)                   { v4 vec; return vec.set(v, w); }
		template <typename T> static v4 make(T const& v)                            { v4 vec; return vec.set(v); }
		template <typename T> static v4 make(T const* v)                            { v4 vec; return vec.set(v); }
		template <typename T> static v4 make(T const* v, float w)                   { v4 vec; return vec.set(v, w); }
		static v4                       normal3(float x, float y, float z, float w) { v4 vec; return Normalise3(vec.set(x, y, z, w)); }
		static v4                       normal4(float x, float y, float z, float w) { v4 vec; return Normalise4(vec.set(x, y, z, w)); }
	};
	static_assert(std::alignment_of<v4>::value == 16, "v4 should have 16 byte alignment");
	static_assert(std::is_pod<v4>::value, "Should be a pod type");

	v4 const v4Zero   = {0.0f, 0.0f, 0.0f, 0.0f};
	v4 const v4One    = {1.0f, 1.0f, 1.0f, 1.0f};
	v4 const v4Min    = {maths::float_min, maths::float_min, maths::float_min, maths::float_min};
	v4 const v4Max    = {maths::float_max, maths::float_max, maths::float_max, maths::float_max};
	v4 const v4XAxis  = {1.0f, 0.0f, 0.0f, 0.0f};
	v4 const v4YAxis  = {0.0f, 1.0f, 0.0f, 0.0f};
	v4 const v4ZAxis  = {0.0f, 0.0f, 1.0f, 0.0f};
	v4 const v4Origin = {0.0f, 0.0f, 0.0f, 1.0f};

	// Limits
	namespace maths
	{
		template <> struct limits<v4>
		{
			static v4 min() { return v4Min; }
			static v4 max() { return v4Max; }
		};
	}

	// Element access
	inline float GetX(v4 const& v) { return v.x; }
	inline float GetY(v4 const& v) { return v.y; }
	inline float GetZ(v4 const& v) { return v.z; }
	inline float GetW(v4 const& v) { return v.w; }

	// Assignment operators
	template <typename T> inline v4& operator += (v4& lhs, T const& rhs) { lhs.x += GetXf(rhs); lhs.y += GetYf(rhs); lhs.z += GetZf(rhs); lhs.w += GetWf(rhs); return lhs; }
	template <typename T> inline v4& operator -= (v4& lhs, T const& rhs) { lhs.x -= GetXf(rhs); lhs.y -= GetYf(rhs); lhs.z -= GetZf(rhs); lhs.w -= GetWf(rhs); return lhs; }
	template <typename T> inline v4& operator *= (v4& lhs, T const& rhs) { lhs.x *= GetXf(rhs); lhs.y *= GetYf(rhs); lhs.z *= GetZf(rhs); lhs.w *= GetWf(rhs); return lhs; }
	template <typename T> inline v4& operator /= (v4& lhs, T const& rhs) { assert(!IsZero4(rhs)); lhs.x /= GetXf(rhs);              lhs.y /= GetYf(rhs);              lhs.z /= GetZf(rhs);              lhs.w /= GetWf(rhs);              return lhs; }
	template <typename T> inline v4& operator %= (v4& lhs, T const& rhs) { assert(!IsZero4(rhs)); lhs.x  = Fmod(lhs.x, GetXf(rhs)); lhs.y  = Fmod(lhs.y, GetYf(rhs)); lhs.z  = Fmod(lhs.z, GetZf(rhs)); lhs.w  = Fmod(lhs.w, GetWf(rhs)); return lhs; }

	// Binary operators
	template <typename T> inline v4 operator + (v4 const& lhs, T const& rhs) { v4 v = lhs; return v += rhs; }
	template <typename T> inline v4 operator - (v4 const& lhs, T const& rhs) { v4 v = lhs; return v -= rhs; }
	template <typename T> inline v4 operator * (v4 const& lhs, T const& rhs) { v4 v = lhs; return v *= rhs; }
	template <typename T> inline v4 operator / (v4 const& lhs, T const& rhs) { v4 v = lhs; return v /= rhs; }
	template <typename T> inline v4 operator % (v4 const& lhs, T const& rhs) { v4 v = lhs; return v %= rhs; }
	inline v4 operator + (float lhs, v4 const& rhs)                          { v4 v = rhs; return v += lhs; }
	inline v4 operator - (float lhs, v4 const& rhs)                          { v4 v = rhs; return v -= lhs; }
	inline v4 operator * (float lhs, v4 const& rhs)                          { v4 v = rhs; return v *= lhs; }
	inline v4 operator / (float lhs, v4 const& rhs)                          { assert(All4(rhs,maths::NonZero<float>)); return v4::make(     lhs /GetXf(rhs),       lhs /GetYf(rhs),       lhs /GetZf(rhs),       lhs /GetWf(rhs)); }
	inline v4 operator % (float lhs, v4 const& rhs)                          { assert(All4(rhs,maths::NonZero<float>)); return v4::make(Fmod(lhs, GetXf(rhs)), Fmod(lhs, GetYf(rhs)), Fmod(lhs, GetZf(rhs)), Fmod(lhs, GetWf(rhs))); }

	// Unary operators
	inline v4 operator + (v4 const& v) { return v; }
	inline v4 operator - (v4 const& v) { return v4::make(-v.x, -v.y, -v.z, -v.w); }

	// Equality operators
	inline bool operator == (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (v4 const& lhs, v4 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// DirectXMath conversion functions
	#if PR_MATHS_USE_DIRECTMATH
	inline DirectX::XMVECTOR const& dxv4(v4 const& v) { return v.vec; }
	inline DirectX::XMVECTOR&       dxv4(v4&       v) { return v.vec; }
	#endif

	// Conversion functions between vector types
	inline v2 const&   cast_v2(v4 const& vec)   { return reinterpret_cast<v2 const&>  (vec); }
	inline v2&         cast_v2(v4& vec)         { return reinterpret_cast<v2&>        (vec); }
	inline v3 const&   cast_v3(v4 const& vec)   { return reinterpret_cast<v3 const&>  (vec); }
	inline v3&         cast_v3(v4& vec)         { return reinterpret_cast<v3&>        (vec); }
	inline Quat const& cast_q (v4 const& vec)   { return reinterpret_cast<Quat const&>(vec); }
	inline Quat&       cast_q (v4& vec)         { return reinterpret_cast<Quat&>      (vec); }

	// Min/Max/Clamp
	template <> inline v4 Max(v4 const& lhs, v4 const& rhs)              { return v4::make(Max(lhs.x,rhs.x), Max(lhs.y,rhs.y), Max(lhs.z,rhs.z), Max(lhs.w,rhs.w)); }
	template <> inline v4 Min(v4 const& lhs, v4 const& rhs)              { return v4::make(Min(lhs.x,rhs.x), Min(lhs.y,rhs.y), Min(lhs.z,rhs.z), Min(lhs.w,rhs.w)); }
	template <> inline v4 Clamp(v4 const& x, v4 const& mn, v4 const& mx) { return v4::make(Clamp(x.x,mn.x,mx.x), Clamp(x.y,mn.y,mx.y), Clamp(x.z,mn.z,mx.z), Clamp(x.w,mn.w,mx.w)); }
	inline v4             Clamp(v4 const& x, float mn, float mx)         { return v4::make(Clamp(x.x,mn,mx),     Clamp(x.y,mn,mx),     Clamp(x.z,mn,mx),     Clamp(x.w,mn,mx));     }

	// Functions
	v4&     Zero(v4& v);
	bool    IsFinite(v4 const& v);
	bool    IsFinite(v4 const& v, float max_value);
	int     SmallestElement2(v4 const& v);
	int     SmallestElement3(v4 const& v);
	int     SmallestElement4(v4 const& v);
	int     LargestElement2(v4 const& v);
	int     LargestElement3(v4 const& v);
	int     LargestElement4(v4 const& v);
	v4      Normalise3(v4 const& v);
	v4      Normalise4(v4 const& v);
	v4      Abs(v4 const& v);
	v4      Trunc(v4 const& v);
	v4      Frac(v4 const& v);
	v4      Sqr(v4 const& v);
	float   Dot3(v4 const& lhs, v4 const& rhs);
	float   Dot4(v4 const& lhs, v4 const& rhs);
	v4      Cross3(v4 const& lhs, v4 const& rhs);
	float   Triple3(v4 const& a, v4 const& b, v4 const& c);
	v4      Quantise(v4 const& v, int pow2);
	v4      Lerp(v4 const& src, v4 const& dest, float frac);
	v4      SLerp3(v4 const& src, v4 const& dest, float frac);
	int     SignCombined3(v4 const& v);
	int     SignCombined4(v4 const& v);
	bool    Parallel(v4 const& v0, v4 const& v1, float tol = 0.0f);
	v4      CreateNotParallelTo(v4 const& v);
	v4      Perpendicular(v4 const& v);
	v4      Permute3(v4 const& v, int n);
	uint    Octant(v4 const& v);
	v4      RotationVectorApprox(m3x4 const& from, m3x4 const& to);
	v4      RotationVectorApprox(m4x4 const& from, m4x4 const& to);
	float   CosAngle3(v4 const& lhs, v4 const& rhs);
}

#endif
