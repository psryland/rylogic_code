//***************************************************
// Drawing Extensions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;

namespace pr.extn
{
	public static class PointFExtensions
	{
		/// <summary>Returns the distance from the origin to this point (squared)</summary>
		public static double LengthSq(this PointF pt)
		{
			return pt.X*pt.X + pt.Y*pt.Y;
		}

		/// <summary>Returns the distance from the origin to this point</summary>
		public static double Length(this PointF pt)
		{
			return Math.Sqrt(pt.LengthSq());
		}

		/// <summary>Returns a normalised version of this point</summary>
		public static PointF Normalised(this PointF pt)
		{
			double len = pt.Length();
			if (Math.Abs(len - 0.0) < double.Epsilon) throw new DivideByZeroException("Cannot normalise a zero vector");
			return new PointF((float)(pt.X / len), (float)(pt.Y / len));
		}
	}
}
