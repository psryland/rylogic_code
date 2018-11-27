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
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec4<void> ang, lin; };
			struct { float arr[8]; };
		};
		#pragma warning(pop)

		Vec8() = default;
		Vec8(v3_cref<> ang_, v3_cref<> lin_)
			:ang(ang_, 0)
			,lin(lin_, 0)
		{}
		Vec8(v4_cref<> ang_, v4_cref<> lin_)
			:ang(ang_)
			,lin(lin_)
		{}
		Vec8(float wx, float wy, float wz, float vx, float vy, float vz)
			:ang(wx, wy, wz, 0)
			, lin(vx, vy, vz, 0)
		{}
		Vec8(float wx, float wy, float wz, float ww, float vx, float vy, float vz, float vw)
			:ang(wx, wy, wz, ww)
			, lin(vx, vy, vz, vw)
		{}

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

		// Reinterpret as a vector of a different vector space
		template <typename U> explicit operator Vec8<U>() const
		{
			return reinterpret_cast<Vec8<U> const&>(*this);
		}

		// Sample the vector field at 'ofs' returning the equivalent linear value assuming ang == 0.
		v4 LinAt(v4_cref<> ofs)
		{
			return v4{lin + Cross(ang, ofs)};
		}

		// Sample the vector field at 'ofs' returning the equivalent angular value assuming lin == 0.
		v4 AngAt(v4_cref<> ofs)
		{
			return v4{ang + Cross(lin, ofs)};
		}

	};
	static_assert(maths::is_vec<Vec8<void>>::value, "");
	static_assert(std::is_pod<Vec8<void>>::value, "v8 must be a pod type");
	static_assert(std::alignment_of<Vec8<void>>::value == 16, "v8 should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	template <typename T = void> using v8_cref = Vec8<T> const;
	#else
	template <typename T = void> using v8_cref = Vec8<T> const&;
	#endif

	//// Define component accessors
	//template <typename T> inline float x_cp(v8_cref<T> v) { return v.lin.x; }
	//template <typename T> inline float y_cp(v8_cref<T> v) { return v.lin.y; }
	//template <typename T> inline float z_cp(v8_cref<T> v) { return v.lin.z; }
	//template <typename T> inline float w_cp(v8_cref<T> v) { return v.lin.w; }

	#pragma region Operators
	template <typename T> inline Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs)
	{
		return lhs;
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs)
	{
		return Vec8<T>{-lhs.ang, -lhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (float lhs, v8_cref<T> rhs)
	{
		return rhs * lhs;
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang * rhs, lhs.lin * rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang / rhs, lhs.lin / rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, float rhs)
	{
		return Vec8<T>{lhs.ang % rhs, lhs.lin % rhs};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator + (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang + rhs.ang, lhs.lin + rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator - (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang - rhs.ang, lhs.lin - rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator * (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang * rhs.ang, lhs.lin * rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator / (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang / rhs.ang, lhs.lin / rhs.lin};
	}
	template <typename T> inline Vec8<T> pr_vectorcall operator % (v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return Vec8<T>{lhs.ang % rhs.ang, lhs.lin % rhs.lin};
	}
	#pragma endregion

	#pragma region Functions

	// Compare for floating point equality
	template <typename T> inline bool FEql(v8_cref<T> lhs, v8_cref<T> rhs)
	{
		return
			FEql(lhs.ang, rhs.ang) &&
			FEql(lhs.lin, rhs.lin);
	}

	#pragma endregion
}
