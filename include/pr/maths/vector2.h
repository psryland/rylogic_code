//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"

namespace pr
{
	template <typename Scalar, typename T>
	struct Vec2
	{
		// Notes:
		//  - Can't use __m64 because it has an alignment of 8.
		//    v2 is a member of the union in v3 which needs alignment of 4, or the size of v3 becomes 16.
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Scalar x, y; };
			struct { Scalar arr[2]; };
		};
		#pragma warning(pop)

		using Vec2_cref = Vec2_cref<Scalar, T>;
		
		// Construct
		Vec2() = default;
		constexpr explicit Vec2(Scalar x_)
			:x(x_)
			,y(x_)
		{}
		constexpr Vec2(Scalar x_, Scalar y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Vec2(Scalar const* v)
			:Vec2(v[0], v[1])
		{}
		template <maths::Vector2 V> constexpr explicit Vec2(V const& v)
			:Vec2(maths::comp<0>(v), maths::comp<1>(v))
		{}

		// Reinterpret as a different vector type
		template <typename U> explicit operator Vec2<Scalar, U> const&() const
		{
			return reinterpret_cast<Vec2<Scalar, U> const&>(*this);
		}
		template <typename U> explicit operator Vec2<Scalar, U>&()
		{
			return reinterpret_cast<Vec2<Scalar, U>&>(*this);
		}
		operator Vec2<Scalar, void> const& () const
		{
			return reinterpret_cast<Vec2<Scalar, void> const&>(*this);
		}
		operator Vec2<Scalar, void>& ()
		{
			return reinterpret_cast<Vec2<Scalar, void>&>(*this);
		}

		// Array access
		Scalar const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Scalar& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Basic constants
		static constexpr Vec2 Zero()    { return Vec2(Scalar(0),Scalar(0)); }
		static constexpr Vec2 XAxis()   { return Vec2(Scalar(1),Scalar(0)); }
		static constexpr Vec2 YAxis()   { return Vec2(Scalar(0),Scalar(1)); }
		static constexpr Vec2 One()     { return Vec2(Scalar(1), Scalar(1)); }
		static constexpr Vec2 Tiny()    { return Vec2(tiny<Scalar>, tiny<Scalar>); }
		static constexpr Vec2 Min()     { return Vec2(limits<Scalar>::min(), limits<Scalar>::min()); }
		static constexpr Vec2 Max()     { return Vec2(limits<Scalar>::max(), limits<Scalar>::max()); }
		static constexpr Vec2 Lowest()  { return Vec2(limits<Scalar>::lowest(), limits<Scalar>::lowest()); }
		static constexpr Vec2 Epsilon() { return Vec2(limits<Scalar>::epsilon(), limits<Scalar>::epsilon()); }

		// Construct normalised
		static Vec2 Normal(Scalar x, Scalar y) requires std::is_floating_point_v<Scalar>
		{
			return Normalise(Vec2(x, y));
		}

		// Create a random vector with unit length
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec2 pr_vectorcall RandomN(Rng& rng)
		{
			std::uniform_real_distribution<Scalar> dist(Scalar(-1), Scalar(1));
			for (;;)
			{
				auto x = dist(rng);
				auto y = dist(rng);
				auto v = Vec2(x, y);
				auto len = LengthSq(v);
				if (len > Scalar(0.01) && len <= Scalar(1))
					return v / Sqrt(len);
			}
		}
		
		// Create a random vector with components on interval ['vmin', 'vmax']
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec2 pr_vectorcall Random(Rng& rng, Vec2_cref vmin, Vec2_cref vmax)
		{
			std::uniform_real_distribution<Scalar> dist_x(vmin.x, vmax.x);
			std::uniform_real_distribution<Scalar> dist_y(vmin.y, vmax.y);
			return Vec2(dist_x(rng), dist_y(rng));
		}

		// Create a random vector with length on interval [min_length, max_length]
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec2 pr_vectorcall Random(Rng& rng, Scalar min_length, Scalar max_length)
		{
			std::uniform_real_distribution<Scalar> dist(min_length, max_length);
			return dist(rng) * RandomN(rng);
		}

		// Create a random vector centred on 'centre' with radius 'radius'
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec2 pr_vectorcall Random(Rng& rng, Vec2_cref centre, Scalar radius)
		{
			return Random(rng, Scalar(0), radius) + centre;
		}

		#pragma region Operators
		friend constexpr Vec2 operator + (Vec2_cref vec)
		{
			return vec;
		}
		friend constexpr Vec2 operator - (Vec2_cref vec)
		{
			return Vec2(-vec.x, -vec.y);
		}
		friend Vec2 operator * (Scalar lhs, Vec2_cref rhs)
		{
			return rhs * lhs;
		}
		friend Vec2 operator * (Vec2_cref lhs, Scalar rhs)
		{
			return Vec2(lhs.x * rhs, lhs.y * rhs);
		}
		friend Vec2 operator / (Vec2_cref lhs, Scalar rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			return Vec2(lhs.x / rhs, lhs.y / rhs);
		}
		friend Vec2 operator % (Vec2_cref lhs, Scalar rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			if constexpr (std::floating_point<Scalar>)
				return Vec2(Fmod(lhs.x, rhs), Fmod(lhs.y, rhs));
			else
				return Vec2(lhs.x % rhs, lhs.y % rhs);
		}
		friend Vec2 operator + (Vec2_cref lhs, Vec2_cref rhs)
		{
			return Vec2(lhs.x + rhs.x, lhs.y + rhs.y);
		}
		friend Vec2 operator - (Vec2_cref lhs, Vec2_cref rhs)
		{
			return Vec2(lhs.x - rhs.x, lhs.y - rhs.y);
		}
		friend Vec2 operator * (Vec2_cref lhs, Vec2_cref rhs)
		{
			return Vec2(lhs.x * rhs.x, lhs.y * rhs.y);
		}
		friend Vec2 operator / (Vec2_cref lhs, Vec2_cref rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			return Vec2(lhs.x / rhs.x, lhs.y / rhs.y);
		}
		friend Vec2 operator % (Vec2_cref lhs, Vec2_cref rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			if constexpr (std::floating_point<Scalar>)
				return Vec2(Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y));
			else
				return Vec2(lhs.x % rhs.x, lhs.y % rhs.y);
		}
		#pragma endregion
	};
	#define PR_VEC2_CHECKS(scalar)\
	static_assert(sizeof(Vec2<scalar, void>) == 2*sizeof(scalar), "Vector<"#scalar"> has the wrong size");\
	static_assert(maths::Vector2<Vec2<scalar, void>>, "Vector<"#scalar"> is not a Vector2");\
	static_assert(std::is_trivially_copyable_v<Vec2<scalar, void>>, "Must be a pod type");
	PR_VEC2_CHECKS(float);
	PR_VEC2_CHECKS(double);
	PR_VEC2_CHECKS(int32_t);
	PR_VEC2_CHECKS(int64_t);
	#undef PR_VEC2_CHECKS

	// Dot product: a.b
	template <typename Scalar, typename T> constexpr Scalar pr_vectorcall Dot(Vec2_cref<Scalar, T> lhs, Vec2_cref<Scalar, T> rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}

	// Cross product: Dot2(Rotate90CW(lhs), rhs)
	template <typename Scalar, typename T> constexpr Scalar pr_vectorcall Cross(Vec2_cref<Scalar, T> lhs, Vec2_cref<Scalar, T> rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y;
	}

	// Rotate a 2d vector by 90deg (when looking down the Z axis)
	template <typename Scalar, typename T> constexpr Vec2<Scalar, T> pr_vectorcall Rotate90CW(Vec2_cref<Scalar, T> v)
	{
		return Vec2<T>(-v.y, v.x);
	}

	// Rotate a 2d vector by -90def (when looking down the Z axis)
	template <typename Scalar, typename T> constexpr Vec2<Scalar, T> pr_vectorcall Rotate90CCW(Vec2_cref<Scalar, T> v)
	{
		return Vec2<Scalar, T>(v.y, -v.x);
	}

	// Returns a vector with the 'xy' values permuted 'n' times. '0=xy, 1=yz'
	template <typename Scalar, typename T> constexpr Vec2<Scalar, T> pr_vectorcall Permute(Vec2_cref<Scalar, T> v, int n)
	{
		return (n%2) == 1 ? Vec2<Scalar, T>(v.y, v.x) : v;
	}

	// Returns a 2-bit bitmask of the quadrant the vector is in. 0=(-x,-y), 1=(+x,-y), 2=(-x,+y), 3=(+x,+y)
	template <typename Scalar, typename T> constexpr uint32_t pr_vectorcall Quadrant(Vec2_cref<Scalar, T> v)
	{
		return
			((v.x >= Scalar(0)) << 0) |
			((v.y >= Scalar(0)) << 1);
	}

	// Divide a circle into N sectors and return an index for the sector that 'vec' is in
	template <typename Scalar, typename T> inline int pr_vectorcall Sector(Vec2_cref<Scalar, T> vec, int sectors)
	{
		return static_cast<int>(ATan2Positive<double>(vec.y, vec.x) * sectors / maths::tau);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector2Tests, float, double, int32_t, int64_t)
	{
		using Scalar = T;
		using vec2_t = Vec2<Scalar, void>;

		{// Create
			auto V0 = vec2_t(Scalar(1));
			PR_CHECK(V0.x == Scalar(1), true);
			PR_CHECK(V0.y == Scalar(1), true);

			auto V1 = vec2_t(Scalar(1), Scalar(2));
			PR_CHECK(V1.x == Scalar(1), true);
			PR_CHECK(V1.y == Scalar(2), true);

			auto V2 = vec2_t({Scalar(3), Scalar(4)});
			PR_CHECK(V2.x == Scalar(3), true);
			PR_CHECK(V2.y == Scalar(4), true);

			vec2_t V3 = {Scalar(4), Scalar(5)};
			PR_CHECK(V3.x == Scalar(4), true);
			PR_CHECK(V3.y == Scalar(5), true);

			if constexpr (std::floating_point<Scalar>)
			{
				auto V4 = vec2_t::Normal(Scalar(3), Scalar(4));
				auto V4_expected = vec2_t(Scalar(0.6), Scalar(0.8));
				PR_CHECK(FEql(V4, V4_expected), true);
				PR_CHECK(FEql(V4[0], Scalar(0.6)), true);
				PR_CHECK(FEql(V4[1], Scalar(0.8)), true);
			}
		}
		{// Operators
			auto V0 = vec2_t(Scalar(10), Scalar(8));
			auto V1 = vec2_t(Scalar(2), Scalar(12));
			auto eql = [](auto lhs, auto rhs)
			{
				if constexpr(std::floating_point<Scalar>)
					return FEql(lhs, rhs);
				else
					return lhs == rhs;
			};
			PR_CHECK(eql(V0 + V1, vec2_t(Scalar(+12), Scalar(+20))), true);
			PR_CHECK(eql(V0 - V1, vec2_t(Scalar(+8), Scalar(-4))), true);
			PR_CHECK(eql(V0 * V1, vec2_t(Scalar(+20), Scalar(+96))), true);
			PR_CHECK(eql(V0 / V1, vec2_t(Scalar(+5), Scalar(8) / Scalar(12))), true);
			PR_CHECK(eql(V0 % V1, vec2_t(Scalar(+0), Scalar(8))), true);

			PR_CHECK(eql(V0 * Scalar(3), vec2_t(Scalar(30), Scalar(24))), true);
			PR_CHECK(eql(V0 / Scalar(2), vec2_t(Scalar(5), Scalar(4))), true);
			PR_CHECK(eql(V0 % Scalar(2), vec2_t(Scalar(0), Scalar(0))), true);

			PR_CHECK(eql(Scalar(3) * V0, vec2_t(Scalar(30), Scalar(24))), true);

			PR_CHECK(eql(+V0, vec2_t(Scalar(+10), Scalar(+8))), true);
			PR_CHECK(eql(-V0, vec2_t(Scalar(-10), Scalar(-8))), true);

			PR_CHECK(V0 == vec2_t(Scalar(10), Scalar(8)), true);
			PR_CHECK(V0 != vec2_t(Scalar(2), Scalar(1)), true);

			// Implicit conversion to T==void
			vec2_t V2 = Vec2<Scalar, int>(Scalar(1));

			// Explicit cast to T!=void
			Vec2<Scalar, int> V3 = static_cast<Vec2<Scalar, int>>(V2);
		}
		{// Min/Max/Clamp
			auto V0 = vec2_t(Scalar(+1), Scalar(+2));
			auto V1 = vec2_t(Scalar(-1), Scalar(-2));
			auto V2 = vec2_t(Scalar(+2), Scalar(+4));

			PR_CHECK(Min(V0, V1, V2) == vec2_t(Scalar(-1), Scalar(-2)), true);
			PR_CHECK(Max(V0, V1, V2) == vec2_t(Scalar(+2), Scalar(+4)), true);
			PR_CHECK(Clamp(V0, V1, V2) == vec2_t(Scalar(1), Scalar(2)), true);
			PR_CHECK(Clamp(V0, Scalar(0), Scalar(1)) == vec2_t(Scalar(1), Scalar(1)), true);
		}
		{// Normalise
			if constexpr (std::floating_point<Scalar>)
			{
				auto arr0 = vec2_t(Scalar(1), Scalar(2));
				auto len = Length(arr0);
				PR_CHECK(FEql(Normalise(vec2_t::Zero(), arr0), arr0), true);
				PR_CHECK(FEql(Normalise(arr0), vec2_t(Scalar(1)/len, Scalar(2)/len)), true);
				PR_CHECK(IsNormal(Normalise(arr0)), true);
			}
		}
		{// CosAngle
			if constexpr (std::floating_point<Scalar>)
			{
				vec2_t arr0(Scalar(1), Scalar(0));
				vec2_t arr1(Scalar(0), Scalar(1));
				PR_CHECK(FEql(CosAngle(arr0, arr1) - Cos(DegreesToRadians(Scalar(90))), Scalar(0)), true);
				PR_CHECK(FEql(Angle(arr0, arr1), DegreesToRadians(Scalar(90))), true);
			}
		}
	}
}
#endif
