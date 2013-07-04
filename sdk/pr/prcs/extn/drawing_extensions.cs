//***************************************************
// Drawing Extensions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System.Drawing;

namespace pr.extn
{
	public static class DrawingExtensions
	{
		// There's already a static method 'Union'
		///// <summary>Replaces this rectangle with the union of itself and 'rect'</summary>
		//public static void Union(this Rectangle r, Rectangle rect)
		//{
		//    r.X = Math.Min(r.X, rect.X);
		//    r.Y = Math.Min(r.Y, rect.Y);
		//    r.Width  = Math.Max(r.Right  - r.X, rect.Right  - r.X);
		//    r.Height = Math.Max(r.Bottom - r.Y, rect.Bottom - r.Y);
		//}

		/// <summary>Returns the top left point of the rectangle</summary>
		public static Point TopLeft(this Rectangle r)
		{
			return new Point(r.Left, r.Top);
		}

		/// <summary>Returns the top centre point of the rectangle</summary>
		public static Point TopCentre(this Rectangle r)
		{
			return new Point(r.Left + r.Width/2, r.Top);
		}

		/// <summary>Returns the top right point of the rectangle</summary>
		public static Point TopRight(this Rectangle r)
		{
			return new Point(r.Right, r.Top);
		}

		/// <summary>Returns the left centre point of the rectangle</summary>
		public static Point LeftCentre(this Rectangle r)
		{
			return new Point(r.Left, r.Top + r.Height/2);
		}

		/// <summary>Returns the center of the rectangle</summary>
		public static Point Centre(this Rectangle r)
		{
			return new Point(r.X + r.Width/2, r.Y + r.Height/2);
		}

		/// <summary>Returns the right centre point of the rectangle</summary>
		public static Point RightCentre(this Rectangle r)
		{
			return new Point(r.Right, r.Top + r.Height/2);
		}

		/// <summary>Returns the bottom left point of the rectangle</summary>
		public static Point BottomLeft(this Rectangle r)
		{
			return new Point(r.Left, r.Bottom);
		}

		/// <summary>Returns the bottom centre point of the rectangle</summary>
		public static Point BottomCentre(this Rectangle r)
		{
			return new Point(r.Left + r.Width/2, r.Bottom);
		}

			/// <summary>Returns the bottom right point of the rectangle</summary>
		public static Point BottomRight(this Rectangle r)
		{
			return new Point(r.Right, r.Bottom);
		}
	}
}
