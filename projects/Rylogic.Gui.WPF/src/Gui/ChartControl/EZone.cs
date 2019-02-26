using System;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Named parts of the chart</summary>
		[Flags]
		public enum EZone
		{
			None,
			Chart = 1 << 0,
			XAxis = 1 << 1,
			YAxis = 1 << 2,
			Title = 1 << 3,
		}
	}
}