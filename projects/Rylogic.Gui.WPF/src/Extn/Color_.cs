using System;
using System.Globalization;
using System.Windows.Media;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public static class Color_
	{
		/// <summary>Parse a color from a string. #RGB, #RRGGBB, #ARGB, #AARRGGBB</summary>
		public static Color From(string s)
		{
			byte a, r, g, b;

			// Accept #AARRGGBB
			if (s.Length == 9 && s[0] == '#' &&
				uint.TryParse(s.Substring(1, 8), NumberStyles.HexNumber, null, out var aarrggbb))
			{
				a = (byte)((aarrggbb >> 24) & 0xFF);
				r = (byte)((aarrggbb >> 16) & 0xFF);
				g = (byte)((aarrggbb >>  8) & 0xFF);
				b = (byte)((aarrggbb >>  0) & 0xFF);
				return Color.FromArgb(a,r,g,b);
			}

			// Accept #RRGGBB
			if (s.Length == 7 && s[0] == '#' &&
				byte.TryParse(s.Substring(1, 2), NumberStyles.HexNumber, null, out r) &&
				byte.TryParse(s.Substring(3, 2), NumberStyles.HexNumber, null, out g) &&
				byte.TryParse(s.Substring(5, 2), NumberStyles.HexNumber, null, out b))
			{
				return Color.FromArgb(0xFF, r, g, b);
			}

			// Accept #ARGB
			if (s.Length == 5 && s[0] == '#' &&
				byte.TryParse(s.Substring(1, 1), NumberStyles.HexNumber, null, out a) &&
				byte.TryParse(s.Substring(2, 1), NumberStyles.HexNumber, null, out r) &&
				byte.TryParse(s.Substring(3, 1), NumberStyles.HexNumber, null, out g) &&
				byte.TryParse(s.Substring(4, 1), NumberStyles.HexNumber, null, out b))
			{
				// 17 because 17 * 15 = 255
				return Color.FromArgb((byte)(17 * a), (byte)(17 * r), (byte)(17 * g), (byte)(17 * b));
			}

			// Accept #RGB
			if (s.Length == 4 && s[0] == '#' &&
				byte.TryParse(s.Substring(1, 1), NumberStyles.HexNumber, null, out r) &&
				byte.TryParse(s.Substring(2, 1), NumberStyles.HexNumber, null, out g) &&
				byte.TryParse(s.Substring(3, 1), NumberStyles.HexNumber, null, out b))
			{
				// 17 because 17 * 15 = 255
				return Color.FromArgb(0xFF, (byte)(17 * r), (byte)(17 * g), (byte)(17 * b));
			}

			throw new FormatException($"Colour string {s} is invalid");
		}

		/// <summary>Convert this colour to a media color</summary>
		public static Color ToMediaColor(this Colour32 col)
		{
			return Color.FromArgb(col.A, col.R, col.G, col.B);
		}
		public static Color ToMediaColor(this System.Drawing.Color col)
		{
			return Color.FromArgb(col.A, col.R, col.G, col.B);
		}

		/// <summary>Convert this colour to a media solid colour brush</summary>
		public static SolidColorBrush ToMediaBrush(this Colour32 col)
		{
			return new SolidColorBrush(col.ToMediaColor());
		}
		public static SolidColorBrush ToMediaBrush(this System.Drawing.Color col)
		{
			return new SolidColorBrush(col.ToMediaColor());
		}

		/// <summary>Convert this media color to an ARGB value</summary>
		public static int ToArgb(this Color col)
		{
			return ((int)col.A << 24) | ((int)col.R << 16) | ((int)col.G << 8) | ((int)col.B << 0);
		}
		public static uint ToArgbU(this Color col)
		{
			return ((uint)col.A << 24) | ((uint)col.R << 16) | ((uint)col.G << 8) | ((uint)col.B << 0);
		}

		/// <summary>Convert this media color to a system drawing color</summary>
		public static System.Drawing.Color ToColor(this Color col)
		{
			return System.Drawing.Color.FromArgb(col.ToArgb());
		}

		/// <summary>Convert this media color to a Rylogic Colour32</summary>
		public static Colour32 ToColour32(this Color col)
		{
			return new Colour32(col.ToArgbU());
		}

		/// <summary>Create a colour by modifying an existing color</summary>
		public static Color Modify(this Color color, byte? a = null, byte? r = null, byte? g = null, byte? b = null)
		{
			return Color.FromArgb(
				a ?? color.A,
				r ?? color.R,
				g ?? color.G,
				b ?? color.B);
		}
	}
}
