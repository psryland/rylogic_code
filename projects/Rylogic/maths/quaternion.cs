//***************************************************
// Quaternion
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using pr.extn;

namespace pr.maths
{
	/// <summary>Quaternion functions. Note: a quaternion is a v4</summary>
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{x}  {y}  {z}  {w}  // Len3={Length3}  Len4={Length4})")]
	public struct quat
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;
		[FieldOffset( 8)] public float z;
		[FieldOffset(12)] public float w;
		[FieldOffset( 0)] public v4 vec;

		public quat(float x, float y, float z, float w) :this()
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}

		/// <summary>Create a quaternion from an axis and an angle</summary>
		public quat(v4 axis, float angle) :this()
		{
			var s = (float)Math.Sin(0.5 * angle);
			x = s * axis.x;
			y = s * axis.y;
			z = s * axis.z;
			w = (float)Math.Cos(0.5 * angle);
		}

		/// <summary>Create a quaternion from an angular displacement vector. length = angle(rad), direction = axis</summary>
		public quat(v4 angular_displacement) :this()
		{
			var len = angular_displacement.Length3;
			this = new quat(angular_displacement / len, len);
		}

		/// <summary>Create a quaternion from Euler angles. Order is: roll, pitch, yaw (to match DirectX)</summary>
		public quat(float pitch, float yaw, float roll) :this()
		{
			// nicked from 'XMQuaternionRotationRollPitchYaw'
			float cos_p = (float)Math.Cos(pitch * 0.5), sin_p = (float)Math.Sin(pitch * 0.5);
			float cos_y = (float)Math.Cos(yaw   * 0.5), sin_y = (float)Math.Sin(yaw   * 0.5);
			float cos_r = (float)Math.Cos(roll  * 0.5), sin_r = (float)Math.Sin(roll  * 0.5);
			x = sin_p * cos_y * cos_r + cos_p * sin_y * sin_r;
			y = cos_p * sin_y * cos_r - sin_p * cos_y * sin_r;
			z = cos_p * cos_y * sin_r - sin_p * sin_y * cos_r;
			w = cos_p * cos_y * cos_r + sin_p * sin_y * sin_r;
		}

		/// <summary>Create a quaternion from a rotation matrix</summary>
		public quat(m3x4 m) :this()
		{
			var trace = m.x.x + m.y.y + m.z.z;
			if (trace >= 0.0f)
			{
				var r = (float)Math.Sqrt(1.0f + trace);
				var s = 0.5f / r;
				x = (m.z.y - m.y.z) * s;
				y = (m.x.z - m.z.x) * s;
				z = (m.y.x - m.x.y) * s;
				w = 0.5f * r;
			}
			else
			{
				int i = 0;
				if (m.y.y > m.x.x  ) ++i;
				if (m.z.z > m[i][i]) ++i;
				int j = (i + 1) % 3;
				int k = (i + 2) % 3;
				var r = (float)Math.Sqrt(m[i][i] - m[j][j] - m[k][k] + 1.0f);
				var s = 0.5f / r;

				this[i] = 0.5f * r;
				this[j] = (m[i][j] + m[j][i]) * s;
				this[k] = (m[k][i] + m[i][k]) * s;
				w       = (m[k][j] - m[j][k]) * s;
			}
		}
	
		/// <summary>Construct a quaternion representing a rotation from 'from' to 'to'</summary>
		public quat(v4 from, v4 to) :this()
		{
			var d = v4.Dot3(from, to); 
			var s = (float)Math.Sqrt(from.Length3Sq * to.Length3Sq) + d;
			var axis = v4.Cross3(from, to);

			// vectors are 180 degrees apart
			if (Maths.FEql(s, 0.0f))
			{
				axis = v4.Perpendicular(to);
				s = 0.0f;
			}

			vec = v4.Normalise4(new v4(axis.x, axis.y, axis.z, s));
		}

		/// <summary>Reinterpret a vector as a quaternion</summary>
		public static quat From(v4 vec)
		{
			return new quat(vec.x, vec.y, vec.z, vec.w);
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

		/// <summary>Length</summary>
		public float LengthSq
		{
			get { return x * x + y * y + z * z + w * w; }
		}
		public float Length
		{
			get { return (float)Math.Sqrt(LengthSq); }
		}

		/// <summary>Get the axis component of the quaternion (normalised)</summary>
		public v4 Axis
		{
			get { return v4.Normalise3(vec.w0); }
		}

		/// <summary>ToString</summary>
		public override string ToString()
		{
			return "{0} {1} {2} {3}".Fmt(x,y,z,w);
		}
		public string ToString(string format)
		{
			return "{0} {1} {2} {3}".Fmt(x.ToString(format),y.ToString(format),z.ToString(format),w.ToString(format));
		}

		/// <summary>ToArray()</summary>
		public float[] ToArray()
		{
			return new float[4]{ x, y, z, w };
		}

		/// <summary>Static quat types</summary>
		private readonly static quat m_zero = new quat(0,0,0,0);
		private readonly static quat m_identity = new quat(0,0,0,1);
		public static quat Zero
		{
			get { return m_zero; }
		}
		public static quat Identity
		{
			get { return m_identity; }
		}

		/// <summary>Compare quaternions</summary>
		public static bool FEql(quat lhs, quat rhs, float tol)
		{
			return
				Maths.FEql(lhs.x, rhs.x, tol) &&
				Maths.FEql(lhs.y, rhs.y, tol) &&
				Maths.FEql(lhs.z, rhs.z, tol) &&
				Maths.FEql(lhs.w, rhs.w, tol);
		}
		public static bool FEql(quat lhs, quat rhs)
		{
			return FEql(lhs, rhs, Maths.TinyF);
		}

		/// <summary>Operators</summary>
		public static quat operator + (quat lhs, quat rhs)
		{
			return new quat(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z, lhs.w + rhs.w);
		}
		public static quat operator - (quat lhs, quat rhs)
		{
			return new quat(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z, lhs.w - rhs.w);
		}
		public static quat operator * (quat lhs, float rhs)
		{
			return new quat(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs, lhs.w * rhs);
		}
		public static quat operator * (float lhs, quat rhs)
		{
			return rhs * lhs;
		}
		public static quat operator * (quat lhs, quat rhs)
		{
			// Quaternion multiply. Same semantics at matrix multiply
			return new quat(
				lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y,
				lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x,
				lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w,
				lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z);
		}
		public static v4   operator * (quat lhs, v4 rhs)
		{
			// Quaternion rotate. Same semantics at matrix multiply
			return Rotate(lhs, rhs);
		}
		public static quat operator / (quat lhs, float rhs)
		{
			return lhs * (1f/rhs);
		}
		
		/// <summary>Normalise a quaternion to unit length</summary>
		public static quat Normalise(quat q)
		{
			return q / q.Length;
		}
		public static quat Normalise(quat q, quat def)
		{
			return Maths.FEql(q.LengthSq, 0f) ? def : q / q.Length;
		}
		
		/// <summary>Return the quaternion conjugate of 'q'</summary>
		public static quat Conjugate(quat q)
		{
			q.x = -q.x;
			q.y = -q.y;
			q.z = -q.z;
			return q;
		}

		/// <summary>Return the axis and angle from a quaternion</summary>
		public static void AxisAngle(quat quat, out v4 axis, out float angle)
		{
			angle = (float)(2.0 * Math.Acos(quat.w));
			var s = (float)Math.Sqrt(1.0f - quat.w * quat.w);
			axis = Maths.FEql(s, 0.0f)
				? v4.Normalise3(new v4(quat.x, quat.y, quat.z, 0.0f))
				: new v4(quat.x/s, quat.y/s, quat.z/s, 0.0f);
		}

		/// <summary>Return possible Euler angles for the quaternion 'q'</summary>
		public static v4 EulerAngles(quat q)
		{
			// From Wikipedia
			double q0 = q.w, q1 = q.x, q2 = q.y, q3 = q.z;
			return new v4(
				(float)Math.Atan2(2.0 * (q0*q1 + q2*q3), 1.0 - 2.0 * (q1*q1 + q2*q2)),
				(float)Math.Asin (2.0 * (q0*q2 - q3*q1)),
				(float)Math.Atan2(2.0 * (q0*q3 + q1*q2), 1.0 - 2.0 * (q2*q2 + q3*q3)),
				0f);
		}

		/// <summary>Linearly interpolate between two quaternions</summary>
		public static quat Lerp(quat lhs, quat rhs, float frac)
		{
			return new quat(
				Maths.Lerp(lhs.x,rhs.x,frac),
				Maths.Lerp(lhs.y,rhs.y,frac),
				Maths.Lerp(lhs.z,rhs.z,frac),
				Maths.Lerp(lhs.w,rhs.w,frac));
		}

		/// <summary>Spherically interpolate between quaternions</summary>
		public static quat Slerp(quat src, quat dst, float frac)
		{
			if (frac <= 0.0f) return src;
			if (frac >= 1.0f) return dst;
			
			var cos_angle = src.x*dst.x + src.y*dst.y + src.z*dst.z + src.w*dst.w;
			var sign      = (cos_angle >= 0) ? 1 : -1;
			var abs_dst   = sign * dst;
			cos_angle    *= sign;

			// Calculate coefficients
			if (cos_angle < 0.95f)
			{
				// standard case ('slerp')
				var angle     = (float)Math.Acos(cos_angle);
				var sin_angle = (float)Math.Sin (angle);
				var scale0    = (float)Math.Sin((1.0f - frac) * angle);
				var scale1    = (float)Math.Sin((       frac) * angle);
				return (scale0*src + scale1*abs_dst) * (1.0f / sin_angle);
			}
			else
			{
				// "src" and "dst" quaternions are very close 
				return quat.Normalise(quat.Lerp(src, abs_dst, frac));
			}
		}

		// this is quaternion multiply
		///// <summary>Rotate 'rotatee' by 'rotator'. If 'rotatee' has w = 0, then this is equivalent to rotating a direction vector</summary>
		//public static quat Rotate(quat rotator, quat rotatee)
		//{
		//	Debug.Assert(Maths.FEql(rotator.LengthSq, 1.0f), "Non-unit quaternion used for rotation");
		//	return rotator * rotatee * Conjugate(rotator);
		//}

		/// <summary>Rotate a vector by a quaternion. This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'</summary>
		public static v4 Rotate(quat lhs, v4 rhs)
		{
			float xx = lhs.x*lhs.x, xy = lhs.x*lhs.y, xz = lhs.x*lhs.z, xw = lhs.x*lhs.w;
			float                   yy = lhs.y*lhs.y, yz = lhs.y*lhs.z, yw = lhs.y*lhs.w;
			float                                     zz = lhs.z*lhs.z, zw = lhs.z*lhs.w;
			float                                                       ww = lhs.w*lhs.w;
			return new v4(
				  ww*rhs.x + 2*yw*rhs.z - 2*zw*rhs.y +   xx*rhs.x + 2*xy*rhs.y + 2*xz*rhs.z -   zz*rhs.x - yy*rhs.x,
				2*xy*rhs.x +   yy*rhs.y + 2*yz*rhs.z + 2*zw*rhs.x -   zz*rhs.y +   ww*rhs.y - 2*xw*rhs.z - xx*rhs.y,
				2*xz*rhs.x + 2*yz*rhs.y +   zz*rhs.z - 2*yw*rhs.x -   yy*rhs.z + 2*xw*rhs.y -   xx*rhs.z + ww*rhs.z,
				rhs.w);
		}

		#region Random

		/// <summary>Construct a random quaternion rotation</summary>
		public static quat Random(v4 axis, float min_angle, float max_angle, Random r)
		{
			return new quat(axis, r.Float(min_angle, max_angle));
		}
		
		/// <summary>Construct a random quaternion rotation</summary>
		public static quat Random(float min_angle, float max_angle, Random r)
		{
			return new quat(v4.Random3N(0f, r), r.Float(min_angle, max_angle));
		}
		
		/// <summary>Construct a random quaternion rotation</summary>
		public static quat Random(Random r)
		{
			return new quat(v4.Random3N(0f, r), r.Float(0f, Maths.Tau));
		}

		#endregion

		#region Equals
		public static bool operator == (quat lhs, quat rhs)
		{
			return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z && lhs.w == rhs.w;
		}
		public static bool operator != (quat lhs, quat rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is quat && (quat)o == this;
		}
		public override int GetHashCode()
		{
			return new { x, y, z, w }.GetHashCode();
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;
	using maths;

	[TestFixture] public class UnitTestQuat
	{
		[Test] public void General()
		{
		}
		[Test] public void EulerAngles()
		{
			#if false
			Action<float,float,float> Check = (p_,y_,r_) =>
			{
				var p = Maths.DegreesToRadians(p_);
				var y = Maths.DegreesToRadians(y_);
				var r = Maths.DegreesToRadians(r_);

				// EulerAngles is not guaranteed to return the same as p,y,r,
				// only angles that combined will create the same rotation as p,y,r.
				var q0    = quat.Make(p, y, r);
				var euler = quat.EulerAngles(q0);
				var q1    = quat.Make(euler.x, euler.y, euler.z);
				Assert.True(quat.FEql(q0, q1));
			};

			Check(+10f,0,0);
			Check(-10f,0,0);
			Check(0,+10f,0);
			Check(0,-10f,0);
			Check(0,0,+10f);
			Check(0,0,-10f);

			// Random cases
			var rnd = new Random(1);
			for (int i = 0; i != 100; ++i)
			{
				var pitch = (float)rnd.NextDoubleCentred(0.0, 360.0);
				var yaw   = (float)rnd.NextDoubleCentred(0.0, 360.0);
				var roll  = (float)rnd.NextDoubleCentred(0.0, 360.0);
				Check(pitch, yaw, roll);
			}

			// Special cases
			var angles = new []{0f, 90f, -90f, 180f, -180f, 270f, -270f, 360f, -360f};
			foreach (var p in angles)
				foreach (var y in angles)
					foreach (var r in angles)
						Check(p, y, r);
			#endif
		}
	}
}
#endif