using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;

namespace Rylogic.Gui.WPF.Converters
{
	public class ToMediaColor : MarkupExtension, IValueConverter
	{
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (targetType == typeof(Brush))
				throw new Exception("Target type is a brush, not a Color. Use ColourToBrush instead");

			switch (value)
			{
				case Colour32 col32:
				{
					return col32.ToMediaColor();
				}
				case Color col:
				{
					return col;
				}
				case System.Drawing.Color dcol:
				{
					return dcol.ToMediaColor();
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
				case Colour32 col32:
				{
					colour = col32;
					break;
				}
				case Color col:
				{
					colour = col.ToColour32();
					break;
				}
				case System.Drawing.Color col1:
				{
					colour = col1.ToArgbU();
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
			if (parameter is string op)
			{
				// lerp:AARRGGBB frac
				if (Regex.Match(op, @"lerp:\s*([0-9a-fA-F]{8})\s+(\d\.?\d*)") is Match m0 && m0.Success)
				{
					if (Colour32.TryParse(m0.Groups[1].Value) is Colour32 target &&
						double_.TryParse(m0.Groups[2].Value) is double frac)
						colour = Colour32.LerpRGB(colour, target, frac);
				}
				if (Regex.Match(op, @"lerpA:\s*([0-9a-fA-F]{8})\s+(\d\.?\d*)") is Match m1 && m1.Success)
				{
					if (Colour32.TryParse(m1.Groups[1].Value) is Colour32 target &&
						double_.TryParse(m1.Groups[2].Value) is double frac)
						colour = Colour32.Lerp(colour, target, frac);
				}
				// lerp:ColourName frac
				else if (Regex.Match(op, @"lerp:\s*(.*?)\s+(\d\.?\d*)") is Match m2 && m2.Success)
				{
					if (Colour32.TryParse(m2.Groups[1].Value) is Colour32 target0 &&
						double_.TryParse(m2.Groups[2].Value) is double frac0)
					{
						colour = Colour32.LerpRGB(colour, target0, frac0);
					}
					else if (Application.Current.TryFindResource(m2.Groups[1].Value) is SolidColorBrush target1 &&
						double_.TryParse(m2.Groups[2].Value) is double frac1)
					{
						colour = Colour32.LerpRGB(colour, target1.Color.ToColour32(), frac1);
					}
				}
				else if (Regex.Match(op, @"lerpA:\s*(.*?)\s+(\d\.?\d*)") is Match m3 && m3.Success)
				{
					if (Colour32.TryParse(m3.Groups[1].Value) is Colour32 target0 &&
						double_.TryParse(m3.Groups[2].Value) is double frac0)
					{
						colour = Colour32.Lerp(colour, target0, frac0);
					}
					else if (Application.Current.TryFindResource(m3.Groups[1].Value) is SolidColorBrush target1 &&
						double_.TryParse(m3.Groups[2].Value) is double frac1)
					{
						colour = Colour32.Lerp(colour, target1.Color.ToColour32(), frac1);
					}
				}
			}
			return new SolidColorBrush(colour.ToMediaColor());
		}
		public object? ConvertBack(object value, Type target_type, object parameter, CultureInfo culture)
		{
			if (value is not SolidColorBrush b)
				return null;
			if (target_type.Equals(typeof(Color)))
				return b.Color;
			if (target_type.Equals(typeof(Colour32)))
				return b.Color.ToColour32();
			if (target_type.Equals(typeof(uint)))
				return b.Color.ToArgbU();
			if (target_type.Equals(typeof(int)))
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
				case Colour32 col32:
				{
					colour = col32;
					break;
				}
				case Color col:
				{
					colour = col.ToColour32();
					break;
				}
				case System.Drawing.Color dcol:
				{
					colour = dcol.ToMediaColor().ToColour32();
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
