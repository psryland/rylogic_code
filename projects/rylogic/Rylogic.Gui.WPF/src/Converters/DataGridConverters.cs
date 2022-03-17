using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	/// <summary>Invert a boolean value</summary>
	[ValueConversion(typeof(bool), typeof(bool))]
	public class ToCellValue : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DataGridCell cell)
			{
				var val = cell.GetCellValue();
				return val != null ? Util.ConvertTo(val, targetType) : null;
			}

			return null;
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
}
