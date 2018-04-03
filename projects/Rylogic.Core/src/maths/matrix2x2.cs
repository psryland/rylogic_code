//***************************************************
// Matrix
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using Rylogic.Extn;

namespace Rylogic.Maths
{
	public struct m2x2
	{
		public v2 x;
		public v2 y;

		public m2x2(v2 x, v2 y) :this()
		{
			this.x = x;
			this.y = y;
		}
		//public m2x2(v2 axis_norm, v4 axis_sine_angle, float cos_angle) :this() { set(axis_norm, axis_sine_angle, cos_angle); }
		//public m2x2(v2 axis_norm, float angle) :this()                         { set(axis_norm, angle); }
		//public m2x2(v2 from, v4 to) :this()                                    { set(from, to); }

		/// <summary>Get/Set columns by index</summary>
		public v2 this[int c]
		{
			get
			{
				switch (c) {
				case 0: return x;
				case 1: return y;
				}
				throw new ArgumentException("index out of range", "i");
			}
			set
			{
				switch (c) {
				case 0: x = value; return;
				case 1: y = value; return;
				}
				throw new ArgumentException("index out of range", "i");
			}
		}
		public v2 this[uint i]
		{
			get { return this[(int)i]; }
			set { this[(int)i] = value; }
		}

		/// <summary>Get/Set components by index</summary>
		public float this[int c, int r]
		{
			get { return this[c][r]; }
			set
			{
				var vec = this[c];
				vec[r] = value;
				this[c] = vec;
			}
		}
		public float this[uint c, uint r]
		{
			get { return this[(int)c][(int)r]; }
			set
			{
				var vec = this[c];
				vec[r] = value;
				this[c] = vec;
			}
		}

		/// <summary>ToString</summary>
		public override string ToString()
		{
			return $"{x} \n{y} \n";
		}

		/// <summary>To flat array</summary>
		public float[] ToArray()
		{
			return new []
			{
				x.x, x.y,
				y.x, y.y,
			};
		}

		// Static m2x2 types
		private readonly static m2x2 m_zero     = new m2x2(v2.Zero , v2.Zero );
		private readonly static m2x2 m_identity = new m2x2(v2.XAxis, v2.YAxis);
		public static m2x2 Zero     { get { return m_zero; } }
		public static m2x2 Identity { get { return m_identity; } }

		// Functions
		public override bool Equals(object o)               { return o is m2x2 && (m2x2)o == this; }
		public override int GetHashCode()                   { unchecked { return x.GetHashCode() + y.GetHashCode(); } }
		public static m2x2 operator * (m2x2 lhs, float rhs) { return new m2x2(lhs.x * rhs   , lhs.y * rhs   ); }
		public static m2x2 operator + (m2x2 lhs, m2x2 rhs)  { return new m2x2(lhs.x + rhs.x , lhs.y + rhs.y ); }
		public static m2x2 operator - (m2x2 lhs, m2x2 rhs)  { return new m2x2(lhs.x - rhs.x , lhs.y - rhs.y ); }
		public static m2x2 operator * (float lhs, m2x2 rhs) { return new m2x2(lhs * rhs.x   , lhs * rhs.y   ); }
		public static m2x2 operator / (m2x2 lhs, float rhs) { return new m2x2(lhs.x / rhs   , lhs.y / rhs   ); }
		public static bool operator ==(m2x2 lhs, m2x2 rhs)  { return lhs.x == rhs.x && lhs.y == rhs.y; }
		public static bool operator !=(m2x2 lhs, m2x2 rhs)  { return !(lhs == rhs); }
		public static v2 operator * (m2x2 lhs, v2 rhs)
		{
			v2 ans;
			Transpose(ref lhs);
			ans.x = Math_.Dot(lhs.x, rhs);
			ans.y = Math_.Dot(lhs.y, rhs);
			return ans;
		}
		public static m2x2 operator * (m2x2 lhs, m2x2 rhs)
		{
			m2x2 ans;
			Transpose(ref lhs);
			ans.x.x = Math_.Dot(lhs.x, rhs.x);
			ans.x.y = Math_.Dot(lhs.y, rhs.x);
			ans.y.x = Math_.Dot(lhs.x, rhs.y);
			ans.y.y = Math_.Dot(lhs.y, rhs.y);
			return ans;
		}

		/// <summary>Return the determinant of 'm'</summary>
		public static float Determinant(m2x2 m)
		{
			return (float)((double)m.x.x*m.y.y - (double)m.x.y*m.y.x);
		}

		/// <summary>Transpose 'm' in-place</summary>
		public static void Transpose(ref m2x2 m)
		{
			Math_.Swap(ref m.x.y, ref m.y.x);
		}

		/// <summary>Return the transpose of 'm'</summary>
		public static m2x2 Transpose(m2x2 m)
		{
			Transpose(ref m);
			return m;
		}

		/// <summary>Invert 'm' in place assuming m is orthonormal</summary>
		public static void InvertFast(ref m2x2 m)
		{
			Debug.Assert(IsOrthonormal(m), "Matrix is not orthonormal");
			Transpose(ref m);
		}

		/// <summary>Return the inverse of 'm' assuming m is orthonormal</summary>
		public static m2x2 InvertFast(m2x2 m)
		{
			InvertFast(ref m);
			return m;
		}

		/// <summary>True if 'm' can be inverted</summary>
		public static bool IsInvertable(m2x2 m)
		{
			return Determinant(m) != 0;
		}

		/// <summary>Return the inverse of 'm'</summary>
		public static m2x2 Invert(m2x2 m)
		{
			Debug.Assert(IsInvertable(m), "Matrix has no inverse");

			var det = Determinant(m);
			var tmp = new m2x2(
				new v2(m.y.y, -m.x.y) / det,
				new v2(-m.y.x, m.x.x) / det);
			return tmp;
		}

		/// <summary>Orthonormalise 'm' in-place</summary>
		public static void Orthonormalise(ref m2x2 m)
		{
			m.x = Math_.Normalise(m.x);
			m.y = Math_.Normalise(m.y - Math_.Dot(m.x,m.y) * m.x);
		}

		/// <summary>Return an orthonormalised version of 'm'</summary>
		public static m2x2 Orthonormalise(m2x2 m)
		{
			Orthonormalise(ref m);
			return m;
		}

		/// <summary>True if 'm' is orthonormal</summary>
		public static bool IsOrthonormal(m2x2 m)
		{
			return
				Math_.FEql(m.x.LengthSq, 1f) &&
				Math_.FEql(m.y.LengthSq, 1f) &&
				Math_.FEql(Math_.Dot(m.x, m.y), 0f);
		}

		// Permute the rotation vectors in a matrix by 'n'
		public static m2x2 PermuteRotation(m2x2 mat, int n)
		{
			switch (n%2)
			{
			default: return mat;
			case 1: return new m2x2(mat.y, mat.x);
			}
		}

		// Make an orientation matrix from a direction.
		public static m2x2 OrientationFromDirection(v2 direction, int axis)
		{
			m2x2 ans = Identity;
			ans.x = Math_.Normalise(direction);
			ans.y = new v2(ans.x.y, -ans.x.x);
			return PermuteRotation(ans, axis);
		}

		//// Create a rotation from an axis and angle
		//public static m3x4 Rotation(v4 axis_norm, v4 axis_sine_angle, float cos_angle)
		//{
		//	return new m3x4(axis_norm, axis_sine_angle, cos_angle);
		//}

		//// Create from an axis and angle. 'axis' should be normalised
		//public static m3x4 Rotation(v4 axis_norm, float angle)
		//{
		//	return new m3x4(axis_norm, angle);
		//}

		//// Create a rotation from 'from' to 'to'
		//public static m3x4 Rotation(v4 from, v4 to)
		//{
		//	return new m3x4(from, to);
		//}

		#region Random

		/// <summary>Create a random 3x4 matrix</summary>
		public static m2x2 Random(Random r, float min_value, float max_value)
		{
			return new m2x2(
				new v2(r.Float(min_value, max_value), r.Float(min_value, max_value)),
				new v2(r.Float(min_value, max_value), r.Float(min_value, max_value)));
		}

		///// <summary>Create a random 2D rotation matrix</summary>
		//public static m3x4 Random(Random r, float min_angle, float max_angle)
		//{
		//	return Rotation(axis, r.Float(min_angle, max_angle));
		//}
		//public static m3x4 Random(Random r)
		//{
		//	return Random(r, v4.Random3N(0.0f, r), 0.0f, (float)Math_.Tau);
		//}

		#endregion
	}

	public static partial class Math_
	{
		/// <summary>Approximate equal</summary>
		public static bool FEqlRelative(m2x2 lhs, m2x2 rhs, float tol)
		{
			return
				FEqlRelative(lhs.x, rhs.x, tol) &&
				FEqlRelative(lhs.y, rhs.y, tol);
		}
		public static bool FEql(m2x2 lhs, m2x2 rhs)
		{
			return FEqlRelative(lhs, rhs, TinyF);
		}

		public static bool IsFinite(m2x2 vec)
		{
			return IsFinite(vec.x) && IsFinite(vec.y);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Maths;

	[TestFixture] public class UnitTestM2x2
	{
		[Test] public void TestInversion()
		{
			var rng = new Random();
			//{
			//	var m = m2x2.Random(rng, v4.Random3N(0, rng), -(float)Math_.Tau, +(float)Math_.Tau);
			//	var inv_m0 = m3x4.InvertFast(m);
			//	var inv_m1 = m3x4.Invert(m);
			//	Assert.True(m3x4.FEql(inv_m0, inv_m1, 0.001f));
			//}
			{
				var m = m2x2.Random(rng, -5.0f, +5.0f);
				var inv_m = m2x2.Invert(m);
				var I0 = inv_m * m;
				var I1 = m * inv_m;

				Assert.True(Math_.FEql(I1, m2x2.Identity));
				Assert.True(Math_.FEql(I0, m2x2.Identity));
			}
			{
				var m = new m2x2(
					new v2(4f, 7f),
					new v2(2f, 6f));
				var inv_m = m2x2.Invert(m);

				var det = 4f*6f - 7f*2f;
				var INV_M = new m2x2(
					new v2(6f, -7f)/det,
					new v2(-2f, 4f)/det);

				Assert.True(Math_.FEql(m*INV_M, m2x2.Identity));
				Assert.True(Math_.FEql(  inv_m,         INV_M));
			}
		}
		[Test] public void TestQuatConversion()
		{
		}
	}
}
#endif