using System.Windows;
using System.Windows.Controls;

namespace CoinFlip.UI.GfxObjects
{
	public static class Misc
	{
		// Notes;
		//  - Helper functions related to 'GfxObjects'

		/// <summary>Ensure 'vis' is a child of 'overlay'</summary>
		public static void AddToOverlay(FrameworkElement vis, Canvas overlay)
		{
			if (vis.Parent is Canvas parent)
			{
				// Already a child.
				if (parent == overlay) return;

				// Child of a different overlay
				parent.Children.Remove(vis);
			}

			// Add 'vis' as a child of 'overlay'
			overlay.Children.Add(vis);
		}
	}
}
