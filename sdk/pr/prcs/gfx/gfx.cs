//***************************************************
// Colour128
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace pr.gfx
{
	/// <summary>Static functions related to graphics</summary>
	public static class Gfx
	{
		/// <summary>Linearly interpolate two colours</summary>
		public static Color Blend(Color c0, Color c1, float t)
		{
			return Color.FromArgb(
				(int)(c0.A*(1f - t) + c1.A*t),
				(int)(c0.R*(1f - t) + c1.R*t),
				(int)(c0.G*(1f - t) + c1.G*t),
				(int)(c0.B*(1f - t) + c1.B*t));
		}

		/// <summary>Return all colors satisfying a predicate</summary>
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

		/// <summary>Useful overloads for DrawImage</summary>
		public static void DrawImage(this Graphics gfx, Image image, Rectangle dst_rect, Rectangle src_rect, GraphicsUnit unit, ImageAttributes attr)
		{
			gfx.DrawImage(image, dst_rect, src_rect.X, src_rect.Y, src_rect.Width, src_rect.Height, unit, attr);
		}
		public static void DrawImage(this Graphics gfx, Image image, int X, int Y, Rectangle src_rect, GraphicsUnit unit, ImageAttributes attr)
		{
			gfx.DrawImage(image, new Rectangle(X, Y, src_rect.Width, src_rect.Height), src_rect, unit, attr);
		}
		
		/// <summary>Create a rounded rectangle path that can be filled or drawn</summary>
		public static GraphicsPath RoundedRectanglePath(Rectangle rect, float radius)
		{
			GraphicsPath gp = new GraphicsPath();
			float d = radius * 2f;
			gp.AddArc (rect.X                  ,rect.Y                   ,d      ,           d, 180 ,90);
			gp.AddArc (rect.X + rect.Width - d ,rect.Y                   ,d      ,           d, 270 ,90);
			gp.AddArc (rect.X + rect.Width - d ,rect.Y + rect.Height - d ,d      ,           d,   0 ,90);
			gp.AddArc (rect.X                  ,rect.Y + rect.Height - d ,d      ,           d,  90 ,90);
			gp.AddLine(rect.X                  ,rect.Y + rect.Height - d ,rect.X ,rect.Y + d/2);
			return gp;
		}
		
		/// <summary>Draws a rectangle with rounded corners</summary>
		public static void DrawRectangleRounded(this Graphics gfx, Pen pen, Rectangle rect, float radius)
		{
			gfx.DrawPath(pen, RoundedRectanglePath(rect, radius));
		}
		
		/// <summary>Fill a rectangle with rounded corners</summary>
		public static void FillRectangleRounded(this Graphics gfx, Brush brush, Rectangle rect, float radius)
		{
			gfx.FillPath(brush, RoundedRectanglePath(rect, radius));
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
	}
}
