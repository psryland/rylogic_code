using System;

namespace CoinFlip
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class IndicatorAttribute :Attribute
	{
		public IndicatorAttribute()
		{
			IsDrawing = false;
		}

		/// <summary>True if this indicator is a simple graphic rather than a data series</summary>
		public bool IsDrawing { get; set; }
	}
}
