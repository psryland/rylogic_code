//***************************************************
// Vector2
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace pr.maths
{
	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public struct v2
	{
		public float x, y;

		// Integer cast accessors
		public int ix { get { return (int)x; } }
		public int iy { get { return (int)y; } }

		// Constructors
		public v2(float x_, float y_) { x = x_; y = y_; }
		public v2(PointF pt) { x = pt.X; y = pt.Y; }
		public v2(SizeF sz)  { x = sz.Width; y = sz.Height; }

		public float this[int i]
		{
			get {switch (i) { case 0:return x;      case 1:return y;      default: throw new ArgumentException("index out of range", "i");} }
			set {switch (i) { case 0:x=value;break; case 1:y=value;break; default: throw new ArgumentException("index out of range", "i");} }
		}

		public void SetZero() { x = y = 0f; }
		public void Set(float x_, float y_) { x = x_; y = y_; }
		public float Length2Sq { get { return x * x + y * y; } }
		public float Length2 { get { return (float)Math.Sqrt(Length2Sq); } }
		public override string ToString() { return x + " " + y; }

		// Static v2 types
		private readonly static v2 m_zero;
		private readonly static v2 m_xaxis;
		private readonly static v2 m_yaxis;
		private readonly static v2 m_one;
		private readonly static v2 m_min;
		private readonly static v2 m_max;
		static v2()
		{
			m_zero = new v2(0f, 0f);
			m_xaxis = new v2(1f, 0f);
			m_yaxis = new v2(0f, 1f);
			m_one = new v2(1f, 1f);
			m_min = new v2(float.MinValue, float.MinValue);
			m_max = new v2(float.MaxValue, float.MaxValue);
		}
		public static v2 Zero { get { return m_zero; } }
		public static v2 XAxis { get { return m_xaxis; } }
		public static v2 YAxis { get { return m_yaxis; } }
		public static v2 One { get { return m_one; } }
		public static v2 Min { get { return m_min; } }
		public static v2 Max { get { return m_max; } }

		public static v2 From(Point point)                     { return new v2(point.X, point.Y); }
		public static v2 From(Size size)                       { return new v2(size.Width, size.Height); }
		public static v2 From(PointF point)                    { return new v2(point.X, point.Y); }
		public static v2 From(SizeF size)                      { return new v2(size.Width, size.Height); }

		// Functions
		public static v2   operator + (v2 lhs, v2 rhs)         { return new v2(lhs.x + rhs.x, lhs.y + rhs.y); }
		public static v2   operator - (v2 lhs, v2 rhs)         { return new v2(lhs.x - rhs.x, lhs.y - rhs.y); }
		public static v2   operator * (v2 lhs, float rhs)      { return new v2(lhs.x * rhs, lhs.y * rhs); }
		public static v2   operator * (float lhs, v2 rhs)      { return new v2(lhs * rhs.x, lhs * rhs.y); }
		public static v2   operator / (v2 lhs, float rhs)      { return new v2(lhs.x / rhs, lhs.y / rhs); }
		public static bool operator ==(v2 lhs, v2 rhs)         { return lhs.x == rhs.x && lhs.y == rhs.y; }
		public static bool operator !=(v2 lhs, v2 rhs)         { return !(lhs == rhs); }
		public static v2 operator - (v2 vec)                   { return new v2(-vec.x, -vec.y); }
		public override bool Equals(object o)                  { return o is v2 && (v2)o == this; }
		public override int  GetHashCode()                     { unchecked { return x.GetHashCode() ^ y.GetHashCode(); } }

		// Conversion
		public static implicit operator v2(PointF p)           { return new v2(p.X, p.Y); }
		public static implicit operator PointF(v2 p)           { return new PointF(p.x, p.y); }
		public static implicit operator v2(SizeF s)           { return new v2(s.Width, s.Height); }
		public static implicit operator SizeF(v2 s)           { return new SizeF(s.x, s.y); }

		public static bool  FEqlZero2(v2 vec, float tol)        { return vec.Length2Sq < tol * tol; }
		public static bool  FEqlZero2(v2 vec)                   { return FEqlZero2(vec, Maths.TinyF); }
		public static bool  FEql2(v2 lhs, v2 rhs, float tol)    { return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol); }
		public static bool  FEql2(v2 lhs, v2 rhs)               { return FEql2(lhs, rhs, Maths.TinyF); }
		public static v2    Random2(Rand r, float z, float w)   { return new v2(r.Float(), r.Float()); }
		public static v2    Random2(float z, float w)           { return Random2(new Rand(), z, w); }

		public static v2    Abs(v2 vec)                        { return new v2(Math.Abs(vec.x), Math.Abs(vec.y)); }
		public static v2    Blend(v2 lhs, v2 rhs, float frac)  { return lhs * (1f - frac) + rhs * (frac); }
		public static v2    Clamp2(v2 vec, v2 min, v2 max)     { return new v2(Maths.Clamp(vec.x, min.x, max.x), Maths.Clamp(vec.y, min.y, max.y)); }
		public static v2    Normalise2(v2 vec)                 { return vec / vec.Length2; }
		public static v2    Normalise2(ref v2 vec)             { return vec /= vec.Length2; }
		public static float Dot2(v2 lhs, v2 rhs)               { return lhs.x * rhs.x + lhs.y * rhs.y; }
		public static v2    ComponentDivide(v2 lhs, v2 rhs)    { return new v2(lhs.x / rhs.x, lhs.y / rhs.y); }
		public static v2    RotateCCW(v2 vec)                  { return new v2(-vec.y, vec.x); }
		public static v2    RotateCW(v2 vec)                   { return new v2(vec.y, -vec.x); }
		public static float CosAngle2(v2 lhs, v2 rhs)
		{
			// Return the cosine of the angle between two vectors
			Debug.Assert(lhs.Length2Sq != 0 && rhs.Length2Sq != 0, "CosAngle undefined for zero vectors");
			return Maths.Clamp(Dot2(lhs,rhs) / (float)Math.Sqrt(lhs.Length2Sq * rhs.Length2Sq), -1f, 1f);
		}
		public static v2 Parse(string s)
		{
			if (s == null) throw new ArgumentNullException("s", "v2.Parse() string argument was null");
			string[] values = s.Split(new char[]{' ',',','\t'},2);
			if (values.Length != 2) throw new FormatException("v2.Parse() string argument does not represent a 2 component vector");
			return new v2(float.Parse(values[0]), float.Parse(values[1]));
		}
		public static bool TryParse(string s, out v2 vec)
		{
			vec = Zero;
			if (s == null) return false;
			string[] values = s.Split(new char[]{' ',',','\t'},2);
			return values.Length == 2 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y);
		}
	}
}