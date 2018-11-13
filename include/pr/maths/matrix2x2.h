//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector2.h"

namespace pr
{
	template <typename A, typename B>
	struct Mat2x2
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec2<void> x, y; };
			struct { Vec2<void> arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Mat2x2() = default;
		Mat2x2(float xx, float xy, float yx, float yy)
			:x(xx, xy)
			,y(yx, yy)
		{}
		Mat2x2(v2_cref<> x_, v2_cref<> y_)
			:x(x_)
			,y(y_)
		{}
		explicit Mat2x2(float x_)
			:x(x_)
			,y(x_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Mat2x2(V2 const& v)
			:Mat2x2(x_as<Vec2<void>>(v), y_as<Vec2<void>>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit Mat2x2(CP const* v)
			:Mat2x3(x_as<Vec2<T>>(v), y_as<Vec2<T>>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Mat2x2& operator = (V2 const& rhs)
		{
			x = x_as<Vec2<T>>(rhs);
			y = y_as<Vec2<T>>(rhs);
			return *this;
		}

		// Array access
		v2_cref<> operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Vec2<void>& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Create a rotation matrix
		static Mat2x2<A,B> Rotation(float angle)
		{
			return Mat2x2<A,B>{
				Vec2<void>(+Cos(angle), +Sin(angle)),
				Vec2<void>(-Sin(angle), +Cos(angle))};
		}
	};
	static_assert(maths::is_mat2<Mat2x2<void,void>>::value, "");
	static_assert(std::is_pod<Mat2x2<void,void>>::value, "m2x2 must be a pod type");
	template <typename A = void, typename B = void> using m2_cref = Mat2x2<A,B> const&;

	// Define component accessors
	template <typename A, typename B> inline v2_cref<> x_cp(m2_cref<A,B> v) { return v.x; }
	template <typename A, typename B> inline v2_cref<> y_cp(m2_cref<A,B> v) { return v.y; }
	template <typename A, typename B> inline v2_cref<> z_cp(m2_cref<A,B>)   { return v2Zero; }
	template <typename A, typename B> inline v2_cref<> w_cp(m2_cref<A,B>)   { return v2Zero; }

	#pragma region Operators
	template <typename A, typename B> inline Mat2x2<A,B> operator + (m2_cref<A,B> mat)
	{
		return mat;
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator - (m2_cref<A,B> mat)
	{
		return Mat2x2<A,B>{-mat.x, -mat.y};
	}
	template <typename A, typename B> inline Mat2x2<A,B> pr_vectorcall operator * (float lhs, m2_cref<A,B> rhs)
	{
		return rhs * lhs;
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator * (m2_cref<A,B> lhs, float rhs)
	{
		return Mat2x2<A,B>{lhs.x * rhs, lhs.y * rhs};
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator / (m2_cref<A,B> lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		return Mat2x2<A,B>{lhs.x / rhs, lhs.y / rhs};
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator % (m2_cref<A,B> lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		return Mat2x2<A,B>{lhs.x % rhs, lhs.y % rhs};
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator + (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
	{
		return Mat2x2<A,B>{lhs.x + rhs.x, lhs.y + rhs.y};
	}
	template <typename A, typename B> inline Mat2x2<A,B> operator - (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
	{
		return Mat2x2<A,B>{lhs.x - rhs.x, lhs.y - rhs.y};
	}
	template <typename A, typename B, typename C> inline Mat2x2<A,C> operator * (m2_cref<B,C> lhs, m2_cref<A,B> rhs)
	{
		auto ans = Mat2x2<A,C>{};
		auto lhsT = Transpose(lhs);
		ans.x.x = Dot2(lhsT.x, rhs.x);
		ans.x.y = Dot2(lhsT.y, rhs.x);
		ans.y.x = Dot2(lhsT.x, rhs.y);
		ans.y.y = Dot2(lhsT.y, rhs.y);
		return ans;
	}
	template <typename A, typename B> inline Vec2<B> operator * (m2_cref<A,B> lhs, v2_cref<A> rhs)
	{
		auto ans = Vec2<B>{};
		auto lhsT = Transpose_(lhs);
		ans.x = Dot2(lhsT.x, rhs);
		ans.y = Dot2(lhsT.y, rhs);
		return ans;
	}
	#pragma endregion

	#pragma region Functions
	
	// 2x2 matrix determinant
	template <typename A, typename B> inline float Determinant(m2_cref<A,B> m)
	{
		return m.x.x*m.y.y - m.x.y*m.y.x;
	}

	// 2x2 matrix transpose
	template <typename A, typename B> inline Mat2x2<A,B> Transpose(m2_cref<A,B> mat)
	{
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		return m;
	}

	// Returns true if 'mat' has an inverse
	template <typename A, typename B> inline bool IsInvertable(m2_cref<A,B> mat)
	{
		return Determinant(mat) != 0;
	}

	// Returns the inverse of 'mat' assuming is it a pure rotation matrix
	template <typename A, typename B> inline Mat2x2<B,A> InvertFast(m2_cref<A,B> mat)
	{
		assert("Matrix is not pure rotation" && FEql(Determinant(mat) ,1.0f));
		
		auto m = Mat2x2<B,A>{mat};
		m.x.x =  mat.y.y;
		m.x.y = -mat.y.x;
		m.y.y =  mat.x.x;
		m.y.x = -mat.x.y;
		return m;
	}

	// Returns the inverse of 'mat'
	template <typename A, typename B> inline Mat2x2<B,A> Invert(m2_cref<A,B> mat)
	{
		assert("Matrix is singular" && Determinant(mat) != 0);

		auto m = Mat2x2<B,A>{mat};
		auto det = Determinant(mat);
		m.x.x =  mat.y.y / det;
		m.x.y = -mat.y.x / det;
		m.y.y =  mat.x.x / det;
		m.y.x = -mat.x.y / det;
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using 'Denman-Beavers' square root iteration. Should converge quadratically
	template <typename A, typename B> inline Mat2x2<A,B> Sqrt(m2_cref<A,B> mat)
	{
		auto A = mat;           // Converges to mat^0.5
		auto B = m2x2Identity;  // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto A_next = 0.5f * (A + Invert(B));
			auto B_next = 0.5f * (B + Invert(A));
			A = A_next;
			B = B_next;
		}
		return A;
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(Matrix2x2Tests)
	{
		{// Create
			auto V0 = m2x2(1,2,3,4);
			PR_CHECK(V0.x == v2(1,2), true);
			PR_CHECK(V0.y == v2(3,4), true);

			auto V1 = m2x2(v2(1,2), v2(3,4));
			PR_CHECK(V1.x == v2(1,2), true);
			PR_CHECK(V1.y == v2(3,4), true);

			auto V2 = m2x2({1,2,3,4});
			PR_CHECK(V2.x == v2(1,2), true);
			PR_CHECK(V2.y == v2(3,4), true);

			auto V3 = m2x2{4,5,6,7};
			PR_CHECK(V3.x == v2(4,5), true);
			PR_CHECK(V3.y == v2(6,7), true);
		}
		{// Operators
			auto V0 = m2x2(1,2,3,4);
			auto V1 = m2x2(2,3,4,5);

			PR_CHECK(FEql(V0 + V1, m2x2(3,5,7,9)), true);
			PR_CHECK(FEql(V0 - V1, m2x2(-1,-1,-1,-1)), true);

			// 1 3     2 4     2+9  4+15     11 19
			// 2 4  x  3 5  =  4+12 8+20  =  16 28
			PR_CHECK(FEql(V0 * V1, m2x2(11,16,19,28)), true);

			PR_CHECK(FEql(V0 / 2.0f, m2x2(1.0f/2.0f, 2.0f/2.0f, 3.0f/2.0f, 4.0f/2.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, m2x2(1,0,1,0)), true);

			PR_CHECK(FEql(V0 * 3.0f, m2x2(3,6,9,12)), true);
			PR_CHECK(FEql(V0 / 2.0f, m2x2(0.5f, 1.0f, 1.5f, 2.0f)), true);
			PR_CHECK(FEql(V0 % 2.0f, m2x2(1,0,1,0)), true);

			PR_CHECK(FEql(3.0f * V0, m2x2(3,6,9,12)), true);

			PR_CHECK(FEql(+V0, m2x2(1,2,3,4)), true);
			PR_CHECK(FEql(-V0, m2x2(-1,-2,-3,-4)), true);

			PR_CHECK(V0 == m2x2(1,2,3,4), true);
			PR_CHECK(V0 != m2x2(4,3,2,1), true);
		}
		{// Min/Max/Clamp
			auto V0 = m2x2(1,2,3,4);
			auto V1 = m2x2(-1,-2,-3,-4);
			auto V2 = m2x2(2,4,6,8);

			PR_CHECK(FEql(Min(V0,V1,V2), m2x2(-1,-2,-3,-4)), true);
			PR_CHECK(FEql(Max(V0,V1,V2), m2x2(2,4,6,8)), true);
			PR_CHECK(FEql(Clamp(V0,V1,V2), m2x2(1,2,3,4)), true);
			PR_CHECK(FEql(Clamp(V0,0.0f,1.0f), m2x2(1,1,1,1)), true);
		}
	}
}
#endif