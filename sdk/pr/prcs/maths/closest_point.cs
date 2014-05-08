//***************************************************
// Bounding Box
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	public static partial class Geometry
	{
		/// <summary>
		/// Find the closest point on 'spline' to 'pt'
		/// Note: the analytic solution to this problem involves solving a 5th order polynomial
		/// This method uses Newton's method and relies on a "good" initial estimate of the nearest point
		/// Should have quadratic convergence</summary>
		public static float ClosestPoint(Spline spline, v4 pt, float initial_estimate, int iterations = 5)
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
			}
			return time;
		}

		/// <summary>
		/// This overload attempts to find the nearest point robustly
		/// by testing 3 starting points and returning minimum.</summary>
		public static float ClosestPoint(Spline spline, v4 pt)
		{
			float t0 = ClosestPoint(spline, pt, -0.5f, 5);
			float t1 = ClosestPoint(spline, pt,  0.5f, 5);
			float t2 = ClosestPoint(spline, pt,  1.5f, 5);
			float d0 = (pt - spline.Position(t0)).Length3Sq;
			float d1 = (pt - spline.Position(t1)).Length3Sq;
			float d2 = (pt - spline.Position(t2)).Length3Sq;
			if (d0 < d1 && d0 < d2) return t0;
			if (d1 < d0 && d1 < d2) return t1;
			return t2;
		}


	}
}
