//***************************************************
// Matrix
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;

namespace pr.maths
{
	public struct m2x2
	{
		public v2 x;
		public v2 y;

		public override string ToString() { return x + " \n" + y + " \n"; }

		public m2x2(v2 x_, v2 y_) :this()                               { set(x_, y_); }
		//public m2x2(v2 axis_norm, v4 axis_sine_angle, float cos_angle) :this() { set(axis_norm, axis_sine_angle, cos_angle); }
		//public m2x2(v2 axis_norm, float angle) :this()                         { set(axis_norm, angle); }
		//public m2x2(v2 from, v4 to) :this()                                    { set(from, to); }

		public v2 this[int i]
		{
			get { switch(i){case 0:return x;case 1:return y;default: throw new ArgumentException("index out of range", "i");} }
			set { switch(i){case 0:x=value;break;case 1:y=value;break;default: throw new ArgumentException("index out of range", "i");} }
		}

		public void set(v2 x_, v2 y_)
		{
			x = x_;
			y = y_;
		}
		//public void set(v4 axis_norm, v4 axis_sine_angle, float cos_angle)
		//{
		//	Debug.Assert(Maths.FEql(axis_norm.Length3Sq, 1f, 2*Maths.TinyF), "'axis_norm' should be normalised");

		//	v4 trace_vec = axis_norm * (1.0f - cos_angle);

		//	x.x = trace_vec.x * axis_norm.x + cos_angle;
		//	y.y = trace_vec.y * axis_norm.y + cos_angle;
		//	z.z = trace_vec.z * axis_norm.z + cos_angle;

		//	trace_vec.x *= axis_norm.y;
		//	trace_vec.z *= axis_norm.x;
		//	trace_vec.y *= axis_norm.z;

		//	x.y = trace_vec.x + axis_sine_angle.z;
		//	x.z = trace_vec.z - axis_sine_angle.y;
		//	x.w = 0.0f;
		//	y.x = trace_vec.x - axis_sine_angle.z;
		//	y.z = trace_vec.y + axis_sine_angle.x;
		//	y.w = 0.0f;
		//	z.x = trace_vec.z + axis_sine_angle.y;
		//	z.y = trace_vec.y - axis_sine_angle.x;
		//	z.w = 0.0f;
		//}
		//public void set(v4 axis_norm, float angle)
		//{
		//	Debug.Assert(Maths.FEql(axis_norm.Length3Sq, 1f, 2*Maths.TinyF), "'axis_norm' should be normalised");
		//	set(axis_norm, axis_norm * (float)Math.Sin(angle), (float)Math.Cos(angle));
		//}
		//public void set(v4 from, v4 to)
		//{
		//	Debug.Assert(Maths.FEql(from.Length3Sq, 1f, 2*Maths.TinyF) && Maths.FEql(to.Length3Sq, 1f, 2*Maths.TinyF), "'from' and 'to' should be normalised");

		//	float cos_angle = v4.Dot3(from, to); // Cos angle
		//	if      (cos_angle >= 1f - Maths.TinyF) { x =  v4.XAxis; y =  v4.YAxis; z =  v4.ZAxis; }
		//	else if (cos_angle <= Maths.TinyF - 1f) { x = -v4.XAxis; y = -v4.YAxis; z = -v4.ZAxis; }
		//	else
		//	{
		//		v4 axis_sine_angle = v4.Cross3(from, to); // Axis multiplied by sine of the angle
		//		v4 axis_norm       = v4.Normalise3(axis_sine_angle);
		//		set(axis_norm, axis_sine_angle, cos_angle);
		//	}
		//}

		// Static m2x2 types
		private readonly static m2x2 m_zero     = new m2x2(v2.Zero , v2.Zero );
		private readonly static m2x2 m_identity = new m2x2(v2.XAxis, v2.YAxis);
		public static m2x2 Zero     { get { return m_zero; } }
		public static m2x2 Identity { get { return m_identity; } }

		// Functions
		public static m2x2 operator * (m2x2 lhs, float rhs) { return new m2x2(lhs.x * rhs   , lhs.y * rhs   ); }
		public static m2x2 operator + (m2x2 lhs, m2x2 rhs)  { return new m2x2(lhs.x + rhs.x , lhs.y + rhs.y ); }
		public static m2x2 operator - (m2x2 lhs, m2x2 rhs)  { return new m2x2(lhs.x - rhs.x , lhs.y - rhs.y ); }
		public static m2x2 operator * (float lhs, m2x2 rhs) { return new m2x2(lhs * rhs.x   , lhs * rhs.y   ); }
		public static m2x2 operator / (m2x2 lhs, float rhs) { return new m2x2(lhs.x / rhs   , lhs.y / rhs   ); }
		public static bool operator ==(m2x2 lhs, m2x2 rhs)  { return lhs.x == rhs.x && lhs.y == rhs.y; }
		public static bool operator !=(m2x2 lhs, m2x2 rhs)  { return !(lhs == rhs); }
		public override bool Equals(object o)               { return o is m2x2 && (m2x2)o == this; }
		public override int GetHashCode()                   { unchecked { return x.GetHashCode() + y.GetHashCode(); } }

		public static void Transpose2x2(ref m2x2 m)
		{
			Maths.Swap(ref m.x.y, ref m.y.x);
		}
		public static m2x2 Transpose2x2(m2x2 m)
		{
			Transpose2x2(ref m);
			return m;
		}

		public static void InverseFast(ref m2x2 m)
		{
			Debug.Assert(IsOrthonormal(m), "Matrix is not orthonormal");
			Transpose2x2(ref m);
		}
		public static m2x2 InverseFast(m2x2 m)
		{
			InverseFast(ref m);
			return m;
		}

		public static void Orthonormalise(ref m2x2 m)
		{
			m.x = v2.Normalise2(m.x);
			m.y = v2.Normalise2(m.y - v2.Dot2(m.x,m.y) * m.x);
		}
		public static m2x2 Orthonormalise(m2x2 m)
		{
			Orthonormalise(ref m);
			return m;
		}

		public static bool IsOrthonormal(m2x2 m)
		{
			return
				Maths.FEql(m.x.Length2Sq, 1f, 2*Maths.TinyF) &&
				Maths.FEql(m.y.Length2Sq, 1f, 2*Maths.TinyF) &&
				Maths.FEql(v2.Dot2(m.x, m.y), 0f, Maths.TinyF);
		}

		public static v2 operator * (m2x2 lhs, v2 rhs)
		{
			v2 ans;
			Transpose2x2(ref lhs);
			ans.x = v2.Dot2(lhs.x, rhs);
			ans.y = v2.Dot2(lhs.y, rhs);
			return ans;
		}

		public static m2x2 operator * (m2x2 lhs, m2x2 rhs)
		{
			m2x2 ans;
			Transpose2x2(ref lhs);
			ans.x.x = v2.Dot2(lhs.x, rhs.x);
			ans.x.y = v2.Dot2(lhs.y, rhs.x);
			ans.y.x = v2.Dot2(lhs.x, rhs.y);
			ans.y.y = v2.Dot2(lhs.y, rhs.y);
			return ans;
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
			ans.x = v2.Normalise2(direction);
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
	}
}