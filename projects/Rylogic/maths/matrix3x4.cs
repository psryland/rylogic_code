//***************************************************
// Matrix4x4
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	public struct m3x4
	{
		public v4 x;
		public v4 y;
		public v4 z;

		public override string ToString() { return x + " \n" + y + " \n" + z + " \n"; }

		public m3x4(v4 x_, v4 y_, v4 z_) :this()                               { set(x_, y_, z_); }
		public m3x4(v4 axis_norm, v4 axis_sine_angle, float cos_angle) :this() { set(axis_norm, axis_sine_angle, cos_angle); }
		public m3x4(v4 axis_norm, float angle) :this()                         { set(axis_norm, angle); }
		public m3x4(v4 from, v4 to) :this()                                    { set(from, to); }

		public v4 this[int i]
		{
			get { switch(i){case 0:return x;case 1:return y;case 2:return z;default: throw new ArgumentException("index out of range", "i");} }
			set { switch(i){case 0:x=value;break;case 1:y=value;break;case 2:z=value;break;default: throw new ArgumentException("index out of range", "i");} }
		}

		public void set(v4 x_, v4 y_, v4 z_)
		{
			x = x_;
			y = y_;
			z = z_;
		}
		public void set(v4 axis_norm, v4 axis_sine_angle, float cos_angle)
		{
			Debug.Assert(Maths.FEql(axis_norm.Length3Sq, 1f, 2*Maths.TinyF), "'axis_norm' should be normalised");

			v4 trace_vec = axis_norm * (1.0f - cos_angle);

			x.x = trace_vec.x * axis_norm.x + cos_angle;
			y.y = trace_vec.y * axis_norm.y + cos_angle;
			z.z = trace_vec.z * axis_norm.z + cos_angle;

			trace_vec.x *= axis_norm.y;
			trace_vec.z *= axis_norm.x;
			trace_vec.y *= axis_norm.z;

			x.y = trace_vec.x + axis_sine_angle.z;
			x.z = trace_vec.z - axis_sine_angle.y;
			x.w = 0.0f;
			y.x = trace_vec.x - axis_sine_angle.z;
			y.z = trace_vec.y + axis_sine_angle.x;
			y.w = 0.0f;
			z.x = trace_vec.z + axis_sine_angle.y;
			z.y = trace_vec.y - axis_sine_angle.x;
			z.w = 0.0f;
		}
		public void set(v4 axis_norm, float angle)
		{
			Debug.Assert(Maths.FEql(axis_norm.Length3Sq, 1f, 2*Maths.TinyF), "'axis_norm' should be normalised");
			set(axis_norm, axis_norm * (float)Math.Sin(angle), (float)Math.Cos(angle));
		}
		public void set(v4 from, v4 to)
		{
			Debug.Assert(Maths.FEql(from.Length3Sq, 1f, 2*Maths.TinyF) && Maths.FEql(to.Length3Sq, 1f, 2*Maths.TinyF), "'from' and 'to' should be normalised");

			float cos_angle = v4.Dot3(from, to); // Cos angle
			if      (cos_angle >= 1f - Maths.TinyF) { x =  v4.XAxis; y =  v4.YAxis; z =  v4.ZAxis; }
			else if (cos_angle <= Maths.TinyF - 1f) { x = -v4.XAxis; y = -v4.YAxis; z = -v4.ZAxis; }
			else
			{
				v4 axis_sine_angle = v4.Cross3(from, to); // Axis multiplied by sine of the angle
				v4 axis_norm       = v4.Normalise3(axis_sine_angle);
				set(axis_norm, axis_sine_angle, cos_angle);
			}
		}

		// Static m3x4 types
		private readonly static m3x4 m_zero = new m3x4(v4.Zero, v4.Zero, v4.Zero);
		private readonly static m3x4 m_identity = new m3x4(v4.XAxis, v4.YAxis, v4.ZAxis);
		public static m3x4 Zero     { get { return m_zero; } }
		public static m3x4 Identity { get { return m_identity; } }

		// Functions
		public static m3x4 operator + (m3x4 lhs, m3x4 rhs)  { return new m3x4(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z); }
		public static m3x4 operator - (m3x4 lhs, m3x4 rhs)  { return new m3x4(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z); }
		public static m3x4 operator * (m3x4 lhs, float rhs) { return new m3x4(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs); }
		public static m3x4 operator * (float lhs, m3x4 rhs) { return new m3x4(lhs * rhs.x, lhs * rhs.y, lhs * rhs.z); }
		public static m3x4 operator / (m3x4 lhs, float rhs) { return new m3x4(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs); }
		public static bool operator ==(m3x4 lhs, m3x4 rhs)  { return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z; }
		public static bool operator !=(m3x4 lhs, m3x4 rhs)  { return !(lhs == rhs); }
		public override bool Equals(object o)               { return o is m3x4 && (m3x4)o == this; }
		public override int GetHashCode()                   { unchecked { return x.GetHashCode() + y.GetHashCode() + z.GetHashCode(); } }

		public static void Transpose3x3(ref m3x4 m)
		{
			Maths.Swap(ref m.x.y, ref m.y.x);
			Maths.Swap(ref m.x.z, ref m.z.x);
			Maths.Swap(ref m.y.z, ref m.z.y);
		}
		public static m3x4 Transpose3x3(m3x4 m)
		{
			Transpose3x3(ref m);
			return m;
		}

		public static void InverseFast(ref m3x4 m)
		{
			Debug.Assert(IsOrthonormal(m), "Matrix is not orthonormal");
			Transpose3x3(ref m);
		}
		public static m3x4 InverseFast(m3x4 m)
		{
			InverseFast(ref m);
			return m;
		}

		public static void Orthonormalise(ref m3x4 m)
		{
			v4.Normalise3(ref m.x);
			m.y = v4.Normalise3(v4.Cross3(m.z, m.x));
			m.z = v4.Cross3(m.x, m.y);
		}
		public static m3x4 Orthonormalise(m3x4 m)
		{
			Orthonormalise(ref m);
			return m;
		}

		public static bool IsOrthonormal(m3x4 m)
		{
			return
				Maths.FEql(m.x.Length3Sq, 1f, 2*Maths.TinyF) &&
				Maths.FEql(m.y.Length3Sq, 1f, 2*Maths.TinyF) &&
				Maths.FEql(m.z.Length3Sq, 1f, 2*Maths.TinyF) &&
				v4.FEqlZero3(v4.Cross3(m.x, m.y) - m.z);
		}

		public static v4 operator * (m3x4 lhs, v4 rhs)
		{
			Transpose3x3(ref lhs);
			return new v4(
				v4.Dot4(lhs.x, rhs),
				v4.Dot4(lhs.y, rhs),
				v4.Dot4(lhs.z, rhs),
				rhs.w);
		}

		public static m3x4 operator * (m3x4 lhs, m3x4 rhs)
		{
			Transpose3x3(ref lhs);
			return new m3x4(
				new v4(v4.Dot4(lhs.x, rhs.x), v4.Dot4(lhs.y, rhs.x), v4.Dot4(lhs.z, rhs.x), 0f),
				new v4(v4.Dot4(lhs.x, rhs.y), v4.Dot4(lhs.y, rhs.y), v4.Dot4(lhs.z, rhs.y), 0f),
				new v4(v4.Dot4(lhs.x, rhs.z), v4.Dot4(lhs.y, rhs.z), v4.Dot4(lhs.z, rhs.z), 0f));
		}

		// Permute the rotation vectors in a matrix by 'n'
		public static m3x4 PermuteRotation(m3x4 mat, int n)
		{
			switch (n%3)
			{
			default:return mat;
			case 1: return new m3x4(mat.y, mat.z, mat.x);
			case 2: return new m3x4(mat.z, mat.x, mat.y);
			}
		}

		// Make an orientation matrix from a direction. Note the rotation around the direction
		// vector is not defined. 'axis' is the axis that 'direction' will become
		public static m3x4 OriFromDir(v4 direction, AxisId axis, v4 preferred_up)
		{
			// Get the preferred up direction (handling parallel cases)
			preferred_up = v4.Parallel(preferred_up, direction) ? v4.Perpendicular(direction) : preferred_up;

			// Create an orientation matrix where +Z points along 'direction'
			m3x4 ans = Identity;
			ans.z = v4.Normalise3(direction);
			ans.x = v4.Normalise3(v4.Cross3(preferred_up, ans.z));
			ans.y = v4.Cross3(ans.z, ans.x);

			// Permute the column vectors so +Z becomes 'axis'
			return Maths.SignF(axis) * PermuteRotation(ans, Math.Abs(axis) - 1);
		}
		public static m3x4 OriFromDir(v4 direction, AxisId axis)
		{
			return OriFromDir(direction, axis, v4.Perpendicular(direction));
		}

		// Create a rotation from an axis and angle
		public static m3x4 Rotation(v4 axis_norm, v4 axis_sine_angle, float cos_angle)
		{
			return new m3x4(axis_norm, axis_sine_angle, cos_angle);
		}

		// Create from an axis and angle. 'axis' should be normalised
		public static m3x4 Rotation(v4 axis_norm, float angle)
		{
			return new m3x4(axis_norm, angle);
		}

		// Create a rotation from 'from' to 'to'
		public static m3x4 Rotation(v4 from, v4 to)
		{
			return new m3x4(from, to);
		}
	}
}