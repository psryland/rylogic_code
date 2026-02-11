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
	PRUnitTestClass(Vector)
	{
		// Notes:
		//  - Tests for arbitrary vector types

		template <VectorType Vec> constexpr bool Eql(Vec lhs, Vec rhs)
		{
			using vt = vector_traits<Vec>;

			if constexpr (std::floating_point<typename vt::element_t>)
				return FEql(lhs, rhs);
			else
				return lhs == rhs;
		};

		PRUnitTestMethod(Operators
			,Vec2<float>, Vec2<double>, Vec2<int32_t>, Vec2<int64_t>
			,Vec3<float>, Vec3<double>, Vec3<int32_t>, Vec3<int64_t>
			,Vec4<float>, Vec4<double>, Vec4<int32_t>, Vec4<int64_t>
		){
			using vec_t = T;

			constexpr auto V0 = vec_t(T(2));
			constexpr auto V1 = vec_t(T(3));

			static_assert(Eql(+V0, vec_t(T(+2))));
			static_assert(Eql(-V0, vec_t(T(-2))));

			static_assert(Eql(V0 + V1, vec_t(T(+5))));
			static_assert(Eql(V0 - V1, vec_t(T(-1))));
			static_assert(Eql(V0 * V1, vec_t(T(+6))));
			static_assert(Eql(V0 / V1, vec_t(T(2) / T(3))));
			PR_EXPECT(Eql(V0 % V1, vec_t(T(+2))));

			static_assert(Eql(V0 * T(3), vec_t(T(6))));
			static_assert(Eql(V0 / T(2), vec_t(T(1))));
			PR_EXPECT(Eql(V0 % T(2), vec_t(T(0))));

			static_assert(Eql(T(3) * V0, vec_t(T(6))));
			static_assert(Eql(T(8) / V0, vec_t(T(4))));

			static_assert(V0 == vec_t(T(2)));
			static_assert(V0 != vec_t(T(3)));
	
			static_assert(V0 < V1);
			static_assert(V1 > V0);
			static_assert(!(V0 >= V1));
			static_assert(!(V1 <= V0));

			// Bitwise, shift, and logical operators (integer types only)
			if constexpr (std::integral<typename vector_traits<vec_t>::element_t>)
			{
				using E = typename vector_traits<vec_t>::element_t;
				constexpr auto VZ = vec_t(E(0));

				static_assert(Eql(~V0, vec_t(static_cast<E>(~E(2)))));
				static_assert(Eql(!VZ, vec_t(E(1))));
				static_assert(Eql(!V0, vec_t(E(0))));
				static_assert(Eql(V0 | V1, vec_t(static_cast<E>(E(2) | E(3)))));
				static_assert(Eql(V0 & V1, vec_t(static_cast<E>(E(2) & E(3)))));
				static_assert(Eql(V0 ^ V1, vec_t(static_cast<E>(E(2) ^ E(3)))));
				static_assert(Eql(V0 << E(1), vec_t(static_cast<E>(E(2) << E(1)))));
				static_assert(Eql(V0 << vec_t(E(1)), vec_t(static_cast<E>(E(2) << E(1)))));
				static_assert(Eql(V0 >> E(1), vec_t(static_cast<E>(E(2) >> E(1)))));
				static_assert(Eql(V0 >> vec_t(E(1)), vec_t(static_cast<E>(E(2) >> E(1)))));
				static_assert(Eql(V0 || VZ, vec_t(E(1))));
				static_assert(Eql(VZ || VZ, vec_t(E(0))));
				static_assert(Eql(V0 && V1, vec_t(E(1))));
				static_assert(Eql(V0 && VZ, vec_t(E(0))));
			}
		}
		PRUnitTestMethod(Constants,
			Vec2<float>, Vec2<double>, Vec2<int32_t>, Vec2<int64_t>
		){
			using vec_t = T;

			#if 0
			static_assert(Zero<vec_t>() == vec_t(T(0)));
			static_assert(Min<vec_t>() == std::numeric_limits<typename vector_traits<vec_t>::element_t>::lowest());
			static_assert(Max<vec_t>() == std::numeric_limits<typename vector_traits<vec_t>::element_t>::max());
			static_assert(XAxis<vec_t>() == vec_t({ T(1), T(0), T(0), T(0) }));
			static_assert(YAxis<vec_t>() == vec_t({ T(0), T(1), T(0), T(0) }));
			static_assert(ZAxis<vec_t>() == vec_t({ T(0), T(0), T(1), T(0) }));
			static_assert(WAxis<vec_t>() == vec_t({ T(0), T(0), T(0), T(1) }));
			static_assert(Origin<vec_t>() == vec_t({ T(0), T(0), T(0), T(1) }));
			#endif
		}
		



		#if 0 // todo

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
