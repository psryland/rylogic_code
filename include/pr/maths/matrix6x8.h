//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector4.h"
#include "pr/maths/vector8.h"
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	// General 6x8 matrix
	template <typename A, typename B>
	struct alignas(16) Mat6x8
	{
		// Careful with memory layout: (Same style of memory layout as m4x4)
		// e.g.
		//  [{x} {y} {z} {w} {u} {v}]
		// is:                                                            memory order
		//  [x.x y.x z.x w.x u.x v.x] = [m00 m00 m00  m01 m01 m01]     [00 05 09  25 29 33]
		//  [x.y y.y z.y w.y u.y v.y] = [m00 m00 m00  m01 m01 m01]     [01 06 10  26 30 34]
		//  [x.z y.z z.z w.z u.z v.z] = [m00 m00 m00  m01 m01 m01]     [02 07 11  27 31 35]
		//  [x.- y.- z.- w.- u.- v.-] = [m00 m00 m00  m01 m01 m01]     [04 08 12  28 32 36]
		//  [x.w y.w z.w w.w u.w v.w] = [m10 m10 m10  m11 m11 m11]     [13 17 21  37 41 45]
		//  [x.u y.u z.u w.u u.u v.u] = [m10 m10 m10  m11 m11 m11]     [14 18 22  38 42 46]
		//  [x.v y.v z.v w.v u.v v.v] = [m10 m10 m10  m11 m11 m11]     [15 19 23  39 43 47]
		//  [x.- y.- z.- w.- u.- v.-] = [m10 m10 m10  m11 m11 m11]     [16 20 24  40 44 48]
		//                                                             
		// Notes:
		//  A,B should be the vector space that the transform operates on.
		//  Transforms within the same vector space should have A == B (e.g. coordinate transforms).
		//  Transforms from one to another vector space have A != B (e.g. inertia transforms).
		Mat3x4<void,void> m00, m10, m01, m11;

		// Construct from sub matrices. WARNING: careful with layout.
		Mat6x8() = default;
		constexpr Mat6x8(m3_cref<> m00_, m3_cref<> m01_, m3_cref<> m10_, m3_cref<> m11_)
			:m00(m00_)
			,m10(m10_)
			,m01(m01_)
			,m11(m11_)
		{}
		constexpr Mat6x8(v8_cref<> x, v8_cref<> y, v8_cref<> z, v8_cref<> u, v8_cref<> v, v8_cref<> w)
			:m00(x.ang, y.ang, z.ang)
			,m10(x.lin, y.lin, z.lin)
			,m01(u.ang, v.ang, w.ang)
			,m11(u.lin, v.lin, w.lin)
		{}

		// Reinterpret as a different matrix type
		template <typename U, typename V> explicit operator Mat6x8<U, V> const&() const
		{
			return reinterpret_cast<Mat6x8<U, V> const&>(*this);
		}
		template <typename U, typename V> explicit operator Mat6x8<U, V>&()
		{
			return reinterpret_cast<Mat6x8<U, V>&>(*this);
		}

		// Array of column vectors
		v8 operator [](int i) const
		{
			// Note: Creating a Vec8Proxy doesn't work because by default the compiler selects the
			// mutable overload for non-const instances, so swap-style assignments don't work.
			assert("index out of range" && i >= 0 && i < 6);
			return i < 3
				? v8{m00[i  ], m10[i  ]}
				: v8{m01[i-3], m11[i-3]};
		}
		v8 col(int i) const
		{
			assert("index out of range" && i >= 0 && i < 6);
			return (*this)[i];
		}
		void col(int i, v8_cref<> rhs)
		{
			assert("index out of range" && i >= 0 && i < 6);
			if (i < 3) { m00[i  ] = rhs.ang; m10[i  ] = rhs.lin; }
			else       { m01[i-3] = rhs.ang; m11[i-3] = rhs.lin; }
		}
	
		#pragma region Operators
		friend constexpr Mat6x8<A, B> operator + (m6_cref<A,B> m)
		{
			return m;
		}
		friend constexpr Mat6x8<A, B> operator - (m6_cref<A,B> m)
		{
			return Mat6x8<A, B>{-m.m00, -m.m01, -m.m10, -m.m11};
		}
		friend Mat6x8<A, B> operator + (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
		{
			return Mat6x8<A, B>{lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m10 + rhs.m10, lhs.m11 + rhs.m11};
		}
		friend Mat6x8<A, B> operator - (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
		{
			return Mat6x8<A, B>{lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m10 - rhs.m10, lhs.m11 - rhs.m11};
		}
		friend Mat6x8<A,B> operator * (m6_cref<A,B> lhs, float rhs)
		{
			return Mat6x8<A,B>{lhs.m00 * rhs, lhs.m01 * rhs, lhs.m10 * rhs, lhs.m11 * rhs};
		}
		friend Mat6x8<A,B> operator * (float lhs, m6_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Vec8<B> operator * (m6_cref<A,B> lhs, Vec8<A> const& rhs)
		{
			// [m00*a + m01*b] = [m00, m01] [a]
			// [m10*a + m11*b]   [m10, m11] [b]
			return Vec8<B>{
				lhs.m00 * rhs.ang + lhs.m01 * rhs.lin,
				lhs.m10 * rhs.ang + lhs.m11 * rhs.lin};
		}
		template <typename C> friend Mat6x8<A, C> pr_vectorcall operator * (m6_cref<B,C> lhs, m6_cref<A,B> rhs)
		{
			// [a00, a01] [b00, b01] = [a00*b00 + a01*b10, a00*b01 + a01*b11]
			// [a10, a11] [b10, b11]   [a10*b00 + a11*b10, a10*b01 + a11*b11]
			return Mat6x8<A, C>{
				lhs.m00*rhs.m00 + lhs.m01*rhs.m10, lhs.m00*rhs.m01 + lhs.m01*rhs.m11,
				lhs.m10*rhs.m00 + lhs.m11*rhs.m10, lhs.m10*rhs.m01 + lhs.m11*rhs.m11};
		}
		#pragma endregion
	};
	static_assert(maths::is_vec<Mat6x8<void,void>>::value, "");
	static_assert(std::is_trivially_copyable_v<Mat6x8<void,void>>, "m6x8 must be a pod type");
	static_assert(std::alignment_of_v<Mat6x8<void, void>> == 16, "m6x8 should have 16 byte alignment");

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

	// Invert the 6x6 matrix 'm'
	template <typename A, typename B> inline Mat6x8<B,A> Invert(m6_cref<A,B> m)
	{
		// 2x2 block matrix inversion
		// R = [A B]  R' = [E F]
		//     [C D]       [G H]
		// For square diagonal partitions of 'R' (i.e. submatrices are square)
		// If 'A' is non-singular then 'R' is invertible iff the Schur complement "D - CA¯B" of A is invertible
		// R'= [A¯ + A¯B(D-CA¯B)¯CA¯ ,  -A¯B(D-CA¯B)¯ ]
		//     [    -(D-CA¯B)¯CA¯    ,    (D-CA¯B)¯   ]
		// or:
		//     [   (A-BD¯C)¯     ,    -(A-BD¯C)¯BD¯   ]
		//     [ -D¯C(A-BD¯C)¯   , D¯+D¯C(A-BD¯C)¯BD¯ ]

		auto& a = m.m00;
		auto& b = m.m01;
		auto& c = m.m10;
		auto& d = m.m11;
		if (IsInvertible(a))
		{
			auto a¯ = Invert(a);
			auto schur = d - c * a¯ * b; // The 'Schur Complement'
			if (IsInvertible(schur))
			{
				auto schur¯ = Invert(schur);
				return Mat6x8<B,A>{
					a¯ + a¯ * b * schur¯ * c * a¯ , -a¯ * b * schur¯,
					             -schur¯ * c * a¯ ,           schur¯};
			}
		}
		if (IsInvertible(d))
		{
			auto d¯ = Invert(d);
			auto schur = a - b * d¯ * c; // The 'Schur Complement'
			if (IsInvertible(schur))
			{
				auto schur¯ = Invert(schur);
				return Mat6x8<B,A>{
					          schur¯ ,              -schur¯ * b * d¯,
					-d¯ * c * schur¯ , d¯ + d¯ * c * schur¯ * b * d¯};
			}
		}
		throw std::runtime_error("matrix is singular");
	}

	#pragma endregion
}
