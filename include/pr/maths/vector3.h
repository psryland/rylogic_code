//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR3_H
#define PR_MATHS_VECTOR3_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"

namespace pr
{
	struct v3
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x, y, z; };
			struct { v2 xy; };
		};
		#pragma warning(pop)
		typedef float Array[3];

		v3&                       set(float x_)                            { x = y = z = x_; return *this; }
		v3&                       set(float x_, float y_, float z_)        { x = x_; y = y_; z = z_; return *this; }
		template <typename T> v3& set(T const& v, float z_)                { x = GetXf(v); y = GetYf(v); z = z_; return *this; }
		template <typename T> v3& set(T const& v)                          { x = GetXf(v); y = GetYf(v); z = GetZf(v); return *this; }
		template <typename T> v3& set(T const* v)                          { x = AsReal(v[0]); y = AsReal(v[1]); z = AsReal(v[2]); return *this; }
		Array const&              ToArray() const                          { return reinterpret_cast<Array const&>(*this); }
		Array&                    ToArray()                                { return reinterpret_cast<Array&>      (*this); }
		float const&              operator [](int i) const                 { assert(i < 3); return ToArray()[i]; }
		float&                    operator [](int i)                       { assert(i < 3); return ToArray()[i]; }
		v2 vec2(int i0, int i1) const;
		v4 w0() const;
		v4 w1() const;

		static v3                       make(float x)                      { v3 vec; return vec.set(x); }
		static v3                       make(float x, float y, float z)    { v3 vec; return vec.set(x, y, z); }
		template <typename T> static v3 make(T const& v, float z)          { v3 vec; return vec.set(v, z); }
		template <typename T> static v3 make(T const& v)                   { v3 vec; return vec.set(v); }
		template <typename T> static v3 make(T const* v)                   { v3 vec; return vec.set(v); }
		static v3                       normal3(float x, float y, float z) { v3 vec; return Normalise3(vec.set(x,y,z)); }
	};
	static_assert(std::is_pod<v3>::value, "Should be a pod type");

	static v3 const v3Zero  = {0.0f, 0.0f, 0.0f};
	static v3 const v3Half  = {0.5f, 0.5f, 0.5f};
	static v3 const v3One   = {1.0f, 1.0f, 1.0f};
	static v3 const v3Min   = {maths::float_min, maths::float_min, maths::float_min};
	static v3 const v3Max   = {maths::float_max, maths::float_max, maths::float_max};
	static v3 const v3XAxis = {1.0f, 0.0f, 0.0f};
	static v3 const v3YAxis = {0.0f, 1.0f, 0.0f};
	static v3 const v3ZAxis = {0.0f, 0.0f, 1.0f};

	// Limits
	namespace maths
	{
		template <> struct limits<v3>
		{
			static v3 min() { return v3Min; }
			static v3 max() { return v3Min; }
		};
	}

	// Element accessors
	inline float GetX(v3 const& v) { return v.x; }
	inline float GetY(v3 const& v) { return v.y; }
	inline float GetZ(v3 const& v) { return v.z; }
	inline float GetW(v3 const&  ) { return 0.0f; }

	// Assignment operators
	template <typename T> inline v3& operator += (v3& lhs, T rhs) { lhs.x += GetXf(rhs); lhs.y += GetYf(rhs); lhs.z += GetZf(rhs); return lhs; }
	template <typename T> inline v3& operator -= (v3& lhs, T rhs) { lhs.x -= GetXf(rhs); lhs.y -= GetYf(rhs); lhs.z -= GetZf(rhs); return lhs; }
	template <typename T> inline v3& operator *= (v3& lhs, T rhs) { lhs.x *= GetXf(rhs); lhs.y *= GetYf(rhs); lhs.z *= GetZf(rhs); return lhs; }
	template <typename T> inline v3& operator /= (v3& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x /= GetXf(rhs);              lhs.y /= GetYf(rhs);              lhs.z /= GetZf(rhs);              return lhs; }
	template <typename T> inline v3& operator %= (v3& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x  = Fmod(lhs.x, GetXf(rhs)); lhs.y  = Fmod(lhs.y, GetYf(rhs)); lhs.z  = Fmod(lhs.z, GetZf(rhs)); return lhs; }

	// Binary operators
	template <typename T> inline v3 operator + (v3 const& lhs, T rhs) { v3 v = lhs; return v += rhs; }
	template <typename T> inline v3 operator - (v3 const& lhs, T rhs) { v3 v = lhs; return v -= rhs; }
	template <typename T> inline v3 operator * (v3 const& lhs, T rhs) { v3 v = lhs; return v *= rhs; }
	template <typename T> inline v3 operator / (v3 const& lhs, T rhs) { v3 v = lhs; return v /= rhs; }
	template <typename T> inline v3 operator % (v3 const& lhs, T rhs) { v3 v = lhs; return v %= rhs; }
	inline v3 operator + (float lhs, v3 const& rhs)                   { v3 v = rhs; return v += lhs; }
	inline v3 operator - (float lhs, v3 const& rhs)                   { v3 v = rhs; return v -= lhs; }
	inline v3 operator * (float lhs, v3 const& rhs)                   { v3 v = rhs; return v *= lhs; }
	inline v3 operator / (float lhs, v3 const& rhs)                   { assert(All3(rhs,maths::NonZero<float>)); return v3::make(     lhs /GetXf(rhs),       lhs /GetYf(rhs),       lhs /GetZf(rhs) ); }
	inline v3 operator % (float lhs, v3 const& rhs)                   { assert(All3(rhs,maths::NonZero<float>)); return v3::make(Fmod(lhs, GetXf(rhs)), Fmod(lhs, GetYf(rhs)), Fmod(lhs, GetZf(rhs))); }

	// Unary operators
	inline v3 operator + (v3 const& v) { return v; }
	inline v3 operator - (v3 const& v) { return v3::make(-v.x, -v.y, -v.z); }

	// Equality operators
	inline bool operator == (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (v3 const& lhs, v3 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Conversion functions between vector types
	inline v2 const& cast_v2(v3 const& vec) { return reinterpret_cast<v2 const&>(vec); }
	inline v2&       cast_v2(v3&       vec) { return reinterpret_cast<v2&>      (vec); }

	// Min/Max/Clamp
	template<> inline v3 Max(v3 const& lhs, v3 const& rhs)              { return v3::make(Max(lhs.x,rhs.x), Max(lhs.y,rhs.y), Max(lhs.z,rhs.z)); }
	template<> inline v3 Min(v3 const& lhs, v3 const& rhs)              { return v3::make(Min(lhs.x,rhs.x), Min(lhs.y,rhs.y), Min(lhs.z,rhs.z)); }
	template<> inline v3 Clamp(v3 const& x, v3 const& mn, v3 const& mx) { return v3::make(Clamp(x.x,mn.x,mx.x), Clamp(x.y,mn.y,mx.y), Clamp(x.z,mn.z,mx.z)); }
	inline v3            Clamp(v3 const& x, float mn, float mx)         { return v3::make(Clamp(x.x,mn,mx),     Clamp(x.y,mn,mx),     Clamp(x.z,mn,mx)); }

	// Functions
	v3&   Zero(v3& v);
	bool  IsFinite(v3 const& v);
	bool  IsFinite(v3 const& v, float max_value);
	int   SmallestElement2(v3 const& v);
	int   SmallestElement3(v3 const& v);
	int   LargestElement2(v3 const& v);
	int   LargestElement3(v3 const& v);
	v3    Abs(const v3& v);
	v3    Trunc(v3 const& v);
	v3    Frac(v3 const& v);
	v3    Sqr(v3 const& v);
	float Dot3(v3 const& lhs, v3 const& rhs);
	v3    Cross3(v3 const& lhs, v3 const& rhs);
	float Triple3(v3 const& a, v3 const& b, v3 const& c);
	v3    Quantise(v3 const& v, int pow2);
	v3    Lerp(v3 const& src, v3 const& dest, float frac);
	v3    SLerp3(v3 const& src, v3 const& dest, float frac);
	float CosAngle3(v3 const& lhs, v3 const& rhs);
	v3    Permute3(v3 const& v, int n);
}

#endif