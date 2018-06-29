using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Rylogic.Gui2
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
}
