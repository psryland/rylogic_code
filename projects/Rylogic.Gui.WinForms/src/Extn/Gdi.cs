//***************************************************
// Colour128
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Interop.Win32;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	/// <summary>Static functions related to graphics</summary>
	public static class Gdi
	{
		/// <summary>Return all colours satisfying a predicate</summary>
		public static IEnumerable<Color> GetColors(Func<Color,bool> pred)
		{
			foreach (var c in Enum.GetValues(typeof(KnownColor)))
			{
				KnownColor kc = (KnownColor)c;
				Color col = Color.FromKnownColor(kc);
				if (pred(col)) yield return col;
			}
		}

		/// <summary>The identity colour matrix</summary>
		public static ColorMatrix IdentityCM
		{
			get { return new ColorMatrix{Matrix00=1, Matrix11=1, Matrix22=1, Matrix33=1, Matrix44=1}; }
		}

		/// <summary>Create a rounded rectangle path that can be filled or drawn</summary>
		public static GraphicsPath RoundedRectanglePath(RectangleF rect, float radius)
		{
			float d = radius * 2f;
			Debug.Assert(d <= rect.Width && d <= rect.Height);

			var gp = new GraphicsPath();

			gp.AddArc(rect.Left      , rect.Top        , d , d, 180 ,90);
			gp.AddArc(rect.Right - d , rect.Top        , d , d, 270 ,90);
			gp.AddArc(rect.Right - d , rect.Bottom - d , d , d,   0 ,90);
			gp.AddArc(rect.Left      , rect.Bottom - d , d , d,  90 ,90);

			gp.AddLine(rect.Left, rect.Bottom - d/2, rect.Left, rect.Top + d/2);
			return gp;
		}

		/// <summary>Helper for making radial gradient brushes</summary>
		public static PathGradientBrush CreateRadialGradientBrush(Point centre, float radiusX, float radiusY, Color centre_color, Color boundary_color)
		{
			var path = new GraphicsPath();
			path.AddEllipse(new RectangleF(centre.X - radiusX, centre.Y - radiusY, 2*radiusX, 2*radiusY));
			var brush = new PathGradientBrush(path);
			brush.CenterColor = centre_color;
			brush.CenterPoint = centre;
			brush.SurroundColors = new[]{boundary_color};
			return brush;
		}

		/// <summary>Create a gradient brush that paints a gradient along a vector</summary>
		public static LinearGradientBrush CreateLinearGradientBrush(Point pt0, Point pt1, Color colour0, Color colour1, WrapMode mode = WrapMode.Clamp)
		{
			return new LinearGradientBrush(pt0, pt1, colour0, colour1)
			{
				WrapMode = mode,
			};
		}

		/// <summary>Converts a bitmap strip into an array of bitmaps</summary>
		public static Bitmap[] LoadStrip(Bitmap strip, int count)
		{
			if ((strip.Width % count) != 0)
				throw new Exception("Bitmap strip width must divide evenly by the number of images in the strip");

			var w = strip.Width / count;
			var h = strip.Height;
			var bm = new Bitmap[count];
			for (int i = 0; i != count; ++i)
				bm[i] = strip.Clone(new Rectangle(i * w, 0, w, h), strip.PixelFormat);

			return bm;
		}

		/// <summary></summary>
		public static void RectangleDropShadow(Graphics gfx, Rectangle rc, Color shadow_colour, int depth, int max_opacity)
		{
			// Generate a circle with dark centre and light outside
			var bm = new Bitmap(2*depth, 2*depth);
			using (var path = new GraphicsPath())
			{
				path.AddEllipse(0, 0, 2*depth, 2*depth);
				using (var pgb = new PathGradientBrush(path))
				{
					pgb.CenterColor    = Color.FromArgb(max_opacity, shadow_colour);
					pgb.SurroundColors = new Color[]{Color.FromArgb(0, shadow_colour)};
					using (var g = Graphics.FromImage(bm))
						g.FillEllipse(pgb, 0, 0, 2*depth, 2*depth);
				}
			}
			using (var sb = new SolidBrush(Color.FromArgb(max_opacity, shadow_colour)))
				gfx.FillRectangle(sb, rc.Left+depth, rc.Top+depth, rc.Width-(2*depth), rc.Height-(2*depth));

			gfx.DrawImage(bm , new Rectangle(rc.Left          , rc.Top            , depth              , depth               ) , 0     , 0     , depth , depth , GraphicsUnit.Pixel); // Top left corner
			gfx.DrawImage(bm , new Rectangle(rc.Left  + depth , rc.Top            , rc.Width - 2*depth , depth               ) , depth , 0     , 1     , depth , GraphicsUnit.Pixel); // Top side
			gfx.DrawImage(bm , new Rectangle(rc.Right - depth , rc.Top            , depth              , depth               ) , depth , 0     , depth , depth , GraphicsUnit.Pixel); // Top right corner
			gfx.DrawImage(bm , new Rectangle(rc.Right - depth , rc.Top    + depth , depth              , rc.Height - 2*depth ) , depth , depth , depth , 1     , GraphicsUnit.Pixel); // Right side
			gfx.DrawImage(bm , new Rectangle(rc.Right - depth , rc.Bottom - depth , depth              , depth               ) , depth , depth , depth , depth , GraphicsUnit.Pixel); // Bottom left corner
			gfx.DrawImage(bm , new Rectangle(rc.Left  + depth , rc.Bottom - depth , rc.Width - 2*depth , depth               ) , depth , depth , 1     , depth , GraphicsUnit.Pixel); // Bottom side
			gfx.DrawImage(bm , new Rectangle(rc.Left          , rc.Bottom - depth , depth              , depth               ) , 0     , depth , depth , depth , GraphicsUnit.Pixel); // Bottom left corner
			gfx.DrawImage(bm , new Rectangle(rc.Left          , rc.Top    + depth , depth              , rc.Height - 2*depth ) , 0     , depth , depth , 1     , GraphicsUnit.Pixel); // Left side
		}

		/// <summary>Save the transform and clip state in an RAII object</summary>
		public static Scope<GraphicsContainer> SaveState(this Graphics gfx)
		{
			return Scope.Create(
				() => gfx.BeginContainer(),
				ctr => gfx.EndContainer(ctr));
		}

		/// <summary>Get the HDC associated with this graphics object. Releases in dispose</summary>
		public static Scope<IntPtr> GetHdcScope(this Graphics gfx)
		{
			return Scope.Create(
				() => gfx.GetHdc(),
				ptr => gfx.ReleaseHdc(ptr));
		}

		/// <summary>Create a region from a list of x,y pairs defining line segments</summary>
		public static Region MakeRegion(params int[] xy)
		{
			if ((xy.Length % 2) == 1) throw new Exception("Point list must be a list of X,Y pairs");
			return MakeRegion(xy.InPairs().Select(x => new Point(x.Item1, x.Item2)).ToArray());
		}

		/// <summary>Create a region from a list of x,y pairs defining line segments</summary>
		public static Region MakeRegion(params Point[] pts)
		{
			if (pts.Length == 0) return new Region(Rectangle.Empty);
			var types = pts.Select(x => (byte)PathPointType.Line).ToArray();
			types[0] = (byte)PathPointType.Start;
			return new Region(new GraphicsPath(pts, types));
		}

		/// <summary>Useful overload of DrawImage</summary>
		public static void DrawImage(this Graphics gfx, Image image, Rectangle dst_rect, Rectangle src_rect, GraphicsUnit unit, ImageAttributes attr)
		{
			gfx.DrawImage(image, dst_rect, src_rect.X, src_rect.Y, src_rect.Width, src_rect.Height, unit, attr);
		}

		/// <summary>Useful overload of DrawImage</summary>
		public static void DrawImage(this Graphics gfx, Image image, int X, int Y, Rectangle src_rect, GraphicsUnit unit, ImageAttributes attr)
		{
			gfx.DrawImage(image, new Rectangle(X, Y, src_rect.Width, src_rect.Height), src_rect, unit, attr);
		}

		/// <summary>Draws a rectangle with rounded corners</summary>
		public static void DrawRectangleRounded(this Graphics gfx, Pen pen, RectangleF rect, float radius)
		{
			gfx.DrawPath(pen, Gdi.RoundedRectanglePath(rect, radius));
		}

		/// <summary>Fill a rectangle with rounded corners</summary>
		public static void FillRectangleRounded(this Graphics gfx, Brush brush, RectangleF rect, float radius)
		{
			gfx.FillPath(brush, Gdi.RoundedRectanglePath(rect, radius));
		}

		/// <summary>Returns a scope object that locks and unlocks the data of a bitmap</summary>
		public static Scope<BitmapData> LockBits(this Bitmap bm, ImageLockMode mode, PixelFormat? format = null, Rectangle? rect = null)
		{
			return Scope.Create(
				() => bm.LockBits(rect ?? bm.Size.ToRect(), mode, format ?? bm.PixelFormat),
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
				sign = Math_.Sign(bits.Value.Stride);
				px = new int[Math.Abs(bits.Value.Stride) * bits.Value.Height / bytes_per_px];
				Marshal.Copy(bits.Value.Scan0, px, 0, px.Length);
			}

			// Get the transparent colour
			var bkcol = bk_colour?.ToArgb() ?? px[0];

			// Generates a graphics path by rastering a bitmap into rectangles
			var y = sign > 0 ? 0 : bitmap.Height - 1;
			var yend = sign > 0 ? bitmap.Height : -1;
			for (; y != yend; y += sign)
			{
				// Find the first non-background colour pixel
				int x0; for (x0 = 0; x0 != bitmap.Width && px[y * bitmap.Height + x0] == bkcol; ++x0) { }

				// Find the last non-background colour pixel
				int x1; for (x1 = bitmap.Width; x1-- != 0 && px[y * bitmap.Height + x1] == bkcol;) { }

				// Add a rectangle for the raster line
				gp.AddRectangle(new Rectangle(x0, y, x1 - x0 + 1, 1));
			}
			return gp;
		}

		/// <summary>Convert this bitmap to a cursor with hotspot at 'hot_spot'</summary>
		public static Cursor ToCursor(this Bitmap bm, Point hot_spot)
		{
			var tmp = new Win32.ICONINFO();
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

		/// <summary>Creates a bitmap containing a checker board where each 'checker' is 'sx' x 'sy'</summary>
		public static Image BitmapChecker(int sx, int sy, uint X = 0xFFFFFFFFU, uint O = 0x00000000U)
		{
			var w = 2 * sx;
			var h = 2 * sy;
			var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			var row0 = Array_.New(sx, unchecked((int)O));
			var row1 = Array_.New(sx, unchecked((int)X));
			using (var bits = bmp.LockBits(ImageLockMode.WriteOnly))
			{
				for (int j = 0; j != sy; ++j)
				{
					Marshal.Copy(row0, 0, bits.Value.Scan0 + ((j + 0) * w) * sizeof(int), row0.Length);
					Marshal.Copy(row1, 0, bits.Value.Scan0 + ((j + 0) * w + sx) * sizeof(int), row1.Length);
					Marshal.Copy(row1, 0, bits.Value.Scan0 + ((j + sy) * w) * sizeof(int), row1.Length);
					Marshal.Copy(row0, 0, bits.Value.Scan0 + ((j + sy) * w + sx) * sizeof(int), row0.Length);
				}
			}
			return bmp;
		}

		/// <summary>A brush containing a checker board pattern</summary>
		public static TextureBrush CheckerBrush(int sx, int sy, uint X = 0xFFFFFFFFU, uint O = 0xFF000000U)
		{
			return new TextureBrush(BitmapChecker(sx, sy, X, O));
		}
	}
}
