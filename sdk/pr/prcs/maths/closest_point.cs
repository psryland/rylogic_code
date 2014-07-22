//***************************************************
// Bounding Box
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	public static partial class Geometry
	{
		/// <summary>Finds the closest point to the 2d line segment a->b returning the parametric value</summary>
		public static float ClosestPoint(v2 a, v2 b, v2 pt)
		{
			v2 ab = b - a;

			// Project 'pt' onto 'ab', but defer divide by 'ab.Length2Sq'
			float t = v2.Dot2(pt - a, ab);
			if (t <= 0.0f)
				return 0.0f; // 'point' projects outside 'line', clamp to 0.0f
			
			float denom = ab.Length2Sq;
			if (t >= denom)
				return 1.0f; // 'point' projects outside 'line', clamp to 1.0f
			
			return t / denom; // 'point' projects inside 'line', do deferred divide now
		}

		/// <summary>Return the closest point on 'rect' to 'pt'</summary>
		public static v2 ClosestPoint(BRect rect, v2 pt)
		{
			v2 lower = rect.Lower;
			v2 upper = rect.Upper;
			v2 closest;
			if (rect.IsWithin(pt))
			{
				// if pt.x/pt.y > rect.sizeX/rect.sizeY then the point
				// is closer to the Y edge of the rectangle
				if (Maths.Abs(pt.x * rect.SizeY) > Maths.Abs(pt.y * rect.SizeX))
					closest = new v2(pt.x, Maths.SignF(pt.y) * rect.SizeY);
				else
					closest = new v2(Maths.SignF(pt.x) * rect.SizeX, pt.y);
			}
			else
			{
				closest = new v2(
					Maths.Clamp(pt.x, lower.x, upper.x),
					Maths.Clamp(pt.y, lower.y, upper.y));
			}
			return closest;
		}

		/// <summary>
		/// Returns the closest points between 'lhs' and 'rhs'.
		/// If 'lhs' and 'rhs' overlap, returns the points of deepest penetration.</summary>
		public static void ClosestPoint(BRect lhs, BRect rhs, out v2 pt0, out v2 pt1)
		{
			pt0 = lhs.Centre;
			pt1 = rhs.Centre;
			if (rhs.Centre.x > lhs.Centre.x) { pt0.x += lhs.Radius.x; pt1.x += rhs.Radius.x; }
			if (rhs.Centre.x < lhs.Centre.x) { pt0.x -= lhs.Radius.x; pt1.x -= rhs.Radius.x; }
			if (rhs.Centre.y > lhs.Centre.y) { pt0.y += lhs.Radius.y; pt1.y += rhs.Radius.y; }
			if (rhs.Centre.y < lhs.Centre.y) { pt0.y -= lhs.Radius.y; pt1.y -= rhs.Radius.y; }
		}
		
		/// <summary>Returns the average of the closest points between 'lhs' and 'rhs'.</summary>
		public static v2 ClosestPoint(BRect lhs, BRect rhs)
		{
			v2 pt0, pt1;
			ClosestPoint(lhs, rhs, out pt0, out pt1);
			return (pt0 + pt1) * 0.5f;
		}

		/// <summary>Return the closest point between two line segments</summary>
		public static void ClosestPoint(v2 s0, v2 e0, v2 s1, v2 e1, out float t0, out float t1)
		{
			v2 line0              = e0 - s0;
			v2 line1              = e1 - s1;
			v2 separation         = s0 - s1;
			float f               = v2.Dot2(line1, separation);
			float c               = v2.Dot2(line0, separation);
			float line0_length_sq = line0.Length2Sq;
			float line1_length_sq = line1.Length2Sq;

			// Check if either or both segments are degenerate
			if (Maths.FEql(line0_length_sq, 0f) && Maths.FEql(line1_length_sq, 0f)) { t0 = 0.0f; t1 = 0.0f; return; }
			if (Maths.FEql(line0_length_sq, 0f))                                    { t0 = 0.0f; t1 = Maths.Clamp( f / line1_length_sq, 0.0f, 1.0f); return; }
			if (Maths.FEql(line1_length_sq, 0f))                                    { t1 = 0.0f; t0 = Maths.Clamp(-c / line0_length_sq, 0.0f, 1.0f); return; }

			// The general nondegenerate case starts here
			float b = v2.Dot2(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b; // Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to segment 1. Otherwise pick arbitrary t0
			t0 = denom != 0.0f ? Maths.Clamp((b*f - c*line1_length_sq) / denom, 0.0f, 1.0f) : 0.0f;

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + f) / line1_length_sq;

			// If t1 in [0,1] then done. Otherwise, clamp t1, recompute t0 for the new value
			// of t1 using t0 = Dot3(pt1 - s0, line0) / line0_length_sq = (b*t1 - c) / line0_length_sq
			// and clamped t0 to [0, 1]
			if      (t1 < 0.0f) { t1 = 0.0f; t0 = Maths.Clamp(( -c) / line0_length_sq, 0.0f, 1.0f); }
			else if (t1 > 1.0f) { t1 = 1.0f; t0 = Maths.Clamp((b-c) / line0_length_sq, 0.0f, 1.0f); }
		}

		/// <summary>Return the closest point between two line segments</summary>
		public static void ClosestPoint(v4 s0, v4 e0, v4 s1, v4 e1, out float t0, out float t1)
		{
			Debug.Assert(s0.w == 1f && e0.w == 1f && s1.w == 1f && e1.w == 1f);

			v4 line0              = e0 - s0;
			v4 line1              = e1 - s1;
			v4 separation         = s0 - s1;
			float f               = v4.Dot3(line1, separation);
			float c               = v4.Dot3(line0, separation);
			float line0_length_sq = line0.Length3Sq;
			float line1_length_sq = line1.Length3Sq;

			// Check if either or both segments are degenerate
			if (Maths.FEql(line0_length_sq, 0f) && Maths.FEql(line1_length_sq, 0f)) { t0 = 0.0f; t1 = 0.0f; return; }
			if (Maths.FEql(line0_length_sq, 0f))                                    { t0 = 0.0f; t1 = Maths.Clamp( f / line1_length_sq, 0.0f, 1.0f); return; }
			if (Maths.FEql(line1_length_sq, 0f))                                    { t1 = 0.0f; t0 = Maths.Clamp(-c / line0_length_sq, 0.0f, 1.0f); return; }

			// The general nondegenerate case starts here
			float b = v4.Dot3(line0, line1);
			float denom = line0_length_sq * line1_length_sq - b * b; // Always non-negative

			// If segments not parallel, calculate closest point on infinite line 'line0'
			// to infinite line 'line1', and clamp to segment 1. Otherwise pick arbitrary t0
			t0 = denom != 0.0f ? Maths.Clamp((b*f - c*line1_length_sq) / denom, 0.0f, 1.0f) : 0.0f;

			// Calculate point on infinite line 'line1' closest to segment 'line0' at t0
			// using t1 = Dot3(pt0 - s1, line1) / line1_length_sq = (b*t0 + f) / line1_length_sq
			t1 = (b*t0 + f) / line1_length_sq;

			// If t1 in [0,1] then done. Otherwise, clamp t1, recompute t0 for the new value
			// of t1 using t0 = Dot3(pt1 - s0, line0) / line0_length_sq = (b*t1 - c) / line0_length_sq
			// and clamped t0 to [0, 1]
			if      (t1 < 0.0f) { t1 = 0.0f; t0 = Maths.Clamp(( -c) / line0_length_sq, 0.0f, 1.0f); }
			else if (t1 > 1.0f) { t1 = 1.0f; t0 = Maths.Clamp((b-c) / line0_length_sq, 0.0f, 1.0f); }
		}

		/// <summary>
		/// Find the closest point on 'spline' to 'pt'
		/// Note: the analytic solution to this problem involves solving a 5th order polynomial
		/// This method uses Newton's method and relies on a "good" initial estimate of the nearest point
		/// Should have quadratic convergence</summary>
		public static float ClosestPoint(Spline spline, v4 pt, float initial_estimate, bool bound01 = true, int iterations = 5)
		{
			// The distance (sqr'd) from 'pt' to the spline is: Dist(t) = |pt - S(t)|^2.    (S(t) = spline at t)
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
				time += v4.Dot3(R, dS) / (v4.Dot3(dS,dS) - v4.Dot3(R,ddS));
				if (bound01 && time <= 0.0f || time >= 1.0f)
					return Maths.Clamp(time, 0.0f, 1.0f);
			}
			return time;
		}

		/// <summary>
		/// This overload attempts to find the nearest point robustly
		/// by testing 3 starting points and returning minimum.</summary>
		public static float ClosestPoint(Spline spline, v4 pt, bool bound01 = true)
		{
			float t0 = ClosestPoint(spline, pt, 0.0f, bound01, 5);
			float t1 = ClosestPoint(spline, pt, 0.5f, bound01, 5);
			float t2 = ClosestPoint(spline, pt, 1.0f, bound01, 5);
			float d0 = (pt - spline.Position(t0)).Length3Sq;
			float d1 = (pt - spline.Position(t1)).Length3Sq;
			float d2 = (pt - spline.Position(t2)).Length3Sq;
			if (d0 < d1 && d0 < d2) return t0;
			if (d1 < d0 && d1 < d2) return t1;
			return t2;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using maths;

	[TestFixture] public static partial class UnitTests
	{
		internal static class TestClosestPoint
		{
			[Test] public static void PointToLine2d()
			{
				var a = new v2(1f,1f);
				var b = new v2(4f,3f);
				Assert.AreEqual(0f, Geometry.ClosestPoint(a,b,new v2(0f,0f)));
				Assert.AreEqual(1f, Geometry.ClosestPoint(a,b,new v2(5f,2f)));
				Assert.AreEqual(0.5f, Geometry.ClosestPoint(a,b,new v2(2.5f,2f)));
			}
		}
	}
}

#endif
