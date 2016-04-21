//***************************************************
// Matrix4x4
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace pr.maths
{
	[StructLayout(LayoutKind.Explicit)]
	public struct m4x4
	{
		[FieldOffset( 0)] public v4   x;
		[FieldOffset(16)] public v4   y;
		[FieldOffset(32)] public v4   z;
		[FieldOffset(48)] public v4   w;

		[FieldOffset( 0)] public m3x4 rot;
		[FieldOffset(48)] public v4   pos;

		public override string ToString() { return x + " \n" + y + " \n" + z + " \n" + w + " \n"; }

		public m4x4(m3x4 rot_, v4 pos_) :this()                                                { rot = rot_; pos = pos_; }
		public m4x4(v4 x_, v4 y_, v4 z_, v4 w_) :this()                                        { set(x_, y_, z_, w_); }
		public m4x4(v4 axis_norm, v4 axis_sine_angle, float cos_angle, v4 translation) :this() { set(axis_norm, axis_sine_angle, cos_angle, translation); }
		public m4x4(v4 axis_norm, float angle, v4 translation) :this()                         { set(axis_norm, angle, translation); }
		public m4x4(v4 from, v4 to, v4 translation) :this()                                    { set(from, to, translation); }
		public m4x4(float pitch, float yaw, float roll, v4 translation) :this()                { set(pitch, yaw, roll, translation); }
		public m4x4(v4 quaternion, v4 translation) :this()                                     { set(quaternion, translation); }

		public v4 this[int i]
		{
			get { switch(i){case 0:return x;case 1:return y;case 2:return z;case 3:return w;default: throw new ArgumentException("index out of range", "i");} }
			set { switch(i){case 0:x=value;break;case 1:y=value;break;case 2:z=value;break;case 3:w=value;break;default: throw new ArgumentException("index out of range", "i");} }
		}

		public void set(v4 x_, v4 y_, v4 z_, v4 w_)
		{
			x = x_;
			y = y_;
			z = z_;
			w = w_;
		}
		public void set(v4 axis_norm, v4 axis_sine_angle, float cos_angle, v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			rot.set(axis_norm, axis_sine_angle, cos_angle);
			pos = translation;
		}
		public void set(v4 axis_norm, float angle, v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			set(axis_norm, axis_norm * (float)Math.Sin(angle), (float)Math.Cos(angle), translation);
		}
		public void set(v4 from, v4 to, v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			rot.set(from, to);
			pos = translation;
		}
		public void set(float pitch, float yaw, float roll, v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			rot.set(pitch, yaw, roll);
			pos = translation;
		}
		public void set(v4 quaternion, v4 translation)
		{
			rot.set(quaternion);
			pos = translation;
		}

		// Static m4x4 types
		private readonly static m4x4 m_zero;
		private readonly static m4x4 m_identity;
		static m4x4()
		{
			m_zero = new m4x4(v4.Zero, v4.Zero, v4.Zero, v4.Zero);
			m_identity = new m4x4(v4.XAxis, v4.YAxis, v4.ZAxis, v4.Origin);
		}
		public static m4x4 Zero                             { get { return m_zero; } }
		public static m4x4 Identity                         { get { return m_identity; } }

		// Functions
		public override bool Equals(object o)               { return o is m4x4 && (m4x4)o == this; }
		public override int GetHashCode()                   { unchecked { return x.GetHashCode() + y.GetHashCode() + z.GetHashCode() + w.GetHashCode(); } }
		public static m4x4 operator + (m4x4 lhs, m4x4 rhs)  { return new m4x4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w); }
		public static m4x4 operator - (m4x4 lhs, m4x4 rhs)  { return new m4x4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w); }
		public static m4x4 operator * (m4x4 lhs, float rhs) { return new m4x4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs); }
		public static m4x4 operator * (float lhs, m4x4 rhs) { return new m4x4(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w); }
		public static m4x4 operator / (m4x4 lhs, float rhs) { return new m4x4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs); }
		public static bool operator ==(m4x4 lhs, m4x4 rhs)  { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w; }
		public static bool operator !=(m4x4 lhs, m4x4 rhs)  { return !(lhs == rhs); }
		public static v4 operator * (m4x4 lhs, v4 rhs)
		{
			Transpose4x4(ref lhs);
			return new v4(
				v4.Dot4(lhs.x, rhs),
				v4.Dot4(lhs.y, rhs),
				v4.Dot4(lhs.z, rhs),
				v4.Dot4(lhs.w, rhs));
		}
		public static m4x4 operator * (m4x4 lhs, m4x4 rhs)
		{
			Transpose4x4(ref lhs);
			return new m4x4(
				new v4(v4.Dot4(lhs.x, rhs.x), v4.Dot4(lhs.y, rhs.x), v4.Dot4(lhs.z, rhs.x), v4.Dot4(lhs.w, rhs.x)),
				new v4(v4.Dot4(lhs.x, rhs.y), v4.Dot4(lhs.y, rhs.y), v4.Dot4(lhs.z, rhs.y), v4.Dot4(lhs.w, rhs.y)),
				new v4(v4.Dot4(lhs.x, rhs.z), v4.Dot4(lhs.y, rhs.z), v4.Dot4(lhs.z, rhs.z), v4.Dot4(lhs.w, rhs.z)),
				new v4(v4.Dot4(lhs.x, rhs.w), v4.Dot4(lhs.y, rhs.w), v4.Dot4(lhs.z, rhs.w), v4.Dot4(lhs.w, rhs.w)));
		}

		/// <summary>Create a translation matrix</summary>
		public static m4x4 Translation(float dx, float dy, float dz)
		{
			return Translation(new v4(dx, dy, dz, 1f));
		}
		public static m4x4 Translation(v2 dxy, float dz)
		{
			return Translation(new v4(dxy, dz, 1f));
		}
		public static m4x4 Translation(v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			return new m4x4(m3x4.Identity, translation);
		}

		/// <summary>Create a rotation matrix</summary>
		public static m4x4 Rotation(v4 axis_norm, float angle, v4 translation)
		{
			return new m4x4(axis_norm, angle, translation);
		}
		public static m4x4 Rotation(v4 from, v4 to, v4 translation)
		{
			return new m4x4(from, to, translation);
		}
		public static m4x4 Rotation(float pitch, float yaw, float roll, v4 translation)
		{
			return new m4x4(pitch, yaw, roll, translation);
		}

		/// <summary>Create a scale matrix</summary>
		public static m4x4 Scale(float s, v4 translation)
		{
			return new m4x4(s*v4.XAxis, s*v4.YAxis, s*v4.ZAxis, translation);
		}
		public static m4x4 Scale(float sx, float sy, float sz, v4 translation)
		{
			return new m4x4(sx*v4.XAxis, sy*v4.YAxis, sz*v4.ZAxis, translation);
		}

		/// <summary>Compare matrices</summary>
		public static bool FEql(m4x4 lhs, m4x4 rhs, float tol)
		{
			return v4.FEql4(lhs.x, rhs.x, tol) && v4.FEql4(lhs.y, rhs.y, tol) && v4.FEql4(lhs.z, rhs.z, tol) && v4.FEql4(lhs.w, rhs.w, tol);
		}
		public static bool FEql(m4x4 lhs, m4x4 rhs)
		{
			return FEql(lhs, rhs, Maths.TinyF);
		}

		public static void Transpose3x3(ref m4x4 m)
		{
			m3x4.Transpose3x3(ref m.rot);
		}
		public static m4x4 Transpose3x3(m4x4 m)
		{
			Transpose3x3(ref m);
			return m;
		}
		public static void Transpose4x4(ref m4x4 m)
		{
			Maths.Swap(ref m.x.y, ref m.y.x);
			Maths.Swap(ref m.x.z, ref m.z.x);
			Maths.Swap(ref m.x.w, ref m.w.x);
			Maths.Swap(ref m.y.z, ref m.z.y);
			Maths.Swap(ref m.y.w, ref m.w.y);
			Maths.Swap(ref m.z.w, ref m.w.z);
		}
		public static m4x4 Transpose4x4(m4x4 m)
		{
			Transpose4x4(ref m);
			return m;
		}

		public static void Orthonormalise(ref m4x4 m)
		{
			m3x4.Orthonormalise(ref m.rot);
		}
		public static m4x4 Orthonormalise(m4x4 m)
		{
			Orthonormalise(ref m);
			return m;
		}
		public static bool IsOrthonormal(m4x4 m)
		{
			return	m3x4.IsOrthonormal(m.rot);
		}

		/// <summary>Invert 'm' in-place (assuming an orthonormal matrix</summary>
		public static void InvertFast(ref m4x4 m)
		{
			Debug.Assert(IsOrthonormal(m), "Matrix is not orthonormal");
			v4 trans = m.w;
			Transpose3x3(ref m);
			m.w.x = -(trans.x * m.x.x + trans.y * m.y.x + trans.z * m.z.x);
			m.w.y = -(trans.x * m.x.y + trans.y * m.y.y + trans.z * m.z.y);
			m.w.z = -(trans.x * m.x.z + trans.y * m.y.z + trans.z * m.z.z);
		}
		
		/// <summary>Return 'm' inverted (assuming an orthonormal matrix</summary>
		public static m4x4 InvertFast(m4x4 m)
		{
			InvertFast(ref m);
			return m;
		}

		/// <summary>Return 'm' inverted</summary>
		public static m4x4 Invert(m4x4 m)
		{
			var A = Transpose4x4(m); // Take the transpose so that row operations are faster
			var B = Identity;

			// Loop through columns of 'A'
			for (int j = 0; j != 4; ++j)
			{
				// Select the pivot element: maximum magnitude in this row
				var pivot = 0; var val = 0f;
				if (j <= 0 && val < Math.Abs(A.x[j])) { pivot = 0; val = Math.Abs(A.x[j]); }
				if (j <= 1 && val < Math.Abs(A.y[j])) { pivot = 1; val = Math.Abs(A.y[j]); }
				if (j <= 2 && val < Math.Abs(A.z[j])) { pivot = 2; val = Math.Abs(A.z[j]); }
				if (j <= 3 && val < Math.Abs(A.w[j])) { pivot = 3; val = Math.Abs(A.w[j]); }
				if (val < Maths.TinyF)
				{
					Debug.Assert(false, "Matrix has no inverse");
					return m;
				}

				// Interchange rows to put pivot element on the diagonal
				if (pivot != j) // skip if already on diagonal
				{
					var a = A[j]; A[j] = A[pivot]; A[pivot] = a;
					var b = B[j]; B[j] = B[pivot]; B[pivot] = b;
				}

				// Divide row by pivot element. Pivot element becomes 1.0f
				var scale = A[j][j];
				A[j] /= scale;
				B[j] /= scale;

				// Subtract this row from others to make the rest of column j zero
				if (j != 0) { scale = A.x[j]; A.x -= scale * A[j]; B.x -= scale * B[j]; }
				if (j != 1) { scale = A.y[j]; A.y -= scale * A[j]; B.y -= scale * B[j]; }
				if (j != 2) { scale = A.z[j]; A.z -= scale * A[j]; B.z -= scale * B[j]; }
				if (j != 3) { scale = A.w[j]; A.w -= scale * A[j]; B.w -= scale * B[j]; }
			}

			// When these operations have been completed, A should have been transformed to the identity matrix
			// and B should have been transformed into the inverse of the original A
			B = Transpose4x4(B);
			return B;
		}

		// Permute the rotation vectors in a matrix by 'n'
		public static m4x4 Permute(m4x4 mat, int n)
		{
			switch (n%3)
			{
			default:return mat;
			case 1: return new m4x4(mat.y, mat.z, mat.x, mat.w);
			case 2: return new m4x4(mat.z, mat.x, mat.y, mat.w);
			}
		}

		// Make an orientation matrix from a direction. Note the rotation around the direction
		// vector is not defined. 'axis' is the axis that 'direction' will become
		public static m4x4 OriFromDir(v4 direction, AxisId axis, v4 preferred_up, v4 translation)
		{
			Debug.Assert(Maths.FEql(translation.w, 1f), "'translation' must be a position vector");
			return new m4x4(m3x4.OriFromDir(direction, axis, preferred_up), translation);
		}
		public static m4x4 OriFromDir(v4 direction, AxisId axis, v4 translation)
		{
			return OriFromDir(direction, axis, v4.Perpendicular(direction), translation);
		}

		/// <summary>
		/// Return the cross product matrix for 'vec'. This matrix can be used to take the cross
		/// product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix4x4(v1) * v2</summary>
		public static m4x4 CrossProductMatrix4x4(v4 vec)
		{
			return new m4x4(
				new v4(+0.0f,   vec.z, -vec.y, 0.0f),
				new v4(-vec.z,   0.0f,  vec.x, 0.0f),
				new v4(+vec.y, -vec.x,   0.0f, 0.0f),
				v4.Zero);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class UnitTestM4x4
	{
		[Test] public void Identity()
		{
			var m1 = m4x4.Identity;
			var m2 = m4x4.Identity;
			var m3 = m1 * m2;
			Assert.True(m4x4.FEql(m3, m4x4.Identity));
		}
		[Test] public void Translation()
		{
			var m1 = new m4x4(v4.XAxis, v4.YAxis, v4.ZAxis, new v4(1.0f, 2.0f, 3.0f, 1.0f));
			var m2 = m4x4.Translation(new v4(1.0f, 2.0f, 3.0f, 1.0f));
			Assert.True(m4x4.FEql(m1, m2));
		}
		[Test] public void CreateFrom()
		{
			var rnd = new Rand();
			var V1 = v4.Random3(0.0f, 10.0f, 1.0f, rnd);
			var a2b = new m4x4(v4.Random3N(0.0f, rnd), rnd.FloatC(0, Maths.TauBy2), v4.Random3(0.0f, 10.0f, 1.0f, rnd));
			var b2c = new m4x4(v4.Random3N(0.0f, rnd), rnd.FloatC(0, Maths.TauBy2), v4.Random3(0.0f, 10.0f, 1.0f, rnd));
			Assert.True(m4x4.IsOrthonormal(a2b));
			Assert.True(m4x4.IsOrthonormal(b2c));

			var V2  = a2b * V1;
			var V3  = b2c * V2;
			var a2c = b2c * a2b;
			var V4  = a2c * V1;
			Assert.True(v4.FEql4(V3, V4));
		}
		[Test] public void CreateFrom2()
		{
			var m1 = m4x4.Rotation(1.0f, 0.5f, 0.7f, v4.Origin);
			var m2 = new m4x4(quat.Make(1.0f, 0.5f, 0.7f), v4.Origin);
			Assert.True(m4x4.IsOrthonormal(m1));
			Assert.True(m4x4.IsOrthonormal(m2));
			Assert.True(m4x4.FEql(m1, m2));

			var rnd = new Rand();
			var ang = rnd.FloatC(0.0f,1.0f);
			var axis = v4.Random3N(0.0f, rnd);
			m1 = m4x4.Rotation(axis, ang, v4.Origin);
			m2 = new m4x4(quat.Make(axis, ang), v4.Origin);
			Assert.True(m4x4.IsOrthonormal(m1));
			Assert.True(m4x4.IsOrthonormal(m2));
			Assert.True(m4x4.FEql(m1, m2));
		}
		[Test] public void CreateFrom3()
		{
			var rnd = new Rand();
			var a2b = new m4x4(v4.Random3N(0.0f, rnd), rnd.FloatC(0.0f,1.0f), v4.Random3(0.0f, 10.0f, 1.0f, rnd));

			var b2a = m4x4.Invert(a2b);
			var a2a = b2a * a2b;
			Assert.True(m4x4.FEql(m4x4.Identity, a2a));

			var b2a_fast = m4x4.InvertFast(a2b);
			Assert.True(m4x4.FEql(b2a_fast, b2a));
		}
		[Test] public void Orthonormalise()
		{
			var a2b = new m4x4();
			a2b.x = new v4(-2.0f , 3.0f , 1.0f , 0.0f);
			a2b.y = new v4(4.0f  ,-1.0f , 2.0f , 0.0f);
			a2b.z = new v4(1.0f  ,-2.0f , 4.0f , 0.0f);
			a2b.w = new v4(1.0f  , 2.0f , 3.0f , 1.0f);
			Assert.True(m4x4.IsOrthonormal(m4x4.Orthonormalise(a2b)));
		}
	}
}
#endif