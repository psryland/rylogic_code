//***********************************************************************
// Interpolation
//  Copyright (c) Rylogic Ltd 2014
//***********************************************************************
#pragma once
#include "pr/common/interpolate.h"
#include "pr/maths/maths.h"
#include "pr/maths/spline.h"

namespace pr
{
	struct InterpolateVector
	{
		CubicCurve3 m_p;
		v4 m_x1;
		float m_interval;

		InterpolateVector(v4_cref x0, v4_cref v0, v4_cref x1, v4_cref v1, float interval)
			: m_p(x0 - x1, v0 * interval, v4::Zero(), v1 * interval, CurveType::Hermite)
			, m_x1(x1)
			, m_interval(interval)
		{
		}
		v4 Eval(float t) const
		{
			return m_x1 + m_p.Eval(t / m_interval);
		}
		v4 EvalDerivative(float t) const
		{
			return m_p.EvalDerivative(t / m_interval) / m_interval;
		}
		v4 EvalDerivative2(float t) const
		{
			return m_p.EvalDerivative2(t / m_interval) / m_interval;
		}
	};

	struct InterpolateRotation
	{
		// Notes:
		// - This is C1-continuous interpolation using a Hermite cubic. I.e, orientation changes smoothly
		//   through key frmes, and angular velocity has no step changes (but does have corners, angular acceleration
		//   isn't continuous)
		CubicCurve3 m_p;
		quat m_q1;
		float m_interval;
		
		InterpolateRotation(quat_cref q0, v4_cref w0, quat_cref q1, v4_cref w1, float interval)
			: m_p(LogMap(~q1 * q0), Tangent(~q1 * q0, Rotate(~q1, w0)) * interval, v4::Zero(), Tangent(quat::Identity(), Rotate(~q1, w1)) * interval, CurveType::Hermite)
			, m_q1(q1)
			, m_interval(interval)
		{
		}
		quat Eval(float t) const
		{
			return m_q1 * ExpMap(m_p.Eval(t / m_interval));
		}
		v4 EvalDerivative(float t) const
		{
			// To calculate 'W' from log_q and log_q`:
			// Say:
			//   u = log(q), r = |u| = angle / 2
			//   q = [qv, qw] = [(u/r) * sin(r), cos(r)] = [u*f(r), cos(r)]
			//     where f(r) = sin(r) / r
			// Also:
			//   u  == m_p.Eval(t)
			//   u` == m_p.EvalDerivative(t)
			//   r` == Dot(u`, u) / r  (where r > 0) (i.e. tangent amount in direction of u)
			//
			// Differentiating:
			//   f`(r) = (r*cos(r) - sin(r)) / r² (product rule)
			//   q` = [qv`, qw`] = [u`*qv + u*qv`*r`, -sin(r)*r`]
			// Also:
			//   q` = 0.5 x [w,0] x q  (quaternion derivative)
			//    => [w,0] = 2*(q` x ~q) = 2*(qw*qv` - qw`*qv - Cross(qv`, qv)) (expanded quaternion multiply)
			//
			// For small 'r' can use expansion for sin:
			//   f(r) = sin(r)/r ~= 1 - r²/6 +...
			//   f`(r) = -r/3 + ...
			// For really small 'r' use:
			//   W ~= 2 * u`  (comes from: if q = [u, 1] => q` ~= [u`, 0]
			constexpr float TinyAngle = 1e-8f;
			constexpr float SmallAngle = 1e-5f;

			auto u = m_p.Eval(t / m_interval);
			auto u_dot = m_p.EvalDerivative(t / m_interval) / m_interval;
			auto r = Length(u);

			auto omega = 2 * u_dot;
			if (r > TinyAngle)
			{
				// Derivative of angle
				auto r_dot = Dot(u, u_dot) / r;
				auto sin_r = Sin(r);
				auto cos_r = Cos(r);

				// Derivative of axis
				auto f = r > SmallAngle ? (sin_r / r) : (1.f - r * r / 6);
				auto f_dot = r > SmallAngle ? (r * cos_r - sin_r) / (r * r) : (-r / 3);

				// q
				auto qv = u * f; // vector part
				auto qw = cos_r; // scalar part

				// q`
				auto qw_dot = -sin_r * r_dot;
				auto qv_dot = u_dot * f + u * (f_dot * r_dot);

				// Vector part of (q` * ~q): vw = qw*qv` - qw`*qv - qv` x qv
				omega = 2.0f * (qw * qv_dot - qw_dot * qv - Cross(qv_dot, qv));
			}

			return Rotate(m_q1, omega);
		}

		// Returns the tangent vector in SO(3)
		static v4 Tangent(quat_cref q, v4_cref w)
		{
			auto q_p1 = ExpMap(-0.5f * w) * q;
			auto q_n1 = ExpMap(+0.5f * w) * q;
			auto tangent = (LogMap(q_n1) - LogMap(q_p1)) / 2;
			return tangent;
		}
	};

	// Interpolate<v4>
	template <> struct Interpolate<v4>
	{
		struct Point
		{
			template <typename F> v2 operator()(v2 lhs, v4_cref, F, F) const
			{
				return lhs;
			}
			template <typename F> v3 operator()(v3 lhs, v4_cref, F, F) const
			{
				return lhs;
			}
			template <typename F> v4 operator()(v4_cref lhs, v4_cref, F, F) const
			{
				return lhs;
			}
		};
		struct Linear
		{
			template <typename F> v2 operator()(v2 lhs, v2 rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
			template <typename F> v3 operator()(v3 lhs, v3 rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
			template <typename F> v4 operator()(v4_cref lhs, v4_cref rhs, F n, F N) const
			{
				if (N-- <= 1) return lhs;
				return Lerp(lhs, rhs, float(n)/N);
			}
		};
	};

}
