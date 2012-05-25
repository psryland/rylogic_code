//***************************************************
// Colour128
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System.Drawing;
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
	}
}
