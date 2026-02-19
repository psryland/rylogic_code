//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"
#include "pr/math_new/types/vector3.h"
#include "pr/math_new/types/vector4.h"

namespace pr::math
{
	template <ScalarType S>
	struct Quat
	{
		enum
		{
			IntrinsicF = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, float>,
			IntrinsicD = PR_MATHS_USE_INTRINSICS && std::is_same_v<S, double>,
			NoIntrinsic = PR_MATHS_USE_INTRINSICS == 0,
		};
		using intrinsic_t =
			std::conditional_t<IntrinsicF, __m128,
			std::conditional_t<IntrinsicD, __m256d,
			std::byte[4*sizeof(S)]
			>>;

		#pragma warning(push)
		#pragma warning(disable:4201) // nameless struct
		union alignas(4 * sizeof(S))
		{
			struct { S x, y, z, w; };
			struct { Vec2<S> xy, zw; };
			struct { Vec3<S> xyz; };
			struct { Vec4<S> xyzw; };
			struct { S arr[4]; };
			intrinsic_t vec;
		};
		#pragma warning(pop)

		// Construct
		Quat() = default;
		constexpr explicit Quat(S x_)
			: x(x_)
			, y(x_)
			, z(x_)
			, w(x_)
		{
		}
		constexpr Quat(S x_, S y_, S z_, S w_)
			: x(x_)
			, y(y_)
			, z(z_)
			, w(w_)
		{}
		constexpr explicit Quat(std::ranges::random_access_range auto&& v)
			:Quat(v[0], v[1], v[2], v[3])
		{}
		constexpr explicit Quat(VectorTypeN<S, 4> auto v)
			:Quat(vec(v).x, vec(v).y, vec(v).z, vec(v).w)
		{}
		constexpr Quat(intrinsic_t vec_) requires (!NoIntrinsic)
			:vec(vec_)
		{}

		// Create a quaternion from a rotation matrix
		explicit Quat(Mat3x4<S> const& m);

		// Create a quaternion from an axis and an angle
		Quat(Vec3<S> axis, S angle)
		{
			auto s = Sin(S(0.5) * angle);
			x = axis.x * s;
			y = axis.y * s;
			z = axis.z * s;
			w = Cos(S(0.5) * angle);
		}
		Quat(Vec4<S> axis, S angle)
			:Quat(axis.xyz, angle)
		{
			pr_assert(axis.w == 0);
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

		// Construct a quaternion from two vectors representing start and end orientations
		Quat(Vec3<S> from, Vec3<S> to)
		{
			auto d = Dot(from, to);
			auto s = Sqrt(LengthSq(from) * LengthSq(to)) + d;
			auto axis = Cross(from, to);

			// Vectors are aligned, 180 degrees apart, or one is zero
			if (FEql(s, S(0)))
			{
				s = S(0);
				axis =
					LengthSq(from) > tiny<S> ? Perpendicular(from) :
					LengthSq(to) > tiny<S> ? Perpendicular(to) :
					Vec4<S>::ZAxis();
			}

			xyzw = Normalise(Vec4<S>{axis.x, axis.y, axis.z, s});
		}
		Quat(Vec4<S> from, Vec4<S> to)
			:Quat(from.xyz, to.xyz)
		{
			pr_assert(from.w == 0 && to.w == 0);
		}

		// Get the axis component of the quaternion (normalised)
		Vec4<S> Axis() const
		{
			// The axis is arbitrary for identity rotations
			return Normalise(xyzw.w0(), Vec4<S>{0, 0, 1, 0});
		}

		// Return the angle of rotation about 'Axis()'
		S Angle() const
		{
			return Acos(CosAngle());
		}

		// Return the cosine of the angle of rotation about 'Axis()'
		S CosAngle() const
		{
			// Trig:
			//' w == cos(θ/2)
			//' cos²(θ/2) = 0.5 * (1 + cos(θ))
			//' w² == cos²(θ/2) == 0.5 * (1 + cos(θ))
			//' cos(θ) = 2w² - 1

			// This is always the smallest arc
			return Clamp(S(2) * Sqr(w) - LengthSq(xyzw), -S(1), +S(1));
		}

		// Return the sine of the angle of rotation about 'Axis()'
		S SinAngle() const
		{
			// Trig:
			//'  w == cos(θ/2)
			//'  sin(θ) = 2 * sin(θ/2) * cos(θ/2)

			// The sign is determined by the sign of w (which represents cos(θ/2))
			auto sin_half_angle = Length(xyz);
			return S(2) * sin_half_angle * w;
		}

		// Array access
		constexpr S operator [] (int i) const
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}
		constexpr S& operator [] (int i)
		{
			pr_assert(i >= 0 && i < _countof(arr) && "index out of range");
			return arr[i];
		}

		// Constants
		static constexpr Quat Zero()
		{
			return Quat(S(0), S(0), S(0), S(0));
		}
		static constexpr Quat Identity()
		{
			return Quat(S(0), S(0), S(0), S(1));
		}
	};

	#define PR_MATH_DEFINE_TYPE(scalar)\
	template <> struct vector_traits<Quat<scalar>>\
		: quaternion_traits_base<scalar>\
		, vector_access_member<Quat<scalar>, scalar, 4>\
	{};\
	\
	static_assert(QuaternionType<Quat<scalar>>, "Quat<"#scalar"> is not a valid quaternion type");\
	static_assert(sizeof(Quat<scalar>) == 4*sizeof(scalar), "Quat<"#scalar"> has the wrong size");\
	static_assert(std::is_trivially_copyable_v<Quat<scalar>>, "Quat<"#scalar"> is not trivially copyable");

	PR_MATH_DEFINE_TYPE(float);
	PR_MATH_DEFINE_TYPE(double);
	#undef PR_MATH_DEFINE_TYPE

	// Decompose a quaternion into axis (normalised) and angle (radians)
	template <QuaternionType Quat>
	inline auto pr_vectorcall AxisAngle(Quat q)
	{
		using S = typename vector_traits<Quat>::element_t;
		struct R { Vec4<S> axis; S angle; };
		pr_assert("quaternion isn't normalised" && IsNormal(q));

		// Use atan2 for the angle — well-conditioned everywhere, unlike acos
		auto sin_half_angle = Length(q.xyz);
		auto angle = S(2) * Atan2(sin_half_angle, Abs(q.w));

		// Normalise the xyz part directly (avoids sqrt(1-w²) cancellation)
		auto axis = sin_half_angle > tiny<S>
			? Vec4<S>(q.x / sin_half_angle, q.y / sin_half_angle, q.z / sin_half_angle, S(0))
			: Vec4<S>(S(0), S(0), S(1), S(0)); // arbitrary axis for identity

		return R{ axis, angle };
	}

	// Test two quaternions for equivalence (i.e. do they represent the same orientation)
	template <QuaternionType Quat>
	inline bool FEqlOrientation(Quat lhs, Quat rhs, typename vector_traits<Quat>::element_t tol = Tiny<typename vector_traits<Quat>::element_t>())
	{
		using S = typename vector_traits<Quat>::element_t;
		return FEqlAbsolute(AxisAngle(rhs * ~lhs).angle, S(0), tol);
	}

	// Quaternion FEql. Note: q == -q
	template <QuaternionType Quat>
	constexpr bool pr_vectorcall FEqlRelative(Quat lhs, Quat rhs, typename vector_traits<Quat>::element_t tol)
	{
		return
			FEqlRelative(lhs.xyzw, +rhs.xyzw, tol) ||
			FEqlRelative(lhs.xyzw, -rhs.xyzw, tol);
	}
	template <QuaternionType Quat>
	constexpr bool pr_vectorcall FEql(Quat lhs, Quat rhs)
	{
		using vt = vector_traits<Quat>;
		using S = typename vt::element_t;

		return FEqlRelative(lhs, rhs, tiny<S>);
	}

	// Returns the value of 'cos(theta / 2)', where 'theta' is the angle between 'a' and 'b'
	template <QuaternionType Quat>
	constexpr typename vector_traits<Quat>::element_t pr_vectorcall CosHalfAngle(Quat a, Quat b)
	{
		// The relative orientation between 'a' and 'b' is given by z = 'a * conj(b)'
		// where operator * is a quaternion multiply. The 'w' component of a quaternion
		// multiply is given by: q.w = a.w*b.w - a.x*b.x - a.y*b.y - a.z*b.z;
		// which is the same as q.w = Dot4(a,b) since conjugate negates the x,y,z
		// components of 'b'. Remember: q.w = Cos(theta/2)
		return Dot(a.xyzw, b.xyzw);
	}

	// Return possible Euler angles for the quaternion 'q'
	template <QuaternionType Quat>
	inline auto EulerAngles(Quat q)
	{
		using vt = vector_traits<Quat>;
		using S = typename vt::element_t;

		// From Wikipedia
		auto q0 = vec(q).w;
		auto q1 = vec(q).x;
		auto q2 = vec(q).y;
		auto q3 = vec(q).z;

		return {
			.pitch = s_cast<S>(std::atan2(2.0 * (q0*q1 + q2*q3), 1.0 - 2.0 * (q1*q1 + q2*q2))),
			.roll = s_cast<S>(std::asin(2.0 * (q0*q2 - q3*q1))),
			.yaw = s_cast<S>(atan2(2.0 * (q0*q3 + q1*q2), 1.0 - 2.0 * (q2*q2 + q3*q3))),
		};
	}

	// Rotate a vector by a quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	constexpr Vec Rotate(Quat lhs, Vec rhs)
	{
		using vt = vector_traits<Vec>;

		// This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'
		auto xx = vec(lhs).x * vec(lhs).x, xy = vec(lhs).x * vec(lhs).y, xz = vec(lhs).x * vec(lhs).z, xw = vec(lhs).x * vec(lhs).w;
		auto yy = vec(lhs).y * vec(lhs).y, yz = vec(lhs).y * vec(lhs).z, yw = vec(lhs).y * vec(lhs).w;
		auto zz = vec(lhs).z * vec(lhs).z, zw = vec(lhs).z * vec(lhs).w;
		auto ww = vec(lhs).w * vec(lhs).w;

		Vec res = {};
		vec(res).x = ww * vec(rhs).x + 2 * yw * vec(rhs).z - 2 * zw * vec(rhs).y + xx * vec(rhs).x + 2 * xy * vec(rhs).y + 2 * xz * vec(rhs).z - zz * vec(rhs).x - yy * vec(rhs).x;
		vec(res).y = 2 * xy * vec(rhs).x + yy * vec(rhs).y + 2 * yz * vec(rhs).z + 2 * zw * vec(rhs).x - zz * vec(rhs).y + ww * vec(rhs).y - 2 * xw * vec(rhs).z - xx * vec(rhs).y;
		vec(res).z = 2 * xz * vec(rhs).x + 2 * yz * vec(rhs).y + zz * vec(rhs).z - 2 * yw * vec(rhs).x - yy * vec(rhs).z + 2 * xw * vec(rhs).y - xx * vec(rhs).z + ww * vec(rhs).z;
		if constexpr (vt::dimension > 3) vec(res).w = vec(rhs).w;
		return res;
	}

	// Scale the rotation 'q' by 'frac'. Returns a rotation about the same axis but with angle scaled by 'frac'
	template <QuaternionType Quat>
	inline Quat pr_vectorcall Scale(Quat q, typename vector_traits<Quat>::element_t frac)
	{
		using S = typename vector_traits<Quat>::element_t;
		pr_assert("quaternion isn't normalised" && IsNormal(q));

		// Use atan2 for the half-angle — well-conditioned everywhere, unlike acos
		auto sin_half_angle = Length(q.xyz);
		auto half_angle = Atan2(sin_half_angle, Abs(q.w));
		auto scaled_half_angle = frac * half_angle;
		auto sin_ha = Sin(scaled_half_angle);
		auto cos_ha = Cos(scaled_half_angle);

		// Normalise the xyz part directly (avoids sqrt(1-w²) cancellation near identity)
		if (sin_half_angle > tiny<S>)
		{
			auto scale = sin_ha / sin_half_angle;
			return Quat{q.x * scale, q.y * scale, q.z * scale, cos_ha};
		}
		else
		{
			// Identity quaternion — arbitrary axis
			return Quat{S(0), S(0), S(0), cos_ha};
		}
	}

	// Spherically interpolate between quaternions
	template <QuaternionType Quat>
	inline Quat pr_vectorcall Slerp(Quat a, Quat b, typename vector_traits<Quat>::element_t frac)
	{
		using vt = vector_traits<Quat>;
		using S = typename vt::element_t;

		if (frac == S(0)) return a;
		if (frac == S(1)) return b;

		// Flip 'b' so that both quaternions are in the same hemisphere (since: q == -q)
		auto cos_angle = CosAngle(a,b);
		auto b_ = cos_angle >= 0 ? b : -b;
		cos_angle = Abs(cos_angle);

		// Calculate coefficients
		if (cos_angle < S(0.95))
		{
			auto angle     = std::acos(cos_angle);
			auto scale0    = std::sin((S(1) - frac) * angle);
			auto scale1    = std::sin((frac       ) * angle);
			auto sin_angle = std::sin(angle);
			auto lerped    = (scale0 * a.xyzw + scale1 * b_.xyzw) / sin_angle;
			return Quat{ lerped };
		}
		else // "a" and "b" quaternions are very close
		{
			auto lerped = Lerp(a.xyzw, b_.xyzw, frac);
			return Normalise(Quat{ lerped });
		}
	}

	// Logarithm map of quaternion to tangent space at identity
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	inline Vec pr_vectorcall LogMap(Quat q)
	{
		using S = typename vector_traits<Quat>::element_t;
		auto xyz0 = Vec{ vec(q).x, vec(q).y, vec(q).z };

		// Quat = [u·sin(A/2), cos(A/2)]
		auto sin_half_ang = Length(xyz0);
		auto ang_by_2 = Atan2(sin_half_ang, Abs(vec(q).w)); // well-conditioned everywhere

		// Scale xyz by (half_angle / sin_half_angle) to get axis * half_angle
		return sin_half_ang > tiny<S>
			? xyz0 * static_cast<S>(ang_by_2 / sin_half_ang)
			: xyz0;
	}
	
	// Exponential map of tangent space at identity to quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	inline Quat pr_vectorcall ExpMap(Vec v)
	{
		using S = typename vector_traits<Vec>::element_t;

		// Vec = (+/-)A * (-/+)u.
		auto ang_by_2 = Length(v); // By convention, log space uses Length = A/2
		auto cos_half_ang = Cos(ang_by_2);
		auto sin_half_ang = Sin(ang_by_2); // != sqrt(1 - cos_half_ang²) when ang_by_2 > tau/2
		auto s = ang_by_2 > Tiny<S>() ? static_cast<S>(sin_half_ang / ang_by_2) : S(1);
		return { vec(v).x * s, vec(v).y * s, vec(v).z * s, static_cast<S>(cos_half_ang) };
	}

	// Evaluates 'ori' after 'time' for a constant angular velocity and angular acceleration
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	inline Quat pr_vectorcall RotationAt(float time, Quat ori, Vec avel, Vec aacc)
	{
		using vt = vector_traits<Quat>;
		using S = typename vt::element_t;

		// Orientation can be computed analytically if angular velocity
		// and angular acceleration are parallel or angular acceleration is zero.
		if (LengthSq(Cross(avel, aacc)) < tiny<S>)
		{
			auto w = avel + aacc * time;
			return ExpMap(S(0.5) * w * time) * ori;
		}
		else
		{
			// Otherwise, use the SPIRAL(6) algorithm. 6th order accurate for moderate 'time_s'

			// 3-point Gauss-Legendre nodes for 6th order accuracy
			constexpr S root15f = S(3.87298334620741688518);
			constexpr S c1 = S(0.5) - root15f / S(10);
			constexpr S c2 = S(0.5);
			constexpr S c3 = S(0.5) + root15f / S(10);

			// Evaluate instantaneous angular velocity at nodes
			auto w0 = avel + aacc * c1 * time;
			auto w1 = avel + aacc * c2 * time;
			auto w2 = avel + aacc * c3 * time;

			auto u0 = ExpMap<Vec>(S(0.5) * w0 * time / S(3));
			auto u1 = ExpMap<Vec>(S(0.5) * w1 * time / S(3));
			auto u2 = ExpMap<Vec>(S(0.5) * w2 * time / S(3));

			return u2 * u1 * u0 * ori; // needs testing
		}
	}

	// Create a quaternion from the rotation part of a matrix
	template <QuaternionType Quat>
	constexpr Quat RotationFrom(Mat3x4<typename vector_traits<Quat>::element_t> const& mat)
	{
		using vt = vector_traits<Quat>;
		using S = typename vt::element_t;
		assert(IsOrthonormal(mat) && "Only orientation matrices can be converted into quaternions");

		constexpr auto Rsqrt = [](float x) { return 1.0f / Sqrt(x); };

		Quat q = {};
		if (mat.x.x + mat.y.y + mat.z.z >= 0)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x + mat.y.y + mat.z.z);
			vec(q).x = (mat.y.z - mat.z.y) * s;
			vec(q).y = (mat.z.x - mat.x.z) * s;
			vec(q).z = (mat.x.y - mat.y.x) * s;
			vec(q).w = (0.25f / s);
		}
		else if (mat.x.x > mat.y.y && mat.x.x > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x - mat.y.y - mat.z.z);
			vec(q).x = (0.25f / s);
			vec(q).y = (mat.x.y + mat.y.x) * s;
			vec(q).z = (mat.z.x + mat.x.z) * s;
			vec(q).w = (mat.y.z - mat.z.y) * s;
		}
		else if (mat.y.y > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x + mat.y.y - mat.z.z);
			vec(q).x = (mat.x.y + mat.y.x) * s;
			vec(q).y = (0.25f / s);
			vec(q).z = (mat.y.z + mat.z.y) * s;
			vec(q).w = (mat.z.x - mat.x.z) * s;
		}
		else
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x - mat.y.y + mat.z.z);
			vec(q).x = (mat.z.x + mat.x.z) * s;
			vec(q).y = (mat.y.z + mat.z.y) * s;
			vec(q).z = (0.25f / s);
			vec(q).w = (mat.x.y - mat.y.x) * s;
		}
		return q;
	}










	#if 0
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

	// Returns the smallest angle between two quaternions (in radians, [0, tau/2])
	template <Scalar S, typename A, typename B> inline S pr_vectorcall Angle(Quat_cref<S,A,B> a, Quat_cref<S,A,B> b)
	{
		// q.w = Cos(theta/2)
		// Note: cos(A) = 2 * cos²(A/2) - 1
		//  and: acos(A) = 0.5 * acos(2A² - 1), for A in [0, tau/2]
		// Using the 'acos(2A² - 1)' form always returns the smallest angle
		auto cos_half_ang = CosHalfAngle(a, b);
		return 
			cos_half_ang > 1.0f - tiny<S> ? S(0) :
			cos_half_ang > 0 ? S(2) * Acos(Clamp(cos_half_ang, -S(1), +S(1))) : // better precision
			Acos(Clamp(S(2) * Sqr(cos_half_ang) - S(1), -S(1), +S(1)));
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

	#endif
}