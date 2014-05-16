//***************************************************
// Quaternion
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	/// <summary>Quaternion functions. Note: a quaternion is a v4</summary>
	[Serializable]
	public static class quat
	{
		// Static quat types
		public static v4 Identity { get { return v4.WAxis; } }
		
		/// <summary>Create a quaternion from an axis and an angle</summary>
		public static v4 Make(v4 axis, float angle)
		{
			float s = (float)Math.Sin(0.5 * angle);
			float w = (float)Math.Cos(0.5 * angle);
			return new v4(s*axis.x, s*axis.y, s*axis.z, w);
		}
		
		/// <summary>Create a quaternion from Euler angles</summary>
		public static v4 Make(float pitch, float yaw, float roll)
		{
			float cos_r = (float)Math.Cos(roll  * 0.5), sin_r = (float)Math.Sin(roll  * 0.5);
			float cos_p = (float)Math.Cos(pitch * 0.5), sin_p = (float)Math.Sin(pitch * 0.5);
			float cos_y = (float)Math.Cos(yaw   * 0.5), sin_y = (float)Math.Sin(yaw   * 0.5);
			return new v4(
				cos_r * sin_p * cos_y + sin_r * cos_p * sin_y,
				cos_r * cos_p * sin_y - sin_r * sin_p * cos_y,
				sin_r * cos_p * cos_y - cos_r * sin_p * sin_y,
				cos_r * cos_p * cos_y + sin_r * sin_p * sin_y);
		}

		/// <summary>Create a quaternion from a rotation matrix</summary>
		public static v4 Make(m3x4 m)
		{
			float trace = m.x.x + m.y.y + m.z.z;
			if (trace >= 0.0f)
			{
				float r = (float)Math.Sqrt(1.0f + trace);
				float s = 0.5f / r;
				return new v4((m.z.y - m.y.z) * s, (m.x.z - m.z.x) * s, (m.y.x - m.x.y) * s, 0.5f * r);
			}
			else
			{
				int i = 0;
				if (m.y.y > m.x.x  ) ++i;
				if (m.z.z > m[i][i]) ++i;
				int j = (i + 1) % 3;
				int k = (i + 2) % 3;
				float r = (float)Math.Sqrt(m[i][i] - m[j][j] - m[k][k] + 1.0f);
				float s = 0.5f / r;
				v4 q = v4.Zero;
				q[i] = 0.5f * r;
				q[j] = (m[i][j] + m[j][i]) * s;
				q[k] = (m[k][i] + m[i][k]) * s;
				q.w  = (m[k][j] - m[j][k]) * s;
				return q;
			}
		}
	
		/// <summary>Construct a quaternion representing a rotation from 'from' to 'to'</summary>
		public static v4 Make(v4 from, v4 to)
		{
			float d = v4.Dot3(from, to); 
			v4 axis = v4.Cross3(from, to);
			float s = (float)Math.Sqrt(from.Length3Sq * to.Length3Sq) + d;
			if (Maths.FEql(s, 0.0f)) { axis = v4.Perpendicular(to); s = 0.0f; }	// vectors are 180 degrees apart
			return v4.Normalise4(new v4(axis.x, axis.y, axis.z, s));
		}

		/// <summary>Quaternion multiply. Same sematics at matrix multiply</summary>
		public static v4 Mul(v4 lhs, v4 rhs)
		{
			return new v4(
				lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y,
				lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x,
				lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w,
				lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z);
		}

		/// <summary>Return the quaternion conjugate of 'q'</summary>
		public static v4 Conjugate(v4 q)
		{
			q.x = -q.x;
			q.y = -q.y;
			q.z = -q.z;
			return q;
		}

		/// <summary>Return the axis and angle from a quaternion</summary>
		public static void AxisAngle(v4 quat, out v4 axis, out float angle)
		{
			angle = (float)(2.0 * Math.Acos(quat.w));
			float s = (float)Math.Sqrt(1.0f - quat.w * quat.w);
			if (Maths.FEql(s, 0.0f)) axis = v4.Normalise3(new v4(quat.x, quat.y, quat.z, 0.0f));
			else                     axis = new v4(quat.x/s, quat.y/s, quat.z/s, 0.0f);
		}

		/// <summary>Construct a random quaternion rotation</summary>
		public static v4 Random(Rand r, v4 axis, float min_angle, float max_angle)
		{
			return Make(axis, r.Float(min_angle, max_angle));
		}
		
		/// <summary>Construct a random quaternion rotation</summary>
		public static v4 Random(float min_angle, float max_angle, Rand r)
		{
			r = r ?? new Rand();
			return Make(v4.Random3N(0.0f, r), r.Float(min_angle, max_angle));
		}
		
		/// <summary>Construct a random quaternion rotation</summary>
		public static v4 Random(Rand r)
		{
			r = r ?? new Rand();
			return Make(v4.Random3N(0.0f, r), r.Float(0.0f, Maths.Tau));
		}

		/// <summary>Spherically interpolate between quaternions</summary>
		public static v4 Slerp(v4 src, v4 dst, float frac)
		{
			if (frac <= 0.0f) return src;
			if (frac >= 1.0f) return dst;
			
			float cos_angle = src.x*dst.x + src.y*dst.y + src.z*dst.z + src.w*dst.w;
			float sign      = (cos_angle >= 0) ? 1 : -1;
			v4 abs_dst      = sign * dst;
			cos_angle      *= sign;

			// Calculate coefficients
			if (cos_angle < 0.95f)
			{
				// standard case (slerp)
				float angle     = (float)Math.Acos(cos_angle);
				float sin_angle = (float)Math.Sin (angle);
				float scale0    = (float)Math.Sin((1.0f - frac) * angle);
				float scale1    = (float)Math.Sin((       frac) * angle);
				return (scale0*src + scale1*abs_dst) * (1.0f / sin_angle);
			}
			// "src" and "dst" quaternions are very close 
			return v4.Normalise4(v4.Lerp(src, abs_dst, frac));
		}
		
		/// <summary>Rotate 'rotatee' by 'rotator'. If 'rotatee' has w = 0,
		/// then this is equivalent to rotating a direction vector</summary>
		public static v4 Rotate(v4 rotator, v4 rotatee)
		{
			Debug.Assert(Maths.FEql(rotator.Length4Sq, 1.0f), "Non-unit quaternion used for rotation");
			return Mul(rotator, Mul(rotatee, Conjugate(rotator)));
		}
	}
}