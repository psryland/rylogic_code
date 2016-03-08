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
	}
}
