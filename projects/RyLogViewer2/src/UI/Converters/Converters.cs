using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RyLogViewer
{
	public class ByteToKByte : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is int)) return null;
			return ((int)value + Constants.OneKB - 1) / Constants.OneKB;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string)) return null;
			if (!int.TryParse((string)value, out var val)) return null;
			return val * Constants.OneKB;
		}
	}
	public class ByteToMByte : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is int)) return null;
			return ((int)value + Constants.OneMB - 1) / Constants.OneMB;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string)) return null;
			if (!int.TryParse((string)value, out var val)) return null;
			return val * Constants.OneMB;
		}
	}
	public class TabPageToInt : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is EOptionsPage)) return null;
			return (int)(EOptionsPage)value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return null;
			return (EOptionsPage)value;
		}
	}
	public class BoolToVisibility : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool)) return null;
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool)) return null;
			return (Visibility)value == Visibility.Visible;
		}
	}
}