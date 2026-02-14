//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/core/functions.h"

namespace pr::math
{
	template <ScalarType S>
	struct Vec2
	{
		// Notes:
		//  - Can't use __m64 because it has an alignment of 8.
		//    v2 is a member of the union in v3 which needs alignment of 4, or the size of v3 becomes 16.

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { S x, y; };
			struct { S arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Vec2() = default;
		constexpr Vec2(S x_)
			: x(x_)
			, y(x_)
		{}
		constexpr Vec2(S x_, S y_)
			: x(x_)
			, y(y_)
		{}
		constexpr explicit Vec2(std::ranges::random_access_range auto&& v)
			:Vec2(v[0], v[1])
		{}
		constexpr explicit Vec2(VectorTypeN<S, 2> auto v)
			:Vec2(vec(v).x, vec(v).y)
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

		// Constants
		static constexpr Vec2 Zero() noexcept
		{
			return Vec2{0, 0};
		}
		static constexpr Vec2 One() noexcept
		{
			return Vec2{1, 1};
		}
		static constexpr Vec2 Tiny() noexcept
		{
			return Vec2(tiny<S>, tiny<S>);
		}
		static constexpr Vec2 Min() noexcept
		{
			return Vec2(limits<S>::min(), limits<S>::min());
		}
		static constexpr Vec2 Max() noexcept
		{
			return Vec2(limits<S>::max(), limits<S>::max());
		}
		static constexpr Vec2 Lowest() noexcept
		{
			return Vec2(limits<S>::lowest(), limits<S>::lowest());
		}
		static constexpr Vec2 Epsilon() noexcept
		{
			return Vec2(limits<S>::epsilon(), limits<S>::epsilon());
		}
		static constexpr Vec2 Infinity() noexcept
		{
			return Vec2(limits<S>::infinity(), limits<S>::infinity());
		}
		static constexpr Vec2 XAxis() noexcept
		{
			return Vec2{1, 0};
		}
		static constexpr Vec2 YAxis() noexcept
		{
			return Vec2{0, 1};
		}
		static constexpr Vec2 Origin() noexcept
		{
			return Vec2{0, 0};
		}

		// Construct normalised
		static constexpr Vec2 Normal(S x, S y) requires std::floating_point<S>
		{
			return Normalise(Vec2(x, y));
		}
	};

	#define PR_MATH_DEFINE_TYPE(scalar)\
	template <> struct vector_traits<Vec2<scalar>>\
		: vector_traits_base<scalar, scalar, 2>\
		, vector_access_member<Vec2<scalar>, scalar, 2>\
	{};\
	\
	static_assert(VectorType<Vec2<scalar>>, "Vec2<"#scalar"> is not a valid vector type");\
	static_assert(sizeof(Vec2<scalar>) == 2*sizeof(scalar), "Vec2<"#scalar"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Vec2<scalar>>, "Vec2<"#scalar"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(float);
	PR_MATH_DEFINE_TYPE(double);
	PR_MATH_DEFINE_TYPE(int32_t);
	PR_MATH_DEFINE_TYPE(int64_t);
	#undef PR_MATH_DEFINE_TYPE
}
