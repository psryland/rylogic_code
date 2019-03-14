using System;
using System.Globalization;
using System.Windows.Data;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>Convert an unknown type to 'targetType' using a TypeDescriptor converter</summary>
	public class ValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
	}

	/// <summary>Scale a value by a parameter</summary>
	public class ScaleConverter : IValueConverter
	{
		/// <summary>Scale a value by the given parameter</summary>
		public object Convert(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter);
		}
	}
}
