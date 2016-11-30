//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"

namespace pr
{
	// template <typename T> - todo: when MS fix the alignment bug for templates
	struct alignas(16) Vec4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x,y,z,w; };
			struct { v2 xy, zw; };
			struct { v3 xyz; float w; };
			struct { float arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#elif PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec;
			#else
			struct { float vec[4]; };
			#endif
		};
		#pragma warning(pop)

		// Construct
		Vec4() = default;
		Vec4(float x_, float y_, float z_, float w_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_ps(w_,z_,y_,x_))
		#else
			:x(x_)
			,y(y_)
			,z(z_)
			,w(w_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		explicit Vec4(float x_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_ps1(x_))
		#else
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		template <typename T, typename = maths::enable_if_v4<T>> explicit Vec4(T const& v)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		template <typename T, typename = maths::enable_if_v3<T>> Vec4(T const& v, float w_)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_)
		{}
		template <typename T, typename = maths::enable_if_v2<T>> Vec4(T const& v, float z_, float w_)
			:Vec4(x_as<float>(v), y_as<float>(v), z_, w_)
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit Vec4(T const* v)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		template <typename T, typename = maths::enable_if_v4<T>> Vec4& operator = (T const& rhs)
		{
			x = x_as<float>(rhs);
			y = y_as<float>(rhs);
			z = z_as<float>(rhs);
			w = w_as<float>(rhs);
			return *this;
		}
		#if PR_MATHS_USE_INTRINSICS
		Vec4(__m128 v)
			:vec(v)
		{
			assert(maths::is_aligned(this));
		}
		#endif

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

		// Create other vector types
		Vec4 w0() const
		{
			Vec4 r(x,y,z,0); // LValue because of alignment
			return r;
		}
		Vec4 w1() const
		{
			Vec4 r(x,y,z,1); // LValue because of alignment
			return r;
		}
		v2 vec2(int i0, int i1) const
		{
			return v2(arr[i0], arr[i1]);
		}
		v3 vec3(int i0, int i1, int i2) const
		{
			return v3(arr[i0], arr[i1], arr[i2]);
		}

		// Construct normalised
		template <typename = void> static Vec4 Normal3(float x, float y, float z, float w)
		{
			return Normalise3(Vec4(x, y, z, w));
		}
		template <typename = void> static Vec4 Normal4(float x, float y, float z, float w)
		{
			return Normalise4(Vec4(x, y, z, w));
		}
	};
	using v4 = Vec4;
	static_assert(maths::is_vec4<v4>::value, "");
	static_assert(std::is_pod<v4>::value, "v4 must be a pod type");
	static_assert(std::alignment_of<v4>::value == 16, "v4 should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using v4_cref = v4 const;
	#else
	using v4_cref = v4 const&;
	#endif

	// Define component accessors
	inline float x_cp(v4_cref v) { return v.x; }
	inline float y_cp(v4_cref v) { return v.y; }
	inline float z_cp(v4_cref v) { return v.z; }
	inline float w_cp(v4_cref v) { return v.w; }

	#pragma region Constants
	static v4 const v4Zero    = {0.0f, 0.0f, 0.0f, 0.0f};
	static v4 const v4Half    = {0.5f, 0.5f, 0.5f, 0.5f};
	static v4 const v4One     = {1.0f, 1.0f, 1.0f, 1.0f};
	static v4 const v4Min     = {+maths::float_min, +maths::float_min, +maths::float_min, +maths::float_min};
	static v4 const v4Max     = {+maths::float_max, +maths::float_max, +maths::float_max, +maths::float_max};
	static v4 const v4Lowest  = {-maths::float_max, -maths::float_max, -maths::float_max, -maths::float_max};
	static v4 const v4Epsilon = {+maths::float_eps, +maths::float_eps, +maths::float_eps, +maths::float_eps};
	static v4 const v4XAxis   = {1.0f, 0.0f, 0.0f, 0.0f};
	static v4 const v4YAxis   = {0.0f, 1.0f, 0.0f, 0.0f};
	static v4 const v4ZAxis   = {0.0f, 0.0f, 1.0f, 0.0f};
	static v4 const v4Origin  = {0.0f, 0.0f, 0.0f, 1.0f};
	#pragma endregion

	#pragma region Operators
	inline v4 pr_vectorcall operator + (v4_cref vec)
	{
		return vec;
	}
	inline v4 pr_vectorcall operator - (v4_cref vec)
	{
		return v4(-vec.x, -vec.y, -vec.z, -vec.w);
	}
	inline v4& pr_vectorcall operator *= (v4& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		lhs.z *= rhs;
		lhs.w *= rhs;
		return lhs;
	}
	inline v4& pr_vectorcall operator /= (v4& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		lhs.w /= rhs;
		return lhs;
	}
	inline v4& pr_vectorcall operator %= (v4& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x = Fmod(lhs.x, rhs);
		lhs.y = Fmod(lhs.y, rhs);
		lhs.z = Fmod(lhs.z, rhs);
		lhs.w = Fmod(lhs.w, rhs);
		return lhs;
	}
	inline v4& pr_vectorcall operator += (v4& lhs, v4_cref rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		lhs.w += rhs.w;
		return lhs;
	}
	inline v4& pr_vectorcall operator -= (v4& lhs, v4_cref rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		lhs.w -= rhs.w;
		return lhs;
	}
	inline v4& pr_vectorcall operator *= (v4& lhs, v4_cref rhs)
	{
		lhs.x *= rhs.x; 
		lhs.y *= rhs.y;
		lhs.z *= rhs.z;
		lhs.w *= rhs.w;
		return lhs;
	}
	inline v4& pr_vectorcall operator /= (v4& lhs, v4_cref rhs)
	{
		assert("divide by zero" && !Any4(rhs, IsZero<float>));
		lhs.x /= rhs.x;
		lhs.y /= rhs.y;
		lhs.z /= rhs.z;
		lhs.w /= rhs.w;
		return lhs;
	}
	inline v4& pr_vectorcall operator %= (v4& lhs, v4_cref rhs)
	{
		assert("divide by zero" && !Any4(rhs, IsZero<float>));
		lhs.x = Fmod(lhs.x, rhs.x);
		lhs.y = Fmod(lhs.y, rhs.y);
		lhs.z = Fmod(lhs.z, rhs.z);
		lhs.w = Fmod(lhs.w, rhs.w);
		return lhs;
	}
	inline v4 pr_vectorcall operator + (v4 const& lhs, v4_cref rhs)
	{
		auto v = lhs;
		return v += rhs;
	}
	inline v4 pr_vectorcall operator - (v4 const& lhs, v4_cref rhs)
	{
		auto v = lhs;
		return v -= rhs;
	}
	inline v4 pr_vectorcall operator * (v4 const& lhs, v4_cref rhs)
	{
		auto v = lhs;
		return v *= rhs;
	}
	inline v4 pr_vectorcall operator / (v4 const& lhs, v4_cref rhs)
	{
		auto v = lhs;
		return v /= rhs;
	}
	inline v4 pr_vectorcall operator % (v4 const& lhs, v4_cref rhs)
	{
		auto v = lhs;
		return v %= rhs;
	}
	#pragma endregion

	#pragma region Functions

	// V4 FEql
	inline bool pr_vectorcall FEql3(v4_cref lhs, v4_cref rhs, float tol = maths::tiny)
	{
		#if PR_MATHS_USE_INTRINSICS
		const __m128 zero = _mm_set_ps(tol,tol,tol,tol);
		auto d = _mm_sub_ps(lhs.vec, rhs.vec);                         /// d = lhs - rhs
		auto r = _mm_cmple_ps(_mm_mul_ps(d,d), _mm_mul_ps(zero,zero)); /// r = sqr(d) <= sqr(zero)
		auto m = _mm_movemask_ps(r);
		return (m & 0x07) == 0x07;
		#else
		return
			FEql(lhs.x, rhs.x, tol) &&
			FEql(lhs.y, rhs.y, tol) &&
			FEql(lhs.z, rhs.z, tol);
		#endif
	}
	inline bool pr_vectorcall FEql4(v4_cref lhs, v4_cref rhs, float tol = maths::tiny)
	{
		#if PR_MATHS_USE_INTRINSICS
		const __m128 zero = {tol, tol, tol, tol};
		auto d = _mm_sub_ps(lhs.vec, rhs.vec);                         /// d = lhs - rhs
		auto r = _mm_cmple_ps(_mm_mul_ps(d,d), _mm_mul_ps(zero,zero)); /// r = sqr(d) <= sqr(zero)
		return (_mm_movemask_ps(r) & 0x0f) == 0x0f;
		#else
		return
			FEql(lhs.x, rhs.x, tol) &&
			FEql(lhs.y, rhs.y, tol) &&
			FEql(lhs.z, rhs.z, tol) &&
			FEql(lhs.w, rhs.w, tol);
		#endif
	}
	inline bool pr_vectorcall FEql(v4_cref lhs, v4_cref rhs, float tol = maths::tiny)
	{
		return FEql4(lhs, rhs, tol);
	}

	// V4 length squared
	inline float pr_vectorcall Length2Sq(v4_cref v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0x31).m128_f32[0];
		#else
		return Len2Sq(v.x, v.y);
		#endif
	}
	inline float pr_vectorcall Length3Sq(v4_cref v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0x71).m128_f32[0];
		#else
		return Len3Sq(v.x, v.y, v.z);
		#endif
	}
	inline float pr_vectorcall Length4Sq(v4_cref v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0xF1).m128_f32[0];
		#else
		return Len4Sq(v.x, v.y, v.z, v.w);
		#endif
	}

	// Normalise the 'xyz' components of 'v'. Note: 'w' is also scaled
	inline v4 pr_vectorcall Normalise3(v4_cref v)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return v4(DirectX::XMVector3Normalize(v.vec));
		#elif PR_MATHS_USE_INTRINSICS
		// _mm_rsqrt_ps isn't accurate enough
		return v4(_mm_div_ps(v.vec, _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0x7F))));
		#else
		return v / Length3(v);
		#endif
	}

	// Normalise all components of 'v'
	inline v4 pr_vectorcall Normalise4(v4_cref v)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return v4(DirectX::XMVector4Normalize(v.vec));
		#elif PR_MATHS_USE_INTRINSICS
		return v4(_mm_div_ps(v.vec, _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0xFF))));
		#else
		return v / Length4(v);
		#endif
	}
	inline v4 pr_vectorcall Normalise(v4_cref v)
	{
		return Normalise4(v);
	}

	// Square: v * v
	inline v4 pr_vectorcall Sqr(v4_cref v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4(_mm_mul_ps(v.vec, v.vec));
		#else
		return v4(Sqr(v.x), Sqr(v.y), Sqr(v.z), Sqr(v.w));
		#endif
	}

	// Dot product: a . b
	inline float pr_vectorcall Dot3(v4_cref a, v4_cref b)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto r = _mm_dp_ps(a.vec, b.vec, 0x71);
		return r.m128_f32[0];
		#else
		return a.x * b.x + a.y * b.y + a.z * b.z;
		#endif
	}
	inline float pr_vectorcall Dot4(v4_cref a, v4_cref b)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(a.vec, b.vec, 0xF1).m128_f32[0];
		#else
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
		#endif
	}
	inline float pr_vectorcall Dot(v4_cref a, v4_cref b)
	{
		return Dot4(a,b);
	}

	// Cross product: a x b
	inline v4 pr_vectorcall Cross3(v4_cref a, v4_cref b)
	{
		#if PR_MATHS_USE_INTRINSICS
		return v4(_mm_sub_ps(
			_mm_mul_ps(_mm_shuffle_ps(a.vec, a.vec, _MM_SHUFFLE(3, 0, 2, 1)), _mm_shuffle_ps(b.vec, b.vec, _MM_SHUFFLE(3, 1, 0, 2))), 
			_mm_mul_ps(_mm_shuffle_ps(a.vec, a.vec, _MM_SHUFFLE(3, 1, 0, 2)), _mm_shuffle_ps(b.vec, b.vec, _MM_SHUFFLE(3, 0, 2, 1)))
			));
		#else
		return v4(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x, 0);
		#endif
	}
	inline v4 pr_vectorcall Cross(v4_cref a, v4_cref b)
	{
		return Cross3(a,b);
	}

	// Triple product: a . b x c
	inline float Triple(v4 const& a, v4 const& b, v4 const& c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Returns true if 'a' and 'b' parallel
	inline bool Parallel(v4 const& v0, v4 const& v1, float tol = maths::tiny)
	{
		return Length3Sq(Cross3(v0, v1)) <= Sqr(tol);
	}

	// Returns a vector guaranteed not parallel to 'v'
	inline v4 CreateNotParallelTo(v4 const& v)
	{
		bool x_aligned = Abs(v.x) > Abs(v.y) && Abs(v.x) > Abs(v.z);
		return v4(static_cast<float>(!x_aligned), 0.0f, static_cast<float>(x_aligned), v.w);
	}

	// Returns a vector perpendicular to 'v'
	inline v4 pr_vectorcall Perpendicular(v4_cref v)
	{
		assert("Cannot make a perpendicular to a zero vector" && !IsZero3(v));
		auto vec = Cross3(v, CreateNotParallelTo(v));
		vec *= Length3(v) / Length3(vec);
		return vec;
	}

	// Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular
	inline v4 pr_vectorcall Perpendicular(v4_cref vec, v4_cref previous)
	{
		assert("Cannot make a perpendicular to a zero vector" && !FEql3(vec, v4Zero));

		// If 'previous' is still perpendicular, keep it
		if (FEql(Dot3(vec, previous), 0))
			return previous;

		// If 'previous' is parallel to 'vec', choose a new perpendicular
		if (Parallel(vec, previous))
			return Perpendicular(vec);

		// Otherwise, make a perpendicular that is close to 'previous'
		return Normalise3(Cross3(Cross3(vec, previous), vec));
	}

	// Returns a vector with the 'xyz' values permuted 'n' times. '0=xyzw, 1=yzxw, 2=zxyw'
	inline v4 Permute3(v4 const& v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return v4(v.y, v.z, v.x, v.w);
		case 2:  return v4(v.z, v.x, v.y, v.w);
		}
	}

	// Returns a vector with the values permuted 'n' times. '0=xyzw, 1=yzwx, 2=zwxy, 3=wxyz'
	inline v4 Permute4(v4 const& v, int n)
	{
		switch (n%4)
		{
		default: return v;
		case 1:  return v4(v.y, v.z, v.w, v.x);
		case 2:  return v4(v.z, v.w, v.x, v.y);
		case 3:  return v4(v.w, v.x, v.y, v.z);
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	inline uint Octant(v4 const& v)
	{
		return Octant(v.xyz);
	}

	#pragma endregion
}

namespace std
{
	#pragma region Numeric limits
	template <> class numeric_limits<pr::v4>
	{
	public:
		static pr::v4 min() throw()     { return pr::v4Min; }
		static pr::v4 max() throw()     { return pr::v4Max; }
		static pr::v4 lowest() throw()  { return pr::v4Lowest; }
		static pr::v4 epsilon() throw() { return pr::v4Epsilon; }
		
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
		PRUnitTest(pr_maths_vector4)
		{
			#if PR_MATHS_USE_DIRECTMATH
			{
				v4 V0 = v4(1,2,3,4);
				DirectX::XMVECTORF32 VX0;
				VX0.v = V0.vec;
				PR_CHECK(V0.x, VX0.f[0]);
				PR_CHECK(V0.y, VX0.f[1]);
				PR_CHECK(V0.z, VX0.f[2]);
				PR_CHECK(V0.w, VX0.f[3]);
			}
			#endif
			{
				v4 a(1,2,-3,-4);
				auto t2 = maths::tiny * 2.0f;

				PR_CHECK(a.x, +1);
				PR_CHECK(a.y, +2);
				PR_CHECK(a.z, -3);
				PR_CHECK(a.w, -4);
				PR_CHECK( FEql (a, v4(1,2,-3,-4)), true);
				PR_CHECK(!FEql (a, v4(1,2,-3,-4+t2)), true);
				PR_CHECK( FEql4(a, v4(1,2,-3,-4)), true);
				PR_CHECK(!FEql4(a, v4(1,2,-3,-4+t2)), true);
				PR_CHECK( FEql3(a, v4(1,2,-3,-4+t2)), true);
				PR_CHECK(!FEql3(a, v4(1,2,-3+t2,-4+t2)), true);
				PR_CHECK( FEql2(a, v4(1,2,-3+t2,-4+t2)), true);
				PR_CHECK(!FEql2(a, v4(1,2+t2,-3+t2,-4+t2)), true);
			}
			{
				v4 a(3,-1,2,-4);
				v4 b = {-2,-1,4,2};
				PR_CHECK(Max(a,b), v4(3,-1,4,2));
				PR_CHECK(Min(a,b), v4(-2,-1,2,-4));
			}
			{
				v4 a(3,-1,2,-4);
				PR_CHECK(Length2Sq(a), a.x*a.x + a.y*a.y);
				PR_CHECK(Length2(a), sqrt(Length2Sq(a)));
				PR_CHECK(Length3Sq(a), a.x*a.x + a.y*a.y + a.z*a.z);
				PR_CHECK(Length3(a), sqrt(Length3Sq(a)));
				PR_CHECK(Length4Sq(a), a.x*a.x + a.y*a.y + a.z*a.z + a.w*a.w);
				PR_CHECK(Length4(a), sqrt(Length4Sq(a)));
			}
			{
				v4 a(3,-1,2,-4);
				v4 b = Normalise3(a);
				v4 c = Normalise4(a);
				PR_CHECK(Length3(b), 1.0f);
				PR_CHECK(b.w, a.w / Length3(a));
				PR_CHECK(sqrt(c.x*c.x + c.y*c.y + c.z*c.z + c.w*c.w), 1.0f);
				PR_CHECK(IsNormal3(a), false);
				PR_CHECK(IsNormal4(a), false);
				PR_CHECK(IsNormal3(b), true);
				PR_CHECK(IsNormal4(c), true);
			}
			{
				PR_CHECK(IsZero3(pr::v4(0,0,0,1)), true);
				PR_CHECK(IsZero4(pr::v4Zero), true);
				PR_CHECK(FEql3(pr::v4(1e-20f,0,0,1)     , pr::v4Zero), true);
				PR_CHECK(FEql4(pr::v4(1e-20f,0,0,1e-19f), pr::v4Zero), true);
			}
			{
				v4 a = {-2,  4,  2,  6};
				v4 b = { 3, -5,  2, -4};
				auto a2b = CPM(a, v4Origin);

				v4 c = Cross3(a,b);
				v4 d = a2b * b;
				PR_CHECK(FEql3(c,d), true);
			}
			{
				v4 a = {-2,  4,  2,  6};
				v4 b = { 3, -5,  2, -4};
				PR_CHECK(Dot4(a,b), -46);
				PR_CHECK(Dot3(a,b), -22);
			}
			{
				char c0;
				v4 const pt0[] =
				{
					v4(1,2,3,4),
					v4(5,6,7,8),
				};
				char c1;
				v4 const pt1[] =
				{
					v4(1,2,3,4),
					v4(5,6,7,8),
				};
				(void)c0,c1;
				PR_CHECK(maths::is_aligned(&pt0[0]), true);
				PR_CHECK(maths::is_aligned(&pt1[0]), true);
			}
		}
	}
}
#endif


	//// DirectXMath conversion functions
	//#if PR_MATHS_USE_DIRECTMATH
	//inline DirectX::XMVECTOR const& dxv4(v4 const& v) { assert(maths::is_aligned(&v)); return v.vec; }
	//inline DirectX::XMVECTOR&       dxv4(v4&       v) { assert(maths::is_aligned(&v)); return v.vec; }
	//#endif

