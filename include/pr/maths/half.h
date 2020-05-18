//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"

namespace pr
{
	template <typename T>
	struct Half4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { half_t x, y, z, w; };
			struct { half_t arr[4]; };
			uint64_t ull;
		};
		#pragma warning(pop)

		// Construct
		Half4() = default;
		Half4(half_t x_, half_t y_, half_t z_, half_t w_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		{}
		explicit Half4(half_t x_)
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		{}
		explicit Half4(uint64_t v)
			:ull(v)
		{}
		explicit Half4(half_t const* v)
			:Half4(v[0], v[1], v[2], v[3])
		{}

		// Array access
		int const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		int& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Create other vector types
		Half4 w0() const
		{
			Half4 r(x,y,z,0);
			return r;
		}
		Half4 w1() const
		{
			Half4 r(x,y,z,1);
			return r;
		}

		// Component accessors
		friend constexpr half_t pr_vectorcall x_cp(Half4<T> v) { return v.x; }
		friend constexpr half_t pr_vectorcall y_cp(Half4<T> v) { return v.y; }
		friend constexpr half_t pr_vectorcall z_cp(Half4<T> v) { return v.z; }
		friend constexpr half_t pr_vectorcall w_cp(Half4<T> v) { return v.w; }
	};
	static_assert(std::is_pod_v<Half4<void>>, "half4 must be a pod type");

	#pragma region Functions

	// Convert between 32-bit float (1s7e24m) and 16-bit float (1s5e10m) at compile time
	constexpr half_t F32toF16CT(float f32)
	{
		// see https://gist.github.com/martin-kallman/5049614 (Note comments though, Martin's implementation has a bug)
		// see https://github.com/numpy/numpy/blob/master/numpy/core/src/npymath/halffloat.c#L466

		// Martin Kallman (mostly. Some parts nicked from numpy)
		//
		// Fast single-precision to half-precision floating point conversion
		//  - Supports signed zero, denormals-as-zero (DAZ), flush-to-zero (FTZ),
		//    clamp-to-max
		//  - Does not support infinities or NaN
		//  - Few, partially pipelinable, non-branching instructions,
		//  - Core opreations ~10 clock cycles on modern x86-64
		using aliaser = union { float f; unsigned int u; };
		auto x = aliaser{f32};
		auto t1 = static_cast<uint32_t>(x.u & 0x7fffffff);        // Non-sign bits
		auto t2 = static_cast<uint32_t>(x.u & 0x80000000);        // Sign bit
		auto t3 = static_cast<uint32_t>(x.u & 0x7f800000);        // Exponent
		auto t4 = static_cast<uint32_t>(x.u & 0x007fffffu) >> 13; // NaN signal >> 13

		t1 >>= 13;                                     // Align mantissa on MSB
		t2 >>= 16;                                     // Shift sign bit into position
		t1 -= 0x1c000;                                 // Adjust bias
		t1 = (t3 < 0x38800000) ? 0 : t1;               // Flush-to-zero
		t1 = (t3 > 0x47000000) ? 0x7c00u : t1;         // Clamp-to-max (inf = 0x7c00u, max = 0x7bffu)
		t1 = (t3 == 0x7f800000) ? (0x7c00u + t4) : t1; // NaN or Inf (t4 == 0)
		t1 = (t3 == 0) ? 0 : t1;                       // Denormals-as-zero
		t1 |= t2;                                      // Re-insert sign bit
		return static_cast<half_t>(t1);
	}
	constexpr float F16toF32CT(half_t f16)
	{
		// Martin Kallman (mostly. Some parts nicked from numpy)
		//
		// Fast half-precision to single-precision floating point conversion
		//  - Supports signed zero and denormals-as-zero (DAZ)
		//  - Does not support infinities or NaN
		//  - Few, partially pipelinable, non-branching instructions,
		//  - Core opreations ~6 clock cycles on modern x86-64
		using aliaser = union { unsigned int u; float f; };
		auto t1 = static_cast<uint32_t>(f16 & 0x7fff);      // Non-sign bits
		auto t2 = static_cast<uint32_t>(f16 & 0x8000);      // Sign bit
		auto t3 = static_cast<uint32_t>(f16 & 0x7c00);      // Exponent
		auto t4 = static_cast<uint32_t>(t1 - 0x7c00) << 13; // NaN signal

		t1 <<= 13;                                         // Align mantissa on MSB
		t2 <<= 16;                                         // Shift sign bit into position
		t1 += 0x38000000;                                  // Adjust bias
		t1 = t3 == 0 ? 0 : t1;                             // Denormals-as-zero
		t1 = t3 >= 0x7c00u ? 0x7f800000u + t4 : t1;        // NaN or Inf (t4 == 0)
		t1 |= t2;                                          // Re-insert sign bit
		return aliaser{ t1 }.f;
	}

	// Convert between 32-bit float (1s7e24m) and 16-bit float (1s5e10m)
	inline half_t F32toF16(float f32)
	{
		#if PR_MATHS_USE_INTRINSICS&&0
		auto vecf32 = _mm_set_ps1(f32);
		auto vecf16 = _mm_cvtps_ph(vecf32, _MM_FROUND_TO_NEAREST_INT); //|_MM_FROUND_NO_EXC - emits a warning
		return vecf16.m128i_u16[0];
		#else
		return F32toF16CT(f32);
		#endif
	}
	inline float F16toF32(half_t f16)
	{
		#if PR_MATHS_USE_INTRINSICS&&0
		auto vecf16 = _mm_set_epi16(0, 0, 0, 0, 0, 0, 0, f16);
		auto vecf32 = _mm_cvtph_ps(vecf16);
		return vecf32.m128_f32[0];
		#else
		return F16toF32CT(f16);
		#endif
	}

	// Return the vector converted to half size floats
	inline half4 pr_vectorcall F32toF16(v4_cref<> v)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto f16 = _mm_cvtps_ph(v.vec, _MM_FROUND_TO_NEAREST_INT); //|_MM_FROUND_NO_EXC - emits a warning
		auto res = half4{f16.m128i_u64[0]};
		return res;
		#else
		return half4{F32toF16(v.x), F32toF16(v.y), F32toF16(v.z), F32toF16(v.w)};
		#endif

	}

	// Return the vector as 32-bit floats
	inline v4 pr_vectorcall F16toF32(half4 v)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto f16 = _mm_set_epi16(0, 0, 0, 0, v.w, v.z, v.y, v.x);
		auto res = v4{_mm_cvtph_ps(f16)};
		return res;
		#else
		return v4{F16toF32(v.x), F16toF32(v.y), F16toF32(v.z), F16toF32(v.w)};
		#endif
	}

	// float literal to half_t
	constexpr half_t operator ""_hf(long double x)
	{
		return F32toF16CT(static_cast<float>(x));
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Half4Tests)
	{
		// Literals
		{
			auto h0 = 1.2345_hf;
			static_assert(sizeof(h0) == sizeof(half_t));

			auto h1 = F32toF16(1.2345f);
			PR_CHECK(h0, h1);
		}

		// Scalar
		{
			auto x0 = 0.0f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x0, x2);
		}
		{
			auto x0 = 6.28318530f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEqlRelative(x0, x2, 0.005f), true);
		}
		{
			auto x0 = -1.0f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEqlRelative(x0, x2, 0.005f), true);
		}
		{
			auto x0 = -4000.0f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEqlRelative(x0, x2, 0.005f), true);
		}
		{
			auto x0 = 200.0f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEqlRelative(x0, x2, 0.005f), true);
		}
		{
			auto x0 = -4.125e-6f;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEqlRelative(x0, x2, 0.005f), true);
		}
		{
			auto x0 = maths::float_inf;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x0, x2);
		}
		{
			auto x0 = -maths::float_inf;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x0, x2);
		}
		{
			auto x0 = maths::float_nan;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x0, x2);
		}

		// Half4
		{
			auto x0 = v4Zero;
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x2, x0);
		}
		{
			auto x0 = v4{1,2,3,4};
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(x2, x0);
		}
		{
			auto x0 = v4{-4000.0f, -200.0f, 0.003f, -4.125e-6f};
			auto x1 = F32toF16(x0);
			auto x2 = F16toF32(x1);
			PR_CHECK(FEql(x2, x0), true);
		}
	}
}
#endif