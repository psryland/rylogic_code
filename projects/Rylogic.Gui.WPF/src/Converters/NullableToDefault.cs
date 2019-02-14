using System;
using System.Globalization;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	public class NullableToDefault: IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return Activator.CreateInstance(targetType);
			return value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value;
		}
	}
}
