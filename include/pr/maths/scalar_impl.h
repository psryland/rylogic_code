//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_SCALER_IMPL_H
#define PR_MATHS_SCALER_IMPL_H

#include "pr/maths/scalar.h"
#include "pr/maths/vector4.h"

namespace pr
{
	// Low precision reciprocal square root
	template <int Intrin> inline float Rsqrt0(float x)
	{
		return 1.0f / pr::Sqrt(x);
	}
	template <> inline float Rsqrt0<1>(float x)
	{
		__m128 r0; float y;
		r0 = _mm_load_ss(&x);
		r0 = _mm_rsqrt_ss(r0);
		_mm_store_ss(&y, r0);
		return y;
	}
	inline float Rsqrt0(float x)
	{
		return Rsqrt0<PR_MATHS_USE_INTRINSICS>(x);
	}

	// High(er) precision reciprocal square root
	template <int Intrin> inline float Rsqrt1(float x)
	{
		return 1.0f / pr::Sqrt(x);
	}
	template <> inline float Rsqrt1<1>(float x)
	{
		static const float c0 = 3.0f, c1 = -0.5f;
		__m128 r0,r1; float y;
		r0 = _mm_load_ss(&x);
		r1 = _mm_rsqrt_ss(r0);
		r0 = _mm_mul_ss(r0, r1); // The general Newton-Raphson reciprocal square root recurrence:
		r0 = _mm_mul_ss(r0, r1); // (3 - b * X * X) * (X / 2)
		r0 = _mm_sub_ss(r0, _mm_load_ss(&c0));
		r1 = _mm_mul_ss(r1, _mm_load_ss(&c1));
		r0 = _mm_mul_ss(r0, r1);
		_mm_store_ss(&y, r0);
		return y;
	}
	inline float Rsqrt1(float x)
	{
		return Rsqrt1<PR_MATHS_USE_INTRINSICS>(x);
	}

	// Cube root
	inline float Cubert(float x)
	{
		union { float f; uint32 i; } as;
		bool flip_sign = x < 0.0f;
		if (flip_sign)  x = -x;
		if (x == 0.0f) return x;
		as.f = x;
		uint32 bits = as.i;

		bits = (bits + (uint32)2 * 0x3f800000) / 3;

		as.i = bits;
		float guess = as.f;

		x *= 1.0f / 3.0f;
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		guess = (x / (guess*guess) + guess * (2.0f / 3.0f));
		return (flip_sign ? -guess : guess);
	}

	// Fast hash
	inline uint Hash(float value, uint max_value)
	{
		// Arbitrary prime
		const uint32 h1 = 0x8da6b343;
		int n = static_cast<int>(h1 * value);
		n = n % max_value;
		if (n < 0) n += max_value;
		return static_cast<uint>(n);
	}
	inline uint Hash(const v4& value, uint max_value)
	{
		// Arbitrary Primes
		const uint32 h1 = 0x8da6b343;
		const uint32 h2 = 0xd8163841;
		const uint32 h3 = 0xcb1ab31f;
		int n = static_cast<int>(h1 * value.x + h2 * value.y + h3 * value.z);
		n = n % max_value;
		if (n < 0) n += max_value;
		return static_cast<uint>(n);
	}

	// 'scale' should be a power of 2, i.e. 256, 1024, 2048, etc
	inline float Quantise(float x, int scale)
	{
		return static_cast<int>(x*scale) / static_cast<float>(scale);
	}

	// Return the cosine of the angle of the triangle apex opposite 'opp'
	inline float CosAngle(float adj0, float adj1, float opp)
	{
		assert(!FEqlZero(adj0) && !FEqlZero(adj1) && "Angle undefined an when adjacent length is zero");
		return Clamp((adj0*adj0 + adj1*adj1 - opp*opp) / (2.0f * adj0 * adj1), -1.0f, 1.0f);
	}

	// Return the angle (in radians) of the triangle apex opposite 'opp'
	inline float Angle(float adj0, float adj1, float opp)
	{
		return ACos(CosAngle(adj0, adj1, opp));
	}

	// Return the length of a triangle side given by two adjacent side lengths and an angle between them
	inline float Length(float adj0, float adj1, float angle)
	{
		float len_sq = adj0*adj0 + adj1*adj1 - 2.0f * adj0 * adj1 * Cos(angle);
		return len_sq > 0.0f ? Sqrt(len_sq) : 0.0f;
	}

	// Returns 1.0f if 'hi' is >= 'lo' otherwise 0.0f
	inline float Step(float lo, float hi)
	{
		return hi >= lo ? 1.0f : 0.0f;
	}

	// Returns the Hermite interpolation (3t² - 2t³) between 'lo' and 'hi' for t=[0,1]
	inline float SmoothStep(float lo, float hi, float t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), 0.0f, 1.0f);
		return t*t*(3 - 2*t);
	}

	// Returns a fifth-order Perlin interpolation (6t^5 - 15t^4 + 10t^3) between 'lo' and 'hi' for t=[0,1]
	inline float SmoothStep2(float lo, float hi, float t)
	{
		if (lo == hi) return lo;
		t = Clamp((t - lo)/(hi - lo), 0.0f, 1.0f);
		return t*t*t*(t*(t*6 - 15) + 10);
	}

	// Return the greatest common factor between 'a' and 'b'
	// Uses the Euclidean algorithm. If the greatest common factor is 1, then 'a' and 'b' are co-prime
	template <typename Int> inline Int GreatestCommonFactor(Int a, Int b)
	{
		while (b) { auto t = b; b = a % b; a = t; }
		return a;
	}
	template <typename Int> inline Int LeastCommonMultiple(Int a, Int b)
	{
		return (a*b) / GreatestCommonFactor(a,b);
	}
}

#endif
