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
		public static DoubleCollection? ToStrokeDashArray(this ELineStyles line_style, double scale = 1.0)
		{
			return line_style switch
			{
				ELineStyles.Solid => null,
				ELineStyles.Dashed => new DoubleCollection(new[] { 5.0 * scale, 2.0 * scale }),
				ELineStyles.Dots => new DoubleCollection(new[] { 1.0 * scale, 5.0 * scale }),
				ELineStyles.DotDash => new DoubleCollection(new[] { 1.0 * scale, 2.0 * scale, 5.0 * scale, 2.0 * scale }),
				ELineStyles.DotDotDash => new DoubleCollection(new[] { 1.0 * scale, 2.0 * scale, 1.0 * scale, 2.0 * scale, 5.0 * scale, 2.0 * scale }),
				_ => throw new Exception($"Unknown line style: {line_style}"),
			};
		}
	}
}
