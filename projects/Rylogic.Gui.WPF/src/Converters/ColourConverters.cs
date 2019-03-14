using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF
{
	public class ColourToBrush : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color col)
			{
				return new SolidColorBrush(col);
			}
			if (value is Colour32 col32)
			{
				return new SolidColorBrush(col32.ToMediaColor());
			}
			if (value is uint u32)
			{
				var a = (byte)((u32 >> 24) & 0xFF);
				var r = (byte)((u32 >> 16) & 0xFF);
				var g = (byte)((u32 >> 8) & 0xFF);
				var b = (byte)((u32 >> 0) & 0xFF);
				return new SolidColorBrush(Color.FromArgb(a, r, g, b));
			}
			if (value is int i32)
			{
				u32 = (uint)i32;
				var a = (byte)((u32 >> 24) & 0xFF);
				var r = (byte)((u32 >> 16) & 0xFF);
				var g = (byte)((u32 >> 8) & 0xFF);
				var b = (byte)((u32 >> 0) & 0xFF);
				return new SolidColorBrush(Color.FromArgb(a, r, g, b));
			}

			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is SolidColorBrush b))
				return null;
			if (targetType.Equals(typeof(Color)))
				return b.Color;
			if (targetType.Equals(typeof(Colour32)))
				return b.Color.ToColour32();
			if (targetType.Equals(typeof(UInt32)))
				return b.Color.ToArgbU();
			if (targetType.Equals(typeof(Int32)))
				return b.Color.ToArgb();
			return null;
		}
	}

	public class ColourToString : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Color col)
			{
				return col.ToString();
			}
			if (value is Colour32 col32)
			{
				return col32.ToString();
			}
			if (value is uint u32)
			{
				return new Colour32(u32).ToString();
			}
			if (value is int i32)
			{
				u32 = (uint)i32;
				return new Colour32(u32).ToString();
			}
			return null;
		}
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string str))
			{
				return null;
			}
			if (targetType.Equals(typeof(Color)))
			{
				return Colour32.TryParse(str)?.ToMediaColor();
			}
			if (targetType.Equals(typeof(Colour32)))
			{
				return Colour32.TryParse(str);
			}
			if (targetType.Equals(typeof(uint)))
			{
				return Colour32.TryParse(str)?.ARGB;
			}
			if (targetType.Equals(typeof(int)))
			{
				return (int?)Colour32.TryParse(str)?.ARGB;
			}
			return null;
		}
	}
}
