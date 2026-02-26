//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/core/functions.h"
#include "pr/math_new/types/vector2.h"

namespace pr::math
{
	template <ScalarType S>
	struct Mat2x2
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec2<S> x, y; };
			struct { Vec2<S> arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Mat2x2() = default;
		constexpr explicit Mat2x2(S x_)
			:x(x_)
			,y(x_)
		{}
		constexpr Mat2x2(S xx, S xy, S yx, S yy)
			:x(xx, xy)
			,y(yx, yy)
		{}
		constexpr Mat2x2(Vec2<S> x_, Vec2<S> y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Mat2x2(std::ranges::random_access_range auto&& v)
			:Mat2x2(
				Vec2<S>(v[0], v[1]),
				Vec2<S>(v[2], v[3]))
		{}

		// Array access
		constexpr Vec2<S> const& operator [](int i) const
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}
		constexpr Vec2<S>& operator [](int i)
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}

		// Constants
		static constexpr Mat2x2 Zero()
		{
			return math::Zero<Mat2x2>();
		}
		static constexpr Mat2x2 Identity()
		{
			return math::Identity<Mat2x2>();
		}

		// Create a rotation matrix
		static Mat2x2 Rotation(S angle)
		{
			return math::Rotation<Mat2x2>(angle);
		}

		// Create a scale matrix
		static constexpr Mat2x2 Scale(S scale)
		{
			return math::Scale<Mat2x2>(scale);
		}
		static constexpr Mat2x2 Scale(S sx, S sy)
		{
			return math::Scale<Mat2x2>(Vec2<S>(sx, sy));
		}

		// Create a shear matrix
		static Mat2x2 Shear(S sxy, S syx)
		{
			return math::Shear<Mat2x2>(sxy, syx);
		}
	};

	#define PR_MATH_DEFINE_TYPE(component, element)\
	template <> struct vector_traits<Mat2x2<element>>\
		: vector_traits_base<element, component, 2>\
		, vector_access_member<Mat2x2<element>, component, 2>\
	{};\
	\
	static_assert(VectorType<Mat2x2<element>>, "Mat2x2<"#element"> is not a valid vector type");\
	static_assert(IsRank2<Mat2x2<element>>, "Mat2x2<"#element"> is not rank 2");\
	static_assert(sizeof(Mat2x2<element>) == 2*2*sizeof(element), "Mat2x2<"#element"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Mat2x2<element>>, "Mat2x2<"#element"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(Vec2<float>, float);
	PR_MATH_DEFINE_TYPE(Vec2<double>, double);
	PR_MATH_DEFINE_TYPE(Vec2<int32_t>, int32_t);
	PR_MATH_DEFINE_TYPE(Vec2<int64_t>, int64_t);
	#undef PR_MATH_DEFINE_TYPE
}


		//#pragma region Operators
		//friend constexpr Mat2x2 operator + (Mat2x2_cref<S,A,B> mat)
		//{
		//	return mat;
		//}
		//friend constexpr Mat2x2 operator - (Mat2x2_cref<S,A,B> mat)
		//{
		//	return Mat2x2{-mat.x, -mat.y};
		//}
		//friend Mat2x2 operator * (S lhs, Mat2x2_cref<S,A,B> rhs)
		//{
		//	return rhs * lhs;
		//}
		//friend Mat2x2 operator * (Mat2x2_cref<S,A,B> lhs, S rhs)
		//{
		//	return Mat2x2{lhs.x * rhs, lhs.y * rhs};
		//}
		//friend Mat2x2 operator / (Mat2x2_cref<S,A,B> lhs, S rhs)
		//{
		//	// Don't check for divide by zero by default. For floats +inf/-inf are valid results
		//	//pr_assert("divide by zero" && rhs != 0);
		//	return Mat2x2{lhs.x / rhs, lhs.y / rhs};
		//}
		//friend Mat2x2 operator % (Mat2x2_cref<S,A,B> lhs, S rhs)
		//{
		//	// Don't check for divide by zero by default. For floats +inf/-inf are valid results
		//	//pr_assert("divide by zero" && rhs != 0);
		//	return Mat2x2{lhs.x % rhs, lhs.y % rhs};
		//}
		//friend Mat2x2 operator + (Mat2x2_cref<S,A,B> lhs, Mat2x2_cref<S,A,B> rhs)
		//{
		//	return Mat2x2{lhs.x + rhs.x, lhs.y + rhs.y};
		//}
		//friend Mat2x2 operator - (Mat2x2_cref<S,A,B> lhs, Mat2x2_cref<S,A,B> rhs)
		//{
		//	return Mat2x2{lhs.x - rhs.x, lhs.y - rhs.y};
		//}
		//#pragma endregions