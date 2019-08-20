using System;
using System.Windows;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public static class Point_
	{
		/// <summary>(0,0)</summary>
		public static Point Zero => new Point();

		/// <summary>Infinite vector</summary>
		public static Point Infinity => new Point(double.PositiveInfinity, double.PositiveInfinity);

		/// <summary>Convert to v2</summary>
		public static v2 ToV2(this Point p)
		{
			return new v2((float)p.X, (float)p.Y);
		}
	}

	public static class Vector_
	{
		/// <summary>Infinite vector</summary>
		public static Vector Infinity => new Vector(double.PositiveInfinity, double.PositiveInfinity);

		/// <summary>Convert to v2</summary>
		public static v2 ToV2(this Vector v)
		{
			return new v2((float)v.X, (float)v.Y);
		}
	}
}
