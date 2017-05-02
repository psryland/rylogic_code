using pr.maths;

namespace RyLogViewer
{
	/// <summary>Subclass the DataGridView to add missing features</summary>
	public sealed class DataGridView :pr.gui.DataGridView
	{
		public DataGridView()
		{
			DoubleBuffered = true;
		}

		/// <summary>
		/// Sets the selection to row 'index'. If the grid has rows, clamps 'index' to [-1,RowCount).
		/// If index == -1, the selection is cleared. Returns the row actually selected.
		/// If 'make_displayed' is true, scrolls the grid to make 'index' displayed</summary>
		public int SelectSingleRow(int row_index, bool make_displayed = false)
		{
			ClearSelection();

			row_index = Maths.Clamp(row_index, -1, RowCount-1);
			if (row_index == -1)
			{
				CurrentCell = null;
			}
			else
			{
				var row = Rows[row_index];
				var col_index = CurrentCell != null ? CurrentCell.ColumnIndex : 0;

				row.Selected = true;
				CurrentCell = row.Cells[col_index];

				if (make_displayed && !row.Displayed)
					FirstDisplayedScrollingRowIndex = row_index;
			}
			return row_index;
		}
	}

	public sealed class ComboBox :pr.gui.ComboBox
	{
//		public override int SelectedIndex
//		{
//			// Workaround for first chance exception in SelectedIndex
//			get { return Items.Count != 0 ? base.SelectedIndex : -1; }
//			set { if (Items.Count != 0) base.SelectedIndex = value; }
//		}
	}

	public sealed class ListBox :pr.gui.ListBox
	{
//		public override int SelectedIndex
//		{
//			// Workaround for first chance exception in SelectedIndex
//			get { return Items.Count != 0 ? base.SelectedIndex : -1; }
//			set { if (Items.Count != 0) base.SelectedIndex = value; }
//		}
	}
}
