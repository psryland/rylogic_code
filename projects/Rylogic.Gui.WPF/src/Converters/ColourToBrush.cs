using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public class ColourToBrush : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color col)
			{
				return new SolidColorBrush(col);
			}
			if (value is uint u32)
			{
				var a = (byte)((u32 >> 24) & 0xFF);
				var r = (byte)((u32 >> 16) & 0xFF);
				var g = (byte)((u32 >>  8) & 0xFF);
				var b = (byte)((u32 >>  0) & 0xFF);
				return new SolidColorBrush(Color.FromArgb(a, r, g, b));
			}			
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is SolidColorBrush b) return b.Color;
			return null;
		}
	}
}
