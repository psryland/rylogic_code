 
//***************************************************
// Vector
//  Copyright (c) Rylogic Ltd 2008
//***************************************************
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rylogic.Maths
{
	#region Vec2f

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	[DebuggerDisplay("{x} {y} // Len={Length})")]
	public struct Vec2f
	{
		public float x, y;

		// Constructors
		public Vec2f(float x_, float y_)
		{
			x = x_;
			y = y_;
		}

		// Properties
		public float LengthSq
		{
			get { return x*x + y*y; }
		}
		public float Length
		{
			get { return (float)Math.Sqrt(LengthSq); }
		}

		// Functions
		public static float Dot(Vec2f lhs, Vec2f rhs)
		{
			return lhs.x*rhs.x + lhs.y*rhs.y;
		}
	}

	#endregion

	#region Vec2d

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	[DebuggerDisplay("{x} {y} // Len={Length})")]
	public struct Vec2d
	{
		public double x, y;

		// Constructors
		public Vec2d(double x_, double y_)
		{
			x = x_;
			y = y_;
		}

		// Properties
		public double LengthSq
		{
			get { return x*x + y*y; }
		}
		public double Length
		{
			get { return (double)Math.Sqrt(LengthSq); }
		}

		// Functions
		public static double Dot(Vec2d lhs, Vec2d rhs)
		{
			return lhs.x*rhs.x + lhs.y*rhs.y;
		}
	}

	#endregion

	#region Vec4f

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	[DebuggerDisplay("{x} {y} {z} {w} // Len={Length})")]
	public struct Vec4f
	{
		public float x, y, z, w;

		// Constructors
		public Vec4f(float x_, float y_, float z_, float w_)
		{
			x = x_;
			y = y_;
			z = z_;
			w = w_;
		}

		// Properties
		public float LengthSq
		{
			get { return x*x + y*y + z*z + w*w; }
		}
		public float Length
		{
			get { return (float)Math.Sqrt(LengthSq); }
		}

		// Functions
		public static float Dot(Vec4f lhs, Vec4f rhs)
		{
			return lhs.x*rhs.x + lhs.y*rhs.y + lhs.z*rhs.z + lhs.w*rhs.w;
		}
	}

	#endregion

	#region Vec4d

	[Serializable]
	[StructLayout(LayoutKind.Sequential)]
	[DebuggerDisplay("{x} {y} {z} {w} // Len={Length})")]
	public struct Vec4d
	{
		public double x, y, z, w;

		// Constructors
		public Vec4d(double x_, double y_, double z_, double w_)
		{
			x = x_;
			y = y_;
			z = z_;
			w = w_;
		}

		// Properties
		public double LengthSq
		{
			get { return x*x + y*y + z*z + w*w; }
		}
		public double Length
		{
			get { return (double)Math.Sqrt(LengthSq); }
		}

		// Functions
		public static double Dot(Vec4d lhs, Vec4d rhs)
		{
			return lhs.x*rhs.x + lhs.y*rhs.y + lhs.z*rhs.z + lhs.w*rhs.w;
		}
	}

	#endregion

}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Maths;

	[TestFixture] public class UnitTestVector_Vec2f
	{
		[Test] public void Basic()
		{
			var A = new int[] {1, 2, 3, 4};
			var a = new Vec2f(A[0],A[1]);
			Assert.True(a.x == A[0]);
			Assert.True(a.y == A[1]);
		}
	}
	[TestFixture] public class UnitTestVector_Vec2d
	{
		[Test] public void Basic()
		{
			var A = new int[] {1, 2, 3, 4};
			var a = new Vec2d(A[0],A[1]);
			Assert.True(a.x == A[0]);
			Assert.True(a.y == A[1]);
		}
	}
	[TestFixture] public class UnitTestVector_Vec4f
	{
		[Test] public void Basic()
		{
			var A = new int[] {1, 2, 3, 4};
			var a = new Vec4f(A[0],A[1],A[2],A[3]);
			Assert.True(a.x == A[0]);
			Assert.True(a.y == A[1]);
			Assert.True(a.z == A[2]);
			Assert.True(a.w == A[3]);
		}
	}
	[TestFixture] public class UnitTestVector_Vec4d
	{
		[Test] public void Basic()
		{
			var A = new int[] {1, 2, 3, 4};
			var a = new Vec4d(A[0],A[1],A[2],A[3]);
			Assert.True(a.x == A[0]);
			Assert.True(a.y == A[1]);
			Assert.True(a.z == A[2]);
			Assert.True(a.w == A[3]);
		}
	}
}
#endif

