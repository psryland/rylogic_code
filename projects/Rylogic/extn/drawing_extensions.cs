//***************************************************
// Drawing Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using pr.gfx;
using pr.maths;
using pr.util;
using pr.win32;

namespace pr.extn
{
	[Flags] public enum EBoxZone
	{
		None   = 0,
		Left   = 1 << 0,
		Right  = 1 << 1,
		Top    = 1 << 2,
		Bottom = 1 << 3,
		TL     = Left  | Top,
		TR     = Right | Top,
		BL     = Left  | Bottom,
		BR     = Right | Bottom,
		LR     = Left | Right,
		TB     = Top  | Bottom,
		Centre = Left|Top|Right|Bottom,
	}

	public static class DrawingEx
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

		/// <summary>Compare 'pt' to 'rect' and return the zone that it is in</summary>
		public static EBoxZone GetBoxZone(Rectangle rect, Point pt)
		{
			// TL | T | TR
			// ---+---+---
			//  L | C | R 
			// ---+---+---
			// BL | B | BR
			//
			// Remember: Use rect.Inflated to shrink a rectangle by the border with before calling this function
			// 
			var mask = EBoxZone.None;
			if (pt.X <  rect.Left  ) mask |= EBoxZone.Left;
			if (pt.X >= rect.Right ) mask |= EBoxZone.Right;
			if (pt.Y <  rect.Top   ) mask |= EBoxZone.Top;
			if (pt.Y >= rect.Bottom) mask |= EBoxZone.Bottom;
			return mask;
		}

		/// <summary>Get the standard resize cursor for this box zone value</summary>
		public static Cursor ToCursor(this EBoxZone bz)
		{
			if (bz == EBoxZone.Left || bz == EBoxZone.Right  || bz == EBoxZone.LR) return Cursors.SizeWE;
			if (bz == EBoxZone.Top  || bz == EBoxZone.Bottom || bz == EBoxZone.TB) return Cursors.SizeNS;
			if (bz == EBoxZone.TL   || bz == EBoxZone.BR) return Cursors.SizeNWSE;
			if (bz == EBoxZone.BL   || bz == EBoxZone.TR) return Cursors.SizeNESW;
			if (bz == EBoxZone.Centre) return Cursors.SizeAll;
			return Cursors.Default;
		}

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

		/// <summary>Returns this size with X,Y multiplied by 'scale'</summary>
		public static Size Scaled(this Size s, float scale)
		{
			return new Size((int)(s.Width * scale), (int)(s.Height * scale));
		}
		public static SizeF Scaled(this SizeF s, float scale)
		{
			return new SizeF(s.Width * scale, s.Height * scale);
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

		/// <summary>Returns the signed area. Returns a negative value if Width and/or Height are negative</summary>
		public static int Area(this Size s)
		{
			return Maths.SignI(s.Width >= 0 && s.Height >= 0) * Math.Abs(s.Width * s.Height);
		}
		public static float Area(this SizeF s)
		{
			return Maths.SignF(s.Width >= 0 && s.Height >= 0) * Math.Abs(s.Width * s.Height);
		}
		public static int Area(this Rectangle r)
		{
			return r.Size.Area();
		}
		public static float Area(this RectangleF r)
		{
			return r.Size.Area();
		}
		public static int Area(this RectangleRef r)
		{
			return r.Size.Area();
		}

		/// <summary>Returns the aspect ratio (W/H) (returns infinite if height is zero)</summary>
		public static double Aspect(this Size s)
		{
			return (double)s.Width / s.Height;
		}
		public static double Aspect(this SizeF s)
		{
			return (double)s.Width / s.Height;
		}
		public static double Aspect(this Rectangle r)
		{
			return r.Size.Aspect();
		}
		public static double Aspect(this RectangleF r)
		{
			return r.Size.Aspect();
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

		/// <summary>Returns a rectangle inflated</summary>
		public static Rectangle Inflated(this Rectangle r, int d)
		{
			return new Rectangle(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
		}
		public static RectangleF Inflated(this RectangleF r, float d)
		{
			return new RectangleF(r.X - d, r.Y - d, r.Width + 2 * d, r.Height + 2 * d);
		}
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
			// If 'x' has no area, then subtraction is identity
			if (x.Area() <= 0)
				return r;

			// If the rectangles do not overlap. Right/Bottom is not considered 'in' the rectangle
			if (r.Left >= x.Right || r.Right <= x.Left || r.Top >= x.Bottom || r.Bottom <= x.Top)
				return r;

			// If 'x' completely covers 'r' then the result is empty
			if (x.Left <= r.Left && x.Right >= r.Right && x.Top <= r.Top && x.Bottom >= r.Bottom)
				return Rectangle.Empty;

			// If 'x' spans 'r' horizontally
			if (x.Left <= r.Left && x.Right >= r.Right)
			{
				// If the top edge of 'r' is aligned with the top edge of 'x', or within 'x'
				// then the top edge of the resulting rectangle is 'x.Bottom'.
				if (x.Top    <= r.Top)    return Rectangle.FromLTRB(r.Left, x.Bottom, r.Right, r.Bottom);
				if (x.Bottom >= r.Bottom) return Rectangle.FromLTRB(r.Left, r.Top, r.Right, x.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			// If 'x' spans 'r' vertically
			if (x.Top <= r.Top && x.Bottom >= r.Bottom)
			{
				if (x.Left  <= r.Left)  return Rectangle.FromLTRB(x.Right, r.Top, r.Right, r.Bottom);
				if (x.Right >= r.Right) return Rectangle.FromLTRB(r.Left, r.Top, x.Left, r.Bottom);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
		}

		/// <summary>Apply this transform to a single point</summary>
		public static Point TransformPoint(this Matrix m, Point pt)
		{
			var p = new[] {pt};
			m.TransformPoints(p);
			return p[0];
		}
		public static PointF TransformPoint(this Matrix m, PointF pt)
		{
			var p = new[] {pt};
			m.TransformPoints(p);
			return p[0];
		}

		/// <summary>Apply this transform to a rectangle</summary>
		public static Rectangle TransformRect(this Matrix m, Rectangle rect)
		{
			var p = new[] {rect.TopLeft(), rect.BottomRight()};
			m.TransformPoints(p);
			return Rectangle.FromLTRB(p[0].X, p[0].Y, p[1].X, p[1].Y);
		}
		public static RectangleF TransformRect(this Matrix m, RectangleF rect)
		{
			var p = new[] {rect.TopLeft(), rect.BottomRight()};
			m.TransformPoints(p);
			return RectangleF.FromLTRB(p[0].X, p[0].Y, p[1].X, p[1].Y);
		}

		/// <summary>lhs - rhs</summary>
		public static Size Subtract(Point lhs, Point rhs)
		{
			return new Size(lhs.X - rhs.X, lhs.Y - rhs.Y);
		}
		public static SizeF Subtract(PointF lhs, PointF rhs)
		{
			return new SizeF(lhs.X - rhs.X, lhs.Y - rhs.Y);
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
		public static Font Dup(this Font prototype, FontFamily family = null, float? em_size = null, float? delta_size = null, FontStyle? style = null, GraphicsUnit? unit = null)
		{
			return new Font(
				family  ?? prototype.FontFamily, 
				em_size ?? ((delta_size ?? 0) + prototype.Size),
				style   ?? prototype.Style,
				unit    ?? prototype.Unit
				);
		}

		/// <summary>Returns a scope object that locks and unlocks the data of a bitmap</summary>
		public static Scope<BitmapData> LockBits(this Bitmap bm, ImageLockMode mode, PixelFormat format, Rectangle? rect = null)
		{
			return Scope<BitmapData>.Create(
				() => bm.LockBits(rect ?? bm.Size.ToRect(), mode, format),
				dat => bm.UnlockBits(dat));
		}

		/// <summary>Generate a GraphicsPath from this bitmap by rastering the non-background pixels</summary>
		public static GraphicsPath ToGraphicsPath(this Bitmap bitmap, Color? bk_colour = null)
		{
			var gp = new GraphicsPath();

			// Copy the bitmap to memory
			int[] px; int sign;
			using (var bits = bitmap.LockBits(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
			{
				const int bytes_per_px = 4;
				sign = Maths.Sign(bits.Value.Stride);
				px = new int[Math.Abs(bits.Value.Stride) * bits.Value.Height / bytes_per_px];
				Marshal.Copy(bits.Value.Scan0, px, 0, px.Length);
			}

			// Get the transparent colour
			var bkcol = bk_colour?.ToArgb() ?? px[0];

			// Generates a graphics path by rastering a bitmap into rectangles
			var y    = sign > 0 ? 0 : bitmap.Height - 1;
			var yend = sign > 0 ? bitmap.Height : -1;
			for (; y != yend; y += sign)
			{
				// Find the first non-background colour pixel
				int x0; for (x0 = 0; x0 != bitmap.Width && px[y * bitmap.Height + x0] == bkcol; ++x0) {}

				// Find the last non-background colour pixel
				int x1; for (x1 = bitmap.Width; x1-- != 0 && px[y * bitmap.Height + x1] == bkcol; ) {}

				// Add a rectangle for the raster line
				gp.AddRectangle(new Rectangle(x0, y, x1-x0+1, 1));
			}
			return gp;
		}

		/// <summary>Convert this bitmap to a cursor with hotspot at 'hot_spot'</summary>
		public static Cursor ToCursor(this Bitmap bm, Point hot_spot)
		{
			var tmp = new Win32.IconInfo();
			Win32.GetIconInfo(bm.GetHicon(), ref tmp);
			tmp.xHotspot = hot_spot.X;
			tmp.yHotspot = hot_spot.Y;
			tmp.fIcon = false;
			return new Cursor(Win32.CreateIconIndirect(ref tmp));
		}

		/// <summary>Convert this bitmap to an icon</summary>
		public static Icon ToIcon(this Bitmap bm)
		{
			if (bm.Width > 128 || bm.Height > 128) throw new Exception("Icons can only be created from bitmaps up to 128x128 pixels in size");
			using (var handle = Scope.Create(() => bm.GetHicon(), h => Win32.DestroyIcon(h)))
				return Icon.FromHandle(handle.Value);
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

		public override string ToString()
		{
			return ((Point)this).ToString();
		}
	}

	/// <summary>A reference type for a size (convertible to Size)</summary>
	public class SizeRef
	{
		public SizeRef() { }
		public SizeRef(Size s) :this(s.Width, s.Height)
		{}
		public SizeRef(int w, int h)
		{
			Width = w;
			Height = h;
		}

		public int Width { get; set; }
		public int Height { get; set; }

		/// <summary>Implicit conversion to the Point struct</summary>
		public static implicit operator Size(SizeRef s)
		{
			return new Size(s.Width, s.Height);
		}

		public override string ToString()
		{
			return ((Size)this).ToString();
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
			set { Width = Right - value; X = value; }
		}

		/// <summary>Get/Set the top edge of the rectangle</summary>
		public int Top
		{
			get { return Y; }
			set { Height = Bottom - value; Y = value; }
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

		/// <summary>The Top/Left of the rectangle</summary>
		public Point Location
		{
			get { return new Point(X, Y); }
			set { X = value.X; Y = value.Y; }
		}

		/// <summary>The size of the rectangle</summary>
		public Size Size
		{
			get { return new Size(Width, Height); }
			set { Width = value.Width; Height = value.Height; }
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
