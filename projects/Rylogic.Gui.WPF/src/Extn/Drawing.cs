using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

	public static class Bitmap_
	{
		/// <summary>Convert an icon to a bitmap source</summary>
		public static BitmapSource ToBitmapSource(this System.Drawing.Icon icon)
		{
			return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		/// <summary>Convert a bitmap to bitmap source</summary>
		public static BitmapImage ToBitmapSource(this System.Drawing.Bitmap bitmap)
		{
			using (var memory = new MemoryStream())
			{
				bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
				memory.Position = 0;

				var bm = new BitmapImage();
				bm.BeginInit();
				bm.StreamSource = memory;
				bm.CacheOption = BitmapCacheOption.OnLoad;
				bm.EndInit();
				return bm;
			}
		}
	}

	public static class Typeface_
	{
		/// <summary>Convert a string to a font style</summary>
		public static FontStyle Style(string style)
		{
			switch (style)
			{
			default: throw new Exception($"Unknown font style: {style}");
			case nameof(FontStyles.Normal): return FontStyles.Normal;
			case nameof(FontStyles.Italic): return FontStyles.Italic;
			case nameof(FontStyles.Oblique): return FontStyles.Oblique;
			}
		}

		/// <summary>Convert a string to a font weight</summary>
		public static FontWeight Weight(string weight)
		{
			if (int.TryParse(weight, out var w))
				return FontWeight.FromOpenTypeWeight(w);

			switch (weight)
			{
			default: throw new Exception($"Unknown font style:{weight}");
			case nameof(FontWeights.Thin): return FontWeights.Thin;
			case nameof(FontWeights.ExtraLight): return FontWeights.ExtraLight;
			case nameof(FontWeights.UltraLight): return FontWeights.UltraLight;
			case nameof(FontWeights.Light): return FontWeights.Light;
			case nameof(FontWeights.Normal): return FontWeights.Normal;
			case nameof(FontWeights.Regular): return FontWeights.Regular;
			case nameof(FontWeights.Medium): return FontWeights.Medium;
			case nameof(FontWeights.DemiBold): return FontWeights.DemiBold;
			case nameof(FontWeights.SemiBold): return FontWeights.SemiBold;
			case nameof(FontWeights.Bold): return FontWeights.Bold;
			case nameof(FontWeights.ExtraBold): return FontWeights.ExtraBold;
			case nameof(FontWeights.UltraBold): return FontWeights.UltraBold;
			case nameof(FontWeights.Black): return FontWeights.Black;
			case nameof(FontWeights.Heavy): return FontWeights.Heavy;
			case nameof(FontWeights.ExtraBlack): return FontWeights.ExtraBlack;
			case nameof(FontWeights.UltraBlack): return FontWeights.UltraBlack;
			}
		}

		/// <summary>Convert a string to a font stretch</summary>
		public static FontStretch Stretches(string stretch)
		{
			switch (stretch)
			{
			default: throw new Exception();
			case nameof(FontStretches.UltraCondensed): return FontStretches.UltraCondensed;
			case nameof(FontStretches.ExtraCondensed): return FontStretches.ExtraCondensed;
			case nameof(FontStretches.Condensed): return FontStretches.Condensed;
			case nameof(FontStretches.SemiCondensed): return FontStretches.SemiCondensed;
			case nameof(FontStretches.Normal): return FontStretches.Normal;
			case nameof(FontStretches.Medium): return FontStretches.Medium;
			case nameof(FontStretches.SemiExpanded): return FontStretches.SemiExpanded;
			case nameof(FontStretches.Expanded): return FontStretches.Expanded;
			case nameof(FontStretches.ExtraExpanded): return FontStretches.ExtraExpanded;
			case nameof(FontStretches.UltraExpanded): return FontStretches.UltraExpanded;
			}
		}
	}
}
