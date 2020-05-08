using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	/// <summary>Select on object from a collection</summary>
	public class Select :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// The value should be an enumerable collection
			if (!(value is IEnumerable collection))
				return null;

			// The parameter should be a lambda expression
			if (!(parameter is string filter_desc))
				return null;

			try
			{
				var options = ScriptOptions.Default.AddReferences(
					Assembly.GetEntryAssembly(),
					Assembly.GetCallingAssembly(),
					Assembly.GetExecutingAssembly(),
					Assembly.GetAssembly(collection.GetType()));

				// Compile the filter string into a lambda
				var filter = CSharpScript.EvaluateAsync<Func<object?, bool>>(filter_desc, options).Result;
				foreach (var item in collection)
				{
					if (!filter(item)) continue;
					return item;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Converter '{nameof(Select)}' threw an error: {ex.Message}");
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException($"MarkupExtension '{nameof(Select)}' cannot convert back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Convert an unknown type to 'targetType' using a TypeDescriptor converter</summary>
	public class ConvertTo : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Convert an unknown type to a string</summary>
	public class ToString :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value?.ToString() ?? "null";
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

	/// <summary>Convert icons to images</summary>
	public class IconToImageSource : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is Icon icon
				? Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
				: null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Display a Unit'T value as a string with units. 'parameter' is the format string</summary>
	public class StringFormat :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			// Get the type of 'value'
			var ty = value.GetType();
			if (ty == null)
				return value.ToString();

			// The parameter is the format string
			var fmt = (string)parameter;
			MethodInfo? mi;

			// Specialty types first
			if (ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(Unit<>))
			{
				// Find the method 'ToString(int sd, bool include_units)'
				if (fmt.TryConvertTo<int,bool>(out var p0) && (mi = ty.GetMethod(nameof(ToString), new[] { typeof(int), typeof(bool) })) != null)
					return (string?)mi.Invoke(value, new object[] { p0.Item1, p0.Item2 });

				// Find the method 'ToString(string fmt, bool include_units)'
				if (fmt.TryConvertTo<string,bool>(out var p1) && (mi = ty.GetMethod(nameof(ToString), new[] { typeof(string), typeof(bool) })) != null)
					return (string?)mi.Invoke(value, new object[] { p0.Item1, p0.Item2 });
			}

			// Find the method 'ToString(int sd)'
			if (int.TryParse(fmt, out var sd))
			{
				if ((mi = ty.GetMethod(nameof(ToString), new[] { typeof(int) })) != null)
					return (string?)mi.Invoke(value, new object[] { sd });
				if (value is float f)
					return float_.ToString(f, sd);
				if (value is double d)
					return double_.ToString(d, sd);
				if (value is decimal m)
					return decimal_.ToString(m, sd);
			}

			// Find the method 'ToString(string fmt)'
			if (fmt != null && (mi = ty.GetMethod(nameof(ToString), new[] { typeof(string) })) != null)
				return (string?)mi.Invoke(value, new object[] { fmt });

			return value.ToString();
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException($"MarkupExtension '{nameof(StringFormat)}' cannot convert any types back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Display a Unit'T value as a string with units. 'parameter' is the format string</summary>
	public class VisibleIfType :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return Visibility.Collapsed;

			var ty = value.GetType();
			if (ty == null)
				return Visibility.Collapsed;

			if (parameter is string type_name)
			{
				// Get the type of 'value'
				var full_name = ty.FullName ?? string.Empty;
				return full_name.EndsWith(type_name) ? Visibility.Visible : Visibility.Collapsed;
			}
			if (parameter is Type type)
			{
				return ty == type ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException($"MarkupExtension '{nameof(VisibleIfType)}' cannot convert visibility back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Returns 'Visible' if 'parameter' is found within a collection</summary>
	public class VisibleIfContains :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// The binding should be an enumerable
			if (!(value is IEnumerable collection))
				return Visibility.Collapsed;

			// Look for 'parameter' in the collection
			foreach (var item in collection)
				if (Equals(item, parameter))
					return Visibility.Visible;

			// Not found
			return Visibility.Collapsed;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException($"MarkupExtension '{nameof(VisibleIfContains)}' cannot convert visibility back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	///// <summary>Display a Unit'T value as a string with units. 'parameter' is the format string</summary>
	//public class WithUnits : MarkupExtension, IValueConverter
	//{
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		if (value == null)
	//			return null;

	//		// The parameter is the format string
	//		var fmt = (string)parameter ?? string.Empty;

	//		// If 'value' is not an instance of 'Unit<>' just convert to string
	//		var ty = value.GetType();
	//		if (!ty.IsGenericType || ty.GetGenericTypeDefinition() != typeof(Unit<>))
	//			return value.ToString();

	//		// Find the method 'ToString(string fmt, bool include_units)'
	//		var to_string = ty.GetMethod(nameof(ToString), new[] { typeof(string), typeof(bool) });
	//		if (to_string == null)
	//			return value.ToString();

	//		// Convert to string with units
	//		return (string)to_string.Invoke(value, new object[] { fmt, true });
	//	}
	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		throw new NotImplementedException($"MarkupExtension '{nameof(WithUnits)}' cannot convert any types back to '{targetType.Name}'");
	//	}
	//	public override object ProvideValue(IServiceProvider serviceProvider)
	//	{
	//		return this;
	//	}
	//}

	///// <summary>Display a Unit'T value as a string without units. 'parameter' is the format string</summary>
	//public class WithoutUnits :MarkupExtension, IValueConverter
	//{
	//	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		if (value == null)
	//			return null;

	//		// The parameter is the format string
	//		var fmt = (string)parameter ?? string.Empty;

	//		// If 'value' is not an instance of 'Unit<>' just convert to string
	//		var ty = value.GetType();
	//		if (!ty.IsGenericType || ty.GetGenericTypeDefinition() != typeof(Unit<>))
	//			return value.ToString();

	//		// Find the method 'ToString(string fmt, bool include_units)'
	//		var to_string = ty.GetMethod(nameof(ToString), new[] { typeof(string), typeof(bool) });
	//		if (to_string == null)
	//			return value.ToString();

	//		// Convert to string with units
	//		return (string)to_string.Invoke(value, new object[] { fmt, false });
	//	}
	//	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
	//	{
	//		throw new NotImplementedException($"MarkupExtension '{nameof(WithUnits)}' cannot convert any types back to '{targetType.Name}'");
	//	}
	//	public override object ProvideValue(IServiceProvider serviceProvider)
	//	{
	//		return this;
	//	}
	//}
}
