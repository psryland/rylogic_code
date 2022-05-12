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
	struct Vec2i
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { int x, y; };
			struct { int arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Vec2i() = default;
		constexpr explicit Vec2i(int x_)
			:x(x_)
			,y(x_)
		{}
		constexpr Vec2i(int x_, int y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Vec2i(int const* v)
			:Vec2i(v[0], v[1])
		{}
		template <maths::Vector2 V> constexpr explicit Vec2i(V const& v)
			:Vec2i(maths::comp<0>(v), maths::comp<1>(v))
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
		static constexpr Vec2i Zero()   { return Vec2i{0,0}; }
		static constexpr Vec2i XAxis()  { return Vec2i{1,0}; }
		static constexpr Vec2i YAxis()  { return Vec2i{0,1}; }

		#pragma region Operators
		friend constexpr Vec2i operator + (v2i_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec2i operator - (v2i_cref<T> vec)
		{
			return Vec2i{-vec.x, -vec.y};
		}
		friend Vec2i operator * (int lhs, v2i_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec2i operator * (v2i_cref<T> lhs, int rhs)
		{
			return Vec2i{lhs.x * rhs, lhs.y * rhs};
		}
		friend Vec2i operator / (v2i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec2i{lhs.x / rhs, lhs.y / rhs};
		}
		friend Vec2i operator % (v2i_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec2i{lhs.x % rhs, lhs.y % rhs};
		}
		friend Vec2i operator + (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend Vec2i operator - (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend Vec2i operator * (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x * rhs.x, lhs.y * rhs.y};
		}
		friend Vec2i operator / (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2i{lhs.x / rhs.x, lhs.y / rhs.y};
		}
		friend Vec2i operator % (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2i{lhs.x % rhs.x, lhs.y % rhs.y};
		}
		friend Vec2i pr_vectorcall operator ~  (v2i_cref<T> rhs)
		{
			return Vec2i{~rhs.x, ~rhs.y};
		}
		friend Vec2i pr_vectorcall operator !  (v2i_cref<T> rhs)
		{
			return Vec2i{!rhs.x, !rhs.y};
		}
		friend Vec2i pr_vectorcall operator |  (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x | rhs.x, lhs.y | rhs.y};
		}
		friend Vec2i pr_vectorcall operator &  (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x & rhs.x, lhs.y & rhs.y};
		}
		friend Vec2i pr_vectorcall operator ^  (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x ^ rhs.x, lhs.y ^ rhs.y};
		}
		friend Vec2i pr_vectorcall operator << (v2i_cref<T> lhs, int rhs)
		{
			return Vec2i{lhs.x << rhs, lhs.y << rhs};
		}
		friend Vec2i pr_vectorcall operator << (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x << rhs.x, lhs.y << rhs.y};
		}
		friend Vec2i pr_vectorcall operator >> (v2i_cref<T> lhs, int rhs)
		{
			return Vec2i{lhs.x >> rhs, lhs.y >> rhs};
		}
		friend Vec2i pr_vectorcall operator >> (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x >> rhs.x, lhs.y >> rhs.y};
		}
		friend Vec2i pr_vectorcall operator || (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x || rhs.x, lhs.y || rhs.y};
		}
		friend Vec2i pr_vectorcall operator && (v2i_cref<T> lhs, v2i_cref<T> rhs)
		{
			return Vec2i{lhs.x && rhs.x, lhs.y && rhs.y};
		}
		#pragma endregion
	};
	static_assert(sizeof(Vec2i<void>) == 8);
	static_assert(maths::Vector2<Vec2i<void>>);
	static_assert(std::is_trivially_copyable_v<Vec2i<void>>, "iv2 must be a pod type");

	// Dot product: a . b
	template <typename T> inline int Dot(v2i_cref<T> a, v2i_cref<T> b)
	{
		return a.x * b.x + a.y * b.y;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(IVector2Tests)
	{
	}
}
#endif
