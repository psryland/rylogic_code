//*********************************************
// Maths Library
//  Copyright (c) Rylogic Ltd 2002
//*********************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/vector2.h"
#include "pr/maths/vector3.h"
#include "pr/maths/vector4.h"
#include "pr/maths/stat.h"
//#include "pr/maths/matrix3x4.h"
//#include "pr/maths/matrix4x4.h" // make mat depend on quat

namespace pr
{
	template <Scalar S, typename A, typename B>
	struct Quat
	{
		static_assert(std::is_floating_point_v<S>);
		enum
		{
			IntrinsicF = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, float>,
			IntrinsicD = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, double>,
			NoIntrinsic = PR_MATHS_USE_INTRINSICS == 0,
		};
		#if PR_MATHS_USE_INTRINSICS
		using intrinsic_t =
			std::conditional_t<IntrinsicF, __m128,
			std::conditional_t<IntrinsicD, __m256d,
			void>>;
		#else
		using intrinsic_t = void;
		#endif

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union alignas(4 * sizeof(S))
		{
			struct { S x, y, z, w; };
			struct { Vec4<S, void> xyzw; };
			struct { Vec3<S, void> xyz; };
			struct { S arr[4]; };
			#if PR_MATHS_USE_INTRINSICS
			intrinsic_t vec;
			#endif
		};
		#pragma warning(pop)

		// Construct
		Quat() = default;
		constexpr Quat(S x_, S y_, S z_, S s_)
			:x(x_)
			,y(y_)
			,z(z_)
			,w(s_)
		{}
		constexpr explicit Quat(Vec4_cref<S, void> vec)
			:xyzw(vec)
		{}
		constexpr explicit Quat(S const* v)
			:Quat(v[0], v[1], v[2], v[3])
		{}
		template <maths::Vector4 V> constexpr explicit Quat(V const& v)
			:Quat(maths::comp<0>(v), maths::comp<1>(v), maths::comp<2>(v), maths::comp<3>(v))
		{}
		#if PR_MATHS_USE_INTRINSICS
		Quat(intrinsic_t v)
			:vec(v)
		{
			assert(maths::is_aligned(this));
		}
		Quat& operator =(intrinsic_t v)
		{
			assert(maths::is_aligned(this));
			vec = v;
			return *this;
		}
		#endif

		// Reinterpret as a different quaternion type
		template <typename C, typename D> explicit operator Quat<S, C, D> const& () const
		{
			return reinterpret_cast<Quat<S, C, D> const&>(*this);
		}
		template <typename C, typename D> explicit operator Quat<S, C, D>& ()
		{
			return reinterpret_cast<Quat<S, C, D>&>(*this);
		}
		operator Quat<S, void, void> const& () const
		{
			return reinterpret_cast<Quat<S, void, void> const&>(*this);
		}
		operator Quat<S, void, void>& ()
		{
			return reinterpret_cast<Quat<S, void, void>&>(*this);
		}

		// Array access
		S const& operator [] (int i) const
		{
			assert("index out of range" && i >= 0 && i < _countof(arr));
			return arr[i];
		}
		S& operator [] (int i)
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
		Quat(Vec4_cref<S, void> axis, S angle)
		{
			auto s = Sin(S(0.5) * angle);
			x = axis.x * s;
			y = axis.y * s;
			z = axis.z * s;
			w = Cos(S(0.5) * angle);
		}

		// Create a quaternion from Euler angles. Order is roll, pitch, yaw
		Quat(S pitch, S yaw, S roll)
		{
			S cos_p = Cos(pitch * S(0.5)), sin_p = Sin(pitch * S(0.5));
			S cos_y = Cos(yaw   * S(0.5)), sin_y = Sin(yaw   * S(0.5));
			S cos_r = Cos(roll  * S(0.5)), sin_r = Sin(roll  * S(0.5));
			x = sin_p * cos_y * cos_r + cos_p * sin_y * sin_r;
			y = cos_p * sin_y * cos_r - sin_p * cos_y * sin_r;
			z = cos_p * cos_y * sin_r - sin_p * sin_y * cos_r;
			w = cos_p * cos_y * cos_r + sin_p * sin_y * sin_r;
		}

		// Create a quaternion from a rotation matrix
		explicit Quat(Mat3x4_cref<S,A,B> m);

		// Create a quaternion from a rotation matrix
		explicit Quat(Mat4x4_cref<S,A,B> m)
			:Quat(m.rot)
		{}

		// Construct a quaternion from two vectors representing start and end orientations
		Quat(Vec4_cref<S, void> from, Vec4_cref<S, void> to)
		{
			assert(from.w == 0 && to.w == 0);
			auto d = Dot(from, to);
			auto axis = Cross3(from, to);
			auto s = Sqrt(LengthSq(from) * LengthSq(to)) + d;
			if (FEql(s, S(0)))
			{
				// vectors are 180 degrees apart
				axis = Perpendicular(to);
				s = S(0);
			}
			xyzw = Normalise(Vec4<S, void>{axis.x, axis.y, axis.z, s});
		}

		// Get the axis component of the quaternion (normalised)
		Vec4<S, void> Axis() const
		{
			// The axis is arbitrary for identity rotations
			return Normalise(xyzw.w0(), Vec4<S, void>{0, 0, 1, 0});
		}

		// Return the angle of rotation about 'Axis()'
		S Angle() const
		{
			return ACos(CosAngle());
		}

		// Return the cosine of the angle of rotation about 'Axis()'
		S CosAngle() const
		{
			assert("quaternion isn't normalised" && IsNormal(*this));

			// Trig:
			//' cos^2(x) = 0.5 * (1 + cos(2x))
			//' w == cos(x/2)
			//' w^2 == cos^2(x/2) == 0.5 * (1 + cos(x))
			//' 2w^2 - 1 == cos(x)
			return Clamp(S(2.0) * Sqr(w) - S(1.0), S(-1.0), S(+1.0));
		}

		// Return the sine of the angle of rotation about 'Axis()'
		S SinAngle() const
		{
			// Trig:
			//' sin^2(x) + cos^2(x) == 1
			//' sin^2(x) == 1 - cos^2(x)
			//' sin(x) == sqrt(1 - cos^2(x))
			return Sqrt(S(1.0) - Sqr(CosAngle()));
		}
		
		// Basic constants
		static constexpr Quat Zero()
		{
			return {0, 0, 0, 0};
		}
		static constexpr Quat Identity()
		{
			return {0, 0, 0, 1};
		}

		// Construct random
		template <typename Rng = std::default_random_engine> static Quat Random(Rng& rng, Vec4_cref<S, void> axis, S min_angle, S max_angle)
		{
			// Create a random quaternion rotation
			std::uniform_real_distribution<S> dist(min_angle, max_angle);
			return Quat(axis, dist(rng));
		}
		template <typename Rng = std::default_random_engine> static Quat Random(Rng& rng, S min_angle, S max_angle)
		{
			// Create a random quaternion rotation
			std::uniform_real_distribution<S> dist(min_angle, max_angle);
			return Quat(Vec4<S,void>::RandomN(rng, 0), dist(rng));
		}
		template <typename Rng = std::default_random_engine> static Quat Random(Rng& rng)
		{
			// Create a random quaternion rotation
			std::uniform_real_distribution<S> dist(S(0), constants<S>::tau);
			return Quat(Vec4<S,void>::RandomN(rng, 0), dist(rng));
		}

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
		template <typename C> friend Quat<S, A, C> pr_vectorcall operator * (Quat_cref<S, B, C> lhs, Quat_cref<S, A, B> rhs)
		{
			auto q = Quat<S, A, C>{};
			q.x = lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y;
			q.y = lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x;
			q.z = lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w;
			q.w = lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z;
			return q;
		}

		// Quaternion rotate. i.e. 'r = q*v*conj(q)' the "sandwich product"
		// This is not really correct, since it's actually two multiplies
		// but it makes the code look nicer.
		friend Vec4<S, B> pr_vectorcall operator * (Quat_cref<S, A, B> lhs, Vec4_cref<S, A> rhs)
		{
			return Rotate(lhs, rhs);
		}

		#pragma endregion
	};
	#define PR_QUAT_CHECKS(scalar)\
	static_assert(sizeof(Quat<scalar, void, void>) == 4 * sizeof(scalar), "Quat<"#scalar"> has the wrong size");\
	static_assert(maths::Vector4<Quat<scalar, void, void>>, "Quat<"#scalar"> in not a Vector4");\
	static_assert(std::is_trivially_copyable_v<Quat<scalar, void, void>>, "Must be a pod type");\
	static_assert(std::alignment_of_v<Quat<scalar, void, void>> == 4 * sizeof(scalar), "Quat<"#scalar"> is not aligned correctly");
	PR_QUAT_CHECKS(float);
	PR_QUAT_CHECKS(double);
	#undef PR_QUAT_CHECKS

	// Quaternion FEql. Note: q == -q
	template <Scalar S, typename A, typename B> inline bool pr_vectorcall FEqlRelative(Quat_cref<S,A,B> lhs, Quat_cref<S,A,B> rhs, S tol)
	{
		return
			FEqlRelative(lhs.xyzw, +rhs.xyzw, tol) ||
			FEqlRelative(lhs.xyzw, -rhs.xyzw, tol);
	}
	template <Scalar S, typename A, typename B> inline bool pr_vectorcall FEql(Quat_cref<S,A,B> lhs, Quat_cref<S,A,B> rhs)
	{
		return FEqlRelative(lhs, rhs, maths::tiny<S>);
	}

	// Component add
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall CompAdd(Quat_cref<S,A,B> lhs, Quat_cref<S,A,B> rhs)
	{
		return Quat<S,A,B>{lhs.xyzw + rhs.xyzw};
	}

	// Component multiply
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall CompMul(Quat_cref<S,A,B> lhs, S rhs)
	{
		return Quat<S,A,B>{lhs.xyzw * rhs};
	}
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall CompMul(Quat_cref<S,A,B> lhs, Quat_cref<S,A,B> rhs)
	{
		return Quat<S,A,B>{lhs.xyzw * rhs.xyzw};
	}

	// Length squared
	template <Scalar S, typename A, typename B> inline S pr_vectorcall LengthSq(Quat_cref<S,A,B> q)
	{
		return LengthSq(q.xyzw);
	}

	// Normalise the quaternion 'q'
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall Normalise(Quat_cref<S,A,B> q)
	{
		return Quat<S,A,B>{Normalise(q.xyzw)};
	}
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall Normalise(Quat_cref<S,A,B> q, Quat_cref<S,A,B> def)
	{
		return Quat<S,A,B>{Normalise(q.xyzw, def.xyzw)};
	}

	// Return the cosine of *twice* the angle between two quaternions (i.e. the dot product)
	template <Scalar S, typename A, typename B> inline S pr_vectorcall CosAngle2(Quat_cref<S,A,B> a, Quat_cref<S,A,B> b)
	{
		// The relative orientation between 'a' and 'b' is given by z = 'a * conj(b)'
		// where operator * is a quaternion multiply. The 'w' component of a quaternion
		// multiply is given by: q.w = a.w*b.w - a.x*b.x - a.y*b.y - a.z*b.z;
		// which is the same as q.w = Dot4(a,b) since conjugate negates the x,y,z
		// components of 'b'. Remember: q.w = Cos(theta/2)
		return Dot4(a.xyzw, b.xyzw);
	}

	// Return the angle between two quaternions (in radians)
	template <Scalar S, typename A, typename B> inline S pr_vectorcall Angle(Quat_cref<S,A,B> a, Quat_cref<S,A,B> b)
	{
		return S(0.5) * ACos(CosAngle2(a,b));
	}

	// Logarithm map of quaternion to tangent space at identity. Converts a quaternion into a length-scaled direction, where length is the angle of rotation
	template <Scalar S, typename A, typename B> inline Vec4<S, void> pr_vectorcall LogMap(Quat_cref<S,A,B> q)
	{
		// Quat = [u.Sin(A/2), Cos(A/2)]
		auto cos_half_ang = Clamp<double>(q.w, -1.0, +1.0); // [0, tau]
		auto sin_half_ang = Length(q.xyz); // Don't use 'sqrt(1 - w*w)', it's not float noise accurate enough when w ~= +/-1
		auto ang_by_2 = ACos(cos_half_ang);
		auto s = Abs(sin_half_ang) > maths::tinyd ? static_cast<float>(2.0 * ang_by_2 / sin_half_ang) : 2.0f;
		return q.xyzw.w0() * s;
	}

	// Exponential map of tangent space at identity to quaternion. Converts a length-scaled direction to a quaternion.
	template <Scalar S> inline Quat<S, void, void> pr_vectorcall ExpMap(Vec4_cref<S,void> v)
	{
		// Vec = (+/-)A * (-/+)u.
		auto ang_by_2 = 0.5 * Length(v);
		auto cos_half_ang = (float)Cos(ang_by_2);
		auto sin_half_ang = (float)Sqrt(1 - cos_half_ang * cos_half_ang);
		auto s = ang_by_2 > maths::tinyd ? static_cast<float>(0.5 * sin_half_ang / ang_by_2) : 0.5f;
		return { v.x * s, v.y * s, v.z * s, cos_half_ang };
	}

	// Scale the rotation 'q' by 'frac'. Returns a rotation about the same axis but with angle scaled by 'frac'
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall Scale(Quat_cref<S,A,B> q, S frac)
	{
		assert("quaternion isn't normalised" && IsNormal4(q));

		// Trig:
		//' sin^2(x) + cos^2(x) == 1
		//' s == sqrt(1 - w^2) == sqrt(1 - cos^2(x/2))
		//' s^2 == 1 - cos^2(x/2) == sin^2(x/2)
		//' s == sin(x/2)
		auto w = Clamp(q.w, S(-1.0), S(+1.0)); // = cos(x/2)
		auto s = Sqrt(S(+1.0) - Sqr(w));       // = sin(x/2)
		auto a = frac * ACos(w);               // = scaled half angle
		auto sin_ha = Sin(a);
		auto cos_ha = Cos(a);
		return Quat<S,A,B>{
			q.x * sin_ha / s,
			q.y * sin_ha / s,
			q.z * sin_ha / s,
			cos_ha};
	}

	// Return the axis and angle from a quaternion
	template <Scalar S, typename A, typename B> inline void pr_vectorcall AxisAngle(Quat_cref<S,A,B> q, Vec4<S,void>& axis, S& angle)
	{
		assert("quaternion isn't normalised" && IsNormal(q));

		// Trig:
		//' sin^2(x) + cos^2(x) == 1
		//' s == sqrt(1 - w^2) == sqrt(1 - cos^2(x/2))
		//' s^2 == 1 - cos^2(x/2) == sin^2(x/2)
		//' s == sin(x/2)
		auto w = Clamp(q.w, S(-1.0), S(+1.0));
		auto s = Sqrt(S(+1.0) - Sqr(w));
		angle = S(2.0) * ACos(w);
		axis = Abs(s) > maths::tinyf
			? Vec4<S,void>(q.x/s, q.y/s, q.z/s, S(0))
			: Vec4<S,void>{S(0), S(0), S(0), S(0)}; // axis is (0,0,0) when angle == 1
	}

	// Return possible Euler angles for the quaternion 'q'
	template <Scalar S, typename A, typename B> inline Vec4<S, void> EulerAngles(Quat_cref<S,A,B> q)
	{
		// From Wikipedia
		double q0 = q.w, q1 = q.x, q2 = q.y, q3 = q.z;
		return Vec4<S,void>{
			s_cast<S>(atan2(2.0 * (q0*q1 + q2*q3), 1.0 - 2.0 * (q1*q1 + q2*q2))),
			s_cast<S>(asin (2.0 * (q0*q2 - q3*q1))),
			s_cast<S>(atan2(2.0 * (q0*q3 + q1*q2), 1.0 - 2.0 * (q2*q2 + q3*q3))),
			S(0)};
	}

	// Spherically interpolate between quaternions
	template <Scalar S, typename A, typename B> inline Quat<S,A,B> pr_vectorcall Slerp(Quat_cref<S,A,B> a, Quat_cref<S,A,B> b, S frac)
	{
		if (frac == S(0)) return a;
		if (frac == S(1)) return b;

		// Flip 'b' so that both quaternions are in the same hemisphere (since: q == -q)
		auto cos_angle = CosAngle(a,b);
		Quat<S,A,B> b_ = cos_angle >= 0 ? b : -b;
		cos_angle = Abs(cos_angle);

		// Calculate coefficients
		if (cos_angle < S(0.95))
		{
			auto angle     = ACos(cos_angle);
			auto scale0    = Sin((S(1) - frac) * angle);
			auto scale1    = Sin((frac       ) * angle);
			auto sin_angle = Sin(angle);
			return Quat<S,A,B>{(scale0*a.xyzw + scale1*b_.xyzw) / sin_angle};
		}
		else // "a" and "b" quaternions are very close
		{
			return Normalise(Quat<S,A,B>{Lerp(a.xyzw, b_.xyzw, frac)});
		}
	}

	// Rotate a vector by a quaternion
	template <Scalar S, typename A, typename B> inline Vec4<S,B> pr_vectorcall Rotate(Quat_cref<S,A,B> lhs, Vec4_cref<S,A> rhs)
	{
		// This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'
		S xx = lhs.x*lhs.x, xy = lhs.x*lhs.y, xz = lhs.x*lhs.z, xw = lhs.x*lhs.w;
		S                   yy = lhs.y*lhs.y, yz = lhs.y*lhs.z, yw = lhs.y*lhs.w;
		S                                     zz = lhs.z*lhs.z, zw = lhs.z*lhs.w;
		S                                                       ww = lhs.w*lhs.w;

		Vec4<S,B> r;
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
		template <Scalar S, typename A, typename B> 
		struct Avr<Quat<S,A,B>, S>
		{
			Avr<Vec4<S, void>, S> m_avr;

			int Count() const
			{
				return m_avr.Count();
			}
			void Reset()
			{
				m_avr.Reset();
			}
			Quat<S,A,B> Mean() const
			{
				return Normalise(Quat<S,A,B>{m_avr.Mean()});
			}
			void Add(Quat_cref<S,A,B> q)
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
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(QuaternionTests, float, double)
	{
		using S = T;
		using quat_t = Quat<S, void, void>;
		std::default_random_engine rng(1U);

		{ // Create from m3x4
			std::uniform_real_distribution<float> rng_angle(-constants<float>::tau, +constants<float>::tau);
			for (int i = 0; i != 100; ++i)
			{
				auto ang = rng_angle(rng);
				auto axis = Vec4<float,void>::RandomN(rng, 0);
				auto mat = m3x4::Rotation(axis, ang);
				auto q = Quat<float, void, void>(mat);
				auto v0 = Vec4<float, void>::RandomN(rng, 0);
				auto r0 = mat * v0;
				auto r1 = Rotate(q, v0);
				PR_CHECK(FEql(r0, r1), true);
			}
		}
		{ // Average
			auto ideal_mean = quat_t(Normalise(Vec4<S,void>(1, 1, 1, 0)), S(0.5));

			std::uniform_int_distribution<int> rng_bool(0, 1);
			std::uniform_real_distribution<S> rng_angle(ideal_mean.Angle() - S(0.2), ideal_mean.Angle() + S(0.2));

			Avr<quat_t, S> avr;
			for (int i = 0; i != 1000; ++i)
			{
				auto axis = Normalise(ideal_mean.Axis() + Vec4<S, void>::Random(rng, S(0.0), S(0.2), S(0)));
				auto angle = rng_angle(rng);
				quat_t q(axis, angle);
				avr.Add(rng_bool(rng) ? q : -q);
			}

			auto actual_mean = avr.Mean();
			PR_CHECK(FEqlRelative(ideal_mean, actual_mean, S(0.01)), true);
		}
		{// LogMap <-> ExpMap
			{
				auto q0 = quat::Random(rng);
				auto v0 = LogMap(q0);
				auto q1 = ExpMap(v0);
				PR_EXPECT(FEql(q0, q1));
			}
			// Edge cases
			{
				auto q0 = quat{ -2.09713704e-08f, -0.00148352725f, -6.48572168e-11f, -0.999998927f };
				q0 = Normalise(q0);

				auto v0 = LogMap(q0);
				auto q1 = ExpMap(v0);
				auto angular_error = Angle(q0, q1);
				PR_EXPECT(Abs(angular_error) < 0.001f);
			}
		}
	}

}
#endif
