using System;
using System.Windows.Media;

namespace CoinFlip.UI.Indicators
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
		public static DoubleCollection ToStrokeDashArray(this ELineStyles line_style)
		{
			switch (line_style)
			{
			case ELineStyles.Solid: return null;
			case ELineStyles.Dashed: return new DoubleCollection(new[] { 5.0, 2.0 });
			case ELineStyles.Dots: return new DoubleCollection(new[] { 1.0, 5.0 });
			case ELineStyles.DotDash: return new DoubleCollection(new[] { 1.0, 2.0, 5.0, 2.0 });
			case ELineStyles.DotDotDash: return new DoubleCollection(new[] { 1.0, 2.0, 1.0, 2.0, 5.0, 2.0 });
			default: throw new Exception($"Unknown line style: {line_style}");
			}
		}
	}
}
