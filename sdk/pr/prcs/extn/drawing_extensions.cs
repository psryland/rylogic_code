//***************************************************
// Drawing Extensions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System.Drawing;
using pr.gfx;
using pr.maths;

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

		/// <summary>Convert this point into a size</summary>
		public static Size ToSize(this Point p)
		{
			return new Size(p);
		}
		public static SizeF ToSize(this PointF p)
		{
			return new SizeF(p);
		}

		/// <summary>Convert this size into a point</summary>
		public static Point ToPoint(this Size s)
		{
			return new Point(s);
		}
		public static PointF ToPoint(this SizeF s)
		{
			return new PointF(s.Width, s.Height);
		}

		/// <summary>Convert this size into a rectangle</summary>
		public static Rectangle ToRect(this Size s)
		{
			return new Rectangle(0, 0, s.Width, s.Height);
		}
		public static RectangleF ToRect(this SizeF s)
		{
			return new RectangleF(0f, 0f, s.Width, s.Height);
		}

		/// <summary>Returns the top left point of the rectangle</summary>
		public static Point TopLeft(this Rectangle r)
		{
			return new Point(r.Left, r.Top);
		}
		public static PointF TopLeft(this RectangleF r)
		{
			return new PointF(r.Left, r.Top);
		}

		/// <summary>Returns the top centre point of the rectangle</summary>
		public static Point TopCentre(this Rectangle r)
		{
			return new Point(r.Left + r.Width/2, r.Top);
		}
		public static PointF TopCentre(this RectangleF r)
		{
			return new PointF(r.Left + r.Width/2, r.Top);
		}

		/// <summary>Returns the top right point of the rectangle</summary>
		public static Point TopRight(this Rectangle r)
		{
			return new Point(r.Right, r.Top);
		}
		public static PointF TopRight(this RectangleF r)
		{
			return new PointF(r.Right, r.Top);
		}

		/// <summary>Returns the left centre point of the rectangle</summary>
		public static Point LeftCentre(this Rectangle r)
		{
			return new Point(r.Left, r.Top + r.Height/2);
		}
		public static PointF LeftCentre(this RectangleF r)
		{
			return new PointF(r.Left, r.Top + r.Height/2);
		}

		/// <summary>Returns the center of the rectangle</summary>
		public static Point Centre(this Rectangle r)
		{
			return new Point(r.X + r.Width/2, r.Y + r.Height/2);
		}
		public static PointF Centre(this RectangleF r)
		{
			return new PointF(r.X + r.Width/2, r.Y + r.Height/2);
		}

		/// <summary>Returns the right centre point of the rectangle</summary>
		public static Point RightCentre(this Rectangle r)
		{
			return new Point(r.Right, r.Top + r.Height/2);
		}
		public static PointF RightCentre(this RectangleF r)
		{
			return new PointF(r.Right, r.Top + r.Height/2);
		}

		/// <summary>Returns the bottom left point of the rectangle</summary>
		public static Point BottomLeft(this Rectangle r)
		{
			return new Point(r.Left, r.Bottom);
		}
		public static PointF BottomLeft(this RectangleF r)
		{
			return new PointF(r.Left, r.Bottom);
		}

		/// <summary>Returns the bottom centre point of the rectangle</summary>
		public static Point BottomCentre(this Rectangle r)
		{
			return new Point(r.Left + r.Width/2, r.Bottom);
		}
		public static PointF BottomCentre(this RectangleF r)
		{
			return new PointF(r.Left + r.Width/2, r.Bottom);
		}

		/// <summary>Returns the bottom right point of the rectangle</summary>
		public static Point BottomRight(this Rectangle r)
		{
			return new Point(r.Right, r.Bottom);
		}
		public static PointF BottomRight(this RectangleF r)
		{
			return new PointF(r.Right, r.Bottom);
		}

		/// <summary>Returns a point shifted by dx,dy</summary>
		public static Point Shifted(this Point pt, int dx, int dy)
		{
			return new Point(pt.X + dx, pt.Y + dy);
		}
		public static PointF Shifted(this PointF pt, float dx, float dy)
		{
			return new PointF(pt.X + dx, pt.Y + dy);
		}

		/// <summary>Returns a rectangle shifted by dx,dy</summary>
		public static Rectangle Shifted(this Rectangle r, int dx, int dy)
		{
			return new Rectangle(r.X + dx, r.Y + dy, r.Width, r.Height);
		}
		public static RectangleF Shifted(this RectangleF r, float dx, float dy)
		{
			return new RectangleF(r.X + dx, r.Y + dy, r.Width, r.Height);
		}

		/// <summary>Returns a rectangle inflated by dx,dy</summary>
		public static Rectangle Inflated(this Rectangle r, int dx, int dy)
		{
			return new Rectangle(r.X - dx, r.Y - dy, r.Width + 2 * dx, r.Height + 2 * dy);
		}
		public static RectangleF Inflated(this RectangleF r, float dx, float dy)
		{
			return new RectangleF(r.X - dx, r.Y - dy, r.Width + 2 * dx, r.Height + 2 * dy);
		}
		public static Rectangle Inflated(this Rectangle r, int dleft, int dtop, int dright, int dbottom)
		{
			return new Rectangle(r.X - dleft, r.Y - dtop, r.Width + dleft + dright, r.Height + dtop + dbottom);
		}
		public static RectangleF Inflated(this RectangleF r, float dleft, float dtop, float dright, float dbottom)
		{
			return new RectangleF(r.X - dleft, r.Y - dtop, r.Width + dleft + dright, r.Height + dtop + dbottom);
		}

		/// <summary>Linearly interpolate from this colour to 'dst' by 'frac'</summary>
		public static Color Lerp(this Color src, Color dst, float frac)
		{
			return Color.FromArgb(
				(int)Maths.Clamp(src.A * (1f - frac) + dst.A * frac, 0f, 255f),
				(int)Maths.Clamp(src.R * (1f - frac) + dst.R * frac, 0f, 255f),
				(int)Maths.Clamp(src.G * (1f - frac) + dst.G * frac, 0f, 255f),
				(int)Maths.Clamp(src.B * (1f - frac) + dst.B * frac, 0f, 255f));
		}

		/// <summary>Convert this colour to HSV</summary>
		public static HSV ToHSV(this Color src)
		{
			return HSV.FromColor(src);
		}
	}
}
