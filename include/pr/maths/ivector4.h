//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/ivector2.h"

namespace pr
{
	template <typename intg> struct alignas(16) IVec4
	{
		using IVec2 = IVec2<intg>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { intg x, y, z, w; };
			struct { IVec2 xy, zw; };
			struct { intg arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128i vec;
			#endif
		};
		#pragma warning(pop)

		// Construct
		IVec4() = default;
		IVec4(intg x_, intg y_, intg z_, intg w_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_epi32(w_,z_,y_,x_))
		#else
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		explicit IVec4(intg x_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set1_epi32(x_))
		#else
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		template <typename T, typename = maths::enable_if_v4<T>> IVec4(T const& v)
			:IVec4(x_as<intg>(v), y_as<intg>(v), z_as<intg>(v), w_as<intg>(v))
		{}
		template <typename T, typename = maths::enable_if_v3<T>> IVec4(T const& v, intg w_)
			:IVec4(x_as<intg>(v), y_as<intg>(v), z_as<intg>(v), w_)
		{}
		template <typename T, typename = maths::enable_if_v2<T>> IVec4(T const& v, intg z_, intg w_)
			:IVec4(x_as<intg>(v), y_as<intg>(v), z_, w_)
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit IVec4(T const* v)
			:IVec4(x_as<intg>(v), y_as<intg>(v), z_as<intg>(v), w_as<intg>(v))
		{}
		template <typename T, typename = maths::enable_if_v4<T>> IVec4& operator = (T const& rhs)
		{
			x = x_as<intg>(rhs);
			y = y_as<intg>(rhs);
			z = z_as<intg>(rhs);
			w = w_as<intg>(rhs);
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

	using iv4 = IVec4<int>;
	static_assert(std::is_pod<iv4>::value || _MSC_VER < 1900, "iv4 must be a pod type");

	// Define component accessors for pointer types
	inline int x_cp(iv4 const& v) { return v.x; }
	inline int y_cp(iv4 const& v) { return v.y; }
	inline int z_cp(iv4 const& v) { return v.z; }
	inline int w_cp(iv4 const& v) { return v.w; }

	#pragma region Traits
	namespace maths
	{
		// Specialise marker traits
		template <> struct is_vec<iv4> :std::true_type
		{
			using elem_type = int;
			using cp_type = int;
			static int const dim = 4;
		};
	}
	#pragma endregion

	#pragma region Constants
	static iv4 const iv4Zero   = {0, 0, 0, 0};
	static iv4 const iv4One    = {1, 1, 1, 1};
	static iv4 const iv4Min    = {+maths::int_min, +maths::int_min, +maths::int_min, +maths::int_min};
	static iv4 const iv4Max    = {+maths::int_max, +maths::int_max, +maths::int_max, +maths::int_max};
	static iv4 const iv4Lowest = {-maths::int_max, -maths::int_max, -maths::int_max, -maths::int_max};
	static iv4 const iv4XAxis  = {1, 0, 0, 0};
	static iv4 const iv4YAxis  = {0, 1, 0, 0};
	static iv4 const iv4ZAxis  = {0, 0, 1, 0};
	static iv4 const iv4Origin = {0, 0, 0, 1};
	#pragma endregion

	#pragma region Operators
	inline iv4 operator + (iv4 const& vec)
	{
		return vec;
	}
	inline iv4 operator - (iv4 const& vec)
	{
		return iv4(-vec.x, -vec.y, -vec.z, -vec.w);
	}
	inline iv4& operator *= (iv4& lhs, int rhs)
	{
		lhs.x += rhs;
		lhs.y += rhs;
		lhs.z += rhs;
		lhs.w += rhs;
		return lhs;
	}
	inline iv4& operator /= (iv4& lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		lhs.w /= rhs;
		return lhs;
	}
	inline iv4& operator %= (iv4& lhs, int rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x %= rhs;
		lhs.y %= rhs;
		lhs.z %= rhs;
		lhs.w %= rhs;
		return lhs;
	}
	inline iv4& operator += (iv4& lhs, iv4 const& rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		lhs.w += rhs.w;
		return lhs;
	}
	inline iv4& operator -= (iv4& lhs, iv4 const& rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		lhs.w -= rhs.w;
		return lhs;
	}
	inline iv4& operator *= (iv4& lhs, iv4 const& rhs)
	{
		lhs.x *= rhs.x;
		lhs.y *= rhs.y;
		lhs.z *= rhs.z;
		lhs.w *= rhs.w;
		return lhs;
	}
	inline iv4& operator /= (iv4& lhs, iv4 const& rhs)
	{
		assert("divide by zero" && !Any4(rhs, IsZero<int>));
		lhs.x /= rhs.x;
		lhs.y /= rhs.y;
		lhs.z /= rhs.z;
		lhs.w /= rhs.w;
		return lhs;
	}
	inline iv4& operator %= (iv4& lhs, iv4 const& rhs)
	{
		assert("divide by zero" && !Any4(rhs, IsZero<int>));
		lhs.x %= rhs.x;
		lhs.y %= rhs.y;
		lhs.z %= rhs.z;
		lhs.w %= rhs.w;
		return lhs;
	}
	#pragma endregion

	#pragma region Functions
	
	// Dot product: a . b
	inline int Dot3(iv4 const& a, iv4 const& b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z;
	}
	inline int Dot4(iv4 const& a, iv4 const& b)
	{
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
	}
	inline int Dot(iv4 const& a, iv4 const& b)
	{
		return Dot4(a,b);
	}

	// Cross product: a x b
	inline iv4 Cross3(iv4 const& a, iv4 const& b)
	{
		return iv4(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x, 0);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class numeric_limits<pr::iv4>
	{
	public:
		static pr::iv4 min() throw()     { return pr::iv4Min; }
		static pr::iv4 max() throw()     { return pr::iv4Max; }
		static pr::iv4 lowest() throw()  { return pr::iv4Lowest; }

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
		PRUnitTest(pr_maths_ivector4)
		{
		}
	}
}
#endif