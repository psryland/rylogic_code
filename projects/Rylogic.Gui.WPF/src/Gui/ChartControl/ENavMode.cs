using Rylogic.Attrib;

namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		// Prefer camera matrix operations for navigation, then call
		// SetRangeFromCamera to update the X/Y axis since these work
		// for 2D or 3D.

		/// <summary>Navigation methods for moving the camera</summary>
		public enum ENavMode
		{
			[Desc("2D Chart")]
			Chart2D,

			[Desc("3D Scene")]
			Scene3D,
		}
	}
}
