//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	template <typename T>
	struct alignas(16) Vec8
	{
		// Notes:
		//  - Spatial vectors describe a vector at a point plus the field of vectors around that point. 
		//  - Don't define component accessors because this is not a normal coordinate vector

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<void> ang, lin; };
			struct { float arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		constexpr Vec8(v3_cref<> ang_, v3_cref<> lin_)
			:ang(ang_, 0)
			,lin(lin_, 0)
		{}
		constexpr Vec8(v4_cref<> ang_, v4_cref<> lin_)
			:ang(ang_)
			,lin(lin_)
		{}
		constexpr Vec8(float wx, float wy, float wz, float vx, float vy, float vz)
			:ang(wx, wy, wz, 0)
			,lin(vx, vy, vz, 0)
		{}
		constexpr Vec8(float wx, float wy, float wz, float ww, float vx, float vy, float vz, float vw)
			:ang(wx, wy, wz, ww)
			,lin(vx, vy, vz, vw)
		{}

		// Type conversion
		template <typename U> explicit operator Vec8<U>() const
		{
			return reinterpret_cast<Vec8<U> const&>(*this);
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

		// Basic constants
		static constexpr Vec8 Zero() { return Vec8{{0, 0, 0, 0}, {0, 0, 0, 0}}; }

		// Sample the vector field at 'ofs'
		// Returns the direction and magnitude of the vector field at 'ofs'
		v4 LinAt(v4_cref<> ofs) const
		{
			return v4{lin + Cross(ang, ofs)};
		}

		// Sample the vector field at 'ofs'
		// Not really sure what the physical interpretation of this is.
		// Returns 'ang - ofs x lin', which is the angular required at 'ofs' to ensure that the angular
		// is constant over the whole vector field, given that 'ofs x lin' contributes to the angular.
		v4 AngAt(v4_cref<> ofs) const
		{
			return v4{ang - Cross(ofs, lin)};
		}

		#pragma region Operators
		friend constexpr Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs)
		{
			return lhs;
		}
		friend constexpr Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs)
		{
			return Vec8<T>{-lhs.ang, -lhs.lin};
		}
		friend Vec8<T> pr_vectorcall operator * (float lhs, v8_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, float rhs)
		{
			return Vec8<T>{lhs.ang * rhs, lhs.lin * rhs};
		}
		friend Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, float rhs)
		{
			return Vec8<T>{lhs.ang / rhs, lhs.lin / rhs};
		}
		friend Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, float rhs)
		{
			return Vec8<T>{lhs.ang % rhs, lhs.lin % rhs};
		}
		friend Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs, v8_cref<T> rhs)
		{
			return Vec8<T>{lhs.ang + rhs.ang, lhs.lin + rhs.lin};
		}
		friend Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs, v8_cref<T> rhs)
		{
			return Vec8<T>{lhs.ang - rhs.ang, lhs.lin - rhs.lin};
		}
		friend Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, v8_cref<T> rhs)
		{
			return Vec8<T>{lhs.ang * rhs.ang, lhs.lin * rhs.lin};
		}
		friend Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, v8_cref<T> rhs)
		{
			return Vec8<T>{lhs.ang / rhs.ang, lhs.lin / rhs.lin};
		}
		friend Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, v8_cref<T> rhs)
		{
			return Vec8<T>{lhs.ang % rhs.ang, lhs.lin % rhs.lin};
		}
		#pragma endregion
	};
	static_assert(maths::is_vec<Vec8<void>>::value, "");
	static_assert(std::is_trivially_copyable_v<Vec8<void>>, "v8 must be a pod type");
	static_assert(std::alignment_of_v<Vec8<void>> == 16, "v8 should have 16 byte alignment");

	#pragma region Functions

	// Compare for floating point equality
	//template <typename T> inline bool FEql(v8_cref<T> lhs, v8_cref<T> rhs)
	template <typename T> inline bool FEql(Vec8<T> lhs, Vec8<T> rhs)
	{
		return
			FEql(lhs.ang, rhs.ang) &&
			FEql(lhs.lin, rhs.lin);
	}

	// Project a vector onto an axis. Loosely "dot(vec,axis)*axis"
	template <typename T> inline Vec8<T> Proj(Vec8<T> const& vec, v4_cref<> axis)
	{
		return Vec8<T>{
			Dot(vec.ang, axis) * axis,
			Dot(vec.lin, axis) * axis};
	}

	// Reflect a vector. Reverses the components of 'vec' in the direction of 'normal'
	template <typename T> inline Vec8<T> Reflect(Vec8<T> const& vec, v4_cref<> normal)
	{
		return vec - 2.0f * Proj(vec, normal);
	}

	#pragma endregion

	// Proxy object for Vec8
	#if 0
	struct Vec8ProxyC
	{
		Vec4<void> const& ang;
		Vec4<void> const& lin;

		Vec8ProxyC(Vec4<void> const& ang, Vec4<void> const& lin)
			:ang(ang)
			,lin(lin)
		{}
		Vec8ProxyC(Vec8<void> const& vec)
			:ang(vec.ang)
			,lin(vec.lin)
		{}
		operator Vec8<void>() const
		{
			return Vec8<void>{ang, lin};
		}
		float operator [](int i) const
		{
			return i < 4
				? ang[i  ]
				: lin[i-4];
		}
		friend bool FEql(Vec8ProxyC const& lhs, Vec8ProxyC const& rhs)
		{
			return FEql((Vec8<void>)lhs, (Vec8<void>)rhs);
		}
	};
	struct Vec8Proxy
	{
		Vec4<void>& ang;
		Vec4<void>& lin;
	
		Vec8Proxy(Vec4<void>& ang, Vec4<void>& lin)
			:ang(ang)
			,lin(lin)
		{}
		Vec8Proxy(Vec8<void>& vec)
			:ang(vec.ang)
			,lin(vec.lin)
		{}
		operator Vec8ProxyC() const
		{
			return Vec8ProxyC{ang, lin};
		}
		operator Vec8<void>() const
		{
			return Vec8<void>{ang, lin};
		}
		Vec8Proxy& operator = (Vec8ProxyC rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		Vec8Proxy& operator = (Vec8Proxy rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		Vec8Proxy& operator = (v8_cref<> rhs)
		{
			ang = rhs.ang;
			lin = rhs.lin;
			return *this;
		}
		float& operator [](int i)
		{
			return i < 4
				? ang[i  ]
				: lin[i-4];
		}
	};
	#endif
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"

namespace pr::maths
{
}
#endif