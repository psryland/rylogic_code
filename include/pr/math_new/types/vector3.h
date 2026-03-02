//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/types/vector2.h"

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
		constexpr Vec3(S x_) noexcept
			: x(x_)
			, y(x_)
			, z(x_)
		{}
		constexpr Vec3(S x_, S y_, S z_) noexcept
			: x(x_)
			, y(y_)
			, z(z_)
		{}
		constexpr explicit Vec3(VectorTypeN<S, 3> auto v) noexcept
			:Vec3(vec(v).x, vec(v).y, vec(v).z)
		{}
		constexpr Vec3(Vec2<S> v, S z_) noexcept
			:Vec3(v.x, v.y, z_)
		{}
		constexpr Vec3(AxisId axis_id) noexcept
			:Vec3(
				Abs(axis_id) == AxisId::PosX ? static_cast<S>(Sign<int>(axis_id)) : S(0),
				Abs(axis_id) == AxisId::PosY ? static_cast<S>(Sign<int>(axis_id)) : S(0),
				Abs(axis_id) == AxisId::PosZ ? static_cast<S>(Sign<int>(axis_id)) : S(0)
			)
		{}
		constexpr explicit Vec3(std::ranges::random_access_range auto&& v) noexcept
			:Vec3(v[0], v[1], v[2])
		{}

		// Explicit cast to different Scalar type
		template <ScalarType S2> constexpr explicit operator Vec3<S2>() const noexcept
		{
			return Vec3<S2>(
				static_cast<S2>(x),
				static_cast<S2>(y),
				static_cast<S2>(z)
			);
		}

		// Array access
		constexpr S operator [] (int i) const noexcept
		{
			pr_assert(i >= 0 && i < 3 && "index out of range");
			if consteval { return i == 0 ? x : i == 1 ? y : z; }
			else { return arr[i]; }
		}
		constexpr S& operator [] (int i) noexcept
		{
			pr_assert(i >= 0 && i < 3 && "index out of range");
			if consteval { return i == 0 ? x : i == 1 ? y : z; }
			else { return arr[i]; }
		}

		// Create other vector types
		constexpr Vec4<S> w0() const noexcept;
		constexpr Vec4<S> w1() const noexcept;
		constexpr Vec2<S> vec2(int i0, int i1) const noexcept
		{
			return Vec2<S>(arr[i0], arr[i1]);
		}

		// Constants
		static constexpr Vec3 Zero()      noexcept
		{
			return Vec3(S(0), S(0), S(0));
		}
		static constexpr Vec3 One()       noexcept
		{
			return Vec3(S(1), S(1), S(1));
		}
		static constexpr Vec3 Tiny()      noexcept
		{
			return Vec3(tiny<S>, tiny<S>, tiny<S>);
		}
		static constexpr Vec3 Min()       noexcept
		{
			return Vec3(limits<S>::min(), limits<S>::min(), limits<S>::min());
		}
		static constexpr Vec3 Max()       noexcept
		{
			return Vec3(limits<S>::max(), limits<S>::max(), limits<S>::max());
		}
		static constexpr Vec3 Lowest()    noexcept
		{
			return Vec3(limits<S>::lowest(), limits<S>::lowest(), limits<S>::lowest());
		}
		static constexpr Vec3 Epsilon()   noexcept
		{
			return Vec3(limits<S>::epsilon(), limits<S>::epsilon(), limits<S>::epsilon());
		}
		static constexpr Vec3 Infinity()  noexcept
		{
			return Vec3(limits<S>::infinity(), limits<S>::infinity(), limits<S>::infinity());
		}
		static constexpr Vec3 XAxis()     noexcept
		{
			return Vec3(S(1), S(0), S(0));
		}
		static constexpr Vec3 YAxis()     noexcept
		{
			return Vec3(S(0), S(1), S(0));
		}
		static constexpr Vec3 ZAxis()     noexcept
		{
			return Vec3(S(0), S(0), S(1));
		}
		static constexpr Vec3 Origin()    noexcept
		{
			return Vec3(S(0), S(0), S(0));
		}
		
		// Construct normalised
		static constexpr Vec3 Normal(S x, S y, S z) noexcept requires std::floating_point<S>
		{
			return Normalise(Vec3(x, y, z));
		}
	};
	
	#define PR_MATH_DEFINE_TYPE(element)\
	template <> struct vector_traits<Vec3<element>>\
		: vector_traits_base<element, element, 3>\
		, vector_access_member<Vec3<element>, element, 3>\
	{};\
	\
	static_assert(VectorType<Vec3<element>>, "Vec3<"#element"> is not a valid vector type");\
	static_assert(IsRank1<Vec3<element>>, "Vec3<"#element"> is not rank 1");\
	static_assert(sizeof(Vec3<element>) == 3*sizeof(element), "Vec3<"#element"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Vec3<element>>, "Vec3<"#element"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(float);
	PR_MATH_DEFINE_TYPE(double);
	PR_MATH_DEFINE_TYPE(int32_t);
	PR_MATH_DEFINE_TYPE(int64_t);
	#undef PR_MATH_DEFINE_TYPE
}
