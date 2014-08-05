//***************************************************
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

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

		/// <summary>Select all rows</summary>
		public static void SelectAll(object sender, KeyEventArgs e)
		{
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

		/// <summary>KeyDown handler for copying selected cells to the clipboard</summary>
		public static void Copy(object sender, KeyEventArgs e)
		{
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

		/// <summary>Cut the selected cells to the clipboard. Cut cells replaced with default values</summary>
		public static void Cut(object sender, KeyEventArgs e)
		{
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

		/// <summary>Delete the contents of the selected cells</summary>
		public static void Delete(object sender, KeyEventArgs e)
		{
			var dgv = (DataGridView)sender;
			if (e.KeyCode != Keys.Delete) return;
			Delete(dgv);
		}

		/// <summary>Grid paste implementation that pastes over existing cells within the current size limits of the grid. Must be 1 cell selected only</summary>
		public static bool PasteReplace(DataGridView grid)
		{
			if (grid.SelectedCells.Count != 1) return false;

			// Read the lines from the clipboard
			var lines = Clipboard.GetText().Split('\n');

			var row = grid.CurrentCell.RowIndex;
			var col = grid.CurrentCell.ColumnIndex;

			for (var j = 0; j != lines.Length && row != grid.RowCount; ++j, ++row)
			{
				// Skip blank lines
				if (lines[j].Length == 0) continue;

				var cells = lines[j].Split('\t',',',';');
				for (var i = 0; i != cells.Length && col != grid.ColumnCount; ++i, ++col)
				{
					var cell = grid[col,row];
					if (cell.ReadOnly) continue;
					if (cells[i].Length == 0) continue;
					try { cell.Value = Convert.ChangeType(cells[i], cell.ValueType); }
					catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
				}
			}
			return true;
		}

		/// <summary>Paste over existing cells within the current size limits of the grid. Must be 1 cell selected only</summary>
		public static void PasteReplace(object sender, KeyEventArgs e)
		{
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (!PasteReplace(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Paste over selected cells. Only replaces those selected. Must be >= 2 cells selected</summary>
		public static bool PasteReplaceSelected(DataGridView grid)
		{
			if (grid.SelectedCells.Count < 2) return false;

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

			// Read the lines from the clipboard
			var lines = Clipboard.GetText().Split('\n');
			var cells = new string[lines.Length][]; // cache split rows from the clipboard data

			foreach (DataGridViewCell cell in selected_cells)
			{
				if (cell.ReadOnly) continue;

				var j = cell.RowIndex - min.Y;
				if (j >= lines.Length) continue;
				if (cells[j] == null) cells[j] = lines[j].Split('\t',',',';');

				var i = cell.ColumnIndex - min.X;
				if (i >= cells[j].Length) continue;

				try
				{
					if (cells[j][i].Length != 0)
						cell.Value = Convert.ChangeType(cells[j][i], cell.ValueType);
				}
				catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
			}
			return true;
		}

		/// <summary>Paste over selected cells. Only replaces those selected. Must be >= 2 cells selected</summary>
		public static void PasteReplaceSelected(object sender, KeyEventArgs e)
		{
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (!PasteReplaceSelected(dgv)) return;
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

		/// <summary>Paste from the first selected cell over anything in the way. Grow the grid if necessary</summary>
		public static void PasteGrow(object sender, KeyEventArgs e)
		{
			var dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (!PasteGrow(dgv)) return;
			e.Handled = true;
		}

		/// <summary>Combined handler for cut, copy, and paste replace functions</summary>
		public static void CutCopyPasteReplace(object sender, KeyEventArgs e)
		{
			SelectAll            (sender, e); if (e.Handled) return;
			Cut                  (sender, e); if (e.Handled) return;
			Copy                 (sender, e); if (e.Handled) return;
			PasteReplace         (sender, e); if (e.Handled) return;
			PasteReplaceSelected (sender, e);//if (e.Handled) return;
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

		/// <summary>Resize the columns intelligently based on currently displayed content</summary>
		public static void SetGridColumnSizes(this DataGridView grid, bool auto_hide_single_column_headers = false)
		{
			if (grid.ColumnCount == 0)
				return;

			var grid_width = grid.DisplayRectangle.Width - 2;
			var col_count  = grid.ColumnCount;

			if (auto_hide_single_column_headers)
				grid.ColumnHeadersVisible = col_count > 1;

			// Measure each column's preferred width
			var col_widths = grid.Columns.Cast<DataGridViewColumn>().Select(x => x.FillWeight).ToArray();//Enumerable.Repeat(30f, col_count).ToArray();
			using (var gfx = grid.CreateGraphics())
			{
				foreach (var row in grid.GetRowsWithState(DataGridViewElementStates.Displayed))
				{
					for (int i = 0, iend = Math.Min(col_count, row.Cells.Count); i != iend; ++i)
					{
						// For some reason, cell.GetPreferredSize or col.GetPreferredWidth don't return correct values
						var cell = row.Cells[i];
						SizeF sz;
						if      (cell.Value is string) sz = gfx.MeasureString((string)cell.Value, cell.InheritedStyle.Font);
						else if (cell.Value is Image ) sz = ((Image)cell.Value).Size;
						else sz = SizeF.Empty;

						var w = Maths.Clamp(sz.Width + 10, 30, 64000); // DGV throws if width is greater than 65535
						col_widths[i] = Math.Max(col_widths[i], w);
					}
				}
			}
			var total_width = Math.Max(col_widths.Sum(), 1);

			// Resize columns. If the total width is less than the control width use the control width instead
			var remainder = 0f;
			var scale = Maths.Max(grid_width / total_width, 1f);
			foreach (DataGridViewColumn col in grid.Columns)
			{
				var width = (int)(col_widths[col.Index] * scale + remainder);
				remainder = col_widths[col.Index] * scale - (float)width;
				col.Width = width;
			}
		}

		/// <summary>Return the first selected row, regardless of multi-select grids</summary>
		public static DataGridViewRow FirstSelectedRow(this DataGridView grid)
		{
			var i = grid.Rows.GetFirstRow(DataGridViewElementStates.Selected);
			return i != -1 ? grid.Rows[i] : null;
			//return grid.GetCellCount(DataGridViewElementStates.Selected) != 0 ? grid.SelectedRows[0] : null;
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

		/// <summary>
		/// Begin a row drag-drop operation on the grid.
		/// Attach this method to the MouseDown event on the grid.
		/// Also, attach 'DragDrop_DoDropMoveRow' to the 'DoDrop' handler and set AllowDrop = true.
		/// See the 'DragDrop' class for more info</summary>
		public static void DragDrop_DragRow(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!(grid.DataSource is IList))
				throw new InvalidOperationException("Drag-drop requires a grid with a data source convertable to an IList");

			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < grid.RowCount)
				grid.DoDragDrop(grid.Rows[hit.RowIndex], DragDropEffects.Move|DragDropEffects.Copy|DragDropEffects.Link);
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
			if (grid == null)
				return false;

			// Must allow move and contain a row
			if ((args.AllowedEffect & DragDropEffects.Move) == 0 || !args.Data.GetDataPresent(typeof(DataGridViewRow)))
				return false;

			// Find where the mouse is over the grid
			Point pt = grid.PointToClient(new Point(args.X, args.Y));
			var hit = grid.HitTest(pt.X, pt.Y);

			// Set the drop effect
			args.Effect = hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < grid.RowCount
				? DragDropEffects.Move : DragDropEffects.None;
			if (args.Effect != DragDropEffects.Move)
				return true;

			// The row the mouse is over
			var hit_idx = hit.RowIndex;
			var hit_row = grid.Rows[hit_idx];
			var over_half = pt.Y - hit.RowY > 0.5f * hit_row.Height;
			var before_idx    = hit_idx - 1;
			var after_idx     = hit_idx + 1;
			var neighbour_idx = over_half ? after_idx : before_idx;

			// If this is not the actual drop then keep returning true until the drop happens
			if (mode != DragDrop.EDrop.Drop)
			{
				// Draw a line to show where the drop would go
				// Use a self removing handler so that it doesn't matter how often we add it
				DataGridViewRowPostPaintEventHandler LinePainter = null;
				LinePainter = (s,a) =>
					{
						float Y = 0f;
						if      (a.RowIndex == hit_idx      ) Y = over_half ? a.RowBounds.Bottom - 1f : a.RowBounds.Top + 1f;
						else if (a.RowIndex == neighbour_idx) Y = over_half ? a.RowBounds.Top + 1f : a.RowBounds.Bottom - 1f;
						else return;

						const float line_thickness = 2f;
						using (var pen = new Pen(Color.DeepSkyBlue, line_thickness))
							a.Graphics.DrawLine(pen, a.RowBounds.Left, Y, a.RowBounds.Right, Y);

						grid.RowPostPaint -= LinePainter;
					};

				// Add two, one for each row that needs a line
				grid.RowPostPaint += LinePainter;
				grid.RowPostPaint += LinePainter;

				// Invalidate the hit rows to ensure repainting
				grid.InvalidateRow(hit_idx);
				if (before_idx >= 0          ) grid.InvalidateRow(before_idx);
				if (after_idx < grid.RowCount) grid.InvalidateRow(after_idx);
				return true;
			}

			// Get the drag data
			var row = (DataGridViewRow)args.Data.GetData(typeof(DataGridViewRow));
			int grab_idx = row.Index;
			if (over_half) ++hit_idx;

			// Insert 'grab_idx' at 'drop_idx'
			var list = grid.DataSource as IList;
			if (list == null) throw new InvalidOperationException("Drag-drop requires a grid with a data source bound to an IList");
			if (grab_idx != hit_idx)
			{
				if (hit_idx > grab_idx) --hit_idx;

				var tmp = list[grab_idx];
				list.RemoveAt(grab_idx);
				list.Insert(hit_idx, tmp);
			}

			// If the list is a binding source, set the current position to the item just moved
			var bs = grid.DataSource as BindingSource;
			if (bs != null)
				bs.Position = hit_idx;

			// Invalidate the hit rows to ensure repainting
			grid.InvalidateRow(hit_idx);
			if (before_idx >= 0          ) grid.InvalidateRow(before_idx);
			if (after_idx < grid.RowCount) grid.InvalidateRow(after_idx);
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
}
