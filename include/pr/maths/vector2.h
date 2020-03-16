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
	template <typename T>
	struct Vec2
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
		Vec2() = default;
		Vec2(float x_, float y_)
			:x(x_)
			,y(y_)
		{}
		explicit Vec2(float x_)
			:Vec2(x_, x_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Vec2(V2 const& v)
			:Vec2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit Vec2(CP const* v)
			:Vec2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Vec2& operator = (V2 const& rhs)
		{
			x = x_as<float>(rhs);
			y = y_as<float>(rhs);
			return *this;
		}

		// Type conversion
		template <typename U> explicit operator Vec2<U>() const
		{
			return Vec2<U>{x, y};
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
		template <typename = void> static Vec2 Normal2(float x, float y)
		{
			return Normalise2(Vec2(x,y));
		}

		#pragma region Operators
		friend Vec2<T> operator + (v2_cref<T> vec)
		{
			return vec;
		}
		friend Vec2<T> operator - (v2_cref<T> vec)
		{
			return Vec2<T>{-vec.x, -vec.y};
		}
		friend Vec2<T> operator * (float lhs, v2_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec2<T> operator * (v2_cref<T> lhs, float rhs)
		{
			return Vec2<T>{lhs.x * rhs, lhs.y * rhs};
		}
		friend Vec2<T> operator / (v2_cref<T> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Vec2<T>{lhs.x / rhs, lhs.y / rhs};
		}
		friend Vec2<T> operator % (v2_cref<T> lhs, float rhs)
		{
			assert("divide by zero" && rhs != 0);
			return Vec2<T>{Fmod(lhs.x, rhs), Fmod(lhs.y, rhs)};
		}
		friend Vec2<T> operator + (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2<T>{lhs.x + rhs.x, lhs.y + rhs.y};
		}
		friend Vec2<T> operator - (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2<T>{lhs.x - rhs.x, lhs.y - rhs.y};
		}
		friend Vec2<T> operator * (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			return Vec2<T>{lhs.x * rhs.x, lhs.y * rhs.y};
		}
		friend Vec2<T> operator / (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			assert("divide by zero" && !Any2(rhs, IsZero<float>));
			return Vec2<T>{lhs.x / rhs.x, lhs.y / rhs.y};
		}
		friend Vec2<T> operator % (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			assert("divide by zero" && !Any2(rhs, IsZero<float>));
			return Vec2<T>{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y)};
		}
		#pragma endregion
	};
	static_assert(maths::is_vec2<Vec2<void>>::value, "");
	static_assert(std::is_pod<Vec2<void>>::value, "Vec2 must be a pod type");

	// Define component accessors
	template <typename T> inline float x_cp(v2_cref<T> v) { return v.x; }
	template <typename T> inline float y_cp(v2_cref<T> v) { return v.y; }
	template <typename T> inline float z_cp(v2_cref<T>)   { return 0; }
	template <typename T> inline float w_cp(v2_cref<T>)   { return 0; }

	#pragma region Functions
	
	// Dot product: a.b
	template <typename T> inline float Dot2(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}
	template <typename T> inline float Dot(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return Dot2(lhs, rhs);
	}

	// Cross product: Dot2(Rotate90CW(lhs), rhs)
	template <typename T> inline float Cross2(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y;
	}

	// Rotate a 2d vector by 90deg (when looking down the Z axis)
	template <typename T> inline Vec2<T> Rotate90CW(v2_cref<T> v)
	{
		return Vec2<T>(-v.y, v.x);
	}

	// Rotate a 2d vector by -90def (when looking down the Z axis)
	template <typename T> inline Vec2<T> Rotate90CCW(v2_cref<T> v)
	{
		return Vec2<T>(v.y, -v.x);
	}

	// Returns a vector with the 'xy' values permuted 'n' times. '0=xy, 1=yz'
	template <typename T> inline Vec2<T> Permute2(v2_cref<T> v, int n)
	{
		switch (n%2)
		{
		default: return v;
		case 1:  return Vec2<T>(v.y, v.x);
		}
	}

	// Returns a 2-bit bitmask of the quadrant the vector is in. 0=(-x,-y), 1=(+x,-y), 2=(-x,+y), 3=(+x,+y)
	template <typename T> inline uint Quadrant(v2_cref<T> v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1);
	}

	// Divide a circle into N sectors and return an index for the sector that 'vec' is in
	template <typename T> inline int Sector(v2_cref<T> vec, int sectors)
	{
		return int(pr::ATan2Positive(vec.y, vec.x) * sectors / maths::tau);
	}

	#pragma endregion
}


#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr::maths
{
	PRUnitTest(Vector2Tests)
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
			PR_CHECK(pr::FEql(V4[0], 0.6f), true);
			PR_CHECK(pr::FEql(V4[1], 0.8f), true);
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
#endif
