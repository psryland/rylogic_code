//***************************************************
// Scalar Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

		public static bool      IsFinite(float x)                               { return !float.IsInfinity(x) && !float.IsNaN(x); }
		public static bool      IsFinite(double x)                              { return !double.IsInfinity(x) && !double.IsNaN(x); }
		public static int       SignI(bool positive)                            { return positive ? 1 : -1; }
		public static float     SignF(bool positive)                            { return positive ? 1f : -1f; }
		public static double    SignD(bool positive)                            { return positive ? 1.0 : -1.0; }
		public static int       Sign(int x)                                     { return SignI(x >= 0.0f); }
		public static float     Sign(float x)                                   { return SignF(x >= 0.0f); }
		public static double    Sign(double x)                                  { return SignD(x >= 0.0); }
		public static int       OneIfZero(int x)                                { return x != 0 ? x : 1; }
		public static float     OneIfZero(float x)                              { return x != 0f ? x : 1f; }
		public static double    Abs(double d)                                   { return Math.Abs(d); }
		public static float     Sqr(float x)                                    { return x * x; }
		public static double    Sqr(double x)                                   { return x * x; }
		public static float     Sqrt(float x)                                   { return (float)Sqrt((double)x); }
		public static double    Sqrt(double x)                                  { return Math.Sqrt(x); }
		public static float     Cubed(float x)                                  { return x * x * x; }
		public static double    Cubed(double x)                                 { return x * x * x; }
		public static float     CubeRoot(float x)                               { return (float)CubeRoot((double)x); }
		public static double    CubeRoot(double x)                              { return Math.Pow(x, 1.0/3.0); }
		public static char      Clamp(char x, char min, char max)               { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static byte      Clamp(byte x, byte min, byte max)               { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static short     Clamp(short x, short min, short max)            { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static ushort    Clamp(ushort x, ushort min, ushort max)         { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static int       Clamp(int x, int min, int max)                  { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static uint      Clamp(uint x, uint min, uint max)               { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static long      Clamp(long x, long min, long max)               { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static ulong     Clamp(ulong x, ulong min, ulong max)            { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static float     Clamp(float x, float min, float max)            { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static double    Clamp(double x, double min, double max)         { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static decimal   Clamp(decimal x, decimal min, decimal max)      { Debug.Assert(min <= max); return (x > max) ? max : (x < min) ? min : x; }
		public static float     DegreesToRadians(float degrees)                 { return (float)(degrees * TauBy360); }
		public static double    DegreesToRadians(double degrees)                { return degrees * TauBy360; }
		public static v4        DegreesToRadians(v4 degrees)                    { return new v4(DegreesToRadians(degrees.x), DegreesToRadians(degrees.y), DegreesToRadians(degrees.z), DegreesToRadians(degrees.w)); }
		public static float     RadiansToDegrees(float radians)                 { return (float)(radians * _360ByTau); }
		public static double    RadiansToDegrees(double radians)                { return radians * _360ByTau; }
		public static v4        RadiansToDegrees(v4 degrees)                    { return new v4(RadiansToDegrees(degrees.x), RadiansToDegrees(degrees.y), RadiansToDegrees(degrees.z), RadiansToDegrees(degrees.w)); }
		public static void      Swap(ref int lhs, ref int rhs)                  { var tmp = lhs; lhs = rhs; rhs = tmp; }
		public static void      Swap(ref long lhs, ref long rhs)                { var tmp = lhs; lhs = rhs; rhs = tmp; }
		public static void      Swap(ref float lhs, ref float rhs)              { var tmp = lhs; lhs = rhs; rhs = tmp; }
		public static void      Swap(ref double lhs, ref double rhs)            { var tmp = lhs; lhs = rhs; rhs = tmp; }
		public static int       Lerp(int lhs, int rhs, double frac)             { return (int)Math.Round(Lerp((float)lhs, (float)rhs, frac), 0); }
		public static long      Lerp(long lhs, long rhs, double frac)           { return (long)Math.Round(Lerp((double)lhs, (double)rhs, frac), 0); }
		public static float     Lerp(float lhs, float rhs, double frac)         { return (float)Lerp((double)lhs, (double)rhs, frac); }
		public static double    Lerp(double lhs, double rhs, double frac)       { return lhs * (1.0 - frac) + rhs * (frac); }
		public static float     Frac(int min, int x, int max)                   { Debug.Assert(max != min); return (float)(x - min) / (max - min); }
		public static double    Frac(long min, long x, long max)                { Debug.Assert(max != min); return (double)(x - min) / (max - min); }
		public static float     Frac(float min, float x, float max)             { Debug.Assert(Math.Abs(max - min) > float .Epsilon); return (x - min) / (max - min); }
		public static double    Frac(double min, double x, double max)          { Debug.Assert(Math.Abs(max - min) > double.Epsilon); return (x - min) / (max - min); }
		public static int       Compare(int lhs, int rhs)                       { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(uint lhs, uint rhs)                     { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(long lhs, long rhs)                     { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(float lhs, float rhs)                   { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(double lhs, double rhs)                 { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static float     Len2Sq(float x, float y)                        { return Sqr(x) + Sqr(y); }
		public static float     Len2(float x, float y)                          { return Sqrt(Len2Sq(x,y)); }
		public static float     Len3Sq(float x, float y, float z)               { return Sqr(x) + Sqr(y) + Sqr(z); }
		public static float     Len3(float x, float y, float z)                 { return Sqrt(Len3Sq(x,y,z)); }

		// Floating point comparisons
		public static bool FEql(float a, float b, float tol = TinyF)
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
		public static bool FEql(double a, double b, double tol = TinyD)
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

		/// <summary>Divide 'a' by 'b' if 'b' is not equal to zero, otherwise return 0</summary>
		public static int Div(int a, int b, int def = 0)
		{
			return b != 0 ? a/b : def;
		}
		public static float Div(float a, float b, float def = 0)
		{
			return b != 0 ? a/b : def;
		}
		public static double Div(double a, double b, double def = 0)
		{
			return b != 0 ? a/b : def;
		}

		/// <summary>Minimum value</summary>
		public static T Min<T>(T lhs, T rhs) where T :struct, IComparable<T>
		{
			return lhs.CompareTo(rhs) <= 0 ? lhs : rhs;
		}
		public static T Min<T>(T lhs, params T[] rhs) where T :struct, IComparable<T>
		{
			foreach (var r in rhs) lhs = Min(lhs, r);
			return lhs;
		}

		/// <summary>Maximum value</summary>
		public static T Max<T>(T lhs, T rhs) where T :struct, IComparable<T>
		{
			return lhs.CompareTo(rhs) >= 0 ? lhs : rhs;
		}
		public static T Max<T>(T lhs, params T[] rhs) where T :struct, IComparable<T>
		{
			foreach (var r in rhs) lhs = Max(lhs, r);
			return lhs;
		}
		//public static float     Min(float lhs, float rhs)                       { return (lhs < rhs) ? lhs : rhs; }
		//public static float     Max(float lhs, float rhs)                       { return (lhs > rhs) ? lhs : rhs; }
		//public static float     Min(float lhs, params float[] rhs)              { foreach (var r in rhs) lhs = Min(lhs, r); return lhs; }
		//public static float     Max(float lhs, params float[] rhs)              { foreach (var r in rhs) lhs = Max(lhs, r); return lhs; }
		//public static double    Min(double lhs, double rhs)                     { return (lhs < rhs) ? lhs : rhs; }
		//public static double    Max(double lhs, double rhs)                     { return (lhs > rhs) ? lhs : rhs; }
		//public static double    Min(double lhs, params double[] rhs)            { foreach (var r in rhs) lhs = Min(lhs, r); return lhs; }
		//public static double    Max(double lhs, params double[] rhs)            { foreach (var r in rhs) lhs = Max(lhs, r); return lhs; }

		/// <summary>True if 'x' is within the interval '[min-tol,max+tol]'</summary>
		public static bool Within(int min, int x, int max, int tol = 0)
		{
			min -= tol;
			max += tol;
			return x >= min && x <= max;
		}
		public static bool Within(float min, float x, float max, float tol = 0f)
		{
			min -= tol;
			max += tol;
			return x >= min && x <= max;
		}
		public static bool Within(double min, double x, double max, double tol = 0.0)
		{
			min -= tol;
			max += tol;
			return x >= min && x <= max;
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
			return (int)(x*scale) / (float)(scale);
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