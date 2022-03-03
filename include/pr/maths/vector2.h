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
		constexpr Vec2(float x_, float y_)
			:x(x_)
			,y(y_)
		{}
		constexpr explicit Vec2(float x_)
			:Vec2(x_, x_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>>
		constexpr Vec2(V2 const& v)
			:Vec2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>>
		constexpr explicit Vec2(CP const* v)
			:Vec2(x_as<float>(v), y_as<float>(v))
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>>
		Vec2& operator = (V2 const& rhs)
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

		// Basic constants
		static constexpr Vec2 Zero()   { return Vec2{0,0}; }
		static constexpr Vec2 XAxis()  { return Vec2{1,0}; }
		static constexpr Vec2 YAxis()  { return Vec2{0,1}; }

		// Construct normalised
		static Vec2 Normal(float x, float y)
		{
			return Normalise(Vec2(x,y));
		}

		#pragma region Operators
		friend constexpr Vec2<T> operator + (v2_cref<T> vec)
		{
			return vec;
		}
		friend constexpr Vec2<T> operator - (v2_cref<T> vec)
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
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec2<T>{lhs.x / rhs, lhs.y / rhs};
		}
		friend Vec2<T> operator % (v2_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
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
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2<T>{lhs.x / rhs.x, lhs.y / rhs.y};
		}
		friend Vec2<T> operator % (v2_cref<T> lhs, v2_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec2<T>{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y)};
		}
		#pragma endregion

		// Define component accessors
		friend constexpr float x_cp(v2_cref<T> v) { return v.x; }
		friend constexpr float y_cp(v2_cref<T> v) { return v.y; }
		friend constexpr float z_cp(v2_cref<T>)   { return 0; }
		friend constexpr float w_cp(v2_cref<T>)   { return 0; }
	};
	static_assert(maths::is_vec2<Vec2<void>>::value, "");
	static_assert(std::is_trivially_copyable_v<Vec2<void>>, "Vec2 must be a pod type");

	#pragma region Functions
	
	// Dot product: a.b
	template <typename T> constexpr float Dot(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.x * rhs.x + lhs.y * rhs.y;
	}

	// Cross product: Dot2(Rotate90CW(lhs), rhs)
	template <typename T> constexpr float Cross(v2_cref<T> lhs, v2_cref<T> rhs)
	{
		return lhs.y * rhs.x - lhs.x * rhs.y;
	}

	// Rotate a 2d vector by 90deg (when looking down the Z axis)
	template <typename T> constexpr Vec2<T> Rotate90CW(v2_cref<T> v)
	{
		return Vec2<T>(-v.y, v.x);
	}

	// Rotate a 2d vector by -90def (when looking down the Z axis)
	template <typename T> constexpr Vec2<T> Rotate90CCW(v2_cref<T> v)
	{
		return Vec2<T>(v.y, -v.x);
	}

	// Returns a vector with the 'xy' values permuted 'n' times. '0=xy, 1=yz'
	template <typename T> constexpr Vec2<T> Permute(v2_cref<T> v, int n)
	{
		return (n%2) == 1 ? Vec2<T>(v.y, v.x) : v;
	}

	// Returns a 2-bit bitmask of the quadrant the vector is in. 0=(-x,-y), 1=(+x,-y), 2=(-x,+y), 3=(+x,+y)
	template <typename T> constexpr uint Quadrant(v2_cref<T> v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1);
	}

	// Divide a circle into N sectors and return an index for the sector that 'vec' is in
	template <typename T> inline int Sector(v2_cref<T> vec, int sectors)
	{
		return int(ATan2Positive(vec.y, vec.x) * sectors / maths::tau);
	}

	#pragma endregion
}

