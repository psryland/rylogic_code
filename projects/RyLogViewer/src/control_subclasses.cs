using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Subclass the DataGridView to add missing features</summary>
	public sealed class DataGridView :System.Windows.Forms.DataGridView
	{
		public DataGridView()
		{
			DoubleBuffered = true;
		}
	}

	public sealed class ComboBox :System.Windows.Forms.ComboBox
	{
		public override int SelectedIndex
		{
			// Workaround for first chance exception in SelectedIndex
			get { return Items.Count != 0 ? base.SelectedIndex : -1; }
			set { if (Items.Count != 0) base.SelectedIndex = value; }
		}
	}

	public sealed class ListBox :System.Windows.Forms.ListBox
	{
		public override int SelectedIndex
		{
			// Workaround for first chance exception in SelectedIndex
			get { return Items.Count != 0 ? base.SelectedIndex : -1; }
			set { if (Items.Count != 0) base.SelectedIndex = value; }
		}
	}
}
