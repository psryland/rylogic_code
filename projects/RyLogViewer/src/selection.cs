using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public partial class Main
	{
		/// <summary>An event raised whenever the selected row in the grid changes</summary>
		private event EventHandler<SelectionEventArgs> SelectionChanged;
		private class SelectionEventArgs :EventArgs
		{
			/// <summary>The selected rows</summary>
			public IEnumerable<ILogDataRow> Rows { get; private set; }
			public SelectionEventArgs(Main main)
			{
				Rows = main.m_grid.GetRowsWithState(DataGridViewElementStates.Selected).Select(x => main.ReadLine(x.Index));
			}
		}
		private void RaiseSelectionChanged()
		{
			if (SelectionChanged == null) return;
			SelectionChanged(this, new SelectionEventArgs(this));
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
		/// Setting the selected row clamps to the range [0,RowCount) and makes it visible in the grid (if possible)</summary>
		private int SelectedRowIndex
		{
			get
			{
				// If the current row is selected, return that
				var row = m_grid.CurrentRow;
				if (row != null && row.Selected) return row.Index;
				return -1;
			}
			set
			{
				using (m_suspend_grid_events.Reference)
				{
					value = m_grid.SelectRow(value);
					Log.Info(this, "Row {0} selected".Fmt(value));
					if (m_grid.RowCount != 0 && value != -1)
						UpdateStatus();
				}
			}
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
	}
}
