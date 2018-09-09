using System;
using System.Drawing;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Rylogic.Gui.WPF
{
	public class ScaleConverter : MarkupExtension, IValueConverter
	{
		/// <summary>Access the singleton</summary>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return m_instance ?? (m_instance = new ScaleConverter());
		}
		private static ScaleConverter m_instance;

		/// <summary>Scale a value by the given parameter</summary>
		public object Convert(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>For binding enums to radio buttons</summary>
	public class RadioBtnConverter : IValueConverter
	{
		// Use:
		// <RadioButton Content = "Words" IsChecked="{Binding PropName, Converter={StaticResource RadioBtnConverter}, ConverterParameter={x:Static common:Enum.Value}}"/>

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Equals(value, parameter);
		}
		public object ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			return parameter;
		}
	}

	/// <summary>Convert icons to images</summary>
	public class IconToImageSourceConverter : IValueConverter
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
	}
}
