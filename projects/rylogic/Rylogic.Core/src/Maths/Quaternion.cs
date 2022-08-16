//***************************************************
// Quaternion
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
	/// <summary>Quaternion functions. Note: a quaternion is a v4</summary>
	[Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[DebuggerDisplay("{Description,nq}")]
	public struct quat
	{
		[FieldOffset( 0)] public float x;
		[FieldOffset( 4)] public float y;
		[FieldOffset( 8)] public float z;
		[FieldOffset(12)] public float w;
		[FieldOffset( 0)] public v4 xyzw; // (same name as the C++ version)

		public quat(float x) 
			:this()
		{
			this.x = x;
			this.y = x;
			this.z = x;
			this.w = x;
		}
		public quat(float x, float y, float z, float w)
			:this()
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.w = w;
		}
		public quat(v4 vec)
			:this()
		{
			this.xyzw = vec;
		}

		/// <summary>Create a quaternion from an axis and an angle</summary>
		public quat(v4 axis, float angle)
			:this()
		{
			var s = (float)Math.Sin(0.5 * angle);
			x = s * axis.x;
			y = s * axis.y;
			z = s * axis.z;
			w = (float)Math.Cos(0.5 * angle);
		}

		/// <summary>Create a quaternion from Euler angles. Order is: roll, pitch, yaw (to match DirectX)</summary>
		public quat(float pitch, float yaw, float roll)
			:this()
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
		public quat(m3x4 m)
			:this()
		{
			Debug.Assert(Math_.IsOrthonormal(m), "Only orientation matrices can be converted into quaternions");
			var trace = m.x.x + m.y.y + m.z.z;
			if (trace >= 0.0f)
			{
				var s = 0.5f / (float)Math.Sqrt(1.0f + trace);
				x = (m.y.z - m.z.y) * s;
				y = (m.z.x - m.x.z) * s;
				z = (m.x.y - m.y.x) * s;
				w = (0.25f / s);
			}
			else if (m.x.x > m.y.y && m.x.x > m.z.z)
			{
				var s = 0.5f / (float)Math.Sqrt(1.0f + m.x.x - m.y.y - m.z.z);
				x = (0.25f / s);
				y = (m.x.y + m.y.x) * s;
				z = (m.z.x + m.x.z) * s;
				w = (m.y.z - m.z.y) * s;
			}
			else if (m.y.y > m.z.z)
			{
				var s = 0.5f / (float)Math.Sqrt(1.0f - m.x.x + m.y.y - m.z.z);
				x = (m.x.y + m.y.x) * s;
				y = (0.25f / s);
				z = (m.y.z + m.z.y) * s;
				w = (m.z.x - m.x.z) * s;
			}
			else
			{
				var s = 0.5f / (float)Math.Sqrt(1.0f - m.x.x - m.y.y + m.z.z);
				x = (m.z.x + m.x.z) * s;
				y = (m.y.z + m.z.y) * s;
				z = (0.25f / s);
				w = (m.x.y - m.y.x) * s;
			}
		}
	
		/// <summary>Construct a quaternion representing a rotation from 'from' to 'to'</summary>
		public quat(v4 from, v4 to)
			:this()
		{
			var d = Math_.Dot(from.xyz, to.xyz); 
			var s = (float)Math.Sqrt(from.xyz.LengthSq * to.xyz.LengthSq) + d;
			var axis = Math_.Cross(from, to);

			// vectors are 180 degrees apart
			if (Math_.FEql(s, 0))
			{
				axis = Math_.Perpendicular(to);
				s = 0.0f;
			}

			xyzw = Math_.Normalise(new v4(axis.x, axis.y, axis.z, s));
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
			get => this[(int)i];
			set => this[(int)i] = value;
		}

		/// <summary>Length</summary>
		public float LengthSq => x * x + y * y + z * z + w * w;
		public float Length => (float)Math.Sqrt(LengthSq);

		/// <summary>Get the axis component of the quaternion (normalised)</summary>
		public v4 Axis => Math_.Normalise(xyzw.w0);

		// Return the angle of rotation about 'Axis()'
		public float Angle => (float)Math.Acos(CosAngle);

		// Return the cosine of the angle of rotation about 'Axis()'
		public float CosAngle
		{
			get
			{
				Debug.Assert(Math_.FEql(LengthSq, 1.0f), "quaternion isn't normalised");

				// Trig:
				//' cos^2(x) = 0.5 * (1 + cos(2x))
				//' w == cos(x/2)
				//' w^2 == cos^2(x/2) == 0.5 * (1 + cos(x))
				//' 2w^2 - 1 == cos(x)
				return Math_.Clamp(2f * Math_.Sqr(w) - 1f, -1f, +1f);
			}
		}

		// Return the sine of the angle of rotation about 'Axis()'
		public float SinAngle
		{
			get
			{
				Debug.Assert(Math_.FEql(LengthSq, 1.0f), "quaternion isn't normalised");

				// Trig:
				//' sin^2(x) + cos^2(x) == 1
				//' sin^2(x) == 1 - cos^2(x)
				//' sin(x) == sqrt(1 - cos^2(x))
				return (float)Math.Sqrt(1f - Math_.Sqr(CosAngle));
			}
		}

		/// <summary>ToString</summary>
		public override string ToString() => $"{x} {y} {z} {w}";
		public string ToString(string format) => $"{x.ToString(format)} {y.ToString(format)} {z.ToString(format)} {w.ToString(format)}";

		/// <summary>ToArray()</summary>
		public float[] ToArray()
		{
			return new float[4]{ x, y, z, w };
		}

		// Static types
		public static quat Zero { get; } = new quat(0, 0, 0, 0);
		public static quat Identity { get; } = new quat(0, 0, 0, 1);

		/// <summary>Operators</summary>
		public static quat operator + (quat q)
		{
			return q;
		}
		public static quat operator - (quat q)
		{
			return new quat(-q.x, -q.y, -q.z, -q.w); // Note: Not conjugate
		}
		public static quat operator ~ (quat q)
		{
			return new quat(-q.x, -q.y, -q.z, q.w);
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
		public static v4 operator * (quat lhs, v4 rhs)
		{
			// Quaternion rotate. Same semantics at matrix multiply
			return Math_.Rotate(lhs, rhs);
		}

		/// <summary>Component add</summary>
		public static quat CompAdd(quat lhs, quat rhs)
		{
			return new quat(lhs.xyzw + rhs.xyzw);
		}

		/// <summary>Component multiply</summary>
		public quat CompMul(quat lhs, float rhs)
		{
			return new quat(lhs.xyzw * rhs);
		}
		public quat CompMul(quat lhs, double rhs)
		{
			return CompMul(lhs, (float)rhs);
		}
		public quat CompMul(quat lhs, quat rhs)
		{
			return new quat(lhs.xyzw * rhs.xyzw);
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
			return new quat(v4.Random3N(0f, r), r.Float(0f, (float)Math_.Tau));
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
		public override bool Equals(object? o)
		{
			return o is quat q && q == this;
		}
		public override int GetHashCode()
		{
			return new { x, y, z, w }.GetHashCode();
		}
		#endregion

		/// <summary></summary>
		public string Description => $"{x}  {y}  {z}  {w}  //Axis({Axis}) Ang({Angle})";
	}

	public static partial class Math_
	{
		/// <summary>Approximate equal. Note: q == -q</summary>
		public static bool FEqlRelative(quat lhs, quat rhs, float tol)
		{
			return
				FEqlRelative(lhs.xyzw, +rhs.xyzw, tol) ||
				FEqlRelative(lhs.xyzw, -rhs.xyzw, tol);
		}
		public static bool FEql(quat lhs, quat rhs)
		{
			return FEqlRelative(lhs, rhs, TinyF);
		}

		/// <summary>Return true if all components of 'vec' are finite</summary>
		public static bool IsFinite(quat q)
		{
			return IsFinite(q.x) && IsFinite(q.y) && IsFinite(q.z) && IsFinite(q.w);
		}

		/// <summary>Return true if any components of 'vec' are NaN</summary>
		public static bool IsNaN(quat q)
		{
			return IsNaN(q.x) || IsNaN(q.y) || IsNaN(q.z) || IsNaN(q.w);
		}

		/// <summary>Normalise a quaternion to unit length</summary>
		public static quat Normalise(quat q)
		{
			return new quat(Normalise(q.xyzw));
		}
		public static quat Normalise(quat q, quat def)
		{
			return new quat(Normalise(q.xyzw, def.xyzw));
		}

		/// <summary>Return the cosine of *twice* the angle between two quaternions (i.e. the dot product)</summary>
		public static float CosAngle2(quat a, quat b)
		{
			// The relative orientation between 'a' and 'b' is given by z = 'a * conj(b)'
			// where operator * is a quaternion multiply. The 'w' component of a quaternion
			// multiply is given by: q.w = a.w*b.w - a.x*b.x - a.y*b.y - a.z*b.z;
			// which is the same as q.w = Dot4(a,b) since conjugate negates the x,y,z
			// components of 'b'. Remember: q.w = Cos(theta/2)
			return Dot(a.xyzw, b.xyzw);
		}

		/// <summary>Return the angle between two quaternions (in radians)</summary>
		public static float Angle(quat a, quat b)
		{
			// q.w = Cos(theta/2)
			return 0.5f * (float)Math.Acos(CosAngle2(a, b));
		}

		/// <summary>Scale the rotation by 'x'. i.e. 'frac' == 2 => double the rotation, 'frac' == 0.5 => halve the rotation</summary>
		public static quat Scale(quat q, float frac)
		{
			Debug.Assert(FEql(q.LengthSq, 1.0f), "quaternion isn't normalised");

			// Trig:
			//' sin^2(x) + cos^2(x) == 1
			//' s == sqrt(1 - w^2) == sqrt(1 - cos^2(x/2))
			//' s^2 == 1 - cos^2(x/2) == sin^2(x/2)
			//' s == sin(x/2)
			var w = Clamp(q.w, -1f, 1f);           // = cos(x/2)
			var s = (float)Math.Sqrt(1f - Sqr(w)); // = sin(x/2)
			var a = frac * (float)Math.Acos(w);    // = scaled half angle
			var sin_ha = (float)Math.Sin(a);
			var cos_ha = (float)Math.Cos(a);
			return new quat(
				q.x * sin_ha / s,
				q.y * sin_ha / s,
				q.z * sin_ha / s,
				cos_ha);
		}

		/// <summary>Return the axis and angle from a quaternion</summary>
		public static void AxisAngle(quat quat, out v4 axis, out float angle)
		{
			angle = (float)(2.0 * Math.Acos(quat.w));
			var s = (float)Math.Sqrt(1.0f - quat.w * quat.w);
			axis = FEql(s, 0.0f)
				? Normalise(new v4(quat.x, quat.y, quat.z, 0.0f))
				: new v4(quat.x / s, quat.y / s, quat.z / s, 0.0f);
		}

		/// <summary>Return possible Euler angles for the quaternion 'q'</summary>
		public static v4 EulerAngles(quat q)
		{
			// From Wikipedia
			double q0 = q.w, q1 = q.x, q2 = q.y, q3 = q.z;
			return new v4(
				(float)Math.Atan2(2.0 * (q0 * q1 + q2 * q3), 1.0 - 2.0 * (q1 * q1 + q2 * q2)),
				(float)Math.Asin(2.0 * (q0 * q2 - q3 * q1)),
				(float)Math.Atan2(2.0 * (q0 * q3 + q1 * q2), 1.0 - 2.0 * (q2 * q2 + q3 * q3)),
				0f);
		}

		/// <summary>Spherically interpolate between quaternions</summary>
		public static quat Slerp(quat a, quat b, double frac)
		{
			// Flip 'b' so that both quaternions are in the same hemisphere (since: q == -q)
			var cos_angle2 = CosAngle2(a, b);
			var b_ = cos_angle2 >= 0 ? b : -b;
			cos_angle2 = Math.Abs(cos_angle2);

			if (cos_angle2 < 0.95f)
			{
				var angle = 0.5 * Math.Acos(cos_angle2);
				var scale0 = Math.Sin((1 - frac) * angle);
				var scale1 = Math.Sin((frac) * angle);
				var sin_angle = Math.Sin(angle);
				return new quat((a.xyzw * (float)scale0 + b_.xyzw * (float)scale1) / (float)sin_angle);
			}
			// "a" and "b" quaternions are very close, use linear interpolation
			else
			{
				return Normalise(new quat(Lerp(a.xyzw, b_.xyzw, frac)));
			}
		}

		/// <summary>Rotate a vector by a quaternion. This is an optimised version of: 'r = q*v*conj(q) for when v.w == 0'</summary>
		public static v4 Rotate(quat lhs, v4 rhs)
		{
			float xx = lhs.x * lhs.x, xy = lhs.x * lhs.y, xz = lhs.x * lhs.z, xw = lhs.x * lhs.w;
			float yy = lhs.y * lhs.y, yz = lhs.y * lhs.z, yw = lhs.y * lhs.w;
			float zz = lhs.z * lhs.z, zw = lhs.z * lhs.w;
			float ww = lhs.w * lhs.w;
			return new v4(
				  ww * rhs.x + 2 * yw * rhs.z - 2 * zw * rhs.y + xx * rhs.x + 2 * xy * rhs.y + 2 * xz * rhs.z - zz * rhs.x - yy * rhs.x,
				2 * xy * rhs.x + yy * rhs.y + 2 * yz * rhs.z + 2 * zw * rhs.x - zz * rhs.y + ww * rhs.y - 2 * xw * rhs.z - xx * rhs.y,
				2 * xz * rhs.x + 2 * yz * rhs.y + zz * rhs.z - 2 * yw * rhs.x - yy * rhs.z + 2 * xw * rhs.y - xx * rhs.z + ww * rhs.z,
				rhs.w);
		}

		/// <summary>
		/// Return the average of a collection of rotations.
		/// Note: this only really works if all the quaternions are relatively close together.
		/// For two quaternions, prefer 'Slerp'</summary>
		public static quat Average(IEnumerable<quat> rotations)
		{
			// Nicked from Unity3D
			// This method is based on a simplified procedure described in this document:
			// http://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/20070017872_2007014421.pdf

			// Ensure the quaternions are in the same hemisphere (since q == -q)
			var first = rotations.First();
			var avr = Average(rotations.Select(q => Dot(q.xyzw, first.xyzw) >= 0 ? q.xyzw : -q.xyzw));
			return Normalise(new quat(avr));
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Extn;
	using Maths;

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
				var p = Math_.DegreesToRadians(p_);
				var y = Math_.DegreesToRadians(y_);
				var r = Math_.DegreesToRadians(r_);

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
		[Test] public void Average()
		{
			var rng = new Random(1);
			var ideal_mean = new quat(Math_.Normalise(new v4(1,1,1,0)), 0.5f);
			var actual_mean = Math_.Average(int_.Range(0, 1000).Select(i =>
			{
				var axis = Math_.Normalise(ideal_mean.Axis + v4.Random3(0.02f, 0f, rng));
				var angle = rng.FloatC(ideal_mean.Angle, 0.02f);
				var q = new quat(axis, angle);
				return rng.Bool() ? q : -q;
			}));
			Assert.True(Math_.FEqlRelative(ideal_mean, actual_mean, 0.01f));
		}
	}
}
#endif