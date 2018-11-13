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
		IVec2(int x_, int y_)
			:x(x_)
			,y(y_)
		{}
		explicit IVec2(int x_)
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
	};
	static_assert(maths::is_vec2<IVec2<void>>::value, "");
	static_assert(std::is_pod<IVec2<void>>::value, "iv2 must be a pod type");
	template <typename T = void> using iv2_cref = IVec2<T> const&;

	// Define component accessors for pointer types
	template <typename T> inline int x_cp(iv2_cref<T> v) { return v.x; }
	template <typename T> inline int y_cp(iv2_cref<T> v) { return v.y; }
	template <typename T> inline int z_cp(iv2_cref<T>)   { return 0; }
	template <typename T> inline int w_cp(iv2_cref<T>)   { return 0; }

	#pragma region Operators
	template <typename T> inline IVec2<T> operator + (iv2_cref<T> vec)
	{
		return vec;
	}
	template <typename T> inline IVec2<T> operator - (iv2_cref<T> vec)
	{
		return IVec2<T>{-vec.x, -vec.y};
	}
	template <typename T> inline IVec2<T> operator * (int lhs, iv2_cref<T> rhs)
	{
		return rhs * lhs;
	}
	template <typename T> inline IVec2<T> operator * (iv2_cref<T> lhs, int rhs)
	{
		return IVec2<T>{lhs.x * rhs, lhs.y * rhs};
	}
	template <typename T> inline IVec2<T> operator / (iv2_cref<T> lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		return IVec2<T>{lhs.x / rhs, lhs.y / rhs};
	}
	template <typename T> inline IVec2<T> operator % (iv2_cref<T> lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		return IVec2<T>{lhs.x % rhs, lhs.y % rhs};
	}
	template <typename T> inline IVec2<T> operator + (iv2_cref<T> lhs, iv2_cref<T> rhs)
	{
		return IVec2<T>{lhs.x + rhs.x, lhs.y + rhs.y};
	}
	template <typename T> inline IVec2<T> operator - (iv2_cref<T> lhs, iv2_cref<T> rhs)
	{
		return IVec2<T>{lhs.x - rhs.x, lhs.y - rhs.y};
	}
	template <typename T> inline IVec2<T> operator * (iv2_cref<T> lhs, iv2_cref<T> rhs)
	{
		return IVec2<T>{lhs.x * rhs.x, lhs.y * rhs.y};
	}
	template <typename T> inline IVec2<T> operator / (iv2_cref<T> lhs, iv2_cref<T> rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<int>));
		return IVec2<T>{lhs.x / rhs.x, lhs.y / rhs.y};
	}
	template <typename T> inline IVec2<T> operator % (iv2_cref<T> lhs, iv2_cref<T> rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<int>));
		return IVec2<T>{lhs.x % rhs.x, lhs.y % rhs.y};
	}
	#pragma endregion

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