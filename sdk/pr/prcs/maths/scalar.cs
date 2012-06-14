//***************************************************
// Scalar Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	/// <summary>
	/// scalar functions
	/// </summary>
	public static class Maths
	{
		public const float m_tiny       = 1.000000e-4F; // Can't go lower than this cos DX uses less precision
		public const float m_tau        = 6.283185e+0F; // circle constant
		public const float m_tau_by_360 = 1.745329e-2F;
		public const float m_360_by_tau = 5.729578e+1F;
		public const float m_phi        = 1.618034e+0F; // "Golden Ratio"
		public const float m_inv_tau    = 1.591549e-1F;
		public const float m_tau_by_2   = 3.141593e+0F;
		public const float m_tau_by_4   = 1.570796e+0F;
		public const float m_tau_by_8   = 7.853982e-1F;
		public const float m_tau_by_16  = 3.926991e-1F;

		public static float     TinyF                                           { get { return m_tiny; } }
		public static double    TinyD                                           { get { return m_tiny; } }
		public static bool      FEql(float lhs, float rhs, float tol)           { return Math.Abs(lhs - rhs) < tol; }
		public static bool      FEql(float lhs, float rhs)                      { return FEql(lhs, rhs, TinyF); }
		public static bool      FEql(double lhs, double rhs, double tol)        { return Math.Abs(lhs - rhs) < tol; }
		public static bool      FEql(double lhs, double rhs)                    { return FEql(lhs, rhs, TinyD); }
		public static int       SignI(bool positive)                            { return positive ? 1 : -1; }
		public static int       SignI(int x)                                    { return SignI(x >= 0.0f); }
		public static float     SignF(bool positive)                            { return positive ? 1f : -1f; }
		public static float     SignF(float x)                                  { return SignF(x >= 0.0f); }
		public static double    Abs(double d)                                   { return Math.Abs(d); }
		public static float     Sqr(float x)                                    { return x * x; }
		public static double    Sqr(double x)                                   { return x * x; }
		public static float     Sqrt(float x)                                   { return (float)Math.Sqrt(x); }
		public static double    Sqrt(double x)                                  { return Math.Sqrt(x); }
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
		public static float     DegreesToRadians(float degrees)                 { return degrees * m_tau_by_360; }
		public static double    DegreesToRadians(double degrees)                { return degrees * m_tau_by_360; }
		public static float     RadiansToDegrees(float radians)                 { return radians * m_360_by_tau; }
		public static double    RadiansToDegrees(double radians)                { return radians * m_360_by_tau; }
		public static void      Swap(ref int lhs, ref int rhs)                  { int tmp = lhs; lhs = rhs; rhs = tmp; }
		public static void      Swap(ref float lhs, ref float rhs)              { float tmp = lhs; lhs = rhs; rhs = tmp; }
		public static float     Lerp(float lhs, float rhs, float frac)          { return lhs * (1f - frac) + rhs * (frac); }
		public static double    Lerp(double lhs, double rhs, double frac)       { return lhs * (1.0 - frac) + rhs * (frac); }
		public static float     Ratio(int min, int x, int max)                  { Debug.Assert(max != min); return (float)(x - min) / (max - min); }
		public static float     Ratio(float min, float x, float max)            { Debug.Assert(Math.Abs(max - min) > float .Epsilon); return (x - min) / (max - min); }
		public static double    Ratio(double min, double x, double max)         { Debug.Assert(Math.Abs(max - min) > double.Epsilon); return (x - min) / (max - min); }
		
		public static int       Compare(int lhs, int rhs)                       { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(uint lhs, uint rhs)                     { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(long lhs, long rhs)                     { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(float lhs, float rhs)                   { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
		public static int       Compare(double lhs, double rhs)                 { return (lhs < rhs) ? -1 : (lhs > rhs) ? 1 : 0; }
	}
}