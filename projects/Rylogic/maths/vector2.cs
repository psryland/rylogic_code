//***************************************************
// Vector2
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using pr.extn;

namespace pr.maths
{
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{x}  {y}  // Len={Length2}")]
	public struct v2
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;

		// Constructors
		public v2(float x_) :this(x_, x_)
		{}
		public v2(float x_, float y_) :this()
		{
			x = x_;
			y = y_;
		}
		public v2(PointF pt) :this(pt.X, pt.Y)
		{}
		public v2(SizeF sz) :this(sz.Width, sz.Height)
		{}
		public v2(float[] arr) :this()
		{
			x = arr[0];
			y = arr[1];
		}

		/// <summary>Get/Set components by index</summary>
		public float this[int i]
		{
			get
			{
				switch (i) {
				case 0: return x;
				case 1: return y;
				}
				throw new ArgumentException("index out of range", "i");
			}
			set
			{
				switch (i) {
				case 0: x = value; return;
				case 1: y = value; return;
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

		/// <summary>Length</summary>
		public float Length2Sq
		{
			get { return x * x + y * y; }
		}
		public float Length2
		{
			get { return (float)Math.Sqrt(Length2Sq); }
		}

		/// <summary>ToString</summary>
		public string ToString2()
		{
			return x + " " + y;
		}
		public string ToString2(string format)
		{
			return x.ToString(format) + " " + y.ToString(format);
		}
		public override string ToString()
		{
			return ToString2();
		}

		/// <summary>ToArray(). Note not implicit because it gets called when converting to an object type. e.g. v2? x = v2.TryParse2("", out v) ? v : null. </summary>
		public float[] ToArray()
		{
			return new[] { x, y };
		}

		// Static v2 types
		public readonly static v2 Zero     = new v2(0f, 0f);
		public readonly static v2 XAxis    = new v2(1f, 0f);
		public readonly static v2 YAxis    = new v2(0f, 1f);
		public readonly static v2 One      = new v2(1f, 1f);
		public readonly static v2 MinValue = new v2(float.MinValue, float.MinValue);
		public readonly static v2 MaxValue = new v2(float.MaxValue, float.MaxValue);

		/// <summary>Operators</summary>
		public static v2 operator + (v2 vec)
		{
			return vec;
		}
		public static v2 operator - (v2 vec)
		{
			return new v2(-vec.x, -vec.y);
		}
		public static v2 operator + (v2 lhs, v2 rhs)
		{
			return new v2(lhs.x + rhs.x, lhs.y + rhs.y);
		}
		public static v2 operator - (v2 lhs, v2 rhs)
		{
			return new v2(lhs.x - rhs.x, lhs.y - rhs.y);
		}
		public static v2 operator * (v2 lhs, float rhs)
		{
			return new v2(lhs.x * rhs, lhs.y * rhs);
		}
		public static v2 operator * (float lhs, v2 rhs)
		{
			return new v2(lhs * rhs.x, lhs * rhs.y);
		}
		public static v2 operator * (v2 lhs, v2 rhs)
		{
			return new v2(lhs.x * rhs.x, lhs.y * rhs.y);
		}
		public static v2 operator / (v2 lhs, float rhs)
		{
			return new v2(lhs.x / rhs, lhs.y / rhs);
		}
		public static v2 operator / (v2 lhs, v2 rhs)
		{
			return new v2(lhs.x / rhs.x, lhs.y / rhs.y);
		}

		/// <summary>Return a vector containing the minimum components</summary>
		public static v2 Min(v2 lhs, v2 rhs)
		{
			 return new v2(
				 Math.Min(lhs.x, rhs.x),
				 Math.Min(lhs.y, rhs.y));
		}
		public static v2 Min(v2 x, params v2[] vecs)
		{
			foreach (var v in vecs)
				x = Min(x,v);
			return x;
		}

		/// <summary>Return a vector containing the maximum components</summary>
		public static v2 Max(v2 lhs, v2 rhs)
		{
			 return new v2(
				 Math.Max(lhs.x, rhs.x),
				 Math.Max(lhs.y, rhs.y));
		}
		public static v2 Max(v2 x, params v2[] vecs)
		{
			foreach (var v in vecs)
				x = Max(x,v);
			return x;
		}

		/// <summary>Clamp the components of 'vec' within the ranges of 'min' and 'max'</summary>
		public static v2 Clamp(v2 vec, v2 min, v2 max)
		{
			return new v2(
				Maths.Clamp(vec.x, min.x, max.x),
				Maths.Clamp(vec.y, min.y, max.y));
		}

		/// <summary>Component absolute value</summary>
		public static v2 Abs(v2 vec)
		{
			return new v2(Math.Abs(vec.x), Math.Abs(vec.y));
		}

		/// <summary>Normalise 'vec' by the length of the XY components</summary>
		public static v2 Normalise2(v2 vec)
		{
			return vec / vec.Length2;
		}
		public static v2 Normalise2(ref v2 vec)
		{
			return vec /= vec.Length2;
		}

		/// <summary>Normalise 'vec' by the length of the XY components or return 'def' if zero</summary>
		public static v2 Normalise2(v2 vec, v2 def)
		{
			if (vec == Zero) return def;
			var norm = Normalise2(vec);
			return norm != Zero ? norm : def;
		}

		/// <summary>Dot product of XYZ components</summary>
		public static float Dot2(v2 lhs, v2 rhs)
		{
			return lhs.x * rhs.x + lhs.y * rhs.y;
		}

		/// <summary>Cross product: Dot2(Rotate90CW(lhs), rhs)</summary>
		public static float Cross2(v2 lhs, v2 rhs)
		{
			return lhs.y * rhs.x - lhs.x * rhs.y;
		}

		/// <summary>True if 'lhs' and 'rhs' are parallel</summary>
		public static bool Parallel(v2 lhs, v2 rhs)
		{
			return Maths.FEql(Cross2(lhs, rhs), 0);
		}

		/// <summary>Linearly interpolate between two vectors</summary>
		public static v2 Lerp(v2 lhs, v2 rhs, float frac)
		{
			return lhs * (1f - frac) + rhs * (frac);
		}

		/// <summary>Returns a vector guaranteed to not be parallel to 'vec'</summary>
		public static v2 CreateNotParallelTo(v2 vec)
		{
			bool x_aligned = Maths.Abs(vec.x) > Maths.Abs(vec.y);
			return new v2(Maths.SignF(!x_aligned), Maths.SignF(x_aligned));
		}

		/// <summary>Returns a vector perpendicular to 'vec'</summary>
		public static v2 Perpendicular(v2 vec)
		{
			Debug.Assert(!Maths.FEql(vec, Zero), "Cannot make a perpendicular to a zero vector");
			return RotateCCW(vec);
		}

		/// <summary>Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular</summary>
		public static v2 Perpendicular(v2 vec, v2 previous)
		{
			Debug.Assert(!Maths.FEql(vec, Zero), "Cannot make a perpendicular to a zero vector");

			// If 'previous' is still perpendicular, keep it
			if (Maths.FEql(Dot2(vec, previous), 0))
				return previous;

			// If 'previous' is parallel to 'vec', choose a new perpendicular
			if (Parallel(vec, previous))
				return Perpendicular(vec);

			// Otherwise, make a perpendicular that is close to 'previous'
			var v = previous - (Dot2(vec,previous) / vec.Length2Sq) * vec;
			v *= (float)Math.Sqrt(vec.Length2Sq / v.Length2Sq);
			return v;
		}

		/// <summary>Return the cosine of the angle between two vectors</summary>
		public static float CosAngle2(v2 lhs, v2 rhs)
		{
			// Return the cosine of the angle between two vectors
			Debug.Assert(lhs.Length2Sq != 0 && rhs.Length2Sq != 0, "CosAngle undefined for zero vectors");
			return Maths.Clamp(Dot2(lhs,rhs) / (float)Math.Sqrt(lhs.Length2Sq * rhs.Length2Sq), -1f, 1f);
		}

		/// <summary>Return the angle between two vectors</summary>
		public static float Angle2(v2 lhs, v2 rhs)
		{
			return (float)Math.Acos(CosAngle2(lhs, rhs));
		}

		/// <summary>Rotate 'vec' counter clockwise</summary>
		public static v2 RotateCCW(v2 vec)
		{
			return new v2(-vec.y, vec.x);
		}

		/// <summary>Rotate 'vec' clockwise</summary>
		public static v2 RotateCW(v2 vec)
		{
			return new v2(vec.y, -vec.x);
		}

		/// <summary>Return a point on the unit circle at 'ang' (radians) from the X axis</summary>
		public static v2 UnitCircle(float ang)
		{
			return new v2((float)Math.Cos(ang), (float)Math.Sin(ang));
		}

		#region System.Drawing conversion
		/// <summary>Create from Drawing type</summary>
		public static v2 From(Point point)
		{
			return new v2(point.X, point.Y);
		}
		public static v2 From(PointF point)
		{
			return new v2(point.X, point.Y);
		}
		public static v2 From(Size size)
		{
			return new v2(size.Width, size.Height);
		}
		public static v2 From(SizeF size)
		{
			return new v2(size.Width, size.Height);
		}

		/// <summary>Convert to Drawing type</summary>
		public Point ToPoint()
		{
			return new Point((int)x, (int)y);
		}
		public PointF ToPointF()
		{
			return new PointF(x, y);
		}
		public Size ToSize()
		{
			return new Size((int)x, (int)y);
		}
		public SizeF ToSizeF()
		{
			return new SizeF(x, y);
		}
		public Rectangle ToRectangle()
		{
			return new Rectangle(Point.Empty, ToSize());
		}
		public RectangleF ToRectangleF()
		{
			return new RectangleF(PointF.Empty, ToSizeF());
		}

		/// <summary>Implicit conversion to/from drawing types</summary>
		public static implicit operator v2(PointF p)
		{
			return From(p);
		}
		public static implicit operator PointF(v2 p)
		{
			return p.ToPointF();
		}
		public static implicit operator v2(SizeF s)
		{
			return From(s);
		}
		public static implicit operator SizeF(v2 s)
		{
			return s.ToSizeF();
		}
		#endregion

		#region Parse
		public static v2 Parse2(string s)
		{
			if (s == null) throw new ArgumentNullException("s", "v2.Parse3() string argument was null");
			var values = s.Split(new char[]{' ',',','\t'},2);
			if (values.Length != 2) throw new FormatException("v2.Parse3() string argument does not represent a 2 component vector");
			return new v2(float.Parse(values[0]), float.Parse(values[1]));
		}
		public static bool TryParse2(string s, out v2 vec)
		{
			vec = Zero;
			if (s == null) return false;
			var values = s.Split(new char[]{' ',',','\t'},2);
			return values.Length == 2 && float.TryParse(values[0], out vec.x) && float.TryParse(values[1], out vec.y);
		}
		public static v2? TryParse2(string s)
		{
			v2 vec;
			return TryParse2(s, out vec) ? (v2?)vec : null;
		}
		#endregion

		#region Random

		/// <summary>Return a random vector with components within the interval [min,max]</summary>
		public static v2 Random2(float min, float max, Random r)
		{
			return new v2(r.Float(min, max), r.Float(min, max));
		}

		/// <summary>Return a random vector with components within the intervals given by each component of min and max</summary>
		public static v2 Random2(v2 min, v2 max, Random r)
		{
			return new v2(r.Float(min.x, max.x), r.Float(min.y, max.y));
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v2 Random2(float rad, Random r)
		{
			var rad_sq = rad*rad;
			v2 v; for (; (v = Random2(-rad, rad, r)).Length2Sq > rad_sq;) { }
			return v;
		}

		/// <summary>Return a random vector on the unit 2D sphere</summary>
		public static v2 Random2N(Random r)
		{
			v2 v; for (; Maths.FEql(v = Random2(1.0f, r), Zero);) { }
			return Normalise2(v);
		}

		#endregion

		#region Equals
		public static bool operator == (v2 lhs, v2 rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y;
		}
		public static bool operator != (v2 lhs, v2 rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is v2 && (v2)o == this;
		}
		public override int GetHashCode()
		{
			return new { x, y }.GetHashCode();
		}
		#endregion
	}

	public static partial class Maths
	{
		/// <summary>Approximate equal</summary>
		public static bool FEqlRelative(v2 lhs, v2 rhs, float tol)
		{
			return
				FEqlRelative(lhs.x, rhs.x, tol) &&
				FEqlRelative(lhs.y, rhs.y, tol);
		}
		public static bool FEql(v2 lhs, v2 rhs)
		{
			return FEqlRelative(lhs.x, rhs.x, TinyF);
		}

		public static v2 Min(v2 lhs, v2 rhs)
		{
			return v2.Min(lhs,rhs);
		}
		public static v2 Max(v2 lhs, v2 rhs)
		{
			return v2.Max(lhs,rhs);
		}
		public static v2 Clamp(v2 vec, v2 min, v2 max)
		{
			return v2.Clamp(vec, min, max);
		}

		public static bool IsFinite(v2 vec)
		{
			return IsFinite(vec.x) && IsFinite(vec.y);
		}

		/// <summary>Return 'a/b', or 'def' if 'b' is zero</summary>
		public static v2 Div(v2 a, v2 b, v2 def)
		{
			return b != v2.Zero ? a / b : def;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class UnitTestv2
	{
		[Test] public void Basic()
		{
			var a = new v2(1,2);
			Assert.True(a.x == +1);
			Assert.True(a.y == +2);
		}
		[Test] public void MinMax()
		{
			var a = new v2(3,-1);
			var b = new v2(-2,-1);
			Assert.True(v2.Max(a,b) == new v2(3,-1));
			Assert.True(v2.Min(a,b) == new v2(-2,-1));
		}
		[Test] public void Length()
		{
			var a = new v2(3,-1);

			Assert.True(Maths.FEql(a.Length2Sq, a.x*a.x + a.y*a.y));
			Assert.True(Maths.FEql(a.Length2  , (float)Math.Sqrt(a.Length2Sq)));
		}
		[Test] public void Normals()
		{
			var a = new v2(3,-1);
			var b = v2.Normalise2(a);
			Assert.True(Maths.FEql(b.Length2, 1.0f));
			Assert.True(Maths.FEql(a.Length2, 1.0f) == false);
		}
		[Test] public void DotProduct()
		{
			var a = new v2(-2,  4);
			var b = new v2( 3, -5);
			Assert.True(Maths.FEql(v2.Dot2(a,b), -26f));
		}
	}
}
#endif