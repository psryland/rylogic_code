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
#include "pr/maths/matrix3x4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/stat.h"

namespace pr
{
	template <typename A, typename B>
	struct alignas(16) Quat
	{
		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union
		{
			struct { float x,y,z,w; };
			struct { Vec4<void> xyzw; };
			struct { Vec3<void> xyz; };
			struct { float arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			__m128 vec;
			#elif PR_MATHS_USE_DIRECTMATH
			DirectX::XMVECTOR vec;
			#else
			struct { Vec4<void> vec; };
			#endif
		};
		#pragma warning(pop)

		// Construct
		Quat() = default;
		Quat(float x_, float y_, float z_, float w_)
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
		explicit Quat(float x_)
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
		explicit Quat(v4_cref<> vec)
			:xyzw(vec)
		{
			assert(maths::is_aligned(this));
		}
		template <typename V4, typename = maths::enable_if_v4<V4>> explicit Quat(V4 const& v)
			:Quat(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		template <typename C, typename = maths::enable_if_vec_cp<C>> explicit Quat(C const* v)
			:Quat(x_as<float>(v), y_as<float>(v), z_as<float>(v), w_as<float>(v))
		{}
		#if PR_MATHS_USE_INTRINSICS
		Quat(__m128 v)
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

		// Operators
		Quat operator +() const
		{
			return *this;
		}
		Quat operator -() const
		{
			return Quat(-x, -y, -z, -w); // Note: Not conjugate
		}
		Quat operator ~() const
		{
			return Quat(-x, -y, -z, w);
		}

		// Create a quaternion from an axis and an angle
		Quat(v4_cref<> axis, float angle)
		{
			auto s = Sin(0.5f * angle);
			x = axis.x * s;
			y = axis.y * s;
			z = axis.z * s;
			w = pr::Cos(0.5f * angle);
		}

		// Create a quaternion from Euler angles. Order is roll, pitch, yaw
		Quat(float pitch, float yaw, float roll)
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
		explicit Quat(m3_cref<A,B> m)
		{
			assert("Only orientation matrices can be converted into quaternions" && IsOrthonormal(m));
			if (m.x.x + m.y.y + m.z.z >= 0)
			{
				auto s = 0.5f * Rsqrt1(1.0f + m.x.x + m.y.y + m.z.z);
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
		explicit Quat(m4_cref<A,B> m)
			#if PR_MATHS_USE_DIRECTMATH
			:vec(DirectX::XMQuaternionRotationMatrix(m))
			#else
			:Quat(m.rot)
			#endif
		{}

		// Construct a quaternion from two vectors represent start and end orientations
		Quat(v4_cref<> from, v4_cref<> to)
		{
			auto d = Dot3(from, to);
			auto axis = Cross3(from, to);
			auto s = Sqrt(Length3Sq(from) * Length3Sq(to)) + d;
			if (FEql(s, 0.0f))
			{
				// vectors are 180 degrees apart
				axis = Perpendicular(to);
				s = 0.0f;
			}
			xyzw = Normalise4(Vec4<void>{axis.x, axis.y, axis.z, s});
		}

		// Get the axis component of the quaternion (normalised)
		Vec4<void> Axis() const
		{
			// The axis is arbitrary for identity rotations
			return Normalise3(xyzw.w0(), v4ZAxis);
		}

		// Return the angle of rotation about 'Axis()'
		float Angle() const
		{
			return ACos(CosAngle());
		}

		// Return the cosine of the angle of rotation about 'Axis()'
		float CosAngle() const
		{
			assert("quaternion isn't normalised" && IsNormal4(*this));

			// Trig:
			//' cos²(x) = 0.5 * (1 + cos(2x))
			//' w == cos(x/2)
			//' w² == cos²(x/2) == 0.5 * (1 + cos(x))
			//' 2w² - 1 == cos(x)
			return Clamp(2.0f * Sqr(w) - 1.0f, -1.0f, +1.0f);
		}

		// Return the sine of the angle of rotation about 'Axis()'
		float SinAngle() const
		{
			// Trig:
			//' sin²(x) + cos²(x) == 1
			//' sin²(x) == 1 - cos²(x)
			//' sin(x) == sqrt(1 - cos²(x))
			return Sqrt(1.0f - Sqr(CosAngle()));
		}
	};
	static_assert(maths::is_vec4<Quat<void,void>>::value, "");
	static_assert(std::is_pod<Quat<void,void>>::value, "Should be a pod type");
	static_assert(std::alignment_of<Quat<void,void>>::value == 16, "Should have 16 byte alignment");
	#if PR_MATHS_USE_INTRINSICS && !defined(_M_IX86)
	template <typename A = void, typename B = void> using quat_cref = Quat<A,B> const;
	#else
	template <typename A = void, typename B = void> using quat_cref = Quat<A,B> const&;
	#endif

	// Define component accessors for pointer types
	template <typename A, typename B> inline float x_cp(quat_cref<A,B> v) { return v.x; }
	template <typename A, typename B> inline float y_cp(quat_cref<A,B> v) { return v.y; }
	template <typename A, typename B> inline float z_cp(quat_cref<A,B> v) { return v.z; }
	template <typename A, typename B> inline float w_cp(quat_cref<A,B> v) { return v.w; }

	#pragma region Operators

	// Quaternion multiply
	// Note about 'quat multiply' vs. 'r = q*v*conj(q)':
	// To rotate a vector or another quaternion, use the "sandwich product"
	// However, combining rotations is done using q1 * q2.
	// This is because:
	//'  r1 = a * v * conj(a)  - first rotation 
	//'  r2 = b * r1 * conj(b) - second rotation
	//'  r2 = b * a * v * conj(a) * conj(b)     
	//'  r2 = (b*a) * v * conj(b*a)             
	template <typename A, typename B, typename C> inline Quat<A,C> pr_vectorcall operator * (quat_cref<B,C> lhs, quat_cref<A,B> rhs)
	{
		auto q = Quat<A,C>{};
		q.x = lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y;
		q.y = lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x;
		q.z = lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w;
		q.w = lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z;
		return q;
	}

	// Quaternion rotate. i.e. 'r = q*v*conj(q)' the "sandwich product"
	// This is not really correct, since it's actually two multiplies
	// but it makes the code look nicer.
	template <typename A, typename B> inline Vec4<B> pr_vectorcall operator * (quat_cref<A,B> lhs, v4_cref<A> rhs)
	{
		return Rotate(lhs, rhs);
	}

	#pragma endregion

	#pragma region Functions

	// Quaternion FEql. Note: q == -q
	template <typename A, typename B> inline bool pr_vectorcall FEqlRelative(quat_cref<A,B> lhs, quat_cref<A,B> rhs, float tol)
	{
		return
			FEqlRelative(lhs.xyzw, +rhs.xyzw, tol) ||
			FEqlRelative(lhs.xyzw, -rhs.xyzw, tol);
	}
	template <typename A, typename B> inline bool pr_vectorcall FEql(quat_cref<A,B> lhs, quat_cref<A,B> rhs)
	{
		return FEqlRelative(lhs, rhs, maths::tiny);
	}

	// Component add
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall CompAdd(quat_cref<A,B> lhs, quat_cref<A,B> rhs)
	{
		return Quat<A,B>{lhs.xyzw + rhs.xyzw};
	}

	// Component multiply
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall CompMul(quat_cref<A,B> lhs, float rhs)
	{
		return Quat<A,B>{lhs.xyzw * rhs};
	}
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall CompMul(quat_cref<A,B> lhs, quat_cref<A,B> rhs)
	{
		return Quat<A,B>{lhs.xyzw * rhs.xyzw};
	}

	// Length squared
	template <typename A, typename B> inline float pr_vectorcall LengthSq(quat_cref<A,B> q)
	{
		return Length4Sq(q.xyzw);
	}

	// Normalise the quaternion 'q'
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall Normalise(quat_cref<A,B> q)
	{
		return Quat<A,B>{Normalise4(q.xyzw)};
	}
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall Normalise(quat_cref<A,B> q, quat_cref<A,B> def)
	{
		return Quat<A,B>{Normalise4(q.xyzw, def.xyzw)};
	}

	// Return the cosine of the angle between two quaternions (i.e. the dot product)
	template <typename A, typename B> inline float pr_vectorcall CosAngle(quat_cref<A,B> a, quat_cref<A,B> b)
	{
		return Dot4(a.xyzw, b.xyzw);
	}

	// Return the angle between two quaternions (in radians)
	template <typename A, typename B> inline float pr_vectorcall Angle(quat_cref<A,B> a, quat_cref<A,B> b)
	{
		return ACos(CosAngle(a,b));
	}

	// Scale the rotation 'q' by 'frac'. Returns a rotation about the same axis but with angle scaled by 'frac'
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall Scale(quat_cref<A,B> q, float frac)
	{
		assert("quaternion isn't normalised" && IsNormal4(q));

		// Trig:
		//' sin²(x) + cos²(x) == 1
		//' s == sqrt(1 - w²) == sqrt(1 - cos²(x/2))
		//' s² == 1 - cos²(x/2) == sin²(x/2)
		//' s == sin(x/2)
		auto w = Clamp(q.w, -1.0f, 1.0f); // = cos(x/2)
		auto s = Sqrt(1.0f - Sqr(w));     // = sin(x/2)
		auto a = frac * ACos(w);          // = scaled half angle
		auto sin_ha = Sin(a);
		auto cos_ha = Cos(a);
		return Quat<A,B>{
			q.x * sin_ha / s,
			q.y * sin_ha / s,
			q.z * sin_ha / s,
			cos_ha};
	}

	// Return the axis and angle from a quaternion
	template <typename A, typename B> inline void pr_vectorcall AxisAngle(quat_cref<A,B> q, Vec4<void>& axis, float& angle)
	{
		assert("quaternion isn't normalised" && IsNormal4(q));

		// Trig:
		//' sin²(x) + cos²(x) == 1
		//' s == sqrt(1 - w²) == sqrt(1 - cos²(x/2))
		//' s² == 1 - cos²(x/2) == sin²(x/2)
		//' s == sin(x/2)
		auto w = Clamp(q.w, -1.0f, 1.0f);
		auto s = Sqrt(1.0f - Sqr(w));
		angle = 2.0f * ACos(w);
		axis = !FEql(s, 0.0f)
			? v4(q.x/s, q.y/s, q.z/s, 0.0f)
			: v4ZAxis; // axis arbitrary for angle = 0
	}

	// Return possible Euler angles for the quaternion 'q'
	template <typename A, typename B> inline Vec4<void> EulerAngles(quat_cref<A,B> q)
	{
		// From Wikipedia
		double q0 = q.w, q1 = q.x, q2 = q.y, q3 = q.z;
		return Vec4<void>{
			(float)atan2(2.0 * (q0*q1 + q2*q3), 1.0 - 2.0 * (q1*q1 + q2*q2)),
			(float)asin (2.0 * (q0*q2 - q3*q1)),
			(float)atan2(2.0 * (q0*q3 + q1*q2), 1.0 - 2.0 * (q2*q2 + q3*q3)),
			0};
	}

	// Spherically interpolate between quaternions
	template <typename A, typename B> inline Quat<A,B> pr_vectorcall Slerp(quat_cref<A,B> a, quat_cref<A,B> b, float frac)
	{
		if (frac == 0.0f) return a;
		if (frac == 1.0f) return b;

		// Flip 'b' so that both quaternions are in the same hemisphere (since: q == -q)
		auto cos_angle = CosAngle(a,b);
		Quat<A,B> b_ = cos_angle >= 0 ? b : -b;
		cos_angle = Abs(cos_angle);

		// Calculate coefficients
		if (cos_angle < 0.95f)
		{
			auto angle     = ACos(cos_angle);
			auto scale0    = Sin((1.0f - frac) * angle);
			auto scale1    = Sin((frac       ) * angle);
			auto sin_angle = Sin(angle);
			return Quat<A,B>{(scale0*a.xyzw + scale1*b_.xyzw) / sin_angle};
		}
		else // "a" and "b" quaternions are very close
		{
			return Normalise(Quat<A,B>{Lerp(a.xyzw, b_.xyzw, frac)});
		}
	}

	// Rotate a vector by a quaternion
	// This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'
	template <typename A, typename B> inline Vec4<B> pr_vectorcall Rotate(quat_cref<A,B> lhs, v4_cref<A> rhs)
	{
		float xx = lhs.x*lhs.x, xy = lhs.x*lhs.y, xz = lhs.x*lhs.z, xw = lhs.x*lhs.w;
		float                   yy = lhs.y*lhs.y, yz = lhs.y*lhs.z, yw = lhs.y*lhs.w;
		float                                     zz = lhs.z*lhs.z, zw = lhs.z*lhs.w;
		float                                                       ww = lhs.w*lhs.w;

		Vec4<B> r;
		r.x =   ww*rhs.x + 2*yw*rhs.z - 2*zw*rhs.y +   xx*rhs.x + 2*xy*rhs.y + 2*xz*rhs.z -   zz*rhs.x - yy*rhs.x;
		r.y = 2*xy*rhs.x +   yy*rhs.y + 2*yz*rhs.z + 2*zw*rhs.x -   zz*rhs.y +   ww*rhs.y - 2*xw*rhs.z - xx*rhs.y;
		r.z = 2*xz*rhs.x + 2*yz*rhs.y +   zz*rhs.z - 2*yw*rhs.x -   yy*rhs.z + 2*xw*rhs.y -   xx*rhs.z + ww*rhs.z;
		r.w = rhs.w;
		return r;
	}

	namespace maths
	{
		// Specialise 'Avr' for quaternions
		// Finds the average rotation.
		template <typename A, typename B> 
		struct Avr<Quat<A,B>, float>
		{
			Avr<Vec4<void>, float> m_avr;

			uint Count() const { return m_avr.Count(); }
			void Reset() { m_avr.Reset(); }
			Quat<A,B> Mean() const
			{
				return Normalise(Quat<A,B>{m_avr.Mean()});
			}
			void Add(quat_cref<A,B> q)
			{
				// Nicked from Unity3D
				// Note: this only really works if all the quaternions are relatively close together.
				// For two quaternions, prefer 'Slerp'
				// This method is based on a simplified procedure described in this document:
				// http://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/20070017872_2007014421.pdf

				// Ensure the quaternions are in the same hemisphere (since q == -q)
				m_avr.Add(Dot4(q.xyzw, Mean().xyzw) >= 0 ? q.xyzw : -q.xyzw);
			}
		};
	}

	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include <directxmath.h>
namespace pr::maths
{
	PRUnitTest(QuaternionTests)
	{
		std::default_random_engine rng(1U);

		{ // Create
			#if PR_MATHS_USE_INTRINSICS
			auto p = DegreesToRadians(  43.0f);
			auto y = DegreesToRadians(  10.0f);
			auto r = DegreesToRadians(-245.0f);

			auto q0 = quat(p,y,r);
			quat q1(DirectX::XMQuaternionRotationRollPitchYaw(p,y,r));
			PR_CHECK(FEql4(q0, q1), true);
			#endif
		}
		{ // Create from m3x4
			std::uniform_real_distribution<float> rng_angle(-maths::tauf, +maths::tauf);
			for (int i = 0; i != 100; ++i)
			{
				auto ang = rng_angle(rng);
				auto axis = Random3N(rng, 0);
				auto mat = m3x4::Rotation(axis, ang);
				auto q = quat(mat);
				auto v0 = Random3N(rng, 0);
				auto r0 = mat * v0;
				auto r1 = Rotate(q, v0);
				PR_CHECK(FEql4(r0, r1), true);
			}
		}
		{ // Average
			auto ideal_mean = quat(Normalise3(v4(1,1,1,0)), 0.5f);

			std::uniform_int_distribution<int> rng_bool(0, 1);
			std::uniform_real_distribution<float> rng_angle(ideal_mean.Angle() - 0.2f, ideal_mean.Angle() + 0.2f);

			Avr<quat, float> avr;
			for (int i = 0; i != 1000; ++i)
			{
				auto axis = Normalise3(ideal_mean.Axis() + Random3(rng, 0.0f, 0.2f, 0.0f));
				auto angle = rng_angle(rng);
				quat q(axis, angle);
				avr.Add(rng_bool(rng) ? q : -q);
			}
				
			auto actual_mean = avr.Mean();
			PR_CHECK(FEqlRelative(ideal_mean, actual_mean, 0.01f), true);
		}
	}
}
#endif

	//// DirectXMath conversion functions
	//#if PR_MATHS_USE_DIRECTMATH
	//inline DirectX::XMVECTOR const& dxv4(quat const& quat) { return quat.vec; }
	//inline DirectX::XMVECTOR&       dxv4(quat&       quat) { return quat.vec; }
	//#endif