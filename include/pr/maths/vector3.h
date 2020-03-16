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
	struct Vec3
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x, y, z; };
			struct { Vec2<T> xy; };
			struct { float arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Vec3() = default;
		Vec3(float x_, float y_, float z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		explicit Vec3(float x_)
			:Vec3(x_, x_, x_)
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> Vec3(V3 const& v)
			:Vec3(x_as<float>(v), y_as<float>(v), z_as<float>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Vec3(V2 const& v, float z_)
			:Vec3(x_as<float>(v), y_as<float>(v), z_)
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit Vec3(CP const* v)
			:Vec3(x_as<float>(v), y_as<float>(v), z_as<float>(v))
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> Vec3& operator = (V3 const& rhs)
		{
			x = x_as<float>(rhs);
			y = y_as<float>(rhs);
			z = z_as<float>(rhs);
			return *this;
		}

		// Type conversion
		template <typename U> explicit operator Vec3<U>() const
		{
			return Vec3<U>{x, y, z};
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
		template <typename V4 = typename Vec4<void>, typename = maths::enable_if_v4<V4>> V4 w0() const
		{
			return V4(x, y, z, 0);
		}
		template <typename V4 = typename Vec4<void>, typename = maths::enable_if_v4<V4>> V4 w1() const
		{
			return V4(x, y, z, 1);
		}
		Vec2<T> vec2(int i0, int i1) const
		{
			return Vec2<T>(arr[i0], arr[i1]);
		}

		// Construct normalised
		static Vec3<T> Normal3(float x, float y, float z)
		{
			return Normalise3(Vec3<T>(x,y,z));
		}

		#pragma region Operators
		friend Vec3<T> operator + (v3_cref<T> vec)
		{
			return vec;
		}
		friend Vec3<T> operator - (v3_cref<T> vec)
		{
			return Vec3<T>(-vec.x, -vec.y, -vec.z);
		}
		friend Vec3<T> operator * (float lhs, v3_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec3<T> operator * (v3_cref<T> lhs, float rhs)
		{
			return Vec3<T>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend Vec3<T> operator / (v3_cref<T> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Vec3<T>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend Vec3<T> operator % (v3_cref<T> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Vec3<T>{Fmod(lhs.x, rhs), Fmod(lhs.y, rhs), Fmod(lhs.z, rhs)};
		}
		friend Vec3<T> operator + (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3<T>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Vec3<T> operator - (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3<T>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec3<T> operator * (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			return Vec3<T>{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z};
		}
		friend Vec3<T> operator / (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			assert("divide by zero" && !Any3(rhs, IsZero<float>));
			return Vec3<T>{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z};
		}
		friend Vec3<T> operator % (v3_cref<T> lhs, v3_cref<T> rhs)
		{
			assert("divide by zero" && !Any3(rhs, IsZero<float>));
			return Vec3<T>{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y), Fmod(lhs.z, rhs.z)};
		}
		#pragma endregion
	};
	static_assert(maths::is_vec3<Vec3<void>>::value, "");
	static_assert(std::is_pod<Vec3<void>>::value, "v3 must be a pod type");

	// Define component accessors for pointer types
	template <typename T> inline float x_cp(v3_cref<T> v) { return v.x; }
	template <typename T> inline float y_cp(v3_cref<T> v) { return v.y; }
	template <typename T> inline float z_cp(v3_cref<T> v) { return v.z; }
	template <typename T> inline float w_cp(v3_cref<T>)   { return 0; }

	#pragma region Functions

	// Dot product: a . b
	template <typename T> inline float Dot(v3_cref<T> a, v3_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	// Cross product: a x b
	template <typename T> inline Vec3<T> Cross(v3_cref<T> a, v3_cref<T> b)
	{
		return Vec3<T>{a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x};
	}

	// Triple product: a . b x c
	template <typename T> inline float Triple(v3_cref<T> a, v3_cref<T> b, v3_cref<T> c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Returns a vector with the values permuted 'n' times. 0=xyz, 1=yzx, 2=zxy, etc
	template <typename T> inline Vec3<T> Permute(v3_cref<T> v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return Vec3<T>{v.y, v.z, v.x};
		case 2:  return Vec3<T>{v.z, v.x, v.y};
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	template <typename T> inline uint Octant(v3_cref<T> v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1) | ((v.z >= 0.0f) << 2);
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector3Tests)
	{
	}
}
#endif
