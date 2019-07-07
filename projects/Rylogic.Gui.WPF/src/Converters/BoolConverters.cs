using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Rylogic.Extn;
using Rylogic.Gfx;
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

	/// <summary>Compare to a constant expression</summary>
	[ValueConversion(typeof(object), typeof(bool), ParameterType=typeof(string))]
	public class Compare :MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Expect parameter to have the form: "<=|>=|<|>|!=|== <constant value>"
			var expr = (string)parameter;
			if (expr == null)
				return Util.ConvertTo<double>(value) != 0;
			if (expr.StartsWith("=="))
				return Util.ConvertTo<double>(value) == double.Parse(expr.Substring(2));
			if (expr.StartsWith("!="))
				return Util.ConvertTo<double>(value) != double.Parse(expr.Substring(2));
			if (expr.StartsWith("<="))
				return Util.ConvertTo<double>(value) <= double.Parse(expr.Substring(2));
			if (expr.StartsWith(">="))
				return Util.ConvertTo<double>(value) >= double.Parse(expr.Substring(2));
			if (expr.StartsWith("<"))
				return Util.ConvertTo<double>(value) < double.Parse(expr.Substring(1));
			if (expr.StartsWith(">"))
				return Util.ConvertTo<double>(value) > double.Parse(expr.Substring(1));
			throw new Exception("Unknown comparison");
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
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

	/// <summary>Select between two values using a bool. Parameter should be 'true_value|false_value' where 'value' is a string that is convertable to 'targetType'</summary>
	public class BoolSelect : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2)
				throw new Exception($"{nameof(BoolSelect)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			var val = b ? c[0] : c[1];
			return Util.ConvertTo(val, targetType);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select between two string values using a bool. Parameter should be 'true_value|false_value' where 'value' is a string</summary>
	public class BoolToString : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2)
				throw new Exception($"{nameof(BoolToString)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? c[0] : c[1];
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select between colours using a bool. Parameter should be 'true_colour|false_colour' where colour is a parsable string like #RGB</summary>
	public class BoolToColour : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !Colour32.TryParse(c[0], out var true_colour) || !Colour32.TryParse(c[1], out var false_colour))
				throw new Exception($"{nameof(BoolToColour)} parameter has the incorrect format. Expected '<true_colour>|<false_colour>'");

			var colour = b ? true_colour : false_colour;
			return colour.ToMediaColor();
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select between colour brushes using a bool. Parameter should be 'true_colour|false_colour' where colour is a parsable string like #RGB</summary>
	public class BoolToBrush : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !Colour32.TryParse(c[0], out var true_colour) || !Colour32.TryParse(c[1], out var false_colour))
				throw new Exception($"{nameof(BoolToBrush)} parameter has the incorrect format. Expected '<true_colour>|<false_colour>'");

			var colour = b ? true_colour : false_colour;
			return new SolidColorBrush(colour.ToMediaColor());
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select between two double values using a bool. Parameter should be 'true_value|false_value' where 'value' is a parsable string like 0.123</summary>
	public class BoolToDouble : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !double.TryParse(c[0], out var true_value) || !double.TryParse(c[1], out var false_value))
				throw new Exception($"{nameof(BoolToDouble)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? true_value : false_value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select between two int values using a bool. Parameter should be 'true_value|false_value' where 'value' is a parsable string like 3</summary>
	public class BoolToInt : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !int.TryParse(c[0], out var true_value) || !int.TryParse(c[1], out var false_value))
				throw new Exception($"{nameof(BoolToDouble)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? true_value : false_value;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
