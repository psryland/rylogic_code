//***************************************************
// Drawing Extensions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System.Drawing;
using Rylogic.Graphix;
using Rylogic.Maths;

namespace Rylogic.Extn
{
	/// <summary>Color extensions</summary>
	public static class Color_
	{
		/// <summary>Create a colour from a uint</summary>
		public static Color FromArgb(uint argb)
		{
			return Color.FromArgb(
				(int)((argb >> 24) & 0xFF),
				(int)((argb >> 16) & 0xFF),
				(int)((argb >>  8) & 0xFF),
				(int)((argb >>  0) & 0xFF));
		}

		/// <summary>Lighten this colour, towards white</summary>
		public static Color Lighten(this Color col, float t, bool alpha_too = false)
		{
			var c = (Colour32)col;
			c = c.Lighten(t, alpha_too);
			return c.ToColor();
		}

		/// <summary>Darken this colour, towards black</summary>
		public static Color Darken(this Color col, float t, bool alpha_too = false)
		{
			var c = (Colour32)col;
			c = c.Darken(t, alpha_too);
			return c.ToColor();
		}

		/// <summary>Linearly interpolate from this colour to 'dst' by 'frac'</summary>
		public static Color Lerp(this Color src, Color dst, float frac)
		{
			return Color.FromArgb(
				(int)Math_.Clamp(src.A * (1f - frac) + dst.A * frac, 0f, 255f),
				(int)Math_.Clamp(src.R * (1f - frac) + dst.R * frac, 0f, 255f),
				(int)Math_.Clamp(src.G * (1f - frac) + dst.G * frac, 0f, 255f),
				(int)Math_.Clamp(src.B * (1f - frac) + dst.B * frac, 0f, 255f));
		}

		/// <summary>Return this colour with the alpha value changed to 'alpha'</summary>
		public static Color Alpha(this Color col, int alpha)
		{
			return Color.FromArgb(alpha, col);
		}
		public static Color Alpha(this Color col, float alpha)
		{
			return Color.FromArgb((int)(255.9999f * alpha), col);
		}

		/// <summary>Convert this colour to ABGR</summary>
		public static int ToAbgr(this Color col)
		{
			return unchecked((int)(((uint)col.A << 24) | ((uint)col.B << 16) | ((uint)col.G << 8) | ((uint)col.R << 0)));
		}

		/// <summary>Gets the 32-bit ARGB values of this Color structure as an unsigned int</summary>
		public static uint ToArgbU(this Color col)
		{
			return unchecked((uint)col.ToArgb());
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

		/// <summary>Convert the colour to a vector. 'xyz' = 'rgb', 'w' = 'alpha'. (1,1,1,1) is white, (0,0,0,0) is transparent black</summary>
		public static v4 ToV4(this Color col)
		{
			return new v4(col.R, col.G, col.B, col.A) / 255f;
		}
	}
}
