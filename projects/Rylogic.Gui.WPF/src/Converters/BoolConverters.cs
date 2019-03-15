using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>Compares a value to the parameter, return true if they are equal</summary>
	public class IsEqual : MarkupExtension, IValueConverter
	{
		// Use:
		// <RadioButton Content = "Words" IsChecked="{Binding PropName, Converter={StaticResource IsEqual}, ConverterParameter={x:Static xml_ns:MyType+MyNestedEnum.Value}}"/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null && parameter == null) return true;
			if (value != null && parameter != null) return Equals(value, Util.ConvertTo(parameter, value.GetType()));
			return false;
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return parameter;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>True if all inputs are true</summary>
	public class AllTrue : MarkupExtension, IMultiValueConverter
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
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>True if any inputs are true</summary>
	public class AnyTrue : MarkupExtension, IMultiValueConverter
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
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Visible if true</summary>
	public class BoolToVisible : MarkupExtension, IValueConverter
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
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Collapsed if true</summary>
	public class BoolToCollapsed : MarkupExtension, IValueConverter
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
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
