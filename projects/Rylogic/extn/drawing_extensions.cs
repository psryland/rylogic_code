//***************************************************
// Drawing Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Drawing.Imaging;
using pr.gfx;
using pr.maths;
using pr.util;

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

		/// <summary>Returns the centre of the rectangle</summary>
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
		public static Rectangle Shifted(this Rectangle r, Point pt)
		{
			return r.Shifted(pt.X, pt.Y);
		}
		public static RectangleF Shifted(this RectangleF r, PointF pt)
		{
			return r.Shifted(pt.X, pt.Y);
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

		/// <summary>Returns a rectangle with a positive Width/Height</summary>
		public static Rectangle NormalizeRect(this Rectangle r)
		{
			return new Rectangle(
				r.Width  >= 0 ? r.X : r.X + r.Width,
				r.Height >= 0 ? r.Y : r.Y + r.Height,
				(int)Math.Abs(r.Width),
				(int)Math.Abs(r.Height));
		}
		public static RectangleF NormalizeRect(this RectangleF r)
		{
			return new RectangleF(
				r.Width  >= 0 ? r.X : r.X + r.Width,
				r.Height >= 0 ? r.Y : r.Y + r.Height,
				(float)Math.Abs(r.Width),
				(float)Math.Abs(r.Height));
		}

		/// <summary>Reduces the size of this rectangle by excluding the area 'x'. The result must be a rectangle or an exception is thrown</summary>
		public static Rectangle Subtract(this Rectangle r, Rectangle x)
		{
			// If 'x' has not area, then subtraction is identity
			if (x.Size.IsEmpty)
				return r;

			// If 'x' spans 'r' horizontally
			if (x.Left <= r.Left && x.Right >= r.Right)
			{
				if (x.Top    <= r.Top)    return Rectangle.FromLTRB(r.Left, Math.Min(x.Bottom, r.Bottom), r.Right, r.Bottom);
				if (x.Bottom >= r.Bottom) return Rectangle.FromLTRB(r.Left, r.Top, r.Right, Math.Max(x.Top, r.Top));
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			// If 'x' spans 'r' vertically
			if (x.Top <= r.Top && x.Bottom >= r.Bottom)
			{
				if (x.Left  <= r.Left)  return Rectangle.FromLTRB(Math.Min(x.Right, r.Right), r.Top, r.Right, r.Bottom);
				if (x.Right >= r.Right) return Rectangle.FromLTRB(r.Left, r.Top, r.Right, Math.Max(x.Left, r.Left));
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
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

		/// <summary>Convert this colour to ABGR</summary>
		public static int ToAbgr(this Color col)
		{
			return unchecked((int)(((uint)col.A << 24) | ((uint)col.B << 16) | ((uint)col.G << 8) | ((uint)col.R << 0)));
		}

		/// <summary>Convert this colour to it's associated grey-scale value</summary>
		public static Color ToGrayScale(this Color col)
		{
			int gray = (int)(0.3f*col.R + 0.59f*col.G + 0.11f*col.B);
			return Color.FromArgb(col.A, gray, gray, gray);
		}

		/// <summary>Convert this colour to HSV</summary>
		public static HSV ToHSV(this Color src)
		{
			return HSV.FromColor(src);
		}

		/// <summary>Duplicate this font with the changes given</summary>
		public static Font Dup(this Font prototype, FontStyle style)
		{
			return new Font(prototype, style);
		}

		/// <summary>Duplicate this font with the changes given</summary>
		public static Font Dup(this Font prototype, FontFamily family = null, float? em_size = null, FontStyle? style = null, GraphicsUnit? unit = null)
		{
			return new Font(
				family ?? prototype.FontFamily,
				em_size ?? prototype.Size,
				style ?? prototype.Style,
				unit ?? prototype.Unit
				);
		}

		/// <summary>Returns a scope object that locks and unlocks the data of a bitmap</summary>
		public static Scope<BitmapData> LockBitsScope(this Bitmap bm, Rectangle rect, ImageLockMode mode, PixelFormat format)
		{
			return Scope<BitmapData>.Create(
				() => bm.LockBits(rect, mode, format),
				dat => bm.UnlockBits(dat));
		}
	}

	/// <summary>A reference type for a point (convertible to Point)</summary>
	public class PointRef
	{
		public PointRef() { }
		public PointRef(Point p) :this(p.X, p.Y)
		{
		}
		public PointRef(int x, int y)
		{
			X = x;
			Y = y;
		}

		public int X { get; set; }
		public int Y { get; set; }

		/// <summary>Implicit conversion to the Point struct</summary>
		public static implicit operator Point(PointRef p)
		{
			return new Point(p.X, p.Y);
		}
	}

	/// <summary>A reference type for a rectangle (convertible to Rectangle)</summary>
	public class RectangleRef
	{
		public RectangleRef() { }
		public RectangleRef(Point pt, Size sz) :this(pt.X, pt.Y, sz.Width, sz.Height)
		{
		}
		public RectangleRef(Rectangle rect) :this(rect.X, rect.Y, rect.Width, rect.Height)
		{
		}
		public RectangleRef(int x, int y, int width, int height)
		{
			X = x;
			Y = y;
			Width = width;
			Height = height;
		}

		public int X { get; set; }
		public int Y { get; set; }

		public int Width { get; set; }
		public int Height { get; set; }

		/// <summary>Get/Set the left edge of the rectangle</summary>
		public int Left
		{
			get { return X; }
			set { X = value; }
		}

		/// <summary>Get/Set the top edge of the rectangle</summary>
		public int Top
		{
			get { return Y; }
			set { Y = value; }
		}

		/// <summary>Get/Set the right edge of the rectangle</summary>
		public int Right
		{
			get { return X + Width; }
			set { Width = value - X; }
		}

		/// <summary>Get/Set the bottom edge of the rectangle</summary>
		public int Bottom
		{
			get { return Y + Height; }
			set { Height = value - Y; }
		}

		/// <summary>Implicit conversion to the Rectangle struct</summary>
		public static implicit operator Rectangle(RectangleRef r)
		{
			return new Rectangle(r.X, r.Y, r.Width, r.Height);
		}

		public override string ToString()
		{
			return ((Rectangle)this).ToString();
		}
	}
}
