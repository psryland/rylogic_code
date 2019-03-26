using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>Convert an unknown type to 'targetType' using a TypeDescriptor converter</summary>
	public class ToValue : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Util.ConvertTo(value, targetType);
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Convert icons to images</summary>
	public class IconToImageSource : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is Icon icon
				? Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
				: null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

	/// <summary>Display a Unit'T value as a string with units</summary>
	public class WithUnits : MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return null;

			// If 'value' is not an instance of 'Unit<>' just convert to string
			var ty = value.GetType();
			if (!ty.IsGenericType || ty.GetGenericTypeDefinition() != typeof(Unit<>))
				return value.ToString();

			// Find the method 'ToString(string fmt, bool include_units)'
			var to_string = ty.GetMethod(nameof(ToString), new[] { typeof(string), typeof(bool) });
			if (to_string == null)
				return value.ToString();

			// The parameter is the format string
			var fmt = (string)parameter ?? string.Empty;

			// Convert to string with units
			return (string)to_string.Invoke(value, new object[] { fmt, true });
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException($"MarkupExtension '{nameof(WithUnits)}' cannot convert any types back to '{targetType.Name}'");
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
