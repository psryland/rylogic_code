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
	struct IVec2
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
		IVec2() = default;
		constexpr IVec2(int x_, int y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit IVec2(int x_)
			:IVec2(x_, x_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> IVec2(V2 const& v)
			:IVec2(x_as<int>(v), y_as<int>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit IVec2(CP const* v)
			:IVec2(x_as<int>(v), y_as<int>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> IVec2& operator = (V2 const& rhs)
		{
			x = x_as<int>(rhs);
			y = y_as<int>(rhs);
			return *this;
		}

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
		static constexpr IVec2 Zero()   { return IVec2{0,0}; }
		static constexpr IVec2 XAxis()  { return IVec2{1,0}; }
		static constexpr IVec2 YAxis()  { return IVec2{0,1}; }

		#pragma region Operators
		friend constexpr IVec2<T> operator + (iv2_cref<T> vec)
		{
			return vec;
		}
		friend constexpr IVec2<T> operator - (iv2_cref<T> vec)
		{
			return IVec2<T>{-vec.x, -vec.y};
		}
		friend IVec2<T> operator * (int lhs, iv2_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend IVec2<T> operator * (iv2_cref<T> lhs, int rhs)
		{
			return IVec2<T>{lhs.x * rhs, lhs.y * rhs};
		}
		friend IVec2<T> operator / (iv2_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec2<T>{lhs.x / rhs, lhs.y / rhs};
		}
		friend IVec2<T> operator % (iv2_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec2<T>{lhs.x % rhs, lhs.y % rhs};
		}
		friend IVec2<T> operator + (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec2<T>{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend IVec2<T> operator - (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec2<T>{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend IVec2<T> operator * (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec2<T>{lhs.x * rhs.x, lhs.y * rhs.y};
		}
		friend IVec2<T> operator / (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec2<T>{lhs.x / rhs.x, lhs.y / rhs.y};
		}
		friend IVec2<T> operator % (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec2<T>{lhs.x % rhs.x, lhs.y % rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator ~  (iv2_cref<T> rhs)
		{
			return IVec2<T>{~rhs.x, ~rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator !  (iv2_cref<T> rhs)
		{
			return IVec2<T>{!rhs.x, !rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator |  (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec2<T>{lhs.x | rhs.x, lhs.y | rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator &  (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x & rhs.x, lhs.y & rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator ^  (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x ^ rhs.x, lhs.y ^ rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator << (iv2_cref<T> lhs, int rhs)
		{
			return IVec4<T>{lhs.x << rhs, lhs.y << rhs};
		}
		friend IVec2<T> pr_vectorcall operator << (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x << rhs.x, lhs.y << rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator >> (iv2_cref<T> lhs, int rhs)
		{
			return IVec4<T>{lhs.x >> rhs, lhs.y >> rhs};
		}
		friend IVec2<T> pr_vectorcall operator >> (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x >> rhs.x, lhs.y >> rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator || (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x || rhs.x, lhs.y || rhs.y};
		}
		friend IVec2<T> pr_vectorcall operator && (iv2_cref<T> lhs, iv2_cref<T> rhs)
		{
			return IVec4<T>{lhs.x && rhs.x, lhs.y && rhs.y};
		}
		#pragma endregion

		// Define component accessors for pointer types
		friend constexpr int x_cp(iv2_cref<T> v) { return v.x; }
		friend constexpr int y_cp(iv2_cref<T> v) { return v.y; }
		friend constexpr int z_cp(iv2_cref<T>)   { return 0; }
		friend constexpr int w_cp(iv2_cref<T>)   { return 0; }
	};
	static_assert(maths::is_vec2<IVec2<void>>::value, "");
	static_assert(std::is_trivially_copyable_v<IVec2<void>>, "iv2 must be a pod type");

	#pragma region Functions

	// Dot product: a . b
	template <typename T> inline int Dot(iv2_cref<T> a, iv2_cref<T> b)
	{
		return a.x * b.x + a.y * b.y;
	}

	#pragma endregion
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