//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "../core/forward.h"
#include "../types/vector2.h"
#include "../types/vector3.h"
// No non-standard dependencies outside of './'

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
	};
}
