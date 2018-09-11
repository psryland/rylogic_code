using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	public class BoolToVisibility : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			return b ? Visibility.Visible : Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is Visibility v)) return null;
			return v == Visibility.Visible;
		}
	}
}
