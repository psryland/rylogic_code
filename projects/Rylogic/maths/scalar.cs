//***************************************************
// Scalar Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	/// <summary>scalar functions</summary>
	public static partial class Maths
	{
		public const float Tiny      = 1.000000e-4F; // Can't go lower than this cos DX uses less precision
		public const float Tau       = 6.283185307179586476925286766559e+0F; // circle constant
		public const float TauBy360  = 0.01745329251994329576923690768489e+0F;
		public const float _360ByTau = 57.295779513082320876798154814105e+0F;
		public const float Phi       = 1.618034e+0F; // "Golden Ratio"
		public const float InvTau    = 0.15915494309189533576888376337251e+0F;
		public const float TauBy2    = 3.1415926535897932384626433832795e+0F;
		public const float TauBy3    = 2.0943951023931954923084289221863e+0F;
		public const float TauBy4    = 1.5707963267948966192313216916398e+0F;
		public const float TauBy5    = 1.2566370614359172953850573533118e+0F;
		public const float TauBy6    = 1.047198551196597746154214461093e+0F;
		public const float TauBy7    = 0.897597901025655210989326680937e+0F;
		public const float TauBy8    = 0.78539816339744830961566084581988e+0F;
		public const float TauBy10   = 0.6283185307179586476925286766559e+0F;
		public const float TauBy16   = 0.39269908169872415480783042290994e+0F;
		public const float TauBy32   = 0.19634954084936207740391521145497e+0F;
		public const float Root2     = 1.4142135623730950488016887242097e+0F;
		public const float Root3     = 1.7320508075688772935274463415059e+0F;

		public static float     TinyF                                           { get { return Tiny; } }
		public static double    TinyD                                           { get { return Tiny; } }
		public static bool      IsFinite(float x)                               { return !float.IsInfinity(x) && !float.IsNaN(x); }
		public static bool      IsFinite(double x)                              { return !double.IsInfinity(x) && !double.IsNaN(x); }
		public static bool      FEql(float lhs, float rhs, float tol)           { return Math.Abs(lhs - rhs) < tol; }
		public static bool      FEql(float lhs, float rhs)                      { return FEql(lhs, rhs, TinyF); }
		public static bool      FEql(double lhs, double rhs, double tol)        { return Math.Abs(lhs - rhs) < tol; }
		public static bool      FEql(double lhs, double rhs)                    { return FEql(lhs, rhs, TinyD); }
		public static int       SignI(bool positive)                            { return positive ? 1 : -1; }
		public static int       SignI(int x)                                    { return SignI(x >= 0.0f); }
		public static float     SignF(bool positive)                            { return positive ? 1f : -1f; }
		public static float     SignF(float x)                                  { return SignF(x >= 0.0f); }
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
		public static float     Min(float lhs, float rhs)                       { return (lhs < rhs) ? lhs : rhs; }
		public static float     Max(float lhs, float rhs)                       { return (lhs > rhs) ? lhs : rhs; }
		public static double    Min(double lhs, double rhs)                     { return (lhs < rhs) ? lhs : rhs; }
		public static double    Max(double lhs, double rhs)                     { return (lhs > rhs) ? lhs : rhs; }
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
		public static float     DegreesToRadians(float degrees)                 { return degrees * TauBy360; }
		public static double    DegreesToRadians(double degrees)                { return degrees * TauBy360; }
		public static float     RadiansToDegrees(float radians)                 { return radians * _360ByTau; }
		public static double    RadiansToDegrees(double radians)                { return radians * _360ByTau; }
		public static void      Swap(ref int lhs, ref int rhs)                  { int tmp = lhs; lhs = rhs; rhs = tmp; }
		public static void      Swap(ref float lhs, ref float rhs)              { float tmp = lhs; lhs = rhs; rhs = tmp; }
		public static float     Lerp(float lhs, float rhs, float frac)          { return lhs * (1f - frac) + rhs * (frac); }
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

		/// <summary>Returns the angle of the corner in a triangle with side lengths a,b,c for the corner opposite 'c'.</summary>
		public static double Angle(double a, double b, double c)
		{
			var numer = Sqr(a) + Sqr(b) - Sqr(c);
			var denom = 2 * a * b;
			var cos_angle = Clamp(denom != 0 ? numer / denom : 1, -1, 1);
			return Math.Acos(cos_angle);
		}
	}
}