//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2.h"

namespace pr
{
	template <typename real = float> struct Vec3
	{
		using Vec2 = Vec2<real>;
		using Vec4 = Vec4<real>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { real x, y, z; };
			struct { Vec2 xy; };
			struct { real arr[3]; };
		};
		#pragma warning(pop)

		// Construct
		Vec3() = default;
		Vec3(real x_, real y_, real z_)
			:x(x_)
			,y(y_)
			,z(z_)
		{}
		explicit Vec3(real x_)
			:Vec3(x_, x_, x_)
		{}
		template <typename T, typename = maths::enable_if_v3<T>> Vec3(T const& v)
			:Vec3(x_as<real>(v), y_as<real>(v), z_as<real>(v))
		{}
		template <typename T, typename = maths::enable_if_v2<T>> Vec3(T const& v, real z_)
			:Vec3(x_as<real>(v), y_as<real>(v), z_)
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit Vec3(T const* v)
			:Vec3(x_as<real>(v), y_as<real>(v), z_as<real>(v))
		{}
		template <typename T, typename = maths::enable_if_v3<T>> Vec3& operator = (T const& rhs)
		{
			x = x_as<real>(rhs);
			y = y_as<real>(rhs);
			z = z_as<real>(rhs);
			return *this;
		}

		// Array access
		real const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		real& operator [] (int i)
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}

		// Create other vector types
		Vec4 w0() const
		{
			return Vec4(x, y, z, 0);
		}
		Vec4 w1() const
		{
			return Vec4(x, y, z, 1);
		}
		Vec2 vec2(int i0, int i1) const
		{
			return Vec2(arr[i0], arr[i1]);
		}

		// Construct normalised
		static Vec3 Normal3(real x, real y, real z)
		{
			return Normalise3(Vec3(x,y,z));
		}
	};

	using v3 = Vec3<>;
	static_assert(std::is_pod<v3>::value || _MSC_VER < 1900, "v3 must be a pod type");

	// Define component accessors for pointer types
	inline float x_cp(v3 const& v) { return v.x; }
	inline float y_cp(v3 const& v) { return v.y; }
	inline float z_cp(v3 const& v) { return v.z; }
	inline float w_cp(v3 const&)   { return 0; }

	#pragma region Traits
	namespace maths
	{
		// Specialise marker traits
		template <> struct is_vec<v3> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 3;
		};
	}
	#pragma endregion

	#pragma region Constants
	static v3 const v3Zero    = {0.0f, 0.0f, 0.0f};
	static v3 const v3Half    = {0.5f, 0.5f, 0.5f};
	static v3 const v3One     = {1.0f, 1.0f, 1.0f};
	static v3 const v3Min     = {+maths::float_min, +maths::float_min, +maths::float_min};
	static v3 const v3Max     = {+maths::float_max, +maths::float_max, +maths::float_max};
	static v3 const v3Lowest  = {-maths::float_max, -maths::float_max, -maths::float_max};
	static v3 const v3Epsilon = {+maths::float_eps, +maths::float_eps, +maths::float_eps};
	static v3 const v3XAxis   = {1.0f, 0.0f, 0.0f};
	static v3 const v3YAxis   = {0.0f, 1.0f, 0.0f};
	static v3 const v3ZAxis   = {0.0f, 0.0f, 1.0f};
	#pragma endregion

	#pragma region Operators
	inline v3 operator + (v3 const& vec)
	{
		return vec;
	}
	inline v3 operator - (v3 const& vec)
	{
		return v3(-vec.x, -vec.y, -vec.z);
	}
	inline v3& operator *= (v3& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		lhs.z *= rhs;
		return lhs;
	}
	inline v3& operator /= (v3& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		return lhs;
	}
	inline v3& operator %= (v3& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x  = Fmod(lhs.x, rhs);
		lhs.y  = Fmod(lhs.y, rhs);
		lhs.z  = Fmod(lhs.z, rhs);
		return lhs;
	}
	inline v3& operator += (v3& lhs, v3 const& rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		return lhs;
	}
	inline v3& operator -= (v3& lhs, v3 const& rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		return lhs;
	}
	inline v3& operator *= (v3& lhs, v3 const& rhs)
	{
		lhs.x *= rhs.x;
		lhs.y *= rhs.y;
		lhs.z *= rhs.z;
		return lhs;
	}
	inline v3& operator /= (v3& lhs, v3 const& rhs)
	{
		assert("divide by zero" && !Any3(rhs, IsZero<float>));
		lhs.x /= rhs.x;
		lhs.y /= rhs.y;
		lhs.z /= rhs.z;
		return lhs;
	}
	inline v3& operator %= (v3& lhs, v3 const& rhs)
	{
		assert("divide by zero" && !Any3(rhs, IsZero<float>));
		lhs.x = Fmod(lhs.x, rhs.x);
		lhs.y = Fmod(lhs.y, rhs.y);
		lhs.z = Fmod(lhs.z, rhs.z);
		return lhs;
	}
	#pragma endregion

	#pragma region Functions

	// Dot product: a . b
	inline float Dot3(v3 const& a, v3 const& b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
	inline float Dot(v3 const& a, v3 const& b)
	{
		return Dot3(a,b);
	}

	// Cross product: a x b
	inline v3 Cross3(v3 const& a, v3 const& b)
	{
		return v3(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
	}

	// Triple product: a . b x c
	inline float Triple3(v3 const& a, v3 const& b, v3 const& c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Returns a vector with the values permuted 'n' times. 0=xyz, 1=yzx, 2=zxy, etc
	inline v3 Permute3(v3 const& v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return v3(v.y, v.z, v.x);
		case 2:  return v3(v.z, v.x, v.y);
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	inline uint Octant(v3 const& v)
	{
		return (v.x >= 0.0f) | ((v.y >= 0.0f) << 1) | ((v.z >= 0.0f) << 2);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class numeric_limits<pr::v3>
	{
	public:
		static pr::v3 min() throw()     { return pr::v3Min; }
		static pr::v3 max() throw()     { return pr::v3Max; }
		static pr::v3 lowest() throw()  { return pr::v3Lowest; }
		static pr::v3 epsilon() throw() { return pr::v3Epsilon; }
		
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
		PRUnitTest(pr_maths_vector3)
		{
		}
	}
}
#endif
