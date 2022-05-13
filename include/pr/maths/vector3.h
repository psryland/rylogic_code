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
	template <typename Scalar, typename T>
	struct Vec3
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Scalar x, y, z; };
			struct { Vec2<Scalar, T> xy; };
			struct { Scalar arr[3]; };
		};
		#pragma warning(pop)

		using Vec3_cref = Vec3_cref<Scalar, T>;

		// Construct
		Vec3() = default;
		constexpr explicit Vec3(Scalar x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{}
		constexpr Vec3(Scalar x_, Scalar y_, Scalar z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		constexpr explicit Vec3(Scalar const* v)
			:Vec3(v[0], v[1], v[2])
		{}
		template <maths::Vector3 V> constexpr explicit Vec3(V const& v)
			:Vec3(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v))
		{}
		template <maths::Vector2 V> constexpr Vec3(V const& v, Scalar z_)
			:Vec3(maths::comp<0>(v), maths::comp<1>(v), z_)
		{}

		// Reinterpret as a different vector type
		template <typename U> explicit operator Vec3<Scalar, U> const& () const
		{
			return reinterpret_cast<Vec3<Scalar, U> const&>(*this);
		}
		template <typename U> explicit operator Vec3<Scalar, U>&()
		{
			return reinterpret_cast<Vec3<Scalar, U>&>(*this);
		}
		operator Vec3<Scalar, void> const& () const
		{
			return reinterpret_cast<Vec3<Scalar, void> const&>(*this);
		}
		operator Vec3<Scalar, void>& ()
		{
			return reinterpret_cast<Vec3<Scalar, void>&>(*this);
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

		// Create other vector types
		Vec4<Scalar, T> w0() const;
		Vec4<Scalar, T> w1() const;
		Vec2<Scalar, T> vec2(int i0, int i1) const
		{
			return Vec2<Scalar, T>(arr[i0], arr[i1]);
		}

		// Basic constants
		static constexpr Vec3 Zero()    { return Vec3(Scalar(0), Scalar(0), Scalar(0)); }
		static constexpr Vec3 XAxis()   { return Vec3(Scalar(1), Scalar(0), Scalar(0)); }
		static constexpr Vec3 YAxis()   { return Vec3(Scalar(0), Scalar(1), Scalar(0)); }
		static constexpr Vec3 ZAxis()   { return Vec3(Scalar(0), Scalar(0), Scalar(1)); }
		static constexpr Vec3 One()     { return Vec3(Scalar(1), Scalar(1), Scalar(1)); }
		static constexpr Vec3 TinyF()   { return Vec3(tiny<Scalar>, tiny<Scalar>, tiny<Scalar>); }
		static constexpr Vec3 Min()     { return Vec3(limits<Scalar>::min(), limits<Scalar>::min(), limits<Scalar>::min()); }
		static constexpr Vec3 Max()     { return Vec3(limits<Scalar>::max(), limits<Scalar>::max(), limits<Scalar>::max()); }
		static constexpr Vec3 Lowest()  { return Vec3(limits<Scalar>::lowest(), limits<Scalar>::lowest(), limits<Scalar>::lowest()); }
		static constexpr Vec3 Epsilon() { return Vec3(limits<Scalar>::epsilon(), limits<Scalar>::epsilon(), limits<Scalar>::epsilon()); }

		// Construct normalised
		static Vec3 Normal(Scalar x, Scalar y, Scalar z) requires std::is_floating_point_v<Scalar>
		{
			return Normalise(Vec3(x, y, z));
		}

		// Create a random vector with unit length
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec3 pr_vectorcall RandomN(Rng& rng)
		{
			std::uniform_real_distribution<Scalar> dist(Scalar(-1), Scalar(1));
			for (;;)
			{
				auto x = dist(rng);
				auto y = dist(rng);
				auto z = dist(rng);
				auto v = Vec3(x, y, z);
				auto len = LengthSq(v);
				if (len > Scalar(0.01) && len <= Scalar(1))
					return v / Sqrt(len);
			}
		}

		// Create a random vector with components on interval ['vmin', 'vmax']
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec3 pr_vectorcall Random(Rng& rng, Vec3_cref vmin, Vec3_cref vmax)
		{
			std::uniform_real_distribution<Scalar> dist_x(vmin.x, vmax.x);
			std::uniform_real_distribution<Scalar> dist_y(vmin.y, vmax.y);
			std::uniform_real_distribution<Scalar> dist_z(vmin.z, vmax.z);
			return Vec3(dist_x(rng), dist_y(rng), dist_z(rng));
		}

		// Create a random vector with length on interval [min_length, max_length]
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec3 pr_vectorcall Random(Rng& rng, Scalar min_length, Scalar max_length)
		{
			std::uniform_real_distribution<Scalar> dist(min_length, max_length);
			return dist(rng) * RandomN(rng);
		}

		// Create a random vector centred on 'centre' with radius 'radius'
		template <typename Rng = std::default_random_engine> requires std::is_floating_point_v<Scalar>
		static Vec3 pr_vectorcall Random(Rng& rng, Vec3_cref centre, Scalar radius)
		{
			return Random(rng, Scalar(0), radius) + centre;
		}

		#pragma region Operators
		friend constexpr Vec3 operator + (Vec3_cref vec)
		{
			return vec;
		}
		friend constexpr Vec3 operator - (Vec3_cref vec)
		{
			return Vec3(-vec.x, -vec.y, -vec.z);
		}
		friend Vec3 operator * (Scalar lhs, Vec3_cref rhs)
		{
			return rhs * lhs;
		}
		friend Vec3 operator * (Vec3_cref lhs, Scalar rhs)
		{
			return Vec3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
		}
		friend Vec3 operator / (Vec3_cref lhs, Scalar rhs)
		{
			// Don't check for divide by zero by default. For Scalars +inf/-inf are valid results
			return Vec3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
		}
		friend Vec3 operator % (Vec3_cref lhs, Scalar rhs)
		{
			// Don't check for divide by zero by default. For Scalars +inf/-inf are valid results
			if constexpr (std::floating_point<Scalar>)
				return Vec3(Fmod(lhs.x, rhs), Fmod(lhs.y, rhs), Fmod(lhs.z, rhs));
			else
				return Vec3(lhs.x % rhs, lhs.y % rhs, lhs.z % rhs);
		}
		friend Vec3 operator + (Vec3_cref lhs, Vec3_cref rhs)
		{
			return Vec3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
		}
		friend Vec3 operator - (Vec3_cref lhs, Vec3_cref rhs)
		{
			return Vec3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
		}
		friend Vec3 operator * (Vec3_cref lhs, Vec3_cref rhs)
		{
			return Vec3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
		}
		friend Vec3 operator / (Vec3_cref lhs, Vec3_cref rhs)
		{
			// Don't check for divide by zero by default. For Scalars +inf/-inf are valid results
			return Vec3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
		}
		friend Vec3 operator % (Vec3_cref lhs, Vec3_cref rhs)
		{
			// Don't check for divide by zero by default. For Scalars +inf/-inf are valid results
			if constexpr (std::floating_point<Scalar>)
				return Vec3(Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y), Fmod(lhs.z, rhs.z));
			else
				return Vec3(lhs.x % rhs.x, lhs.y % rhs.y, lhs.z % rhs.z);
		}
		#pragma endregion
	};
	#define PR_VEC3_CHECKS(scalar)\
	static_assert(sizeof(Vec3<scalar, void>) == 3 * sizeof(scalar), "Vector<"#scalar"> has the wrong size");\
	static_assert(maths::Vector3<Vec3<scalar, void>>, "Vector<"#scalar"> is not a Vector3");\
	static_assert(std::is_trivially_copyable_v<Vec3<scalar, void>>, "Must be a pod type");
	PR_VEC3_CHECKS(float);
	PR_VEC3_CHECKS(double);
	PR_VEC3_CHECKS(int32_t);
	PR_VEC3_CHECKS(int64_t);
	#undef PR_VEC3_CHECKS

	// Dot product: a . b
	template <typename Scalar, typename T> constexpr Scalar pr_vectorcall Dot(Vec3_cref<Scalar, T> lhs, Vec3_cref<Scalar, T> rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
	}

	// Cross product: a x b
	template <typename Scalar, typename T> constexpr Vec3<Scalar, T> pr_vectorcall Cross(Vec3_cref<Scalar, T> lhs, Vec3_cref<Scalar, T> rhs)
	{
		return Vec3<Scalar, T>{lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x};
	}

	// Triple product: a . b x c
	template <typename Scalar, typename T> constexpr Scalar pr_vectorcall Triple(Vec3_cref<Scalar, T> a, Vec3_cref<Scalar, T> b, Vec3_cref<Scalar, T> c)
	{
		return Dot(a, Cross3(b, c));
	}

	// Returns a vector with the values permuted 'n' times. 0=xyz, 1=yzx, 2=zxy, etc
	template <typename Scalar, typename T> constexpr Vec3<Scalar, T> pr_vectorcall Permute(Vec3_cref<Scalar, T> v, int n)
	{
		switch (n % 3)
		{
			case 1:  return Vec3<Scalar, T>{v.y, v.z, v.x};
			case 2:  return Vec3<Scalar, T>{v.z, v.x, v.y};
			default: return v;
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	template <typename Scalar, typename T> constexpr uint32_t Octant(Vec3_cref<Scalar, T> v)
	{
		return
			((v.x >= Scalar(0)) << 0) |
			((v.y >= Scalar(0)) << 1) |
			((v.z >= Scalar(0)) << 2);
	}

}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector3Tests, float, double, int32_t, int64_t)
	{
		using Scalar = T;
		using vec3_t = Vec3<Scalar, void>;

		{// Create
			auto V0 = vec3_t(Scalar(1));
			PR_CHECK(V0.x == Scalar(1), true);
			PR_CHECK(V0.y == Scalar(1), true);
			PR_CHECK(V0.z == Scalar(1), true);

			auto V1 = vec3_t(Scalar(1), Scalar(2), Scalar(3));
			PR_CHECK(V1.x == Scalar(1), true);
			PR_CHECK(V1.y == Scalar(2), true);

			auto V2 = vec3_t({Scalar(3), Scalar(4), Scalar(5)});
			PR_CHECK(V2.x == Scalar(3), true);
			PR_CHECK(V2.y == Scalar(4), true);
			PR_CHECK(V2.z == Scalar(5), true);

			vec3_t V3 = {Scalar(4), Scalar(5), Scalar(6)};
			PR_CHECK(V3[0] == Scalar(4), true);
			PR_CHECK(V3[1] == Scalar(5), true);
			PR_CHECK(V3[2] == Scalar(6), true);

			if constexpr (std::floating_point<Scalar>)
			{
				auto V4 = vec3_t::Normal(Scalar(3), Scalar(4), Scalar(5));
				auto V4_expected = vec3_t(Scalar(0.42426406871192), Scalar(0.56568542494923), Scalar(0.70710678118654));
				PR_CHECK(FEql(V4, V4_expected), true);
			}
		}
	}
}
#endif
