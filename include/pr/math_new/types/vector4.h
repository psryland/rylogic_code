//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/types/vector2.h"
#include "pr/math_new/types/vector3.h"

namespace pr::math
{
	template <ScalarType S>
	struct Vec4
	{
		enum
		{
			IntrinsicF = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, float>,
			IntrinsicD = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, double>,
			IntrinsicI = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, int32_t>,
			IntrinsicL = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, int64_t>,
			NoIntrinsic = PR_MATHS_USE_INTRINSICS == 0,
		};
		using intrinsic_t =
			std::conditional_t<IntrinsicF, __m128,
			std::conditional_t<IntrinsicD, __m256d,
			std::conditional_t<IntrinsicI, __m128i,
			std::conditional_t<IntrinsicL, __m256i,
			std::byte[4*sizeof(S)]
			>>>>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union alignas(4 * sizeof(S))
		{
			struct { S x, y, z, w; };
			struct { Vec2<S> xy, zw; };
			struct { Vec3<S> xyz; };
			struct { S arr[4]; };
			intrinsic_t vec;
		};
		#pragma warning(pop)

		// Construct
		Vec4() = default;
		constexpr explicit Vec4(S x_)
			: x(x_)
			, y(x_)
			, z(x_)
			, w(x_)
		{
		}
		constexpr Vec4(S x_, S y_, S z_, S w_)
			: x(x_)
			, y(y_)
			, z(z_)
			, w(w_)
		{}
		constexpr explicit Vec4(std::ranges::random_access_range auto&& v)
			:Vec4(v[0], v[1], v[2], v[3])
		{}
		constexpr explicit Vec4(VectorTypeN<S, 4> auto v)
			:Vec4(vec(v).x, vec(v).y, vec(v).z, vec(v).w)
		{}
		constexpr Vec4(Vec3<S> v, S w_)
			:Vec4(vec(v).x, vec(v).y, vec(v).z, w_)
		{}
		constexpr Vec4(Vec2<S> v, S z_, S w_)
			:Vec4(vec(v).x, vec(v).y, z_, w_)
		{}
		constexpr Vec4(Vec2<S> xy_, Vec2<S> zw_)
			:Vec4(vec(xy_).x, vec(xy_).y, vec(zw_).z, vec(zw_).w)
		{}
		constexpr Vec4(intrinsic_t vec_) requires (!NoIntrinsic)
			:vec(vec_)
		{}

		// Array access
		constexpr S operator [] (int i) const
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}
		constexpr S& operator [] (int i)
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}

		// Create other vector types
		constexpr Vec4 w0() const
		{
			Vec4 r(x, y, z, S(0)); // LValue because of alignment
			return r;
		}
		constexpr Vec4 w1() const
		{
			Vec4 r(x, y, z, S(1)); // LValue because of alignment
			return r;
		}
		constexpr Vec2<S> vec2(int i0, int i1) const
		{
			return Vec2<S>{arr[i0], arr[i1]};
		}
		constexpr Vec3<S> vec3(int i0, int i1, int i2) const
		{
			return Vec3<S>{arr[i0], arr[i1], arr[i2]};
		}

		// Constants
		static constexpr Vec4 Zero()
		{
			return Vec4(S(0), S(0), S(0), S(0));
		}
		static constexpr Vec4 One()
		{
			return Vec4(S(1), S(1), S(1), S(1));
		}
		static constexpr Vec4 Tiny()
		{
			return Vec4(tiny<S>, tiny<S>, tiny<S>, tiny<S>);
		}
		static constexpr Vec4 Min()
		{
			return Vec4(limits<S>::min(), limits<S>::min(), limits<S>::min(), limits<S>::min());
		}
		static constexpr Vec4 Max()
		{
			return Vec4(limits<S>::max(), limits<S>::max(), limits<S>::max(), limits<S>::max());
		}
		static constexpr Vec4 Lowest()
		{
			return Vec4(limits<S>::lowest(), limits<S>::lowest(), limits<S>::lowest(), limits<S>::lowest());
		}
		static constexpr Vec4 Epsilon()
		{
			return Vec4(limits<S>::epsilon(), limits<S>::epsilon(), limits<S>::epsilon(), limits<S>::epsilon());
		}
		static constexpr Vec4 Infinity()
		{
			return Vec4(limits<S>::infinity(), limits<S>::infinity(), limits<S>::infinity(), limits<S>::infinity());
		}
		static constexpr Vec4 XAxis()
		{
			return Vec4(S(1), S(0), S(0), S(0));
		}
		static constexpr Vec4 YAxis()
		{
			return Vec4(S(0), S(1), S(0), S(0));
		}
		static constexpr Vec4 ZAxis()
		{
			return Vec4(S(0), S(0), S(1), S(0));
		}
		static constexpr Vec4 WAxis()
		{
			return Vec4(S(0), S(0), S(0), S(1));
		}
		static constexpr Vec4 Origin()
		{
			return Vec4(S(0), S(0), S(0), S(1));
		}

		// Construct normalised
		static Vec4 Normal(S x, S y, S z, S w) requires std::floating_point<S>
		{
			return Normalise(Vec4(x, y, z, w));
		}

		// Optimised operators
		#pragma region Operators
		friend constexpr Vec4 pr_vectorcall operator * (S lhs, Vec4 rhs)
		{
			return rhs * lhs;
		}
		friend constexpr Vec4 pr_vectorcall operator * (Vec4 lhs, S rhs)
		{
			if constexpr (IntrinsicF)
			{
				return Vec4{_mm_mul_ps(lhs.vec, _mm_set_ps1(rhs))};
			}
			else if constexpr (IntrinsicD)
			{
				return Vec4{_mm256_mul_pd(lhs.vec, _mm256_set1_pd(rhs))};
			}
			else
			{
				return Vec4{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs};
			}
		}
		friend constexpr Vec4 pr_vectorcall operator / (Vec4 lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			if constexpr (IntrinsicF)
			{
				return Vec4{_mm_div_ps(lhs.vec, _mm_set_ps1(rhs))};
			}
			else if constexpr (IntrinsicD)
			{
				return Vec4{_mm256_div_pd(lhs.vec, _mm256_set1_pd(rhs))};
			}
			else
			{
				return Vec4{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
			}
		}
		friend constexpr Vec4 pr_vectorcall operator % (Vec4 lhs, S rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			return Vec4{ Modulus(lhs.x, rhs), Modulus(lhs.y, rhs), Modulus(lhs.z, rhs), Modulus(lhs.w, rhs) };
		}
		friend constexpr Vec4 pr_vectorcall operator / (S lhs, Vec4 rhs)
		{
			if constexpr (IntrinsicF)
			{
				return Vec4{_mm_div_ps(_mm_set_ps1(lhs), rhs.vec)};
			}
			else if constexpr (IntrinsicD)
			{
				return Vec4{_mm256_div_pd(_mm256_set1_pd(lhs), rhs.vec)};
			}
			else
			{
				return Vec4{lhs / rhs.x, lhs / rhs.y, lhs / rhs.z, lhs / rhs.w};
			}
		}
		friend constexpr Vec4 pr_vectorcall operator % (S lhs, Vec4 rhs)
		{
			if constexpr (std::floating_point<S>)
			{
				return Vec4{Fmod(lhs, rhs.x), Fmod(lhs, rhs.y), Fmod(lhs, rhs.z), Fmod(lhs, rhs.w)};
			}
			else
			{
				return Vec4{lhs % rhs.x, lhs % rhs.y, lhs % rhs.z, lhs % rhs.w};
			}
		}
		friend constexpr Vec4 pr_vectorcall operator + (Vec4 lhs, Vec4 rhs)
		{
			auto fallback = [&]() constexpr { return Vec4{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w}; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					return Vec4{ _mm_add_ps(lhs.vec, rhs.vec) };
				}
				else if constexpr (IntrinsicD)
				{
					return Vec4{ _mm256_add_pd(lhs.vec, rhs.vec) };
				}
				else
				{
					return fallback();
				}
			}
		}
		friend constexpr Vec4 pr_vectorcall operator - (Vec4 lhs, Vec4 rhs)
		{
			auto fallback = [&]() constexpr { return Vec4{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w}; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					return Vec4{ _mm_sub_ps(lhs.vec, rhs.vec) };
				}
				else if constexpr (IntrinsicD)
				{
					return Vec4{ _mm256_sub_pd(lhs.vec, rhs.vec) };
				}
				else
				{
					return fallback();
				}
			}
		}
		friend constexpr Vec4 pr_vectorcall operator * (Vec4 lhs, Vec4 rhs)
		{
			auto fallback = [&]() constexpr { return Vec4{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w}; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					return Vec4{ _mm_mul_ps(lhs.vec, rhs.vec) };
				}
				else if constexpr (IntrinsicD)
				{
					return Vec4{ _mm256_mul_pd(lhs.vec, rhs.vec) };
				}
				else
				{
					return fallback();
				}
			}
		}
		friend constexpr Vec4 pr_vectorcall operator / (Vec4 lhs, Vec4 rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			auto fallback = [&]() constexpr { return Vec4{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w}; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					return Vec4{ _mm_div_ps(lhs.vec, rhs.vec) };
				}
				else if constexpr (IntrinsicD)
				{
					return Vec4{ _mm256_div_pd(lhs.vec, rhs.vec) };
				}
				else
				{
					return fallback();
				}
			}
		}
		friend constexpr Vec4 pr_vectorcall operator % (Vec4 lhs, Vec4 rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			auto fallback = [&]() constexpr { return Vec4{ Modulus(lhs.x, rhs.x), Modulus(lhs.y, rhs.y), Modulus(lhs.z, rhs.z), Modulus(lhs.w, rhs.w) }; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					auto div = _mm_div_ps(lhs.vec, rhs.vec);                                // a / b
					auto trunc = _mm_round_ps(div, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC); // trunc(a / b)
					auto prod = _mm_mul_ps(trunc, rhs.vec);                                 // trunc(a / b) * b
					auto rem = _mm_sub_ps(lhs.vec, prod);                                   // a - b * trunc(a / b) => fmod
					return Vec4{ rem };
				}
				else if constexpr (IntrinsicD)
				{
					auto div = _mm256_div_pd(lhs.vec, rhs.vec);                                // a / b
					auto trunc = _mm256_round_pd(div, _MM_FROUND_TO_ZERO | _MM_FROUND_NO_EXC); // trunc(a / b)
					auto prod = _mm256_mul_pd(trunc, rhs.vec);                                 // trunc(a / b) * b
					auto rem = _mm256_sub_pd(lhs.vec, prod);                                   // a - b * trunc(a / b) => fmod
					return Vec4{ rem };
				}
				else
				{
					return fallback();
				}
			}
		}
		friend constexpr bool pr_vectorcall operator == (Vec4 lhs, Vec4 rhs)
		{
			auto fallback = [&]() constexpr { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w; };
			if consteval
			{
				return fallback();
			}
			else
			{
				if constexpr (IntrinsicF)
				{
					return _mm_movemask_ps(_mm_cmpeq_ps(lhs.vec, rhs.vec)) == 0xF;
				}
				else if constexpr (IntrinsicD)
				{
					return _mm256_movemask_pd(_mm256_cmp_pd(lhs.vec, rhs.vec, _CMP_EQ_OQ)) == 0xF;
				}
				else
				{
					return fallback();
				}
			}
		}
		#pragma endregion
	};

	#define PR_MATH_DEFINE_TYPE(scalar)\
	template <> struct vector_traits<Vec4<scalar>>\
		: vector_traits_base<scalar, scalar, 4>\
		, vector_access_member<Vec4<scalar>, scalar, 4>\
	{};\
	\
	static_assert(VectorType<Vec4<scalar>>, "Vec4<"#scalar"> is not a valid vector type");\
	static_assert(sizeof(Vec4<scalar>) == 4*sizeof(scalar), "Vec4<"#scalar"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Vec4<scalar>>, "Vec4<"#scalar"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(float);
	PR_MATH_DEFINE_TYPE(double);
	PR_MATH_DEFINE_TYPE(int32_t);
	PR_MATH_DEFINE_TYPE(int64_t);
	#undef PR_MATH_DEFINE_TYPE
}
