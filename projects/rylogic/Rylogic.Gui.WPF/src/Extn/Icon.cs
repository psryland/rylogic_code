using System;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public static class Icon_
	{
		/// <summary>Reads a given image resource into a WinForms icon.</summary>
		public static System.Drawing.Icon ToIcon(this ImageSource image_source)
		{
			if (image_source == null)
				return null!;

			var uri = new Uri(image_source.ToString());
			var stream = Application.GetResourceStream(uri)?.Stream ?? throw new ArgumentException($"The supplied image source '{image_source}' could not be resolved.");
			var hicon = new System.Drawing.Bitmap(stream).GetHicon();
			return System.Drawing.Icon.FromHandle(hicon);
		}
	}
}
