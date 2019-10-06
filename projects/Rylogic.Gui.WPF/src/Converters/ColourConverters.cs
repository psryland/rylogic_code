using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Rylogic.Attrib;
using Rylogic.Common;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF.Converters
{
	public class ToMediaColor : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (value)
			{
			case Color col:
				{
					return col;
				}
			case System.Drawing.Color dcol:
				{
					return dcol.ToMediaColor();
				}
			case Colour32 col32:
				{
					return col32.ToMediaColor();
				}
			case uint u32:
				{
					return new Colour32(u32).ToMediaColor();
				}
			case int i32:
				{
					return new Colour32((uint)i32).ToMediaColor();
				}
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is Color col))
				return null;
			if (targetType.Equals(typeof(Color)))
				return col;
			if (targetType.Equals(typeof(System.Drawing.Color)))
				return col.ToColor();
			if (targetType.Equals(typeof(Colour32)))
				return col.ToColour32();
			if (targetType.Equals(typeof(uint)))
				return col.ToColour32().ARGB;
			if (targetType.Equals(typeof(int)))
				return (int)col.ToColour32().ARGB;
			return null;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class ColourToBrush : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			// Convert all colour types to Colour32
			var colour = Colour32.Black;
			switch (value)
			{
			case Color col:
				{
					colour = col.ToColour32();
					break;
				}
			case Colour32 col32:
				{
					colour = col32;
					break;
				}
			case uint u32:
				{
					colour = new Colour32(u32);
					break;
				}
			case int i32:
				{
					colour = new Colour32((uint)i32);
					break;
				}
			}
			return new SolidColorBrush(colour.ToMediaColor());
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
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
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class ColourToString : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			switch (value)
			{
			case Color col:
				return col.ToString();
			case Colour32 col32:
				return col32.ToString();
			case uint u32:
				return new Colour32(u32).ToString();
			case int i32:
				return new Colour32((uint)i32).ToString();
			}
			return null;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string str))
				return null;
			if (targetType.Equals(typeof(Color)))
				return Colour32.TryParse(str)?.ToMediaColor();
			if (targetType.Equals(typeof(Colour32)))
				return Colour32.TryParse(str);
			if (targetType.Equals(typeof(uint)))
				return Colour32.TryParse(str)?.ARGB;
			if (targetType.Equals(typeof(int)))
				return (int?)Colour32.TryParse(str)?.ARGB;
			return null;
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class TextOverColourBrush : MarkupExtension, IValueConverter
	{
		// Notes:
		//   - Given a colour, returns a brush suitable for text written over that colour
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var colour = Colour32.White;
			switch (value)
			{
			case Color col:
				colour = col.ToColour32();
				break;
			case Colour32 col32:
				colour = col32;
				break;
			case uint u32:
				colour = new Colour32(u32);
				break;
			case int i32:
				colour = new Colour32((uint)i32);
				break;
			}
			return colour.Intensity > 0.5
				? Brushes.Black
				: Brushes.White;
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
	public class ErrorLevelToBrush : MarkupExtension, IValueConverter
	{
		// Notes:
		//   - Returns colours for a given error level
		//   - Use parameter="bg" to get the background colour
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is EErrorLevel lvl)) return Colors.Black;
			return parameter is string str && str == "bg"
				? new SolidColorBrush(lvl.Background().ToMediaColor())
				: new SolidColorBrush(lvl.Foreground().ToMediaColor());
		}
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
