//***************************************************
// Scalar Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;
using pr.util;

namespace pr.maths
{
	/// <summary>scalar functions</summary>
	public static partial class Maths
	{
		public const float TinyF     = 1.00000007e-05f;
		public const float TinySqF   = 1.00000015e-10f;
		public const float TinySqrtF = 3.16227786e-03f;

		public const double TinyD     = 1.0000000000000002e-12;
		public const double TinySqD   = 1.0000000000000003e-24;
		public const double TinySqrtD = 1.0000000000000002e-06;
		
		public const double Phi       = 1.618033988749894848204586834; // "Golden Ratio"
		public const double Tau       = 6.283185307179586476925286766559; // circle constant
		public const double InvTau    = 1.0 / Tau;
		public const double TauBy2    = Tau / 2.0;
		public const double TauBy3    = Tau / 3.0;
		public const double TauBy4    = Tau / 4.0;
		public const double TauBy5    = Tau / 5.0;
		public const double TauBy6    = Tau / 6.0;
		public const double TauBy7    = Tau / 7.0;
		public const double TauBy8    = Tau / 8.0;
		public const double TauBy10   = Tau / 10.0;
		public const double TauBy16   = Tau / 16.0;
		public const double TauBy32   = Tau / 32.0;
		public const double TauBy360  = Tau / 360.0;
		public const double _360ByTau = 360.0 / Tau;
		public const double Root2     = 1.4142135623730950488016887242097;
		public const double Root3     = 1.7320508075688772935274463415059;
		public const double InvRoot2  = 1.0 / 1.4142135623730950488016887242097;
		public const double InvRoot3  = 1.0 / 1.7320508075688772935274463415059;

		public const float PhiF       = (float)Phi;
		public const float TauF       = (float)Tau;
		public const float InvTauF    = (float)InvTau;
		public const float TauBy2F    = (float)TauBy2;
		public const float TauBy3F    = (float)TauBy3;
		public const float TauBy4F    = (float)TauBy4;
		public const float TauBy5F    = (float)TauBy5;
		public const float TauBy6F    = (float)TauBy6;
		public const float TauBy7F    = (float)TauBy7;
		public const float TauBy8F    = (float)TauBy8;
		public const float TauBy10F   = (float)TauBy10;
		public const float TauBy16F   = (float)TauBy16;
		public const float TauBy32F   = (float)TauBy32;
		public const float TauBy360F  = (float)TauBy360;
		public const float _360ByTauF = (float)_360ByTau;
		public const float Root2F     = (float)Root2;
		public const float Root3F     = (float)Root3;
		public const float InvRoot2F  = (float)InvRoot2;
		public const float InvRoot3F  = (float)InvRoot3;

		public static bool      IsFinite(float x)                               { return !float.IsInfinity(x) && !float.IsNaN(x); }
		public static bool      IsFinite(double x)                              { return !double.IsInfinity(x) && !double.IsNaN(x); }
		public static int       SignI(bool positive)                            { return positive ? 1 : -1; }
		public static float     SignF(bool positive)                            { return positive ? 1f : -1f; }
		public static double    SignD(bool positive)                            { return positive ? 1.0 : -1.0; }
		public static decimal   SignM(bool positive)                            { return positive ? 1m : -1m; }
		public static int       Sign(int x)                                     { return SignI(x >= 0.0f); }
		public static float     Sign(float x)                                   { return SignF(x >= 0.0f); }
		public static double    Sign(double x)                                  { return SignD(x >= 0.0); }
		public static decimal   Sign(decimal x)                                 { return SignM(x >= 0m); }
		public static int       OneIfZero(int x)                                { return x != 0 ? x : 1; }
		public static float     OneIfZero(float x)                              { return x != 0f ? x : 1f; }
		public static int       Sqr(int x)                                      { return x * x; }
		public static float     Sqr(float x)                                    { return x * x; }
		public static double    Sqr(double x)                                   { return x * x; }
		public static float     Sqrt(float x)                                   { return (float)Sqrt((double)x); }
		public static double    Sqrt(double x)                                  { return Math.Sqrt(x); }
		public static float     Cubed(float x)                                  { return x * x * x; }
		public static double    Cubed(double x)                                 { return x * x * x; }
		public static float     CubeRoot(float x)                               { return (float)CubeRoot((double)x); }
		public static double    CubeRoot(double x)                              { return Math.Pow(x, 1.0/3.0); }
		public static float     DegreesToRadians(float degrees)                 { return (float)(degrees * TauBy360); }
		public static double    DegreesToRadians(double degrees)                { return degrees * TauBy360; }
		public static v4        DegreesToRadians(v4 degrees)                    { return new v4(DegreesToRadians(degrees.x), DegreesToRadians(degrees.y), DegreesToRadians(degrees.z), DegreesToRadians(degrees.w)); }
		public static float     RadiansToDegrees(float radians)                 { return (float)(radians * _360ByTau); }
		public static double    RadiansToDegrees(double radians)                { return radians * _360ByTau; }
		public static v4        RadiansToDegrees(v4 degrees)                    { return new v4(RadiansToDegrees(degrees.x), RadiansToDegrees(degrees.y), RadiansToDegrees(degrees.z), RadiansToDegrees(degrees.w)); }

		public static void      Swap<T>(ref T lhs, ref T rhs)                   { var tmp = lhs; lhs = rhs; rhs = tmp; }
		public static float     Frac(int min, int x, int max)                   { Debug.Assert(max != min); return (float)(x - min) / (max - min); }
		public static double    Frac(long min, long x, long max)                { Debug.Assert(max != min); return (double)(x - min) / (max - min); }
		public static float     Frac(float min, float x, float max)             { Debug.Assert(Math.Abs(max - min) > float .Epsilon); return (x - min) / (max - min); }
		public static double    Frac(double min, double x, double max)          { Debug.Assert(Math.Abs(max - min) > double.Epsilon); return (x - min) / (max - min); }
		public static decimal   Frac(decimal min, decimal x, decimal max)       { Debug.Assert(Math.Abs(max - min) > 0); return (x - min) / (max - min); }
		public static float     Len2Sq(float x, float y)                        { return Sqr(x) + Sqr(y); }
		public static float     Len2(float x, float y)                          { return Sqrt(Len2Sq(x,y)); }
		public static float     Len3Sq(float x, float y, float z)               { return Sqr(x) + Sqr(y) + Sqr(z); }
		public static float     Len3(float x, float y, float z)                 { return Sqrt(Len3Sq(x,y,z)); }

		// Floating point comparisons
		/// <summary>
		/// Compare floats for equality.
		/// *WARNING* 'tol' is the fraction of the largest value. i.e. abs(a-b) &lt; tol * max(abs(a), abs(b))
		/// Do not use this expecting this behaviour: a.Within(b - tol, b + tol)</summary>
		public static bool FEqlRelative(float a, float b, float tol)
		{
			// Floating point compare is dangerous and subtle.
			// See: https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
			// and: http://floating-point-gui.de/errors/NearlyEqualsTest.java
			// Tests against zero treat 'tol' as an absolute difference threshold.
			// Tests between two non-zero values use 'tol' as a relative difference threshold.
			// i.e.
			//    FEql(2e-30, 1e-30) == false
			//    FEql(2e-30 - 1e-30, 0) == true

			// Handles tests against zero where relative error is meaningless
			// Tests with 'b == 0' are the most common so do them first
			if (b == 0) return Math.Abs(a) < tol;
			if (a == 0) return Math.Abs(b) < tol;

			// Handle infinities and exact values
			if (a == b) return true;

			// Test relative error as a fraction of the largest value
			return Math.Abs(a - b) < tol * Math.Max(Math.Abs(a), Math.Abs(b));
		}
		public static bool FEqlRelative(double a, double b, double tol)
		{
			// Handles tests against zero where relative error is meaningless
			// Tests with 'b == 0' are the most common so do them first
			if (b == 0) return Math.Abs(a) < tol;
			if (a == 0) return Math.Abs(b) < tol;

			// Handle infinities and exact values
			if (a == b) return true;

			// Test relative error as a fraction of the largest value
			return Math.Abs(a - b) < tol * Math.Max(Math.Abs(a), Math.Abs(b));
		}
		public static bool FEql(float a, float b)
		{
			return FEqlRelative(a, b, TinyF);
		}
		public static bool FEql(double a, double b)
		{
			return FEqlRelative(a, b, TinyD);
		}

		/// <summary>Absolute value of 'a'</summary>
		public static T Abs<T>(T a)
		{
			return Operators<T>.GreaterEql(a, default(T)) ? a : Operators<T>.Neg(a);
		}

		/// <summary>Return 'a/b', or 'def' if 'b' is zero</summary>
		public static T Div<T>(T a, T b, T def = default(T))
		{
			return !Equals(b, default(T)) ? Operators<T>.Div(a,b) : def;
		}

		/// <summary>Minimum value</summary>
		public static T Min<T>(T lhs, T rhs) where T :IComparable<T>
		{
			return lhs.CompareTo(rhs) <= 0 ? lhs : rhs;
		}
		public static T Min<T>(T lhs, params T[] rhs) where T :IComparable<T>
		{
			foreach (var r in rhs) lhs = Min(lhs, r);
			return lhs;
		}

		/// <summary>Maximum value</summary>
		public static T Max<T>(T lhs, T rhs) where T :IComparable<T>
		{
			return lhs.CompareTo(rhs) >= 0 ? lhs : rhs;
		}
		public static T Max<T>(T lhs, params T[] rhs) where T :IComparable<T>
		{
			foreach (var r in rhs) lhs = Max(lhs, r);
			return lhs;
		}

		/// <summary>Clamp value to an inclusive range</summary>
		public static T Clamp<T>(T x, T min, T max) where T:IComparable<T>
		{
			Debug.Assert(min.CompareTo(max) <= 0);
			return
				x.CompareTo(max) > 0 ? max :
				x.CompareTo(min) < 0 ? min : x;
		}

		/// <summary>True if 'x' is within the interval '[min-tol,max+tol]'</summary>
		public static bool Within<T>(T min, T x, T max, T tol = default(T)) where T:IComparable<T>
		{
			min = Operators<T>.Sub(min, tol);
			max = Operators<T>.Add(max, tol);
			return x.CompareTo(min) >= 0 && x.CompareTo(max) <= 0;
		}

		/// <summary>Return the average of the given values</summary>
		public static T Average<T>(params T[] values)
		{
			if (values.Length == 0)
				throw new Exception("Average: no values provided");
			if (values.Length == 1)
				return values[0];

			var sum = default(T);
			foreach (var v in values)
				sum = Operators<T>.Add(sum, v); 

			return Operators<T,int>.Div(sum, values.Length);
		}

		/// <summary>Return the median of the given values</summary>
		public static T Median<T>(params T[] values) where T:IComparable<T>
		{
			if (values.Length == 0)
				throw new Exception("Median: no values provided");
			if (values.Length == 1)
				return values[0];

			// If there are an even number of values, find the centre two and average them.
			// Otherwise, the median is just the middle value.
			var idx = values.Length / 2;
			return (values.Length & 1) == 0
				? Average(values.NthElement(idx-1), values.NthElement(idx))
				: values.NthElement(idx);
		}

		/// <summary>Linear interpolate</summary>
		public static int Lerp(int lhs, int rhs, float frac)
		{
			return (int)Math.Round(Lerp((float)lhs, (float)rhs, frac), 0);
		}
		public static long Lerp(long lhs, long rhs, double frac)
		{
			return (long)Math.Round(Lerp((double)lhs, (double)rhs, frac), 0);
		}
		public static float Lerp(float lhs, float rhs, float frac)
		{
			return (float)Lerp((double)lhs, (double)rhs, (double)frac);
		}
		public static double Lerp(double lhs, double rhs, double frac)
		{
			return lhs * (1.0 - frac) + rhs * (frac);
		}
		public static decimal Lerp(decimal lhs, decimal rhs, double frac)
		{
			return lhs * (decimal)(1.0 - frac) + rhs * (decimal)(frac);
		}
		public static T Lerp<T>(T lhs, T rhs, double frac)
		{
			return Operators<T>.Add(Operators<T,double>.Mul(lhs, 1.0 - frac), Operators<T,double>.Mul(rhs, frac));
		}
		public static T Lerp<T>(double frac, params T[] values)
		{
			var len = values.Length;
			if (len == 0)
				throw new Exception("No values to interpolate");

			// Scale 'frac' up to the number of values
			var fidx = frac * (len - 1);
			if (fidx <=   0.0) return values[0];
			if (fidx >= len-1) return values[len-1];
			
			var idx = (int)fidx;
			return Lerp(values[idx], values[idx+1], fidx - idx);
		}

		/// <summary>
		/// Return the greatest common factor between 'a' and 'b'
		/// Uses the Euclidean algorithm. If the greatest common factor is 1, then 'a' and 'b' are co-prime</summary>
		public static int GreatestCommonFactor(int a, int b)
		{
			while (b != 0) { int t = b; b = a % b; a = t; }
			return a;
		}
		public static int LeastCommonMultiple(int a, int b)
		{
			return (a*b) / GreatestCommonFactor(a,b);
		}

		/// <summary>Returns the radius of a sphere with the given volume</summary>
		public static double SphereRadius(double volume)
		{
			// The volume of a sphere, v = 2/3τr³ => r = ³root(3v/2τ)
			return CubeRoot(1.5 * volume / Tau);
		}

		/// <summary>Returns the cosine of the angle of the corner in a triangle with side lengths a,b,c for the corner opposite 'c'.</summary>
		public static double CosAngle(double a, double b, double c)
		{
			var numer = Sqr(a) + Sqr(b) - Sqr(c);
			var denom = 2 * a * b;
			var cos_angle = Clamp(denom != 0 ? numer / denom : 1, -1, 1);
			return cos_angle;
		}

		/// <summary>Returns the angle of the corner in a triangle with side lengths a,b,c for the corner opposite 'c'.</summary>
		public static double Angle(double a, double b, double c)
		{
			return Math.Acos(CosAngle(a,b,c));
		}

		/// <summary>Return the length of a triangle side given by two adjacent side lengths and an angle between them</summary>
		public static double Length(double adj0, double adj1, double angle)
		{
			var len_sq = adj0*adj0 + adj1*adj1 - 2.0f * adj0 * adj1 * Math.Cos(angle);
			return len_sq > 0 ? Sqrt(len_sq) : 0.0;
		}

		/// <summary>Round a number to 'digits' significant figures</summary>
		public static double RoundSF(double d, int digits)
		{
			if (d == 0)
				return 0;

			var scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
			return (double)(scale * Math.Round((decimal)d / scale, digits));
		}

		/// <summary>Quantise a value to 'scale'. For best results, 'scale' should be a power of 2, i.e. 256, 1024, 2048, etc</summary>
		public static double Quantise(double x, int scale)
		{
			return (long)(x*scale) / (double)scale;
		}

		/// <summary>Quantise a value to units of 'quantum'</summary>
		public static double Quantise(double x, double quantum)
		{
			return (long)((x + 0.5*Sign(x)*quantum) / quantum) * quantum;
		}
		public static decimal Quantise(decimal x, decimal quantum)
		{
			return (long)((x + 0.5m*Sign(x)*quantum) / quantum) * quantum;
		}

		/// <summary>Convert a series of floating point values into a series of integers, preserving the remainders such that the sum of integers is within 1.0 of the sum of the floats</summary>
		public static IEnumerable<int> TruncWithRemainder(IEnumerable<double> floats)
		{
			var remainder = 0.0;
			foreach (var value in floats)
			{
				var fval = value + remainder;
				var ival = (int)(fval + Sign(fval) * 0.5);
				remainder = fval - ival;
				yield return ival;
			}
		}
		public static IEnumerable<int> TruncWithRemainder(IEnumerable<float> floats)
		{
			return TruncWithRemainder(floats.Select(x => (double)x));
		}

		/// <summary>Returns 1 if 'hi' is > 'lo' otherwise 0</summary>
		public static double Step(double lo, double hi)
		{
			return lo <= hi ? 0.0 : 1.0;
		}

		/// <summary>Returns the 'Hermite' interpolation (3t² - 2t³) between 'lo' and 'hi' for t=[0,1]</summary>
		public static double SmoothStep(double lo, double hi, double t)
		{
			if (lo == hi) return lo;
			t = Clamp((t - lo)/(hi - lo), 0.0, 1.0);
			return t*t*(3 - 2*t);
		}

		/// <summary>Returns a fifth-order 'Perlin' interpolation (6t^5 - 15t^4 + 10t^3) between 'lo' and 'hi' for t=[0,1]</summary>
		public static double SmoothStep2(double lo, double hi, double t)
		{
			if (lo == hi) return lo;
			t = Clamp((t - lo)/(hi - lo), 0.0, 1.0);
			return t*t*t*(t*(t*6 - 15) + 10);
		}

		/// <summary>
		/// Scale a value on the range [-inf,+inf] to within the range [-1,+1].
		/// 'n' is a horizontal scaling factor.
		/// If n = 1, [-1,+1] maps to [-0.5, +0.5]
		/// If n = 10, [-10,+10] maps to [-0.5, +0.5], etc</summary>
		public static double Sigmoid(double x, double n = 1.0)
		{
			return Math.Atan(x/n) / TauBy4;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Text;
	using maths;

	[TestFixture] public class TestMathsScalar
	{
		[Test] public void Average()
		{
			var a = Maths.Average(1.0, 4.0, 2.0, 7.0, -3.0);
			Assert.AreEqual(a, 2.2);

			var m = Maths.Median(1.0, 4.0, 2.0, 7.0, -3.0);
			Assert.AreEqual(m, 2.0);

			m = Maths.Median(1.0, 4.0, 2.0, 7.0, -3.0, -1.0);
			Assert.AreEqual(m, 1.5);
		}
		[Test] public void Lerp()
		{
			var a0 = new[]{ 1.0, 10.0, 2.0, 5.0 };
			Assert.AreEqual(Maths.Lerp(-0.1, a0), 1.0);
			Assert.AreEqual(Maths.Lerp( 0.0, a0), 1.0);
			Assert.AreEqual(Maths.Lerp( 1.0, a0), 5.0);
			Assert.AreEqual(Maths.Lerp( 1.1, a0), 5.0);
			Assert.AreEqual(Maths.Lerp(1/2.0, a0), 6.0);
			Assert.AreEqual(Maths.Lerp(1/3.0, a0), 10.0);
			Assert.AreEqual(Maths.Lerp(2/3.0, a0), 2.0);
			Assert.AreEqual(Maths.Lerp(1/6.0, a0), 5.5);
			Assert.AreEqual(Maths.Lerp(5/6.0, a0), 3.5);
		}
	}
}
#endif