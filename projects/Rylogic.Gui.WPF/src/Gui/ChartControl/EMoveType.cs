using System;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Flags for how a chart was scrolled</summary>
		[Flags]
		public enum EMoveType
		{
			None = 0,
			XZoomed = 1 << 0,
			YZoomed = 1 << 1,
			XScrolled = 1 << 2,
			YScrolled = 1 << 3,
		}
	}
}