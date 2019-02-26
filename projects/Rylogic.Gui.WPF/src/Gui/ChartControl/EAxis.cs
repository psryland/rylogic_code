using System;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Axis type</summary>
		[Flags]
		public enum EAxis
		{
			None = 0,
			XAxis = 1 << 0,
			YAxis = 1 << 1,
			Both = XAxis | YAxis,
		}
	}
}
