﻿using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is bool b ? !b : value == null;
		}
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null && parameter == null)
				return true;
			if (value == null || parameter == null)
				return false;

			var ty = parameter.GetType();
			return Equals(Util.ConvertTo(value, ty), parameter);
		}
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return parameter;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Compares a value to the parameter, return true or 'visible' if (value & parameter) != 0</summary>
	[ValueConversion(typeof(object), typeof(bool))]
	public class HasFlag : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(bool))
			{
				if (value == null && parameter == null)
					return true;
				if (value == null || parameter == null)
					return false;

				var lhs = Util.ConvertTo<long>(value);
				var rhs = Util.ConvertTo<long>(parameter);
				return (lhs & rhs) != 0;
			}
			if (targetType == typeof(Visibility))
			{
				if (value == null && parameter == null)
					return Visibility.Visible;
				if (value == null || parameter == null)
					return Visibility.Collapsed;

				var lhs = Util.ConvertTo<long>(value);
				var rhs = Util.ConvertTo<long>(parameter);
				return (lhs & rhs) != 0 ? Visibility.Visible : Visibility.Collapsed;
			}
			throw new Exception("Unsupported target type");
		}
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			// Cannot convert back to enum flag, because there isn't a way to set one bit on the bound variable.
			throw new Exception("Unsupported target type");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>True if all inputs are true</summary>
	public class AllTrue : MarkupExtension, IMultiValueConverter
	{
		public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.All(x => x is bool b && b == true);
		}
		public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			if (value is not bool b) return null;
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
		public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.Any(x => x is bool b && b == true);
		}
		public object[]? ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			if (value is not bool b) return null;
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
		// Notes:
		// Consider using BoolSelect
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not bool b) return null;
			return b ? Visibility.Visible : Visibility.Collapsed;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not Visibility v) return null;
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
		// Notes:
		// Consider using BoolSelect
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not bool b) return null;
			return b ? Visibility.Collapsed : Visibility.Visible;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not Visibility v) return null;
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (parameter is string s && string.Compare(s, "invert", true) == 0) b = !b;
			return b ? TextWrapping.Wrap : TextWrapping.NoWrap;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Allow 'value' to be non-boolean, where not-null == true
			if (value is not bool b) b = value != null;
			if (parameter is not string s) return null;

			try
			{
				var c = s.Split('|');
				if (c.Length != 2)
					throw new Exception($"{nameof(BoolSelect)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

				var val = b ? c[0] : c[1];

				// Special case conversions not handled by 'ConvertTo'
				if (targetType == typeof(Colour32))
				{
					return Colour32.Parse(val);
				}
				if (targetType == typeof(Color))
				{
					return Colour32.Parse(val).ToMediaColor();
				}
				if (targetType == typeof(Brush))
				{
					return Colour32.Parse(val).ToMediaBrush();
				}
				if (targetType == typeof(Thickness))
				{
					var i = (int?)Util.ConvertTo(val, typeof(int)) ?? 0;
					return new Thickness(i);
				}
				if (targetType == typeof(ImageSource))
				{
					// 'val' should be the name of a static resource. The problem is we don't have the control that
					// this binding converter is being used on, so we don't know which window to search for resources.
					// 'TryFindResource' only searches upwards through the parents, so really we need a pointer to the
					// FrameworkElement that the converter is being used on. So far, I don't know how to get that reference,
					// so instead, just search all the UserControls. A better solution is needed ...
					var res = Application.Current.TryFindResource(val); // Search App.xaml resources first.
					if (res == null)
					{
						foreach (var win in Application.Current.Windows.Cast<Window>())
						{
							res ??= win
								.AllVisualChildren().OfType<UserControl>()
								.Select(x => x.TryFindResource(val))
								.FirstOrDefault(x => x != null);
							if (res != null)
								break;
						}
					}
					return res;
					//// Use: ConverterParameter='image_key1|image_key2'
					//// Image keys should be static resource keys.
					//// Only works if the resources are in App.xml or the parent window.
					//var res = Application.Current.TryFindResource(val);
					//res ??= Application.Current.MainWindow.TryFindResource(val);
					//res ??= Application.Current.Windows.Cast<Window>().Select(x => x.TryFindResource(val)).FirstOrDefault(x => x != null);
					//return res;
				}
				return Util.ConvertTo(val, targetType);
			}
			catch
			{
				// Handle this silently so that runtime editing XML works
				return null;
			}
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2)
				throw new Exception($"{nameof(BoolToString)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? c[0] : c[1];
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !Colour32.TryParse(c[0], out var true_colour) || !Colour32.TryParse(c[1], out var false_colour))
				throw new Exception($"{nameof(BoolToColour)} parameter has the incorrect format. Expected '<true_colour>|<false_colour>'");

			var colour = b ? true_colour : false_colour;
			return colour.ToMediaColor();
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !Colour32.TryParse(c[0], out var true_colour) || !Colour32.TryParse(c[1], out var false_colour))
				throw new Exception($"{nameof(BoolToBrush)} parameter has the incorrect format. Expected '<true_colour>|<false_colour>'");

			var colour = b ? true_colour : false_colour;
			return new SolidColorBrush(colour.ToMediaColor());
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !double.TryParse(c[0], out var true_value) || !double.TryParse(c[1], out var false_value))
				throw new Exception($"{nameof(BoolToDouble)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? true_value : false_value;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool b)) return null;
			if (!(parameter is string s)) return null;

			var c = s.Split('|');
			if (c.Length != 2 || !int.TryParse(c[0], out var true_value) || !int.TryParse(c[1], out var false_value))
				throw new Exception($"{nameof(BoolToDouble)} parameter has the incorrect format. Expected '<true_value>|<false_value>'");

			return b ? true_value : false_value;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
