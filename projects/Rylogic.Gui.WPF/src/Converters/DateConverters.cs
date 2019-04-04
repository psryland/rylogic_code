using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Rylogic.Gui.WPF.Converters
{
	public class TimeSpanToDouble : MarkupExtension, IValueConverter
	{
		// Notes:
		//  - Convert a time span to a double in units of 'parameter'
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is TimeSpan ts)) return value;
			if (!(parameter is string s)) s = "s";
			switch (s)
			{
			default:
				throw new Exception($"Unknown time unit given for parameter: {s}");
			case "d":
			case "days":
				return ts.TotalDays;
			case "h":
			case "hrs":
			case "hours":
				return ts.TotalHours;
			case "m":
			case "min":
			case "minutes":
				return ts.TotalMinutes;
			case "s":
			case "sec":
			case "seconds":
				return ts.TotalSeconds;
			case "ms":
			case "msec":
			case "milliseconds":
				return ts.TotalMilliseconds;
			}
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is double ts)) return value;
			if (!(parameter is string s)) s = "s";
			switch (s)
			{
			default:
				throw new Exception($"Unknown time unit given for parameter: {s}");
			case "d":
			case "days":
				return TimeSpan.FromDays(ts);
			case "h":
			case "hrs":
			case "hours":
				return TimeSpan.FromHours(ts);
			case "m":
			case "min":
			case "minutes":
				return TimeSpan.FromMinutes(ts);
			case "s":
			case "sec":
			case "seconds":
				return TimeSpan.FromSeconds(ts);
			case "ms":
			case "msec":
			case "milliseconds":
				return TimeSpan.FromMilliseconds(ts);
			}
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
