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
	struct v2
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x, y; };
			struct { float arr[2]; };
		};
		#pragma warning(pop)

		// Construct
		v2() = default;
		v2(float x_, float y_)
			:x(x_)
			,y(y_)
		{}
		explicit v2(float x_)
			:v2(x_, x_)
		{}
		template <typename T, typename = maths::enable_if_v2<T>> v2(T const& v)
			:v2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit v2(T const* v)
			:v2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename T, typename = maths::enable_if_v2<T>> v2& operator = (T const& rhs)
		{
			x = x_as<float>(rhs);
			y = y_as<float>(rhs);
			return *this;
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

		// Construct normalised
		template <typename = void> static v2 Normal2(float x, float y)
		{
			return Normalise2(v2(x,y));
		}
	};
	static_assert(maths::is_vec2<v2>::value, "");
	static_assert(std::is_pod<v2>::value, "v2 must be a pod type");

	// Define component accessors
	inline float x_cp(v2 const& v) { return v.x; }
	inline float y_cp(v2 const& v) { return v.y; }
	inline float z_cp(v2 const&)   { return 0; }
	inline float w_cp(v2 const&)   { return 0; }

	#pragma region Constants
	static v2 const v2Zero    = {0.0f, 0.0f};
	static v2 const v2Half    = {0.5f, 0.5f};
	static v2 const v2One     = {1.0f, 1.0f};
	static v2 const v2Min     = {+maths::float_min, +maths::float_min};
	static v2 const v2Max     = {+maths::float_max, +maths::float_max};
	static v2 const v2Lowest  = {-maths::float_max, -maths::float_max};
	static v2 const v2Epsilon = {+maths::float_eps, +maths::float_eps};
	static v2 const v2XAxis   = {1.0f, 0.0f};
	static v2 const v2YAxis   = {0.0f, 1.0f};
	#pragma endregion

	#pragma region Operators
	inline v2 operator + (v2 const& vec)
	{
		return vec;
	}
	inline v2 operator - (v2 const& vec)
	{
		return v2(-vec.x, -vec.y);
	}
	inline v2& operator *= (v2& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		return lhs;
	}
	inline v2& operator /= (v2& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		return lhs;
	}
	inline v2& operator %= (v2& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x = Fmod(lhs.x, rhs);
		lhs.y = Fmod(lhs.y, rhs);
		return lhs;
	}
	inline v2& operator += (v2& lhs, v2 const& rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		return lhs;
	}
	inline v2& operator -= (v2& lhs, v2 const& rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		return lhs;
	}
	inline v2& operator *= (v2& lhs, v2 const& rhs)
	{
		lhs.x *= rhs.x;
		lhs.y *= rhs.y;
		return lhs;
	}
	inline v2& operator /= (v2& lhs, v2 const& rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<float>));
		lhs.x /= rhs.x;
		lhs.y /= rhs.y;
		return lhs;
	}
	inline v2& operator %= (v2& lhs, v2 const& rhs)
	{
		assert("divide by zero" && !Any2(rhs, IsZero<float>));
		lhs.x = Fmod(lhs.x, rhs.x);
		lhs.y = Fmod(lhs.y, rhs.y);
		return lhs;
	}
	#pragma endregion

	#pragma region Functions
	
	// Dot product: a.b
	inline float Dot2(v2 const& lhs, v2 const& rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}
	inline float Dot(v2 const& lhs, v2 const& rhs)
	{
		return Dot2(lhs, rhs);
	}

	// Cross product: Dot2(Rotate90CW(lhs), rhs)
	inline float Cross2(v2 const& lhs, v2 const& rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y;
	}

	// Rotate a 2d vector by 90deg (when looking down the Z axis)
	inline v2 Rotate90CW(v2 const& v)
	{
		return v2(-v.y, v.x);
	}

	// Rotate a 2d vector by -90def (when looking down the Z axis)
	inline v2 Rotate90CCW(v2 const& v)
	{
		return v2(v.y, -v.x);
	}

	// Returns a vector with the 'xy' values permuted 'n' times. '0=xy, 1=yz'
	inline v2 Permute2(v2 const& v, int n)
	{
		switch (n%2)
		{
		default: return v;
		case 1:  return v2(v.y, v.x);
		}
	}

	// Returns a 2-bit bitmask of the quadrant the vector is in. 0=(-x,-y), 1=(+x,-y), 2=(-x,+y), 3=(+x,+y)
	inline uint Quadrant(v2 const& v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1);
	}

	// Divide a circle into N sectors and return an index for the sector that 'vec' is in
	inline int Sector(v2 const& vec, int sectors)
	{
		return int(pr::ATan2Positive(vec.y, vec.x) * sectors / maths::tau);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class std::numeric_limits<pr::v2>
	{
	public:
		static pr::v2 min() throw()     { return pr::v2Min; }
		static pr::v2 max() throw()     { return pr::v2Max; }
		static pr::v2 lowest() throw()  { return pr::v2Lowest; }
		static pr::v2 epsilon() throw() { return pr::v2Epsilon; }

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
#include <directxmath.h>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_vector2)
		{
			{// Create
				auto V0 = v2(1,1);
				PR_CHECK(V0.x == 1.0f, true);
				PR_CHECK(V0.y == 1.0f, true);

				auto V1 = v2(1,2);
				PR_CHECK(V1.x == 1.0f, true);
				PR_CHECK(V1.y == 2.0f, true);

				auto V2 = v2({3,4});
				PR_CHECK(V2.x == 3.0f, true);
				PR_CHECK(V2.y == 4.0f, true);

				v2 V3 = {4,5};
				PR_CHECK(V3.x == 4.0f, true);
				PR_CHECK(V3.y == 5.0f, true);

				auto V4 = v2::Normal2(3,4);
				PR_CHECK(FEql2(V4, v2(0.6f,0.8f)), true);
				PR_CHECK(FEql(V4[0], 0.6f), true);
				PR_CHECK(FEql(V4[1], 0.8f), true);
			}
			{// Operators
				auto V0 = v2(1,2);
				auto V1 = v2(2,3);

				PR_CHECK(FEql(V0 + V1, v2(3,5)), true);
				PR_CHECK(FEql(V0 - V1, v2(-1,-1)), true);
				PR_CHECK(FEql(V0 * V1, v2(2,6)), true);
				PR_CHECK(FEql(V0 / V1, v2(1.0f/2.0f, 2.0f/3.0f)), true);
				PR_CHECK(FEql(V0 % V1, v2(1,2)), true);

				PR_CHECK(FEql(V0 * 3.0f, v2(3,6)), true);
				PR_CHECK(FEql(V0 / 2.0f, v2(0.5f, 1.0f)), true);
				PR_CHECK(FEql(V0 % 2.0f, v2(1,0)), true);

				PR_CHECK(FEql(3.0f * V0, v2(3,6)), true);

				PR_CHECK(FEql(+V0, v2(1,2)), true);
				PR_CHECK(FEql(-V0, v2(-1,-2)), true);

				PR_CHECK(V0 == v2(1,2), true);
				PR_CHECK(V0 != v2(2,1), true);
			}
			{// Min/Max/Clamp
				auto V0 = v2(1,2);
				auto V1 = v2(-1,-2);
				auto V2 = v2(2,4);

				PR_CHECK(FEql(Min(V0,V1,V2), v2(-1,-2)), true);
				PR_CHECK(FEql(Max(V0,V1,V2), v2(2,4)), true);
				PR_CHECK(FEql(Clamp(V0,V1,V2), v2(1,2)), true);
				PR_CHECK(FEql(Clamp(V0,0.0f,1.0f), v2(1,1)), true);
			}
		}
	}
}
#endif
