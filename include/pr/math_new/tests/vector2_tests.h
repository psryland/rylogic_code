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
		#if 0 // todo
		PRUnitTestMethod(Create, float, double, int32_t, int64_t)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V0 = vec2_t(S(1));
			PR_EXPECT(V0.x == S(1));
			PR_EXPECT(V0.y == S(1));

			auto V1 = vec2_t(S(1), S(2));
			PR_EXPECT(V1.x == S(1));
			PR_EXPECT(V1.y == S(2));

			auto V2 = vec2_t({ S(3), S(4) });
			PR_EXPECT(V2.x == S(3));
			PR_EXPECT(V2.y == S(4));

			vec2_t V3 = { S(4), S(5) };
			PR_EXPECT(V3.x == S(4));
			PR_EXPECT(V3.y == S(5));
		}
		PRUnitTestMethod(Normal, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V4 = vec2_t::Normal(S(3), S(4));
			auto V4_expected = vec2_t(S(0.6), S(0.8));
			PR_EXPECT(FEql(V4, V4_expected));
			PR_EXPECT(FEql(V4[0], S(0.6)));
			PR_EXPECT(FEql(V4[1], S(0.8)));
		}
		PRUnitTestMethod(Operators, float, double, int32_t, int64_t)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V0 = vec2_t(S(10), S(8));
			auto V1 = vec2_t(S(2), S(12));
			auto eql = [](auto lhs, auto rhs)
			{
				if constexpr (std::floating_point<S>)
					return FEql(lhs, rhs);
				else
					return lhs == rhs;
			};
			PR_EXPECT(eql(V0 + V1, vec2_t(S(+12), S(+20))));
			PR_EXPECT(eql(V0 - V1, vec2_t(S(+8), S(-4))));
			PR_EXPECT(eql(V0 * V1, vec2_t(S(+20), S(+96))));
			PR_EXPECT(eql(V0 / V1, vec2_t(S(+5), S(8) / S(12))));
			PR_EXPECT(eql(V0 % V1, vec2_t(S(+0), S(8))));

			PR_EXPECT(eql(V0 * S(3), vec2_t(S(30), S(24))));
			PR_EXPECT(eql(V0 / S(2), vec2_t(S(5), S(4))));
			PR_EXPECT(eql(V0 % S(2), vec2_t(S(0), S(0))));

			PR_EXPECT(eql(S(3) * V0, vec2_t(S(30), S(24))));

			PR_EXPECT(eql(+V0, vec2_t(S(+10), S(+8))));
			PR_EXPECT(eql(-V0, vec2_t(S(-10), S(-8))));

			PR_EXPECT(V0 == vec2_t(S(10), S(8)));
			PR_EXPECT(V0 != vec2_t(S(2), S(1)));

			// Implicit conversion to T==void
			vec2_t V2 = Vec2<S, int>(S(1));

			// Explicit cast to T!=void
			Vec2<S, int> V3 = static_cast<Vec2<S, int>>(V2);
		}
		PRUnitTestMethod(MinMaxClamp, float, double, int32_t, int64_t)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto V0 = vec2_t(S(+1), S(+2));
			auto V1 = vec2_t(S(-1), S(-2));
			auto V2 = vec2_t(S(+2), S(+4));

			PR_EXPECT(Min(V0, V1, V2) == vec2_t(S(-1), S(-2)));
			PR_EXPECT(Max(V0, V1, V2) == vec2_t(S(+2), S(+4)));
			PR_EXPECT(Clamp(V0, V1, V2) == vec2_t(S(1), S(2)));
			PR_EXPECT(Clamp(V0, S(0), S(1)) == vec2_t(S(1), S(1)));
		}
		PRUnitTestMethod(Normalise, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			auto arr0 = vec2_t(S(1), S(2));
			auto len = Length(arr0);
			PR_EXPECT(FEql(Normalise(vec2_t::Zero(), arr0), arr0));
			PR_EXPECT(FEql(Normalise(arr0), vec2_t(S(1) / len, S(2) / len)));
			PR_EXPECT(IsNormal(Normalise(arr0)));
		}
		PRUnitTestMethod(CosAngle, float, double)
		{
			using S = T;
			using vec2_t = Vec2<S, void>;

			vec2_t arr0(S(1), S(0));
			vec2_t arr1(S(0), S(1));
			PR_EXPECT(FEql(CosAngle(arr0, arr1) - Cos(DegreesToRadians(S(90))), S(0)));
			PR_EXPECT(FEql(Angle(arr0, arr1), DegreesToRadians(S(90))));
		}
		#endif
	};
}
#endif
