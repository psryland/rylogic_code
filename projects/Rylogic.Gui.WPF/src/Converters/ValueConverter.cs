using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	public class ValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			try
			{
				// determine if the supplied value is of a suitable type
				var converter = TypeDescriptor.GetConverter(targetType);
				return converter.CanConvertFrom(value.GetType())
					? converter.ConvertFrom(value)
					: converter.ConvertFrom(value.ToString());
			}
			catch
			{
				return value;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
