using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Graphix;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Windows32;

namespace Rylogic.Extn
{
	public static class Gfx_
	{
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

		/// <summary>Creates a bitmap containing a checker board where each 'checker' is 'sx' x 'sy'</summary>
		public static Image BitmapChecker(int sx, int sy, uint X = 0xFFFFFFFFU, uint O = 0x00000000U)
		{
			var w = 2*sx;
			var h = 2*sy;
			var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);
			var row0 = Array_.New(sx, unchecked((int)O));
			var row1 = Array_.New(sx, unchecked((int)X));
			using (var bits = bmp.LockBits(ImageLockMode.WriteOnly))
			{
				for (int j = 0; j != sy; ++j)
				{
					Marshal.Copy(row0, 0, bits.Value.Scan0 + ((j+ 0)*w   ) * sizeof(int), row0.Length);
					Marshal.Copy(row1, 0, bits.Value.Scan0 + ((j+ 0)*w+sx) * sizeof(int), row1.Length);
					Marshal.Copy(row1, 0, bits.Value.Scan0 + ((j+sy)*w   ) * sizeof(int), row1.Length);
					Marshal.Copy(row0, 0, bits.Value.Scan0 + ((j+sy)*w+sx) * sizeof(int), row0.Length);
				}
			}
			return bmp;
		}

		/// <summary>A brush containing a checker board pattern</summary>
		public static TextureBrush CheckerBrush(int sx, int sy, uint X = 0xFFFFFFFFU, uint O = 0xFF000000U)
		{
			return new TextureBrush(BitmapChecker(sx,sy,X,O));
		}
	}
}
