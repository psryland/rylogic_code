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

		PRUnitTestMethod(Operators,
			Vec2<float>, Vec2<double>, Vec2<int32_t>, Vec2<int64_t>
			//Vec3<float>, Vec3<double>, Vec3<int32_t>, Vec3<int64_t>
			//Vec4<float>, Vec4<double>, Vec4<int32_t>, Vec4<int64_t>
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
		}
	};
}
#endif
