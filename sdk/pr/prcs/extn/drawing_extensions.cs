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
		
		/// <summary>Returns the center of the rectangle</summary>
		public static Point Centre(this Rectangle r)
		{
			return new Point(r.X + r.Width/2, r.Y + r.Height/2);
		}
	}
}
