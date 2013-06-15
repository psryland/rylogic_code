using System.Windows.Forms;

namespace RyLogViewer
{
	/// <summary>Subclass the DGV to add missing features</summary>
	public class DGV :DataGridView
	{
		public DGV()
		{
			//VerticalScrollBar  .VisibleChanged += (s,a) => VerticalScrollBar  .Visible = true;
			//HorizontalScrollBar.VisibleChanged += (s,a) => HorizontalScrollBar.Visible = true;
		}
	}
}
