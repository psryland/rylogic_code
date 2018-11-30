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
		Mat6x8(m3_cref<> m00_, m3_cref<> m01_, m3_cref<> m10_, m3_cref<> m11_)
			:m00(m00_)
			,m10(m10_)
			,m01(m01_)
			,m11(m11_)
		{}
		Mat6x8(v8_cref<> x, v8_cref<> y, v8_cref<> z, v8_cref<> u, v8_cref<> v, v8_cref<> w)
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
		Vec8<void> operator [](int i) const
		{
			// Note: Creating a Vec8Proxy doesn't work because by default the compiler selects the
			// mutable overload for non-const instances, so swap-style assignments don't work.
			assert("index out of range" && i >= 0 && i < 6);
			return i < 3
				? Vec8<void>{m00[i  ], m10[i  ]}
				: Vec8<void>{m01[i-3], m11[i-3]};
		}
		Vec8<void> col(int i) const
		{
			assert("index out of range" && i >= 0 && i < 6);
			return (*this)[i];
		}
		void col(int i, Vec8<void> const& rhs)
		{
			assert("index out of range" && i >= 0 && i < 6);
			if (i < 3) { m00[i  ] = rhs.ang; m10[i  ] = rhs.lin; }
			else       { m01[i-3] = rhs.ang; m11[i-3] = rhs.lin; }
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
	template <typename A, typename B> inline Mat6x8<A, B> operator + (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
	{
		return Mat6x8<A, B>{lhs.m00 + rhs.m00, lhs.m01 + rhs.m01, lhs.m10 + rhs.m10, lhs.m11 + rhs.m11};
	}
	template <typename A, typename B> inline Mat6x8<A, B> operator - (m6_cref<A,B> lhs, m6_cref<A,B> rhs)
	{
		return Mat6x8<A, B>{lhs.m00 - rhs.m00, lhs.m01 - rhs.m01, lhs.m10 - rhs.m10, lhs.m11 - rhs.m11};
	}
	template <typename A, typename B> inline Mat6x8<A,B> operator * (m6_cref<A,B> lhs, float rhs)
	{
		return Mat6x8<A,B>{lhs.m00 * rhs, lhs.m01 * rhs, lhs.m10 * rhs, lhs.m11 * rhs};
	}
	template <typename A, typename B> inline Mat6x8<A,B> operator * (float lhs, m6_cref<A,B> rhs)
	{
		return rhs * lhs;
	}
	template <typename A, typename B> inline Vec8<B> operator * (m6_cref<A,B> lhs, Vec8<A> const& rhs)
	{
		// [m00*a + m01*b] = [m00, m01] [a]
		// [m10*a + m11*b]   [m10, m11] [b]
		return Vec8<B>{
			lhs.m00 * rhs.ang + lhs.m01 * rhs.lin,
			lhs.m10 * rhs.ang + lhs.m11 * rhs.lin};
	}
	template <typename A, typename B, typename C> inline Mat6x8<A, C> pr_vectorcall operator * (m6_cref<B,C> lhs, m6_cref<A,B> rhs)
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
#include "pr/maths/matrix.h"
namespace pr::maths
{
	PRUnitTest(Matrix6x8Tests)
	{
		{// Memory order tests
			auto m1 = Matrix<float>{6, 8,  // = [{1} {2} {3} {4} {5} {6}]
			{
				1, 1, 1, 1, 1, 1, 1, 1,
				2, 2, 2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<float>{6, 8,
			{
				1, 1, 1, 1, 2, 2, 2, 2, // = [[1] [3]]
				1, 1, 1, 1, 2, 2, 2, 2, //   [[2] [4]]
				1, 1, 1, 1, 2, 2, 2, 2,
				3, 3, 3, 3, 4, 4, 4, 4,
				3, 3, 3, 3, 4, 4, 4, 4,
				3, 3, 3, 3, 4, 4, 4, 4,
			}};
			auto M1 = m6x8 // = [{1} {2} {3} {4} {5} {6}]
			{
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}},
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}
			};
			auto M2 = m6x8
			{
				m3x4{1}, m3x4{3}, // = [[1] [3]]
				m3x4{2}, m3x4{4}  //   [[2] [4]]
			};

			for (int c = 0; c != 6; ++c)
			for (int r = 0; r != 8; ++r)
			{
				PR_CHECK(FEql(m1(c,r), M1[c][r]), true);
				PR_CHECK(FEql(m2(c,r), M2[c][r]), true);
			}

			auto M3 = m6x8 // = [{1} {2} {3} {4} {5} {6}]
			{
				v8{1,1,1,1, 1,1,1,1},
				v8{2,2,2,2, 2,2,2,2},
				v8{3,3,3,3, 3,3,3,3},
				v8{4,4,4,4, 4,4,4,4},
				v8{5,5,5,5, 5,5,5,5},
				v8{6,6,6,6, 6,6,6,6},
			};
			PR_CHECK(FEql(M3, M1), true);
		}
		{// Array access
			auto m1 = m6x8
			{
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}, // = [{1} {2} {3} {4} {5} {6}]
				m3x4{v4{1},v4{2},v4{3}}, m3x4{v4{4},v4{5},v4{6}}
			};
			PR_CHECK(FEql(m1[0], v8{1,1,1,1, 1,1,1,1}), true);
			PR_CHECK(FEql(m1[1], v8{2,2,2,2, 2,2,2,2}), true);
			PR_CHECK(FEql(m1[2], v8{3,3,3,3, 3,3,3,3}), true);
			PR_CHECK(FEql(m1[3], v8{4,4,4,4, 4,4,4,4}), true);
			PR_CHECK(FEql(m1[4], v8{5,5,5,5, 5,5,5,5}), true);
			PR_CHECK(FEql(m1[5], v8{6,6,6,6, 6,6,6,6}), true);

			auto tmp = m1.col(0);
			m1.col(0, m1[5]);
			m1.col(5, tmp);
			PR_CHECK(FEql(m1[0], v8{6,6,6,6, 6,6,6,6}), true);
			PR_CHECK(FEql(m1[5], v8{1,1,1,1, 1,1,1,1}), true);
		}
		{// Multiply vector
			auto m = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto v = Matrix<float>{1, 6,
			{
				1, 2, 3, 4, 5, 6,
			}};
			auto e = Matrix<float>{1, 6,
			{
				91, 91, 91, 91, 91, 91,
			}};
			auto r = m * v;
			PR_CHECK(FEql(r, e), true);

			auto M = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}},
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto V = v8
			{
				v4{1, 2, 3, 0},
				v4{4, 5, 6, 0},
			};
			auto E = v8
			{
				v4{91, 91, 91, 0},
				v4{91, 91, 91, 0},
			};
			auto R = M * V;
			PR_CHECK(FEql(R, E), true);
		}
		{// Multiply matrix
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<float>{6, 6,
			{
				1, 1, 1, 2, 2, 2, // = [[1] [3]]
				1, 1, 1, 2, 2, 2, //   [[2] [4]]
				1, 1, 1, 2, 2, 2,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
			}};
			auto m3 = Matrix<float>{6, 6,
			{
				36, 36, 36, 36, 36, 36, 
				36, 36, 36, 36, 36, 36, 
				36, 36, 36, 36, 36, 36, 
				78, 78, 78, 78, 78, 78, 
				78, 78, 78, 78, 78, 78, 
				78, 78, 78, 78, 78, 78, 
			}};
			auto m4 = m1 * m2;
			PR_CHECK(FEql(m3, m4), true);

			auto M1 = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}},
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto M2 = m6x8
			{
				m3x4{v3{1},v3{1},v3{1}}, m3x4{v3{3},v3{3},v3{3}}, // = [[1] [3]]
				m3x4{v3{2},v3{2},v3{2}}, m3x4{v3{4},v3{4},v3{4}}  //   [[2] [4]]
			};
			auto M3 = m6x8
			{
				m3x4{v3{36},v3{36},v3{36}}, m3x4{v3{78},v3{78},v3{78}},
				m3x4{v3{36},v3{36},v3{36}}, m3x4{v3{78},v3{78},v3{78}} 
			};
			auto M4 = M1 * M2;
			PR_CHECK(FEql(M3, M4), true);
		}
		{// Transpose
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto M1 = m6x8
			{
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}, // = [{1} {2} {3} {4} {5} {6}]
				m3x4{v3{1},v3{2},v3{3}}, m3x4{v3{4},v3{5},v3{6}}
			};
			auto m2 = Transpose(m1);
			auto M2 = Transpose(M1);

			for (int c = 0; c != 6; ++c)
			{
				PR_CHECK(FEql(M2[c], v8{v3{1,2,3},v3{4,5,6}}), true);

				PR_CHECK(FEql(m2(c,0), M2[c].ang.x), true);
				PR_CHECK(FEql(m2(c,1), M2[c].ang.y), true);
				PR_CHECK(FEql(m2(c,2), M2[c].ang.z), true);
				PR_CHECK(FEql(m2(c,3), M2[c].lin.x), true);
				PR_CHECK(FEql(m2(c,4), M2[c].lin.y), true);
				PR_CHECK(FEql(m2(c,5), M2[c].lin.z), true);
			}
		}
	}
}
#endif