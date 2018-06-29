//***************************************************
// Matrix4x4
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Rylogic.Extn;

namespace Rylogic.Maths
{
	/// <summary>4x4 Matrix</summary>
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{x}  {y}  {z}  {w}")]
	public struct m4x4
	{
		[FieldOffset( 0)] public v4   x;
		[FieldOffset(16)] public v4   y;
		[FieldOffset(32)] public v4   z;
		[FieldOffset(48)] public v4   w;

		[FieldOffset( 0)] public m3x4 rot;
		[FieldOffset(48)] public v4   pos;

		/// <summary>Constructors</summary>
		public m4x4(v4 x, v4 y, v4 z, v4 w) :this()
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
		public m4x4(m3x4 rot, v4 pos) :this()
		{
			this.rot = rot;
			this.pos = pos;
		}
		public m4x4(quat q, v4 pos) :this()
		{
			this.rot = new m3x4(q);
			this.pos = pos;
		}
		public m4x4(float[] arr, int start = 0)
			:this(new v4(arr, 0), new v4(arr, 4), new v4(arr, 8), new v4(arr, 12))
		{}

		/// <summary>Get/Set columns by index</summary>
		public v4 this[int c]
		{
			get
			{
				switch (c) {
				case 0: return x;
				case 1: return y;
				case 2: return z;
				case 3: return w;
				}
				throw new ArgumentException("index out of range", "i");
			}
			set
			{
				switch (c) {
				case 0: x = value; return;
				case 1: y = value; return;
				case 2: z = value; return;
				case 3: w = value; return;
				}
				throw new ArgumentException("index out of range", "i");
			}
		}
		public v4 this[uint c]
		{
			get { return this[(int)c]; }
			set { this[(int)c] = value; }
		}

		/// <summary>Get/Set components by index. Note: row,col is standard (i.e. as in Matlab)</summary>
		public float this[int r, int c]
		{
			get { return this[c][r]; }
			set
			{
				var vec = this[c];
				vec[r] = value;
				this[c] = vec;
			}
		}
		public float this[uint r, uint c]
		{
			get { return this[(int)c][(int)r]; }
			set
			{
				var vec = this[c];
				vec[r] = value;
				this[c] = vec;
			}
		}
		[Obsolete]
		public float get(int r, int c)
		{
			return this[r, c];
		}
		[Obsolete]
		public void set(int r, int c, float value)
		{
			this[r, c] = value;
		}

		/// <summary>ToString()</summary>
		public override string ToString()
		{
			return $"{x} \n{y} \n{z} \n{w} \n";
		}
		public string ToString3x4()
		{
			return $"{x.ToString3()} \n{y.ToString3()} \n{z.ToString3()} \n{w.ToString3()} \n";
		}

		/// <summary>To flat array</summary>
		public float[] ToArray()
		{
			return new []
			{
				x.x, x.y, x.z, x.w,
				y.x, y.y, y.z, y.w,
				z.x, z.y, z.z, z.w,
				w.x, w.y, w.z, w.w,
			};
		}

		/// <summary>Static m4x4 types</summary>
		private readonly static m4x4 m_zero = new m4x4(v4.Zero, v4.Zero, v4.Zero, v4.Zero);
		private readonly static m4x4 m_identity = new m4x4(v4.XAxis, v4.YAxis, v4.ZAxis, v4.Origin);
		public static m4x4 Zero
		{
			get { return m_zero; }
		}
		public static m4x4 Identity
		{
			get { return m_identity; }
		}

		// Functions
		public static m4x4 operator + (m4x4 lhs, m4x4 rhs)
		{
			return new m4x4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
		}
		public static m4x4 operator - (m4x4 lhs, m4x4 rhs)
		{
			return new m4x4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);
		}
		public static m4x4 operator * (m4x4 lhs, float rhs)
		{
			return new m4x4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs);
		}
		public static m4x4 operator * (float lhs, m4x4 rhs)
		{
			return new m4x4(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z, lhs * rhs.w);
		}
		public static m4x4 operator / (m4x4 lhs, float rhs)
		{
			return new m4x4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs, lhs.w / rhs);
		}
		public static v4   operator * (m4x4 lhs, v4 rhs)
		{
			Transpose4x4(ref lhs);
			return new v4(
				Math_.Dot(lhs.x, rhs),
				Math_.Dot(lhs.y, rhs),
				Math_.Dot(lhs.z, rhs),
				Math_.Dot(lhs.w, rhs));
		}
		public static m4x4 operator * (m4x4 lhs, m4x4 rhs)
		{
			Transpose4x4(ref lhs);
			return new m4x4(
				new v4(Math_.Dot(lhs.x, rhs.x), Math_.Dot(lhs.y, rhs.x), Math_.Dot(lhs.z, rhs.x), Math_.Dot(lhs.w, rhs.x)),
				new v4(Math_.Dot(lhs.x, rhs.y), Math_.Dot(lhs.y, rhs.y), Math_.Dot(lhs.z, rhs.y), Math_.Dot(lhs.w, rhs.y)),
				new v4(Math_.Dot(lhs.x, rhs.z), Math_.Dot(lhs.y, rhs.z), Math_.Dot(lhs.z, rhs.z), Math_.Dot(lhs.w, rhs.z)),
				new v4(Math_.Dot(lhs.x, rhs.w), Math_.Dot(lhs.y, rhs.w), Math_.Dot(lhs.z, rhs.w), Math_.Dot(lhs.w, rhs.w)));
		}

		/// <summary>Return the 4x4 determinant of the arbitrary transform 'mat'</summary>
		public static float Determinant4(m4x4 m)
		{
			float c1 = (m.z.z * m.w.w) - (m.z.w * m.w.z);
			float c2 = (m.z.y * m.w.w) - (m.z.w * m.w.y);
			float c3 = (m.z.y * m.w.z) - (m.z.z * m.w.y);
			float c4 = (m.z.x * m.w.w) - (m.z.w * m.w.x);
			float c5 = (m.z.x * m.w.z) - (m.z.z * m.w.x);
			float c6 = (m.z.x * m.w.y) - (m.z.y * m.w.x);
			return
				m.x.x * (m.y.y*c1 - m.y.z*c2 + m.y.w*c3) -
				m.x.y * (m.y.x*c1 - m.y.z*c4 + m.y.w*c5) +
				m.x.z * (m.y.x*c2 - m.y.y*c4 + m.y.w*c6) -
				m.x.w * (m.y.x*c3 - m.y.y*c5 + m.y.z*c6);
		}

		/// <summary>Transpose the rotation part of an affine transform</summary>
		public static void Transpose3x3(ref m4x4 m)
		{
			m3x4.Transpose(ref m.rot);
		}
		public static m4x4 Transpose3x3(m4x4 m)
		{
			Transpose3x3(ref m);
			return m;
		}

		/// <summary>Transpose the 4x4 matrix</summary>
		public static void Transpose4x4(ref m4x4 m)
		{
			Math_.Swap(ref m.x.y, ref m.y.x);
			Math_.Swap(ref m.x.z, ref m.z.x);
			Math_.Swap(ref m.x.w, ref m.w.x);
			Math_.Swap(ref m.y.z, ref m.z.y);
			Math_.Swap(ref m.y.w, ref m.w.y);
			Math_.Swap(ref m.z.w, ref m.w.z);
		}
		public static m4x4 Transpose4x4(m4x4 m)
		{
			Transpose4x4(ref m);
			return m;
		}

		/// <summary>Orthonormalise the rotation part of an affine transform</summary>
		public static void Orthonormalise(ref m4x4 m)
		{
			m3x4.Orthonormalise(ref m.rot);
		}
		public static m4x4 Orthonormalise(m4x4 m)
		{
			Orthonormalise(ref m);
			return m;
		}

		/// <summary>True if the rotation part of this transform is orthonormal</summary>
		public static bool IsOrthonormal(m4x4 m)
		{
			return	m3x4.IsOrthonormal(m.rot);
		}

		// Return true if 'mat' is an affine transform
		public static bool IsAffine(m4x4 mat)
		{
			return
				mat.x.w == 0.0f &&
				mat.y.w == 0.0f &&
				mat.z.w == 0.0f &&
				mat.w.w == 1.0f;
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

		/// <summary>True if 'mat' has an inverse</summary>
		public static bool IsInvertable(m4x4 m)
		{
			return Determinant4(m) != 0;
		}

		/// <summary>Return 'm' inverted</summary>
		public static m4x4 Invert(m4x4 m)
		{
			Debug.Assert(IsInvertable(m), "Matrix has no inverse");

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
				if (val < Math_.TinyF)
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

		// Return the inverse of 'mat' using double precision floats
		public static m4x4 InvertPrecise(m4x4 mat)
		{
			var inv = new double[4,4];
			inv[0,0] = 0.0 + mat.y.y * mat.z.z * mat.w.w - mat.y.y * mat.z.w * mat.w.z - mat.z.y * mat.y.z * mat.w.w + mat.z.y * mat.y.w * mat.w.z + mat.w.y * mat.y.z * mat.z.w - mat.w.y * mat.y.w * mat.z.z;
			inv[0,1] = 0.0 - mat.x.y * mat.z.z * mat.w.w + mat.x.y * mat.z.w * mat.w.z + mat.z.y * mat.x.z * mat.w.w - mat.z.y * mat.x.w * mat.w.z - mat.w.y * mat.x.z * mat.z.w + mat.w.y * mat.x.w * mat.z.z;
			inv[0,2] = 0.0 + mat.x.y * mat.y.z * mat.w.w - mat.x.y * mat.y.w * mat.w.z - mat.y.y * mat.x.z * mat.w.w + mat.y.y * mat.x.w * mat.w.z + mat.w.y * mat.x.z * mat.y.w - mat.w.y * mat.x.w * mat.y.z;
			inv[0,3] = 0.0 - mat.x.y * mat.y.z * mat.z.w + mat.x.y * mat.y.w * mat.z.z + mat.y.y * mat.x.z * mat.z.w - mat.y.y * mat.x.w * mat.z.z - mat.z.y * mat.x.z * mat.y.w + mat.z.y * mat.x.w * mat.y.z;
			inv[1,0] = 0.0 - mat.y.x * mat.z.z * mat.w.w + mat.y.x * mat.z.w * mat.w.z + mat.z.x * mat.y.z * mat.w.w - mat.z.x * mat.y.w * mat.w.z - mat.w.x * mat.y.z * mat.z.w + mat.w.x * mat.y.w * mat.z.z;
			inv[1,1] = 0.0 + mat.x.x * mat.z.z * mat.w.w - mat.x.x * mat.z.w * mat.w.z - mat.z.x * mat.x.z * mat.w.w + mat.z.x * mat.x.w * mat.w.z + mat.w.x * mat.x.z * mat.z.w - mat.w.x * mat.x.w * mat.z.z;
			inv[1,2] = 0.0 - mat.x.x * mat.y.z * mat.w.w + mat.x.x * mat.y.w * mat.w.z + mat.y.x * mat.x.z * mat.w.w - mat.y.x * mat.x.w * mat.w.z - mat.w.x * mat.x.z * mat.y.w + mat.w.x * mat.x.w * mat.y.z;
			inv[1,3] = 0.0 + mat.x.x * mat.y.z * mat.z.w - mat.x.x * mat.y.w * mat.z.z - mat.y.x * mat.x.z * mat.z.w + mat.y.x * mat.x.w * mat.z.z + mat.z.x * mat.x.z * mat.y.w - mat.z.x * mat.x.w * mat.y.z;
			inv[2,0] = 0.0 + mat.y.x * mat.z.y * mat.w.w - mat.y.x * mat.z.w * mat.w.y - mat.z.x * mat.y.y * mat.w.w + mat.z.x * mat.y.w * mat.w.y + mat.w.x * mat.y.y * mat.z.w - mat.w.x * mat.y.w * mat.z.y;
			inv[2,1] = 0.0 - mat.x.x * mat.z.y * mat.w.w + mat.x.x * mat.z.w * mat.w.y + mat.z.x * mat.x.y * mat.w.w - mat.z.x * mat.x.w * mat.w.y - mat.w.x * mat.x.y * mat.z.w + mat.w.x * mat.x.w * mat.z.y;
			inv[2,2] = 0.0 + mat.x.x * mat.y.y * mat.w.w - mat.x.x * mat.y.w * mat.w.y - mat.y.x * mat.x.y * mat.w.w + mat.y.x * mat.x.w * mat.w.y + mat.w.x * mat.x.y * mat.y.w - mat.w.x * mat.x.w * mat.y.y;
			inv[2,3] = 0.0 - mat.x.x * mat.y.y * mat.z.w + mat.x.x * mat.y.w * mat.z.y + mat.y.x * mat.x.y * mat.z.w - mat.y.x * mat.x.w * mat.z.y - mat.z.x * mat.x.y * mat.y.w + mat.z.x * mat.x.w * mat.y.y;
			inv[3,0] = 0.0 - mat.y.x * mat.z.y * mat.w.z + mat.y.x * mat.z.z * mat.w.y + mat.z.x * mat.y.y * mat.w.z - mat.z.x * mat.y.z * mat.w.y - mat.w.x * mat.y.y * mat.z.z + mat.w.x * mat.y.z * mat.z.y;
			inv[3,1] = 0.0 + mat.x.x * mat.z.y * mat.w.z - mat.x.x * mat.z.z * mat.w.y - mat.z.x * mat.x.y * mat.w.z + mat.z.x * mat.x.z * mat.w.y + mat.w.x * mat.x.y * mat.z.z - mat.w.x * mat.x.z * mat.z.y;
			inv[3,2] = 0.0 - mat.x.x * mat.y.y * mat.w.z + mat.x.x * mat.y.z * mat.w.y + mat.y.x * mat.x.y * mat.w.z - mat.y.x * mat.x.z * mat.w.y - mat.w.x * mat.x.y * mat.y.z + mat.w.x * mat.x.z * mat.y.y;
			inv[3,3] = 0.0 + mat.x.x * mat.y.y * mat.z.z - mat.x.x * mat.y.z * mat.z.y - mat.y.x * mat.x.y * mat.z.z + mat.y.x * mat.x.z * mat.z.y + mat.z.x * mat.x.y * mat.y.z - mat.z.x * mat.x.z * mat.y.y;

			var det = mat.x.x * inv[0,0] + mat.x.y * inv[1,0] + mat.x.z * inv[2,0] + mat.x.w * inv[3,0];
			Debug.Assert(det != 0, "matrix has no inverse");
			var inv_det = 1.0 / det;

			return new m4x4(
				new v4((float)(inv[0,0] * inv_det), (float)(inv[0,1] * inv_det), (float)(inv[0,2] * inv_det), (float)(inv[0,3] * inv_det)),
				new v4((float)(inv[1,0] * inv_det), (float)(inv[1,1] * inv_det), (float)(inv[1,2] * inv_det), (float)(inv[1,3] * inv_det)),
				new v4((float)(inv[2,0] * inv_det), (float)(inv[2,1] * inv_det), (float)(inv[2,2] * inv_det), (float)(inv[2,3] * inv_det)),
				new v4((float)(inv[3,0] * inv_det), (float)(inv[3,1] * inv_det), (float)(inv[3,2] * inv_det), (float)(inv[3,3] * inv_det)));
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
			Debug.Assert(Math_.FEql(translation.w, 1f), "'translation' must be a position vector");
			return new m4x4(m3x4.OriFromDir(direction, axis, preferred_up), translation);
		}
		public static m4x4 OriFromDir(v4 direction, AxisId axis, v4 translation)
		{
			return OriFromDir(direction, axis, Math_.Perpendicular(direction), translation);
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
			Debug.Assert(Math_.FEql(translation.w, 1f), "'translation' must be a position vector");
			return new m4x4(m3x4.Identity, translation);
		}

		/// <summary>Create a rotation/translation matrix</summary>
		public static m4x4 Transform(m3x4 rot, v4 translation)
		{
			return new m4x4(rot, translation);
		}
		public static m4x4 Transform(float pitch, float yaw, float roll, v4 translation)
		{
			return new m4x4(m3x4.Rotation(pitch, yaw, roll), translation);
		}
		public static m4x4 Transform(v4 axis_norm, float angle, v4 translation)
		{
			return new m4x4(m3x4.Rotation(axis_norm, angle), translation);
		}
		public static m4x4 Transform(v4 angular_displacement, v4 translation)
		{
			return new m4x4(m3x4.Rotation(angular_displacement), translation);
		}
		public static m4x4 Transform(v4 from, v4 to, v4 translation)
		{
			return new m4x4(m3x4.Rotation(from, to), translation);
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

		// Create a shear matrix
		public static m4x4 Shear(float sxy, float sxz, float syx, float syz, float szx, float szy, v4 translation)
		{
			return new m4x4(m3x4.Shear(sxy, sxz, syx, syz, szx, szy), translation);
		}

		// Orientation matrix to "look" at a point
		public static m4x4 LookAt(v4 eye, v4 at, v4 up)
		{
			Debug.Assert(eye.w == 1.0f && at.w == 1.0f && up.w == 0.0f, "Invalid position/direction vectors passed to LookAt");
			Debug.Assert(eye - at != v4.Zero, "LookAt 'eye' and 'at' positions are coincident");
			Debug.Assert(!Math_.Parallel(eye - at, up), "LookAt 'forward' and 'up' axes are aligned");
			var mat = new m4x4{};
			mat.z = Math_.Normalise(eye - at);
			mat.x = Math_.Normalise(Math_.Cross(up, mat.z));
			mat.y = Math_.Cross(mat.z, mat.x);
			mat.pos = eye;
			return mat;
		}

		// Construct an orthographic projection matrix
		public static m4x4 ProjectionOrthographic(float w, float h, float zn, float zf, bool righthanded)
		{
			Debug.Assert(Math_.IsFinite(w) && Math_.IsFinite(h) && w > 0 && h > 0, "invalid view rect");
			Debug.Assert(Math_.IsFinite(zn) && Math_.IsFinite(zf) && (zn - zf) != 0, "invalid near/far planes");
			var rh = Math_.SignF(righthanded);
			var mat = new m4x4{};
			mat.x.x = 2.0f / w;
			mat.y.y = 2.0f / h;
			mat.z.z = rh / (zn - zf);
			mat.w.w = 1.0f;
			mat.w.z = rh * zn / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix
		public static m4x4 ProjectionPerspective(float w, float h, float zn, float zf, bool righthanded)
		{
			Debug.Assert(Math_.IsFinite(w) && Math_.IsFinite(h) && w > 0 && h > 0, "invalid view rect");
			Debug.Assert(Math_.IsFinite(zn) && Math_.IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0, "invalid near/far planes");
			var rh = Math_.SignF(righthanded);
			var mat = new m4x4{};
			mat.x.x = 2.0f * zn / w;
			mat.y.y = 2.0f * zn / h;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix offset from the centre
		public static m4x4 ProjectionPerspective(float l, float r, float t, float b, float zn, float zf, bool righthanded)
		{
			Debug.Assert(Math_.IsFinite(l)  && Math_.IsFinite(r) && Math_.IsFinite(t) && Math_.IsFinite(b) && (r - l) > 0 && (t - b) > 0, "invalid view rect");
			Debug.Assert(Math_.IsFinite(zn) && Math_.IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0, "invalid near/far planes");
			var rh = Math_.SignF(righthanded);
			var mat = new m4x4{};
			mat.x.x = 2.0f * zn / (r - l);
			mat.y.y = 2.0f * zn / (t - b);
			mat.z.x = rh * (r + l) / (r - l);
			mat.z.y = rh * (t + b) / (t - b);
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		// Construct a perspective projection matrix using field of view
		public static m4x4 ProjectionPerspectiveFOV(float fovY, float aspect, float zn, float zf, bool righthanded)
		{
			Debug.Assert(Math_.IsFinite(aspect) && aspect > 0, "invalid aspect ratio");
			Debug.Assert(Math_.IsFinite(zn) && Math_.IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0, "invalid near/far planes");
			var rh = Math_.SignF(righthanded);
			var mat = new m4x4{};
			mat.y.y = (float)(1 / Math.Tan(fovY/2));
			mat.x.x = mat.y.y / aspect;
			mat.z.w = -rh;
			mat.z.z = rh * zf / (zn - zf);
			mat.w.z = zn * zf / (zn - zf);
			return mat;
		}

		/// <summary>Spherically interpolate between two affine transforms</summary>
		public static m4x4 Slerp(m4x4 lhs, m4x4 rhs, float frac)
		{
			Debug.Assert(IsAffine(lhs));
			Debug.Assert(IsAffine(rhs));

			var q = quat.Slerp(new quat(lhs.rot), new quat(rhs.rot), frac);
			var p = Math_.Lerp(lhs.pos, rhs.pos, frac);
			return new m4x4(q, p);
		}

		/// <summary>Return the average of a collection of affine transforms</summary>
		public static m4x4 Average(IEnumerable<m4x4> a2b)
		{
			var rot = m3x4.Average(a2b.Select(x => x.rot));
			var pos = Math_.Average(a2b.Select(x => x.pos));
			return new m4x4(rot, pos);
		}

		#region Random

		// Create a random 4x4 matrix
		public static m4x4 Random4x4(float min, float max, Random r)
		{
			return new m4x4(
				v4.Random4(min, max, r),
				v4.Random4(min, max, r),
				v4.Random4(min, max, r),
				v4.Random4(min, max, r));
		}

		// Create a random affine transform matrix
		public static m4x4 Random4x4(v4 axis, float min_angle, float max_angle, v4 position, Random r)
		{
			return Transform(axis, r.Float(min_angle, max_angle), position);
		}
		public static m4x4 Random4x4(float min_angle, float max_angle, v4 position, Random r)
		{
			return Random4x4(v4.Random3N(0.0f, r), min_angle, max_angle, position, r);
		}
		public static m4x4 Random4x4(v4 axis, float min_angle, float max_angle, v4 centre, float radius, Random r)
		{
			return Random4x4(axis, min_angle, max_angle, centre + v4.Random3(0.0f, radius, 0.0f, r), r);
		}
		public static m4x4 Random4x4(float min_angle, float max_angle, v4 centre, float radius, Random r)
		{
			return Random4x4(v4.Random3N(0.0f, r), min_angle, max_angle, centre, radius, r);
		}
		public static m4x4 Random4x4(v4 centre, float radius, Random r)
		{
			return Random4x4(v4.Random3N(0.0f, r), 0.0f, (float)Math_.Tau, centre, radius, r);
		}

		#endregion

		#region Equals
		public static bool operator == (m4x4 lhs, m4x4 rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
		}
		public static bool operator != (m4x4 lhs, m4x4 rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is m4x4 && (m4x4)o == this;
		}
		public override int GetHashCode()
		{
			return new { x, y, z, w }.GetHashCode();
		}
		#endregion
	}

	public static partial class Math_
	{
		/// <summary>Approximate equal</summary>
		public static bool FEqlRelative(m4x4 lhs, m4x4 rhs, float tol)
		{
			return
				FEqlRelative(lhs.x, rhs.x, tol) &&
				FEqlRelative(lhs.y, rhs.y, tol) &&
				FEqlRelative(lhs.z, rhs.z, tol) &&
				FEqlRelative(lhs.w, rhs.w, tol);
		}
		public static bool FEql(m4x4 lhs, m4x4 rhs)
		{
			return FEqlRelative(lhs, rhs, TinyF);
		}

		public static bool IsFinite(m4x4 vec)
		{
			return IsFinite(vec.x) && IsFinite(vec.y) && IsFinite(vec.z) && IsFinite(vec.w);
		}

		/// <summary>Spherically interpolate between two affine transforms</summary>
		public static m4x4 Slerp(m4x4 lhs, m4x4 rhs, float frac)
		{
			return m4x4.Slerp(lhs, rhs, frac);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Maths;

	[TestFixture] public class UnitTestM4x4
	{
		[Test] public void Basic()
		{
			var rng = new Random(1);
			var m = m4x4.Random4x4(-5, +5, rng);

			Assert.True(m.x.x == m[0][0]);
			Assert.True(m.z.y == m[2][1]);
			Assert.True(m.w.x == m[3][0]);
		}
		[Test] public void Identity()
		{
			var m1 = m4x4.Identity;
			var m2 = m4x4.Identity;
			var m3 = m1 * m2;
			Assert.True(Math_.FEql(m3, m4x4.Identity));
		}
		[Test] public void Translation()
		{
			var m1 = new m4x4(v4.XAxis, v4.YAxis, v4.ZAxis, new v4(1.0f, 2.0f, 3.0f, 1.0f));
			var m2 = m4x4.Translation(new v4(1.0f, 2.0f, 3.0f, 1.0f));
			Assert.True(Math_.FEql(m1, m2));
		}
		[Test] public void CreateFrom()
		{
			var rnd = new Random(123456789);
			var V1 = v4.Random3(0.0f, 10.0f, 1.0f, rnd);
			var a2b = m4x4.Transform(v4.Random3N(0.0f, rnd), rnd.FloatC(0, (float)Math_.TauBy2), v4.Random3(0.0f, 10.0f, 1.0f, rnd));
			var b2c = m4x4.Transform(v4.Random3N(0.0f, rnd), rnd.FloatC(0, (float)Math_.TauBy2), v4.Random3(0.0f, 10.0f, 1.0f, rnd));
			Assert.True(m4x4.IsOrthonormal(a2b));
			Assert.True(m4x4.IsOrthonormal(b2c));

			var V2  = a2b * V1;
			var V3  = b2c * V2;
			var a2c = b2c * a2b;
			var V4  = a2c * V1;
			Assert.True(Math_.FEql(V3, V4));
		}
		[Test] public void CreateFrom2()
		{
			var m1 = m4x4.Transform(1.0f, 0.5f, 0.7f, v4.Origin);
			var m2 = new m4x4(new quat(1.0f, 0.5f, 0.7f), v4.Origin);
			Assert.True(m4x4.IsOrthonormal(m1));
			Assert.True(m4x4.IsOrthonormal(m2));
			Assert.True(Math_.FEql(m1, m2));

			var rng = new Random(123456879);
			var ang = rng.FloatC(0.0f,1.0f);
			var axis = v4.Random3N(0.0f, rng);
			m1 = m4x4.Transform(axis, ang, v4.Origin);
			m2 = new m4x4(new quat(axis, ang), v4.Origin);
			Assert.True(m4x4.IsOrthonormal(m1));
			Assert.True(m4x4.IsOrthonormal(m2));
			Assert.True(Math_.FEql(m1, m2));
		}
		[Test] public void CreateFrom3()
		{
			var rng = new Random(123456789);
			var a2b = m4x4.Transform(v4.Random3N(0.0f, rng), rng.FloatC(0.0f,1.0f), v4.Random3(0.0f, 10.0f, 1.0f, rng));

			var b2a = m4x4.Invert(a2b);
			var a2a = b2a * a2b;
			Assert.True(Math_.FEql(m4x4.Identity, a2a));

			var b2a_fast = m4x4.InvertFast(a2b);
			Assert.True(Math_.FEql(b2a_fast, b2a));
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