using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public static class Size_
	{
		/// <summary>Zero size</summary>
		public static Size Zero => new Size(0, 0);

		/// <summary>Infinite size</summary>
		public static Size Infinity => new Size(double.PositiveInfinity, double.PositiveInfinity);
	}

	public static class Rect_
	{
		/// <summary>Zero size</summary>
		public static Rect Zero => new Rect(0, 0, 0, 0);

		/// <summary>Construct from LTRB</summary>
		public static Rect FromLTRB(double left, double top, double right, double bottom)
		{
			return new Rect(left, top, right - left, bottom - top);
		}
		
		/// <summary>Return the centre position of the rectangle</summary>
		public static Point Centre(this Rect rect)
		{
			return new Point(rect.Left + rect.Width * 0.5, rect.Top + rect.Height * 0.5);
		}

		/// <summary>Left/Centre point</summary>
		public static Point LeftCentre(this Rect rect)
		{
			return new Point(rect.Left, (rect.Top + rect.Bottom) * 0.5);
		}

		/// <summary>Right/Centre point</summary>
		public static Point RightCentre(this Rect rect)
		{
			return new Point(rect.Right, (rect.Top + rect.Bottom) * 0.5);
		}

		/// <summary>Top/Centre point</summary>
		public static Point TopCentre(this Rect rect)
		{
			return new Point((rect.Left + rect.Right) * 0.5, rect.Top);
		}

		/// <summary>Bottom/Centre point</summary>
		public static Point BottomCentre(this Rect rect)
		{
			return new Point((rect.Left + rect.Right) * 0.5, rect.Bottom);
		}

		/// <summary>Reduces the size of this rectangle by excluding the area 'x'. The result must be a rectangle or an exception is thrown</summary>
		public static Rect Subtract(this Rect r, Rect x)
		{
			// If 'x' has no area, then subtraction is identity
			if (x.Width * x.Height <= 0)
				return r;

			// If the rectangles do not overlap. Right/Bottom is not considered 'in' the rectangle
			if (r.Left >= x.Right || r.Right <= x.Left || r.Top >= x.Bottom || r.Bottom <= x.Top)
				return r;

			// If 'x' completely covers 'r' then the result is empty
			if (x.Left <= r.Left && x.Right >= r.Right && x.Top <= r.Top && x.Bottom >= r.Bottom)
				return Rect.Empty;

			// If 'x' spans 'r' horizontally
			if (x.Left <= r.Left && x.Right >= r.Right)
			{
				// If the top edge of 'r' is aligned with the top edge of 'x', or within 'x'
				// then the top edge of the resulting rectangle is 'x.Bottom'.
				if (x.Top <= r.Top) return new Rect(r.Left, x.Bottom, r.Right - r.Left, r.Bottom - x.Bottom);
				if (x.Bottom >= r.Bottom) return new Rect(r.Left, r.Top, r.Right - r.Left, x.Top - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			// If 'x' spans 'r' vertically
			if (x.Top <= r.Top && x.Bottom >= r.Bottom)
			{
				if (x.Left <= r.Left) return new Rect(x.Right, r.Top, r.Right - x.Right, r.Bottom - r.Top);
				if (x.Right >= r.Right) return new Rect(r.Left, r.Top, x.Left - r.Left, r.Bottom - r.Top);
				throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
			}

			throw new Exception("The result of subtracting rectangle 'x' does not result in a rectangle");
		}
	}

	public static class Color_
	{
		/// <summary>Convert this colour to a media color</summary>
		public static Color ToMediaColor(this Colour32 col)
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
			return new Colour32((uint)col.ToArgb());
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
