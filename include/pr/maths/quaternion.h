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
#include "pr/maths/vector4.h"

namespace pr
{
	template <typename real = float> struct alignas(16) Quat
	{
		using Vec3   = Vec3<real>;
		using Vec4   = Vec4<real>;
		using Mat3x4 = Mat3x4<Vec4,real>;
		using Mat4x4 = Mat4x4<Vec4,real>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { real x,y,z,w; };
			struct { Vec4 xyzw; };
			struct { Vec3 xyz; };
			struct { real arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#endif
		};
		#pragma warning(pop)

		// Construct
		Quat() = default;
		Quat(real x_, real y_, real z_, real w_)
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
		explicit Quat(real x_)
		#if PR_MATHS_USE_INTRINSICS
			:vec(_mm_set_ps1(x_,x_,x_,x_))
		#else
			:x(x_)
			,y(x_)
			,z(x_)
			,w(x_)
		#endif
		{
			assert(maths::is_aligned(this));
		}
		template <typename T, typename = maths::enable_if_v4<T>> explicit Quat(T const& v)
			:Quat(x_as<real>(v), y_as<real>(v), z_as<real>(v), w_as<real>(v))
		{}
		template <typename T, typename = maths::enable_if_vec_cp<T>> explicit Quat(T const* v)
			:Quat(x_as<real>(v), y_as<real>(v), z_as<real>(v), w_as<real>(v))
		{}
		#if PR_MATHS_USE_INTRINSICS
		Quat(__m128 v)
			:vec(v)
		{
			assert(maths::is_aligned(this));
		}
		#endif

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

		// Create a quaternion from an axis and an angle
		Quat(Vec4 const& axis, real angle)
		{
			auto s = Sin(0.5f * angle);
			x = axis.x * s;
			y = axis.y * s;
			z = axis.z * s;
			w = pr::Cos(0.5f * angle);
		}

		// Create a quaternion from Euler angles. Order is roll, pitch, yaw
		Quat(real pitch, real yaw, real roll)
		{
			#if PR_MATHS_USE_DIRECTMATH
			vec = DirectX::XMQuaternionRotationRollPitchYaw(pitch, yaw, roll);
			#else
			// nicked from 'XMQuaternionRotationRollPitchYaw'
			float cos_p = pr::Cos(pitch * 0.5f), sin_p = pr::Sin(pitch * 0.5f);
			float cos_y = pr::Cos(yaw   * 0.5f), sin_y = pr::Sin(yaw   * 0.5f);
			float cos_r = pr::Cos(roll  * 0.5f), sin_r = pr::Sin(roll  * 0.5f);
			x = sin_p * cos_y * cos_r + cos_p * sin_y * sin_r;
			y = cos_p * sin_y * cos_r - sin_p * cos_y * sin_r;
			z = cos_p * cos_y * sin_r - sin_p * sin_y * cos_r;
			w = cos_p * cos_y * cos_r + sin_p * sin_y * sin_r;
			#endif
		}

		// Create a quaternion from a rotation matrix
		explicit Quat(Mat3x4 const& m)
		{
			auto trace = m.x.x + m.y.y + m.z.z;
			if (trace >= 0.0f)
			{
				auto s = 0.5f * Rsqrt1(1.0f + trace);
				x = (m.y.z - m.z.y) * s;
				y = (m.z.x - m.x.z) * s;
				z = (m.x.y - m.y.x) * s;
				w = (0.25f / s);
			}
			else if (m.x.x > m.y.y && m.x.x > m.z.z)
			{
				auto s = 0.5f * Rsqrt1(1.0f + m.x.x - m.y.y - m.z.z);
				x = (0.25f / s);
				y = (m.x.y + m.y.x) * s;
				z = (m.z.x + m.x.z) * s;
				w = (m.y.z - m.z.y) * s;
			}
			else if (m.y.y > m.z.z)
			{
				auto s = 0.5f * Rsqrt1(1.0f - m.x.x + m.y.y - m.z.z);
				x = (m.x.y + m.y.x) * s;
				y = (0.25f / s);
				z = (m.y.z + m.z.y) * s;
				w = (m.z.x - m.x.z) * s;
			}
			else
			{
				auto s = 0.5f * Rsqrt1(1.0f - m.x.x - m.y.y + m.z.z);
				x = (m.z.x + m.x.z) * s;
				y = (m.y.z + m.z.y) * s;
				z = (0.25f / s);
				w = (m.x.y - m.y.x) * s;
			}
		}

		// Create a quaternion from a rotation matrix
		explicit Quat(Mat4x4 const& m)
			#if PR_MATHS_USE_DIRECTMATH
			:vec(DirectX::XMQuaternionRotationMatrix(dxm4(m)))
			#else
			:this(m.rot)
			#endif
		{}

		// Construct a quaternion from two vectors represent start and end orientations
		Quat(Vec4 const& from, Vec4 const& to)
		{
			auto d = Dot3(from, to);
			auto axis = Cross3(from, to);
			auto s = Sqrt(Length3Sq(from) * Length3Sq(to)) + d;
			if (FEql(s, 0.0f))
			{
				// vectors are 180 degrees apart
				axis = Perpendicular3(to);
				s = 0.0f;
			}
			xyzw = Normalise4(Vec4(axis.x, axis.y, axis.z, s));
		}
	};

	using quat = Quat<>;
	static_assert(std::is_pod<quat>::value || _MSC_VER < 1900, "Should be a pod type");
	static_assert(std::alignment_of<quat>::value == 16, "Should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	using quat_cref = quat const;
	#else
	using quat_cref = quat const&;
	#endif

	// Define component accessors for pointer types
	inline float x_cp(quat_cref v) { return v.x; }
	inline float y_cp(quat_cref v) { return v.y; }
	inline float z_cp(quat_cref v) { return v.z; }
	inline float w_cp(quat_cref v) { return v.w; }

	#pragma region Traits
	namespace maths
	{
		// Specialise marker traits
		template <> struct is_vec<quat> :std::true_type
		{
			using elem_type = float;
			using cp_type = float;
			static int const dim = 4;
		};
	}
	#pragma endregion

	#pragma region Constants
	static quat const quatZero     = {0.0f, 0.0f, 0.0f, 0.0f};
	static quat const quatIdentity = {0.0f, 0.0f, 0.0f, 1.0f};
	#pragma endregion

	#pragma region Operators
	inline quat pr_vectorcall operator + (quat_cref q)
	{
		return q;
	}
	inline quat pr_vectorcall operator - (quat_cref q)
	{
		return quat(-q.x, -q.y, -q.z, -q.w); // Note: Not conjugate
	}
	inline quat& operator *= (quat& lhs, float rhs)
	{
		lhs.x *= rhs;
		lhs.y *= rhs;
		lhs.z *= rhs;
		lhs.w *= rhs;
		return lhs;
	}
	inline quat& operator /= (quat& lhs, float rhs)
	{
		assert("divide by zero" && rhs != 0);
		lhs.x /= rhs;
		lhs.y /= rhs;
		lhs.z /= rhs;
		lhs.w /= rhs;
		return lhs;
	}
	inline quat& pr_vectorcall operator += (quat& lhs, quat_cref rhs)
	{
		lhs.x += rhs.x;
		lhs.y += rhs.y;
		lhs.z += rhs.z;
		lhs.w += rhs.w;
		return lhs;
	}
	inline quat& pr_vectorcall operator -= (quat& lhs, quat_cref rhs)
	{
		lhs.x -= rhs.x;
		lhs.y -= rhs.y;
		lhs.z -= rhs.z;
		lhs.w -= rhs.w;
		return lhs;
	}
	inline quat pr_vectorcall operator + (quat_cref lhs, quat_cref rhs)
	{
		auto q = lhs;
		return q += rhs;
	}
	inline quat pr_vectorcall operator - (quat_cref lhs, quat_cref rhs)
	{
		auto q = lhs;
		return q -= rhs;
	}
	template <typename = void> inline quat& operator %= (quat&, float)
	{
		static_assert(false, "Quaternion modulus has no meaning");
	}
	template <typename = void> inline quat operator *= (quat&, quat const&)
	{
		static_assert(false,
			"Quaternion component multiply is not defined because it is incorrect semantically. "
			"To rotate a vector by a quaternion use: r = q * (v3,0) * Conjugate(q). "
			"Or use the 'Rotate()' functions. ");
	}
	template <typename = void> inline quat operator /= (quat&, quat const&)
	{
		static_assert(false,
			"Quaternion component multiply is not defined because it is incorrect semantically. "
			"To rotate a vector by a quaternion use: r = q * (v3,0) * Conjugate(q). "
			"Or use the 'Rotate()' functions. ");
	}
	template <typename = void> inline quat operator %= (quat&, quat const&)
	{
		static_assert(false,
			"Quaternion component multiply is not defined because it is incorrect semantically. "
			"To rotate a vector by a quaternion use: r = q * (v3,0) * Conjugate(q). "
			"Or use the 'Rotate()' functions. ");
	}

	// Quaternion multiply
	// Note about 'quat multiply' vs. 'r = q*v*conj(q)':
	// To rotate a vector or another quaternion, use the "sandwich product"
	// However, combining rotations is done using q1 * q2.
	// This is because:
	//  'r1 = a * v * conj(a)  - first rotation '
	//  'r2 = b * r1 * conj(b) - second rotation'
	//  'r2 = b * a * v * conj(a) * conj(b)     '
	//  'r2 = (b*a) * v * conj(b*a)             '
	inline quat pr_vectorcall operator * (quat_cref lhs, quat_cref rhs)
	{
		auto q = quat{};
		q.x = lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y;
		q.y = lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x;
		q.z = lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w;
		q.w = lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z;
		return q;
	}

	#pragma endregion

	#pragma region Functions

	// Return the conjugate of quaternion 'q'
	inline quat Conjugate_(quat const& q)
	{
		return quat(-q.x, -q.y, -q.z, q.w);
	}

	// Return the axis and angle from a quaternion
	inline void AxisAngle(quat const& q, v4& axis, float& angle)
	{
		assert("quaternion isn't normalised" && IsNormal4(q));
		float w = Clamp(q.w, -1.0f, 1.0f);
		float s = Sqrt(1.0f - w * w);
		angle = 2.0f * pr::ACos(w);
		axis = !FEql(s, 0.0f)
			? v4(q.x/s, q.y/s, q.z/s, 0.0f)
			: v4ZAxis; // axis arbitrary for angle = 0
	}

	// Spherically interpolate between quaternions
	inline quat Slerp(quat const& a, quat const& b, float frac)
	{
		if (frac <= 0.0f) return a;
		if (frac >= 1.0f) return b;

		// Calculate cosine
		quat abs_b;
		auto cos_angle = a.x*b.x + a.y*b.y + a.z*b.z + a.w*b.w;
		if (cos_angle >= 0) { abs_b =  b; }
		else                { abs_b = -b; cos_angle = -cos_angle; }

		// Calculate coefficients
		if (FGtr(1.0f, cos_angle, 0.05f))
		{
			// standard case (SLerp)
			float angle     = ACos(cos_angle);
			float sin_angle = Sin(angle);
			float scale0    = Sin((1.0f - frac) * angle);
			float scale1    = Sin((frac) * angle);
			return (scale0*a + scale1*abs_b) * (1.0f / sin_angle);
		}
		else // "a" and "b" quaternions are very close
		{
			return Normalise4(Lerp(a, abs_b, frac));
		}
	}

	// Rotate 'rotatee' by 'rotator'
	inline quat Rotate(quat const& rotator, quat const& rotatee)
	{
		assert("Non-unit quaternion used for rotation" && FEql(Length4Sq(rotator), 1.0f));
		return rotator * rotatee * Conjugate_(rotator);
	}

	// Rotate a vector by a quaternion
	// This is an optimised version of: r = q*v*conj(q) for when v.w == 0
	inline v4 Rotate(quat const& lhs, v4 const& rhs)
	{
		float xx = lhs.x*lhs.x, xy = lhs.x*lhs.y, xz = lhs.x*lhs.z, xw = lhs.x*lhs.w;
		float                   yy = lhs.y*lhs.y, yz = lhs.y*lhs.z, yw = lhs.y*lhs.w;
		float                                     zz = lhs.z*lhs.z, zw = lhs.z*lhs.w;
		float                                                       ww = lhs.w*lhs.w;

		v4 r;
		r.x =   ww*rhs.x + 2*yw*rhs.z - 2*zw*rhs.y +   xx*rhs.x + 2*xy*rhs.y + 2*xz*rhs.z -   zz*rhs.x - yy*rhs.x;
		r.y = 2*xy*rhs.x +   yy*rhs.y + 2*yz*rhs.z + 2*zw*rhs.x -   zz*rhs.y +   ww*rhs.y - 2*xw*rhs.z - xx*rhs.y;
		r.z = 2*xz*rhs.x + 2*yz*rhs.y +   zz*rhs.z - 2*yw*rhs.x -   yy*rhs.z + 2*xw*rhs.y -   xx*rhs.z + ww*rhs.z;
		r.w = rhs.w;
		return r;
	}
	//inline v3 Rotate(quat const& lhs, v3 const& rhs)
	//{
	//	return Rotate(lhs, v4(rhs, 0.0f)).xyz;
	//}
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_quaternion)
		{
			{ // Create
				auto p = DegreesToRadians(  43.0f);
				auto y = DegreesToRadians(  10.0f);
				auto r = DegreesToRadians(-245.0f);

				auto q0 = quat(p,y,r);
				quat q1(DirectX::XMQuaternionRotationRollPitchYaw(p,y,r));
				PR_CHECK(FEql4(q0, q1), true);
			}
		}
	}
}
#endif

	//// DirectXMath conversion functions
	//#if PR_MATHS_USE_DIRECTMATH
	//inline DirectX::XMVECTOR const& dxv4(quat const& quat) { return quat.vec; }
	//inline DirectX::XMVECTOR&       dxv4(quat&       quat) { return quat.vec; }
	//#endif