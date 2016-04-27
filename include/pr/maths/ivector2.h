//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"

namespace pr
{
	template <typename intg = int> struct IVec2
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { intg x, y; };
			struct { intg arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		IVec2() = default;
		IVec2(intg x_, intg y_)
			:x(x_)
			,y(y_)
		{}
		explicit IVec2(intg x_)
			:IVec2(x_, x_)
		{}
		template <typename T, typename = maths::enable_if_v2<T>> IVec2(T const& v)
			:IVec2(x_as<intg>(v), y_as<intg>(v))
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit IVec2(T const* v)
			:IVec2(x_as<intg>(v), y_as<intg>(v))
		{}
		template <typename T, typename = maths::enable_if_v2<T>> IVec2& operator = (T const& rhs)
		{
			x = x_as<intg>(rhs);
			y = y_as<intg>(rhs);
			return *this;
		}

		// Array access
		intg const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		intg& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
	};

	using iv2 = IVec2<>;
	static_assert(std::is_pod<iv2>::value || _MSC_VER < 1900, "iv2 must be a pod type");

	// Define component accessors for pointer types
	inline int x_cp(iv2 const& v) { return v.x; }
	inline int y_cp(iv2 const& v) { return v.y; }
	inline int z_cp(iv2 const&)   { return 0; }
	inline int w_cp(iv2 const&)   { return 0; }

	#pragma region Traits
	namespace maths
	{
		// Specialise marker traits
		template <> struct is_vec<iv2> :std::true_type
		{
			using elem_type = int;
			using cp_type = int;
			static int const dim = 2;
		};
	}
	#pragma endregion

	#pragma region Constants
	static iv2 const iv2Zero    = {0, 0};
	static iv2 const iv2One     = {1, 1};
	static iv2 const iv2Min     = {+maths::int_min, +maths::int_min};
	static iv2 const iv2Max     = {+maths::int_max, +maths::int_max};
	static iv2 const iv2Lowest  = {-maths::int_max, -maths::int_max};
	static iv2 const iv2XAxis   = {1, 0};
	static iv2 const iv2YAxis   = {0, 1};
	#pragma endregion

	#pragma region Operators
	inline iv2 operator + (iv2 const& vec)
	{
		return vec;
	}
	inline iv2 operator - (iv2 const& vec)
	{
		return iv2(-vec.x, -vec.y);
	}
	inline iv2& operator *= (iv2& lhs, int rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		return lhs;
	}
	inline iv2& operator /= (iv2& lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		return lhs;
	}
	inline iv2& operator %= (iv2& lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x %= rhs;
		lhs.y %= rhs;
		return lhs;
	}
	inline iv2& operator += (iv2& lhs, iv2 const& rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		return lhs;
	}
	inline iv2& operator -= (iv2& lhs, iv2 const& rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		return lhs;
	}
	inline iv2& operator *= (iv2& lhs, iv2 const& rhs)
	{
		lhs.x *= rhs.x;
		lhs.y *= rhs.y;
		return lhs;
	}
	inline iv2& operator /= (iv2& lhs, iv2 const& rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<int>));
		lhs.x /= rhs.x;
		lhs.y /= rhs.y;
		return lhs;
	}
	inline iv2& operator %= (iv2& lhs, iv2 const& rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<int>));
		lhs.x %= rhs.x;
		lhs.y %= rhs.y;
		return lhs;
	}
	#pragma endregion

	#pragma region Functions

	// Dot product: a . b
	inline int Dot2(iv2 const& a, iv2 const& b)
	{
		return a.x * b.x + a.y * b.y;
	}
	inline int Dot(iv2 const& a, iv2 const& b)
	{
		return Dot2(a,b);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class numeric_limits<pr::iv2>
	{
	public:
		static pr::iv2 min() throw()     { return pr::iv2Min; }
		static pr::iv2 max() throw()     { return pr::iv2Max; }
		static pr::iv2 lowest() throw()  { return pr::iv2Lowest; }

		static const bool is_specialized = true;
		static const bool is_signed = true;
		static const bool is_integer = true;
		static const bool is_exact = true;
		static const bool has_infinity = false;
		static const bool has_quiet_NaN = false;
		static const bool has_signaling_NaN = false;
		static const bool has_denorm_loss = false;
		static const float_denorm_style has_denorm = denorm_absent;
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
		PRUnitTest(pr_maths_ivector2)
		{
		}
	}
}
#endif