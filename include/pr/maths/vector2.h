//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_VECTOR2_H
#define PR_MATHS_VECTOR2_H

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/scalar.h"

namespace pr
{
	struct v2
	{
		float x, y;
		typedef float Array[2];

		v2&                       set(float x_);
		v2&                       set(float x_, float y_);
		template <typename T> v2& set(T const& v);
		template <typename T> v2& set(T const* v);
		Array const&              ToArray() const;
		Array&                    ToArray();
		float const&              operator [] (int i) const;
		float&                    operator [] (int i);
		v2& operator = (iv2 const& rhs);

		static v2                       make(float x)             { v2 vec; return vec.set(x); }
		static v2                       make(float x, float y)    { v2 vec; return vec.set(x, y); }
		template <typename T> static v2 make(T const& v)          { v2 vec; return vec.set(v); }
		template <typename T> static v2 make(T const* v)          { v2 vec; return vec.set(v); }
		static v2                       normal2(float x, float y) { v2 vec; return Normalise2(vec.set(x,y)); }
	};
	static_assert(std::is_pod<v2>::value, "Should be a pod type");

	static v2 const v2Zero    = {0.0f, 0.0f};
	static v2 const v2Half    = {0.5f, 0.5f};
	static v2 const v2One     = {1.0f, 1.0f};
	static v2 const v2Min     = {+maths::float_min, +maths::float_min};
	static v2 const v2Max     = {+maths::float_max, +maths::float_max};
	static v2 const v2Lowest  = {-maths::float_max, -maths::float_max};
	static v2 const v2Epsilon = {+maths::float_eps, +maths::float_eps};
	static v2 const v2XAxis   = {1.0f, 0.0f};
	static v2 const v2YAxis   = {0.0f, 1.0f};

	// Element accessors
	inline float GetX(v2 const& v) { return v.x; }
	inline float GetY(v2 const& v) { return v.y; }
	inline float GetZ(v2 const&  ) { return 0.0f; }
	inline float GetW(v2 const&  ) { return 0.0f; }

	// Assignment operators
	template <typename T> inline v2& operator += (v2& lhs, T rhs) { lhs.x += GetXf(rhs); lhs.y += GetYf(rhs); return lhs; }
	template <typename T> inline v2& operator -= (v2& lhs, T rhs) { lhs.x -= GetXf(rhs); lhs.y -= GetYf(rhs); return lhs; }
	template <typename T> inline v2& operator *= (v2& lhs, T rhs) { lhs.x *= GetXf(rhs); lhs.y *= GetYf(rhs); return lhs; }
	template <typename T> inline v2& operator /= (v2& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x /= GetXf(rhs);              lhs.y /= GetYf(rhs);              return lhs; }
	template <typename T> inline v2& operator %= (v2& lhs, T rhs) { assert(!IsZero4(rhs)); lhs.x  = Fmod(lhs.x, GetXf(rhs)); lhs.y  = Fmod(lhs.y, GetYf(rhs)); return lhs; }

	// Binary operators
	template <typename T> inline v2 operator + (v2 const& lhs, T rhs) { v2 v = lhs; return v += rhs; }
	template <typename T> inline v2 operator - (v2 const& lhs, T rhs) { v2 v = lhs; return v -= rhs; }
	template <typename T> inline v2 operator * (v2 const& lhs, T rhs) { v2 v = lhs; return v *= rhs; }
	template <typename T> inline v2 operator / (v2 const& lhs, T rhs) { v2 v = lhs; return v /= rhs; }
	template <typename T> inline v2 operator % (v2 const& lhs, T rhs) { v2 v = lhs; return v %= rhs; }
	inline v2 operator + (float lhs, v2 const& rhs)                   { v2 v = rhs; return v += lhs; }
	inline v2 operator - (float lhs, v2 const& rhs)                   { v2 v = rhs; return v -= lhs; }
	inline v2 operator * (float lhs, v2 const& rhs)                   { v2 v = rhs; return v *= lhs; }
	inline v2 operator / (float lhs, v2 const& rhs)                   { assert(All2(rhs,maths::NonZero<float>)); return v2::make(     lhs /GetXf(rhs),       lhs /GetYf(rhs) ); }
	inline v2 operator % (float lhs, v2 const& rhs)                   { assert(All2(rhs,maths::NonZero<float>)); return v2::make(Fmod(lhs, GetXf(rhs)), Fmod(lhs, GetYf(rhs))); }

	// Unary operators
	inline v2 operator + (v2 const& vec) { return vec; }
	inline v2 operator - (v2 const& vec) { return v2::make(-vec.x, -vec.y); }

	// Equality operators
	inline bool operator == (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
	inline bool operator != (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
	inline bool operator <  (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
	inline bool operator >  (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
	inline bool operator <= (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
	inline bool operator >= (v2 const& lhs, v2 const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }

	// Min/Max/Clamp
	template <> inline v2 Max(v2 const& lhs, v2 const& rhs)              { return v2::make(Max(lhs.x,rhs.x), Max(lhs.y,rhs.y)); }
	template <> inline v2 Min(v2 const& lhs, v2 const& rhs)              { return v2::make(Min(lhs.x,rhs.x), Min(lhs.y,rhs.y)); }
	template <> inline v2 Clamp(v2 const& x, v2 const& mn, v2 const& mx) { return v2::make(Clamp(x.x,mn.x,mx.x), Clamp(x.y,mn.y,mx.y)); }
	inline v2             Clamp(v2 const& x, float mn, float mx)         { return v2::make(Clamp(x.x,mn,mx),     Clamp(x.y,mn,mx)); }

	// Functions
	v2&   Zero(v2& v);
	bool  IsFinite(v2 const& v);
	bool  IsFinite(v2 const& v, float max_value);
	int   SmallestElement2(v2 const& v);
	int   LargestElement2(v2 const& v);
	v2    Abs(v2 const& v);
	v2    Trunc(v2 const& v);
	v2    Frac(v2 const& v);
	v2    Sqr(v2 const& v);
	float Dot2(v2 const& lhs, v2 const& rhs);
	float Cross2(v2 const& lhs, v2 const& rhs);
	v2    Rotate90CW(v2 const& v);
	v2    Rotate90CCW(v2 const& v);
	v2    Quantise(v2 const& v, int pow2);
	v2    Lerp(v2 const& src, v2 const& dest, float frac);
	v2    SLerp2(v2 const& src, v2 const& dest, float frac);
	uint  Quadrant(v2 const& v);
	int   Sector(v2 const& v, int sectors);
	float CosAngle2(v2 const& lhs, v2 const& rhs);
}

namespace std
{
	template <> class numeric_limits<pr::v2>
	{
	public:
		static pr::v2 min() throw()     { return pr::v2Min; }
		static pr::v2 max() throw()     { return pr::v2Max; }
		static pr::v2 lowest() throw()  { return pr::v2Lowest; }
		static pr::v2 epsilon() throw() { return pr::v2Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};
}


#endif
