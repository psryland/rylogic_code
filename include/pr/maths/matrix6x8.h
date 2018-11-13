//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	// General 6x8 matrix
	template <typename A, typename B>
	struct alignas(16) Mat6x8
	{
		// Notes:
		//  A,B should be the vector space that the transform operates on.
		//  Transforms within the same vector space should have A == B (e.g. coordinate transforms).
		//  Transforms from one to another vector space have A != B (e.g. inertia transforms).
		Mat3x4<void,void> m00, m01;
		Mat3x4<void,void> m10, m11;

		Mat6x8() = default;
		Mat6x8(m3_cref<> m00_, m3_cref<> m01_, m3_cref<> m10_, m3_cref<> m11_)
			:m00(m00_)
			,m01(m01_)
			,m10(m10_)
			,m11(m11_)
		{}

		// Reinterpret as a different matrix type
		template <typename U, typename V> explicit operator Mat6x8<U, V>() const
		{
			return reinterpret_cast<Mat6x8<U, V> const&>(*this);
		}

		// Array access
		v8_cref<> operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < 8);
			return i < 3
				? Vec8<void>{m00[i  ], m10[i  ]}
				: Vec8<void>{m01[i-3], m11[i-3]};
		}
	};
	static_assert(maths::is_vec<Mat6x8<void,void>>::value, "");
	static_assert(std::is_pod<Mat6x8<void,void>>::value, "m6x8 must be a pod type");
	static_assert(std::alignment_of<Mat6x8<void, void>>::value == 16, "m6x8 should have 16 byte alignment");
	template <typename A = void, typename B = void> using m6_cref = Mat6x8<A,B> const&;
	
	#pragma region Operators
	template <typename A, typename B> inline Mat6x8<A, B> operator + (m6_cref<A,B> m)
	{
		return m;
	}
	template <typename A, typename B> inline Mat6x8<A, B> operator - (m6_cref<A,B> m)
	{
		return Mat6x8<A, B>{-m.m00, -m.m01, -m.m10, -m.m11};
	}
	template <typename A, typename B> inline Mat6x8<A,B> operator * (float lhs, m6_cref<A,B> rhs)
	{
		return rhs * lhs;
	}
	template <typename A, typename B> inline Mat6x8<A, B> operator + (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
	{
		return Mat6x8<A, B>{lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m10 + rhs.m10, lhs.m11 + rhs.m11};
	}
	template <typename A, typename B> inline Mat6x8<A, B> operator - (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
	{
		return Mat6x8<A, B>{lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m10 - rhs.m10, lhs.m11 - rhs.m11};
	}
	template <typename A, typename B> inline Vec8<B> operator * (m6_cref<A,B> lhs, Vec8<A> const& rhs)
	{
		// [m00*a + m01*b] = [m00, m01] [a]
		// [m10*a + m11*b]   [m10, m11] [b]
		return Vec8<B>{
			lhs.m00 * rhs.ang + lhs.m01 * rhs.lin,
			lhs.m10 * rhs.ang + lhs.m11 * rhs.lin};
	}
	template <typename A, typename B, typename C> inline Mat6x8<A, C> operator * (Mat6x8<B, C> const& lhs, m6_cref<A,B> rhs)
	{
		// [a00, a01] [b00, b01] = [a00*b00 + a01*b10, a00*b01 + a01*b11]
		// [a10, a11] [b10, b11]   [a10*b00 + a11*b10, a10*b01 + a11*b11]
		return Mat6x8<A, C>{
			lhs.m00*rhs.m00 + lhs.m01*rhs.m10, lhs.m00*rhs.m01 + lhs.m01*rhs.m11,
			lhs.m10*rhs.m00 + lhs.m11*rhs.m10, lhs.m10*rhs.m01 + lhs.m11*rhs.m11};
	}
	#pragma endregion

	#pragma region Functions

	// Compare for floating point equality
	template <typename A, typename B> inline bool FEql(m6_cref<A,B> lhs, m6_cref<A,B> rhs)
	{
		return
			FEql(lhs.m00, rhs.m00) &&
			FEql(lhs.m01, rhs.m01) &&
			FEql(lhs.m10, rhs.m10) &&
			FEql(lhs.m11, rhs.m11);
	}

	// Return the transpose of a spatial matrix
	template <typename A, typename B> inline Mat6x8<A, B> Transpose(m6_cref<A,B> m)
	{
		return Mat6x8<A, B>(
			Transpose(m.m00), Transpose(m.m10),
			Transpose(m.m01), Transpose(m.m11));
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(SpatialMatrixTests)
	{
//		auto v0 = Vec8<Motion>{v4{1,1,1,0}, v4{0,0,1,0}};
//		auto ofs = v4{1,0,0,0};
//		auto rot = m3x4::Rotation(v4YAxis);
//		auto a2b_mm = Mat6x8<Motion,Motion>{rot, m3x4Zero, -rot * CPM(ofs), rot};
//		auto b2a_mm = Mat6x8<Motion,Motion>{Transpose(rot), -rot * CPM(ofs), m3x4Zero, Transpose(rot)};;
//
//		PR_CHECK(Transpose(a2b_mm) == b2a_mm, true);
	}
}
#endif