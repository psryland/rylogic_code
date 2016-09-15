using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using pr.gfx;
using pr.util;

namespace pr.extn
{
	public static class GfxExtensions
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
			gfx.DrawPath(pen, Gfx.RoundedRectanglePath(rect, radius));
		}

		/// <summary>Fill a rectangle with rounded corners</summary>
		public static void FillRectangleRounded(this Graphics gfx, Brush brush, RectangleF rect, float radius)
		{
			gfx.FillPath(brush, Gfx.RoundedRectanglePath(rect, radius));
		}
	}
}
