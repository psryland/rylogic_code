using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.Converters
{
	public class ScaleValue : MarkupExtension, IValueConverter
	{
		// Notes:
		//  - Scale a value by the given parameter
		public object? Convert(object value, Type target_type, object parameter, CultureInfo culture)
		{
			try
			{
				var scale =
					parameter is double d ? d :
					System.Convert.ToDouble(parameter);

				// Trim anything that isn't a digit, minus sign, or decimal point
				var num =
					value is double vd ? vd :
					value is string vs ? System.Convert.ToDouble(vs.Strip(x => !char.IsDigit(x) && x != '.' && x != '-')) :
					System.Convert.ToDouble(value);

				return System.Convert.ChangeType(num * scale, target_type);
			}
			catch
			{
				return null;
			}
		}
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			try
			{
				var scale =
					parameter is double sd ? sd :
					System.Convert.ToDouble(parameter);

				// Trim anything that isn't a digit, minus sign, or decimal point
				var num =
					value is double vd ? vd :
					value is string vs ? System.Convert.ToDouble(vs.Strip(x => !char.IsDigit(x) && x != '.' && x != '-')) :
					System.Convert.ToDouble(value);

				return System.Convert.ChangeType(num / scale, target_type);
			}
			catch
			{
				return null;
			}
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class VecToString : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (value)
			{
			case v4 vec4:
				return parameter != null ? vec4.ToString3() : vec4.ToString4();
			case v3 vec3:
				return vec3.ToString3();
			case v2 vec2:
				return vec2.ToString2();
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string str))
				return null;

			if (targetType.Equals(typeof(v4)))
			{
				if (v4.TryParse4(str, out var vec4))
					return vec4;

				var w = parameter != null ? Util.ConvertTo<float>(parameter) : 0f;
				if (v4.TryParse3(str, out vec4, w))
					return vec4;

				return null;
			}
			if (targetType.Equals(typeof(v3)))
			{
				if (v3.TryParse3(str, out var vec3))
					return vec3;

				return null;
			}
			if (targetType.Equals(typeof(v2)))
			{
				if (v2.TryParse2(str, out var vec2))
					return vec2;

				return null;
			}

			return null;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class NoNaN :MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (value)
			{
				case v4 vec4:
					return !Math_.IsNaN(vec4) ? vec4 : "---";
				case v3 vec3:
					return !Math_.IsNaN(vec3) ? vec3 : "---";
				case v2 vec2:
					return !Math_.IsNaN(vec2) ? vec2 : "---";
				case double db:
					return !Math_.IsNaN(db) ? db : "---";
				case float ft:
					return !Math_.IsNaN(ft) ? ft : "---";
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string str)
			{
				if (targetType == typeof(v4)) return v4.NaN;
				if (targetType == typeof(v3)) return v3.NaN;
				if (targetType == typeof(v2)) return v2.NaN;
				if (targetType == typeof(double)) return double.NaN;
				if (targetType == typeof(float)) return float.NaN;
			}
			return value;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}

}
