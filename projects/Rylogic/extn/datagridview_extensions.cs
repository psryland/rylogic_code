//***************************************************
//  Copyright (c) Rylogic Ltd 2008
//***************************************************
// DGV helpers
// Notes:
//   Useful link of CellStyle inheritance: https://msdn.microsoft.com/en-us/library/1yef90x0.aspx

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using pr.common;
using pr.container;
using pr.gui;
using pr.maths;
using pr.util;
using pr.win32;
using DataGridView = System.Windows.Forms.DataGridView;

namespace pr.extn
{
	/// <summary>
	/// Grid standard keyboard shortcuts
	/// Use: add for each function you want supported
	///   e.g.
	///   m_grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
	///   m_grid.KeyDown += DataGridViewEx.Cut;
	///   m_grid.ContextMenuStrip.Items.Add("Copy", null, (s,a) => DataGridViewEx.Copy(m_grid, a));
	/// </summary>
	public static class DataGridViewEx
	{
		[Flags] public enum EEditOptions
		{
			Copy      = 1 << 0,
			Cut       = 1 << 1,
			Paste     = 1 << 2,
			Delete    = 1 << 3,
			SelectAll = 1 << 4,

			ReadOnly  = Copy | SelectAll,
			ReadWrite = ReadOnly | Cut | Paste,
			All       = ReadWrite | Delete,
		}

		/// <summary>Grid select all implementation. (for consistency)</summary>
		public static void SelectAll(DataGridView grid)
		{
			grid.SelectAll();
		}

		/// <summary>Select all rows. Attach to the KeyDown handler or use as context menu handler</summary>
		public static void SelectAll(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(Control.ModifierKeys.HasFlag(Keys.Control) && ke.KeyCode == Keys.A)) return; // not ctrl + A
			}
			try
			{
				SelectAll(dgv);
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView select all operation failed: {0}".Fmt(ex.MessageFull()));
			}
		}

		/// <summary>Grid copy implementation. Returns true if something was added to the clipboard</summary>
		public static bool Copy(DataGridView grid)
		{
			var d = grid.GetClipboardContent();
			if (d == null) return false;
			Clipboard.SetDataObject(d);
			return true;
		}

		/// <summary>Copy selected cells to the clipboard. Attach to the KeyDown handler</summary>
		public static void Copy(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(Control.ModifierKeys.HasFlag(Keys.Control) && ke.KeyCode == Keys.C)) return; // not ctrl + C
			}
			try
			{
				if (!Copy(dgv)) return;
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView copy operation failed: {0}".Fmt(ex.MessageFull()));
			}
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
		public static void Cut(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(Control.ModifierKeys.HasFlag(Keys.Control) && ke.KeyCode == Keys.X)) return; // not ctrl + X
			}
			try
			{
				if (!Cut(dgv)) return;
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView cut operation failed: {0}".Fmt(ex.MessageFull()));
			}
		}

		/// <summary>Grid paste implementation that pastes over existing cells within the current size limits of the grid</summary>
		public static bool Paste(DataGridView grid)
		{
			return PasteReplace(grid);
		}

		/// <summary>Grid paste implementation that pastes over existing cells within the current size limits of the grid</summary>
		public static void Paste(object sender, EventArgs e)
		{
			PasteReplace(sender, e);
		}

		/// <summary>Grid delete implementation. Deletes selected items from the grid setting cell values to null</summary>
		public static void Delete(DataGridView grid)
		{
			foreach (DataGridViewCell c in grid.SelectedCells)
				c.Value = null;
		}

		/// <summary>Delete the contents of the selected cells. Attach to the KeyDown handler</summary>
		public static void Delete(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(ke.KeyCode == Keys.Delete)) return; // not the delete key
			}
			try
			{
				Delete(dgv);
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView delete operation failed: {0}".Fmt(ex.MessageFull()));
			}
		}

		/// <summary>Grid paste implementation that pastes over existing cells within the current size limits of the grid</summary>
		public static bool PasteReplace(DataGridView grid)
		{
			// Read the lines from the clipboard
			var lines = Clipboard.GetText().Split('\n');

			if (grid.SelectedCells.Count == 1)
			{
				var row = grid.CurrentCell.RowIndex;
				for (var j = 0; j != lines.Length && row != grid.RowCount; ++j, ++row)
				{
					// Skip blank lines
					if (lines[j].Length == 0) continue;

					var col = grid.CurrentCell.ColumnIndex;
					var cells = lines[j].Split(new[] { ' ','\t',',',';','|' }, StringSplitOptions.RemoveEmptyEntries);
					for (var i = 0; i != cells.Length && col != grid.ColumnCount; ++i, ++col)
					{
						var cell = grid[col,row];
						if (cell.ReadOnly) continue;
						if (cells[i].Length == 0) continue;
						try
						{
							var val = Util.ConvertTo(cells[i].Trim(), cell.ValueType);
							cell.Value = val;
							grid.InvalidateCell(cell);
						}
						catch (FormatException)
						{
							cell.Value = cell.DefaultNewRowValue;
							grid.InvalidateCell(cell);
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
				var dst_dim = new Point(max.X - min.X, max.Y - min.Y);

				// Read the cells from the clipboard
				var cells = new string[lines.Length][];
				var src_dim = new Point(0,0);
				for (var r = 0; r != lines.Length; ++r)
				{
					var row = lines[r].Split(new[] { ' ','\t',',',';','|' }, StringSplitOptions.RemoveEmptyEntries);
					cells[r] = row;
					src_dim.X = Math.Max(src_dim.X, row.Length);
					src_dim.Y = src_dim.Y + 1;
				}

				// Special case 1-dimensional data. If a row vector is pasted
				// into a column, or visa versa, automatically transpose the data.
				if ((src_dim.X == 1 && dst_dim.Y == 1 && src_dim.Y == dst_dim.X) ||
					(src_dim.Y == 1 && dst_dim.X == 1 && src_dim.X == dst_dim.Y))
				{
					var cellsT = new string[src_dim.X][];
					for (var r = 0; r != cellsT.Length; ++r)
					{
						cellsT[r] = new string[cells.Length];
						for (var c = 0; c != cells.Length; ++c)
							cellsT[r][c] = cells[c][r];
					}

					cells = cellsT;
					var tmp = src_dim.X; src_dim.Y = src_dim.X; src_dim.X = tmp;
				}

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
						{
							cell.Value = Convert.ChangeType(cells[row][col], cell.ValueType);
							grid.InvalidateCell(cell);
						}
					}
					catch (FormatException)
					{
						cell.Value = cell.DefaultNewRowValue;
						grid.InvalidateCell(cell);
					}
				}
			}
			return true;
		}

		/// <summary>Paste over existing cells within the current size limits of the grid. Attach to the KeyDown handler</summary>
		public static void PasteReplace(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(Control.ModifierKeys.HasFlag(Keys.Control) && ke.KeyCode == Keys.V)) return; // not ctrl + V
			}
			try
			{
				if (!PasteReplace(dgv)) return;
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView paste (replace) operation failed: {0}".Fmt(ex.MessageFull()));
			}
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
		public static void PasteGrow(object sender, EventArgs e)
		{
			var dgv = (DataGridView)sender;

			var ke = e as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(Control.ModifierKeys.HasFlag(Keys.Control) && ke.KeyCode == Keys.V)) return; // not ctrl + V
			}
			try
			{
				if (!PasteGrow(dgv)) return;
				if (ke != null) ke.Handled = true;
			}
			catch (Exception ex)
			{
				Debug.WriteLine("DataGridView paste (grow) operation failed: {0}".Fmt(ex.MessageFull()));
			}
		}

		/// <summary>Combined handler for cut, copy, and paste replace functions. Attach to the KeyDown handler</summary>
		public static void CutCopyPasteReplace(object sender, EventArgs e)
		{
			var ke = e as KeyEventArgs;
			if (ke != null && ke.Handled) return; // already handled
			SelectAll   (sender, e);
			Cut         (sender, e);
			Copy        (sender, e);
			PasteReplace(sender, e);
		}

		/// <summary>Create a context menu with basic Copy,Cut,Paste,Delete options</summary>
		public static ContextMenuStrip CMenu(DataGridView grid, EEditOptions edit_options)
		{
			var cmenu = new ContextMenuStrip();
			using (cmenu.SuspendLayout(false))
			{
				if (edit_options.HasFlag(EEditOptions.Copy))
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Copy"));
					opt.Click += (s,a) => Copy(grid);
				}
				if (edit_options.HasFlag(EEditOptions.Cut))
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Cut"));
					opt.Click += (s,a) => Cut(grid);
				}
				if (edit_options.HasFlag(EEditOptions.Paste))
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Paste"));
					opt.Click += (s,a) => Paste(grid);
				}
				cmenu.Items.AddSeparator();
				if (edit_options.HasFlag(EEditOptions.Delete))
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Delete"));
					opt.Click += (s,a) => Delete(grid);
				}
				cmenu.Items.AddSeparator();
				if (edit_options.HasFlag(EEditOptions.SelectAll))
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Select All"));
					opt.Click += (s,a) => SelectAll(grid);
				}
				cmenu.Items.TidySeparators();
			}
			return cmenu;
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

		/// <summary>Display a context menu for showing/hiding columns in the grid. Attach to MouseDown</summary>
		public static void ColumnVisibility(object sender, MouseEventArgs args)
		{
			var grid = (DataGridView)sender;

			var hit = grid.HitTestEx(args.X, args.Y);
			if (args.Button == MouseButtons.Right)
			{
				// Right mouse on a column header displays a context menu for hiding/showing columns
				if (hit.Type == DataGridViewHitTestType.ColumnHeader || hit.Type == DataGridViewHitTestType.None)
					grid.ColumnVisibilityContextMenu(hit.GridPoint);
			}
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

			// Grid column auto sizing only works when the grid isn't trying to resize itself
			Debug.Assert(grid.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.None);
			Debug.Assert(
				grid.Columns[column_index].AutoSizeMode == DataGridViewAutoSizeColumnMode.None ||
				grid.Columns[column_index].AutoSizeMode == DataGridViewAutoSizeColumnMode.NotSet);
				
			// Get the display area width and the column widths
			var columns         = grid.Columns.Cast<DataGridViewColumn>();
			var grid_width      = GridDisplayWidth(grid);
			var widths          = columns.Select(x => (float)x.Width).ToArray();
			var min_widths      = columns.Select(x => (float)x.MinimumWidth).ToArray();
			var left_width      = widths.Take(column_index+1).Sum();
			var right_width     = widths.Skip(column_index+1).Sum();
			var left_min_width  = min_widths.Take(column_index+1).Sum();
			var right_min_width = min_widths.Skip(column_index+1).Sum();
			float space, scale;

			// Squish columns to the right, leaving left columns unchanged
			if (Control.ModifierKeys == Keys.None)
			{
				// Squish the right hand side columns into the remaining space
				space = Math.Max(grid_width - left_width, right_min_width);
				scale = space / right_width;
				for (int i = column_index+1; i != widths.Length; ++i)
					widths[i] *= scale;
			}

			// Squish columns to the left, and stretch columns to the right
			else if (Control.ModifierKeys == Keys.Alt)
			{
				var left_weights    = columns.Take(column_index+1).Select(x => x.FillWeight).Normalise().ToArray();
				var right_weights   = columns.Skip(column_index+1).Select(x => x.FillWeight).Normalise().ToArray();

				// Squish the left hand side columns
				space = Math.Max(left_width, left_min_width);
				for (int i = 0; i != column_index+1; ++i)
					widths[i] = left_weights[i] * space;

				// Expand the right hand side columns into the remaining space
				space = Math.Max(grid_width - space, right_min_width);
				for (int i = column_index+1; i != widths.Length; ++i)
					widths[i] = right_weights[i-column_index-1] * space;
			}

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
			// Note, when scroll bars are visible, DisplayRectangle excludes their area
			var grid_width = GridDisplayWidth(grid);

			// Get the column fill weights
			var fill_weights = grid.FillWeights(zero_if_not_visible:true, normalised:true);

			// Generate a default set of widths
			// Note, fill weights are relative to the display area, not the area minus row headers if row headers are visible
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

		/// <summary>
		/// Sets the selection to row 'index' and the current cell to the first cell of that row.
		/// Clears all other selection.
		/// If the grid has rows, clamps 'index' to [-1,RowCount).
		/// If index == -1, the selection is cleared. Returns the row actually selected.
		/// If 'make_displayed' is true, scrolls the grid to make 'index' displayed</summary>
		public static int SelectSingleRow(this DataGridView grid, int index, bool make_displayed = false)
		{
			// Clear the current selection
			grid.ClearSelection();

			// If there are no rows, select nothing
			if (grid.RowCount == 0 || index == -1)
			{
				index = -1;
				grid.CurrentCell = null;
			}

			// Otherwise, select the row index and set the current cell
			else
			{
				index = Maths.Clamp(index, 0, grid.RowCount - 1);
				var row = grid.Rows[index];
				row.Selected = true;
				grid.CurrentCell = row.Cells[0];

				// Scroll the row into view
				if (make_displayed && !row.Displayed)
					grid.FirstDisplayedScrollingRowIndex = index;
			}
			return index;
		}

		/// <summary>Returns an enumerator for accessing rows with the given property.</summary>
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

		/// <summary>Return the number of selected rows (up to 'max_count') (i.e. efficiently test for multiple selections)</summary>
		public static int SelectedRowCount(this DataGridView grid, int max_count = int.MaxValue)
		{
			return grid.SelectedRows().CountAtMost(max_count);
		}

		/// <summary>Return the number of selected columns (up to 'max_count') (i.e. efficiently test for multiple selections)</summary>
		public static int SelectedColumnCount(this DataGridView grid, int max_count = int.MaxValue)
		{
			return grid.SelectedColumns().CountAtMost(max_count);
		}

		/// <summary>Return the selected rows. Note: 'SelectedRows' allocates and populates a collection on every call! This is way more efficient</summary>
		public static IEnumerable<DataGridViewRow> SelectedRows(this DataGridView grid)
		{
			foreach (var r in grid.GetRowsWithState(DataGridViewElementStates.Selected))
				yield return r;
		}

		/// <summary>Return the selected columns. Note: 'SelectedColumns' allocates and populates a collection on every call! This is way more efficient</summary>
		public static IEnumerable<DataGridViewColumn> SelectedColumns(this DataGridView grid)
		{
			foreach (var c in grid.GetColumnsWithState(DataGridViewElementStates.Selected))
				yield return c;
		}

		/// <summary>Return the indices of the selected rows. Note: 'SelectedRows' allocates and populates a collection on every call! This is way more efficient</summary>
		public static IEnumerable<int> SelectedRowIndices(this DataGridView grid)
		{
			foreach (var r in grid.SelectedRows())
				yield return r.Index;
		}

		/// <summary>Return the indices of the selected columns. Note: 'SelectedColumns' allocates and populates a collection on every call! This is way more efficient</summary>
		public static IEnumerable<int> SelectedColumnIndices(this DataGridView grid)
		{
			foreach (var r in grid.SelectedColumns())
				yield return r.Index;
		}

		/// <summary>Return the index of the first selected row (or -1 if no rows are selected). This is the selected row with the minimum row index, *not* the same as SelectedRows[0]</summary>
		public static int FirstSelectedRowIndex(this DataGridView grid)
		{
			return grid.Rows.GetFirstRow(DataGridViewElementStates.Selected);
		}

		/// <summary>Return the index of the last selected row (or -1 if no rows are selected). This is the selected row with the maximum row index, *not* the same as SelectedRows[count-1]</summary>
		public static int LastSelectedRowIndex(this DataGridView grid)
		{
			return grid.Rows.GetLastRow(DataGridViewElementStates.Selected);
		}

		/// <summary>Return the (inclusive) bounds of the selected rows (or [-1,-1] if no rows are selected). Warning: 'Empty' means only 1 row is selected (unless it's -1). Note: independent of the order of SelectedRows</summary>
		public static Range SelectedRowIndexRange(this DataGridView grid)
		{
			return new Range(grid.FirstSelectedRowIndex(), grid.LastSelectedRowIndex());
		}

		/// <summary>Return the first selected row, regardless of multi-select grids</summary>
		public static DataGridViewRow FirstSelectedRow(this DataGridView grid)
		{
			var i = grid.FirstSelectedRowIndex();
			return i != -1 ? grid.Rows[i] : null;
		}

		/// <summary>Return the last selected row, regardless of multi-select grids</summary>
		public static DataGridViewRow LastSelectedRow(this DataGridView grid)
		{
			var i = grid.LastSelectedRowIndex();
			return i != -1 ? grid.Rows[i] : null;
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

		/// <summary>Return this content alignment value adjusted by 'horz' and 'vert'. If non-null, then -1 = Left/Top, 0 = Middle/Centre, +1 = Right/Bottom</summary>
		public static DataGridViewContentAlignment Shift(this DataGridViewContentAlignment align, int? horz = null, int? vert = null)
		{
			// 'align' is a single bit
			int x = 0, y = 0;

			if (horz != null) x = horz.Value + 1;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.TopLeft  |DataGridViewContentAlignment.MiddleLeft  |DataGridViewContentAlignment.BottomLeft  ))) x = 0;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.TopCenter|DataGridViewContentAlignment.MiddleCenter|DataGridViewContentAlignment.BottomCenter))) x = 1;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.TopRight |DataGridViewContentAlignment.MiddleRight |DataGridViewContentAlignment.BottomRight ))) x = 2;

			if (vert != null) y = vert.Value + 1;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.TopLeft   |DataGridViewContentAlignment.TopCenter   |DataGridViewContentAlignment.TopRight   ))) y = 0;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.MiddleLeft|DataGridViewContentAlignment.MiddleCenter|DataGridViewContentAlignment.MiddleRight))) y = 1;
			else if (Bit.AnySet((int)align ,(int)(DataGridViewContentAlignment.BottomLeft|DataGridViewContentAlignment.BottomCenter|DataGridViewContentAlignment.BottomRight))) y = 2;

			return (DataGridViewContentAlignment)(1 << (y*3 + x));
		}

		/// <summary>Return the bounds of this cell in DGV space</summary>
		public static Rectangle CellBounds(this DataGridViewCell cell, bool cut_overflow)
		{
			return cell.DataGridView.GetCellDisplayRectangle(cell.ColumnIndex, cell.RowIndex, cut_overflow);
		}

		/// <summary>Temporarily remove the data source from this grid</summary>
		public static Scope PauseBinding(this DataGridView grid)
		{
			var ds = grid.DataSource;
			return Scope.Create(() => grid.DataSource = null, () => grid.DataSource = ds);
		}

		#region Hit Test

		/// <summary>Hit test the grid (includes more useful information than the normal hit test). 'pt' is in grid-relative coordinates</summary>
		public static HitTestInfo HitTestEx(this DataGridView grid, Point pt)
		{
			return new HitTestInfo(grid, pt);
		}
		public static HitTestInfo HitTestEx(this DataGridView grid, int x, int y)
		{
			return new HitTestInfo(grid, new Point(x, y));
		}
		public class HitTestInfo
		{
			private readonly DataGridView.HitTestInfo m_info;
			public HitTestInfo(DataGridView grid, Point pt)
			{
				Grid = grid;
				GridPoint = pt;
				m_info = grid.HitTest(pt.X, pt.Y);
			}

			/// <summary>The grid that was hit tested</summary>
			public DataGridView Grid { get; private set; }

			/// <summary>The grid relative location of where the hit test was performed</summary>
			public Point GridPoint { get; private set; }

			/// <summary>The cell relative location of where the hit test was performed</summary>
			public Point CellPoint { get { return new Point(GridPoint.X - ColumnX, GridPoint.Y - RowY); } }

			/// <summary>What was hit</summary>
			public DataGridViewHitTestType Type { get { return m_info.Type; } }

			/// <summary>True if a column or row divider was hit</summary>
			public bool Divider
			{
				get
				{
					var fi = m_info.GetType().GetField("typeInternal", BindingFlags.Instance | BindingFlags.NonPublic);
					var value = fi.GetValue(m_info).ToString();
					return
						value.Equals("RowResizeTop") || value.Equals("RowResizeBottom") ||
						value.Equals("ColumnResizeLeft") || value.Equals("ColumnResizeRight");
				}
			}

			/// <summary>Gets the index of the column that contains the hit test point</summary>
			public int ColumnIndex { get { return m_info.ColumnIndex; } }

			/// <summary>Gets the index of the row that contains the hit test point</summary>
			public int RowIndex { get { return m_info.RowIndex; } }

			/// <summary>Gets the x-coordinate of the column 'ColumnIndex'</summary>
			public int ColumnX { get { return m_info.ColumnX; } }

			/// <summary>Gets the y-coordinate of the row 'RowIndex'</summary>
			public int RowY { get { return m_info.RowY; } }

			/// <summary>The X,Y grid-relative coordinate of the hit cell</summary>
			public Point CellPosition { get { return new Point(ColumnX, RowY); } }

			/// <summary>
			/// Gets the index of the row that is closest to the hit location.
			/// If the mouse is over the top half of a row, then the row index is returned.
			/// If the mouse is over the lower half of the row, then the next row index is returned.</summary>
			public int InsertIndex
			{
				get
				{
					if (Row == null) return RowIndex;
					var over_half = GridPoint.Y - RowY > 0.5f * Row.Height;
					return RowIndex + (over_half ? 1 : 0);
				}
			}

			/// <summary>The y-coordinate of the top of the insert row</summary>
			public int InsertY
			{
				get
				{
					if (Row == null) return RowY;
					var over_half = GridPoint.Y - RowY > 0.5f * Row.Height;
					return RowY + (over_half ? Row.Height : 0);
				}
			}

			/// <summary>Get the hit header cell (or null)</summary>
			public DataGridViewHeaderCell HeaderCell
			{
				get { return RowIndex == -1 ? Grid.Columns[ColumnIndex].HeaderCell : null; }
			}

			/// <summary>Get the hit data cell (or null)</summary>
			public DataGridViewCell Cell
			{
				get
				{
					DataGridViewCell cell;
					return Grid.Within(ColumnIndex, RowIndex, out cell) ? cell : null;
				}
			}

			/// <summary>Get the hit Column (or null)</summary>
			public DataGridViewColumn Column
			{
				get { return ColumnIndex >= 0 && ColumnIndex < Grid.ColumnCount ? Grid.Columns[ColumnIndex] : null; }
			}

			/// <summary>Get the hit row (or null)</summary>
			public DataGridViewRow Row
			{
				get { return RowIndex >= 0 && RowIndex < Grid.RowCount ? Grid.Rows[RowIndex] : null; }
			}

			/// <summary>Implicit conversion to 'DataGridView.HitTestInfo'</summary>
			public static implicit operator DataGridView.HitTestInfo(HitTestInfo hit) { return hit.m_info; }
		}

		#endregion

		#region Drag/Drop

		/// <summary>Data used for dragging rows around</summary>
		public class DragDropData :IDisposable
		{
			public DragDropData(DataGridViewRow row, int x, int y)
			{
				Row = row;
				GrabX = x;
				GrabY = y;
				StartRowIndex = row.Index;
				Dragging = false;
				Indicator = new IndicatorCtrl(row.DataGridView.TopLevelControl as Form);
			}
			public void Dispose()
			{
				Indicator?.Close();
				Indicator = null;
			}

			/// <summary>The grid that owns 'Row'</summary>
			public DataGridView DataGridView { get { return Row?.DataGridView; } }

			/// <summary>The row being dragged</summary>
			public DataGridViewRow Row { get; private set; }

			/// <summary>The pixel location of where the row was grabbed (in DGV space)</summary>
			public int GrabX { get; private set; }
			public int GrabY { get; private set; }

			/// <summary>The row index of 'Row' when the drag operation began</summary>
			public int StartRowIndex { get; private set; }

			/// <summary>True once the mouse has moved enough to be detected as a drag operation</summary>
			public bool Dragging { get; set; }

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
		/// Begin a row drag-drop operation on the grid. Works on RowHeaders.
		/// Attach this method to the 'MouseDown' event on the grid.
		/// Note: Do not attach to 'MouseMove' as well, DoDragDrop handles mouse move
		/// Also, attach 'DragDrop_DoDropMoveRow' to the 'DoDrop' handler and set AllowDrop = true.
		/// See the 'DragDrop' class for more info</summary>
		public static void DragDrop_DragRow(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
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
					using (var data = new DragDropData(grid.Rows[hit.RowIndex], grid_pt.X, grid_pt.Y))
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
			return DragDrop_DoDropMoveRow(sender, args, mode, (grid, grab, hit) =>
			{
				// Insert 'grab_idx' at 'drop_idx'
				var list = grid.DataSource as IList;
				if (list == null)
					throw new InvalidOperationException("Drag-drop requires a grid with a data source bound to an IList");

				var grab_idx = grab.Row.Index;
				var drop_idx = hit.InsertIndex;
				if (grab_idx != drop_idx)
				{
					// Allow for the index value to change when 'grap_idx' is removed
					if (drop_idx > grab_idx)
						--drop_idx;

					var tmp = list[grab_idx];
					list.RemoveAt(grab_idx);
					list.Insert(drop_idx, tmp);
				}

				// If the list is a binding source, set the current position to the item just moved
				var cm = grid.DataSource as ICurrencyManagerProvider;
				if (cm != null)
					cm.CurrencyManager.Position = drop_idx;
			});
		}
		public static bool DragDrop_DoDropMoveRow(object sender, DragEventArgs args, DragDrop.EDrop mode, Action<DataGridView, DragDropData, HitTestInfo> OnDrop)
		{
			// Must allow move
			if (!args.AllowedEffect.HasFlag(DragDropEffects.Move))
				return false;

			// This method could be hooked up to a pr.util.DragDrop so the events could come from anything.
			// Only accept a DGV that contains the row in 'data'
			var grid = sender as DataGridView;
			var data = (DragDropData)args.Data.GetData(typeof(DragDropData));
			if (grid == null || data == null || !ReferenceEquals(grid, data.DataGridView))
				return false;

			var row_count = grid.RowCount - (grid.AllowUserToAddRows ? 1 : 0);
			var scn_pt = new Point(args.X, args.Y);
			var pt = grid.PointToClient(scn_pt); // Find where the mouse is over the grid

			// Check the mouse has moved enough to start dragging
			if (!data.Dragging)
			{
				var distsq = Maths.Len2Sq(pt.X - data.GrabX, pt.Y - data.GrabY);
				if (distsq < 25) return false;
				data.Dragging = true;
			}

			// Set the drop effect
			var hit = grid.HitTestEx(pt.X, pt.Y);
			args.Effect =
				hit.Type == DataGridViewHitTestType.RowHeader &&
				hit.RowIndex >= 0 && hit.RowIndex < row_count && hit.RowIndex != data.Row.Index
				? DragDropEffects.Move : DragDropEffects.None;
			if (args.Effect != DragDropEffects.Move)
				return true;

			// If this is not the actual drop then just update the indicator position
			if (mode != DragDrop.EDrop.Drop)
			{
				// Ensure the indicator is visible
				data.Indicator.Visible = true;
				data.Indicator.Location = grid.PointToScreen(new Point(0, hit.InsertY));

				// Auto scroll when at the first or last displayed row
				var idx = hit.InsertIndex;
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
					// Handle the drop action
					OnDrop(grid, data, hit);

					grid.Invalidate(true);
				}
			}
			return true;
		}

		#endregion

		#region Filter Columns

		/// <summary>Live instances of ColumnFiltersData objects in use by grids</summary>
		[ThreadStatic] private static Dictionary<DataGridView, ColumnFiltersData> m_column_filters = new Dictionary<DataGridView, ColumnFiltersData>();

		/// <summary>Per-grid object for managing column filters</summary>
		public class ColumnFiltersData :IMessageFilter ,IDisposable
		{
			/// <summary>The grid whose data is to be filtered</summary>
			private DataGridView m_dgv;

			/// <summary>Preserves the state of the DGV</summary>
			private Dictionary<string, object> m_dgv_state;

			/// <summary>The data source that the grid had before filters were used</summary>
			private object m_original_src;

			public ColumnFiltersData(DataGridView dgv)
			{
				Debug.Assert(dgv.DataSource != null, "Column filters requires a data source");

				m_dgv          = dgv;
				m_dgv_state    = new Dictionary<string, object>();
				m_header_cells = new FilterHeaderCell[dgv.ColumnCount];
				m_original_src = dgv.DataSource;
				BSFilter       = null;
				ShortcutKey    = Keys.F;

				m_dgv.DataSourceChanged += HandleDataSourceChanged;
				m_dgv.Disposed += HandleGridDisposed;

				HandleDataSourceChanged();
			}
			public void Dispose()
			{
				Enabled = false;
				m_dgv.DataSourceChanged -= HandleDataSourceChanged;
				m_dgv.Disposed -= HandleGridDisposed;
				m_header_cells = Util.DisposeAll(m_header_cells);
				BSFilter = null;
			}

			/// <summary>The column header cells corresponding to the columns in the associated grid</summary>
			public FilterHeaderCell[] HeaderCells { get { return m_header_cells; } }
			private FilterHeaderCell[] m_header_cells;

			/// <summary>Contains a BindingSource<> (created from the given DGV) and creates 'Views' of the binding source based on filter predicates</summary>
			private BindingSourceFilter BSFilter
			{
				get { return m_impl_bs_filter; }
				set
				{
					if (m_impl_bs_filter == value) return;
					Util.Dispose(ref m_impl_bs_filter);
					m_impl_bs_filter = value;
				}
			}
			private BindingSourceFilter m_impl_bs_filter;
			private class BindingSourceFilter :IDisposable
			{
				/// <summary>The bound data properties for the columns</summary>
				private Dictionary<string,MethodInfo> m_elem_props;

				/// <summary>The method on BindingSource<> for creating views</summary>
				private MethodInfo m_create_view;

				/// <summary>The binding source instance from which we create filtered views</summary>
				private object m_bs;

				public BindingSourceFilter(DataGridView dgv)
				{
					Debug.Assert(dgv.DataSource != null, "DGV requires a data source ");

					var original_src = dgv.DataSource;
					var orig_ty = original_src.GetType();

					// Get the element properties for each column
					var elem_ty = GetElementType(original_src);
					m_elem_props = new Dictionary<string, MethodInfo>();
					foreach (var col in dgv.Columns.OfType<DataGridViewColumn>())
					{
						// Silently ignores 'DataPropertyName' values that aren't valid properties on the element type
						if (!col.DataPropertyName.HasValue()) continue;
						var mi = elem_ty.GetProperty(col.DataPropertyName)?.GetGetMethod();
						if (mi == null) continue;
						m_elem_props[col.DataPropertyName] = mi;
					}

					// Get the binding source type we need to make views from
					var bs_ty = typeof(BindingSource<>).MakeGenericType(elem_ty);

					// If the original source is a BindingSource<>, use it otherwise create a binding source with 'original_src' as it's data source
					m_bs = orig_ty == bs_ty ? original_src : Activator.CreateInstance(bs_ty, original_src, (string)null);

					// Get the CreateView method on the binding source
					m_create_view = bs_ty.GetMethod(nameof(BindingSource<int>.CreateView), new[] { typeof(Func<,>).MakeGenericType(elem_ty, typeof(bool)) });
				}
				public void Dispose()
				{
					FilteredView = null;
				}

				/// <summary>The filtered view of the binding source</summary>
				public object FilteredView
				{
					get { return m_impl_view; }
					set
					{
						if (m_impl_view == value) return;
						if (m_impl_view != null) Util.Dispose((IDisposable)m_impl_view);
						m_impl_view = value;
					}
				}
				private object m_impl_view;

				/// <summary>Get the element type from 'original_src'</summary>
				private Type GetElementType(object original_src)
				{
					var src_ty = original_src.GetType();

					// Array/pointer source
					if (src_ty.HasElementType)
						return src_ty.GetElementType();

					// An IList<> source
					var face = src_ty.GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));
					if (face != null)
						return face.GetGenericArguments()[0];

					// A non-empty IList source
					var list = original_src as IList;
					if (list != null && list.Count != 0)
						return list[0].GetType();

					// Dunno, but the grid seems to know
					//if (dgv.RowCount != 0 && dgv.Rows[0].DataBoundItem != null)
					//	return dgv.Rows[0].DataBoundItem.GetType();

					throw new Exception("Could not determine the element type for the data source type {0}".Fmt(original_src.GetType().Name));
				}

				/// <summary>Create a view of the data from the binding source using the given filter</summary>
				public void SetFilter(Func<object,bool> filter)
				{
					FilteredView = m_create_view.Invoke(m_bs, new object[] { filter });
				}

				/// <summary>Return the value of property 'prop_name' on 'item' as a string</summary>
				public string GetValue(object item, string prop_name)
				{
					MethodInfo mi;
					return m_elem_props.TryGetValue(prop_name, out mi) ? mi.Invoke(item, null).ToString() : string.Empty;
				}
			}

			/// <summary>Enable/Disable the column filters</summary>
			public bool Enabled
			{
				get { return m_enabled; }
				set
				{
					if (m_enabled == value) return;
					m_enabled = value;
					if (m_enabled)
					{
						// Save the data source that the grid starts with
						m_original_src = m_dgv.DataSource;

						// Preserve the current state of the DGV
						m_dgv_state[nameof(m_dgv.ColumnHeadersHeightSizeMode)]   = m_dgv.ColumnHeadersHeightSizeMode;
						m_dgv_state[nameof(m_dgv.ColumnHeadersHeight)]           = m_dgv.ColumnHeadersHeight;
						m_dgv_state[nameof(m_dgv.ColumnHeadersDefaultCellStyle)] = m_dgv.ColumnHeadersDefaultCellStyle;

						// Set the column header height and text alignment
						m_dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
						m_dgv.ColumnHeadersHeight += FilterHeaderCell.FieldHeight;
						m_dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle(m_dgv.ColumnHeadersDefaultCellStyle)
						{
							Alignment = m_dgv.ColumnHeadersDefaultCellStyle.Alignment.Shift(vert:-1), // Align to the top of the cell
						};

						// Ensure the array of header cells equals the number of columns
						if (m_header_cells.Length > m_dgv.ColumnCount)
							Util.DisposeRange(m_header_cells, m_dgv.ColumnCount, m_header_cells.Length - m_dgv.ColumnCount);
						Array.Resize(ref m_header_cells, m_dgv.ColumnCount);

						// Create header cells for the current columns in the grid
						foreach (var col in m_dgv.Columns.OfType<DataGridViewColumn>())
						{
							m_header_cells[col.Index] = m_header_cells[col.Index] ?? new FilterHeaderCell(this, col.HeaderCell, OnPatternChanged);
							col.HeaderCell = m_header_cells[col.Index];
						}

						// Shift focus to the filter for the current cell
						if (m_dgv.CurrentCell != null)
							m_header_cells[m_dgv.CurrentCell.ColumnIndex].EditFilter();

						// Update the binding source for the grid to the filtered source
						OnPatternChanged();

						// Start watching for mouse clicks on the filter fields
						Application.AddMessageFilter(this);
					}
					else
					{
						// Stop watching for mouse clicks on the search field
						Application.RemoveMessageFilter(this);

						// Restore the original column header cells
						foreach (var col in m_dgv.Columns.OfType<DataGridViewColumn>())
						{
							var cell = (FilterHeaderCell)col.HeaderCell;
							col.HeaderCell = cell.OriginalHeaderCell;
						}

						// Restore the DGV state
						if (m_dgv.IsHandleCreated)
						{
							m_dgv.ColumnHeadersHeight           = (int)m_dgv_state[nameof(m_dgv.ColumnHeadersHeight)];
							m_dgv.ColumnHeadersHeightSizeMode   = (DataGridViewColumnHeadersHeightSizeMode)m_dgv_state[nameof(m_dgv.ColumnHeadersHeightSizeMode)];
							m_dgv.ColumnHeadersDefaultCellStyle = (DataGridViewCellStyle)m_dgv_state[nameof(m_dgv.ColumnHeadersDefaultCellStyle)];
						}

						// Restore back to the original data source
						m_dgv.DataSource = m_original_src;
					}
				}
			}
			private bool m_enabled;

			/// <summary>The keyboard shortcut that enables/disables filters. Defaults to 'Ctrl+F'</summary>
			public Keys ShortcutKey { get; set; }

			/// <summary>
			/// Pre-filter mouse down messages so we can intercept clicks on the filter field before
			/// the grid handles them as column clicks</summary>
			public bool PreFilterMessage(ref Message m)
			{
				// Watch for mouse down events for our grid
				if (m.HWnd == m_dgv.Handle && m.Msg == Win32.WM_LBUTTONDOWN)
				{
					// If the mouse down position is within a FilterHeaderCell
					var pt = Win32.LParamToPoint(m.LParam);
					var hit = m_dgv.HitTestEx(pt);
					if (hit.Type == DataGridViewHitTestType.ColumnHeader && hit.HeaderCell is FilterHeaderCell)
					{
						// If the mouse down position is within the filter field of the header cell
						var filter_cell = (FilterHeaderCell)hit.HeaderCell;
						var bounds = filter_cell.FieldBounds.Shifted(hit.CellPosition);
						if (bounds.Contains(pt))
						{
							// Edit the filter field
							// BeginInvoke so that Editing starts after the message queue has processed the mouse events
							m_dgv.BeginInvoke(filter_cell.EditFilter);
							m_eat_lbuttonup = true;
							return true;
						}
					}
				}
				// Consume the mouse up event after an edit filter click
				if (m.HWnd == m_dgv.Handle && m.Msg == Win32.WM_LBUTTONUP && m_eat_lbuttonup)
				{
					m_eat_lbuttonup = false;
					return true;
				}
				return false;
			}
			private bool m_eat_lbuttonup;

			/// <summary>Handle the filter string changing in one of the FilterHeaderCells</summary>
			private void OnPatternChanged(object sender = null, EventArgs args = null)
			{
				// If enabled, use the filtered binding source, otherwise use the original source
				if (Enabled)
				{
					try
					{
						// Create a view filter function
						Func<object,bool> filter = item =>
						{
							for (var i = 0; i != m_header_cells.Length; ++i)
							{
								// If the pattern for this column is not valid, assume no filter
								var patn = m_header_cells[i]?.Pattern;
								if (patn == null || !patn.IsValid || !patn.Expr.HasValue()) continue;

								// Read the value from the source
								var val = BSFilter.GetValue(item, m_header_cells[i].OwningColumn.DataPropertyName);
								var str = val.ToString();

								// Filter out the item if it doesn't match the pattern
								if (!patn.IsMatch(str))
									return false;
							}
							return true;
						};

						// Set the view as the data source for the grid
						BSFilter.SetFilter(filter);
						m_dgv.DataSource = BSFilter.FilteredView;
					}
					catch (Exception ex)
					{
						throw new Exception("Failed to create a binding source view with the given column filters", ex);
					}
				}
				else
				{
					m_dgv.DataSource = m_original_src;
				}
			}

			/// <summary>When the associated grid is disposed, remove this instance of column filters</summary>
			private void HandleGridDisposed(object sender, EventArgs e)
			{
				m_column_filters.Remove(m_dgv);
				Dispose();
			}

			/// <summary>If the data source on the DGV changes, we need to reset the BSFilter</summary>
			private void HandleDataSourceChanged(object sender = null, EventArgs e = null)
			{
				// Remember, while filtering is enabled the DGV's data source will be 'm_bs.FilteredView'
				if (BSFilter != null && m_dgv.DataSource == BSFilter.FilteredView)
				{}

				// If we don't currently have a bs_filter, and the grid has a data source, then create one
				else if (BSFilter == null && m_dgv.DataSource != null)
					BSFilter = new BindingSourceFilter(m_dgv);

				// Otherwise, if the grid no longer has a source, release our runtime data source
				else if (BSFilter != null && m_dgv.DataSource == null)
					BSFilter = null;
			}

			/// <summary>Custom column header cell for showing the filter string text box</summary>
			public class FilterHeaderCell :DataGridViewColumnHeaderCell
			{
				public const int FieldHeight = 18;

				public FilterHeaderCell(ColumnFiltersData cfd, DataGridViewColumnHeaderCell header_cell, EventHandler on_pattern_changed = null)
				{
					OriginalHeaderCell = header_cell;
					Pattern = new Pattern(EPattern.Substring, string.Empty) { IgnoreCase = true };
					Value = OriginalHeaderCell.Value;
					EditCtrl = new EditControl(cfd);

					ContextMenuStrip = new ContextMenuStrip();
					ContextMenuStrip.Items.Add2("Substring", null, (s,a) => Pattern.PatnType = EPattern.Substring        );
					ContextMenuStrip.Items.Add2("Wildcard" , null, (s,a) => Pattern.PatnType = EPattern.Wildcard         );
					ContextMenuStrip.Items.Add2("Regex"    , null, (s,a) => Pattern.PatnType = EPattern.RegularExpression);
					ContextMenuStrip.Items.Add2(new ToolStripSeparator());
					ContextMenuStrip.Items.Add2("Properties", null, (s,a) => ShowPatternUI());
					ContextMenuStrip.Opening += (s,a) =>
					{
						((ToolStripMenuItem)ContextMenuStrip.Items[0]).Checked = Pattern.PatnType == EPattern.Substring        ;
						((ToolStripMenuItem)ContextMenuStrip.Items[1]).Checked = Pattern.PatnType == EPattern.Wildcard         ;
						((ToolStripMenuItem)ContextMenuStrip.Items[2]).Checked = Pattern.PatnType == EPattern.RegularExpression;
					};

					if (on_pattern_changed != null)
						PatternChanged += on_pattern_changed;
				}
				protected override void Dispose(bool disposing)
				{
					Pattern = null;
					EditCtrl.Dispose();
					base.Dispose(disposing);
				}

				/// <summary>The column header cell that this cell replaced</summary>
				public DataGridViewColumnHeaderCell OriginalHeaderCell { get; private set; }

				/// <summary>Returns the cell-relative area of the search field</summary>
				public Rectangle FieldBounds
				{
					get { return new Rectangle(1, Size.Height - FieldHeight - 2, Size.Width - 3, FieldHeight); }
				}

				/// <summary>The search text for this column</summary>
				public Pattern Pattern
				{
					get { return m_pattern; }
					set
					{
						if (m_pattern == value) return;
						if (m_pattern != null)
						{
							m_pattern.PatternChanged -= HandlePatternChanged;
						}
						m_pattern = value;
						if (m_pattern != null)
						{
							m_pattern.PatternChanged += HandlePatternChanged;
						}
						HandlePatternChanged();
					}
				}
				private Pattern m_pattern;

				/// <summary>Raised when the pattern expression changes</summary>
				public event EventHandler PatternChanged;
				private void HandlePatternChanged(object sender = null, EventArgs args = null)
				{
					PatternChanged.Raise(this);
					ToolTipText = Pattern != null && !Pattern.IsValid ? Pattern.ValidateExpr().Message : null;
				}

				/// <summary>The text box for the filter string in this cell</summary>
				public TextBox FilterTextBox
				{
					get { return EditCtrl.TextBox; }
				}

				/// <summary>The control used to edit the filter field</summary>
				private EditControl EditCtrl { get; set; }
				private class EditControl :ToolForm
				{
					private readonly ColumnFiltersData m_cfd;

					public EditControl(ColumnFiltersData cfd) :base()
					{
						FormBorderStyle = FormBorderStyle.None;
						StartPosition = FormStartPosition.Manual;
						ShowInTaskbar = false;
						HideOnClose = true;
						MinimumSize = new Size(1,1);

						m_cfd = cfd;

						var tb = Controls.Add2(new TextBox{ Dock = DockStyle.Fill, AcceptsTab = true, AcceptsReturn = true, Multiline = true });
						tb.KeyDown += HandleKeyDown;
						tb.LostFocus += Hide;
					}
					protected override void Dispose(bool disposing)
					{
						Cell = null;
						base.Dispose(disposing);
					}
					protected override CreateParams CreateParams
					{
						get
						{
							var cp = base.CreateParams;
							cp.ClassStyle &= ~Win32.CS_DROPSHADOW;
							return cp;
						}
					}

					/// <summary>The text box on the edit control</summary>
					public TextBox TextBox
					{
						get { return (TextBox)Controls[0]; }
					}

					/// <summary>The cell being edited</summary>
					public FilterHeaderCell Cell
					{
						get { return m_cell; }
						set
						{
							if (m_cell == value) return;
							if (m_cell != null)
							{
								TextBox.TextChanged -= HandleTextChanged;
								TextBox.Text = string.Empty;
								PinTarget = null;
							}
							m_cell = value;
							if (m_cell != null)
							{
								TextBox.Text = m_cell.Pattern.Expr;
								TextBox.TextChanged += HandleTextChanged;

								// Position the control over the cell
								PinTarget = DGV;
								var cell_bounds = m_cell.CellBounds(false);
								Bounds = DGV.RectangleToScreen(m_cell.FieldBounds.Shifted(cell_bounds.TopLeft()));
							}
						}
					}
					private FilterHeaderCell m_cell;

					/// <summary>The data grid view that 'Cell' belongs to</summary>
					public DataGridView DGV
					{
						get { return Cell.DataGridView; }
					}

					/// <summary>Show the edit control within 'cell'</summary>
					public void Show(FilterHeaderCell cell)
					{
						Cell = cell;
						TextBox.Select(TextBox.TextLength, 0);
						base.Show(DGV);
					}
					public override void Hide()
					{
						base.Hide();
						Cell = null;
					}

					/// <summary>Update the cell pattern when the filter text changes</summary>
					private void HandleTextChanged(object sender, EventArgs args)
					{
						Cell.Pattern.Expr = TextBox.Text;
						TextBox.BackColor = Cell.Pattern.IsValid ? SystemColors.Window : Color.LightSalmon;
					}

					/// <summary>Handle keys in the filter field</summary>
					private void HandleKeyDown(object sender, KeyEventArgs args)
					{
						// On Tab, select the next/prev column
						if (args.KeyCode == Keys.Tab)
						{
							var i = Cell.ColumnIndex;
							var next = i+1 < DGV.ColumnCount ? DGV.Columns[i+1].HeaderCell as FilterHeaderCell : null;
							var prev = i-1 > -1              ? DGV.Columns[i-1].HeaderCell as FilterHeaderCell : null;
							if (!args.Shift && next != null) DGV.BeginInvoke(next.EditFilter);
							if ( args.Shift && prev != null) DGV.BeginInvoke(prev.EditFilter);
						}

						// Close on these keys
						if (args.KeyCode == Keys.Enter || args.KeyCode == Keys.Escape || args.KeyCode == Keys.Tab || (args.KeyCode == m_cfd.ShortcutKey && args.Control))
						{
							Hide();
						}
					}
				}

				/// <summary>Edit the value of the filter</summary>
				public void EditFilter()
				{
					DataGridView.EndEdit();
					EditCtrl.Show(this);
				}

				/// <summary>Show a small UI for editing the pattern</summary>
				public void ShowPatternUI()
				{
					var p = new PatternUI { Dock = DockStyle.Fill };
					using (var f = p.FormWrap(title: "Edit Pattern", loc: DataGridView.PointToScreen(this.CellBounds(false).BottomLeft())))
					{
						p.EditPattern(Pattern);
						p.Commit += (s,a) =>
						{
							Pattern = p.Pattern;
							f.Close();
							DataGridView.InvalidateCell(this);
						};
						f.ShowDialog(DataGridView);
					}
				}

				/// <summary>Paint the custom cell</summary>
				protected override void Paint(Graphics graphics, Rectangle clipBounds, Rectangle cell_bounds, int rowIndex, DataGridViewElementStates dataGridViewElementState, object value, object formattedValue, string errorText, DataGridViewCellStyle cellStyle, DataGridViewAdvancedBorderStyle advancedBorderStyle, DataGridViewPaintParts paintParts)
				{
					base.Paint(graphics, clipBounds, cell_bounds, rowIndex, dataGridViewElementState, value, formattedValue, errorText, cellStyle, advancedBorderStyle, paintParts);
		
					// Paint the search string text box
					var font = DataGridView.ColumnHeadersDefaultCellStyle.Font;
					var bounds = FieldBounds.Shifted(cell_bounds.Left, cell_bounds.Top);
					graphics.FillRectangle(Pattern.IsValid ? SystemBrushes.Window : Brushes.LightSalmon, bounds);
					graphics.DrawRectangle(Pens.Black, bounds.Inflated(0,0,-1,-1));
					graphics.DrawString(Pattern.Expr, font, Brushes.Black, bounds.Shifted(0, Math.Max(0, (bounds.Height - font.Height)/2)));
				}
			}
		}

		/// <summary>
		/// Get the column filters associated with this grid.
		/// To remove column filters, just Dispose the returned instance.
		/// Column filters are automatically disposed when the associated grid is disposed</summary>
		public static ColumnFiltersData ColumnFilters(this DataGridView grid, bool create_if_necessary = false)
		{
			ColumnFiltersData column_filters;
			if (m_column_filters.TryGetValue(grid, out column_filters)) return column_filters;
			if (create_if_necessary) return m_column_filters[grid] = new ColumnFiltersData(grid);
			return null;
		}

		/// <summary>Toggle column filter fields on/off for a grid. Attach this method to KeyDown and ensure 'ColumnFilters(create_if_necessary:true)' has been called</summary>
		public static void ColumnFilters(object sender, EventArgs args)
		{
			var grid = (DataGridView)sender;

			// Get the associated column filters
			var cf = grid.ColumnFilters();
			if (cf == null)
				return; // not enabled

			// If this is a KeyDown handler, check for the required shortcut keys
			var ke = args as KeyEventArgs;
			if (ke != null)
			{
				if (ke.Handled) return; // already handled
				if (!(ke.Control && ke.KeyCode == cf.ShortcutKey)) return; // wrong key shortcut
				ke.Handled = true;
			}

			// Toggle column filter fields
			cf.Enabled = !cf.Enabled;
		}

		#endregion

		#region Virtual Data Source

		/// <summary>An interface for a type that provides data for a grid</summary>
		public interface IDGVVirtualDataSource
		{
			void DGVCellValueNeeded(object sender, DataGridViewCellValueEventArgs args);
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

		#endregion
	}

	/// <summary>Event args for sorting a DGV using the DataGridViewEx</summary>
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
