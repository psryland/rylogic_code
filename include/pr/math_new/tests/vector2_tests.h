//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "../math.h"

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::math
{
	PRUnitTestClass(Vector2)
	{
		PRUnitTestMethod(Create, float, double, int32_t, int64_t)
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
		}
		#if 0 // todo
		PRUnitTestMethod(Normal, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V4 = vec2_t::Normal(T(3), T(4));
			auto V4_expected = vec2_t(T(0.6), T(0.8));
			PR_EXPECT(FEql(V4, V4_expected));
			PR_EXPECT(FEql(V4[0], T(0.6)));
			PR_EXPECT(FEql(V4[1], T(0.8)));
		}
		PRUnitTestMethod(Operators, float, double, int32_t, int64_t)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V0 = vec2_t(T(10), T(8));
			auto V1 = vec2_t(T(2), T(12));
			auto eql = [](auto lhs, auto rhs)
			{
				if constexpr (std::floating_point<S>)
					return FEql(lhs, rhs);
				else
					return lhs == rhs;
			};
			PR_EXPECT(eql(V0 + V1, vec2_t(T(+12), T(+20))));
			PR_EXPECT(eql(V0 - V1, vec2_t(T(+8), T(-4))));
			PR_EXPECT(eql(V0 * V1, vec2_t(T(+20), T(+96))));
			PR_EXPECT(eql(V0 / V1, vec2_t(T(+5), T(8) / T(12))));
			PR_EXPECT(eql(V0 % V1, vec2_t(T(+0), T(8))));

			PR_EXPECT(eql(V0 * T(3), vec2_t(T(30), T(24))));
			PR_EXPECT(eql(V0 / T(2), vec2_t(T(5), T(4))));
			PR_EXPECT(eql(V0 % T(2), vec2_t(T(0), T(0))));

			PR_EXPECT(eql(T(3) * V0, vec2_t(T(30), T(24))));

			PR_EXPECT(eql(+V0, vec2_t(T(+10), T(+8))));
			PR_EXPECT(eql(-V0, vec2_t(T(-10), T(-8))));

			PR_EXPECT(V0 == vec2_t(T(10), T(8)));
			PR_EXPECT(V0 != vec2_t(T(2), T(1)));

			// Implicit conversion to T==void
			vec2_t V2 = Vec2<S, int>(T(1));

			// Explicit cast to T!=void
			Vec2<S, int> V3 = static_cast<Vec2<S, int>>(V2);
		}
		PRUnitTestMethod(MinMaxClamp, float, double, int32_t, int64_t)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V0 = vec2_t(T(+1), T(+2));
			auto V1 = vec2_t(T(-1), T(-2));
			auto V2 = vec2_t(T(+2), T(+4));

			PR_EXPECT(Min(V0, V1, V2) == vec2_t(T(-1), T(-2)));
			PR_EXPECT(Max(V0, V1, V2) == vec2_t(T(+2), T(+4)));
			PR_EXPECT(Clamp(V0, V1, V2) == vec2_t(T(1), T(2)));
			PR_EXPECT(Clamp(V0, T(0), T(1)) == vec2_t(T(1), T(1)));
		}
		PRUnitTestMethod(Normalise, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto arr0 = vec2_t(T(1), T(2));
			auto len = Length(arr0);
			PR_EXPECT(FEql(Normalise(vec2_t::Zero(), arr0), arr0));
			PR_EXPECT(FEql(Normalise(arr0), vec2_t(T(1) / len, T(2) / len)));
			PR_EXPECT(IsNormal(Normalise(arr0)));
		}
		PRUnitTestMethod(CosAngle, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			vec2_t arr0(T(1), T(0));
			vec2_t arr1(T(0), T(1));
			PR_EXPECT(FEql(CosAngle(arr0, arr1) - CoT(DegreesToRadianT(T(90))), T(0)));
			PR_EXPECT(FEql(Angle(arr0, arr1), DegreesToRadianT(T(90))));
		}
		#endif
	};
}
#endif
