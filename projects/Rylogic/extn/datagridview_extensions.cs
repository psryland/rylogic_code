//***************************************************
//  Copyright (c) Rylogic Ltd 2008
//***************************************************
// DGV helpers
// Notes:
//   Useful link of CellStyle inheritance: https://msdn.microsoft.com/en-us/library/1yef90x0.aspx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.maths;
using pr.util;

namespace pr.extn
{
	/// <summary>
	/// Grid standard keyboard shortcuts
	/// Use: add for each function you want supported
	///   e.g.
	///   m_grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
	///   m_grid.KeyDown += DataGridViewExtensions.Cut;</summary>
	public static class DataGridViewExtensions
	{
		/// <summary>Grid select all implementation. (for consistency)</summary>
		public static void SelectAll(DataGridView grid)
		{
			grid.SelectAll();
		}

		/// <summary>Select all rows. Attach to the KeyDown handler</summary>
		public static void SelectAll(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.A) return;
			SelectAll(dgv);
			e.Handled = true;
		}

		/// <summary>Grid copy implementation. Returns true if something was added to the clip board</summary>
		public static bool Copy(DataGridView grid)
		{
			var d = grid.GetClipboardContent();
			if (d == null) return false;
			Clipboard.SetDataObject(d);
			return true;
		}

		/// <summary>Copy selected cells to the clipboard. Attach to the KeyDown handler</summary>
		public static void Copy(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.C) return;
			if (!Copy(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Grid cut implementation. Returns true if something was cut and added to the clip board</summary>
		public static bool Cut(DataGridView grid)
		{
			DataObject d = grid.GetClipboardContent();
			if (d == null) return false;
			Clipboard.SetDataObject(d);

			// Set the selected cells to defaults
			foreach (DataGridViewCell c in grid.SelectedCells)
				if (!c.ReadOnly) c.Value = c.DefaultNewRowValue;

			return true;
		}

		/// <summary>Cut the selected cells to the clipboard. Cut cells replaced with default values. Attach to the KeyDown handler</summary>
		public static void Cut(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.X) return;
			if (!Cut(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Grid delete implementation. Deletes selected items from the grid setting cell values to null</summary>
		public static void Delete(DataGridView grid)
		{
			foreach (DataGridViewCell c in grid.SelectedCells)
				c.Value = null;
		}

		/// <summary>Delete the contents of the selected cells. Attach to the KeyDown handler</summary>
		public static void Delete(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (e.KeyCode != Keys.Delete) return;
			Delete(dgv);
		}

		/// <summary>Grid paste implementation that pastes over existing cells within the current size limits of the grid. Must be 1 cell selected only</summary>
		public static bool PasteReplace(DataGridView grid)
		{
			if (grid.SelectedCells.Count == 1)
			{
				// Read the lines from the clipboard
				var lines = Clipboard.GetText().Split('\n');

				var row = grid.CurrentCell.RowIndex;
				for (var j = 0; j != lines.Length && row != grid.RowCount; ++j, ++row)
				{
					// Skip blank lines
					if (lines[j].Length == 0) continue;

					var col = grid.CurrentCell.ColumnIndex;
					var cells = lines[j].Split('\t',',',';');
					for (var i = 0; i != cells.Length && col != grid.ColumnCount; ++i, ++col)
					{
						var cell = grid[col,row];
						if (cell.ReadOnly) continue;
						if (cells[i].Length == 0) continue;
						try
						{
							var val = Util.ConvertTo(cells[i].Trim(), cell.ValueType);
							cell.Value = val;
						}
						catch (FormatException)
						{
							cell.Value = cell.DefaultNewRowValue;
						}
					}
				}
			}
			else if (grid.SelectedCells.Count > 1)
			{
				// Get a snapshot of the selected grid cells
				var selected_cells = grid.SelectedCells;

				// Find the bounds of the selected cells
				var min = new Point(selected_cells[0].ColumnIndex, selected_cells[0].RowIndex);
				var max = min;
				foreach (DataGridViewCell item in selected_cells)
				{
					min.X = Math.Min(min.X, item.ColumnIndex);
					min.Y = Math.Min(min.Y, item.RowIndex);
					max.X = Math.Max(max.X, item.ColumnIndex+1);
					max.Y = Math.Max(max.Y, item.RowIndex+1);
				}

				// Read the cells from the clipboard
				var lines = Clipboard.GetText().Split('\n');
				var cells = new string[lines.Length][];
				for (var i = 0; i != lines.Length; ++i)
					cells[i] = lines[i].Split('\t',',',';');

				// Paste into the selected cells, filling if the selected cell area
				// is bigger than the clipboard cells
				foreach (DataGridViewCell cell in selected_cells)
				{
					if (cell.ReadOnly) continue;

					try
					{
						var row = Math.Min(cell.RowIndex    - min.Y, cells.Length      - 1);
						var col = Math.Min(cell.ColumnIndex - min.X, cells[row].Length - 1);
						if (cells[row][col].Length != 0)
							cell.Value = Convert.ChangeType(cells[row][col], cell.ValueType);
					}
					catch (FormatException)
					{
						cell.Value = cell.DefaultNewRowValue;
					}
				}
			}
			return true;
		}

		/// <summary>Paste over existing cells within the current size limits of the grid. Must be 1 cell selected only. Attach to the KeyDown handler</summary>
		public static void PasteReplace(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (!PasteReplace(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Paste from the first selected cell over anything in the way. Grow the grid if necessary</summary>
		public static bool PasteGrow(DataGridView grid)
		{
			if (grid.SelectedCells.Count > 1) return false;

			var row = grid.CurrentCell.RowIndex;
			var col = grid.CurrentCell.ColumnIndex;

			// Read the lines from the clipboard
			var lines = Clipboard.GetText().Split('\n');

			// Grow the dgv if necessary
			if (row + lines.Length > grid.RowCount)
				grid.RowCount = row + lines.Length;

			for (var j = 0; j != lines.Length; ++j)
			{
				// Skip blank lines
				if (lines[j].Length == 0) continue;

				var cells = lines[j].Split('\t', ',', ';');

				// Grow the grid if necessary
				if (col + cells.Length > grid.ColumnCount)
					grid.ColumnCount = col + cells.Length;

				for (var i = 0; i != cells.Length; ++i)
				{
					var cell = grid[i+col,j+row];
					if (cell.ReadOnly) continue;
					if (cells[i].Length == 0) continue;
					try { cell.Value = Convert.ChangeType(cells[i], cell.ValueType); }
					catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
				}
			}
			return true;
		}

		/// <summary>Paste from the first selected cell over anything in the way. Grow the grid if necessary. Attach to the KeyDown handler</summary>
		public static void PasteGrow(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (!PasteGrow(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Combined handler for cut, copy, and paste replace functions. Attach to the KeyDown handler</summary>
		public static void CutCopyPasteReplace(object sender, KeyEventArgs e)
		{
			if (e.Handled) return; // already handled
			SelectAll   (sender, e);
			Cut         (sender, e);
			Copy        (sender, e);
			PasteReplace(sender, e);
		}

		/// <summary>
		/// Handle column sorting for grids with data sources that don't support sorting by default.
		/// 'handle_sort' will be called after the column header glyph has changed.
		/// *WARNING*: 'handle_sort' cannot be removed so only call once and be careful with reference lifetimes</summary>
		public static void SupportSorting(this DataGridView grid, EventHandler<DGVSortEventArgs> handle_sort)
		{
			// TODO, using reflection, you could search the invocation list of ColumnHeaderMouseClick for
			// DGVSortDelegate (subclassed from Delegate) and remove it if 'handle_sort' is null.
			// DGVSortDelegate could also contain the reference to 'handle_sort'

			grid.ColumnHeaderMouseClick += (s,a) =>
				{
					var dgv = s.As<DataGridView>();
					if (a.Button == MouseButtons.Left && Control.ModifierKeys == Keys.None)
					{
						// Reset the sort glyph for the other columns
						foreach (var c in dgv.Columns.Cast<DataGridViewColumn>())
						{
							if (c.Index == a.ColumnIndex) continue;
							c.HeaderCell.SortGlyphDirection = SortOrder.None;
						}

						// Set the glyph on the selected column
						var col = dgv.Columns[a.ColumnIndex];
						var hdr = col.HeaderCell;
						hdr.SortGlyphDirection = Enum<SortOrder>.Cycle(hdr.SortGlyphDirection);

						// Apply the sort
						handle_sort.Raise(dgv, new DGVSortEventArgs(a.ColumnIndex, hdr.SortGlyphDirection));
					}
				};
		}

		/// <summary>Display a context menu for showing/hiding columns in the grid (at 'location' relative to the grid).</summary>
		public static void ColumnVisibilityContextMenu(this DataGridView grid, Point location, Action<DataGridViewColumn> on_vis_changed = null)
		{
			var menu = new ContextMenuStrip();
			foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
			{
				var item = new ToolStripMenuItem
				{
					Text = col.HeaderText,
					Checked = col.Visible,
					CheckOnClick = true,
					Tag = col
				};
				item.Click += (s,a)=>
				{
					var c = (DataGridViewColumn)item.Tag;
					c.Visible = item.Checked;
					if (on_vis_changed != null)
						on_vis_changed(c);
				};
				menu.Items.Add(item);
			}
			menu.Show(grid, location);
		}

		/// <summary>Return a collection of the fill weights</summary>
		public static float[] FillWeights(this DataGridView grid)
		{
			var w = new float[grid.Columns.Count];
			for (var i = 0; i != w.Length; ++i) w[i] = grid.Columns[i].FillWeight;
			return w;
		}

		/// <summary>Scale the fill weights so that they sum up to 1f</summary>
		public static void NormaliseFillWeights(this DataGridView grid)
		{
			var sum = (float)grid.Columns.Cast<DataGridViewColumn>().Sum(x => x.FillWeight);
			foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
				col.FillWeight /= sum;
		}

		/// <summary>Options for smart column resizing</summary>
		[Flags] public enum EColumnSizeOptions
		{
			None                 = 0,
			Preferred            = 1 << 0,
			IncludeColumnHeaders = 1 << 1,
			GrowToDisplayWidth   = 1 << 2,
			ShrinkToDisplayWidth = 1 << 3,
			FitToDisplayWidth    = GrowToDisplayWidth | ShrinkToDisplayWidth,
		}

		/// <summary>Return the array of ideal widths for the columns</summary>
		public static float[] GetColumnWidths(this DataGridView grid, EColumnSizeOptions opts)
		{
			var columns     = grid.Columns.Cast<DataGridViewColumn>();
			var grid_width  = Math.Max(grid.DisplayRectangle.Width - 3f, 1f);

			// Adjust for the RowHeaders being visible
			if (grid.RowHeadersVisible)
				grid_width -= grid.RowHeadersWidth;

			// Adjust for the vertical scroll bar being visible
			if ((grid.GetScrollBarVisibility() & ScrollBars.Vertical) != 0)
				grid_width -= SystemInformation.VerticalScrollBarWidth;

			var widths      = columns.Select(x => x.Visible ? grid_width * x.FillWeight : 0).ToArray();
			var total_width = Math.Max(widths.Sum(), 1f);

			// Measure each column's preferred width
			var pref_widths = widths.ToArray();
			using (var gfx = grid.CreateGraphics())
			{
				// For some reason, cell.GetPreferredSize or col.GetPreferredWidth don't return correct values
				Func<DataGridViewCell,float> preferred_width = (cell) =>
					{
						SizeF sz;
						if      (cell.Value is string) sz = gfx.MeasureString((string)cell.Value, cell.InheritedStyle.Font);
						else if (cell.Value is Image ) sz = ((Image)cell.Value).Size;
						else sz = SizeF.Empty;
						var w = Maths.Clamp(sz.Width + 10, 30, 64000); // DGV throws if width is greater than 65535
						return w;
					};

				if ((opts & EColumnSizeOptions.IncludeColumnHeaders) != 0)
				{
					for (int i = 0, iend = grid.ColumnCount; i != iend; ++i)
					{
						if (!grid.Columns[i].Visible) continue;
						pref_widths[i] = Math.Max(pref_widths[i], preferred_width(grid.Columns[i].HeaderCell));
					}
				}

				foreach (var row in grid.GetRowsWithState(DataGridViewElementStates.Displayed))
				{
					for (int i = 0, iend = Math.Min(grid.ColumnCount, row.Cells.Count); i != iend; ++i)
					{
						if (!grid.Columns[i].Visible) continue;
						pref_widths[i] = Math.Max(pref_widths[i], preferred_width(row.Cells[i]));
					}
				}
			}
			var total_pref_width = Math.Max(pref_widths.Sum(), 1f);

			if ((opts & EColumnSizeOptions.Preferred) != 0)
			{
				if ((total_pref_width < grid_width && (opts & EColumnSizeOptions.GrowToDisplayWidth  ) != 0) ||
					(total_pref_width > grid_width && (opts & EColumnSizeOptions.ShrinkToDisplayWidth) != 0))
				{
					var scale = grid_width / total_pref_width;
					Enumerable.Range(0,widths.Length).ForEach(i => pref_widths[i] *= scale);
				}
				return pref_widths;
			}
			if ((total_width < grid_width && (opts & EColumnSizeOptions.GrowToDisplayWidth  ) != 0) ||
				(total_width > grid_width && (opts & EColumnSizeOptions.ShrinkToDisplayWidth) != 0))
			{
				var scale = grid_width / total_width;
				Enumerable.Range(0,widths.Length).ForEach(i => widths[i] *= scale);
			}
			return widths;
		}

		/// <summary>Set the FillWeights based on the current column widths</summary>
		public static void SetFillWeightsFromCurrentWidths(this DataGridView grid)
		{
			// Sum the column widths
			var sum = (float)grid.Columns.Cast<DataGridViewColumn>().Sum(x => x.Width);
			foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
				col.FillWeight = col.Width / sum;
		}

		/// <summary>When a column width is changed, adjust the fill weights to preserve the column's width, squashing up the columns with indices > 'column_index'</summary>
		public static void SetFillWeightsOnColumnWidthChanged(this DataGridView grid, int column_index)
		{
			if (grid.ColumnCount == 0)
				return;

			// Get the display area width and the total column widths
			var columns     = grid.Columns.Cast<DataGridViewColumn>();
			var grid_width  = Math.Max(grid.DisplayRectangle.Width - 3f, 1f);
			var total_width = columns.Sum(x => x.Width);
			var left_width  = columns.Take(column_index+1).Sum(x => x.Width);

			if (left_width >= grid_width)
			{
				// Set fill weights based on column widths only
				grid.SetFillWeightsFromCurrentWidths();
			}
			else
			{
				// Scale columns > column_index to fill the remaining space
				var scale = (float)(grid_width - left_width) / (total_width - left_width);
				foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
				{
					if (col.Index <= column_index)
						col.FillWeight = (float)col.Width / grid_width;
					else
						col.FillWeight = (float)col.Width * scale / grid_width;
				}
			}
		}

		/// <summary>
		/// Resize the columns intelligently based on currently displayed content
		/// To handle user resized columns, call SetFillWeightsFromCurrentWidths() in the OnColumnWidthChanged() handler, then SetGridColumnSizes()
		/// You'll need to prevent recursion with a flag however</summary>
		public static void SetGridColumnSizes(this DataGridView grid, EColumnSizeOptions opts, bool auto_hide_single_column_header = false)
		{
			if (m_set_grid_columns_sizes != null) return;
			using (Scope.Create(() => m_set_grid_columns_sizes = grid, () => m_set_grid_columns_sizes = null))
			{
				if (grid.ColumnCount == 0)
					return;

				if (auto_hide_single_column_header)
					grid.ColumnHeadersVisible = grid.ColumnCount > 1;

				// Get the ideal column widths
				grid.NormaliseFillWeights();
				var col_widths = grid.GetColumnWidths(opts);

				// Resize columns.
				using (grid.SuspendLayout(true))
				{
					var remainder = 0f;
					foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
					{
						var fwidth = col_widths[col.Index] + remainder;
						remainder  = fwidth - (float)(int)fwidth;

						if (col.Visible)
							col.Width  = (int)fwidth;
					}
				}
			}
		}
		[ThreadStatic] private static object m_set_grid_columns_sizes;

		/// <summary>Return the first selected row, regardless of multi-select grids</summary>
		public static DataGridViewRow FirstSelectedRow(this DataGridView grid)
		{
			var i = grid.Rows.GetFirstRow(DataGridViewElementStates.Selected);
			return i != -1 ? grid.Rows[i] : null;
		}

		/// <summary>Attempts to scroll the grid to 'first_row_index' clamped by the number of rows in the grid</summary>
		public static void TryScrollToRowIndex(this DataGridView grid, int first_row_index)
		{
			if (grid.RowCount == 0) return;
			grid.FirstDisplayedScrollingRowIndex = Maths.Clamp(first_row_index, 0, grid.RowCount - 1);
		}

		/// <summary>Returns an enumerator for accessing rows with the given property</summary>
		public static IEnumerable<DataGridViewRow> GetRowsWithState(this DataGridView grid, DataGridViewElementStates state)
		{
			for (var i = grid.Rows.GetFirstRow(state); i != -1; i = grid.Rows.GetNextRow(i, state))
				yield return grid.Rows[i];
		}

		/// <summary>
		/// Sets the selection to row 'index'. If the grid has rows, clamps 'index' to [-1,RowCount).
		/// If index == -1, the selection is cleared. Returns the row actually selected.
		/// If 'make_displayed' is true, scrolls the grid to make 'index' displayed</summary>
		public static int SelectRow(this DataGridView grid, int index, bool make_displayed = false)
		{
			grid.ClearSelection();
			if (grid.RowCount == 0 || index == -1)
			{
				index = -1;
				grid.CurrentCell = null;
			}
			else
			{
				index = Maths.Clamp(index, 0, grid.RowCount - 1);
				var row = grid.Rows[index];
				row.Selected = true;
				grid.CurrentCell = row.Cells[0];
				if (make_displayed && !row.Displayed)
					grid.FirstDisplayedScrollingRowIndex = index;
			}
			return index;
		}

		/// <summary>Return the index of the first selected row (or -1) if no rows are selected</summary>
		public static int FirstSelectedRowIndex(this DataGridView grid)
		{
			var row = FirstSelectedRow(grid);
			return row != null ? row.Index : -1;
		}

		/// <summary>Checks if the given column/row are within the grid and returns the associated column and cell</summary>
		public static bool Within(this DataGridView grid, int column_index, int row_index, out DataGridViewColumn col, out DataGridViewCell cell)
		{
			col = null; cell = null;
			if (column_index < 0 || column_index >= grid.ColumnCount) return false;
			if (row_index    < 0 || row_index    >= grid.RowCount) return false;
			col = grid.Columns[column_index];
			cell = grid[column_index, row_index];
			return true;
		}

		/// <summary>Checks if the given column/row are within the grid and returns the associated column</summary>
		public static bool Within(this DataGridView grid, int column_index, int row_index, out DataGridViewColumn col)
		{
			DataGridViewCell dummy;
			return Within(grid, column_index, row_index, out col, out dummy);
		}

		/// <summary>Checks if the given column/row are within the grid and returns the associated cell</summary>
		public static bool Within(this DataGridView grid, int column_index, int row_index, out DataGridViewCell cell)
		{
			DataGridViewColumn dummy;
			return Within(grid, column_index, row_index, out dummy, out cell);
		}

		/// <summary>Checks if the given column/row are within the grid</summary>
		public static bool Within(this DataGridView grid, int column_index, int row_index)
		{
			DataGridViewCell dummy;
			return Within(grid, column_index, row_index, out dummy);
		}

		/// <summary>Checks if the given row index is within the range of grid rows</summary>
		public static bool WithinRows(this DataGridView grid, int row_index)
		{
			return row_index >= 0 && row_index < grid.RowCount;
		}

		/// <summary>Checks if the given column index is within the range of grid columns</summary>
		public static bool WithinCols(this DataGridView grid, int col_index)
		{
			return col_index >= 0 && col_index < grid.ColumnCount;
		}

		/// <summary>Data used for dragging rows around</summary>
		private class DGV_DragDropData
		{
			public DataGridViewRow Row { get; private set; }
			public int GrabX { get; private set; }
			public int GrabY { get; private set; }

			/// <summary>The rows with the indicator line drawn on them, last update</summary>
			public int Idx0 { get; set; }
			public int Idx1 { get; set; }

			public DGV_DragDropData(DataGridViewRow row, int x, int y)
			{
				Row = row;
				GrabX = x;
				GrabY = y;
				Idx0 = -1;
				Idx1 = -1;
			}
		}

		/// <summary>
		/// Begin a row drag-drop operation on the grid.
		/// Attach this method to the MouseDown event on the grid.
		/// Note: Do not attach to MouseMove as well, DoDragDrop handles mouse move
		/// Also, attach 'DragDrop_DoDropMoveRow' to the 'DoDrop' handler and set AllowDrop = true.
		/// See the 'DragDrop' class for more info</summary>
		public static void DragDrop_DragRow(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!(grid.DataSource is IList))
				throw new InvalidOperationException("Drag-drop requires a grid with a data source convertable to an IList");

			// Drag by grabbing row headers
			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < grid.RowCount)
			{
				var data = new DGV_DragDropData(grid.Rows[hit.RowIndex], e.X, e.Y);
				grid.DoDragDrop(data, DragDropEffects.Move|DragDropEffects.Copy|DragDropEffects.Link);
			}
		}

		/// <summary>
		/// A drag drop function for moving a row in a grid to a new position.
		/// Attach this method to the 'DoDrop' handler.
		/// Also, attach 'DragDrop_DragRow' to the MouseDown event and set AllowDrop = true on the grid.
		/// See the 'DragDrop' class for more info.</summary>
		public static bool DragDrop_DoDropMoveRow(object sender, DragEventArgs args, DragDrop.EDrop mode)
		{
			// This method could be hooked up to a pr.util.DragDrop so the
			// events could come from anything. Only accept dgvs
			var grid = sender as DataGridView;
			if (grid == null || args == null)
				return false;

			// Must allow move and contain a row
			if ((args.AllowedEffect & DragDropEffects.Move) == 0 || !args.Data.GetDataPresent(typeof(DGV_DragDropData)))
				return false;

			// Get the drag data
			var data = (DGV_DragDropData)args.Data.GetData(typeof(DGV_DragDropData));
			
			// Check the mouse has moved enough to start dragging
			var pt = grid.PointToClient(new Point(args.X, args.Y)); // Find where the mouse is over the grid
			var distsq = Maths.Len2Sq(pt.X - data.GrabX, pt.Y - data.GrabY);
			if (distsq < 25)
				return false;

			// Set the drop effect
			var hit = grid.HitTest(pt.X, pt.Y);
			args.Effect = hit.Type == DataGridViewHitTestType.RowHeader &&
				((hit.RowIndex >= 0 && hit.RowIndex < data.Row.Index) || (hit.RowIndex > data.Row.Index && hit.RowIndex < grid.RowCount))
				? DragDropEffects.Move : DragDropEffects.None;
			if (args.Effect != DragDropEffects.Move)
				return true;

			// The row the mouse is over
			var hit_idx = hit.RowIndex;
			var hit_row = grid.Rows[hit_idx];

			// Find the two rows on either side of the line
			var over_half = pt.Y - hit.RowY > 0.5f * hit_row.Height;
			var idx0 = over_half ? hit_idx : hit_idx - 1;
			var idx1 = over_half ? hit_idx + 1 : hit_idx;

			// If this is not the actual drop then keep returning true until the drop happens
			if (mode != DragDrop.EDrop.Drop)
			{
				grid.RowPostPaint -= DragDrop_LinePainter;
				grid.RowPostPaint += DragDrop_LinePainter;

				// Only invalidate rows when idx0,idx1 change
				if (idx0 != data.Idx0)
				{
					if (grid.WithinRows(data.Idx0)) grid.InvalidateRow(data.Idx0);
					if (grid.WithinRows(idx0     )) grid.InvalidateRow(idx0);
					data.Idx0 = idx0;
				}
				if (idx1 != data.Idx1)
				{
					if (grid.WithinRows(data.Idx1)) grid.InvalidateRow(data.Idx1);
					if (grid.WithinRows(idx1     )) grid.InvalidateRow(idx1);
					data.Idx1 = idx1;
				}

				// Auto scroll when at the first or last displayed row
				if (idx0 >= grid.FirstDisplayedScrollingRowIndex + grid.DisplayedRowCount(false) && idx0 < grid.RowCount - 1)
					grid.FirstDisplayedScrollingRowIndex++;
				if (idx1 <= grid.FirstDisplayedScrollingRowIndex && idx1 > 0)
					grid.FirstDisplayedScrollingRowIndex--;
			}

			// Otherwise, this is the drop
			else
			{
				grid.RowPostPaint -= DragDrop_LinePainter;

				int grab_idx = data.Row.Index;
				if (over_half) ++hit_idx;

				// Insert 'grab_idx' at 'drop_idx'
				var list = grid.DataSource as IList;
				if (list == null) throw new InvalidOperationException("Drag-drop requires a grid with a data source bound to an IList");
				if (grab_idx != hit_idx)
				{
					if (hit_idx > grab_idx)
						--hit_idx;

					var tmp = list[grab_idx];
					list.RemoveAt(grab_idx);
					list.Insert(hit_idx, tmp);
				}

				// If the list is a binding source, set the current position to the item just moved
				var bs = grid.DataSource as BindingSource;
				if (bs != null)
					bs.Position = hit_idx;

				// Invalidate the hit rows to ensure repainting
				grid.InvalidateRow(data.Idx0);
				grid.InvalidateRow(data.Idx1);
				grid.InvalidateRow(idx0);
				grid.InvalidateRow(idx1);
			}
			return true;
		}

		/// <summary>A handler used in DGV drag/drop operations when dragging rows around</summary>
		private static void DragDrop_LinePainter(object sender, DataGridViewRowPostPaintEventArgs args)
		{
			var grid = sender.As<DataGridView>();
			var pt = grid.PointToClient(Control.MousePosition);

			// Only paint on the rows the mouse is over
			var hit = grid.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= grid.RowCount)
				return;

			// The row the mouse is over
			var hit_idx = hit.RowIndex;
			var hit_row = grid.Rows[hit_idx];

			// Find the two rows on either side of the line
			var over_half = pt.Y - hit.RowY > 0.5f * hit_row.Height;
			var idx0 = over_half ? hit_idx : hit_idx - 1;
			var idx1 = over_half ? hit_idx + 1 : hit_idx;

			// Only need to draw on the row the mouse is over
			float Y = 0f;
			if      (args.RowIndex == idx0) Y = args.RowBounds.Bottom - 1f;
			else if (args.RowIndex == idx1) Y = args.RowBounds.Top    + 1f;
			else return;

			// Draw a line to show where the drop would go
			const float line_thickness = 2f;
			using (var pen = new Pen(Color.DeepSkyBlue, line_thickness))
				args.Graphics.DrawLine(pen, args.RowBounds.Left, Y, args.RowBounds.Right, Y);
		}

		/// <summary>Temporarily remove the data source from this grid</summary>
		public static Scope PauseBinding(this DataGridView grid)
		{
			var ds = grid.DataSource;
			return Scope.Create(() => grid.DataSource = null, () => grid.DataSource = ds);
		}

		/// <summary>
		/// Attaches 'source' as the data provider for this grid.
		/// Source is connected with weak references so an external reference to 'source' must be held</summary>
		public static void SetVirtualDataSource(this DataGridView grid, IDGVVirtualDataSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source","Source cannot be null");

			var w = new WeakReference(source);
			DataGridViewCellValueEventHandler handler = null;
			handler = (sender,args) =>
				{
					var src = w.Target as IDGVVirtualDataSource;
					if (src == null) grid.CellValueNeeded -= handler;
					else src.DGVCellValueNeeded(sender, args);
				};

			grid.VirtualMode = true;
			grid.CellValueNeeded += handler;
		}
	}

	/// <summary>An interface for a type that provides data for a grid</summary>
	public interface IDGVVirtualDataSource
	{
		void DGVCellValueNeeded(object sender, DataGridViewCellValueEventArgs args);
	}

	/// <summary>Event args for sorting a DGV using the DataGridViewExtensions</summary>
	public class DGVSortEventArgs :EventArgs
	{
		public DGVSortEventArgs(int column_index, SortOrder direction)
		{
			ColumnIndex = column_index;
			Direction  = direction;
		}

		/// <summary>The column to sort</summary>
		public int ColumnIndex { get; private set; }

		/// <summary>The direction to sort it in</summary>
		public SortOrder Direction { get; private set; }
	}
}
