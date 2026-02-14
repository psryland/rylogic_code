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
	PRUnitTestClass(Matrix)
	{
		// Notes:
		//  - Tests for arbitrary matrix types

			#if 0
		PRUnitTestMethod(Constants,
			Mat4x4<float>, Mat4x4<double>, Mat4x4<int32_t>, Mat4x4<int64_t>
		) {
			using mat_t = T;
			static_assert(Identity<mat_t>() == {
				{ T(1), T(0), T(0), T(0) },
				{ T(0), T(1), T(0), T(0) },
				{ T(0), T(0), T(1), T(0) },
				{ T(0), T(0), T(0), T(1) },
			};
		}
			#endif
	};
}
#endif
