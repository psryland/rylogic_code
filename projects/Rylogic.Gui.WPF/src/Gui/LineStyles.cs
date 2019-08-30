using System;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	/// <summary>Line graphics styles</summary>
	public enum ELineStyles
	{
		Solid,
		Dashed,
		Dots,
		DotDash,
		DotDotDash,
	}

	public static class LineStyles_
	{
		/// <summary>Return the stroke dash array for this line style</summary>
		public static DoubleCollection ToStrokeDashArray(this ELineStyles line_style, double scale = 1.0)
		{
			switch (line_style)
			{
			case ELineStyles.Solid:      return null;
			case ELineStyles.Dashed:     return new DoubleCollection(new[] { 5.0 * scale, 2.0 * scale });
			case ELineStyles.Dots:       return new DoubleCollection(new[] { 1.0 * scale, 5.0 * scale });
			case ELineStyles.DotDash:    return new DoubleCollection(new[] { 1.0 * scale, 2.0 * scale, 5.0 * scale, 2.0 * scale });
			case ELineStyles.DotDotDash: return new DoubleCollection(new[] { 1.0 * scale, 2.0 * scale, 1.0 * scale, 2.0 * scale, 5.0 * scale, 2.0 * scale });
			default: throw new Exception($"Unknown line style: {line_style}");
			}
		}
	}
}
