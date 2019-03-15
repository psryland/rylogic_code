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
}
