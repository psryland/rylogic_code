using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	/// <summary>Convert an unknown type to 'targetType' using a TypeDescriptor converter</summary>
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

	/// <summary>Scale a value by a parameter</summary>
	public class ScaleConverter : IValueConverter
	{
		//MarkupExtension, 
		///// <summary>Access the singleton</summary>
		//public override object ProvideValue(IServiceProvider serviceProvider)
		//{
		//	return m_instance ?? (m_instance = new ScaleConverter());
		//}
		//private static ScaleConverter m_instance;

		/// <summary>Scale a value by the given parameter</summary>
		public object Convert(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
