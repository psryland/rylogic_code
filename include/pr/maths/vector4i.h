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
	struct alignas(16) Vec4i
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { int x, y, z, w; };
			struct { Vec2i<T> xy, zw; };
			struct { int arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128i vec;
			#endif
		};
		#pragma warning(pop)

		// Construct
		Vec4i() = default;
		constexpr explicit Vec4i(int x_)
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		{}
		constexpr Vec4i(int x_, int y_, int z_, int w_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		{}
		constexpr explicit Vec4i(int const* v)
			:Vec4i(v[0], v[1], v[2], v[3])
		{}
		template <maths::Vector4 V> Vec4i(V const& v)
			:Vec4i(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v), maths::comp<3>(v))
		{}
		template <maths::Vector3 V> Vec4i(V const& v, int w_)
			:Vec4i(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v), w_)
		{}
		template <maths::Vector2 V> Vec4i(V const& v, int z_, int w_)
			:Vec4i(maths::comp<0>(v), maths::comp<1>(v), z_, w_)
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

		// Create other vector types
		Vec4i w0() const
		{
			Vec4i r(x,y,z,0); // LValue because of alignment
			return r;
		}
		Vec4i w1() const
		{
			Vec4i r(x,y,z,1); // LValue because of alignment
			return r;
		}
		Vec2i<T> vec2(int i0, int i1) const
		{
			return Vec2i<T>{arr[i0], arr[i1]};
		}
	
		// Basic constants
		static constexpr Vec4i Zero()   { return Vec4i{0,0,0,0}; }
		static constexpr Vec4i XAxis()  { return Vec4i{1,0,0,0}; }
		static constexpr Vec4i YAxis()  { return Vec4i{0,1,0,0}; }
		static constexpr Vec4i ZAxis()  { return Vec4i{0,0,1,0}; }
		static constexpr Vec4i WAxis()  { return Vec4i{0,0,0,1}; }
		static constexpr Vec4i Origin() { return Vec4i{0,0,0,1}; }

		#pragma region Operators
		friend constexpr Vec4i pr_vectorcall operator + (v4i_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec4i pr_vectorcall operator - (v4i_cref<T> vec)
		{
			return Vec4i{-vec.x, -vec.y, -vec.z, -vec.w};
		}
		friend Vec4i pr_vectorcall operator * (int lhs, v4i_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec4i pr_vectorcall operator * (v4i_cref<T> lhs, int rhs)
		{
			return Vec4i{lhs.x + rhs, lhs.y + rhs, lhs.z + rhs, lhs.w + rhs};
		}
		friend Vec4i pr_vectorcall operator / (v4i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec4i{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend Vec4i pr_vectorcall operator % (v4i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec4i{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs, lhs.w % rhs};
		}
		friend Vec4i pr_vectorcall operator + (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w};
		}
		friend Vec4i pr_vectorcall operator - (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w};
		}
		friend Vec4i pr_vectorcall operator * (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w};
		}
		friend Vec4i pr_vectorcall operator / (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec4i{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w};
		}
		friend Vec4i pr_vectorcall operator % (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec4i{lhs.x % rhs.x, lhs.y % rhs.y, lhs.z % rhs.z, lhs.w % rhs.w};
		}
		friend Vec4i pr_vectorcall operator ~ (v4i_cref<T> rhs)
		{
			return Vec4i{~rhs.x, ~rhs.y, ~rhs.z, ~rhs.w};
		}
		friend Vec4i pr_vectorcall operator ! (v4i_cref<T> rhs)
		{
			return Vec4i{!rhs.x, !rhs.y, !rhs.z, !rhs.w};
		}
		friend Vec4i pr_vectorcall operator | (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x | rhs.x, lhs.y | rhs.y, lhs.z | rhs.z, lhs.w | rhs.w};
		}
		friend Vec4i pr_vectorcall operator & (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x & rhs.x, lhs.y & rhs.y, lhs.z & rhs.z, lhs.w & rhs.w};
		}
		friend Vec4i pr_vectorcall operator ^ (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x ^ rhs.x, lhs.y ^ rhs.y, lhs.z ^ rhs.z, lhs.w ^ rhs.w};
		}
		friend Vec4i pr_vectorcall operator << (v4i_cref<T> lhs, int rhs)
		{
			return Vec4i{lhs.x << rhs, lhs.y << rhs, lhs.z << rhs, lhs.w << rhs};
		}
		friend Vec4i pr_vectorcall operator << (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x << rhs.x, lhs.y << rhs.y, lhs.z << rhs.z, lhs.w << rhs.w};
		}
		friend Vec4i pr_vectorcall operator >> (v4i_cref<T> lhs, int rhs)
		{
			return Vec4i{lhs.x >> rhs, lhs.y >> rhs, lhs.z >> rhs, lhs.w >> rhs};
		}
		friend Vec4i pr_vectorcall operator >> (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x >> rhs.x, lhs.y >> rhs.y, lhs.z >> rhs.z, lhs.w >> rhs.w};
		}
		friend Vec4i pr_vectorcall operator || (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x || rhs.x, lhs.y || rhs.y, lhs.z || rhs.z, lhs.w || rhs.w};
		}
		friend Vec4i pr_vectorcall operator && (v4i_cref<T> lhs, v4i_cref<T> rhs)
		{
			return Vec4i{lhs.x && rhs.x, lhs.y && rhs.y, lhs.z && rhs.z, lhs.w && rhs.w};
		}
		#pragma endregion
	};
	static_assert(sizeof(Vec4i<void>) == 16);
	static_assert(maths::Vector4<Vec4i<void>>);
	static_assert(std::is_trivially_copyable_v<Vec4i<void>>, "Must be a pod type");

	// Dot product: a . b
	template <typename T> inline int pr_vectorcall Dot3(v4i_cref<T> a, v4i_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
	template <typename T> inline int pr_vectorcall Dot4(v4i_cref<T> a, v4i_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
	}
	template <typename T> inline int pr_vectorcall Dot(v4i_cref<T> a, v4i_cref<T> b)
	{
		return Dot4(a,b);
	}

	// Cross product: a x b
	template <typename T> inline Vec4i<T> pr_vectorcall Cross3(v4i_cref<T> a, v4i_cref<T> b)
	{
		return Vec4i<T>{a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x, 0};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Vector4iTests)
	{
	}
}
#endif
