using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	/// <summary>Use a function to convert a value to something else</summary>
	public class Lambda :MarkupExtension, IValueConverter
	{
		/// <summary>Cache the lambda expression</summary>
		private string m_expr_string = string.Empty;
		private Func<object?, object>? m_expr = null;

		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// The parameter should be a lambda expression
			if (parameter is not string lambda_expr)
				return null;

			try
			{
				if (m_expr == null || m_expr_string != lambda_expr)
				{
					// Add the assemblies and namespaces needed to compile the expression
					var assemblies = AppDomain.CurrentDomain.GetAssemblies()
						.Where(x => x.IsDynamic == false)
						.Where(x => !string.IsNullOrEmpty(x.Location))
						.ToArray();
					var namespaces = new[]
					{
						"System",
						"System.Collections",
						"System.Drawing",
						"System.Globalization",
						"System.Linq",
						"System.Windows",
						"System.Windows.Controls",
						"Rylogic.Extn",
						"Rylogic.Gui.WPF",
						"Rylogic.Utility"
					};

					// Compile the lambda expression string into a lambda
					var options = ScriptOptions.Default.AddReferences(assemblies).AddImports(namespaces);
					var expr = CSharpScript.EvaluateAsync<Func<object?, object>>(lambda_expr, options).Result;

					// Cache the compiled expression
					m_expr = expr;
					m_expr_string = lambda_expr;
				}

				// Evaluate the expression
				return m_expr(value);
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Converter '{nameof(Lambda)}' threw an error: {ex.Message}");
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException($"MarkupExtension '{nameof(Lambda)}' cannot convert back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Select an object from a collection</summary>
	public class Select :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// The value should be an enumerable collection
			if (value is not IEnumerable collection)
				return null;

			// The parameter should be a lambda expression
			if (parameter is not string filter_desc)
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
			if (value == null)
				return "null";

			var ty = value.GetType();

			// Try for a description property
			if (ty.GetProperty("Description") is PropertyInfo pi)
			{
				if (pi.GetValue(value) is string desc && desc.Length != 0)
					return desc;
			}

			// If the type converts to a string
			if (value.ToString() is string str && str.Length != 0)
				return str;

			// Fall back to the type name
			return ty.Name;
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

	/// <summary>Convert bitmaps/icons to image sources</summary>
	public class ToImageSource : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Bitmap bmp)
			{
				return bmp.ToBitmapSource();
			}
			if (value is Icon icon)
			{
				return icon.ToBitmapSource();
			}
			return value;
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

	/// <summary>Returns visible if 'value' is a given type</summary>
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

	/// <summary>Returns 'Visible' if an element in the collection matches a predicate</summary>
	public class VisibleIfMatch :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// The binding should be an enumerable
			if (!(value is IEnumerable collection))
				return Visibility.Collapsed;

			if (!(parameter is string filter))
				return Visibility.Collapsed;

			try
			{
				var options = ScriptOptions.Default.AddReferences(
					Assembly.GetEntryAssembly(),
					Assembly.GetCallingAssembly(),
					Assembly.GetExecutingAssembly(),
					Assembly.GetAssembly(collection.GetType()));

				// Compile the filter string into a lambda
				var filter_func = CSharpScript.EvaluateAsync<Func<object?, bool>>(filter, options).Result;
				foreach (var item in collection)
				{
					if (!filter_func(item)) continue;
					return Visibility.Visible;
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"Converter '{nameof(VisibleIfMatch)}' threw an error: {ex.Message}");
			}

			// Not found
			return Visibility.Collapsed;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException($"MarkupExtension '{nameof(VisibleIfMatch)}' cannot convert visibility back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
