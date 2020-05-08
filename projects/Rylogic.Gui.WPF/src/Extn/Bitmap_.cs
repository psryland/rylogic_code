using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Rylogic.Gui.WPF
{
	public static class Bitmap_
	{
		/// <summary>Convert an icon to a bitmap source</summary>
		public static BitmapSource ToBitmapSource(this System.Drawing.Icon icon)
		{
			return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
		}

		/// <summary>Convert a bitmap to bitmap source</summary>
		public static BitmapImage ToBitmapSource(this System.Drawing.Bitmap bitmap) => ToBitmapSource(bitmap, System.Drawing.Imaging.ImageFormat.Png);
		public static BitmapImage ToBitmapSource(this System.Drawing.Bitmap bitmap, System.Drawing.Imaging.ImageFormat fmt)
		{
			using var memory = new MemoryStream();
			bitmap.Save(memory, fmt);
			memory.Position = 0;

			var bm = new BitmapImage();
			bm.BeginInit();
			bm.StreamSource = memory;
			bm.CacheOption = BitmapCacheOption.OnLoad;
			bm.EndInit();
			bm.Freeze();

			return bm;
		}
	}
}
