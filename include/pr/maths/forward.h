//*********************************************
// Maths Library
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include <iterator>
#include <algorithm>
#include <memory>
#include <thread>
#include <array>
#include <limits>
#include <type_traits>
#include <intrin.h>
#include <cmath>
#include <cstdlib>
#include <complex>
#include <cassert>
#include <random>
#include "pr/container/span.h"

// Libraries built to use DirectXMath should be fine when linked in projects
// that don't use DirectXMath because all of the maths types have the same
// size/alignment requirements regardless.

static_assert(_MSC_VER >= 1900, "VS v140 is required due to a value initialisation bug in v120");

// Use intrinsics by default
#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif

// Select DirectXMath if already included, otherwise, don't
#ifndef PR_MATHS_USE_DIRECTMATH
#  if defined(DIRECTX_MATH_VERSION)
#    define PR_MATHS_USE_DIRECTMATH 1
#  else
#    define PR_MATHS_USE_DIRECTMATH 0
#  endif
#endif

// Include 'DirectXMath.h'
#if PR_MATHS_USE_DIRECTMATH
#  include <directxmath.h>
#  if !PR_MATHS_USE_INTRINSICS
#     error "Intrinsics are required if using DirectX maths functions"
#  endif
#else
namespace DirectX
{
	// Forward declare DX types
	struct XMMATRIX;
}
#endif

// Use 'vectorcall' if intrinsics are enabled
#if PR_MATHS_USE_INTRINSICS
#  pragma intrinsic(sin, cos, tan, asin, acos, atan, atan2, sinh, cosh, tanh, pow, fmod, sqrt, exp, log10, log, abs, fabs, labs, memcmp, memcpy, memset)
#  define pr_vectorcall __vectorcall
#else
#  define pr_vectorcall __fastcall
#endif

// C++11's thread_local
#ifndef thread_local
#define thread_local __declspec(thread)
#endif

// C++11's 'alignas'
#if _MSC_VER < 1900
#  ifndef alignas
#    define alignas(alignment) __declspec(align(alignment))
#  endif
#endif
#if _MSC_VER < 1900
#  ifndef constexpr
#    define constexpr
#  endif
#endif

namespace pr
{
	typedef          char      int8;
	typedef unsigned char     uint8;
	typedef          short    int16;
	typedef unsigned short   uint16;
	typedef unsigned int       uint;
	typedef          int      int32;
	typedef unsigned int     uint32;
	typedef          __int64  int64;
	typedef unsigned __int64 uint64;
	typedef unsigned long     ulong;
	typedef float            real32;
	typedef double           real64;
	using half_t = unsigned short;

	template <typename T> struct Vec2;
	template <typename T> struct Vec3;
	template <typename T> struct Vec4;
	template <typename T> struct Vec8;
	template <typename T> struct IVec2;
	template <typename T> struct IVec4;
	template <typename T> struct Half4;
	template <typename A, typename B> struct Mat2x2;
	template <typename A, typename B> struct Mat3x4;
	template <typename A, typename B> struct Mat4x4;
	template <typename A, typename B> struct Mat6x8;
	template <typename A, typename B> struct Quat;
	struct BBox;
	struct BSphere;
	struct OBox;
	template <typename V> struct Rectangle;
	struct Line3;
	struct ISize;
	struct Frustum;

	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	template <typename T = void> using v2_cref = Vec2<T> const;
	template <typename T = void> using v3_cref = Vec3<T> const;
	template <typename T = void> using v4_cref = Vec4<T> const;
	template <typename T = void> using v8_cref = Vec8<T> const;
	template <typename T = void> using iv2_cref = IVec2<T> const;
	template <typename T = void> using iv4_cref = IVec4<T> const;
	template <typename T = void> using half4_cref = Half4<T> const;
	template <typename A = void, typename B = void> using m2_cref = Mat2x2<A, B> const;
	template <typename A = void, typename B = void> using m3_cref = Mat3x4<A, B> const;
	template <typename A = void, typename B = void> using m4_cref = Mat4x4<A, B> const;
	template <typename A = void, typename B = void> using m6_cref = Mat6x8<A, B> const&;
	template <typename A = void, typename B = void> using quat_cref = Quat<A,B> const;
	using BBox_cref = BBox const;
	using BSphere_cref = BSphere const;
	#else
	template <typename T = void> using v2_cref = Vec2<T> const&;
	template <typename T = void> using v3_cref = Vec3<T> const&;
	template <typename T = void> using v4_cref = Vec4<T> const&;
	template <typename T = void> using v8_cref = Vec8<T> const&;
	template <typename T = void> using iv2_cref = IVec2<T> const&;
	template <typename T = void> using iv4_cref = IVec4<T> const&;
	template <typename T = void> using half4_cref = Half4<T> const&;
	template <typename A = void, typename B = void> using m2_cref = Mat2x2<A, B> const&;
	template <typename A = void, typename B = void> using m3_cref = Mat3x4<A, B> const&;
	template <typename A = void, typename B = void> using m4_cref = Mat4x4<A, B> const&;
	template <typename A = void, typename B = void> using m6_cref = Mat6x8<A, B> const&;
	template <typename A = void, typename B = void> using quat_cref = Quat<A,B> const&;
	using BBox_cref = BBox const&;
	using BSphere_cref = BSphere const&;
	#endif

	namespace maths
	{
		// Allowed vector component types
		template <typename T> using is_vec_cp = typename std::integral_constant<bool,
			std::is_same<T, short          >::value ||
			std::is_same<T, unsigned short >::value ||
			std::is_same<T, int            >::value ||
			std::is_same<T, unsigned int   >::value ||
			std::is_same<T, long           >::value ||
			std::is_same<T, unsigned long  >::value ||
			std::is_same<T, int64          >::value ||
			std::is_same<T, uint64         >::value ||
			std::is_same<T, float          >::value ||
			std::is_same<T, double         >::value
			>::type;

		// The 'is_vec' traits means, "Can be converted to a N component vector"
		// If true, 'x_cp', 'y_cp', 'z_cp', 'w_cp' is expected to be defined for that type.
		// Don't specialise this for scalars because that could lead to accidental use of vectors in scalar functions.
		template <typename T> struct is_vec :std::false_type
		{
			// The type of the x,y,z,... elements of the vector
			using elem_type = void;
			// The type of the lowest level elements
			using cp_type = void;
			static int const dim = 0;
		};
		template <typename T> struct is_vec2 :std::integral_constant<bool, is_vec<T>::dim >= 2> {};
		template <typename T> struct is_vec3 :std::integral_constant<bool, is_vec<T>::dim >= 3> {};
		template <typename T> struct is_vec4 :std::integral_constant<bool, is_vec<T>::dim >= 4> {};
		template <typename T> struct is_mat2 :std::integral_constant<bool, is_vec<T>::dim == 2 && is_vec<typename is_vec<T>::elem_type>::value> {};
		template <typename T> struct is_mat3 :std::integral_constant<bool, is_vec<T>::dim == 3 && is_vec<typename is_vec<T>::elem_type>::value> {};
		template <typename T> struct is_mat4 :std::integral_constant<bool, is_vec<T>::dim == 4 && is_vec<typename is_vec<T>::elem_type>::value> {};

		// Helper meta functions
		template <typename T> using enable_if_enum = typename std::enable_if<std::is_enum<T>::value>::type;
		template <typename T> using enable_if_arith = typename std::enable_if<std::is_arithmetic<T>::value>::type;
		template <typename T> using enable_if_intg = typename std::enable_if<std::is_integral<T>::value>::type;
		template <typename T> using enable_if_vec_cp = typename std::enable_if<is_vec_cp<T>::value>::type;
		template <typename T> using enable_if_vN = typename std::enable_if<is_vec<T>::value>::type;
		template <typename T> using enable_if_v2 = typename std::enable_if<is_vec2<T>::value>::type;
		template <typename T> using enable_if_v3 = typename std::enable_if<is_vec3<T>::value>::type;
		template <typename T> using enable_if_v4 = typename std::enable_if<is_vec4<T>::value>::type;
		template <typename T> using enable_if_m2 = typename std::enable_if<is_mat2<T>::value>::type;
		template <typename T> using enable_if_m3 = typename std::enable_if<is_mat3<T>::value>::type;
		template <typename T> using enable_if_m4 = typename std::enable_if<is_mat4<T>::value>::type;
		template <typename T> using enable_if_fp_vec = typename std::enable_if<is_vec<T>::value && std::is_floating_point<typename is_vec<T>::elem_type>::value>::type;
		template <typename T> using enable_if_ig_vec = typename std::enable_if<is_vec<T>::value && std::is_integral      <typename is_vec<T>::elem_type>::value>::type;
		template <typename T> using enable_if_dx_mat = typename std::enable_if<std::is_same<T, DirectX::XMMATRIX>::value>::type;
		template <typename T> using enable_if_not_vN = typename std::enable_if<!is_vec<T>::value>::type;

		// Test alignment of 't'
		template <typename T, int A> inline bool is_aligned(T const* t)
		{
			return (reinterpret_cast<char const*>(t) - static_cast<char const*>(nullptr)) % A == 0;
		}
		template <typename T> inline bool is_aligned(T const* t)
		{
			return is_aligned<T, std::alignment_of<T>::value>(t);
		}

		#pragma region Traits
		template <typename T, int N> struct is_vec<T[N]> :is_vec_cp<T>
		{
			using elem_type = T;
			using cp_type = T;
			static int const dim = N;
		};
		static_assert(!is_vec<char* >::value, "");
		static_assert(!is_vec<wchar_t* >::value, "");
		static_assert(!is_vec<float* >::value, "");
		static_assert(!is_vec<int*  >::value, "");
		static_assert(is_vec<float[2]>::value, "");
		static_assert(is_vec<int[2] >::value, "");
		template <typename T, int N> struct is_vec<std::array<T,N>> :is_vec_cp<T>
		{
			using elem_type = T;
			using cp_type = T;
			static int const dim = N;
		};
		template <typename T> struct is_vec<Vec2<T>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 2;
		};
		template <typename T> struct is_vec<Vec3<T>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 3;
		};
		template <typename T> struct is_vec<Vec4<T>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
		template <typename T> struct is_vec<Vec8<T>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 8;
		};
		template <typename T> struct is_vec<IVec2<T>> :std::true_type
		{
			using elem_type = int;
			using cp_type = int;
			static int const dim = 2;
		};
		template <typename T> struct is_vec<IVec4<T>> :std::true_type
		{
			using elem_type = int;
			using cp_type = int;
			static int const dim = 4;
		};
		template <typename A, typename B> struct is_vec<Mat2x2<A,B>> :std::true_type
		{
			using elem_type = Vec2<void>;
			using cp_type = float;
			static int const dim = 2;
		};
		template <typename A, typename B> struct is_vec<Mat3x4<A,B>> :std::true_type
		{
			using elem_type = Vec4<void>;
			using cp_type = float;
			static int const dim = 3;
		};
		template <typename A, typename B> struct is_vec<Mat4x4<A,B>> :std::true_type
		{
			using elem_type = Vec4<void>;
			using cp_type = float;
			static int const dim = 4;
		};
		template <typename A, typename B> struct is_vec<Mat6x8<A,B>> :std::true_type
		{
			using elem_type = Vec8<void>;
			using cp_type = float;
			static int const dim = 6;
		};
		template <typename A, typename B> struct is_vec<Quat<A,B>> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
		#pragma endregion
	}

	// Common vector types
	using v2 = Vec2<void>;
	using v3 = Vec3<void>;
	using v4 = Vec4<void>;
	using v8 = Vec8<void>;
	using m2x2 = Mat2x2<void,void>;
	using m3x4 = Mat3x4<void,void>;
	using m4x4 = Mat4x4<void,void>;
	using m6x8 = Mat6x8<void,void>;
	using quat = Quat<void,void>;
	using iv2 = IVec2<void>;
	using iv4 = IVec4<void>;
	using half4 = Half4<void>;

	// Default implementations of the component accessors
	template <typename T, typename = maths::enable_if_v2<T>> inline typename maths::is_vec<T>::elem_type x_cp(T const& v) { return v.x; }
	template <typename T, typename = maths::enable_if_v2<T>> inline typename maths::is_vec<T>::elem_type y_cp(T const& v) { return v.y; }
	template <typename T, typename = maths::enable_if_v3<T>> inline typename maths::is_vec<T>::elem_type z_cp(T const& v) { return v.z; }
	template <typename T, typename = maths::enable_if_v4<T>> inline typename maths::is_vec<T>::elem_type w_cp(T const& v) { return v.w; }

	// Helper trait for 'underlying_type' that works for non-enums as well
	template <typename T, bool = std::is_enum_v<T>> struct underlying_type : std::underlying_type<T> {};
	template <typename T> struct underlying_type<T, false> { using type = T; };
	template <typename T> using underlying_type_t = typename underlying_type<T>::type;

	// Maths library build options
	struct MathsBuildOptions
	{
		int PrMathsUseIntrinsics;
		int PrMathsDirectMath;

		MathsBuildOptions()
			:PrMathsUseIntrinsics(PR_MATHS_USE_INTRINSICS)
			,PrMathsDirectMath(PR_MATHS_USE_DIRECTMATH)
		{}
	};
}
