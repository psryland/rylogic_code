using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.Converters
{
	public class ToDateTime :MarkupExtension, IValueConverter
	{
		// Notes:
		// - Convert a DateTimeOffset to a DateTime

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTimeOffset dto)) return value;
			if (parameter is string s)
			{
				switch (s.ToLowerInvariant())
				{
				case "utc": dto = dto.ToUniversalTime(); break;
				case "local": dto = dto.ToLocalTime(); break;
				}
			}
			return
				targetType == typeof(DateTime?) ? (DateTime?)dto.DateTime :
				(object)dto.DateTime;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DateTime dt)) return value;
			if (dt.Kind == DateTimeKind.Unspecified && parameter is string s)
			{
				if (s.ToLowerInvariant() == "utc") dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
				if (s.ToLowerInvariant() == "local") dt = DateTime.SpecifyKind(dt, DateTimeKind.Local);
			}
			if (dt.Kind == DateTimeKind.Unspecified)
			{
				throw new Exception("DateTime.Kind is unknown. Specify 'utc' or 'local' as a converter parameter");
			}
			return
				targetType == typeof(DateTimeOffset?) ? (DateTimeOffset?)new DateTimeOffset(dt) :
				(object)new DateTimeOffset(dt);
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class ToPrettyString :MarkupExtension, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DateTimeOffset dto)
			{
				return parameter is string s
					? dto.ToString(s)
					: dto.ToString("yyyy-MM-dd HH:mm:ss");
			}
			if (value is double db)
			{
				return (parameter is string s
					? s switch
					{
						"d"            => TimeSpan.FromDays(db),
						"days"         => TimeSpan.FromDays(db),
						"h"            => TimeSpan.FromHours(db),
						"hrs"          => TimeSpan.FromHours(db),
						"hours"        => TimeSpan.FromHours(db),
						"m"            => TimeSpan.FromMinutes(db),
						"min"          => TimeSpan.FromMinutes(db),
						"minutes"      => TimeSpan.FromMinutes(db),
						"s"            => TimeSpan.FromSeconds(db),
						"sec"          => TimeSpan.FromSeconds(db),
						"seconds"      => TimeSpan.FromSeconds(db),
						"ms"           => TimeSpan.FromMilliseconds(db),
						"msec"         => TimeSpan.FromMilliseconds(db),
						"milliseconds" => TimeSpan.FromMilliseconds(db),
						_              => throw new Exception($"Unknown time unit given for parameter: {s}"),
					}
					: TimeSpan.FromMilliseconds(db))
					.ToPrettyString();
			}
			if (value is TimeSpan ts)
			{
				var short_format = parameter is bool sf ? sf : true;
				return ts.ToPrettyString(short_format);
			}
			else
			{
				return string.Empty;
			}
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(DateTimeOffset))
			{
				if (value is string s)
				{
					if (parameter is string fmt)
					{
						return DateTimeOffset.TryParseExact(s, fmt, null, DateTimeStyles.AssumeLocal|DateTimeStyles.AllowWhiteSpaces, out var dto1)
							? dto1 : null!;
					}
					if (DateTimeOffset.TryParse(s, out var dto2))
					{
						return dto2;
					}
					return null!;
				}
				if (value is long ticks)
				{
					return new DateTimeOffset(ticks, TimeSpan.Zero);
				}
			}
			if (targetType == typeof(TimeSpan))
			{
				if (value is double d && parameter is string fmt)
				{
					return fmt switch
					{
						"d" => TimeSpan.FromDays(d),
						"days" => TimeSpan.FromDays(d),
						"h" => TimeSpan.FromHours(d),
						"hrs" => TimeSpan.FromHours(d),
						"hours" => TimeSpan.FromHours(d),
						"m" => TimeSpan.FromMinutes(d),
						"min" => TimeSpan.FromMinutes(d),
						"minutes" => TimeSpan.FromMinutes(d),
						"s" => TimeSpan.FromSeconds(d),
						"sec" => TimeSpan.FromSeconds(d),
						"seconds" => TimeSpan.FromSeconds(d),
						"ms" => TimeSpan.FromMilliseconds(d),
						"msec" => TimeSpan.FromMilliseconds(d),
						"milliseconds" => TimeSpan.FromMilliseconds(d),
						_ => throw new Exception($"Unknown time unit given for parameter: {fmt}"),
					};
				}
				if (value is string s && TimeSpan_.TryParseExpr(s) is TimeSpan ts)
				{
					return ts;
				}
			}
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
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
