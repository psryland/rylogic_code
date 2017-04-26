using System.Drawing;
using System.Reflection;

namespace RyLogViewer
{
	/// <summary>Subclass the DataGridView to add missing features</summary>
	public sealed class DataGridView :System.Windows.Forms.DataGridView
	{
		public DataGridView()
		{
			DoubleBuffered = true;
			m_fi_ptAnchorCell = typeof(System.Windows.Forms.DataGridView).GetField("ptAnchorCell", BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
		}

		/// <summary>Get/Set the cell anchor, used for Shift+Selection</summary>
		public Point SelectionAnchorCell
		{
			get { return (Point)m_fi_ptAnchorCell.GetValue(this); }
			set { m_fi_ptAnchorCell.SetValue(this, value); }
		}
		private FieldInfo m_fi_ptAnchorCell;
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
