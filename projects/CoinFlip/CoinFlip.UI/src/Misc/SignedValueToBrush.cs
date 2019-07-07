using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public class SignedValueToBrush :MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var num = Util.ConvertTo<double>(value);
			return new SolidColorBrush(num >= 0 ? Colors.Black : Colors.Red);
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
