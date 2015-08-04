using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using pr.common;
using pr.gfx;
using pr.util;

namespace pr.extn
{
	public static class GfxExtensions
	{
		public class GfxContainerScope :Scope
		{
			public GraphicsContainer GfxContainer;
		}

		/// <summary>Save the transform and clip state in an RAII object</summary>
		public static GfxContainerScope StateScope(this Graphics gfx)
		{
			return Scope.Create<GfxContainerScope>(
				s => s.GfxContainer = gfx.BeginContainer(),
				s => gfx.EndContainer(s.GfxContainer));
		}

		/// <summary>Get the HDC associated with this graphics object. Releases in dispose</summary>
		public static IntPtrScope GetHdcScope(this Graphics gfx)
		{
			return Scope.Create<IntPtrScope>(
				s => s.Ptr = gfx.GetHdc(),
				s => gfx.ReleaseHdc(s.Ptr));
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
		public static void DrawRectangleRounded(this Graphics gfx, Pen pen, Rectangle rect, float radius)
		{
			gfx.DrawPath(pen, Gfx.RoundedRectanglePath(rect, radius));
		}

		/// <summary>Fill a rectangle with rounded corners</summary>
		public static void FillRectangleRounded(this Graphics gfx, Brush brush, Rectangle rect, float radius)
		{
			gfx.FillPath(brush, Gfx.RoundedRectanglePath(rect, radius));
		}
	}
}
