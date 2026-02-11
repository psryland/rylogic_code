//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/core/forward.h"
#include "pr/math_new/core/traits.h"
#include "pr/math_new/core/constants.h"

namespace pr::math
{
	template <ScalarType S>
	struct Quat
	{

	};

	
	// Test two quaternions for equivalence (i.e. do they represent the same orientation)
	template <QuaternionType Quat>
	inline bool FEqlOrientation(Quat const& lhs, Quat const& rhs, typename vector_traits<Quat>::element_t tol = Tiny<typename vector_traits<Quat>::element_t>())
	{
		using S = typename vector_traits<Quat>::element_t;
		return FEqlAbsolute(AxisAngle(rhs * ~lhs).angle, S(0), tol);
	}

	// Rotate a vector by a quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	constexpr Vec Rotate(Quat const& lhs, Vec rhs)
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

	// Logarithm map of quaternion to tangent space at identity
	template <VectorType Vec, QuaternionType Quat>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	Vec LogMap(Quat const& q)
	{
		using S = typename vector_traits<Vec>::element_t;

		// Quat = [u.Sin(A/2), Cos(A/2)]
		auto cos_half_ang = std::clamp<double>(vec(q).w, -1.0, +1.0); // [0, tau]
		auto sin_half_ang = std::sqrt(Square(vec(q).x) + Square(vec(q).y) + Square(vec(q).z)); // Don't use 'sqrt(1 - w*w)', it's not float noise accurate enough when w ~= +/-1
		auto ang_by_2 = std::acos(cos_half_ang); // By convention, log space uses Length = A/2
		return std::abs(sin_half_ang) > Tiny<S>()
			? Vec{ vec(q).x, vec(q).y, vec(q).z } * static_cast<S>(ang_by_2 / sin_half_ang)
			: Vec{ vec(q).x, vec(q).y, vec(q).z };
	}
	
	// Exponential map of tangent space at identity to quaternion
	template <QuaternionType Quat, VectorType Vec>
		requires (IsRank1<Vec>) &&
		requires () { std::floating_point<typename vector_traits<Vec>::element_t>; } &&
		requires () { vector_traits<Vec>::dimension >= 3; }
	Quat ExpMap(Vec v)
	{
		using S = typename vector_traits<Vec>::element_t;

		// Vec = (+/-)A * (-/+)u.
		auto ang_by_2 = Length(v); // By convention, log space uses Length = A/2
		auto cos_half_ang = std::cos(ang_by_2);
		auto sin_half_ang = std::sin(ang_by_2); // != sqrt(1 - cos_half_ang²) when ang_by_2 > tau/2
		auto s = ang_by_2 > Tiny<S>() ? static_cast<S>(sin_half_ang / ang_by_2) : S(1);
		return { vec(v).x * s, vec(v).y * s, vec(v).z * s, static_cast<S>(cos_half_ang) };
	}

	
	#if 0
	// Create a quaternion from the rotation part of a matrix
	constexpr quaternion RotationFrom(float4x4 const& mat)
	{
		assert("Only orientation matrices can be converted into quaternions" && IsOrthonormal(mat));
		constexpr auto Rsqrt = [](float x) { return 1.0f / Sqrt(x); };

		quaternion q = {};
		if (mat.x.x + mat.y.y + mat.z.z >= 0)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x + mat.y.y + mat.z.z);
			q.x = (mat.y.z - mat.z.y) * s;
			q.y = (mat.z.x - mat.x.z) * s;
			q.z = (mat.x.y - mat.y.x) * s;
			q.w = (0.25f / s);
		}
		else if (mat.x.x > mat.y.y && mat.x.x > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f + mat.x.x - mat.y.y - mat.z.z);
			q.x = (0.25f / s);
			q.y = (mat.x.y + mat.y.x) * s;
			q.z = (mat.z.x + mat.x.z) * s;
			q.w = (mat.y.z - mat.z.y) * s;
		}
		else if (mat.y.y > mat.z.z)
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x + mat.y.y - mat.z.z);
			q.x = (mat.x.y + mat.y.x) * s;
			q.y = (0.25f / s);
			q.z = (mat.y.z + mat.z.y) * s;
			q.w = (mat.z.x - mat.x.z) * s;
		}
		else
		{
			auto s = 0.5f * Rsqrt(1.f - mat.x.x - mat.y.y + mat.z.z);
			q.x = (mat.z.x + mat.x.z) * s;
			q.y = (mat.y.z + mat.z.y) * s;
			q.z = (0.25f / s);
			q.w = (mat.x.y - mat.y.x) * s;
		}
		return q;
	}
	#endif
	
	#if 0
	// Decompose a quaternion into axis (normalised) and angle (radians)
	template <QuaternionType Quat>
	auto AxisAngle(Quat const& quat)
	{
		// Trig:
		//' cos^2(x) = 0.5 * (1 + cos(2x))
		//' w == cos(x/2)
		//' w^2 == cos^2(x/2) == 0.5 * (1 + cos(x))
		//' 2w^2 - 1 == cos(x)
		using S = typename vector_traits<Quat>::element_t;
		struct AA { float3 axis; float angle; };

		assert(IsNormalised(quat) && "quaternion isn't normalised");

		// The axis is arbitrary for identity rotations
		if (LengthSq(quat) < TinySq<S>())
			return AA{ .axis = float3{0, 0, 1}, .angle = 0.0f };

		auto axis = Normalise(quat.xyz);
		auto cos_angle = std::clamp(S(2) * Pow<S>(vec(quat).w, 2) - S(1), -S(1), +S(1)); // The cosine of the angle of rotation about the axis

		return AA{ .axis = axis, .angle = std::acosf(cos_angle) };
	}
	#endif
}