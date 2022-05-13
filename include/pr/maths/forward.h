//*********************************************
// Maths Library
//  Copyright (c) Rylogic Ltd 2006
//*********************************************
#pragma once

#include <concepts>
#include <type_traits>
#include <iterator>
#include <algorithm>
#include <memory>
#include <thread>
#include <array>
#include <limits>
#include <intrin.h>
#include <immintrin.h>
#include <emmintrin.h>
#include <cmath>
#include <cstdlib>
#include <cstdint>
#include <complex>
#include <cassert>
#include <random>
#include "pr/container/span.h"
#include "pr/meta/dep_constants.h"

// Libraries built to use DirectXMath should be fine when linked in projects
// that don't use DirectXMath because all of the maths types have the same
// size/alignment requirements regardless.

static_assert(_MSC_VER >= 1900, "VS v140 is required due to a value initialisation bug in v120");

// Use intrinsics by default
#ifndef PR_MATHS_USE_INTRINSICS
#define PR_MATHS_USE_INTRINSICS 1
#endif

// Select DirectXMath if already included, otherwise, don't
#define PR_MATHS_USE_DIRECTMATH -- DX Math not supported --
//#ifndef PR_MATHS_USE_DIRECTMATH
//#  if defined(DIRECTX_MATH_VERSION)
//#    define PR_MATHS_USE_DIRECTMATH 1
//#  else
//#    define PR_MATHS_USE_DIRECTMATH 0
//#  endif
//#endif

//// Include 'DirectXMath.h'
//#if PR_MATHS_USE_DIRECTMATH
//#  include <directxmath.h>
//#  if !PR_MATHS_USE_INTRINSICS
//#     error "Intrinsics are required if using DirectX maths functions"
//#  endif
//#else
//namespace DirectX
//{
//	// Forward declare DX types
//	struct XMMATRIX;
//}
//#endif

// Use 'vectorcall' if intrinsics are enabled
#if PR_MATHS_USE_INTRINSICS
#  define pr_vectorcall __vectorcall
#  pragma intrinsic(sin, cos, tan, asin, acos, atan, atan2, sinh, cosh, tanh, pow, fmod, sqrt, exp, log10, log, abs, fabs, labs, memcmp, memcpy, memset)
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
	template <typename Scalar, typename T> struct Vec2;
	template <typename T> using Vec2f = Vec2<float, T>;
	template <typename T> using Vec2d = Vec2<float, T>;
	template <typename T> using Vec2i = Vec2<int32_t, T>;
	template <typename T> using Vec2l = Vec2<int64_t, T>;

	template <typename Scalar, typename T> struct Vec3;
	template <typename T> using Vec3f = Vec3<float, T>;
	template <typename T> using Vec3d = Vec3<float, T>;
	template <typename T> using Vec3i = Vec3<int32_t, T>;
	template <typename T> using Vec3l = Vec3<int64_t, T>;

	template <typename Scalar, typename T> struct Vec4;
	template <typename T> using Vec4f = Vec4<float, T>;
	template <typename T> using Vec4d = Vec4<float, T>;
	template <typename T> using Vec4i = Vec4<int32_t, T>;
	template <typename T> using Vec4l = Vec4<int64_t, T>;

	template <typename T> struct Vec8f;
	template <typename T> struct Half4;
	template <typename A, typename B> struct Mat2x2f;
	template <typename A, typename B> struct Mat3x4f;
	template <typename A, typename B> struct Mat4x4f;
	template <typename A, typename B> struct Mat6x8f;
	template <typename A, typename B> struct Quatf;
	struct BBox;
	struct BSphere;
	struct OBox;
	template <typename V> struct Rectangle;
	struct Line3;
	struct ISize;
	struct Frustum;
	using half_t = unsigned short;
	
	namespace maths
	{
		// The 'is_vec' traits means, "Can be converted to a N component vector"
		// If true, 'x_cp', 'y_cp', 'z_cp', 'w_cp' is expected to be defined for that type.
		// Don't specialise this for scalars because that could lead to accidental use of vectors in scalar functions.
		template <typename T> struct is_vec :std::false_type
		{
			// The type of the x,y,etc elements of the vector (can be vectors for matrix types)
			using elem_type = void;

			// The type of the components (typically int, float, double, etc)
			using comp_type = void;

			// The dimension of the vector
			static int const dim = 0;
		};
		template <typename T> using vec_elem_t = typename is_vec<T>::elem_type;
		template <typename T> using vec_comp_t = typename is_vec<T>::comp_type;

		// Scaler types
		template <typename T>
		concept Arithmetic =
			std::is_floating_point_v<T> ||
			std::is_integral_v<T>;

		// Concepts of vector types
		template <typename V> concept VectorX = is_vec<V>::dim >= 1;
		template <typename V> concept Vector2 = is_vec<V>::dim >= 2;
		template <typename V> concept Vector3 = is_vec<V>::dim >= 3;
		template <typename V> concept Vector4 = is_vec<V>::dim >= 4;

		template <typename M> concept MatrixX = is_vec<M>::dim >= 1 && VectorX<vec_elem_t<M>>;
		template <typename M> concept Matrix2 = is_vec<M>::dim >= 2 && VectorX<vec_elem_t<M>>;
		template <typename M> concept Matrix3 = is_vec<M>::dim >= 3 && VectorX<vec_elem_t<M>>;
		template <typename M> concept Matrix4 = is_vec<M>::dim >= 4 && VectorX<vec_elem_t<M>>;

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
		template <Arithmetic T, int N> struct is_vec<T[N]>
		{
			using elem_type = T;
			using comp_type = T;
			static int const dim = N;
		};
		template <Arithmetic T, int N> struct is_vec<std::array<T,N>>
		{
			using elem_type = T;
			using comp_type = T;
			static int const dim = N;
		};
		template <typename Scalar, typename T> struct is_vec<Vec2<Scalar, T>> :std::true_type
		{
			using elem_type = Scalar;
			using comp_type = Scalar;
			static int const dim = 2;
		};
		template <typename Scalar, typename T> struct is_vec<Vec3<Scalar, T>> :std::true_type
		{
			using elem_type = Scalar;
			using comp_type = Scalar;
			static int const dim = 3;
		};
		template <typename Scalar, typename T> struct is_vec<Vec4<Scalar, T>> :std::true_type
		{
			using elem_type = Scalar;
			using comp_type = Scalar;
			static int const dim = 4;
		};
		template <typename T> struct is_vec<Vec8f<T>> :std::true_type
		{
			using elem_type = float;
			using comp_type = float;
			static int const dim = 8;
		};
		template <typename T> struct is_vec<Vec4i<T>> :std::true_type
		{
			using elem_type = int;
			using comp_type = int;
			static int const dim = 4;
		};
		template <typename A, typename B> struct is_vec<Mat2x2f<A,B>> :std::true_type
		{
			using elem_type = Vec2f<void>;
			using comp_type = float;
			static int const dim = 2;
		};
		template <typename A, typename B> struct is_vec<Mat3x4f<A,B>> :std::true_type
		{
			using elem_type = Vec4f<void>;
			using comp_type = float;
			static int const dim = 3;
		};
		template <typename A, typename B> struct is_vec<Mat4x4f<A,B>> :std::true_type
		{
			using elem_type = Vec4f<void>;
			using comp_type = float;
			static int const dim = 4;
		};
		template <typename A, typename B> struct is_vec<Mat6x8f<A,B>> :std::true_type
		{
			using elem_type = Vec8f<void>;
			using comp_type = float;
			static int const dim = 6;
		};
		template <typename A, typename B> struct is_vec<Quatf<A,B>> :std::true_type
		{
			using elem_type = float;
			using comp_type = float;
			static int const dim = 4;
		};

		// Checks
		static_assert(!VectorX<char*>);
		static_assert(!VectorX<wchar_t*>);
		static_assert(!VectorX<float*>);
		static_assert(!VectorX<int*>);
		static_assert(Vector2<float[2]>);
		static_assert(Vector2<int[2]>);
		#pragma endregion

		// Component accessor with default for out-of-bounds
		template <int idx, maths::VectorX V> constexpr maths::vec_elem_t<V> comp(V const& v)
		{
			if constexpr (maths::is_vec<V>::dim > idx)
				return v[idx];
			else
				return maths::vec_elem_t<V>{};
		}
		template <int idx, typename E, maths::VectorX V> constexpr E comp(V const& v)
		{
			if constexpr (maths::is_vec<V>::dim > idx)
				return static_cast<E>(v[idx]);
			else
				return E{};
		}
	}

	//// Common vector types
	//using v2f = Vec2f<void>;
	//using v3f = Vec3f<void>;
	//using v4f = Vec4f<void>;
	//using v8f = Vec8f<void>;
	//using quatf = Quatf<void,void>;
	//using m2x2f = Mat2x2f<void,void>;
	//using m3x4f = Mat3x4f<void,void>;
	//using m4x4f = Mat4x4f<void,void>;
	//using m6x8f = Mat6x8f<void,void>;
	//using v2i = Vec2i<void>;
	//using v3i = Vec3i<void>;
	//using v4i = Vec4i<void>;
	using half4 = Half4<void>;

	// Constant reference types
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	#define pr_cref const
	#else
	#define pr_cref const&
	#endif
	template <typename Scalar, typename T> using Vec2_cref = Vec2<Scalar, T> pr_cref;
	template <typename Scalar, typename T> using Vec3_cref = Vec3<Scalar, T> pr_cref;
	template <typename Scalar, typename T> using Vec4_cref = Vec4<Scalar, T> pr_cref;

	template <typename T = void> using v8_cref = Vec8f<T> pr_cref;
	template <typename T = void> using half4_cref = Half4<T> pr_cref;
	template <typename A = void, typename B = void> using m2_cref = Mat2x2f<A, B> pr_cref;
	template <typename A = void, typename B = void> using m3_cref = Mat3x4f<A, B> pr_cref;
	template <typename A = void, typename B = void> using m4_cref = Mat4x4f<A, B> pr_cref;
	template <typename A = void, typename B = void> using m6_cref = Mat6x8f<A, B> const&;
	template <typename A = void, typename B = void> using quat_cref = Quatf<A,B> pr_cref;
	using BBox_cref = BBox pr_cref;
	using BSphere_cref = BSphere pr_cref;
	#undef pr_cref

	template <typename T> using v2f_cref = Vec2_cref<float, T>;
	template <typename T> using v2d_cref = Vec2_cref<double, T>;
	template <typename T> using v2i_cref = Vec2_cref<int32_t, T>;
	template <typename T> using v2l_cref = Vec2_cref<int64_t, T>;

	// Old names
	using v2 = Vec2<float, void>;
	using v3 = Vec3<float, void>;
	using v4 = Vec4<float, void>;
	using v8 = Vec8f<void>;
	using quat = Quatf<void,void>;
	using m2x2 = Mat2x2f<void,void>;
	using m3x4 = Mat3x4f<void,void>;
	using m4x4 = Mat4x4f<void,void>;
	using m6x8 = Mat6x8f<void,void>;
	using iv2 = Vec2<int, void>;
	using iv3 = Vec3i<void>;
	using iv4 = Vec4i<void>;
	template <typename T = void> using v2_cref = Vec2_cref<float, T>;
	template <typename T = void> using v3_cref = Vec3_cref<float, T>;
	template <typename T = void> using v4_cref = Vec4_cref<float, T>;
	template <typename T = void> using iv4_cref = Vec4_cref<int, T>;

	// Helper trait for 'underlying_type' that works for non-enums as well
	template <typename T, bool = std::is_enum_v<T>> struct underlying_type : std::underlying_type<T> {};
	template <typename T> struct underlying_type<T, false> { using type = T; };
	template <typename T> using underlying_type_t = typename underlying_type<T>::type;

	// Maths library build options
	struct MathsBuildOptions
	{
		int PrMathsUseIntrinsics;
		//int PrMathsDirectMath;

		MathsBuildOptions()
			:PrMathsUseIntrinsics(PR_MATHS_USE_INTRINSICS)
			//,PrMathsDirectMath(PR_MATHS_USE_DIRECTMATH)
		{}
	};
}
