using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rylogic.Gui.WPF
{
	/// <summary>If the value is null, return a default instance of 'targetType'</summary>
	public class NullableToDefault : IValueConverter
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

	/// <summary>If the value is null, return Visible</summary>
	public class NullToVisible : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (value is string s && s.Length == 0) ? Visibility.Visible : Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>If the value is null, return Collapsed</summary>
	public class NullToCollapsed : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null || (value is string s && s.Length == 0) ? Visibility.Collapsed : Visibility.Visible;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
