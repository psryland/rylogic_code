//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2.h"

namespace pr
{
	template <typename T>
	struct Vec3f
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x, y, z; };
			struct { Vec2f<T> xy; };
			struct { float arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Vec3f() = default;
		constexpr explicit Vec3f(float x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{}
		constexpr Vec3f(float x_, float y_, float z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		constexpr explicit Vec3f(float const* v)
			:Vec3f(v[0], v[1], v[2])
		{}
		template <maths::Vector3 V> constexpr explicit Vec3f(V const& v)
			:Vec3f(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v))
		{}
		template <maths::Vector2 V> constexpr Vec3f(V const& v, float z_)
			:Vec3f(maths::comp<0>(v), maths::comp<1>(v), z_)
		{}

		// Reinterpret as a different vector type
		template <typename U> explicit operator Vec3f<U> const& () const
		{
			return reinterpret_cast<Vec3f<U> const&>(*this);
		}
		template <typename U> explicit operator Vec3f<U>&()
		{
			return reinterpret_cast<Vec3f<U>&>(*this);
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

		// Create other vector types
		Vec4f<T> w0() const;
		Vec4f<T> w1() const;
		Vec2f<T> vec2(int i0, int i1) const
		{
			return Vec2f<T>(arr[i0], arr[i1]);
		}

		// Basic constants
		static constexpr Vec3f Zero()   { return Vec3f{0,0,0}; }
		static constexpr Vec3f XAxis()  { return Vec3f{1,0,0}; }
		static constexpr Vec3f YAxis()  { return Vec3f{0,1,0}; }
		static constexpr Vec3f ZAxis()  { return Vec3f{0,0,1}; }

		// Construct normalised
		static Vec3f Normal(float x, float y, float z)
		{
			return Normalise(Vec3f(x,y,z));
		}

		// Construct random
		template <typename Rng = std::default_random_engine> static Vec3f RandomN(Rng& rng)
		{
			// Create a random vector with unit length
			std::uniform_real_distribution<float> dist(-1.0f, 1.0f);
			for (;;)
			{
				auto x = dist(rng);
				auto y = dist(rng);
				auto z = dist(rng);
				auto v = Vec3f(x, y, z);
				auto len = LengthSq(v);
				if (len > 0.01f && len <= 1.0f)
					return v / Sqrt(len);
			}
		}
		template <typename Rng = std::default_random_engine> static Vec3f Random(Rng& rng, v3_cref<> vmin, v3_cref<> vmax)
		{
			// Create a random vector with components on interval ['vmin', 'vmax']
			std::uniform_real_distribution<float> dist_x(vmin.x, vmax.x);
			std::uniform_real_distribution<float> dist_y(vmin.y, vmax.y);
			std::uniform_real_distribution<float> dist_z(vmin.z, vmax.z);
			return Vec3f(dist_x(rng), dist_y(rng), dist_z(rng));
		}
		template <typename Rng = std::default_random_engine> static Vec3f Random(Rng& rng, float min_length, float max_length)
		{
			// Create a random vector with length on interval [min_length, max_length]
			std::uniform_real_distribution<float> dist(min_length, max_length);
			return dist(rng) * RandomN(rng);
		}
		template <typename Rng = std::default_random_engine> static Vec3f Random(Rng& rng, v3_cref<> centre, float radius)
		{
			// Create a random vector centred on 'centre' with radius 'radius'
			return Random(rng, 0.0f, radius) + centre;
		}

		#pragma region Operators
		friend constexpr Vec3f operator + (v3_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec3f operator - (v3_cref<T> vec)
		{
			return Vec3f(-vec.x, -vec.y, -vec.z);
		}
		friend Vec3f operator * (float lhs, v3_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec3f operator * (v3_cref<T> lhs, float rhs)
		{
			return Vec3f{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend Vec3f operator / (v3_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec3f{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend Vec3f operator % (v3_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec3f{Fmod(lhs.x, rhs), Fmod(lhs.y, rhs), Fmod(lhs.z, rhs)};
		}
		friend Vec3f operator + (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3f{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Vec3f operator - (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3f{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec3f operator * (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3f{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z};
		}
		friend Vec3f operator / (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && !Any3(rhs, IsZero<float>));
			return Vec3f{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z};
		}
		friend Vec3f operator % (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && !Any3(rhs, IsZero<float>));
			return Vec3f{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y), Fmod(lhs.z, rhs.z)};
		}
		#pragma endregion
	};
	static_assert(sizeof(Vec3f<void>) == 12);
	static_assert(maths::Vector3<Vec3f<void>>);
	static_assert(std::is_trivially_copyable_v<Vec3f<void>>, "v3 must be a pod type");

	// Dot product: a . b
	template <typename T> constexpr float Dot(v3_cref<T> a, v3_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	// Cross product: a x b
	template <typename T> constexpr Vec3f<T> Cross(v3_cref<T> a, v3_cref<T> b)
	{
		return Vec3f<T>{a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x};
	}

	// Triple product: a . b x c
	template <typename T> constexpr float Triple(v3_cref<T> a, v3_cref<T> b, v3_cref<T> c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Returns a vector with the values permuted 'n' times. 0=xyz, 1=yzx, 2=zxy, etc
	template <typename T> constexpr Vec3f<T> Permute(v3_cref<T> v, int n)
	{
		switch (n % 3)
		{
			case 1:  return Vec3f<T>{v.y, v.z, v.x};
			case 2:  return Vec3f<T>{v.z, v.x, v.y};
			default: return v;
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	template <typename T> constexpr uint32_t Octant(v3_cref<T> v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1) | ((v.z >= 0.0f) << 2);
	}

}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector3Tests)
	{
		{// Create
			auto V0 = v3(1,1,1);
			PR_CHECK(V0.x == 1.0f, true);
			PR_CHECK(V0.y == 1.0f, true);
			PR_CHECK(V0.z == 1.0f, true);

			auto V1 = v3(1, 2, 3);
			PR_CHECK(V1.x == 1.0f, true);
			PR_CHECK(V1.y == 2.0f, true);

			auto V2 = v3({3, 4, 5});
			PR_CHECK(V2.x == 3.0f, true);
			PR_CHECK(V2.y == 4.0f, true);
			PR_CHECK(V2.z == 5.0f, true);

			v3 V3 = {4, 5, 6};
			PR_CHECK(V3[0] == 4.0f, true);
			PR_CHECK(V3[1] == 5.0f, true);
			PR_CHECK(V3[2] == 6.0f, true);

			auto V4 = v3::Normal(3, 4, 5);
			PR_CHECK(FEql(V4, v3(0.42426406871192f, 0.56568542494923f, 0.70710678118654f)), true);
		}	}
}
#endif
