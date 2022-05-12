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
	template <typename T>
	struct Vec2f
	{
		// Notes:
		//  - Can't use __m64 because it has an alignment of 8.
		//    v2 is a union member in v3 which needs alignment of 4, or the size of v3 becomes 16.

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x, y; };
			struct { float arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Vec2f() = default;
		constexpr explicit Vec2f(float x_)
			:x(x_)
			,y(x_)
		{}
		constexpr Vec2f(float x_, float y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Vec2f(float const* v)
			:Vec2f(v[0], v[1])
		{}
		template <maths::Vector2 V> constexpr explicit Vec2f(V const& v)
			:Vec2f(maths::comp<0>(v), maths::comp<1>(v))
		{}

		// Reinterpret as a different vector type
		template <typename U> explicit operator Vec2f<U> const&() const
		{
			return reinterpret_cast<Vec2f<U> const&>(*this);
		}
		template <typename U> explicit operator Vec2f<U>&()
		{
			return reinterpret_cast<Vec2f<U>&>(*this);
		}

		// Array access
		float const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		float& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Basic constants
		static constexpr Vec2f Zero()   { return Vec2f{0,0}; }
		static constexpr Vec2f XAxis()  { return Vec2f{1,0}; }
		static constexpr Vec2f YAxis()  { return Vec2f{0,1}; }

		// Construct normalised
		static Vec2f Normal(float x, float y)
		{
			return Normalise(Vec2f(x,y));
		}

		// Construct random
		template <typename Rng = std::default_random_engine> static Vec2f RandomN(Rng& rng)
		{
			// Create a random vector with unit length
			std::uniform_real_distribution<float> dist(-1.0f, 1.0f);
			for (;;)
			{
				auto x = dist(rng);
				auto y = dist(rng);
				auto v = v2(x, y);
				auto len = LengthSq(v);
				if (len > 0.01f && len <= 1.0f)
					return Normalise(v);
			}
		}
		template <typename Rng = std::default_random_engine> static Vec2f Random(Rng& rng, v2_cref<> vmin, v2_cref<> vmax)
		{
			// Create a random vector with components on interval ['vmin', 'vmax']
			std::uniform_real_distribution<float> dist_x(vmin.x, vmax.x);
			std::uniform_real_distribution<float> dist_y(vmin.y, vmax.y);
			return Vec2f(dist_x(rng), dist_y(rng));
		}
		template <typename Rng = std::default_random_engine> static Vec2f Random(Rng& rng, float min_length, float max_length)
		{
			// Create a random vector with length on interval [min_length, max_length]
			std::uniform_real_distribution<float> dist(min_length, max_length);
			return dist(rng) * RandomN(rng);
		}
		template <typename Rng = std::default_random_engine> static Vec2f Random(Rng& rng, v2_cref<> centre, float radius)
		{
			// Create a random vector centred on 'centre' with radius 'radius'
			return Random(rng, 0.0f, radius) + centre;
		}

		#pragma region Operators
		friend constexpr Vec2f operator + (v2_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec2f operator - (v2_cref<T> vec)
		{
			return Vec2f{-vec.x, -vec.y};
		}
		friend Vec2f operator * (float lhs, v2_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec2f operator * (v2_cref<T> lhs, float rhs)
		{
			return Vec2f{lhs.x * rhs, lhs.y * rhs};
		}
		friend Vec2f operator / (v2_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec2f{lhs.x / rhs, lhs.y / rhs};
		}
		friend Vec2f operator % (v2_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec2f{Fmod(lhs.x, rhs), Fmod(lhs.y, rhs)};
		}
		friend Vec2f operator + (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2f{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend Vec2f operator - (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2f{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend Vec2f operator * (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2f{lhs.x * rhs.x, lhs.y * rhs.y};
		}
		friend Vec2f operator / (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2f{lhs.x / rhs.x, lhs.y / rhs.y};
		}
		friend Vec2f operator % (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2f{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y)};
		}
		#pragma endregion
	};
	static_assert(sizeof(Vec2f<void>) == 8);
	static_assert(maths::Vector2<Vec2f<void>>);
	static_assert(std::is_trivially_copyable_v<Vec2f<void>>, "Vec2f must be a pod type");
	
	// Dot product: a.b
	template <typename T> constexpr float Dot(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}

	// Cross product: Dot2(Rotate90CW(lhs), rhs)
	template <typename T> constexpr float Cross(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y;
	}

	// Rotate a 2d vector by 90deg (when looking down the Z axis)
	template <typename T> constexpr Vec2f<T> Rotate90CW(v2_cref<T> v)
	{
		return Vec2f<T>(-v.y, v.x);
	}

	// Rotate a 2d vector by -90def (when looking down the Z axis)
	template <typename T> constexpr Vec2f<T> Rotate90CCW(v2_cref<T> v)
	{
		return Vec2f<T>(v.y, -v.x);
	}

	// Returns a vector with the 'xy' values permuted 'n' times. '0=xy, 1=yz'
	template <typename T> constexpr Vec2f<T> Permute(v2_cref<T> v, int n)
	{
		return (n%2) == 1 ? Vec2f<T>(v.y, v.x) : v;
	}

	// Returns a 2-bit bitmask of the quadrant the vector is in. 0=(-x,-y), 1=(+x,-y), 2=(-x,+y), 3=(+x,+y)
	template <typename T> constexpr uint32_t Quadrant(v2_cref<T> v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1);
	}

	// Divide a circle into N sectors and return an index for the sector that 'vec' is in
	template <typename T> inline int Sector(v2_cref<T> vec, int sectors)
	{
		return int(ATan2Positive(vec.y, vec.x) * sectors / maths::tau);
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector2Tests)
	{
		{// Create
			auto V0 = v2(1, 1);
			PR_CHECK(V0.x == 1.0f, true);
			PR_CHECK(V0.y == 1.0f, true);

			auto V1 = v2(1, 2);
			PR_CHECK(V1.x == 1.0f, true);
			PR_CHECK(V1.y == 2.0f, true);

			auto V2 = v2({3, 4});
			PR_CHECK(V2.x == 3.0f, true);
			PR_CHECK(V2.y == 4.0f, true);

			v2 V3 = {4, 5};
			PR_CHECK(V3.x == 4.0f, true);
			PR_CHECK(V3.y == 5.0f, true);

			auto V4 = v2::Normal(3, 4);
			PR_CHECK(FEql(V4, v2(0.6f, 0.8f)), true);
			PR_CHECK(FEql(V4[0], 0.6f), true);
			PR_CHECK(FEql(V4[1], 0.8f), true);
		}
		{// Operators
			auto V0 = v2(1, 2);
			auto V1 = v2(2, 3);

			PR_CHECK(FEql(V0 + V1, v2(3, 5)), true);
			PR_CHECK(FEql(V0 - V1, v2(-1, -1)), true);
			PR_CHECK(FEql(V0 * V1, v2(2, 6)), true);
			PR_CHECK(FEql(V0 / V1, v2(1.0f / 2.0f, 2.0f / 3.0f)), true);
			PR_CHECK(FEql(V0 % V1, v2(1, 2)), true);

			PR_CHECK(FEql(V0 * 3.0f, v2(3, 6)), true);
			PR_CHECK(FEql(V0 / 2.0f, v2(0.5f, 1.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, v2(1, 0)), true);

			PR_CHECK(FEql(3.0f * V0, v2(3, 6)), true);

			PR_CHECK(FEql(+V0, v2(1, 2)), true);
			PR_CHECK(FEql(-V0, v2(-1, -2)), true);

			PR_CHECK(V0 == v2(1, 2), true);
			PR_CHECK(V0 != v2(2, 1), true);
		}
		{// Min/Max/Clamp
			auto V0 = v2(1, 2);
			auto V1 = v2(-1, -2);
			auto V2 = v2(2, 4);

			PR_CHECK(FEql(Min(V0, V1, V2), v2(-1, -2)), true);
			PR_CHECK(FEql(Max(V0, V1, V2), v2(2, 4)), true);
			PR_CHECK(FEql(Clamp(V0, V1, V2), v2(1, 2)), true);
			PR_CHECK(FEql(Clamp(V0, 0.0f, 1.0f), v2(1, 1)), true);
		}
		{// Normalise
			auto arr0 = v2(1, 2);
			PR_CHECK(FEql(Normalise(v2::Zero(), arr0), arr0), true);
			PR_CHECK(FEql(Normalise(arr0), v2(0.4472136f, 0.8944272f)), true);
			PR_CHECK(IsNormal(Normalise(arr0)), true);
		}
		{// CosAngle
			v2 arr0(1, 0);
			v2 arr1(0, 1);
			PR_CHECK(FEql(CosAngle(1.0, 1.0, maths::root2) - Cos(DegreesToRadians(90.0)), 0), true);
			PR_CHECK(FEql(CosAngle(arr0, arr1) - Cos(DegreesToRadians(90.0f)), 0), true);
			PR_CHECK(FEql(Angle(1.0, 1.0, maths::root2), DegreesToRadians(90.0)), true);
			PR_CHECK(FEql(Angle(arr0, arr1), DegreesToRadians(90.0f)), true);
			PR_CHECK(FEql(Length(1.0f, 1.0f, DegreesToRadians(90.0f)), float(maths::root2)), true);
		}
	}
}
#endif
