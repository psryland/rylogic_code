//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"

namespace pr
{
	struct alignas(16) v4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x,y,z,w; };
			struct { v2 xy, zw; };
			struct { v3 xyz; };
			#if PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec;
			#elif PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#endif
		};
		#pragma warning(pop)
		typedef float Array[4];

		v4& set(float x_);
		v4& set(float x_, float y_, float z_, float w_);
		#if PR_MATHS_USE_INTRINSICS
		v4& set(__m128 v);
		#endif
		template <typename T> v4& set(T const& v, float z_, float w_);
		template <typename T> v4& set(T const& v, float w_);
		template <typename T> v4& set(T const& v);
		template <typename T> v4& set(T const* v);
		template <typename T> v4& set(T const* v, float w_);

		v4 w0() const;
		v4 w1() const;

		Array const& ToArray() const;
		Array&       ToArray();

		float const& operator [] (int i) const;
		float&       operator [] (int i);
		v4&          operator = (iv4 const& rhs);

		v2  vec2(int i0, int i1) const;
		v3  vec3(int i0, int i1, int i2) const;

		static v4                       make(float x)                               { v4 vec; return vec.set(x); }
		static v4                       make(float x, float y, float z, float w)    { v4 vec; return vec.set(x, y, z, w); }
		#if PR_MATHS_USE_INTRINSICS
		static v4                       make(__m128 v)                              { v4 vec; return vec.set(v); }
		#endif
		template <typename T> static v4 make(T const& v, float z, float w)          { v4 vec; return vec.set(v, z, w); }
		template <typename T> static v4 make(T const& v, float w)                   { v4 vec; return vec.set(v, w); }
		template <typename T> static v4 make(T const& v)                            { v4 vec; return vec.set(v); }
		template <typename T> static v4 make(T const* v)                            { v4 vec; return vec.set(v); }
		template <typename T> static v4 make(T const* v, float w)                   { v4 vec; return vec.set(v, w); }
		static v4                       normal3(float x, float y, float z, float w) { v4 vec; return Normalise3(vec.set(x, y, z, w)); }
		template <typename T> static v4 normal3(T const& v, float z, float w)       { v4 vec; return Normalise3(vec.set(v, z, w)); }
		template <typename T> static v4 normal3(T const& v, float w)                { v4 vec; return Normalise3(vec.set(v, w)); }
		template <typename T> static v4 normal3(T const& v)                         { v4 vec; return Normalise3(vec.set(v)); }
		template <typename T> static v4 normal3(T const* v)                         { v4 vec; return Normalise3(vec.set(v)); }
		template <typename T> static v4 normal3(T const* v, float w)                { v4 vec; return Normalise3(vec.set(v, w)); }
		static v4                       normal4(float x, float y, float z, float w) { v4 vec; return Normalise4(vec.set(x, y, z, w)); }
		template <typename T> static v4 normal4(T const& v, float z, float w)       { v4 vec; return Normalise4(vec.set(v, z, w)); }
		template <typename T> static v4 normal4(T const& v, float w)                { v4 vec; return Normalise4(vec.set(v, w)); }
		template <typename T> static v4 normal4(T const& v)                         { v4 vec; return Normalise4(vec.set(v)); }
		template <typename T> static v4 normal4(T const* v)                         { v4 vec; return Normalise4(vec.set(v)); }
		template <typename T> static v4 normal4(T const* v, float w)                { v4 vec; return Normalise4(vec.set(v, w)); }
	};
	static_assert(std::alignment_of<v4>::value == 16, "v4 should have 16 byte alignment");
	static_assert(std::is_pod<v4>::value, "Should be a pod type");

	static v4 const v4Zero    = {0.0f, 0.0f, 0.0f, 0.0f};
	static v4 const v4Half    = {0.5f, 0.5f, 0.5f, 0.5f};
	static v4 const v4One     = {1.0f, 1.0f, 1.0f, 1.0f};
	static v4 const v4Min     = {+maths::float_min, +maths::float_min, +maths::float_min, +maths::float_min};
	static v4 const v4Max     = {+maths::float_max, +maths::float_max, +maths::float_max, +maths::float_max};
	static v4 const v4Lowest  = {-maths::float_max, -maths::float_max, -maths::float_max, -maths::float_max};
	static v4 const v4Epsilon = {+maths::float_eps, +maths::float_eps, +maths::float_eps, +maths::float_eps};
	static v4 const v4XAxis   = {1.0f, 0.0f, 0.0f, 0.0f};
	static v4 const v4YAxis   = {0.0f, 1.0f, 0.0f, 0.0f};
	static v4 const v4ZAxis   = {0.0f, 0.0f, 1.0f, 0.0f};
	static v4 const v4Origin  = {0.0f, 0.0f, 0.0f, 1.0f};

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
	inline v4 operator + (float lhs, v4 const& rhs) { v4 v = rhs; return v += lhs; }
	inline v4 operator - (float lhs, v4 const& rhs) { v4 v = rhs; return v -= lhs; }
	inline v4 operator * (float lhs, v4 const& rhs) { v4 v = rhs; return v *= lhs; }
	inline v4 operator / (float lhs, v4 const& rhs) { assert(All4(rhs,maths::NonZero<float>)); return v4::make(     GetXf(lhs)/rhs.x,       GetYf(lhs)/rhs.y,       GetZf(lhs)/rhs.z,       GetWf(lhs)/rhs.w); }
	inline v4 operator % (float lhs, v4 const& rhs) { assert(All4(rhs,maths::NonZero<float>)); return v4::make(Fmod(GetXf(lhs),rhs.x), Fmod(GetYf(lhs),rhs.y), Fmod(GetZf(lhs),rhs.z), Fmod(GetWf(lhs),rhs.w)); }
	inline v4 operator + (double lhs, v4 const& rhs) { return GetXf(lhs) + rhs; }
	inline v4 operator - (double lhs, v4 const& rhs) { return GetXf(lhs) - rhs; }
	inline v4 operator * (double lhs, v4 const& rhs) { return GetXf(lhs) * rhs; }
	inline v4 operator / (double lhs, v4 const& rhs) { assert(All4(rhs,maths::NonZero<float>)); return v4::make(     GetXf(lhs)/rhs.x,       GetYf(lhs)/rhs.y,       GetZf(lhs)/rhs.z,       GetWf(lhs)/rhs.w); }
	inline v4 operator % (double lhs, v4 const& rhs) { assert(All4(rhs,maths::NonZero<float>)); return v4::make(Fmod(GetXf(lhs),rhs.x), Fmod(GetYf(lhs),rhs.y), Fmod(GetZf(lhs),rhs.z), Fmod(GetWf(lhs),rhs.w)); }

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
	inline DirectX::XMVECTOR const& dxv4(v4 const& v) { assert(maths::is_aligned(&v)); return v.vec; }
	inline DirectX::XMVECTOR&       dxv4(v4&       v) { assert(maths::is_aligned(&v)); return v.vec; }
	#endif

	v4      Max(v4 const& lhs, v4 const& rhs);
	v4      Min(v4 const& lhs, v4 const& rhs);
	v4      Clamp(v4 const& x, v4 const& mn, v4 const& mx);
	v4      Clamp(v4 const& x, float mn, float mx);
	float   Length2Sq(v4 const& x);
	float   Length3Sq(v4 const& x);
	float   Length4Sq(v4 const& x);
	float   Length2(v4 const& x);
	float   Length3(v4 const& x);
	float   Length4(v4 const& x);
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

namespace std
{
	template <> class numeric_limits<pr::v4>
	{
	public:
		static pr::v4 min() throw()     { return pr::v4Min; }
		static pr::v4 max() throw()     { return pr::v4Max; }
		static pr::v4 lowest() throw()  { return pr::v4Lowest; }
		static pr::v4 epsilon() throw() { return pr::v4Epsilon; }
		
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

		//static const int digits = 128;
		//static const int digits10 = 38;
		//static T round_error() throw() { return 0; }
		//static const int min_exponent = 0;
		//static const int min_exponent10 = 0;
		//static const int max_exponent = 0;
		//static const int max_exponent10 = 0;
		//static T infinity() throw() { return 0; }
		//static T quiet_NaN() throw() { return 0; }
		//static T signaling_NaN() throw() { return 0; }
		//static T denorm_min() throw() { return 0; }
		//static const bool is_iec559 = false;
		//static const bool is_bounded = true;
		//static const bool is_modulo = true;
		//static const bool traps = false;
		//static const bool tinyness_before = false;
		//static const float_round_style round_style = round_toward_zero;
	};
}