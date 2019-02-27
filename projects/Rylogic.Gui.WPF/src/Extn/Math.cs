using System.Windows;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF
{
	public static class Point_
	{
		/// <summary>Convert to v2</summary>
		public static v2 ToV2(this Point p)
		{
			return new v2((float)p.X, (float)p.Y);
		}
	}

	public static class Vector_
	{
		/// <summary>Convert to v2</summary>
		public static v2 ToV2(this Vector v)
		{
			return new v2((float)v.X, (float)v.Y);
		}

		/// <summary>The squared length of the vector</summary>
		public static double LengthSq(this Vector v)
		{
			return Math_.Sqr(v.X) + Math_.Sqr(v.Y);
		}
	}
}
