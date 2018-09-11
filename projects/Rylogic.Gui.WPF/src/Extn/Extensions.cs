using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rylogic.Gui.WPF
{
	public static class Extensions
	{
		/// <summary>Return a typeface derived from this controls font</summary>
		public static Typeface Typeface(this Control ui)
		{
			return new Typeface(ui.FontFamily, ui.FontStyle, ui.FontWeight, ui.FontStretch);
		}

		/// <summary>Convert text to formatted text based on this control's font settings</summary>
		public static FormattedText ToFormattedText(this Control ui, string text, double emSize = 12.0, Brush brush = null)
		{
			var tf = ui.Typeface();
			return new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, emSize, brush ?? Brushes.Black, 96.0);
		}

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

		/// <summary>Create a colour by modifying an existing color</summary>
		public static Color Modify(this Color color, byte? a = null, byte? r = null, byte? g = null, byte? b = null)
		{
			return Color.FromArgb(
				a ?? color.A,
				r ?? color.R,
				g ?? color.G,
				b ?? color.B);
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
	}
}
