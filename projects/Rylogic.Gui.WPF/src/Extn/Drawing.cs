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
	public static class Rect_
	{
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
		public static Color ToMediaColor(this Colour32 col)
		{
			return Color.FromArgb(col.A, col.R, col.G, col.B);
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
}
