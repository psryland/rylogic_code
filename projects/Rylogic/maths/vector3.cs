//***************************************************
// Vector2
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using pr.extn;

namespace pr.maths
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{x}  {y}  {z}  // Len={Length3}")]
	public struct v3
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;
		[FieldOffset( 8)] public float z;

		[FieldOffset( 0)] public v2 xy;

		// Constructors
		public v3(float x_) :this(x_,x_,x_)
		{}
		public v3(float x_, float y_, float z_) :this()
		{
			x = x_;
			y = y_;
			z = z_;
		}
		public v3(v2 xy_, float z_) :this()
		{
			xy = xy_;
			z = z_;
		}
		public v3(float[] arr) :this()
		{
			x = arr[0];
			y = arr[1];
			z = arr[2];
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
				}
				throw new ArgumentException("index out of range", "i");
			}
			set
			{
				switch (i) {
				case 0: x = value; return;
				case 1: y = value; return;
				case 2: z = value; return;
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

		/// <summary>Length</summary>
		public float Length2Sq
		{
			get { return x * x + y * y; }
		}
		public float Length3Sq
		{
			get { return x * x + y * y + z * z; }
		}
		public float Length2
		{
			get { return (float)Math.Sqrt(Length2Sq); }
		}
		public float Length3
		{
			get { return (float)Math.Sqrt(Length3Sq); }
		}

		/// <summary>ToString</summary>
		public string ToString2()
		{
			return x + " " + y;
		}
		public string ToString3()
		{
			return x + " " + y + " " + z;
		}
		public string ToString2(string format)
		{
			return x.ToString(format) + " " + y.ToString(format);
		}
		public string ToString3(string format)
		{
			return ToString2(format) + " " + z.ToString(format);
		}
		public override string ToString()
		{
			return ToString3();
		}

		/// <summary>ToArray(). Note not implicit because it gets called when converting to an object type. e.g. v3? x = v3.TryParse3("", out v) ? v : null. </summary>
		public float[] ToArray()
		{
			return new[] { x, y, z };
		}

		// Static v3 types
		public readonly static v3 Zero     = new v3(0f, 0f, 0f);
		public readonly static v3 Xaxis    = new v3(1f, 0f, 0f);
		public readonly static v3 Yaxis    = new v3(0f, 1f, 0f);
		public readonly static v3 Zaxis    = new v3(0f, 0f, 1f);
		public readonly static v3 One      = new v3(1f, 1f, 1f);
		public readonly static v3 MinValue = new v3(float.MinValue, float.MinValue, float.MinValue);
		public readonly static v3 MaxValue = new v3(float.MaxValue, float.MaxValue, float.MaxValue);

		/// <summary>Operators</summary>
		public static v3 operator + (v3 vec)
		{
			return vec;
		}
		public static v3 operator - (v3 vec)
		{
			return new v3(-vec.x, -vec.y, -vec.z);
		}
		public static v3 operator + (v3 lhs, v3 rhs)
		{
			return new v3(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
		}
		public static v3 operator - (v3 lhs, v3 rhs)
		{
			return new v3(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
		}
		public static v3 operator * (v3 lhs, float rhs)
		{
			return new v3(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
		}
		public static v3 operator * (float lhs, v3 rhs)
		{
			return new v3(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z);
		}
		public static v3 operator * (v3 lhs, v3 rhs)
		{
			return new v3(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
		}
		public static v3 operator / (v3 lhs, float rhs)
		{
			return new v3(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
		}
		public static v3 operator / (v3 lhs, v3 rhs)
		{
			return new v3(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
		}

		/// <summary>Return a vector containing the minimum components</summary>
		public static v3 Min(v3 lhs, v3 rhs)
		{
			 return new v3(
				 Math.Min(lhs.x, rhs.x),
				 Math.Min(lhs.y, rhs.y),
				 Math.Min(lhs.z, rhs.z));
		}
		public static v3 Min(v3 x, params v3[] vecs)
		{
			foreach (var v in vecs)
				x = Min(x,v);
			return x;
		}

		/// <summary>Return a vector containing the maximum components</summary>
		public static v3 Max(v3 lhs, v3 rhs)
		{
			 return new v3(
				 Math.Max(lhs.x, rhs.x),
				 Math.Max(lhs.y, rhs.y),
				 Math.Max(lhs.z, rhs.z));
		}
		public static v3 Max(v3 x, params v3[] vecs)
		{
			foreach (var v in vecs)
				x = Max(x,v);
			return x;
		}

		/// <summary>Clamp the components of 'vec' within the ranges of 'min' and 'max'</summary>
		public static v3 Clamp(v3 vec, v3 min, v3 max)
		{
			return new v3(
				Maths.Clamp(vec.x, min.x, max.x),
				Maths.Clamp(vec.y, min.y, max.y),
				Maths.Clamp(vec.z, min.z, max.z));
		}

		/// <summary>Component absolute value</summary>
		public static v3 Abs(v3 vec)
		{
			return new v3(Math.Abs(vec.x), Math.Abs(vec.y), Math.Abs(vec.z));
		}

		/// <summary>Normalise 'vec' by the length of the XY components</summary>
		public static v3 Normalise2(v3 vec)
		{
			return vec / vec.Length2;
		}
		public static v3 Normalise2(ref v3 vec)
		{
			return vec /= vec.Length2;
		}

		/// <summary>Normalise 'vec' by the length of the XYZ components</summary>
		public static v3 Normalise3(v3 vec)
		{
			return vec / vec.Length3;
		}
		public static v3 Normalise3(ref v3 vec)
		{
			return vec /= vec.Length3;
		}

		/// <summary>Normalise 'vec' by the length of the XY components or return 'def' if zero</summary>
		public static v3 Normalise2(v3 vec, v3 def)
		{
			if (vec.xy == v2.Zero) return def;
			var norm = Normalise2(vec);
			return norm.xy != v2.Zero ? norm : def;
		}

		/// <summary>Normalise 'vec' by the length of the XYZ components or return 'def' if zero</summary>
		public static v3 Normalise3(v3 vec, v3 def)
		{
			if (vec == Zero) return def;
			var norm = Normalise3(vec);
			return norm != Zero ? norm : def;
		}

		/// <summary>Dot product of XYZ components</summary>
		public static float Dot3(v3 lhs, v3 rhs)
		{
			return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
		}

		/// <summary>Cross product of XYZ components</summary>
		public static v3 Cross3(v3 lhs, v3 rhs)
		{
			return new v3(lhs.y * rhs.z - lhs.z * rhs.y, lhs.z * rhs.x - lhs.x * rhs.z, lhs.x * rhs.y - lhs.y * rhs.x);
		}

		/// <summary>Triple product of XYZ components</summary>
		public static float Triple3(v3 a, v3 b, v3 c)
		{
			return Dot3(a, Cross3(b, c));
		}

		/// <summary>True if 'lhs' and 'rhs' are parallel</summary>
		public static bool Parallel(v3 lhs, v3 rhs)
		{
			return Maths.FEql(Cross3(lhs, rhs).Length3Sq, 0);
		}

		/// <summary>Linearly interpolate between two vectors</summary>
		public static v3 Lerp(v3 lhs, v3 rhs, float frac)
		{
			return lhs * (1f - frac) + rhs * (frac);
		}

		/// <summary>Returns a vector guaranteed to not be parallel to 'vec'</summary>
		public static v3 CreateNotParallelTo(v3 vec)
		{
			bool x_aligned = Maths.Abs(vec.x) > Maths.Abs(vec.y) && Maths.Abs(vec.x) > Maths.Abs(vec.z);
			return new v3(Maths.SignF(!x_aligned), 0.0f, Maths.SignF(x_aligned));
		}

		/// <summary>Returns a vector perpendicular to 'vec'</summary>
		public static v3 Perpendicular(v3 vec)
		{
			Debug.Assert(!Maths.FEql(vec, Zero), "Cannot make a perpendicular to a zero vector");
			var v = Cross3(vec, CreateNotParallelTo(vec));
			v *= vec.Length3 / v.Length3;
			return v;
		}

		/// <summary>Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular</summary>
		public static v3 Perpendicular(v3 vec, v3 previous)
		{
			Debug.Assert(!Maths.FEql(vec, Zero), "Cannot make a perpendicular to a zero vector");

			// If 'previous' is parallel to 'vec', choose a new perpendicular (includes previous == zero)
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

		/// <summary>Return the cosine of the angle between two vectors</summary>
		public static float CosAngle3(v3 lhs, v3 rhs)
		{
			Debug.Assert(lhs.Length3Sq != 0 && rhs.Length3Sq != 0, "CosAngle undefined for zero vectors");
			return Maths.Clamp(Dot3(lhs,rhs) / (float)Math.Sqrt(lhs.Length3Sq * rhs.Length3Sq), -1f, 1f);
		}

		/// <summary>Return the angle between two vectors</summary>
		public static float Angle3(v3 lhs, v3 rhs)
		{
			return (float)Math.Acos(CosAngle3(lhs, rhs));
		}

		/// <summary>Return the average of a collection of vectors</summary>
		public static v3 Average(IEnumerable<v3> vecs)
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
		public static v3 Parse3(string s)
		{
			if (s == null) throw new ArgumentNullException("s", "v3.Parse3() string argument was null");
			var values = s.Split(new char[]{' ',',','\t'},3);
			if (values.Length != 3) throw new FormatException("v3.Parse3() string argument does not represent a 3 component vector");
			return new v3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
		}
		public static bool TryParse3(string s, out v3 vec)
		{
			vec = Zero;
			if (s == null) return false;
			var values = s.Split(new char[]{' ',',','\t'},3);
			return values.Length == 3 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y) && float.TryParse(values[2], out vec.z);
		}
		public static v3? TryParse3(string s)
		{
			v3 vec;
			return TryParse3(s, out vec) ? (v3?)vec : null;
		}
		#endregion

		#region Random

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v3 Random3(float min, float max, Random r)
		{
			return new v3(r.Float(min, max), r.Float(min, max), r.Float(min, max));
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v3 Random3(v3 min, v3 max, Random r)
		{
			return new v3(r.Float(min.x, max.x), r.Float(min.y, max.y), r.Float(min.z, max.z));
		}

		/// <summary>Return a random vector within a 3D sphere of radius 'rad' (Note: *not* on a sphere)</summary>
		public static v3 Random3(float rad, Random r)
		{
			var rad_sq = rad*rad;
			v3 v; for (; (v = Random3(-rad, rad, r)).Length3Sq > rad_sq; ){}
			return v;
		}

		/// <summary>Return a random vector on the unit 3D sphere</summary>
		public static v3 Random3N(Random r)
		{
			v3 v; for (; Maths.FEql(v = Random3(1.0f, r), Zero); ) { }
			return Normalise3(v);
		}

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v3 Random2(float min, float max, float z, Random r)
		{
			return new v3(r.Float(min, max), r.Float(min, max), z);
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v3 Random2(v3 min, v3 max, float z, Random r)
		{
			return new v3(r.Float(min.x, max.x), r.Float(min.y, max.y), z);
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v3 Random2(float rad, float z, Random r)
		{
			var rad_sq = rad*rad;
			v3 v; for (; (v = Random2(-rad, rad, z, r)).Length2Sq > rad_sq;) { }
			return v;
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v3 Random2N(float z, Random r)
		{
			v3 v; for (; Maths.FEql(v = Random2(1.0f, z, r), Zero);) { }
			return Normalise2(v);
		}

		#endregion

		#region Equals
		public static bool operator == (v3 lhs, v3 rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
		}
		public static bool operator != (v3 lhs, v3 rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is v3 && (v3)o == this;
		}
		public override int GetHashCode()
		{
			return new { x, y, z }.GetHashCode();
		}
		#endregion
	}

	public static partial class Maths
	{
		/// <summary>Approximate equal</summary>
		public static bool FEqlRelative(v3 lhs, v3 rhs, float tol)
		{
			return
				FEqlRelative(lhs.x, rhs.x, tol) &&
				FEqlRelative(lhs.y, rhs.y, tol) &&
				FEqlRelative(lhs.z, rhs.z, tol);
		}
		public static bool FEql(v3 lhs, v3 rhs)
		{
			return FEqlRelative(lhs, rhs, TinyF);
		}

		public static v3 Min(v3 lhs, v3 rhs)
		{
			return v3.Min(lhs,rhs);
		}
		public static v3 Max(v3 lhs, v3 rhs)
		{
			return v3.Max(lhs,rhs);
		}
		public static v3 Clamp(v3 vec, v3 min, v3 max)
		{
			return v3.Clamp(vec, min, max);
		}

		public static bool IsFinite(v3 vec)
		{
			return IsFinite(vec.x) && IsFinite(vec.y) && IsFinite(vec.z);
		}

		/// <summary>Return 'a/b', or 'def' if 'b' is zero</summary>
		public static v3 Div(v3 a, v3 b, v3 def)
		{
			return b != v3.Zero ? a / b : def;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class UnitTestv3
	{
		[Test] public void Basic()
		{
			var a = new v3(1,2,-3);
			Assert.True(a.x == +1);
			Assert.True(a.y == +2);
			Assert.True(a.z == -3);
		}
		[Test] public void MinMax()
		{
			var a = new v3(3,-1,2);
			var b = new v3(-2,-1,4);
			Assert.True(v3.Max(a,b) == new v3(3,-1,4));
			Assert.True(v3.Min(a,b) == new v3(-2,-1,2));
		}
		[Test] public void Length()
		{
			var a = new v3(3,-1,2);

			Assert.True(Maths.FEql(a.Length2Sq, a.x*a.x + a.y*a.y));
			Assert.True(Maths.FEql(a.Length2  , (float)Math.Sqrt(a.Length2Sq)));
			Assert.True(Maths.FEql(a.Length3Sq, a.x*a.x + a.y*a.y + a.z*a.z));
			Assert.True(Maths.FEql(a.Length3  , (float)Math.Sqrt(a.Length3Sq)));
		}
		[Test] public void Normals()
		{
			var a = new v3(3,-1,2);
			var b = v3.Normalise3(a);
			Assert.True(Maths.FEql(b.Length3, 1.0f));
			Assert.True(Maths.FEql(a.Length3, 1.0f) == false);
			Assert.True(Maths.FEql(b.Length3, 1.0f));
		}
		[Test] public void DotProduct()
		{
			var a = new v3(-2,  4,  2);
			var b = new v3( 3, -5,  2);
			Assert.True(Maths.FEql(v3.Dot3(a,b), -22f));
		}
	}
}
#endif