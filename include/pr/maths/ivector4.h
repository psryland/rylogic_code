//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/ivector2.h"

namespace pr
{
	template <typename T>
	struct alignas(16) IVec4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { int x, y, z, w; };
			struct { IVec2<T> xy, zw; };
			struct { int arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128i vec;
			#endif
		};
		#pragma warning(pop)

		// Construct
		IVec4() = default;
		IVec4(int x_, int y_, int z_, int w_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_epi32(w_,z_,y_,x_))
		#else
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		explicit IVec4(int x_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set1_epi32(x_))
		#else
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		template <typename V4, typename = maths::enable_if_v4<V4>> IVec4(V4 const& v)
			:IVec4(x_as<int>(v), y_as<int>(v), z_as<int>(v), w_as<int>(v))
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> IVec4(V3 const& v, int w_)
			:IVec4(x_as<int>(v), y_as<int>(v), z_as<int>(v), w_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> IVec4(V2 const& v, int z_, int w_)
			:IVec4(x_as<int>(v), y_as<int>(v), z_, w_)
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit IVec4(CP const* v)
			:IVec4(x_as<int>(v), y_as<int>(v), z_as<int>(v), w_as<int>(v))
		{}
		template <typename V4, typename = maths::enable_if_v4<V4>> IVec4& operator = (V4 const& rhs)
		{
			x = x_as<int>(rhs);
			y = y_as<int>(rhs);
			z = z_as<int>(rhs);
			w = w_as<int>(rhs);
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

		// Create other vector types
		IVec4 w0() const
		{
			IVec4 r(x,y,z,0); // LValue because of alignment
			return r;
		}
		IVec4 w1() const
		{
			IVec4 r(x,y,z,1); // LValue because of alignment
			return r;
		}
		IVec2<T> vec2(int i0, int i1) const
		{
			return IVec2<T>{arr[i0], arr[i1]};
		}
		
		// Explicit cast to v4
		explicit operator Vec4<T>() const
		{
			return Vec4<T>(x, y, z, w);
		}

		#pragma region Operators
		friend IVec4<T> pr_vectorcall operator + (iv4_cref<T> vec)
		{
			return vec;
		}
		friend IVec4<T> pr_vectorcall operator - (iv4_cref<T> vec)
		{
			return IVec4<T>{-vec.x, -vec.y, -vec.z, -vec.w};
		}
		friend IVec4<T> pr_vectorcall operator * (int lhs, iv4_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend IVec4<T> pr_vectorcall operator * (iv4_cref<T> lhs, int rhs)
		{
			return IVec4<T>{lhs.x + rhs, lhs.y + rhs, lhs.z + rhs, lhs.w + rhs};
		}
		friend IVec4<T> pr_vectorcall operator / (iv4_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec4<T>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
		}
		friend IVec4<T> pr_vectorcall operator % (iv4_cref<T> lhs, int rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return IVec4<T>{lhs.x % rhs, lhs.y % rhs, lhs.z % rhs, lhs.w % rhs};
		}
		friend IVec4<T> pr_vectorcall operator + (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator - (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator * (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator / (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec4<T>{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator % (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return IVec4<T>{lhs.x % rhs.x, lhs.y % rhs.y, lhs.z % rhs.z, lhs.w % rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator ~ (iv4_cref<T> rhs)
		{
			return IVec4<T>{~rhs.x, ~rhs.y, ~rhs.z, ~rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator ! (iv4_cref<T> rhs)
		{
			return IVec4<T>{!rhs.x, !rhs.y, !rhs.z, !rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator | (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x | rhs.x, lhs.y | rhs.y, lhs.z | rhs.z, lhs.w | rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator & (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x & rhs.x, lhs.y & rhs.y, lhs.z & rhs.z, lhs.w & rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator ^ (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x ^ rhs.x, lhs.y ^ rhs.y, lhs.z ^ rhs.z, lhs.w ^ rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator << (iv4_cref<T> lhs, int rhs)
		{
			return IVec4<T>{lhs.x << rhs, lhs.y << rhs, lhs.z << rhs, lhs.w << rhs};
		}
		friend IVec4<T> pr_vectorcall operator << (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x << rhs.x, lhs.y << rhs.y, lhs.z << rhs.z, lhs.w << rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator >> (iv4_cref<T> lhs, int rhs)
		{
			return IVec4<T>{lhs.x >> rhs, lhs.y >> rhs, lhs.z >> rhs, lhs.w >> rhs};
		}
		friend IVec4<T> pr_vectorcall operator >> (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x >> rhs.x, lhs.y >> rhs.y, lhs.z >> rhs.z, lhs.w >> rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator || (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x || rhs.x, lhs.y || rhs.y, lhs.z || rhs.z, lhs.w || rhs.w};
		}
		friend IVec4<T> pr_vectorcall operator && (iv4_cref<T> lhs, iv4_cref<T> rhs)
		{
			return IVec4<T>{lhs.x && rhs.x, lhs.y && rhs.y, lhs.z && rhs.z, lhs.w && rhs.w};
		}
		#pragma endregion

		// Component accessors
		friend constexpr int pr_vectorcall x_cp(iv4_cref<T> v) { return v.x; }
		friend constexpr int pr_vectorcall y_cp(iv4_cref<T> v) { return v.y; }
		friend constexpr int pr_vectorcall z_cp(iv4_cref<T> v) { return v.z; }
		friend constexpr int pr_vectorcall w_cp(iv4_cref<T> v) { return v.w; }
	};
	static_assert(maths::is_vec4<IVec4<void>>::value, "");
	static_assert(std::is_pod_v<IVec4<void>>, "iv4 must be a pod type");

	#pragma region Functions
	
	// Dot product: a . b
	template <typename T> inline int pr_vectorcall Dot3(iv4_cref<T> a, iv4_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
	template <typename T> inline int pr_vectorcall Dot4(iv4_cref<T> a, iv4_cref<T> b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
	}
	template <typename T> inline int pr_vectorcall Dot(iv4_cref<T> a, iv4_cref<T> b)
	{
		return Dot4(a,b);
	}

	// Cross product: a x b
	template <typename T> inline IVec4<T> pr_vectorcall Cross3(iv4_cref<T> a, iv4_cref<T> b)
	{
		return IVec4<T>{a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x, 0};
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(IVector4Tests)
	{
	}
}
#endif