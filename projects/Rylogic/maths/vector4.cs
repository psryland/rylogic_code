//***************************************************
// Vector4
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using pr.extn;

namespace pr.maths
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{x}  {y}  {z}  {w}  // Len3={Length3}  Len4={Length4}")]
	public struct v4
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;
		[FieldOffset( 8)] public float z;
		[FieldOffset(12)] public float w;

		[FieldOffset( 0)] public v2 xy;
		[FieldOffset( 4)] public v2 yz;
		[FieldOffset( 8)] public v2 zw;

		[FieldOffset( 0)] public v3 xyz;
		
		// Constructors
		public v4(float x_) :this(x_,x_,x_,x_)
		{}
		public v4(float x_, float y_, float z_, float w_) :this()
		{
			x = x_;
			y = y_;
			z = z_;
			w = w_;
		}
		public v4(v2 xy_, float z_, float w_) :this()
		{
			xy = xy_;
			z = z_;
			w = w_;
		}
		public v4(v2 xy_, v2 zw_) :this()
		{
			xy = xy_;
			zw = zw_;
		}
		public v4(float[] arr) :this()
		{
			x = arr[0];
			y = arr[1];
			z = arr[2];
			w = arr[3];
		}
		public v4(float[] arr, float w_) :this()
		{
			x = arr[0];
			y = arr[1];
			z = arr[2];
			w = w_;
		}

		/// <summary>Get/Set components by index</summary>
		public float this[int i]
		{
			get
			{
				switch (i) {
				case 0: return x;
				case 1: return y;
				case 2: return z;
				case 3: return w;
				}
				throw new ArgumentException("index out of range", "i");
			}
			set
			{
				switch (i) {
				case 0: x = value; return;
				case 1: y = value; return;
				case 2: z = value; return;
				case 3: w = value; return;
				}
				throw new ArgumentException("index out of range", "i");
			}
		}
		public float this[uint i]
		{
			get { return this[(int)i]; }
			set { this[(int)i] = value; }
		}

		/// <summary>Integer cast accessors</summary>
		public int ix
		{
			get { return (int)x; }
		}
		public int iy
		{
			get { return (int)y; }
		}
		public int iz
		{
			get { return (int)z; }
		}
		public int iw
		{
			get { return (int)w; }
		}

		/// <summary>Length</summary>
		public float Length2Sq
		{
			get { return x * x + y * y; }
		}
		public float Length3Sq
		{
			get { return x * x + y * y + z * z; }
		}
		public float Length4Sq
		{
			get { return x * x + y * y + z * z + w * w; }
		}
		public float Length2
		{
			get { return (float)Math.Sqrt(Length2Sq); }
		}
		public float Length3
		{
			get { return (float)Math.Sqrt(Length3Sq); }
		}
		public float Length4
		{
			get { return (float)Math.Sqrt(Length4Sq); }
		}

		/// <summary>ToString</summary>
		public string ToString2()
		{
			return x + " " + y;
		}
		public string ToString3()
		{
			return ToString2() + " " + z;
		}
		public string ToString4()
		{
			return ToString3() + " " + w;
		}
		public string ToString2(string format)
		{
			return x.ToString(format) + " " + y.ToString(format);
		}
		public string ToString3(string format)
		{
			return ToString2(format) + " " + z.ToString(format);
		}
		public string ToString4(string format)
		{
			return ToString3(format) + " " + w.ToString(format);
		}
		public override string ToString()
		{
			return ToString4();
		}

		/// <summary>ToArray(). Note not implicit because it gets called when converting v4 to an object type. e.g. v4? x = v4.TryParse4("", out v) ? v : null. </summary>
		public float[] ToArray()
		{
			return new[] { x, y, z, w };
		}
		// 
		//public static implicit operator v4(float[] a)           { return new v4(a[0], a[1], a[2], a[3]); }
		//public static implicit operator float[](v4 p)           { return p.ToArray(); }


		public v4 w0
		{
			get { return new v4(x, y, z, 0); }
		}
		public v4 w1
		{
			get { return new v4(x, y, z, 1); }
		}

		// Static v4 types
		public readonly static v4 Zero     = new v4(0f, 0f, 0f, 0f);
		public readonly static v4 XAxis    = new v4(1f, 0f, 0f, 0f);
		public readonly static v4 YAxis    = new v4(0f, 1f, 0f, 0f);
		public readonly static v4 ZAxis    = new v4(0f, 0f, 1f, 0f);
		public readonly static v4 WAxis    = new v4(0f, 0f, 0f, 1f);
		public readonly static v4 Origin   = new v4(0f, 0f, 0f, 1f);
		public readonly static v4 One      = new v4(1f, 1f, 1f, 1f);
		public readonly static v4 MinValue = new v4(float.MinValue, float.MinValue, float.MinValue, float.MinValue);
		public readonly static v4 MaxValue = new v4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);

		/// <summary>Operators</summary>
		public static v4 operator + (v4 vec)
		{
			return vec;
		}
		public static v4 operator - (v4 vec)
		{
			return new v4(-vec.x, -vec.y, -vec.z, -vec.w);
		}
		public static v4 operator + (v4 lhs, v4 rhs)
		{
			return new v4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
		}
		public static v4 operator - (v4 lhs, v4 rhs)
		{
			return new v4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);
		}
		public static v4 operator * (v4 lhs, float rhs)
		{
			return new v4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs);
		}
		public static v4 operator * (float lhs, v4 rhs)
		{
			return new v4(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w);
		}
		public static v4 operator * (v4 lhs, v4 rhs)
		{
			// Not really sound mathematically, but too useful not to have
			return new v4(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z, lhs.w * rhs.w);
		}
		public static v4 operator / (v4 lhs, float rhs)
		{
			return new v4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs);
		}
		public static v4 operator / (v4 lhs, v4 rhs)
		{
			return new v4(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z, lhs.w / rhs.w);
		}

		/// <summary>Approximate equal</summary>
		public static bool FEql2(v4 lhs, v4 rhs, float tol)
		{
			return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol);
		}
		public static bool FEql3(v4 lhs, v4 rhs, float tol)
		{
			return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol) && Maths.FEql(lhs.z, rhs.z, tol);
		}
		public static bool FEql4(v4 lhs, v4 rhs, float tol)
		{
			return Maths.FEql(lhs.x, rhs.x, tol) && Maths.FEql(lhs.y, rhs.y, tol) && Maths.FEql(lhs.z, rhs.z, tol) && Maths.FEql(lhs.w, rhs.w, tol);
		}
		public static bool FEql2(v4 lhs, v4 rhs)
		{
			return FEql2(lhs, rhs, Maths.TinyF);
		}
		public static bool FEql3(v4 lhs, v4 rhs)
		{
			return FEql3(lhs, rhs, Maths.TinyF);
		}
		public static bool FEql4(v4 lhs, v4 rhs)
		{
			return FEql4(lhs, rhs, Maths.TinyF);
		}

		/// <summary>Return a vector containing the minimum components</summary>
		public static v4 Min(v4 lhs, v4 rhs)
		{
			 return new v4(
				 Math.Min(lhs.x, rhs.x),
				 Math.Min(lhs.y, rhs.y),
				 Math.Min(lhs.z, rhs.z),
				 Math.Min(lhs.w, rhs.w));
		}
		public static v4 Min(v4 x, params v4[] vecs)
		{
			foreach (var v in vecs)
				x = Min(x,v);
			return x;
		}

		/// <summary>Return a vector containing the maximum components</summary>
		public static v4 Max(v4 lhs, v4 rhs)
		{
			 return new v4(
				 Math.Max(lhs.x, rhs.x),
				 Math.Max(lhs.y, rhs.y),
				 Math.Max(lhs.z, rhs.z),
				 Math.Max(lhs.w, rhs.w));
		}
		public static v4 Max(v4 x, params v4[] vecs)
		{
			foreach (var v in vecs)
				x = Max(x,v);
			return x;
		}

		/// <summary>Clamp the components of 'vec' within the ranges of 'min' and 'max'</summary>
		public static v4 Clamp3(v4 vec, v4 min, v4 max)
		{
			return new v4(
				Maths.Clamp(vec.x, min.x, max.x),
				Maths.Clamp(vec.y, min.y, max.y),
				Maths.Clamp(vec.z, min.z, max.z),
				vec.w);
		}
		public static v4 Clamp4(v4 vec, v4 min, v4 max)
		{
			return new v4(
				Maths.Clamp(vec.x, min.x, max.x),
				Maths.Clamp(vec.y, min.y, max.y),
				Maths.Clamp(vec.z, min.z, max.z),
				Maths.Clamp(vec.w, min.w, max.w));
		}

		/// <summary>Component absolute value</summary>
		public static v4 Abs(v4 vec)
		{
			return new v4(Math.Abs(vec.x), Math.Abs(vec.y), Math.Abs(vec.z), Math.Abs(vec.w));
		}

		/// <summary>Normalise 'vec' by the length of the XY components</summary>
		public static v4 Normalise2(v4 vec)
		{
			return vec / vec.Length2;
		}
		public static v4 Normalise2(ref v4 vec)
		{
			return vec /= vec.Length2;
		}

		/// <summary>Normalise 'vec' by the length of the XYZ components</summary>
		public static v4 Normalise3(v4 vec)
		{
			return vec / vec.Length3;
		}
		public static v4 Normalise3(ref v4 vec)
		{
			return vec /= vec.Length3;
		}

		/// <summary>Normalise 'vec' by the length of the XYZW components</summary>
		public static v4 Normalise4(v4 vec)
		{
			return vec / vec.Length4;
		}
		public static v4 Normalise4(ref v4 vec)
		{
			return vec /= vec.Length4;
		}

		/// <summary>Normalise 'vec' by the length of the XY components or return 'def' if zero</summary>
		public static v4 Normalise2(v4 vec, v4 def)
		{
			if (vec.xy == v2.Zero) return def;
			var norm = Normalise2(vec);
			return norm.xy != v2.Zero ? norm : def;
		}

		/// <summary>Normalise 'vec' by the length of the XYZ components or return 'def' if zero</summary>
		public static v4 Normalise3(v4 vec, v4 def)
		{
			if (vec.xyz == v3.Zero) return def;
			var norm = Normalise3(vec);
			return norm.xyz != v3.Zero ? norm : def;
		}

		/// <summary>Normalise 'vec' by the length of the XYZW components or return 'def' if zero</summary>
		public static v4 Normalise4(v4 vec, v4 def)
		{
			if (vec == v4.Zero) return def;
			var norm = Normalise4(vec);
			return norm != v4.Zero ? norm : def;
		}

		/// <summary>Dot product of XYZ components</summary>
		public static float Dot3(v4 lhs, v4 rhs)
		{
			return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
		}

		/// <summary>Dot product of XYZW components</summary>
		public static float Dot4(v4 lhs, v4 rhs)
		{
			return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
		}

		/// <summary>Cross product of XYZ components</summary>
		public static v4 Cross3(v4 lhs, v4 rhs)
		{
			return new v4(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x, 0.0f);
		}

		/// <summary>Triple product (a . (b x c)) of XYZ components</summary>
		public static float Triple3(v4 a, v4 b, v4 c)
		{
			return Dot3(a, Cross3(b, c));
		}

		/// <summary>True if 'lhs' and 'rhs' are parallel</summary>
		public static bool Parallel(v4 lhs, v4 rhs, float tol)
		{
			return Maths.FEql(Cross3(lhs, rhs).Length3Sq, 0, tol);
		}
		public static bool Parallel(v4 lhs, v4 rhs)
		{
			return Parallel(lhs, rhs, Maths.TinyF);
		}

		/// <summary>Linearly interpolate between two vectors</summary>
		public static v4 Lerp(v4 lhs, v4 rhs, float frac)
		{
			return lhs * (1f - frac) + rhs * (frac);
		}

		/// <summary>Returns a vector guaranteed to not be parallel to 'vec'</summary>
		public static v4 CreateNotParallelTo(v4 vec)
		{
			bool x_aligned = Maths.Abs(vec.x) > Maths.Abs(vec.y) && Maths.Abs(vec.x) > Maths.Abs(vec.z);
			return new v4(Maths.SignF(!x_aligned), 0.0f, Maths.SignF(x_aligned), vec.w);
		}

		/// <summary>Returns a vector perpendicular to 'vec'</summary>
		public static v4 Perpendicular(v4 vec)
		{
			Debug.Assert(!FEql3(vec, Zero), "Cannot make a perpendicular to a zero vector");
			var v = Cross3(vec, CreateNotParallelTo(vec));
			v *= (float)Math.Sqrt(vec.Length3Sq / v.Length3Sq);
			return v;
		}

		/// <summary>Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular</summary>
		public static v4 Perpendicular(v4 vec, v4 previous)
		{
			Debug.Assert(!FEql3(vec, Zero), "Cannot make a perpendicular to a zero vector");

			// If 'previous' is parallel to 'vec', choose a new perpendicular (includes previouis == zero)
			if (Parallel(vec, previous))
				return Perpendicular(vec);

			// If 'previous' is still perpendicular, keep it
			if (Maths.FEql(Dot3(vec, previous), 0))
				return previous;

			// Otherwise, make a perpendicular that is close to 'previous'
			var v = Cross3(Cross3(vec, previous), vec);
			v *= (float)Math.Sqrt(vec.Length3Sq / v.Length3Sq);
			return v;
		}

		/// <summary>Returns a 3 bit bitmask of the octant the vector is in where X = 0x1, Y = 0x2, Z = 0x4</summary>
		public static uint Octant(v4 vec)
		{
			return (uint)(((int)Maths.SignF(vec.x >= 0.0f)) | ((int)Maths.SignF(vec.y >= 0.0f) << 1) | ((int)Maths.SignF(vec.z >= 0.0f) << 2));
		}

		/// <summary>Return the cosine of the angle between two vectors</summary>
		public static float CosAngle3(v4 lhs, v4 rhs)
		{
			Debug.Assert(lhs.Length3Sq != 0 && rhs.Length3Sq != 0, "CosAngle undefined for zero vectors");
			return Maths.Clamp(Dot3(lhs,rhs) / (float)Math.Sqrt(lhs.Length3Sq * rhs.Length3Sq), -1f, 1f);
		}

		/// <summary>Return the angle between two vectors</summary>
		public static float Angle3(v4 lhs, v4 rhs)
		{
			return (float)Math.Acos(CosAngle3(lhs, rhs));
		}

		/// <summary>Return the average of a collection of vectors</summary>
		public static v4 Average(IEnumerable<v4> vecs)
		{
			var acc = Zero;
			var count = 0;
			foreach (var v in vecs)
			{
				acc += v;
				++count;
			}
			if (count == 0) throw new Exception("Cannot average zero items");
			return acc / count;
		}

		#region Parse
		public static v4 Parse3(string s, float w)
		{
			if (s == null) throw new ArgumentNullException("s", "v4.Parse3() string argument was null");
			var values = s.Split(new char[]{' ',',','\t'},3);
			if (values.Length != 3) throw new FormatException("v4.Parse3() string argument does not represent a 3 component vector");
			return new v4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), w);
		}
		public static v4 Parse4(string s)
		{
			if (s == null) throw new ArgumentNullException("s", "v4.Parse4() string argument was null");
			var values = s.Split(new char[]{' ',',','\t'},4);
			if (values.Length != 4) throw new FormatException("v4.Parse4() string argument does not represent a 4 component vector");
			return new v4(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]), float.Parse(values[3]));
		}
		public static bool TryParse3(string s, out v4 vec, float w)
		{
			vec = Zero;
			if (s == null) return false;
			var values = s.Split(new char[]{' ',',','\t'},3);
			vec.w = w;
			return values.Length == 3 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y) && float.TryParse(values[2], out vec.z);
		}
		public static v4? TryParse3(string s, float w)
		{
			v4 vec;
			return TryParse3(s, out vec, w) ? (v4?)vec : null;
		}
		public static bool TryParse4(string s, out v4 vec)
		{
			vec = Zero;
			if (s == null) return false;
			var values = s.Split(new char[]{' ',',','\t'},4);
			return  values.Length == 4 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y) && float.TryParse(values[2], out vec.z) && float.TryParse(values[3], out vec.w);
		}
		public static v4? TryParse4(string s)
		{
			v4 vec;
			return TryParse4(s, out vec) ? (v4?)vec : null;
		}
		#endregion

		#region Random

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v4 Random4(float min, float max, Random r)
		{
			return new v4(r.Float(min, max), r.Float(min, max), r.Float(min, max), r.Float(min, max));
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v4 Random4(v4 min, v4 max, Random r)
		{
			return new v4(r.Float(min.x, max.x), r.Float(min.y, max.y), r.Float(min.z, max.z), r.Float(min.w, max.w));
		}
		
		/// <summary>Return a random vector within a 4D sphere of radius 'rad' (Note: *not* on the sphere)</summary>
		public static v4 Random4(float rad, Random r)
		{
			var rad_sq = rad*rad;
			v4 v; for (; (v = Random4(-rad, rad, r)).Length4Sq > rad_sq;) { }
			return v;
		}

		/// <summary>Return a random vector on the unit 4D sphere</summary>
		public static v4 Random4N(Random r)
		{
			v4 v; for (; FEql4(v = Random4(1.0f, r), Zero); ) { }
			return Normalise4(v);
		}

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v4 Random3(float min, float max, float w, Random r)
		{
			return new v4(r.Float(min, max), r.Float(min, max), r.Float(min, max), w);
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v4 Random3(v4 min, v4 max, float w, Random r)
		{
			return new v4(r.Float(min.x, max.x), r.Float(min.y, max.y), r.Float(min.z, max.z), w);
		}

		/// <summary>Return a random vector within a 3D sphere of radius 'rad' (Note: *not* on a sphere)</summary>
		public static v4 Random3(float rad, float w, Random r)
		{
			var rad_sq = rad*rad;
			v4 v; for (; (v = Random3(-rad, rad, w, r)).Length3Sq > rad_sq; ){}
			return v;
		}

		/// <summary>Return a random vector on the unit 3D sphere</summary>
		public static v4 Random3N(float w, Random r)
		{
			v4 v; for (; FEql3(v = Random3(1.0f, w, r), Zero); ) { }
			return Normalise3(v);
		}

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v4 Random2(float min, float max, float z, float w, Random r)
		{
			return new v4(r.Float(min, max), r.Float(min, max), z, w);
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v4 Random2(v4 min, v4 max, float z, float w, Random r)
		{
			return new v4(r.Float(min.x, max.x), r.Float(min.y, max.y), z, w);
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v4 Random2(float rad, float z, float w, Random r)
		{
			var rad_sq = rad*rad;
			v4 v; for (; (v = Random2(-rad, rad, z, w, r)).Length2Sq > rad_sq;) { }
			return v;
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v4 Random2N(float z, float w, Random r)
		{
			v4 v; for (; FEql2(v = Random2(1.0f, z, w, r), Zero);) { }
			return Normalise2(v);
		}

		#endregion

		#region Equals
		public static bool operator == (v4 lhs, v4 rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
		}
		public static bool operator != (v4 lhs, v4 rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is v4 && (v4)o == this;
		}
		public override int GetHashCode()
		{
			return new { x, y, z, w }.GetHashCode();
		}
		#endregion
	}

	public static partial class Maths
	{
		public static v4 Min(v4 lhs, v4 rhs)
		{
			return v4.Min(lhs,rhs);
		}
		public static v4 Max(v4 lhs, v4 rhs)
		{
			return v4.Max(lhs,rhs);
		}
		public static v4 Clamp(v4 vec, v4 min, v4 max)
		{
			return v4.Clamp4(vec, min, max);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class UnitTestV4
	{
		[Test] public void Basic()
		{
			var a = new v4(1,2,-3,-4);
			Assert.True(a.x == +1);
			Assert.True(a.y == +2);
			Assert.True(a.z == -3);
			Assert.True(a.w == -4);
		}
		[Test] public void MinMax()
		{
			var a = new v4(3,-1,2,-4);
			var b = new v4(-2,-1,4,2);
			Assert.True(v4.Max(a,b) == new v4(3,-1,4,2));
			Assert.True(v4.Min(a,b) == new v4(-2,-1,2,-4));
		}
		[Test] public void Length()
		{
			var a = new v4(3,-1,2,-4);

			Assert.True(Maths.FEql(a.Length2Sq, a.x*a.x + a.y*a.y));
			Assert.True(Maths.FEql(a.Length2  , Math.Sqrt(a.Length2Sq)));
			Assert.True(Maths.FEql(a.Length3Sq, a.x*a.x + a.y*a.y + a.z*a.z));
			Assert.True(Maths.FEql(a.Length3  , Math.Sqrt(a.Length3Sq)));
			Assert.True(Maths.FEql(a.Length4Sq, a.x*a.x + a.y*a.y + a.z*a.z + a.w*a.w));
			Assert.True(Maths.FEql(a.Length4  , Math.Sqrt(a.Length4Sq)));
		}
		[Test] public void Normals()
		{
			var a = new v4(3,-1,2,-4);
			var b = v4.Normalise3(a);
			var c = v4.Normalise4(a);
			Assert.True(Maths.FEql(b.Length3, 1.0f));
			Assert.True(Maths.FEql(b.w, a.w / a.Length3));
			Assert.True(Maths.FEql(Math.Sqrt(c.x*c.x + c.y*c.y + c.z*c.z + c.w*c.w), 1.0f));
			Assert.True(Maths.FEql(a.Length3, 1.0f) == false);
			Assert.True(Maths.FEql(a.Length4, 1.0f) == false);
			Assert.True(Maths.FEql(b.Length3, 1.0f));
			Assert.True(Maths.FEql(c.Length4, 1.0f));
		}
		[Test] public void DotProduct()
		{
			var a = new v4(-2,  4,  2,  6);
			var b = new v4( 3, -5,  2, -4);
			Assert.True(Maths.FEql(v4.Dot4(a,b), -46f));
			Assert.True(Maths.FEql(v4.Dot3(a,b), -22f));
		}
	}
}
#endif
