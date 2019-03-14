using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF
{
	/// <summary>Compares a value to the parameter, return true if they are equal</summary>
	public class IsEqual : IValueConverter
	{
		// Use:
		// <RadioButton Content = "Words" IsChecked="{Binding PropName, Converter={StaticResource IsEqual}, ConverterParameter={x:Static xml_ns:MyType+MyNestedEnum.Value}}"/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Equals(value, parameter);
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return parameter;
		}
	}

	/// <summary>True if all inputs are true</summary>
	public class AllTrue : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.All(x => x is bool b && b == true);
		}
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			return Array_.New<object>(targetTypes.Length, i => b);
		}
	}

	/// <summary>True if any inputs are true</summary>
	public class AnyTrue : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.Any(x => x is bool b && b == true);
		}
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			return Array_.New<object>(targetTypes.Length, i => b);
		}
	}
	
	/// <summary>Visible if true</summary>
	public class BoolToVisible : IValueConverter
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

	/// <summary>Collapsed if true</summary>
	public class BoolToCollapsed : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			return b ? Visibility.Collapsed : Visibility.Visible;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is Visibility v)) return null;
			return v == Visibility.Collapsed;
		}
	}
}
