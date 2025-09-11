﻿//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include <span>
#include <concepts>
#include "pr/container/vector.h"
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/vector4.h"
#include "pr/maths/matrix4x4.h"
#include "pr/maths/bbox.h"

namespace pr
{
	// Notes:
	//  - A spline is a collection of curves.
	enum class ECurveType
	{
		Bezier,
		Hermite,
		Cardinal,
		CatmullRom,
		BSpline,
		Trajectory,
	};

	// How to interpret arrays of points for splines
	enum class ECurveTopology
	{
		// Notes:
		//  - Some of these are not compatible with some spline types. It's up to the caller
		//    to choice logical combinations of topology and curve type.

		// 3 points per curve. Sliding window of 3 points per curve
		Continuous3,

		// 4 points per curve. Last point is the first point of the next curve
		Continuous4,

		// 3 points per curve. Each set of 3 points is a separate curve
		Disjoint3,

		// 4 points per curve. Each set of 4 points is a separate curve
		Disjoint4,
	};

	// Curve coefficients
	struct CurveType
	{
		// Notes:
		//  - A general cubic curve is given by a parametric matrix equation:
		//                           [x x x x] [P0]
		//       P(t) =  [1 t t² t³] [x x x x] [P1]
		//                           [x x x x] [P2]
		//                           [x x x x] [P3]
		//       P'(t) = [0 1 2t 3t²] <same as above>
		//       P''(t) = [0 0 2 6t] <same as above>
		//       P'''(t) = [0 0 0 6] <same as above>
		//     where t is the parametric time, PX are control points,
		//     and M is a 4x4 matrix of cooefficients.
		//
		//  - Different spline types come from different matrix values
		//    e.g.
		//                    [+1 +0 +0 +0] [P0]            [+1 +0 +0 +0] [P0]
		//     Cubic Bezier = [-3 +3 +0 +0] [P1]  Hermite = [+0 +1 +0 +0] [V0]
		//                    [+3 -6 +3 +0] [P2]            [-3 -2 +3 -1] [P1]
		//                    [-1 +3 -3 +1] [P3]            [+2 +1 -2 +1] [V1]
		//
		//                    [ +0  +1   +0 +0] [P0]                     [+0 +2 +0 +0] [P0]
		//  Cardinal Spline = [ -s  +0   +s +0] [P1]   Catmull-Rom = 0.5*[-1 +0 +1 +0] [P1]
		//    (s = scale)     [ 2s s-3 3-2s -s] [P2]   (Cardinal w\)     [+2 -5 +4 -1] [P2]
		//                    [ -s 2-s  s-2 +s] [P3]   (scale = 0.5)     [-1 +3 -3 +1] [P3]
		//
		//                    [ +1 +4 +1 +0] [P0]                        [1/0!   +0  +0   +0] [P0] (position     = x(t))
		//         B-Spline = [ -3 +0 +3 +0] [P1]   Physics Trajectory = [  +0 1/1!  +0   +0] [V0] (velocity     = x'(t))
		//                    [ +3 -6 +3 +0] [P2]                        [  +0   +0 1/2!  +0] [A0] (acceleration = x''(t))
		//                    [ -1 +3 -3 +1] [P3]                        [  +0   +0  -0 1/3!] [J0] (jolt         = x'''(t))
		//
		//      Name      | Deg | Continuity | Tangents | Interpolates |         Use Cases
		//  --------------+-----+------------+----------+--------------+----------------------------------
		//    Bezier      |  3  |   C0/C1    |  manual  | some points	 | Shapes, fonts, vector graphics
		//    Hermite     |  3  |   C0/C1    | explicit |  all points	 | animation, physics sim, interpolation
		//    Catmull-Rom |  3  |    C1      |  auto    |  all points	 | animation, path smoothing
		//    B-Spline    |  3  |    C2      |  auto    |   no points	 | curvature-sensitive shapes (e.g. reflective), animations, camera paths
		//    Linear      |  1  |    C0      |  auto    |  all points  | 
		//  
		//        Time Continuity: C(N) => C(N-1) (derivatives: position <= velocity <= acceleration <= jolt)
		//   Geometric Continuity: G(N) => G(N-1) (tangents: position <= tangent <= curvature <= torsion)
		//
		//  - A hermite curve can be expressed as a Bezier curve using:
		//     [p0, p1, p2, p3] <=> [x0, x0 + v0/3, x1 - v1/3, x1]
		//    For rotations that's:
		//     [q0, q1, q2, q3] <=> [q0, q0*exp(w0/3), q1*~exp(w1/3), q1]
		//
		//  - Excellent summary video: https://www.youtube.com/watch?v=jvPPXbo87ds (Freya Holmer - The Continuity of Splines)


		inline static constexpr m4x4 Bezier = {
			{+1, +0, +0, +0},
			{-3, +3, +0, +0},
			{+3, -6, +3, +0},
			{-1, +3, -3, +1},
		};
		inline static constexpr m4x4 Hermite = {
			{+1, +0, +0, +0},
			{+0, +1, +0, +0},
			{-3, -2, +3, -1},
			{+2, +1, -2, +1},
		};
		inline static constexpr m4x4 CatmullRom = {
			{+0.0f, +1.0f, +0.0f, +0.0f},
			{-0.5f, +0.0f, +0.5f, +0.0f},
			{+1.0f, -2.5f, +2.0f, -0.5f},
			{-0.5f, +1.5f, -1.5f, +0.5f},
		};
		inline static constexpr m4x4 BSpline = {
			{ +1, +4, +1, +0},
			{ -3, +0, +3, +0},
			{ +3, -6, +3, +0},
			{ -1, +3, -3, +1},
		};
		inline static constexpr m4x4 Trajectory = {
			{+1, +0, +0, +0      },
			{+0, +1, +0, +0      },
			{+0, +0, +1 / 2.f, +0},
			{+0, +0, +0, +1 / 6.f},
		};
		inline static constexpr m4x4 Cardinal(float s)
		{
			return {
				{    +0,    +1,        +0, +0},
				{    -s,    +0,        +s, +0},
				{ 2 * s, s - 3, 3 - 2 * s, -s},
				{    -s, 2 - s,     s - 2, +s},
			};
		}
		template <ECurveType Type>
		inline static constexpr m4x4 Coeff()
		{
			if constexpr (Type == ECurveType::Bezier) return Bezier;
			if constexpr (Type == ECurveType::Hermite) return Hermite;
			if constexpr (Type == ECurveType::CatmullRom) return CatmullRom;
			if constexpr (Type == ECurveType::BSpline) return BSpline;
			if constexpr (Type == ECurveType::Trajectory) return Trajectory;
			static_assert(std::is_same_v<Type, void>, "Unknown curve type");
		}
	};

	// A curve is a segment within a spline
	struct CubicCurve3
	{
		m4x4 m_coeff;

		// Interpretation of these control points depends on the spline type
		//  Bezier, etc: p0, p1, p2, p3
		//  Hermite:     p0, v0, p1, v1
		//  Trajector:   p0, v0, a0, j0
		CubicCurve3(v4_cref p0, v4_cref p1, v4_cref p2, v4_cref p3, m4x4 const& coeff)
			: m_coeff(coeff * m4x4{ p0, p1, p2, p3 })
		{
		}
		v4 Eval(float t) const
		{
			t = Clamp<float>(t, 0, 1);
			return m_coeff * v4{ 1, t, t * t, t * t * t };
		}
		v4 EvalDerivative(float t) const
		{
			t = Clamp<float>(t, 0, 1);
			return m_coeff * v4{ 0, 1, 2 * t, 3 * t * t };
		}
		v4 EvalDerivative2(float t) const
		{
			t = Clamp<float>(t, 0, 1);
			return m_coeff * v4{ 0, 0, 2, 6 * t };
		}
		v4 EvalDerivative3() const
		{
			return m_coeff * v4{ 0, 0, 0, 6 };
		}
	};

	// A Spline made from a continuous collection of CubicCurves
	struct CubicSpline
	{
		using CurvePoints = std::span<v4 const>;
		vector<CubicCurve3, 1> m_curves;

		CubicSpline() = default;
		CubicSpline(v4_cref p0, v4_cref p1, v4_cref p2, v4_cref p3, m4x4 const& coeff)
			: m_curves()
		{
			m_curves.push_back(CubicCurve3{ p0, p1, p2, p3, coeff });
		}

		// Interpolated position on the spline at time 't'
		v4 Position(float time) const
		{
			auto const& curve = m_curves[static_cast<int>(time)];
			return curve.Eval(time - static_cast<int>(time));
		}

		// Interpolated tangent on the spline at time 't'
		v4 Tangent(float time) const
		{
			auto const& curve = m_curves[static_cast<int>(time)];
			return curve.EvalDerivative(time - static_cast<int>(time));
		}

		// Interpolated curvature of the spline at time 't'
		v4 Curvature(float time) const
		{
			auto const& curve = m_curves[static_cast<int>(time)];
			return curve.EvalDerivative2(time - static_cast<int>(time));
		}

		// Construct a spline from a collection of points
		static CubicSpline pr_vectorcall FromPoints(std::span<v4 const> p, ECurveTopology topo, m4x4 const& coeff)
		{
			CubicSpline spline;

			switch (topo)
			{
				// 3 points per curve. Sliding window of 3 points per curve
				case ECurveTopology::Continuous3:
				{
					for (int i = 0, iend = isize(p) - 2; i < iend; ++i)
					{
						spline.m_curves.push_back(CubicCurve3(
							p[i + 0],
							0.5f * (p[i + 0] + p[i + 1]),
							0.5f * (p[i + 1] + p[i + 2]),
							p[i + 2],
							coeff
						));
					}
					break;
				}

				// 4 points per curve. Last point is the first point of the next curve
				case ECurveTopology::Continuous4:
				{
					for (int i = 0, iend = isize(p) - 3; i < iend; i += 3)
					{
						spline.m_curves.push_back(CubicCurve3(
							p[i + 0],
							p[i + 1],
							p[i + 2],
							p[i + 3],
							coeff
						));
					}
					break;
				}

				// 3 points per curve. Each set of 3 points is a separate curve
				case ECurveTopology::Disjoint3:
				{
					for (int i = 0, iend = isize(p) - 2; i < iend; i += 3)
					{
						spline.m_curves.push_back(CubicCurve3(
							p[i + 0],
							0.5f * (p[i + 0] + p[i + 1]),
							0.5f * (p[i + 1] + p[i + 2]),
							p[i + 2],
							coeff
						));
					}
					break;
				}

				// 4 points per curve. Each set of 4 points is a separate curve
				case ECurveTopology::Disjoint4:
				{
					for (int i = 0, iend = isize(p) - 3; i < iend; i += 4)
					{
						spline.m_curves.push_back(CubicCurve3(
							p[i + 0],
							p[i + 1],
							p[i + 2],
							p[i + 3],
							coeff
						));
					}
					break;
				}
			}
		
			return spline;
		}
	};

	// Previous implementation

	struct Spline :m4x4
	{
		// Notes:
		//   x = P0, start position
		//   y = P1, start control point. tangent = y - x
		//   z = P2, end control point.   tangent = w - z
		//   w = P3, end position
		// Info:
		//  A Cubic Bezier Curve is defined by four points: two endpoints P0, P3, and two control points P1, P2
		//  In parametric form:
		//     P(t) = (1-t)^3.P0 + 3(1-t)^2.t.P1 + 3(1-t)t^2.P2 + t^3.P3, where t is in [0,1]
		enum { Start, SCtrl, ECtrl, End };
		enum class ETopo
		{
			/// <summary>3 points per spline. Sliding window of 3 points per spline</summary>
			Continuous3,

			/// <summary>4 points per spline. Last point is the first point of the next spline</summary>
			Continuous4,

			/// <summary>3 points per spline. Each set of 3 points is a separate spline</summary>
			Disjoint3,

			/// <summary>4 points per spline. Each set of 4 points is a separate spline</summary>
			Disjoint4,
		};

		// Construct a spline from 4 control points
		Spline() = default;
		Spline(v4 const& start, v4 const& start_ctrl, v4 const& end_ctrl, v4 const& end)
			:m4x4(start, start_ctrl, end_ctrl, end)
		{
			assert(start.w == 1.0f && start_ctrl.w == 1.0f && end_ctrl.w == 1.0f && end.w == 1.0f && "Splines are constructed from 4 positions");
		}
		Spline(v4 const* spline)
			:Spline(spline[Spline::Start], spline[Spline::SCtrl], spline[Spline::ECtrl], spline[Spline::End])
		{}

		// Interpretation of the control points
		v4 Point0() const   { return x; }
		v4 Forward0() const { return y - x; }
		v4 Forward1() const { return w - z; }
		v4 Point1() const   { return w; }

		// Return the position along the spline at 'time'
		v4 Position(float time) const
		{
			v4 blend;
			blend.x = (1.0f - time) * (1.0f - time) * (1.0f - time);
			blend.y = 3.0f * time * (1.0f - time) * (1.0f - time);
			blend.z = 3.0f * time * time * (1.0f - time);
			blend.w = time * time * time;
			return (static_cast<m4x4 const&>(*this) * blend).w1();
		}

		// Return the tangent along the spline at 'time'
		// Notes about velocity:
		// A spline from (0,0,0) to (1,0,0) with control points at (1/3,0,0) and (2/3,0,0) will
		// have a constant velocity of (1,0,0) over the full length of the spline.
		v4 Velocity(float time) const
		{
			v4 dblend; // the derivative of blend
			dblend.x = 3.0f * (time - 1.0f) * (1.0f - time);
			dblend.y = 3.0f * (1.0f - time) * (1.0f - 3.0f * time);
			dblend.z = 3.0f * time * (2.0f - 3.0f * time);
			dblend.w = 3.0f * time * time;
			return (static_cast<m4x4 const&>(*this) * dblend).w0();
		}

		// Return the acceleration along the spline at 'time'
		v4 Acceleration(float time) const
		{
			v4 ddblend; // the 2nd derivative of blend
			ddblend.x = 6.0f * (1.0f - time);
			ddblend.y = 6.0f * (3.0f * time - 2.0f);
			ddblend.z = 6.0f * (1.0f - 3.0f * time);
			ddblend.w = 6.0f * time;
			return (static_cast<m4x4 const&>(*this) * ddblend).w0();
		}

		// Return an object to world transform for a position along the spline
		// 'axis' is the axis id that will lie along the tangent of the spline
		// By default, the z axis is aligned to the spline with Y as up
		m4x4 O2W(float time) const
		{
			return O2W(time, 2, v4YAxis);
		}
		m4x4 O2W(float time, int axis, v4 const& up) const
		{
			return OriFromDir(Velocity(time), axis, up, Position(time));
		}

		// Equality operators
		friend bool operator == (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) == 0; }
		friend bool operator != (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) != 0; }
		friend bool operator <  (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <  0; }
		friend bool operator >  (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >  0; }
		friend bool operator <= (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) <= 0; }
		friend bool operator >= (Spline const& lhs, Spline const& rhs) { return memcmp(&lhs, &rhs, sizeof(lhs)) >= 0; }
	};

	// Spline output function func(Spline const& spline, bool last)
	template <typename T> concept SplineOutput = std::invocable<T, Spline const&, bool>;

	// Smoothed points output function func(std::span<v4 const> points, std::span<float const> times)
	template <typename T> concept SmoothOutput = std::invocable<T, std::span<v4 const>, std::span<float const>>;

	// Split 'spline' at 't' to produce two new splines
	// Given the 4 control points P0,P1,P2,P3 of 'spline'
	// the position is given by:
	//  P4 = lerp(P0,P1,t); P5 = lerp(P1,P2,t); P6 = lerp(P2,P3,t);
	//  P7 = lerp(P4,P5,t); P8 = lerp(P5,P6,t);
	//  P9 = lerp(P7,P8,t);
	// The two resulting splines 'lhs' and 'rhs' are:
	//  lhs = P0,P4,P7,P9;  rhs = P9,P8,P6,P3
	// Note: 'spline' passed by value to prevent aliasing problems with 'lhs' and 'rhs'
	inline void Split(Spline const& spline, float t, Spline& lhs, Spline& rhs)
	{
		assert(&lhs != &rhs && "lhs and rhs must not be the same spline");
		v4 P5;
		lhs.x = spline.x;                      // P0
		lhs.y = Lerp(spline.x, spline.y, t);   // P4
		P5    = Lerp(spline.y, spline.z, t);   // P5
		rhs.z = Lerp(spline.z, spline.w, t);   // P6
		rhs.w = spline.w;                      // P3
		lhs.z = Lerp(lhs.y, P5, t);            // P7
		rhs.y = Lerp(P5, rhs.z, t);            // P8
		lhs.w = rhs.x = Lerp(lhs.z, rhs.y, t); // P9
	}

	// Return the length of a spline from t0 to t1
	inline float Length(Spline const& spline, float t0, float t1, float tol = maths::tinyf)
	{
		struct L
		{
			static float Len(Spline const& s, float tol)
			{
				float poly_length = Length(s.y - s.x) + Length(s.z - s.y) + Length(s.w - s.z);
				float chord_length = Length(s.w - s.x);
				if ((poly_length - chord_length) < tol)
					return (poly_length + chord_length) * 0.5f;

				Spline lhs, rhs;
				Split(s, 0.5f, lhs, rhs);
				return Len(lhs, tol) + Len(rhs, tol);
			}
		};

		// Trim 'spline' to the region of interest
		Spline clipped = spline, dummy;
		if (t0 != 0.0f) Split(clipped, t0, dummy, clipped);
		if (t1 != 1.0f) Split(clipped, t1, clipped, dummy);
		return L::Len(clipped, tol);
	}

	// Find the closest point on 'spline' to 'pt'
	// Note: the analytic solution to this problem involves solving a 5th order polynomial
	// This method uses Newton's method and relies on a "good" initial estimate of the nearest point
	// Should have quadratic convergence
	inline float ClosestPoint_PointToSpline(Spline const& spline, v4 const& pt, float initial_estimate, bool bound01 = true, int iterations = 5)
	{
		// The distance (squared) from 'pt' to the spline is: Dist(t) = |pt - S(t)|^2.    (S(t) = spline at t)
		// At the closest point, Dist'(t) = 0.
		// Dist'(t) = -2(pt - S(t)).S'(t)
		// So we want to find 't' such that Dist'(t) = 0
		// Newton's method of iteration = t_next = t_current - f(x)/f'(x)
		//	f(x) = Dist'(t)
		//	f'(x) = Dist''(t) = 2S'(t).S'(t) - 2(pt - S(t)).S''(t)
		float time = initial_estimate;
		for (int iter = 0; iter != iterations; ++iter)
		{
			v4 S   = spline.Position(time);
			v4 dS  = spline.Velocity(time);
			v4 ddS = spline.Acceleration(time);
			v4 R   = pt - S;
			time += Dot3(R, dS) / (Dot3(dS,dS) - Dot3(R,ddS));
			if (bound01 && (time <= 0.0f || time >= 1.0f))
				return Clamp(time, 0.0f, 1.0f);
		}
		return time;
	}

	// This overload attempts to find the nearest point robustly
	// by testing 3 starting points and returning minimum.
	inline float ClosestPoint_PointToSpline(Spline const& spline, v4 const& pt, bool bound01 = true)
	{
		float t0 = ClosestPoint_PointToSpline(spline, pt, -0.5f, bound01, 5);
		float t1 = ClosestPoint_PointToSpline(spline, pt,  0.5f, bound01, 5);
		float t2 = ClosestPoint_PointToSpline(spline, pt,  1.5f, bound01, 5);
		float d0 = LengthSq(pt - spline.Position(t0));
		float d1 = LengthSq(pt - spline.Position(t1));
		float d2 = LengthSq(pt - spline.Position(t2));
		if (d0 < d1 && d0 < d2) return t0;
		if (d1 < d0 && d1 < d2) return t1;
		return t2;
	}

	// Convert a container of points into a list of splines
	// Generates a spline from each set of three points in 'points'
	// VOut(Spline const& spline, bool last);
	template <SplineOutput VOut>
	void CreateSplines(std::span<v4 const> points, Spline::ETopo topo, VOut out)
	{
		auto beg = points.data();
		auto end = beg + points.size();
		v4 p0,p1,p2,p3;

		// Zero points, no splines
		if (beg == end)
		{
			return;
		}

		// Zero points, no splines
		p0 = *beg++;
		if (beg == end)
		{
			return;
		}

		// Two points, straight line
		p1 = *beg++;
		if (beg == end)
		{
			out(Spline(p0, p1, p0, p1), true);
			return;
		}

		// Degenerate control point
		p2 = *beg++;
		if (beg == end)
		{
			out(Spline(p0, (p0 + p1)*0.5f, (p1 + p2)*0.5f, p2), true);
			return;
		}

		// Generate the stream of splines
		switch (topo)
		{
			case Spline::ETopo::Continuous3:
			{
				// Generate splines from a sliding window of three points
				for (bool first = true, last = false; ; first = false)
				{
					auto sp = first ? p0 : (p0 + p1)*0.5f;
					auto sc = p1;
					auto ec = p1;
					auto ep = last ? p2 : (p2 + p1)*0.5f;

					// Yield a spline
					out(Spline(sp, sc, ec, ep), last);
					if (last) break;

					// Slide the window
					p0 = p1; p1 = p2; p2 = *beg++;
					last = beg == end;
				}
				break;
			}
			case Spline::ETopo::Continuous4:
			{
				p3 = *beg++;
				for (; ; )
				{
					// Yield a spline
					out(Spline(p0, p1, p2, p3), beg == end);
					if (beg == end) break;

					// Slide the window. An exception means, the wrong number of points
					p0 = p3;
					p1 = *beg++;
					p2 = *beg++;
					p3 = *beg++;
				}
				break;
			}
			case Spline::ETopo::Disjoint3:
			{
				// Generate splines from each set of three points
				for (; ; )
				{
					// Yield a spline
					out(Spline(p0, (p0 + p1)*0.5f, (p1 + p2)*0.5f, p2), beg == end);
					if (beg == end) break;

					// Slide the window. An exception means, the wrong number of points
					p0 = *beg++;
					p1 = *beg++;
					p2 = *beg++;
				}
				break;
			}
			case Spline::ETopo::Disjoint4:
			{
				// Generate splines from each set of four points
				p3 = *beg++;
				for (;;)
				{
					// Yield a spline
					out(Spline(p0, p1, p2, p3), beg == end);
					if (beg == end) break;

					// Slide the window. An exception means, the wrong number of points
					p0 = *beg++;
					p1 = *beg++;
					p2 = *beg++;
					p3 = *beg++;
				}
				break;
			}
			default:
			{
				throw std::runtime_error("Unsupported spline topology");
			}
		}
	}

	// Fill a container of points with a rasterized version of 'spline'
	// 'points' is the vert along the spline
	// 'times' is the times along 'spline' at the point locations
	template <typename PCont, typename TCont>
	void Raster(Spline const& spline, PCont& points, TCont& times, int max_points, float tol = maths::tinyf)
	{
		struct L
		{
			struct Elem // Linked list element type used to form a priority queue
			{
				Elem*         m_next;      // The next subsection of spline with less error
				Spline const* m_spline;    // Pointer to the subsection of the spline
				float         m_t0, m_t1;  // Times along the original spline
				int           m_ins;       // The position to insert a vert in the out container
				float         m_err;       // How much a straight line diverges from 'm_spline'
				
				Elem(Spline const& s, float t0, float t1, int ins)
					:m_next(0)
					,m_spline(&s)
					,m_t0(t0)
					,m_t1(t1)
					,m_ins(ins)
					,m_err(Length(s.y - s.x) + Length(s.z - s.y) + Length(s.w - s.z) - Length(s.w - s.x))
				{}
			};

			// Insert 'elem' into the priority queue of which 'queue' is the head
			static Elem* QInsert(Elem* queue, Elem& elem)
			{
				if (queue == 0 || queue->m_err < elem.m_err) { elem.m_next = queue; return &elem; }
				Elem* i; for (i = queue; i->m_next && i->m_next->m_err > elem.m_err; i = i->m_next) {}
				elem.m_next = i->m_next; // 'i->m_next' has an error less than 'elem' (i.m_next might be 0)
				i->m_next = &elem;
				return queue;
			}

			// Breadth-first recursive raster of this spline
			static void Raster(PCont& points, TCont& times, Elem* queue, int& pts_remaining, float tol)
			{
				// Pop the top spline segment from the queue or we've run out of points
				Elem const& elem = *queue;
				queue = queue->m_next;

				// Stop if the error is less than 'tol'
				if (elem.m_err < tol || pts_remaining <= 0)
					return;

				// Subdivide the spline segment and insert the mid-point into 'points'
				Spline Lhalf, Rhalf;                       // Spline segments for each half
				Split(*elem.m_spline, 0.5f, Lhalf, Rhalf); // splits the spline at t=0.5
				float t = (elem.m_t0 + elem.m_t1) * 0.5f;  // Find the time on the original spline
				points.insert(points.begin() + elem.m_ins, Lhalf.Point1());
				times .insert(times .begin() + elem.m_ins, t);

				// Increment the insert position for all elements after 'elem.m_ins'
				for (Elem* i = queue; i != 0; i = i->m_next)
					i->m_ins += (i->m_ins > elem.m_ins);

				// Insert both halves into the queue
				Elem lhs(Lhalf, elem.m_t0, t, elem.m_ins  );
				Elem rhs(Rhalf, t, elem.m_t1, elem.m_ins+1);
				queue = QInsert(queue, lhs);
				queue = QInsert(queue, rhs);
				Raster(points, times, queue, --pts_remaining, tol); // continue recursively
			}
		};

		points.insert(std::end(points), spline.Point0());
		points.insert(std::end(points), spline.Point1());
		times.insert(std::end(times), 0.0f);
		times.insert(std::end(times), 1.0f);

		int pts_remaining = max_points - 2;
		typename L::Elem elem(spline, 0.0f, 1.0f, 1);
		L::Raster(points, times, &elem, pts_remaining, tol);
	}
	template <typename PCont>
	void Raster(Spline const& spline, PCont& points, int max_points, float tol = maths::tinyf)
	{
		// Dummy container for the time values
		struct TCont
		{
			int begin() { return 0; }
			int end()   { return 0; }
			void insert(int, float) {}
		} times;
		Raster(spline, points, times, max_points, tol);
	}

	// Fill a container of points with a smoothed spline based on 'points
	template <SmoothOutput VOut, int MaxPointsPerSpline = 30>
	void Smooth(std::span<v4 const> points, Spline::ETopo topo, VOut out, float tol = maths::tinyf)
	{
		if (points.size() < 3)
		{
			float times[] = {0.0f, 1.0f};
			out(points, times);
			return;
		}

		pr::vector<v4, MaxPointsPerSpline, true> spline_points;
		pr::vector<float, MaxPointsPerSpline, true> spline_times;
		CreateSplines(points, topo, [&](Spline const& spline, bool last)
		{
			spline_points.resize(0);
			spline_times.resize(0);

			// Raster the spline into a temp buffer
			Raster(spline, spline_points, spline_times, MaxPointsPerSpline, tol);

			// Stream out the verts
			auto points = std::span<v4 const>(spline_points.data(), spline_points.size() - !last);
			auto times = std::span<float const>(spline_times.data(), spline_times.size() - !last);
			out(points, times);
		});
	}
	
	// Random infinite spline within a bounding box
	template <typename Rng = std::default_random_engine> 
	struct RandSpline :Spline
	{
		Rng*   m_rng;
		Spline m_next;
		BBox   m_bbox;
		float  m_clock;  // The current 'time' along the spline [0,1)

		RandSpline(Rng& rng, BBox const& bbox = BBoxUnit)
			:m_rng(&rng)
			,m_next()
			,m_bbox(bbox)
			,m_clock(0.0f)
		{
			m_next = Spline(GenPoint(), GenPoint(), GenPoint(), GenPoint());
			Roll();
			Roll();
		}
		void Reset(Rng& rng)
		{
			m_rng = &rng;
			m_clock = 0.0f;
		}
		v4 GenPoint()
		{
			return Random3(*m_rng, m_bbox.Lower(), m_bbox.Upper(), 1.0f);
		}
		void Roll()
		{
			static_cast<Spline&>(*this) = m_next;
			m_next.x = m_next.Point1();
			m_next.y = m_next.Point1() + m_next.Forward1();
			m_next.z = GenPoint();
			m_next.w = GenPoint();
		}
		void Adv(float dt)
		{
			m_clock += dt;
			for (int i = 0; i != 2 && m_clock >= 1.0f; ++i, m_clock -= 1.0f) Roll();
		}

		// Return an object to world transform for the current position on the spline
		m4x4 O2W() const
		{
			return Spline::O2W(m_clock);
		}
		m4x4 O2W(int axis, v4 const& up) const
		{
			return Spline::O2W(m_clock, axis, up);
		}

		// Return the current position along the spline
		v4 Position() const
		{
			return Spline::Position(m_clock);
		}

		// Return the current velocity along the spline
		v4 Velocity() const
		{
			return Spline::Velocity(m_clock);
		}

		// Return the acceleration along the spline at 'time'
		v4 Acceleration() const
		{
			return Spline::Acceleration(m_clock);
		}
	};
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(SplineTests)
	{
		
	}
}
#endif