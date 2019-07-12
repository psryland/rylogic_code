using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>An event raised whenever the selected row in the grid changes</summary>
		private event EventHandler<SelectionEventArgs> SelectionChanged;
		private class SelectionEventArgs :EventArgs
		{
			private readonly Main m_main;
			public SelectionEventArgs(Main main)
			{
				m_main = main;
			}

			/// <summary>The selected rows</summary>
			public IEnumerable<ILogDataRow> Rows
			{
				get { return m_main.m_grid.GetRowsWithState(DataGridViewElementStates.Selected).Select(x => m_main.ReadLine(x.Index)); }
			}
		}
		private void RaiseSelectionChanged()
		{
			var args = new SelectionEventArgs(this);
			SelectionChanged?.Invoke(this, args);
		}

		/// <summary>Select the row in the file that contains the byte offset 'addr'</summary>
		private void SelectRowByAddr(long addr)
		{
			// If 'addr' is within the currently loaded range, select the row now
			if (LineIndexRange.Contains(addr))
			{
				SelectedRowIndex = LineIndex(m_line_index, addr);
			}
			else // Otherwise, load the range around 'addr', then select the row
			{
				BuildLineIndex(addr, false, ()=> SelectedRowIndex = LineIndex(m_line_index, addr));
			}
		}

		/// <summary>
		/// Get/Set the currently selected grid row. Get returns -1 if there are no rows in the grid.
		/// Setting the selected row clamps to the range [0,RowCount) and clears all other selected rows</summary>
		private int SelectedRowIndex
		{
			get
			{
				// If the current row is selected, return that
				var row = m_grid.CurrentRow;
				if (row != null && row.Selected)
					return row.Index;

				return -1;
			}
			set
			{
				Log.Info(this, $"Row {value} selected");
				using (m_suspend_grid_events.RefToken(this))
					m_grid.SelectSingleRow(value);

				UpdateStatus();
			}
		}

		/// <summary>Make 'index' visible (if not already)</summary>
		private void DisplayRow(int index)
		{
			//	if (make_displayed && !row.Displayed)
			//				grid.FirstDisplayedScrollingRowIndex = index;
			//		}
			//return index;
		}

		/// <summary>Returns the number of selected rows</summary>
		private int SelectedRowCount
		{
			get { return m_grid.Rows.GetRowCount(DataGridViewElementStates.Selected); }
		}

		/// <summary>Returns the byte offset of the selected row, or 0 if there is no selection</summary>
		private Range SelectedRowByteRange
		{
			get
			{
				var idx = SelectedRowIndex;
				if (idx == -1) idx = 0;
				Debug.Assert(idx >= 0 && idx < m_line_index.Count, "SelectedRowByteOffset should not be called when there are no lines");
				return m_line_index[idx];
			}
		}

		/// <summary>Returns the byte range that includes all selected rows (even if there are holes in the selection)</summary>
		private Range SelectedRowsByteRange
		{
			get
			{
				var r = m_grid.SelectedRowIndexRange();
				if (r.Empty) return SelectedRowByteRange; // 0 or 1 row selected
				return new Range(
					m_line_index[r.Begi].Beg,
					m_line_index[r.Endi].End + 1);
			}
		}
	}
}
