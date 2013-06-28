using System.Windows.Forms;

namespace RyLogViewer
{
	/// <summary>Subclass the DGV to add missing features</summary>
	public sealed class DGV :DataGridView
	{
		public DGV()
		{
			DoubleBuffered = true;
		}
	}
}
