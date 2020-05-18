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
	template <typename T>
	struct alignas(16) Vec4
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x,y,z,w; };
			struct { Vec2<T> xy, zw; };
			struct { Vec3<T> xyz; float w; };
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
		template <typename V4, typename = maths::enable_if_v4<V4>> explicit Vec4(V4 const& v)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		template <typename V3, typename = maths::enable_if_v3<V3>> Vec4(V3 const& v, float w_)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Vec4(V2 const& v, float z_, float w_)
			:Vec4(x_as<float>(v), y_as<float>(v), z_, w_)
		{}
		template <typename V2, typename = maths::enable_if_v2<V2>> Vec4(V2 const& xy, V2 const& zw)
			:Vec4(x_as<float>(xy), y_as<float>(xy), x_as<float>(zw), y_as<float>(zw))
		{}
		template <typename CP, typename = maths::enable_if_vec_cp<CP>> explicit Vec4(CP const* v)
			:Vec4(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		template <typename V4, typename = maths::enable_if_v4<V4>> Vec4& operator = (V4 const& rhs)
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

		// Type conversion
		template <typename U> explicit operator Vec4<U>() const
		{
			return Vec4<U>{x, y, z, w};
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

		// Create other vector types
		Vec4<T> w0() const
		{
			Vec4<T> r(x,y,z,0); // LValue because of alignment
			return r;
		}
		Vec4<T> w1() const
		{
			Vec4<T> r(x,y,z,1); // LValue because of alignment
			return r;
		}
		Vec2<T> vec2(int i0, int i1) const
		{
			return Vec2<T>{arr[i0], arr[i1]};
		}
		Vec3<T> vec3(int i0, int i1, int i2) const
		{
			return Vec3<T>{arr[i0], arr[i1], arr[i2]};
		}

		// Construct normalised
		static Vec4 Normal(float x, float y, float z, float w)
		{
			return Normalise(Vec4<T>{x, y, z, w});
		}

		#pragma region Operators
		friend bool pr_vectorcall operator == (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return _mm_movemask_ps(_mm_cmpeq_ps(lhs.vec, rhs.vec)) == 0xF;
			#else
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
			#endif
		}
		friend bool pr_vectorcall operator != (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			return !(lhs == rhs);
		}
		friend Vec4<T> pr_vectorcall operator + (v4_cref<T> vec)
		{
			return vec;
		}
		friend Vec4<T> pr_vectorcall operator - (v4_cref<T> vec)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_sub_ps(_mm_setzero_ps(), vec.vec)};
			#else
			return Vec4<T>{-vec.x, -vec.y, -vec.z, -vec.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator * (float lhs, v4_cref<T> rhs)
		{
			return rhs * lhs;
		}
		friend Vec4<T> pr_vectorcall operator * (v4_cref<T> lhs, float rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_mul_ps(lhs.vec, _mm_set_ps1(rhs))};
			#else
			return Vec4<T>{lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator / (v4_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_div_ps(lhs.vec, _mm_set_ps1(rhs))};
			#else
			return Vec4<T>{lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator % (v4_cref<T> lhs, float rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && rhs != 0);
			return Vec4<T>{Fmod(lhs.x, rhs), Fmod(lhs.y, rhs), Fmod(lhs.z, rhs), Fmod(lhs.w, rhs)};
		}
		friend Vec4<T> pr_vectorcall operator / (float lhs, v4_cref<T> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_div_ps(_mm_set_ps1(lhs), rhs.vec)};
			#else
			return Vec4<T>{lhs / rhs.x, lhs / rhs.y, lhs / rhs.z, lhs / rhs.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator % (float lhs, v4_cref<T> rhs)
		{
			return Vec4<T>{Fmod(lhs, rhs.x), Fmod(lhs, rhs.y), Fmod(lhs, rhs.z), Fmod(lhs, rhs.w)};
		}
		friend Vec4<T> pr_vectorcall operator + (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_add_ps(lhs.vec, rhs.vec)};
			#else
			return Vec4<T>{lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator - (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_sub_ps(lhs.vec, rhs.vec)};
			#else
			return Vec4<T>{lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator * (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_mul_ps(lhs.vec, rhs.vec)};
			#else
			return Vec4<T>{lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator / (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			#if PR_MATHS_USE_INTRINSICS
			return Vec4<T>{_mm_div_ps(lhs.vec, rhs.vec)};
			#else
			return Vec4<T>{lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w};
			#endif
		}
		friend Vec4<T> pr_vectorcall operator % (v4_cref<T> lhs, v4_cref<T> rhs)
		{
			// Don't check for divide by zero by default. For floats +inf/-inf are valid results
			//assert("divide by zero" && All(rhs, [](auto x) { return x != 0; }));
			return Vec4<T>{Fmod(lhs.x, rhs.x), Fmod(lhs.y, rhs.y), Fmod(lhs.z, rhs.z), Fmod(lhs.w, rhs.w)};
		}
		#pragma endregion

		// Component accessors
		friend constexpr float pr_vectorcall x_cp(v4_cref<T> v) { return v.x; }
		friend constexpr float pr_vectorcall y_cp(v4_cref<T> v) { return v.y; }
		friend constexpr float pr_vectorcall z_cp(v4_cref<T> v) { return v.z; }
		friend constexpr float pr_vectorcall w_cp(v4_cref<T> v) { return v.w; }
	};
	static_assert(maths::is_vec4<Vec4<void>>::value, "");
	static_assert(std::is_pod_v<Vec4<void>>, "Vec4 must be a pod type");
	static_assert(std::alignment_of_v<Vec4<void>> == 16, "Vec4 should have 16 byte alignment");

	#pragma region Functions

	// V4 FEql
	template <typename T> inline bool pr_vectorcall FEqlAbsolute(v4_cref<T> a, v4_cref<T> b, float tol)
	{
		// abs(a - b) < tol
		#if PR_MATHS_USE_INTRINSICS
		auto d = _mm_sub_ps(a.vec, b.vec);                 // d = a - b;
		auto abs_d = _mm_andnot_ps(_mm_set_ps1(-0.0f), d); // d = abs(a - b);
		auto r = _mm_cmplt_ps(abs_d, _mm_set_ps1(tol));    // r = abs(d) < tol
		return (_mm_movemask_ps(r) & 0x0f) == 0x0f;
		#else
		return
			FEqlAbsolute(lhs.x, rhs.x, tol) &&
			FEqlAbsolute(lhs.y, rhs.y, tol) &&
			FEqlAbsolute(lhs.z, rhs.z, tol) &&
			FEqlAbsolute(lhs.w, rhs.w, tol);
		#endif
	}
	template <typename T> inline bool pr_vectorcall FEqlRelative(v4_cref<T> a, v4_cref<T> b, float tol)
	{
		// Handles tests against zero where relative error is meaningless
		// Tests with 'b == 0' are the most common so do them first
		if (b == v4{}) return MaxElementAbs(a) < tol;
		if (a == v4{}) return MaxElementAbs(b) < tol;

		// Handle infinities and exact values
		if (a == b) return true;

		auto abs_max_element = Max(MaxElementAbs(a), MaxElementAbs(b));
		return FEqlAbsolute(a, b, tol * abs_max_element);
	}
	template <typename T> inline bool pr_vectorcall FEql(v4_cref<T> a, v4_cref<T> b)
	{
		return FEqlRelative(a, b, maths::tinyf);
	}

	// Abs
	template <typename T> inline Vec4<T> pr_vectorcall Abs(v4_cref<T> v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_andnot_ps(_mm_set_ps1(-0.0f), v.vec);
		#else
		return Vec4<T>{Abs(v.x), Abs(v.y), Abs(v.z), Abs(v.w)};
		#endif
	}

	// V4 length squared
	template <typename T> inline float pr_vectorcall LengthSq(v4_cref<T> v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(v.vec, v.vec, 0xF1).m128_f32[0];
		#else
		return Len4Sq(v.x, v.y, v.z, v.w);
		#endif
	}

	// Largest/Smallest element
	template <typename T> inline float pr_vectorcall MinElement(v4_cref<T> const& v)
	{
		#if PR_MATHS_USE_INTRINSICS
		// min([x y z w], [y x w z]) = [x<y?x:y y<x?y:x z<w?z:w w<z?w:z] = [a a b b]
		// min([a a b b], [b b a a]) = [m m m m]
		auto abcd = v.vec;
		auto aabb = _mm_min_ps(abcd, _mm_shuffle_ps(abcd, abcd, _MM_SHUFFLE(1, 0, 3, 2)));
		auto mmmm = _mm_min_ps(aabb, _mm_shuffle_ps(aabb, aabb, _MM_SHUFFLE(2, 3, 0, 1)));
		return mmmm.m128_f32[0];
		#else
		return MinElement4<Vec4<T>, float>(v);
		#endif
	}
	template <typename T> inline float pr_vectorcall MaxElement(v4_cref<T> const& v)
	{
		#if PR_MATHS_USE_INTRINSICS
		// max([x y z w], [y x w z]) = [x>y?x:y y>x?y:x z>w?z:w w>z?w:z] = [a a b b]
		// max([a a b b], [b b a a]) = [m m m m]
		auto abcd = v.vec;
		auto aabb = _mm_max_ps(abcd, _mm_shuffle_ps(abcd, abcd, _MM_SHUFFLE(1, 0, 3, 2)));
		auto mmmm = _mm_max_ps(aabb, _mm_shuffle_ps(aabb, aabb, _MM_SHUFFLE(2, 3, 0, 1)));
		return mmmm.m128_f32[0];
		#else
		return MaxElement4<Vec4<T>, float>(v);
		#endif
	}

	// Normalise all components of 'v'
	template <typename T> inline Vec4<T> pr_vectorcall Normalise(v4_cref<T> v)
	{
		#if PR_MATHS_USE_DIRECTMATH
		return Vec4<T>{DirectX::XMVector4Normalize(v.vec)};
		#elif PR_MATHS_USE_INTRINSICS
		return Vec4<T>{_mm_div_ps(v.vec, _mm_sqrt_ps(_mm_dp_ps(v.vec, v.vec, 0xFF)))};
		#else
		return v / Length(v);
		#endif
	}

	// Square: v * v
	template <typename T> inline Vec4<T> pr_vectorcall Sqr(v4_cref<T> v)
	{
		#if PR_MATHS_USE_INTRINSICS
		return Vec4<T>{_mm_mul_ps(v.vec, v.vec)};
		#else
		return Vec4<T>{Sqr(v.x), Sqr(v.y), Sqr(v.z), Sqr(v.w)};
		#endif
	}

	// Dot product: a . b
	template <typename T> inline float pr_vectorcall Dot3(v4_cref<T> a, v4_cref<T> b)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto r = _mm_dp_ps(a.vec, b.vec, 0x71);
		return r.m128_f32[0];
		#else
		return a.x * b.x + a.y * b.y + a.z * b.z;
		#endif
	}
	template <typename T> inline float pr_vectorcall Dot4(v4_cref<T> a, v4_cref<T> b)
	{
		#if PR_MATHS_USE_INTRINSICS
		return _mm_dp_ps(a.vec, b.vec, 0xF1).m128_f32[0];
		#else
		return a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
		#endif
	}
	template <typename T> inline float pr_vectorcall Dot(v4_cref<T> a, v4_cref<T> b)
	{
		return Dot4(a,b);
	}

	// Cross product: a x b
	template <typename T> inline Vec4<T> pr_vectorcall Cross3(v4_cref<T> a, v4_cref<T> b)
	{
		#if PR_MATHS_USE_INTRINSICS
		return Vec4<T>{
			_mm_sub_ps(
			_mm_mul_ps(_mm_shuffle_ps(a.vec, a.vec, _MM_SHUFFLE(3, 0, 2, 1)), _mm_shuffle_ps(b.vec, b.vec, _MM_SHUFFLE(3, 1, 0, 2))), 
			_mm_mul_ps(_mm_shuffle_ps(a.vec, a.vec, _MM_SHUFFLE(3, 1, 0, 2)), _mm_shuffle_ps(b.vec, b.vec, _MM_SHUFFLE(3, 0, 2, 1))))};
		#else
		return Vec4<T>{a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x, 0};
		#endif
	}
	template <typename T> inline Vec4<T> pr_vectorcall Cross(v4_cref<T> a, v4_cref<T> b)
	{
		return Cross3(a,b);
	}

	// Triple product: a . b x c
	template <typename T> inline float pr_vectorcall Triple(v4_cref<T> a, v4_cref<T> b, v4_cref<T> c)
	{
		return Dot3(a, Cross3(b, c));
	}

	// Returns true if 'a' and 'b' parallel
	template <typename T> inline bool pr_vectorcall Parallel(v4_cref<T> v0, v4_cref<T> v1, float tol = maths::tinyf)
	{
		// '<=' to allow for 'tol' == 0.0
		return LengthSq(Cross3(v0, v1)) <= Sqr(tol);
	}

	// Returns a vector guaranteed not parallel to 'v'
	template <typename T> inline v4 pr_vectorcall CreateNotParallelTo(v4_cref<T> v)
	{
		bool x_aligned = Abs(v.x) > Abs(v.y) && Abs(v.x) > Abs(v.z);
		return Vec4<T>{static_cast<float>(!x_aligned), 0.0f, static_cast<float>(x_aligned), v.w};
	}

	// Returns a vector perpendicular to 'v' with the same length of 'v'
	template <typename T> inline Vec4<T> pr_vectorcall Perpendicular(v4_cref<T> v)
	{
		assert("Cannot make a perpendicular to a zero vector" && v != v4{});
		auto vec = Cross3(v, CreateNotParallelTo(v));
		vec *= Sqrt(LengthSq(v) / LengthSq(vec));
		return vec;
	}

	// Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular.
	// The length of the returned vector will be 'Length(vec)' or 'Length(previous)' (typically they'd be the same)
	// Either 'vec' or 'previous' can be zero, but not both.
	template <typename T> inline Vec4<T> pr_vectorcall Perpendicular(v4_cref<T> vec, v4_cref<T> previous)
	{
		if (vec == v4{})
		{
			// Both 'vec' and 'previous' cannot be zero
			assert("Cannot make a perpendicular to a zero vector" && previous != v4{});
			return previous;
		}
		if (Parallel(vec, previous)) // includes 'previous' == zero
		{
			// If 'previous' is parallel to 'vec', choose a new perpendicular
			return Perpendicular(vec);
		}

		// If 'previous' is still perpendicular, reuse it
		if (FEql(Dot3(vec, previous), 0))
		{
			return previous;
		}

		// Otherwise, make a perpendicular that is close to 'previous'
		return Normalise(Cross3(Cross3(vec, previous), vec));
	}

	// Returns a vector with the 'xyz' values permuted 'n' times. '0=xyzw, 1=yzxw, 2=zxyw'
	template <typename T> inline Vec4<T> pr_vectorcall Permute3(v4_cref<T> v, int n)
	{
		switch (n%3)
		{
		default: return v;
		case 1:  return Vec4<T>{v.y, v.z, v.x, v.w};
		case 2:  return Vec4<T>{v.z, v.x, v.y, v.w};
		}
	}

	// Returns a vector with the values permuted 'n' times. '0=xyzw, 1=yzwx, 2=zwxy, 3=wxyz'
	template <typename T> inline Vec4<T> pr_vectorcall Permute4(v4_cref<T> v, int n)
	{
		switch (n%4)
		{
		default: return v;
		case 1:  return Vec4<T>{v.y, v.z, v.w, v.x};
		case 2:  return Vec4<T>{v.z, v.w, v.x, v.y};
		case 3:  return Vec4<T>{v.w, v.x, v.y, v.z};
		}
	}

	// Returns a 3-bit bitmask of the octant the vector is in. 0=(-x,-y,-z), 1=(+x,-y,-z), 2=(-x,+y,-z), 3=(+x,+y,-z), 4=(-x,-y+z), 5=(+x,-y,+z), 6=(-x,+y,+z), 7=(+x,+y,+z)
	template <typename T> inline uint pr_vectorcall Octant(v4_cref<T> v)
	{
		return Octant(v.xyz);
	}

	// Return the component sum
	template <typename T> inline float pr_vectorcall ComponentSum(v4_cref<T> v)
	{
		#if PR_MATHS_USE_INTRINSICS
		auto sum = v.vec;
		sum = _mm_hadd_ps(sum, sum);
		sum = _mm_hadd_ps(sum, sum);
		float s; _mm_store_ss(&s, sum);
		return s;
		#else
		return v.x + v.y + v.z + v.w;
		#endif
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr::maths
{
	PRUnitTest(Vector4Tests)
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
		{// Operators
			auto a = v4{1, 2, 3, 4};
			auto b = v4{-4, -3, -2, -1};

			PR_CHECK(a + b, v4{-3, -1, +1, +3});
			PR_CHECK(a - b, v4{+5, +5, +5, +5});
			PR_CHECK(3 * a, v4{+3, +6, +9, +12});
			PR_CHECK(a % 2, v4{+1, +0, +1, +0});
			PR_CHECK(a/2.0f, v4{1.0f/2.0f, 2.0f/2.0f, 3.0f/2.0f, 4.0f/2.0f});
			PR_CHECK(1.0f/a, v4{1.0f/1.0f, 1.0f/2.0f, 1.0f/3.0f, 1.0f/4.0f});
		}
		{// Largest/Smallest element
			auto v1 = v4{1,-2,-3,4};
			PR_CHECK(MinElement(v1) == -3, true);
			PR_CHECK(MaxElement(v1) == +4, true);
			PR_CHECK(MinElementIndex(v1) == 2, true);
			PR_CHECK(MaxElementIndex(v1) == 3, true);
		}
		{// FEql
			auto a = v4{0, 0, -1, 0.5f};
			auto b = v4{0, 0, -1, 0.5f};
			
			// Equal if the relative difference is less than tiny compared to the maximum element in the matrix.
			a.x = a.y = 1.0e-5f;
			b.x = b.y = 1.1e-5f;
			PR_CHECK(FEql(MinElement(a), -1.0f), true);
			PR_CHECK(FEql(MinElement(b), -1.0f), true);
			PR_CHECK(FEql(MaxElement(a), +0.5f), true);
			PR_CHECK(FEql(MaxElement(b), +0.5f), true);
			PR_CHECK(FEql(a,b), true);
			
			a.z = a.w = 1.0e-5f;
			b.z = b.w = 1.1e-5f;
			PR_CHECK(FEql(MaxElement(a), 1.0e-5f), true);
			PR_CHECK(FEql(MaxElement(b), 1.1e-5f), true);
			PR_CHECK(FEql(a,b), false);
		}
		{// FEql
			v4 a(1,1,-1,-1);
			auto t2 = maths::tinyf * 2.0f;
			PR_CHECK(FEql(a, v4(1   ,1,-1,-1)), true);
			PR_CHECK(FEql(a, v4(1+t2,1,-1,-1)), false);
			PR_CHECK(FEql(v4(1e-20f,0,0,1).xyz, v4Zero.xyz), true);
			PR_CHECK(FEql(v4(1e-20f,0,0,1e-19f), v4Zero), true);
		}
		{
			v4 a(3,-1,2,-4);
			v4 b = {-2,-1,4,2};
			PR_CHECK(Max(a,b), v4(3,-1,4,2));
			PR_CHECK(Min(a,b), v4(-2,-1,2,-4));
		}
		{
			v4 a(3,-1,2,-4);
			PR_CHECK(MinElement(a), -4.0f);
			PR_CHECK(MaxElement(a), 3.0f);
		}
		{
			v4 a(3,-1,2,-4);
			PR_CHECK(LengthSq(a), a.x*a.x + a.y*a.y + a.z*a.z + a.w*a.w);
			PR_CHECK(Length(a), sqrt(LengthSq(a)));
		}
		{
			v4 a(3,-1,2,-4);
			v4 b = Normalise(a.w0());
			v4 c = Normalise(a);
			PR_CHECK(Length(b), 1.0f);
			PR_CHECK(Length(c), 1.0f);
			PR_CHECK(IsNormal(a), false);
			PR_CHECK(IsNormal(b), true);
			PR_CHECK(IsNormal(c), true);
		}
		{
			v4 a = {-2,  4,  2,  6};
			v4 b = { 3, -5,  2, -4};
			auto a2b = CPM(a, v4Origin);

			v4 c = Cross3(a,b);
			v4 d = a2b * b;
			PR_CHECK(FEql(c.xyz, d.xyz), true);
		}
		{
			v4 a = {-2,  4,  2,  6};
			v4 b = { 3, -5,  2, -4};
			PR_CHECK(Dot4(a,b), -46);
			PR_CHECK(Dot3(a,b), -22);
		}
		{ // ComponentSum
			v4 a = {1, 2, 3, 4};
			PR_CHECK(ComponentSum(a), 1+2+3+4);
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
#endif


	//// DirectXMath conversion functions
	//#if PR_MATHS_USE_DIRECTMATH
	//inline DirectX::XMVECTOR const& dxv4(v4 const& v) { assert(maths::is_aligned(&v)); return v.vec; }
	//inline DirectX::XMVECTOR&       dxv4(v4&       v) { assert(maths::is_aligned(&v)); return v.vec; }
	//#endif

