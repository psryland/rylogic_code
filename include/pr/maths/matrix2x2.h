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
	struct Mat2x2f
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec2f<void> x, y; };
			struct { Vec2f<void> arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Mat2x2f() = default;
		constexpr explicit Mat2x2f(float x_)
			:x(x_)
			,y(x_)
		{}
		constexpr Mat2x2f(float xx, float xy, float yx, float yy)
			:x(xx, xy)
			,y(yx, yy)
		{}
		constexpr Mat2x2f(v2_cref<> x_, v2_cref<> y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Mat2x2f(float const* v)
			:Mat2x2f(Vec2f<void>(&v[0]), Vec2f<void>(&v[2]))
		{}

		// Array access
		Vec2f<void> const& operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Vec2f<void>& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Basic constants
		static constexpr Mat2x2f Zero()
		{
			return Mat2x2f{v2::Zero(), v2::Zero()};
		}
		static constexpr Mat2x2f Identity()
		{
			return Mat2x2f{v2::XAxis(), v2::YAxis()};
		}

		// Create a rotation matrix
		static Mat2x2f<A,B> Rotation(float angle)
		{
			return Mat2x2f<A,B>{
				Vec2<void>(+Cos(angle), +Sin(angle)),
				Vec2<void>(-Sin(angle), +Cos(angle))};
		}

		// Create a 2D matrix containing random rotation between angles [min_angle, max_angle)
		template <typename Rng = std::default_random_engine> inline Mat2x2f Random(Rng& rng, float min_angle, float max_angle)
		{
			std::uniform_real_distribution<float> dist(min_angle, max_angle);
			return Rotation(dist(rng));
		}

		// Create a random 2D rotation matrix
		template <typename Rng = std::default_random_engine> inline Mat2x2f Random(Rng& rng)
		{
			return Random(rng, 0.0f, maths::tau);
		}

		#pragma region Operators
		friend constexpr Mat2x2f<A,B> operator + (m2_cref<A,B> mat)
		{
			return mat;
		}
		friend constexpr Mat2x2f<A,B> operator - (m2_cref<A,B> mat)
		{
			return Mat2x2f<A,B>{-mat.x, -mat.y};
		}
		friend Mat2x2f<A,B> operator * (float lhs, m2_cref<A,B> rhs)
		{
			return rhs * lhs;
		}
		friend Mat2x2f<A,B> operator * (m2_cref<A,B> lhs, float rhs)
		{
			return Mat2x2f<A,B>{lhs.x * rhs, lhs.y * rhs};
		}
		friend Mat2x2f<A,B> operator / (m2_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat2x2f<A,B>{lhs.x / rhs, lhs.y / rhs};
		}
		friend Mat2x2f<A,B> operator % (m2_cref<A,B> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Mat2x2f<A,B>{lhs.x % rhs, lhs.y % rhs};
		}
		friend Mat2x2f<A,B> operator + (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
		{
			return Mat2x2f<A,B>{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend Mat2x2f<A,B> operator - (m2_cref<A,B> lhs, m2_cref<A,B> rhs)
		{
			return Mat2x2f<A,B>{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend Vec2f<B> operator * (m2_cref<A,B> lhs, v2_cref<A> rhs)
		{
			auto ans = Vec2f<B>{};
			auto lhsT = Transpose_(lhs);
			ans.x = Dot2(lhsT.x, rhs);
			ans.y = Dot2(lhsT.y, rhs);
			return ans;
		}
		template <typename C> friend Mat2x2f<A,C> operator * (m2_cref<B,C> lhs, m2_cref<A,B> rhs)
		{
			auto ans = Mat2x2f<A,C>{};
			auto lhsT = Transpose(lhs);
			ans.x.x = Dot(lhsT.x, rhs.x);
			ans.x.y = Dot(lhsT.y, rhs.x);
			ans.y.x = Dot(lhsT.x, rhs.y);
			ans.y.y = Dot(lhsT.y, rhs.y);
			return ans;
		}
		#pragma endregion
	};
	static_assert(sizeof(Mat2x2f<void,void>) == 2*8);
	static_assert(maths::Matrix2<Mat2x2f<void,void>>);
	static_assert(std::is_trivially_copyable_v<Mat2x2f<void,void>>, "m2x2 must be a pod type");

	// 2x2 matrix determinant
	template <typename A, typename B> inline float Determinant(m2_cref<A,B> m)
	{
		return m.x.x*m.y.y - m.x.y*m.y.x;
	}

	// 2x2 matrix transpose
	template <typename A, typename B> inline Mat2x2f<A,B> Transpose(m2_cref<A,B> mat)
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
	template <typename A, typename B> inline Mat2x2f<B,A> InvertFast(m2_cref<A,B> mat)
	{
		assert("Matrix is not pure rotation" && FEql(Determinant(mat) ,1.0f));
		
		auto m = Mat2x2f<B,A>{mat};
		m.x.x =  mat.y.y;
		m.x.y = -mat.y.x;
		m.y.y =  mat.x.x;
		m.y.x = -mat.x.y;
		return m;
	}

	// Returns the inverse of 'mat'
	template <typename A, typename B> inline Mat2x2f<B,A> Invert(m2_cref<A,B> mat)
	{
		assert("Matrix is singular" && Determinant(mat) != 0);

		auto m = Mat2x2f<B,A>{mat};
		auto det = Determinant(mat);
		m.x.x =  mat.y.y / det;
		m.x.y = -mat.y.x / det;
		m.y.y =  mat.x.x / det;
		m.y.x = -mat.x.y / det;
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	template <typename A, typename B> inline Mat2x2f<A,B> Sqrt(m2_cref<A,B> mat)
	{
		// Using 'Denman-Beavers' square root iteration. Should converge quadratically
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
