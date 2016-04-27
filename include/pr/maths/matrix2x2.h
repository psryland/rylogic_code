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
	template <typename Vec2 = v2, typename real = maths::is_vec<Vec2>::elem_type> struct Mat2x2
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { Vec2 x, y; };
			struct { Vec2 arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		Mat2x2() = default;
		Mat2x2(real xx, real xy, real yx, real yy)
			:x(xx, xy)
			,y(yx, yy)
		{}
		Mat2x2(Vec2 const& x_, Vec2 const& y_)
			:x(x_)
			,y(y_)
		{}
		explicit Mat2x2(real x_)
			:x(x_)
			,y(x_)
		{}
		template <typename T, typename = maths::enable_if_v2<T>> Mat2x2(T const& v)
			:Mat2x2(x_as<Vec2>(v), y_as<Vec2>(v))
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit Mat2x2(T const* v)
			:Mat2x3(x_as<Vec2>(v), y_as<Vec2>(v))
		{}
		template <typename T, typename = maths::enable_if_v2<T>> Mat2x2& operator = (T const& rhs)
		{
			x = x_as<Vec2>(rhs);
			y = y_as<Vec2>(rhs);
			return *this;
		}

		// Array access
		Vec2 const& operator [](int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		Vec2& operator [](int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Create a rotation matrix
		static Mat2x2 Rotation(real angle)
		{
			return Mat2x2(
				Vec2(+Cos(angle), +Sin(angle)),
				Vec2(-Sin(angle), +Cos(angle)));
		}
	};

	using m2x2 = Mat2x2<>;
	static_assert(std::is_pod<m2x2>::value || _MSC_VER < 1900, "m2x2 must be a pod type");

	// Define component accessors
	inline v2 const& x_cp(m2x2 const& v) { return v.x; }
	inline v2 const& y_cp(m2x2 const& v) { return v.y; }
	inline v2 const& z_cp(m2x2 const&) { return v2Zero; }
	inline v2 const& w_cp(m2x2 const&) { return v2Zero; }

	#pragma region Traits
	namespace maths
	{
		// Specialise marker traits
		template <> struct is_vec<m2x2> :std::true_type
		{
			using elem_type = v2;
			static int const dim = 2;
		};
	}
	#pragma endregion

	#pragma region Constants
	static m2x2 const m2x2Zero     = {v2Zero, v2Zero};
	static m2x2 const m2x2Identity = {v2XAxis, v2YAxis};
	static m2x2 const m2x2One      = {v2One, v2One};
	static m2x2 const m2x2Min      = {+v2Min, +v2Min};
	static m2x2 const m2x2Max      = {+v2Max, +v2Max};
	static m2x2 const m2x2Lowest   = {-v2Max, -v2Max};
	static m2x2 const m2x2Epsilon  = {+v2Epsilon, +v2Epsilon};
	#pragma endregion

	#pragma region Operators
	inline m2x2 operator + (m2x2 const& mat)
	{
		return mat;
	}
	inline m2x2 operator - (m2x2 const& mat)
	{
		return m2x2(-mat.x, -mat.y);
	}
	inline m2x2& operator *= (m2x2& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		return lhs;
	}
	inline m2x2& operator /= (m2x2& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		return lhs;
	}
	inline m2x2& operator %= (m2x2& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x %= rhs;
		lhs.y %= rhs;
		return lhs;
	}
	inline m2x2& operator += (m2x2& lhs, m2x2 const& rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		return lhs;
	}
	inline m2x2& operator -= (m2x2& lhs, m2x2 const& rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		return lhs;
	}
	template <typename = void> inline m2x2 operator * (m2x2 const& lhs, m2x2 const& rhs)
	{
		auto ans = m2x2{};
		auto lhsT = Transpose_(lhs);
		ans.x.x = Dot2(lhsT.x, rhs.x);
		ans.x.y = Dot2(lhsT.y, rhs.x);
		ans.y.x = Dot2(lhsT.x, rhs.y);
		ans.y.y = Dot2(lhsT.y, rhs.y);
		return ans;
	}
	template <typename = void> inline v2 operator * (m2x2 const& lhs, v2 const& rhs)
	{
		auto ans = v2{};
		auto lhsT = Transpose_(lhs);
		ans.x = Dot2(lhsT.x, rhs);
		ans.y = Dot2(lhsT.y, rhs);
		return ans;
	}
	#pragma endregion

	#pragma region Functions
	
	// 2x2 matrix determinant
	inline float Determinant(m2x2 const& m)
	{
		return m.x.x*m.y.y - m.x.y*m.y.x;
	}

	// 2x2 matrix transpose
	inline m2x2 Transpose_(m2x2 const& mat)
	{
		auto m = mat;
		std::swap(m.x.y, m.y.x);
		return m;
	}

	// Returns true if 'mat' has an inverse
	inline bool IsInvertable(m2x2 const& mat)
	{
		return !FEql(Determinant(mat), 0);
	}

	// Returns the inverse of 'mat' assuming is it a pure rotation matrix
	inline m2x2 InvertFast(m2x2 const& mat)
	{
		assert("Matrix is not pure rotation" && FEql(Determinant(mat) ,1.0f));
		
		auto m = mat;
		m.x.x =  mat.y.y;
		m.x.y = -mat.y.x;
		m.y.y =  mat.x.x;
		m.y.x = -mat.x.y;
		return m;
	}

	// Returns the inverse of 'mat'
	inline m2x2 Invert(m2x2 const& mat)
	{
		assert("Matrix is singular" && Determinant(mat) != 0);

		auto m = mat;
		auto det = Determinant(mat);
		m.x.x =  mat.y.y / det;
		m.x.y = -mat.y.x / det;
		m.y.y =  mat.x.x / det;
		m.y.x = -mat.x.y / det;
		return m;
	}

	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using 'Denman-Beavers' square root iteration. Should converge quadratically
	inline m2x2 Sqrt(m2x2 const& mat)
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

namespace std
{
	#pragma region Numeric limits
	template <> class std::numeric_limits<pr::m2x2>
	{
	public:
		static pr::m2x2 min() throw()     { return pr::m2x2Min; }
		static pr::m2x2 max() throw()     { return pr::m2x2Max; }
		static pr::m2x2 lowest() throw()  { return pr::m2x2Lowest; }
		static pr::m2x2 epsilon() throw() { return pr::m2x2Epsilon; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = false;
		static const bool is_exact = false;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = true;
		static const float_denorm_style has_denorm = denorm_present;
		static const int radix = 10;
	};
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_matrix2x2)
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
}
#endif