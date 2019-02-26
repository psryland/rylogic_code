namespace Rylogic.Gui.WPF
{
	public partial class ChartControl
	{
		/// <summary>Chart change types</summary>
		public enum EChangeType
		{
			/// <summary>
			/// Raised after elements in the chart have been moved, resized, or had their content changed.
			/// This event will be raised in addition to the more detailed modification events below</summary>
			Edited,

			/// <summary>
			/// Elements are about to be deleted from the chart by the user.
			/// Setting 'Cancel' for this event will abort the deletion.</summary>
			RemovingElements,
		}
	}
}
