using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public static class Typeface_
	{
		/// <summary>Convert a string to a font style</summary>
		public static FontStyle Style(string style)
		{
			switch (style)
			{
			default: throw new Exception($"Unknown font style: {style}");
			case nameof(FontStyles.Normal): return FontStyles.Normal;
			case nameof(FontStyles.Italic): return FontStyles.Italic;
			case nameof(FontStyles.Oblique): return FontStyles.Oblique;
			}
		}

		/// <summary>Convert a string to a font weight</summary>
		public static FontWeight Weight(string weight)
		{
			if (int.TryParse(weight, out var w))
				return FontWeight.FromOpenTypeWeight(w);

			switch (weight)
			{
			default: throw new Exception($"Unknown font style:{weight}");
			case nameof(FontWeights.Thin): return FontWeights.Thin;
			case nameof(FontWeights.ExtraLight): return FontWeights.ExtraLight;
			case nameof(FontWeights.UltraLight): return FontWeights.UltraLight;
			case nameof(FontWeights.Light): return FontWeights.Light;
			case nameof(FontWeights.Normal): return FontWeights.Normal;
			case nameof(FontWeights.Regular): return FontWeights.Regular;
			case nameof(FontWeights.Medium): return FontWeights.Medium;
			case nameof(FontWeights.DemiBold): return FontWeights.DemiBold;
			case nameof(FontWeights.SemiBold): return FontWeights.SemiBold;
			case nameof(FontWeights.Bold): return FontWeights.Bold;
			case nameof(FontWeights.ExtraBold): return FontWeights.ExtraBold;
			case nameof(FontWeights.UltraBold): return FontWeights.UltraBold;
			case nameof(FontWeights.Black): return FontWeights.Black;
			case nameof(FontWeights.Heavy): return FontWeights.Heavy;
			case nameof(FontWeights.ExtraBlack): return FontWeights.ExtraBlack;
			case nameof(FontWeights.UltraBlack): return FontWeights.UltraBlack;
			}
		}

		/// <summary>Convert a string to a font stretch</summary>
		public static FontStretch Stretches(string stretch)
		{
			switch (stretch)
			{
			default: throw new Exception();
			case nameof(FontStretches.UltraCondensed): return FontStretches.UltraCondensed;
			case nameof(FontStretches.ExtraCondensed): return FontStretches.ExtraCondensed;
			case nameof(FontStretches.Condensed): return FontStretches.Condensed;
			case nameof(FontStretches.SemiCondensed): return FontStretches.SemiCondensed;
			case nameof(FontStretches.Normal): return FontStretches.Normal;
			case nameof(FontStretches.Medium): return FontStretches.Medium;
			case nameof(FontStretches.SemiExpanded): return FontStretches.SemiExpanded;
			case nameof(FontStretches.Expanded): return FontStretches.Expanded;
			case nameof(FontStretches.ExtraExpanded): return FontStretches.ExtraExpanded;
			case nameof(FontStretches.UltraExpanded): return FontStretches.UltraExpanded;
			}
		}

		/// <summary>Creates typeface from the framework element.</summary>
		public static Typeface CreateTypeface(this FrameworkElement fe)
		{
			return new Typeface(
				(FontFamily)fe.GetValue(TextBlock.FontFamilyProperty),
				(FontStyle)fe.GetValue(TextBlock.FontStyleProperty),
				(FontWeight)fe.GetValue(TextBlock.FontWeightProperty),
				(FontStretch)fe.GetValue(TextBlock.FontStretchProperty));
		}

		/// <summary>Return the font size associated with this framework element</summary>
		public static double FontSize(this FrameworkElement fe)
		{
			return (double)fe.GetValue(TextBlock.FontSizeProperty);
		}
	}
}
