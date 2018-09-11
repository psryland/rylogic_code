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
			if (!(value is Color col)) return null;
			return new SolidColorBrush(col);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is SolidColorBrush b)) return null;
			return b.Color;
		}
	}
}
