using System;
using System.Globalization;
using System.Windows.Data;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public class VecToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is v4 vec4)
				return parameter != null ? vec4.ToString3() : vec4.ToString4();
			if (value is v3 vec3)
				return vec3.ToString3();
			if (value is v2 vec2)
				return vec2.ToString2();
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
	}
}
