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
		constexpr Mat2x2(float xx, float xy, float yx, float yy)
			:x(xx, xy)
			,y(yx, yy)
		{}
		constexpr Mat2x2(v2_cref<> x_, v2_cref<> y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Mat2x2(float x_)
			:x(x_)
			,y(x_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>>
		constexpr explicit Mat2x2(V2 const& v)
			:Mat2x2(x_as<Vec2<void>>(v), y_as<Vec2<void>>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>>
		constexpr explicit Mat2x2(CP const* v)
			:Mat2x2(x_as<Vec2<void>>(v), y_as<Vec2<void>>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>>
		Mat2x2& operator = (V2 const& rhs)
		{
			x = x_as<Vec2<void>>(rhs);
			y = y_as<Vec2<void>>(rhs);
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

		#pragma region Operators
		friend constexpr Mat2x2<A,B> operator + (m2_cref<A,B> mat)
		{
			return mat;
		}
		friend constexpr Mat2x2<A,B> operator - (m2_cref<A,B> mat)
		{
			return Mat2x2<A,B>{-mat.x, -mat.y};
		}
		friend Mat2x2<A,B> operator * (float lhs, m2_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat2x2<A,B> operator * (m2_cref<A,B> lhs, float rhs)
		{
			return Mat2x2<A,B>{lhs.x * rhs, lhs.y * rhs};
		}
		friend Mat2x2<A,B> operator / (m2_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat2x2<A,B>{lhs.x / rhs, lhs.y / rhs};
		}
		friend Mat2x2<A,B> operator % (m2_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat2x2<A,B>{lhs.x % rhs, lhs.y % rhs};
		}
		friend Mat2x2<A,B> operator + (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
		{
			return Mat2x2<A,B>{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend Mat2x2<A,B> operator - (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
		{
			return Mat2x2<A,B>{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend Vec2<B> operator * (m2_cref<A,B> lhs, v2_cref<A> rhs)
		{
			auto ans = Vec2<B>{};
			auto lhsT = Transpose_(lhs);
			ans.x = Dot2(lhsT.x, rhs);
			ans.y = Dot2(lhsT.y, rhs);
			return ans;
		}
		template <typename C> friend Mat2x2<A,C> operator * (m2_cref<B,C> lhs, m2_cref<A,B> rhs)
		{
			auto ans = Mat2x2<A,C>{};
			auto lhsT = Transpose(lhs);
			ans.x.x = Dot(lhsT.x, rhs.x);
			ans.x.y = Dot(lhsT.y, rhs.x);
			ans.y.x = Dot(lhsT.x, rhs.y);
			ans.y.y = Dot(lhsT.y, rhs.y);
			return ans;
		}
		#pragma endregion

		// Define component accessors
		friend constexpr v2_cref<> x_cp(m2_cref<A,B> v) { return v.x; }
		friend constexpr v2_cref<> y_cp(m2_cref<A,B> v) { return v.y; }
		friend constexpr v2_cref<> z_cp(m2_cref<A,B>)   { return v2{}; }
		friend constexpr v2_cref<> w_cp(m2_cref<A,B>)   { return v2{}; }

		// Create a rotation matrix
		static Mat2x2<A,B> Rotation(float angle)
		{
			return Mat2x2<A,B>{
				Vec2<void>(+Cos(angle), +Sin(angle)),
				Vec2<void>(-Sin(angle), +Cos(angle))};
		}
	};
	static_assert(maths::is_mat2<Mat2x2<void,void>>::value, "");
	static_assert(std::is_trivially_copyable_v<Mat2x2<void,void>>, "m2x2 must be a pod type");

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
	template <typename A, typename B> inline bool IsInvertible(m2_cref<A,B> mat)
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
		auto a = mat;              // Converges to mat^0.5
		auto b = m2x2{1, 0, 0, 1}; // Converges to mat^-0.5
		for (int i = 0; i != 10; ++i)
		{
			auto a_next = 0.5f * (a + Invert(b));
			auto b_next = 0.5f * (b + Invert(a));
			a = a_next;
			b = b_next;
		}
		return a;
	}

	#pragma endregion
}
