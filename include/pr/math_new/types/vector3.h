//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "../core/forward.h"
#include "../types/vector2.h"

namespace pr::math
{
	template <ScalarType S>
	struct Vec3
	{
		// Notes:
		//  - Can't use __m64 because it has an alignment of 8.
		//    v2 is a member of the union in v3 which needs alignment of 4, or the size of v3 becomes 16.
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { S x, y, z; };
			struct { Vec2<S> xy; };
			struct { S arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Vec3() = default;
		constexpr Vec3(S x_)
			: x(x_)
			, y(x_)
			, z(x_)
		{}
		constexpr Vec3(S x_, S y_, S z_)
			: x(x_)
			, y(y_)
			, z(z_)
		{}

		//
	};
	
	#define PR_MATH_DEFINE_TYPE(scalar)\
	template <> struct vector_traits<Vec3<scalar>>\
		: vector_traits_base<scalar, scalar, 3>\
		, vector_access_member<Vec3<scalar>, scalar, 3>\
	{};\
	\
	static_assert(VectorType<Vec3<scalar>>, "Vec3<"#scalar"> is not a valid vector type");\
	static_assert(sizeof(Vec3<scalar>) == 3*sizeof(scalar), "Vec3<"#scalar"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Vec3<scalar>>, "Vec3<"#scalar"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(float);
	PR_MATH_DEFINE_TYPE(double);
	PR_MATH_DEFINE_TYPE(int32_t);
	PR_MATH_DEFINE_TYPE(int64_t);
	#undef PR_MATH_DEFINE_TYPE
}
