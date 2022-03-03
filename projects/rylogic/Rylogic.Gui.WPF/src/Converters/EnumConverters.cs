using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	// Notes:
	//  - [Flags] enum => use 'HasFlag' in BoolConverters

	public class EnumToDesc : MarkupExtension, IValueConverter
	{
		// Notes:
		//  - Convert an enum to its description attribute
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is IEnumerable collection)
				return collection.Cast<object>().OfType<Enum>().Select(x => x.Desc());
			if (value is Enum en)
				return en.Desc();
			return null;
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
	public class EnumValues : MarkupExtension, IValueConverter
	{
		// Use:
		//  <ComboBox
		//     ItemsSource="{Binding MyProp, Converter={conv:EnumValues}, Mode=OneTime}"
		//     ItemTemplate= "{conv:EnumValues+ToDesc}"  <-- Remove this if your enum doesn't use [Desc()] attributes
		//     SelectedItem="{Binding MyProp}"
		//     />
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				throw new ArgumentNullException();
			if (!value.GetType().IsEnum)
				throw new ArgumentException("Expected an enum property");

			var values = Enum.GetValues(value.GetType());
			return values;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}

		/// <summary>Helper for mapping enum values to their description</summary>
		public class ToDesc : MarkupExtension
		{
			public override object ProvideValue(IServiceProvider serviceProvider)
			{
				var dt = new DataTemplate();
				dt.VisualTree = new FrameworkElementFactory(typeof(TextBlock));
				dt.VisualTree.SetValue(TextBlock.TextProperty, new Binding(".") { Converter = new EnumToDesc() });
				return dt;
			}
		}
	}

	/// <summary>Select between options based on an enum.</summary>
	public class EnumSelect :MarkupExtension, IValueConverter
	{
		// Notes:
		//  - Parameter should be 'case0:value0|case1:value1|case2:value2|default_value'
		//    where 'value' is a string that is convertable to 'targetType'.
		//  - Any value with no 'case' is assumed to be the default value.
		//    The default case should always be last

		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not Enum e)
				return null;
			if (parameter is not string s)
				return null;

			try
			{
				// Look for a matching case
				foreach (var pair in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
				{
					var case_value = pair.Split(':');

					// Default case?
					if (case_value.Length == 1)
						return InterpretValue(case_value[0]);

					// Valid 'case:value' pair?
					if (case_value.Length != 2)
						continue;

					// Case name is not a member of the enum
					if (!Enum.IsDefined(value.GetType(), case_value[0]))
						continue;

					// Case matches 'value'
					var cas = (Enum)Enum.Parse(value.GetType(), case_value[0]);
					var is_flags = e.GetType().HasAttribute<FlagsAttribute>();
					var is_match = is_flags ? e.HasFlag(cas) : Equals(cas, value);
					if (!is_match)
						continue;

					// Interpret the value to the target type
					return InterpretValue(case_value[1]);
				}
				return null;
			}
			catch
			{
				// Handle this silently so that runtime editing XML works
				return null;
			}

			// Convert the string to the target type
			object? InterpretValue(string val)
			{
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
				}
				return Util.ConvertTo(val, targetType);
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

	/// <summary>Convert between int values in enum values</summary>
	public class EnumToInt :MarkupExtension, IValueConverter
	{
		// Use:
		//  <ComboBox
		//     SelectedIndex="{Binding MyEnumValue, Converter={conv:EnumToInt}}"
		//     />
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is not Enum en)
				throw new ArgumentException("Expected an enum property");

			return Util.ConvertTo(value, typeof(int));
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!Enum.IsDefined(targetType, value))
				throw new ArgumentException("Expected an enum property");

			return Util.ConvertTo(value, targetType);
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
