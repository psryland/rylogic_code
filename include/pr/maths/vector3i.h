//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2i.h"

namespace pr
{
	template <typename T>
	struct Vec3i
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { int x, y, z; };
			struct { iv2 xy; };
			struct { int arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Vec3i() = default;
		constexpr explicit Vec3i(int x_)
			:x(x_)
			,y(x_)
			,z(x_)
		{}
		constexpr Vec3i(int x_, int y_, int z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		constexpr explicit Vec3i(int const* v)
			:Vec3i(v[0], v[1], v[2])
		{}
		template <maths::Vector3 V> constexpr explicit Vec3i(V const& v)
			:Vec3i(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v))
		{}

		// Array access
		int const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		int& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Basic constants
		static constexpr Vec3i Zero()   { return Vec3i{0,0,0}; }
		static constexpr Vec3i XAxis()  { return Vec3i{1,0,0}; }
		static constexpr Vec3i YAxis()  { return Vec3i{0,1,0}; }
		static constexpr Vec3i ZAxis()  { return Vec3i{0,0,1}; }

		#pragma region Operators
		friend constexpr Vec3i operator + (v3i_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec3i operator - (v3i_cref<T> vec)
		{
			return Vec3i{-vec.x, -vec.y, -vec.z};
		}
		friend Vec3i operator * (int lhs, v3i_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec3i operator * (v3i_cref<T> lhs, int rhs)
		{
			return Vec3i{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend Vec3i operator / (v3i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec3i{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend Vec3i operator % (v3i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec3i{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs};
		}
		friend Vec3i operator + (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend Vec3i operator - (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend Vec3i operator * (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z};
		}
		friend Vec3i operator / (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec3i{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z};
		}
		friend Vec3i operator % (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec3i{lhs.x % rhs.x, lhs.y % rhs.y, lhs.z % rhs.z};
		}
		friend Vec3i pr_vectorcall operator ~  (v3i_cref<T> rhs)
		{
			return Vec3i{~rhs.x, ~rhs.y, ~rhs.z};
		}
		friend Vec3i pr_vectorcall operator !  (v3i_cref<T> rhs)
		{
			return Vec3i{!rhs.x, !rhs.y, !rhs.z};
		}
		friend Vec3i pr_vectorcall operator |  (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x | rhs.x, lhs.y | rhs.y, lhs.z | rhs.z};
		}
		friend Vec3i pr_vectorcall operator &  (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x & rhs.x, lhs.y & rhs.y, lhs.z & rhs.z};
		}
		friend Vec3i pr_vectorcall operator ^  (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x ^ rhs.x, lhs.y ^ rhs.y, lhs.z ^ rhs.z};
		}
		friend Vec3i pr_vectorcall operator << (v3i_cref<T> lhs, int rhs)
		{
			return Vec3i{lhs.x << rhs, lhs.y << rhs, lhs.z << rhs};
		}
		friend Vec3i pr_vectorcall operator << (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x << rhs.x, lhs.y << rhs.y, lhs.z << rhs.z};
		}
		friend Vec3i pr_vectorcall operator >> (v3i_cref<T> lhs, int rhs)
		{
			return Vec3i{lhs.x >> rhs, lhs.y >> rhs, lhs.z >> rhs};
		}
		friend Vec3i pr_vectorcall operator >> (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x >> rhs.x, lhs.y >> rhs.y, lhs.z >> rhs.z};
		}
		friend Vec3i pr_vectorcall operator || (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x || rhs.x, lhs.y || rhs.y, lhs.z || rhs.z};
		}
		friend Vec3i pr_vectorcall operator && (v3i_cref<T> lhs, v3i_cref<T> rhs)
		{
			return Vec3i{lhs.x && rhs.x, lhs.y && rhs.y, lhs.z && rhs.z};
		}
		#pragma endregion
	};
	static_assert(sizeof(Vec3i<void>) == 12);
	static_assert(maths::Vector3<Vec3i<void>>);
	static_assert(std::is_trivially_copyable_v<Vec3i<void>>, "iv3 must be a pod type");

	// Dot product: a . b
	template <typename T> inline int Dot(v3i_cref<T> a, v3i_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector3iTests)
	{
	}
}
#endif
