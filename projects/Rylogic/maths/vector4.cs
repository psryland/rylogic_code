//***************************************************
// Vector4
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using pr.maths;

namespace pr.maths
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	public struct v4
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;
		[FieldOffset( 8)] public float z;
		[FieldOffset(12)] public float w;

		[FieldOffset( 0)] public v2 xy;
		[FieldOffset( 4)] public v2 yz;
		[FieldOffset( 8)] public v2 zw;

		// Integer cast accessors
		public int ix { get { return (int)x; } }
		public int iy { get { return (int)y; } }
		public int iz { get { return (int)z; } }
		public int iw { get { return (int)w; } }

		// Constructors
		public v4(float x_, float y_, float z_, float w_) :this() { x = x_; y = y_; z = z_; w = w_; }
		public v4(v2 xy_, float z_, float w_) :this()             { xy = xy_; z = z_; w = w_; }
		public v4(v2 xy_, v2 zw_) :this()                         { xy = xy_; zw = zw_; }

		public float this[int i]
		{
			get { switch(i){case 0:return x;case 1:return y;case 2:return z;case 3:return w;default: throw new ArgumentException("index out of range", "i");} }
			set { switch(i){case 0:x=value;break;case 1:y=value;break;case 2:z=value;break;case 3:w=value;break;default: throw new ArgumentException("index out of range", "i");} }
		}
		
		public void SetZero()                                   { x = y = z = w = 0f; }
		public void SetOrigin()                                 { x = y = z = 0f; w = 1f; }
		public void Set(float x_, float y_, float z_, float w_) { x = x_; y = y_; z = z_; w = w_; }
		public float Length2Sq                                  { get { return x * x + y * y; } }
		public float Length3Sq                                  { get { return x * x + y * y + z * z; } }
		public float Length4Sq                                  { get { return x * x + y * y + z * z + w * w; } }
		public float Length2                                    { get { return (float)Math.Sqrt(Length2Sq); } }
		public float Length3                                    { get { return (float)Math.Sqrt(Length3Sq); } }
		public float Length4                                    { get { return (float)Math.Sqrt(Length4Sq); } }
		public v4 AsPos                                         { get { v4 v = this; v.w = 1.0f; return v; } }
		public v4 AsDir                                         { get { v4 v = this; v.w = 0.0f; return v; } }
		public string ToString2()                               { return x + " " + y; }
		public string ToString2(string format)                  { return x.ToString(format) + " " + y.ToString(format); }
		public string ToString3()                               { return ToString2() + " " + z; }
		public string ToString3(string format)                  { return ToString2(format) + " " + z.ToString(format); }
		public string ToString4()                               { return ToString3() + " " + w; }
		public string ToString4(string format)                  { return ToString3(format) + " " + w.ToString(format); }
		public override string ToString()                       { return ToString4(); }

		public float[] ToArray()                                { return new[]{x, y, z, w}; }
		public static implicit operator v4(float[] a)           { return new v4(a[0], a[1], a[2], a[3]); }
		public static implicit operator float[](v4 p)           { return p.ToArray(); }

		public v4 w0 { get { return new v4(x, y, z, 0); } }
		public v4 w1 { get { return new v4(x, y, z, 1); } }

		// Static v4 types
		private readonly static v4 m_zero;
		private readonly static v4 m_xaxis;
		private readonly static v4 m_yaxis;
		private readonly static v4 m_zaxis;
		private readonly static v4 m_waxis;
		private readonly static v4 m_origin;
		private readonly static v4 m_one;
		private readonly static v4 m_min;
		private readonly static v4 m_max;
		static v4()
		{
			m_zero = new v4(0f, 0f, 0f, 0f);
			m_xaxis = new v4(1f, 0f, 0f, 0f);
			m_yaxis = new v4(0f, 1f, 0f, 0f);
			m_zaxis = new v4(0f, 0f, 1f, 0f);
			m_waxis = new v4(0f, 0f, 0f, 1f);
			m_origin = new v4(0f, 0f, 0f, 1f);
			m_one = new v4(1f, 1f, 1f, 1f);
			m_min = new v4(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
			m_max = new v4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
		}
		public static v4 Zero { get { return m_zero; } }
		public static v4 XAxis { get { return m_xaxis; } }
		public static v4 YAxis { get { return m_yaxis; } }
		public static v4 ZAxis { get { return m_zaxis; } }
		public static v4 WAxis { get { return m_waxis; } }
		public static v4 Origin { get { return m_origin; } }
		public static v4 One { get { return m_one; } }
		public static v4 Min { get { return m_min; } }
		public static v4 Max { get { return m_max; } }

		// Functions
		public static v4 operator + (v4 lhs, v4 rhs)           { return new v4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w); }
		public static v4 operator - (v4 lhs, v4 rhs)           { return new v4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w); }
		public static v4 operator * (v4 lhs, float rhs)        { return new v4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }
		public static v4 operator * (float lhs, v4 rhs)        { return new v4(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w); }
		public static v4 operator / (v4 lhs, float rhs)        { return new v4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs); }
		public static bool operator ==(v4 lhs, v4 rhs)         { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w; }
		public static bool operator !=(v4 lhs, v4 rhs)         { return !(lhs == rhs); }
		public static v4 operator + (v4 vec)                   { return vec; }
		public static v4 operator - (v4 vec)                   { return new v4(-vec.x, -vec.y, -vec.z, -vec.w); }
		public override bool Equals(object o)                  { return o is v4 && (v4)o == this; }
		public override int GetHashCode()                      { unchecked { return x.GetHashCode() ^ y.GetHashCode() ^ z.GetHashCode() ^ w.GetHashCode(); } }

		public static bool FEqlZero2(v4 vec, float tol)        { return vec.Length2Sq < tol * tol; }
		public static bool FEqlZero2(v4 vec)                   { return FEqlZero2(vec, Maths.TinyF); }
		public static bool FEqlZero3(v4 vec, float tol)        { return vec.Length3Sq < tol * tol; }
		public static bool FEqlZero3(v4 vec)                   { return FEqlZero3(vec, Maths.TinyF); }
		public static bool FEqlZero4(v4 vec, float tol)        { return vec.Length4Sq < tol * tol; }
		public static bool FEqlZero4(v4 vec)                   { return FEqlZero4(vec, Maths.TinyF); }

		public static bool FEql2(v4 lhs, v4 rhs, float tol)    { return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol); }
		public static bool FEql2(v4 lhs, v4 rhs)               { return FEql2(lhs, rhs, Maths.TinyF); }
		public static bool FEql3(v4 lhs, v4 rhs, float tol)    { return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol) && Maths.FEql(lhs.z, rhs.z, tol); }
		public static bool FEql3(v4 lhs, v4 rhs)               { return FEql3(lhs, rhs, Maths.TinyF); }
		public static bool FEql4(v4 lhs, v4 rhs, float tol)    { return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol) && Maths.FEql(lhs.z, rhs.z, tol) && Maths.FEql(lhs.w, rhs.w, tol); }
		public static bool FEql4(v4 lhs, v4 rhs)               { return FEql4(lhs, rhs, Maths.TinyF); }

		public static v4 Random4(float min, float max, Rand r) { r = r ?? new Rand(); return new v4(r.Float(min,max)     ,r.Float(min,max)     ,r.Float(min,max)     ,r.Float(min,max)    ); }
		public static v4 Random4(v4 min, v4 max, Rand r)       { r = r ?? new Rand(); return new v4(r.Float(min.x,max.x) ,r.Float(min.y,max.y) ,r.Float(min.z,max.z) ,r.Float(min.w,max.w)); }
		public static v4 Random4(float rad, Rand r)            { r = r ?? new Rand(); var rad_sq = rad*rad; v4 v; for (; (v = Random4(-rad, rad, r)).Length4Sq > rad_sq; ){} return v; }
		public static v4 Random4N(Rand r)                      { r = r ?? new Rand(); return v4.Random4(1.0f, r); }

		public static v4 Random3(float min, float max, float w, Rand r) { r = r ?? new Rand(); return new v4(r.Float(min,max)     ,r.Float(min,max)     ,r.Float(min,max)     ,w); }
		public static v4 Random3(v4 min, v4 max, float w, Rand r)       { r = r ?? new Rand(); return new v4(r.Float(min.x,max.x) ,r.Float(min.y,max.y) ,r.Float(min.z,max.z) ,w); }
		public static v4 Random3(float rad, float w, Rand r)            { r = r ?? new Rand(); var rad_sq = rad*rad; v4 v; for (; (v = Random3(-rad, rad, w, r)).Length3Sq > rad_sq; ){} return v; }
		public static v4 Random3N(float w, Rand r)                      { r = r ?? new Rand(); return v4.Random3(1.0f, w, r); }

		public static v4 Random2(float min, float max, float z, float w, Rand r) { r = r ?? new Rand(); return new v4(r.Float(min,max)     ,r.Float(min,max)     ,z ,w); }
		public static v4 Random2(v4 min, v4 max, float z, float w, Rand r)       { r = r ?? new Rand(); return new v4(r.Float(min.x,max.x) ,r.Float(min.y,max.y) ,z ,w); }
		public static v4 Random2(float rad, float z, float w, Rand r)            { r = r ?? new Rand(); var rad_sq = rad*rad; v4 v; for (; (v = Random2(-rad, rad, z, w, r)).Length2Sq > rad_sq; ){} return v; }
		public static v4 Random2N(float z, float w, Rand r)                      { r = r ?? new Rand(); return v4.Random2(1.0f, z, w, r); }
		
		public static v4 Clamp3(v4 vec, v4 min, v4 max)        { return new v4(Maths.Clamp(vec.x, min.x, max.x), Maths.Clamp(vec.y, min.y, max.y), Maths.Clamp(vec.z, min.z, max.z), vec.w); }
		public static v4 Clamp4(v4 vec, v4 min, v4 max)        { return new v4(Maths.Clamp(vec.x, min.x, max.x), Maths.Clamp(vec.y, min.y, max.y), Maths.Clamp(vec.z, min.z, max.z), Maths.Clamp(vec.w, min.w, max.w)); }
		
		public static v4    Abs(v4 vec)                        { return new v4(Math.Abs(vec.x), Math.Abs(vec.y), Math.Abs(vec.z), Math.Abs(vec.w)); }
		public static v4    Blend(v4 lhs, v4 rhs, float frac)  { return lhs * (1f - frac) + rhs * (frac); }
		public static v4    Normalise2(v4 vec)                 { return vec / vec.Length2; }
		public static v4    Normalise3(v4 vec)                 { return vec / vec.Length3; }
		public static v4    Normalise4(v4 vec)                 { return vec / vec.Length4; }
		public static v4    Normalise2(v4 vec, v4 def)         { return Maths.FEql(vec.Length2Sq, 0f) ? def : vec / vec.Length2; }
		public static v4    Normalise3(v4 vec, v4 def)         { return Maths.FEql(vec.Length3Sq, 0f) ? def : vec / vec.Length3; }
		public static v4    Normalise4(v4 vec, v4 def)         { return Maths.FEql(vec.Length4Sq, 0f) ? def : vec / vec.Length4; }
		public static v4    Normalise2(ref v4 vec)             { return vec /= vec.Length2; }
		public static v4    Normalise3(ref v4 vec)             { return vec /= vec.Length3; }
		public static v4    Normalise4(ref v4 vec)             { return vec /= vec.Length4; }
		public static float Dot3(v4 lhs, v4 rhs)               { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z; }
		public static float Dot4(v4 lhs, v4 rhs)               { return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w; }
		public static v4    Cross3(v4 lhs, v4 rhs)             { return new v4(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x, 0.0f); }
		public static float Triple3(v4 a, v4 b, v4 c)          { return Dot3(a, Cross3(b, c)); }
		public static v4    ComponentDivide(v4 lhs, v4 rhs)    { return new v4(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w); }
		public static bool  Parallel(v4 lhs, v4 rhs, float tol){ return Cross3(lhs, rhs).Length3Sq <= tol; }
		public static bool  Parallel(v4 lhs, v4 rhs)           { return Parallel(lhs, rhs, Maths.TinyF); }

		/// <summary>Linearly interpolate between two vectors</summary>
		public static v4 Lerp(v4 lhs, v4 rhs, float frac)
		{
			return new v4(
				Maths.Lerp(lhs.x,rhs.x,frac),
				Maths.Lerp(lhs.y,rhs.y,frac),
				Maths.Lerp(lhs.z,rhs.z,frac),
				Maths.Lerp(lhs.w,rhs.w,frac));
		}

		public static v4 CreateNotParallelTo(v4 vec)
		{
			bool x_aligned = Maths.Abs(vec.x) > Maths.Abs(vec.y) && Maths.Abs(vec.x) > Maths.Abs(vec.z);
			return new v4(Maths.SignF(!x_aligned), 0.0f, Maths.SignF(x_aligned), vec.w);
		}
		public static v4 Perpendicular(v4 vec)
		{
			Debug.Assert(!FEqlZero3(vec), "Cannot make a perpendicular to a zero vector");
			v4 v = Cross3(vec, CreateNotParallelTo(vec));
			v *= vec.Length3 / v.Length3;
			return v;
		}
		public static uint Octant(v4 vec)
		{
			// Returns a 3 bit bitmask of the octant the vector is in where X = 0x1, Y = 0x2, Z = 0x4    
			return (uint)(((int)Maths.SignF(vec.x >= 0.0f)) | ((int)Maths.SignF(vec.y >= 0.0f) << 1) | ((int)Maths.SignF(vec.z >= 0.0f) << 2));
		}
		public static v4 Rotate(v4 vec, v4 axis_norm, float angle)
		{
			// Rotate 'vec' about 'axis_norm' by 'angle'
			return m3x4.Rotation(axis_norm, angle) * vec;
		}
		public static float CosAngle3(v4 lhs, v4 rhs)
		{
			// Return the cosine of the angle between two vectors
			Debug.Assert(lhs.Length3Sq != 0 && rhs.Length3Sq != 0, "CosAngle undefined for zero vectors");
			return Maths.Clamp(Dot3(lhs,rhs) / (float)Math.Sqrt(lhs.Length3Sq * rhs.Length3Sq), -1f, 1f);
		}
		public static v4 Parse3(string s, float w)
		{
			if (s == null) throw new ArgumentNullException("s", "v4.Parse3() string argument was null");
			string[] values = s.Split(new char[]{' ',',','\t'},3);
			if (values.Length != 3) throw new FormatException("v4.Parse3() string argument does not represent a 3 component vector");
			return new v4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), w);
		}
		public static v4 Parse4(string s)
		{
			if (s == null) throw new ArgumentNullException("s", "v4.Parse4() string argument was null");
			string[] values = s.Split(new char[]{' ',',','\t'},4);
			if (values.Length != 4) throw new FormatException("v4.Parse4() string argument does not represent a 4 component vector");
			return new v4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
		}
		public static bool TryParse3(string s, out v4 vec, float w)
		{
			vec = Zero;
			if (s == null) return false;
			string[] values = s.Split(new char[]{' ',',','\t'},3);
			vec.w = w;
			return values.Length == 3 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y) && float.TryParse(values[2], out vec.z);
		}
		public static bool TryParse4(string s, out v4 vec)
		{
			vec = Zero;
			if (s == null) return false;
			string[] values = s.Split(new char[]{' ',',','\t'},4);
			return  values.Length == 4 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y) && float.TryParse(values[2], out vec.z) && float.TryParse(values[3], out vec.w);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestVec4
	{
		[Test] public void TestV4()
		{
			Assert.AreEqual(new v4(1,2,3,4), v4.Parse4("1 2,3\t4"));
		}
	}
}
#endif
