//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/math.h"

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::math
{
	PRUnitTestClass(Vector2)
	{
		PRUnitTestMethod(Construction, float, double, int32_t, int64_t)
		{
			using vec2_t = Vec2<T>;

			constexpr auto V0 = vec2_t(T(1));
			static_assert(V0.x == T(1));
			static_assert(V0.y == T(1));

			constexpr auto V1 = vec2_t(T(1), T(2));
			static_assert(V1.x == T(1));
			static_assert(V1.y == T(2));

			constexpr auto V2 = vec2_t({ T(3), T(4) });
			static_assert(V2.x == T(3));
			static_assert(V2.y == T(4));

			if constexpr (std::floating_point<T>)
			{
				constexpr auto V3 = vec2_t::Normal(T(3), T(4));
				static_assert(FEql(V3.x, T(0.6)));
				static_assert(FEql(V3.y, T(0.8)));
			}

			constexpr T arr[] = { T(5), T(6) };
			constexpr auto V4 = vec2_t(arr);
			static_assert(V4.x == T(5));
			static_assert(V4.y == T(6));
		}
	};
}
#endif
