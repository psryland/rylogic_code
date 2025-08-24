//***********************************************************************
// Interpolation
//  Copyright (c) Rylogic Ltd 2014
//***********************************************************************
#pragma once
#include "pr/common/interpolate.h"
#include "pr/maths/maths.h"

namespace pr
{
	// Third order, vector valued, polynomial
	template <typename S>
	struct Polynomial3
	{
		using Vec4 = Vec4<S, void>;
		Vec4 m_a, m_b, m_c, m_d;
		double m_interval;

		Polynomial3()
			: m_a()
			, m_b()
			, m_c()
			, m_d()
			, m_interval()
		{}
		Polynomial3(Vec4 const& x0, Vec4 const& v0, Vec4 const& x1, Vec4 const& v1, S interval)
		{
			assert(interval > maths::tinyf);
			m_interval = interval;

			// These are the normal coefficients, but dividing by T3 can create large floats so instead, defer the divides to evaluation
			//'   A = -2 * (x1 - x0) / T³ + (v0 + v1) / T²;
			//'   B = +3 * (x1 - x0) / T² - (2 * v0 + v1) / T;
			//'   C = v0;
			//'   D = x0;
			m_a = S(-2) * (x1 - x0) / interval + (v0 + v1);        //' Let A := A * interval²
			m_b = S(+3) * (x1 - x0) / interval - (S(2) * v0 + v1); //' Let B := B * interval
			m_c = v0;
			m_d = x0;
		}
		Vec4 Eval(S t) const
		{
			auto t1 = std::clamp<S>(t, S(0), S(m_interval));
			auto f1 = t1 / S(m_interval);
			auto f2 = f1 * f1;
			
			// This is the normal polynomial evaluation, but we've deferred the divides
			//  P(t) = A * T³ + B * T² + C * T + D;
			
			// From above, A includes a factor of 'interval²', so A*T³ => A*T³/interval² => A * T * F²
			// Similarly, B includes a factor of 'interval', so B*T² => B*T²/interval => B * T * F
			auto p = m_a * t1 * f2 + m_b * t1 * f1 + m_c * t1 + m_d;
			return p;
		}
		Vec4 dEval(S T) const
		{
			auto t1 = std::clamp<S>(T, S(0), S(m_interval));
			auto f1 = t1 / S(m_interval);
			auto f2 = f1 * f1;
			
			// This is the normal first derivative evaluation, but we've deferred the divides
			//  dP(t)/dt = 3 * A * T² + 2 * B * T + C;

			// From above, A includes a factor of 'interval²', so A*T² => A*T²/Interval² => A * F²
			// Similarly, B includes a factor of 'interval', so B*T => B*T/Interval => B * F
			auto dp = S(3) * m_a * f2 + S(2) * m_b * f1 + m_c;
			return dp;
		}
	};

	// Position interpolator
	struct InertializeV
	{
		// Notes:
		//  - Interpolates position for an object between 'pos0,vel0' at T = 0 and 'pos1,vel1' at T = Interval
		Polynomial3<float> p;
		InertializeV()
			: p()
		{}
		InertializeV(v4_cref pos0, v4_cref vel0, v4_cref pos1, v4_cref vel1, float interval)
			: p(pos0, vel0, pos1, vel1, interval)
		{}
		v4 Eval(float T) const
		{
			return p.Eval(T);
		}
		v4 dEval(float T) const
		{
			return p.dEval(T);
		}
	};

	// Rotation interpolator
	struct InertializeQ
	{
		// Notes:
		//  - Interpolates orientation for an object between 'Q0,W0' at T = 0 and 'Q1,W1' at T = Interval
		Polynomial3<float> p;

		InertializeQ()
			: p()
		{}
		InertializeQ(quat_cref q0, v4_cref w0, quat_cref q1, v4_cref w1, float interval)
			: p(LogMap(q0), w0, LogMap(q1), w1, interval)
		{
			assert(IsNormal(q0) && "q0 is not normalised");
			assert(IsNormal(q1) && "q1 is not normalised");
			assert(Dot(q0, q1) >= 0 && "q0 -> q1 should be the shortest arc");
		}
		quat Eval(float T) const
		{
			auto log_q = p.Eval(T);

			// Wrap to [-PI, +PI] range
			//if (auto len = Length(log_q); len > maths::tau_by_2f)
			//{
			//	auto wrapped_length = fmodf(len + maths::tau_by_2f, maths::tau) - maths::tau_by_2f;
			//	log_q *= Abs(wrapped_length) > maths::tinyf ? wrapped_length / len : 0.0f;
			//}

			return ExpMap(log_q);
		}
		v4 dEval(float T) const
		{
			return p.dEval(T);
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
