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
	struct IVec3
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
		IVec3() = default;
		constexpr IVec3(int x_, int y_, int z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		constexpr explicit IVec3(int x_)
			:IVec3(x_, x_, x_)
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> IVec3(V3 const& v)
			:IVec3(x_as<int>(v), y_as<int>(v), z_as<int>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit IVec3(CP const* v)
			:IVec3(x_as<int>(v), y_as<int>(v), z_as<int>(v))
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> IVec3& operator = (V3 const& rhs)
		{
			x = x_as<int>(rhs);
			y = y_as<int>(rhs);
			z = z_as<int>(rhs);
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
		static constexpr IVec3 Zero()   { return IVec3{0,0,0}; }
		static constexpr IVec3 XAxis()  { return IVec3{1,0,0}; }
		static constexpr IVec3 YAxis()  { return IVec3{0,1,0}; }
		static constexpr IVec3 ZAxis()  { return IVec3{0,0,1}; }

		#pragma region Operators
		friend constexpr IVec3<T> operator + (iv3_cref<T> vec)
		{
			return vec;
		}
		friend constexpr IVec3<T> operator - (iv3_cref<T> vec)
		{
			return IVec3<T>{-vec.x, -vec.y, -vec.z};
		}
		friend IVec3<T> operator * (int lhs, iv3_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend IVec3<T> operator * (iv3_cref<T> lhs, int rhs)
		{
			return IVec3<T>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs};
		}
		friend IVec3<T> operator / (iv3_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec3<T>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs};
		}
		friend IVec3<T> operator % (iv3_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec3<T>{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs};
		}
		friend IVec3<T> operator + (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z};
		}
		friend IVec3<T> operator - (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z};
		}
		friend IVec3<T> operator * (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z};
		}
		friend IVec3<T> operator / (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec3<T>{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z};
		}
		friend IVec3<T> operator % (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec3<T>{lhs.x % rhs.x, lhs.y % rhs.y, lhs.z % rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator ~  (iv3_cref<T> rhs)
		{
			return IVec3<T>{~rhs.x, ~rhs.y, ~rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator !  (iv3_cref<T> rhs)
		{
			return IVec3<T>{!rhs.x, !rhs.y, !rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator |  (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x | rhs.x, lhs.y | rhs.y, lhs.z | rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator &  (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x & rhs.x, lhs.y & rhs.y, lhs.z & rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator ^  (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x ^ rhs.x, lhs.y ^ rhs.y, lhs.z ^ rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator << (iv3_cref<T> lhs, int rhs)
		{
			return IVec3<T>{lhs.x << rhs, lhs.y << rhs, lhs.z << rhs};
		}
		friend IVec3<T> pr_vectorcall operator << (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x << rhs.x, lhs.y << rhs.y, lhs.z << rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator >> (iv3_cref<T> lhs, int rhs)
		{
			return IVec3<T>{lhs.x >> rhs, lhs.y >> rhs, lhs.z >> rhs};
		}
		friend IVec3<T> pr_vectorcall operator >> (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x >> rhs.x, lhs.y >> rhs.y, lhs.z >> rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator || (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x || rhs.x, lhs.y || rhs.y, lhs.z || rhs.z};
		}
		friend IVec3<T> pr_vectorcall operator && (iv3_cref<T> lhs, iv3_cref<T> rhs)
		{
			return IVec3<T>{lhs.x && rhs.x, lhs.y && rhs.y, lhs.z && rhs.z};
		}
		#pragma endregion

		// Define component accessors for pointer types
		friend constexpr int x_cp(iv3_cref<T> v) { return v.x; }
		friend constexpr int y_cp(iv3_cref<T> v) { return v.y; }
		friend constexpr int z_cp(iv3_cref<T> v) { return v.z; }
		friend constexpr int w_cp(iv3_cref<T>)   { return 0; }
	};
	static_assert(maths::is_vec3<IVec3<void>>::value, "");
	static_assert(std::is_trivially_copyable_v<IVec3<void>>, "iv3 must be a pod type");

	#pragma region Functions

	// Dot product: a . b
	template <typename T> inline int Dot(iv3_cref<T> a, iv3_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}

	#pragma endregion
}
