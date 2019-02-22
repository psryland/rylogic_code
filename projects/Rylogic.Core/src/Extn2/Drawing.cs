using System;
using System.Drawing;
using Rylogic.Maths;

namespace Rylogic.Extn
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

	public static class Point_
    {
		/// <summary>Convert to 'Point'</summary>
		public static Point ToPoint(this PointF p)
		{
			return new Point((int)p.X, (int)p.Y);
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

		/// <summary>Returns the squared 2D length: X² + Y²</summary>
		public static int Length2Sq(this Point p)
		{
			return Math_.Sqr(p.X) + Math_.Sqr(p.Y);
		}
		public static float Length2Sq(this PointF p)
		{
			return Math_.Sqr(p.X) + Math_.Sqr(p.Y);
		}

		/// <summary>Returns the 2D length: sqrt(X² + Y²)</summary>
		public static float Length2(this Point p)
		{
			return Math_.Sqrt(p.Length2Sq());
		}
		public static float Length2(this PointF p)
		{
			return Math_.Sqrt(p.Length2Sq());
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
 
		/// <summary>lhs - rhs</summary>
		public static Size Subtract(Point lhs, Point rhs)
		{
			return new Size(lhs.X - rhs.X, lhs.Y - rhs.Y);
		}
		public static SizeF Subtract(PointF lhs, PointF rhs)
		{
			return new SizeF(lhs.X - rhs.X, lhs.Y - rhs.Y);
		}
		public static Point Subtract(Point lhs, Size rhs)
		{
			return new Point(lhs.X - rhs.Width, lhs.Y - rhs.Height);
		}
		public static PointF Subtract(PointF lhs, SizeF rhs)
		{
			return new PointF(lhs.X - rhs.Width, lhs.Y - rhs.Height);
		}

		/// <summary>Returns a normalised version of this point</summary>
		public static PointF Normalised(this PointF pt)
		{
			double len = pt.Length2();
			if (Math.Abs(len - 0.0) < double.Epsilon) throw new DivideByZeroException("Cannot normalise a zero vector");
			return new PointF((float)(pt.X / len), (float)(pt.Y / len));
		}
	}

	public static class Size_
	{
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

		/// <summary>Returns the aspect ratio (W/H) (returns infinite if height is zero)</summary>
		public static double Aspect(this Size s)
		{
			return (double)s.Width / s.Height;
		}
		public static double Aspect(this SizeF s)
		{
			return (double)s.Width / s.Height;
		}

		/// <summary>Returns the signed area. Returns a negative value if Width and/or Height are negative</summary>
		public static int Area(this Size s)
		{
			return Math_.SignI(s.Width >= 0 && s.Height >= 0) * Math.Abs(s.Width * s.Height);
		}
		public static float Area(this SizeF s)
		{
			return Math_.SignF(s.Width >= 0 && s.Height >= 0) * Math.Abs(s.Width * s.Height);
		}

		/// <summary>Returns the squared 2D length: X² + Y²</summary>
		public static int Length2Sq(this Size s)
		{
			return Math_.Sqr(s.Width) + Math_.Sqr(s.Height);
		}
		public static float Length2Sq(this SizeF s)
		{
			return Math_.Sqr(s.Width) + Math_.Sqr(s.Height);
		}

		/// <summary>Returns the 2D length: sqrt(X² + Y²)</summary>
		public static float Length2(this Size s)
		{
			return Math_.Sqrt(s.Length2Sq());
		}
		public static float Length2(this SizeF s)
		{
			return Math_.Sqrt(s.Length2Sq());
		}
	}

	public static class Rectangle_
	{
		/// <summary>Convert this floating point rectangle into a rectangle</summary>
		public static Rectangle ToRect(this RectangleF r)
		{
			return new Rectangle(
				(int)Math.Round(r.Left  , MidpointRounding.AwayFromZero),
				(int)Math.Round(r.Top   , MidpointRounding.AwayFromZero),
				(int)Math.Round(r.Width , MidpointRounding.AwayFromZero),
				(int)Math.Round(r.Height, MidpointRounding.AwayFromZero));
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

		/// <summary>Returns the aspect ratio (W/H) (returns infinite if height is zero)</summary>
		public static double Aspect(this Rectangle r)
		{
			return r.Size.Aspect();
		}
		public static double Aspect(this RectangleF r)
		{
			return r.Size.Aspect();
		}

		/// <summary>Returns the signed area. Returns a negative value if Width and/or Height are negative</summary>
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

		/// <summary>Returns a rectangle with Width/Height clamped to >= 0</summary>
		public static Rectangle ClampPositive(this Rectangle r)
		{
			return new Rectangle(r.X, r.Y, Math.Max(r.Width, 0), Math.Max(r.Height, 0)); 
		}
		public static RectangleF ClampPositive(this RectangleF r)
		{
			return new RectangleF(r.X, r.Y, Math.Max(r.Width, 0), Math.Max(r.Height, 0)); 
		}

		/// <summary>Returns a rectangle with Width/Height clamped to <= 0</summary>
		public static Rectangle ClampNegative(this Rectangle r)
		{
			return new Rectangle(r.X, r.Y, Math.Min(r.Width, 0), Math.Min(r.Height, 0)); 
		}
		public static RectangleF ClampNegative(this RectangleF r)
		{
			return new RectangleF(r.X, r.Y, Math.Min(r.Width, 0), Math.Min(r.Height, 0)); 
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

		/// <summary>Compare 'pt' to 'rect' and return the zone that it is in</summary>
		public static EBoxZone GetBoxZone(this RectangleF rect, PointF pt)
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
		public static EBoxZone GetBoxZone(this Rectangle rect, Point pt)
		{
			return GetBoxZone((RectangleF)rect, (PointF)pt);
		}

		/// <summary>Returns a rectangle scaled such that 'rect' will fit centred within 'area'</summary>
		public static RectangleF ZoomToFit(RectangleF area, SizeF rect, bool allow_grow = true)
		{
			if (rect.Width == 0 && rect.Height == 0)
				return new RectangleF(area.Centre(), SizeF.Empty);
		
			// If we need to zoom out to fit 'rect' within 'area', or zoom in is allowed,
			// return a  rectangle the fills 'area' while preserving the aspect ratio of 'rect'.
			if (allow_grow || rect.Width > area.Width || rect.Height > area.Height)
			{
				// If the aspect ratio (width/height) of 'rect' is greater than the
				// aspect ratio of 'area' then 'rect' is width bound within 'area'
				if (rect.Width * area.Height > area.Width * rect.Height)
				{
					// 'rect.Width' cannot be zero, because if it was we wouldn't be width bound
					rect.Height = area.Width * rect.Height / rect.Width;
					rect.Width  = area.Width;
				}
				// Otherwise, 'rect' is height bound within 'area'
				else
				{
					// 'rect.Height' cannot be zero, because if it was we wouldn't be height bound
					rect.Width  = area.Height * rect.Width / rect.Height;
					rect.Height = area.Height;
				}
			}

			// Return 'rect' centred within 'area'
			return new RectangleF(
				area.Left + (area.Width - rect.Width)/2,
				area.Top + (area.Height - rect.Height)/2,
				rect.Width,
				rect.Height);
		}

		// Note: There's already a static method 'Union'
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
namespace Rylogic.Maths
{
	public static partial class Math_
	{
		/// <summary>Component-wise minimum</summary>
		public static Size Min(Size lhs, Size rhs)
		{
			return new Size(
				Math.Min(lhs.Width, rhs.Width),
				Math.Min(lhs.Height, rhs.Height));
		}
		public static SizeF Min(SizeF lhs, SizeF rhs)
		{
			return new SizeF(
				Math.Min(lhs.Width, rhs.Width),
				Math.Min(lhs.Height, rhs.Height));
		}

		/// <summary>Component-wise maximum</summary>
		public static Size Max(Size lhs, Size rhs)
		{
			return new Size(
				Math.Max(lhs.Width, rhs.Width),
				Math.Max(lhs.Height, rhs.Height));
		}
		public static SizeF Max(SizeF lhs, SizeF rhs)
		{
			return new SizeF(
				Math.Max(lhs.Width, rhs.Width),
				Math.Max(lhs.Height, rhs.Height));
		}
	}
}