using System;
using System.Windows;

namespace Rylogic.Extn.Windows
{
	public static class Size_
	{
		/// <summary>Zero size</summary>
		public static Size Zero => new Size(0, 0);

		/// <summary>Infinite size</summary>
		public static Size Infinity => new Size(double.PositiveInfinity, double.PositiveInfinity);
	}

	public static class Rect_
	{
		/// <summary>Zero size</summary>
		public static Rect Zero => new Rect(0, 0, 0, 0);

		/// <summary>Construct from LTRB</summary>
		public static Rect FromLTRB(double left, double top, double right, double bottom)
		{
			return new Rect(left, top, right - left, bottom - top);
		}

		/// <summary>Return the centre position of the rectangle</summary>
		public static Point Centre(this Rect rect)
		{
			return new Point(rect.Left + rect.Width * 0.5, rect.Top + rect.Height * 0.5);
		}

		/// <summary>Left/Centre point</summary>
		public static Point LeftCentre(this Rect rect)
		{
			return new Point(rect.Left, (rect.Top + rect.Bottom) * 0.5);
		}

		/// <summary>Right/Centre point</summary>
		public static Point RightCentre(this Rect rect)
		{
			return new Point(rect.Right, (rect.Top + rect.Bottom) * 0.5);
		}

		/// <summary>Top/Centre point</summary>
		public static Point TopCentre(this Rect rect)
		{
			return new Point((rect.Left + rect.Right) * 0.5, rect.Top);
		}

		/// <summary>Bottom/Centre point</summary>
		public static Point BottomCentre(this Rect rect)
		{
			return new Point((rect.Left + rect.Right) * 0.5, rect.Bottom);
		}

		/// <summary>Reduces the size of this rectangle by excluding the area 'x'. The result must be a rectangle or an exception is thrown</summary>
		public static Rect Subtract(this Rect r, Rect x)
		{
			// If 'x' has no area, then subtraction is identity
			if (x.Width * x.Height <= 0)
				return r;

			// If the rectangles do not overlap. Right/Bottom is not considered 'in' the rectangle
			if (r.Left >= x.Right || r.Right <= x.Left || r.Top >= x.Bottom || r.Bottom <= x.Top)
				return r;

			// If 'x' completely covers 'r' then the result is empty
			if (x.Left <= r.Left && x.Right >= r.Right && x.Top <= r.Top && x.Bottom >= r.Bottom)
				return Rect.Empty;

			// If 'x' spans 'r' horizontally
			if (x.Left <= r.Left && x.Right >= r.Right)
			{
				// If the top edge of 'r' is aligned with the top edge of 'x', or within 'x'
				// then the top edge of the resulting rectangle is 'x.Bottom'.
				if (x.Top <= r.Top) return new Rect(r.Left, x.Bottom, r.Right - r.Left, r.Bottom - x.Bottom);
				if (x.Bottom >= r.Bottom) return new Rect(r.Left, r.Top, r.Right - r.Left, x.Top - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			// If 'x' spans 'r' vertically
			if (x.Top <= r.Top && x.Bottom >= r.Bottom)
			{
				if (x.Left <= r.Left) return new Rect(x.Right, r.Top, r.Right - x.Right, r.Bottom - r.Top);
				if (x.Right >= r.Right) return new Rect(r.Left, r.Top, x.Left - r.Left, r.Bottom - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
		}
	}

	public static class Drawing_
	{
		// Notes:
		//  - System.Drawing is available in .NET core, System.Windows is not.
		//    So use System.Drawing primitive types for shared code. System.Windows
		//    primitive types are used by WPF however.

		// Points

		public static System.Drawing.PointF ToPointF(this System.Windows.Point pt)
		{
			return new System.Drawing.PointF((float)pt.X, (float)pt.Y);
		}
		public static Maths.v2 ToV2(this System.Windows.Point pt)
		{
			return new Maths.v2((float)pt.X, (float)pt.Y);
		}
		public static System.Windows.Point ToSysWinPoint(this System.Drawing.PointF pt)
		{
			return new System.Windows.Point(pt.X, pt.Y);
		}
		public static System.Windows.Point ToSysWinPoint(this Maths.v2 pt)
		{
			return new System.Windows.Point(pt.x, pt.y);
		}

		// Sizes

		// Rectangles

		public static System.Drawing.RectangleF ToRectF(this System.Windows.Rect rect)
		{
			return new System.Drawing.RectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
		}
		public static System.Windows.Rect ToSysWinRect(this System.Drawing.RectangleF rect)
		{
			return new System.Windows.Rect(rect.X, rect.Y, rect.Width, rect.Height);
		}
	}
}
