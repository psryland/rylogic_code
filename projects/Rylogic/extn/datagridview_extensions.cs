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
using pr.win32;

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

			// Grow the DGV if necessary
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
			// DGVSortDelegate (sub-classed from Delegate) and remove it if 'handle_sort' is null.
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

		/// <summary>
		/// Returns an array of the fill weights for the columns in this grid.
		/// If 'zero_if_not_visible' is true, then invisible columns return a fill weight of zero
		/// If 'normalised' is true, the returned array sums to one (note, zero_if_not_visible applies before normalisation)</summary>
		public static float[] FillWeights(this DataGridView grid, bool zero_if_not_visible = false, bool normalised = false)
		{
			var columns = grid.Columns.Cast<DataGridViewColumn>();
			var fw = columns.Select(x => !zero_if_not_visible || x.Visible ? x.FillWeight : 0f).ToArray();
			if (normalised)
			{
				var sum = fw.Sum();
				for (int i = 0; i != fw.Length; ++i)
					fw[i] /= sum;
			}
			return fw;
		}

		/// <summary>Scale the fill weights so that they sum up to 1f</summary>
		public static void NormaliseFillWeights(this DataGridView grid)
		{
			var sum = grid.Columns.Cast<DataGridViewColumn>().Sum(x => x.FillWeight);
			foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
				col.FillWeight /= sum;
		}

		/// <summary>Set the FillWeights based on the current column widths</summary>
		public static void SetFillWeightsFromCurrentWidths(this DataGridView grid)
		{
			var widths = grid.Columns.Cast<DataGridViewColumn>().Select(x => (float)x.Width).ToArray();
			var sum = widths.Sum();
			for (int i = 0; i != widths.Length; ++i)
				grid.Columns[i].FillWeight = widths[i] / sum;
		}

		/// <summary>Returns the width of a grid available for columns</summary>
		private static int GridDisplayWidth(DataGridView grid)
		{
			const int GridDisplayRectMargin = 2;
			var width = grid.DisplayRectangle.Width - GridDisplayRectMargin;
			if (grid.RowHeadersVisible) width -= grid.RowHeadersWidth;
			return Math.Max(width, 0);
		}

		/// <summary>When a column width is changed, adjust the fill weights to preserve the column's width, squashing up the columns with indices > 'column_index'</summary>
		public static void SetFillWeightsOnColumnWidthChanged(this DataGridView grid, int column_index)
		{
			if (grid.ColumnCount == 0)
				return;

			// Get the display area width and the column widths
			var columns         = grid.Columns.Cast<DataGridViewColumn>();
			var grid_width      = GridDisplayWidth(grid);
			var widths          = columns.Select(x => (float)x.Width).ToArray();
			var min_widths      = columns.Select(x => (float)x.MinimumWidth).ToArray();

			var left_width      = widths.Take(column_index+1).Sum();
			var right_width     = widths.Skip(column_index+1).Sum();
			var right_min_width = min_widths.Skip(column_index+1).Sum();

			// Squish the right hand side columns into the remaining space
			var remaining = Math.Max(grid_width - left_width, right_min_width);
			var scale = remaining / right_width;
			for (int i = column_index+1; i != widths.Length; ++i)
				widths[i] *= scale;

			// Set the fill weights based on the new distribution of widths
			var total_width = widths.Sum();
			for (int i = 0; i != widths.Length; ++i)
				grid.Columns[i].FillWeight = widths[i] / total_width;
		}

		/// <summary>Options for smart column resizing</summary>
		[Flags] public enum EColumnSizeOptions
		{
			None = 0,

			/// <summary>Scale column widths based on their preferred width (which is derived from the cell contents)</summary>
			Preferred = 1 << 0,

			/// <summary>The text in column headers is included when determining the preferred size of a column</summary>
			IncludeColumnHeaders = 1 << 1,

			/// <summary>Columns widths may be expanded to fill the display width</summary>
			GrowToDisplayWidth = 1 << 2,

			/// <summary>Columns widths may be shrunk to fit to the display width</summary>
			ShrinkToDisplayWidth = 1 << 3,

			/// <summary>Columns widths may be expanded or shrunk to fit to the display width</summary>
			FitToDisplayWidth = GrowToDisplayWidth | ShrinkToDisplayWidth,
		}

		/// <summary>Returns an array of ideal widths for the columns based on visibility and FillWeight</summary>
		public static float[] GetColumnWidths(this DataGridView grid, EColumnSizeOptions opts)
		{
			var columns = grid.Columns.Cast<DataGridViewColumn>();

			// Get the visible width available to the columns.
			// Note, with scroll bars are visible, DisplayRectangle excludes their area
			var grid_width = GridDisplayWidth(grid);

			// Get the column fill weights
			var fill_weights = grid.FillWeights(zero_if_not_visible:true, normalised:true);

			// Generate a default set of widths
			// Note, fill weights are relative to the display area, not the area minus row headers
			var widths = fill_weights.Select(x => x*grid_width).ToArray();

			// Measure each column's preferred width
			var pref_widths = widths.ToArray();
			
			// Return the cell's preferred size
			Func<DataGridViewCell,float> MeasurePreferredWidth = (cell) =>
				{
					var sz = cell.PreferredSize;
					var w = Maths.Clamp(sz.Width, 30, 64000); // DGV throws if width is greater than 65535
					return w;
				};

			// Measure the column header cells,
			if (opts.HasFlag(EColumnSizeOptions.IncludeColumnHeaders))
			{
				foreach (var col in columns.Where(x => x.Visible))
					pref_widths[col.Index] = Math.Max(pref_widths[col.Index], MeasurePreferredWidth(col.HeaderCell));
			}

			// Measure the cells in the displayed rows only
			foreach (var row in grid.GetRowsWithState(DataGridViewElementStates.Displayed))
			{
				for (int i = 0, iend = Math.Min(grid.ColumnCount, row.Cells.Count); i != iend; ++i)
				{
					if (!grid.Columns[i].Visible) continue;
					pref_widths[i] = Math.Max(pref_widths[i], MeasurePreferredWidth(row.Cells[i]));
				}
			}

			// Adjust the values in 'widths' based on 'opts'
			if (opts.HasFlag(EColumnSizeOptions.Preferred))
			{
				var total_width = Math.Max(pref_widths.Sum(), 1f);

				// If the total preferred width fits within the display area, and we're allowed to expand the column widths
				// Or, if the total preferred width exceeds the display area, and we're allowed to shrink the column widths
				if ((total_width < grid_width && opts.HasFlag(EColumnSizeOptions.GrowToDisplayWidth  )) ||
					(total_width > grid_width && opts.HasFlag(EColumnSizeOptions.ShrinkToDisplayWidth)))
				{
					// Find the scaling factor that will fit all columns within the display area
					var scale = grid_width / total_width;
					for (int i = 0; i != pref_widths.Length; ++i)
						pref_widths[i] *= scale;
				}
				return pref_widths;
			}
			else
			{
				var total_width = Math.Max(widths.Sum(), 1f);

				// Otherwise, if the total current width fits within the display area, and we're allowed to expand the column widths
				// Or, if the total current width exceeds the display area, and we're allowed to shrink the column widths
				if ((total_width < grid_width && opts.HasFlag(EColumnSizeOptions.GrowToDisplayWidth  )) ||
					(total_width > grid_width && opts.HasFlag(EColumnSizeOptions.ShrinkToDisplayWidth)))
				{
					// Find the scaling factor that will fit all columns within the display area
					var scale = grid_width / total_width;
					for (int i = 0; i != widths.Length; ++i)
						widths[i] *= scale;
				}
				return widths;
			}
		}

		/// <summary>
		/// Resize the columns intelligently based on column content, visibility, and fill weights.
		/// To handle user resized columns, call SetFillWeightsOnColumnWidthChanged() in the OnColumnWidthChanged()
		/// handler like this: <para/>
		///    if (!dgv.SettingGridColumnWidths()) <para/>
		///    { <para/>
		///        dgv.SetFillWeightsOnColumnWidthChanged(a.Column.Index); <para/>
		///        dgv.SetGridColumnSizes(); <para/>
		///    } <para/>
		///</summary>
		public static void SetGridColumnSizes(this DataGridView grid, EColumnSizeOptions opts)
		{
			// Prevent reentrancy
			if (m_set_grid_columns_sizes != null) return;
			using (Scope.Create(() => m_set_grid_columns_sizes = grid, () => m_set_grid_columns_sizes = null))
			{
				// No columns, nothing to resize
				if (grid.ColumnCount == 0)
					return;

				// Get the column widths
				var col_widths = Maths.TruncWithRemainder(grid.GetColumnWidths(opts)).ToArray();

				// Set the column sizes
				using (grid.SuspendLayout(true))
				{
					// Setting 'Width' will fire the OnColumnWidthChanged event
					// Callers should use 'SettingGridColumnWidths' to ignore these events
					foreach (var col in grid.Columns.Cast<DataGridViewColumn>())
						col.Width = col_widths[col.Index];
				}
			}
		}

		/// <summary>True while column widths are being set for this grid</summary>
		public static bool SettingGridColumnWidths(this DataGridView grid)
		{
			return m_set_grid_columns_sizes == grid;
		}
		[ThreadStatic] private static object m_set_grid_columns_sizes;

		/// <summary>
		/// An event handler that resizes the columns in a grid to fill the available space, while preserving user column size changes.
		/// Attach this handler to 'ColumnWidthChanged' and 'RowHeadersWidthChanged' and 'SizeChanged'</summary>
		public static void FitToDisplayWidth(object sender, EventArgs args = null)
		{
			var grid = (DataGridView)sender;
			if (!grid.SettingGridColumnWidths())
			{
				// If this event is a column/row header width changed event,
				// record the user column widths before resizing.
				var a = args as DataGridViewColumnEventArgs;
				if (a != null)
					grid.SetFillWeightsOnColumnWidthChanged(a.Column.Index);

				// Resize columns to fit
				grid.SetGridColumnSizes(EColumnSizeOptions.FitToDisplayWidth);
			}
		}

		/// <summary>An event handler for auto hiding the column header text when there is only one column visible in the grid</summary>
		public static void AutoHideSingleColumnHeader(object sender, EventArgs args)
		{
			var grid = (DataGridView)sender;
			grid.ColumnHeadersVisible = grid.ColumnCount > 1;
		}

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

		/// <summary>RAII object for preserving the scroll position in a grid</summary>
		public static Scope ScrollScope(this DataGridView grid)
		{
			return Scope.Create(
			() => new Point(grid.FirstDisplayedScrollingColumnIndex, grid.FirstDisplayedScrollingRowIndex),
			pos =>
			{
				if (pos.X == -1 || pos.Y == -1) return;
				if (grid.ColumnCount == 0 || grid.RowCount == 0) return;
				grid.FirstDisplayedScrollingColumnIndex = Math.Min(pos.X, grid.ColumnCount - 1);
				grid.FirstDisplayedScrollingRowIndex    = Math.Min(pos.Y, grid.RowCount - 1);
			});
		}

		/// <summary>RAII object for preserving the selected cells/rows in a grid</summary>
		public static Scope SelectionScope(this DataGridView grid)
		{
			switch (grid.SelectionMode)
			{
			default:
				throw new Exception("Unknown DataGridView selection mode");

			case DataGridViewSelectionMode.CellSelect:
			case DataGridViewSelectionMode.ColumnHeaderSelect:
			case DataGridViewSelectionMode.RowHeaderSelect:
				return Scope.Create(
					() => grid.SelectedCells.Cast<DataGridViewCell>().Select(c => new {c.ColumnIndex, c.RowIndex}).ToArray(),
					cells =>
					{
						grid.ClearSelection();
						foreach (var c in cells)
						{
							if (c.ColumnIndex >= grid.ColumnCount) continue;
							if (c.RowIndex >= grid.RowCount) continue;
							grid[c.ColumnIndex, c.RowIndex].Selected = true;
						}
					});

			case DataGridViewSelectionMode.FullRowSelect:
				return Scope.Create(
					() => grid.GetRowsWithState(DataGridViewElementStates.Selected).Select(x => x.Index).ToArray(),
					row_indices =>
					{
						grid.ClearSelection();
						foreach (var r in row_indices)
						{
							if (r >= grid.RowCount) continue;
							grid.Rows[r].Selected = true;
						}
					});
			case DataGridViewSelectionMode.FullColumnSelect:
				return Scope.Create(
					() => grid.GetColumnsWithState(DataGridViewElementStates.Selected).Select(x => x.Index).ToArray(),
					col_indices =>
					{
						grid.ClearSelection();
						foreach (var c in col_indices)
						{
							if (c >= grid.ColumnCount) continue;
							grid.Columns[c].Selected = true;
						}
					});
			}
		}

		/// <summary>Returns an enumerator for accessing rows with the given property</summary>
		public static IEnumerable<DataGridViewRow> GetRowsWithState(this DataGridView grid, DataGridViewElementStates state)
		{
			for (var i = grid.Rows.GetFirstRow(state); i != -1; i = grid.Rows.GetNextRow(i, state))
				yield return grid.Rows[i];
		}

		/// <summary>Returns an enumerator for accessing columns with the given property</summary>
		public static IEnumerable<DataGridViewColumn> GetColumnsWithState(this DataGridView grid, DataGridViewElementStates state, DataGridViewElementStates excl = DataGridViewElementStates.None)
		{
			for (var i = grid.Columns.GetFirstColumn(state, excl); i != null; i = grid.Columns.GetNextColumn(i, state, excl))
				yield return i;
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
		private class DGV_DragDropData :IDisposable
		{
			public DGV_DragDropData(DataGridViewRow row, int x, int y)
			{
				Row = row;
				GrabX = x;
				GrabY = y;
				Indicator = new IndicatorCtrl(row.DataGridView.TopLevelControl as Form);
			}
			public void Dispose()
			{
				Indicator?.Close();
				Indicator = null;
			}

			/// <summary>The row being dragged</summary>
			public DataGridViewRow Row { get; private set; }

			/// <summary>The pixel location of where the row was grabbed (in DGV space)</summary>
			public int GrabX { get; private set; }
			public int GrabY { get; private set; }

			/// <summary>A form used to indicate where the row will be inserted</summary>
			public IndicatorCtrl Indicator { get; private set; }
			public class IndicatorCtrl :Form
			{
				public IndicatorCtrl(Form owner)
				{
					SetStyle(ControlStyles.Selectable, false);
					FormBorderStyle = FormBorderStyle.None;
					StartPosition = FormStartPosition.Manual;
					ShowInTaskbar = false;
					Size = new Size(10,10);
					Ofs = new Size(10, 5);
					Owner = owner;
					Region = GfxExtensions.MakeRegion(0,0, Ofs.Width,Ofs.Height, 0,Ofs.Height*2);
					CreateHandle();
				}
				protected override CreateParams CreateParams
				{
					get
					{
						var cp = base.CreateParams;
						cp.ClassStyle |= Win32.CS_DROPSHADOW;
						cp.Style &= ~Win32.WS_VISIBLE;
						cp.ExStyle |= Win32.WS_EX_NOACTIVATE;// | Win32.WS_EX_LAYERED | Win32.WS_EX_TRANSPARENT;
						return cp;
					}
				}
				protected override bool ShowWithoutActivation
				{
					get { return true; }
				}
				protected override void OnPaint(PaintEventArgs e)
				{
					base.OnPaint(e);
					e.Graphics.FillRectangle(Brushes.DeepSkyBlue, ClientRectangle);
				}
				public new Point Location
				{
					get { return base.Location + Ofs; }
					set { base.Location = value - Ofs; }
				}
				public Size Ofs { get; private set; }
			}
		}

		/// <summary>
		/// Begin a row drag-drop operation on the grid.
		/// Attach this method to the 'MouseDown' event on the grid.
		/// Note: Do not attach to 'MouseMove' as well, DoDragDrop handles mouse move
		/// Also, attach 'DragDrop_DoDropMoveRow' to the 'DoDrop' handler and set AllowDrop = true.
		/// See the 'DragDrop' class for more info</summary>
		public static void DragDrop_DragRow(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!(grid.DataSource is IList))
				throw new InvalidOperationException("Drag-drop requires a grid with a data source convertible to an IList");

			var row_count = grid.RowCount - (grid.AllowUserToAddRows ? 1 : 0);

			// Drag by grabbing row headers
			// Only drag if:
			//  A data row header is hit (i.e. not the 'add' row, or other parts of the grid)
			//  The resize cursor isn't visible
			//  No control keys are down
			var grid_pt = grid.PointToClient(Control.MousePosition);
			var hit = grid.HitTest(grid_pt.X, grid_pt.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader &&
				hit.RowIndex >= 0 && hit.RowIndex < row_count &&
				grid.Cursor != Cursors.SizeNS &&
				Control.ModifierKeys == Keys.None)
			{
				// The grid.MouseDown event calls CellMouseDown (and others) after the drag/drop operation has
				// finished. A consequence is the cell that the drag started from gets 'clicked' causing the selection
				// to change back to that cell. BeginInvoke ensures the drag happens after any selection changes.
				grid.BeginInvoke(() =>
				{
					using (var data = new DGV_DragDropData(grid.Rows[hit.RowIndex], grid_pt.X, grid_pt.Y))
						grid.DoDragDrop(data, DragDropEffects.Move|DragDropEffects.Copy|DragDropEffects.Link);
				});
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
			// events could come from anything. Only accept DGVs
			var grid = sender as DataGridView;
			if (grid == null || args == null)
				return false;

			// Must allow move and contain a row
			if ((args.AllowedEffect & DragDropEffects.Move) == 0 || !args.Data.GetDataPresent(typeof(DGV_DragDropData)))
				return false;

			// Get the drag data
			var data = (DGV_DragDropData)args.Data.GetData(typeof(DGV_DragDropData));
			var row_count = grid.RowCount - (grid.AllowUserToAddRows ? 1 : 0);

			// Check the mouse has moved enough to start dragging
			var scn_pt = new Point(args.X, args.Y);
			var pt = grid.PointToClient(scn_pt); // Find where the mouse is over the grid
			var distsq = Maths.Len2Sq(pt.X - data.GrabX, pt.Y - data.GrabY);
			if (distsq < 25)
				return false;

			// Set the drop effect
			var hit = grid.HitTest(pt.X, pt.Y);
			args.Effect =
				hit.Type == DataGridViewHitTestType.RowHeader &&
				hit.RowIndex >= 0 && hit.RowIndex < row_count && hit.RowIndex != data.Row.Index
				? DragDropEffects.Move : DragDropEffects.None;
			if (args.Effect != DragDropEffects.Move)
				return true;

			// The row the mouse is over
			var hit_idx = hit.RowIndex;
			var hit_row = grid.Rows[hit_idx];

			// Find the two rows on either side of the line
			var over_half = pt.Y - hit.RowY > 0.5f * hit_row.Height;
			var idx = over_half ? hit_idx + 1 : hit_idx;

			// If this is not the actual drop then just update the indicator position
			if (mode != DragDrop.EDrop.Drop)
			{
				// Ensure the indicator is visible
				data.Indicator.Visible = true;
				data.Indicator.Location = grid.PointToScreen(new Point(0, hit.RowY + (over_half ? hit_row.Height : 0)));

				// Auto scroll when at the first or last displayed row
				if (idx <= grid.FirstDisplayedScrollingRowIndex && idx > 0)
					grid.FirstDisplayedScrollingRowIndex--;
				if (idx >= grid.FirstDisplayedScrollingRowIndex + grid.DisplayedRowCount(false) && idx < row_count)
					grid.FirstDisplayedScrollingRowIndex++;
			}

			// Otherwise, this is the drop
			else
			{
				using (grid.SuspendLayout(false))
				{
					var grab_idx = data.Row.Index;

					// Insert 'grab_idx' at 'drop_idx'
					var list = grid.DataSource as IList;
					if (list == null) throw new InvalidOperationException("Drag-drop requires a grid with a data source bound to an IList");
					if (grab_idx != idx)
					{
						if (idx > grab_idx)
							--idx;

						var tmp = list[grab_idx];
						list.RemoveAt(grab_idx);
						list.Insert(idx, tmp);
					}

					// If the list is a binding source, set the current position to the item just moved
					var cm = grid.DataSource as ICurrencyManagerProvider;
					if (cm != null)
						cm.CurrencyManager.Position = idx;

					grid.Invalidate(true);
				}
			}
			return true;
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
