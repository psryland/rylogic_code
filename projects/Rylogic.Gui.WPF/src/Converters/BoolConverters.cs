using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	/// <summary>Invert a boolean value</summary>
	[ValueConversion(typeof(bool), typeof(bool))]
	public class Not : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is bool b ? !b : value == null;
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return value is bool b ? !b : value == null;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Compares a value to the parameter, return true if they are equal</summary>
	[ValueConversion(typeof(object), typeof(bool))]
	public class IsEqual : MarkupExtension, IValueConverter
	{
		// Use:
		// <RadioButton Content = "Words" IsChecked="{Binding PropName, Converter={StaticResource IsEqual}, ConverterParameter={x:Static xml_ns:MyType+MyNestedEnum.Value}}"/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null && parameter == null)
				return true;
			if (value == null || parameter == null)
				return false;

			var ty = parameter.GetType();
			return Equals(Util.ConvertTo(value, ty), parameter);
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

	/// <summary>Compares a value to the parameter, return true if (value & parameter) != 0</summary>
	[ValueConversion(typeof(object), typeof(bool))]
	public class HasFlag : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null && parameter == null)
				return true;
			if (value == null || parameter == null)
				return false;

			var lhs = Util.ConvertTo<long>(value);
			var rhs = Util.ConvertTo<long>(parameter);
			return (lhs & rhs) != 0;
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

	/// <summary>TextWrapping if true. Use parameter="invert" for true == NoWrap</summary>
	public class BoolToWrap : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (parameter is string s && string.Compare(s, "invert", true) == 0) b = !b;
			return b ? TextWrapping.Wrap : TextWrapping.NoWrap;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TextWrapping wrap)) return null;
			var b = wrap == TextWrapping.Wrap;
			if (parameter is string s && string.Compare(s, "invert", true) == 0) b = !b;
			return b;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
