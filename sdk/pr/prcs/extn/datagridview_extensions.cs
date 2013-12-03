//***************************************************
//  Copyright © Rylogic Ltd 2008
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
		/// If index == -1, the selection is cleared. Returns the row actually selected.</summary>
		public static int SelectRow(this DataGridView grid, int index)
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
				grid.Rows[index].Selected = true;
				grid.CurrentCell = grid.Rows[index].Cells[0];
			}
			return index;
		}

		/// <summary>Return the index of the first selected row (or -1) if no rows are selected</summary>
		public static int FirstSelectedRowIndex(this DataGridView grid)
		{
			var row = FirstSelectedRow(grid);
			return row != null ? row.Index : -1;
		}

		/// <summary>Begin a row drag-drop operation on the grid</summary>
		public static void DragDrop_DragRow(object sender, MouseEventArgs e)
		{
			var grid = (DataGridView)sender;
			if (!(grid.DataSource is IList))
				throw new InvalidOperationException("Drag-drop requires a grid with a data source convertable to an IList");

			var hit = grid.HitTest(e.X, e.Y);
			if (hit.Type == DataGridViewHitTestType.RowHeader && hit.RowIndex >= 0 && hit.RowIndex < grid.RowCount)
				grid.DoDragDrop(grid.Rows[hit.RowIndex], DragDropEffects.Move|DragDropEffects.Copy|DragDropEffects.Link);
		}

		/// <summary>A drag drop function for move a row in a grid to a new position</summary>
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

			// We'll use move thanks
			args.Effect = DragDropEffects.Move;
			if (mode != DragDrop.EDrop.Drop)
				return true;

			// Find where the mouse is over the grid
			Point pt = grid.PointToClient(new Point(args.X, args.Y));
			var hit = grid.HitTest(pt.X, pt.Y);
			if (hit.Type != DataGridViewHitTestType.RowHeader || hit.RowIndex < 0 || hit.RowIndex >= grid.RowCount)
				return true;

			// Get the drag data
			var row = (DataGridViewRow)args.Data.GetData(typeof(DataGridViewRow));
			int idx1 = row.Index;
			int idx2 = hit.RowIndex;

			// Swap the rows
			var list = grid.DataSource as IList;
			if (list == null) throw new InvalidOperationException("Drag-drop requires a grid with a data source bound to an IList");
			object tmp = list[idx1];
			list[idx1] = list[idx2];
			list[idx2] = tmp;

			// Invalidate the rows so that they draw again
			grid.InvalidateRow(idx1);
			grid.InvalidateRow(idx2);
			return true;
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

	/// <summary>A base class with default implementations for a cell edit control.
	/// To make a cell editing control:</summary>
	public class DataGridViewCellEditBase :Control ,IDataGridViewEditingControl
	{
		public virtual DataGridView EditingControlDataGridView                                             { get; set; }
		public virtual int          EditingControlRowIndex                                                 { get; set; }
		public virtual bool         EditingControlValueChanged                                             { get; set; }
		public virtual object       GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) { return EditingControlFormattedValue.ToString(); }
		public virtual void         ApplyCellStyleToEditingControl(DataGridViewCellStyle style)            {}
		public virtual bool         EditingControlWantsInputKey(Keys key_data, bool wants_input_key)       { return !wants_input_key; }
		public virtual bool         RepositionEditingControlOnValueChange                                  { get { return false; } }
		public virtual Cursor       EditingPanelCursor                                                     { get { return base.Cursor; } }
		public virtual void         PrepareEditingControlForEdit(bool select_all)                          {}
		public virtual object       EditingControlFormattedValue                                           { get { return Value; } set { Value = value; } }
		public virtual void         ValueChanged()                                                         { EditingControlValueChanged = true; if (EditingControlDataGridView != null) EditingControlDataGridView.NotifyCurrentCellDirty(true); }

		/// <summary>Override to Get/Set the result of the editing control</summary>
		public virtual object       Value                                                                  { get { return m_value; } set { if (value != m_value) {m_value = value; ValueChanged();} } }
		private object m_value;
	}

	/// <summary>A column of track bars</summary>
	public class DataGridViewTrackBarColumn :DataGridViewColumn
	{
		public DataGridViewTrackBarColumn() :base(new DataGridViewTrackBarCell{Value=0, MinValue=0, MaxValue=100}) {}
		public new DataGridViewTrackBarCell CellTemplate { get { return (DataGridViewTrackBarCell)base.CellTemplate; } }
		public int MinValue { get { return  CellTemplate.MinValue; } set { CellTemplate.MinValue = value; } }
		public int MaxValue { get { return  CellTemplate.MaxValue; } set { CellTemplate.MaxValue = value; } }
	}

	/// <summary>A data grid view cell containing a track bar</summary>
	public class DataGridViewTrackBarCell :DataGridViewTextBoxCell
	{
		public int MinValue                            { get; set; }
		public int MaxValue                            { get; set; }
		public override Type ValueType                 { get { return typeof(int); } }
		public override Type EditType                  { get { return typeof(DataGridViewTrackBarEditCtrl); } }
		public override object DefaultNewRowValue      { get { return 0; } }
		public DataGridViewTrackBarCell()              { Value = 0; }
		public override object Clone()
		{
			DataGridViewTrackBarCell c = (DataGridViewTrackBarCell)base.Clone();
			c.MinValue = MinValue;
			c.MaxValue = MaxValue;
			c.Value = 0;
			return c;
		}
		protected override void Paint(Graphics gfx, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			paint_parts &= ~DataGridViewPaintParts.ContentForeground;
			base.Paint(gfx, clip_bounds, cell_bounds, row_index, cell_state, value, formatted_value, error_text, cell_style, advanced_border_style, paint_parts);
			DataGridViewTrackBarEditCtrl.PaintTrackBar(gfx, cell_bounds, (int)value, MinValue, MaxValue);
		}
		public override void InitializeEditingControl(int row_index, object initial_formatted_value, DataGridViewCellStyle data_grid_view_cell_style)
		{
			base.InitializeEditingControl(row_index, initial_formatted_value, data_grid_view_cell_style);
			DataGridViewTrackBarEditCtrl ctrl = DataGridView.EditingControl as DataGridViewTrackBarEditCtrl;
			if (ctrl != null) ctrl.Value = this;
		}
	}

	/// <summary>Editing control for editing the track bar in a track bar cell</summary>
	public class DataGridViewTrackBarEditCtrl :DataGridViewCellEditBase
	{
		public const int m_btn_width = 12;
		private static readonly Brush m_gray0 = new SolidBrush(Color.DarkGray);
		private static readonly Brush m_gray1 = new SolidBrush(Color.Gray);
		private static readonly Brush m_gray2 = new SolidBrush(Color.LightGray);

		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			e.IsInputKey |= e.KeyCode == Keys.Left || e.KeyCode == Keys.Right;
			base.OnPreviewKeyDown(e);
		}
		public override bool EditingControlWantsInputKey(Keys key_data, bool wants_input_key)
		{
			return !wants_input_key || key_data == Keys.Left || key_data == Keys.Right;
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			DataGridViewTrackBarCell cell = (DataGridViewTrackBarCell)Value;
			if (e.KeyCode == Keys.Left ) { cell.Value = Maths.Clamp((int)cell.Value - 1, cell.MinValue, cell.MaxValue); e.Handled = true; Refresh(); }
			if (e.KeyCode == Keys.Right) { cell.Value = Maths.Clamp((int)cell.Value + 1, cell.MinValue, cell.MaxValue); e.Handled = true; Refresh(); }
			base.OnKeyDown(e);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Refresh();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			DataGridViewTrackBarCell cell = (DataGridViewTrackBarCell)Value;
			e.Graphics.FillRectangle(Brushes.LightBlue, e.ClipRectangle);
			PaintTrackBar(e.Graphics, e.ClipRectangle, (int)cell.Value, cell.MinValue, cell.MaxValue);
		}
		private void ReadValue(int x)
		{
			DataGridViewTrackBarCell cell = (DataGridViewTrackBarCell)Value;
			int value = Maths.Clamp(cell.MinValue + (cell.MaxValue - cell.MinValue) * (x - Left - m_btn_width/2) / (Width - m_btn_width), cell.MinValue, cell.MaxValue);
			cell.Value = value;
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			ReadValue(e.X);
			Refresh();
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			ReadValue(e.X);
			Refresh();
		}
		/// <summary>Paint the track bar</summary>
		public static void PaintTrackBar(Graphics gfx, Rectangle cell_bounds, int pos, int min_pos, int max_pos)
		{
			float frac = Maths.Clamp((pos - min_pos) / (float)(max_pos - min_pos), 0f, 1f);

			// Draw the track
			RectangleF track_rect = cell_bounds;
			track_rect.Width -= m_btn_width; track_rect.Height = 4;
			track_rect.Offset(m_btn_width/2f, cell_bounds.Height * 0.5f - 1f);
			gfx.FillRectangle(m_gray1, track_rect);
			track_rect.Inflate(-1,-1);
			gfx.FillRectangle(m_gray0, track_rect);

			// Draw the thumb
			RectangleF thumb_rect = cell_bounds;
			thumb_rect.Width = m_btn_width; thumb_rect.Height -= 4;
			thumb_rect.Offset((cell_bounds.Width - m_btn_width) * frac, 2f);
			gfx.FillRectangle(m_gray1, thumb_rect);
			thumb_rect.Inflate(-1,-1);
			gfx.FillRectangle(m_gray2, thumb_rect);
		}
	}

	///// <summary>A cell editing control for colour cells</summary>
	//public class DataGridViewColorPicker :DataGridViewCellEditBase
	//{
	//    private readonly ColorDialog m_dlg = new ColorDialog();
	//    public DataGridViewColorPicker()
	//    {
	//        Value = Color.White;
	//    }

	//    protected override void  OnVisibleChanged(EventArgs e)
	//    {
	//        ColorPanel
	//        base.OnVisibleChanged(e);
	//        if (Value is Color) m_dlg.Color = (Color)Value;
	//        if (m_dlg.ShowDialog() == DialogResult.OK)
	//            Value = m_dlg.Color;
	//    }
	//}

	///// <summary>A data grid cell for showing/picking a colour</summary>
	//public class DataGridViewColorCell :DataGridViewTextBoxCell
	//{
	//    private Color m_colour;

	//    /// <summary>Get/Set the value of this cell as a 'Color'</summary>
	//    public Color Color
	//    {
	//        get { return m_colour; }
	//        set { m_colour = value; Value = value.ToArgb().ToString(); }
	//    }

	//    /// <summary>Default cell</summary>
	//    public DataGridViewColorCell() { Value = Color; }

	//    /// <summary>Clone this cell</summary>
	//    public override object Clone()
	//    {
	//        // Clone is required when deriving from DataGridViewCell
	//        DataGridViewColorCell c = (DataGridViewColorCell)base.Clone();
	//        c.m_colour = m_colour;
	//        return c;
	//    }

	//    /// <summary>The type used to edit this cell</summary>
	//    public override Type EditType { get { return typeof(DataGridViewColorPicker); } }

	//    //// Start/stop editing
	//    //public override void InitializeEditingControl(int row_index, object initial_formatted_value, DataGridViewCellStyle style)
	//    //{
	//    //    base.InitializeEditingControl(row_index, initial_formatted_value, style);
	//    //    DataGridViewColorPicker ctrl = DataGridView.EditingControl as DataGridViewColorPicker;
	//    //    if (ctrl != null) ctrl.Value = initial_formatted_value;
	//    //}

	//    //protected override void Paint(
	//    //    Graphics gfx,
	//    //    Rectangle clip_rect,
	//    //    Rectangle cell_rect,
	//    //    int row_index,
	//    //    DataGridViewElementStates cell_state,
	//    //    object value,
	//    //    object formatted_value,
	//    //    string error_text,
	//    //    DataGridViewCellStyle cell_style,
	//    //    DataGridViewAdvancedBorderStyle advanced_border_style,
	//    //    DataGridViewPaintParts paint_parts)
	//    //{
	//    //    cell_style.BackColor = Color;
	//    //    cell_style.ForeColor = Color;
	//    //    const DataGridViewPaintParts pp = DataGridViewPaintParts.All ^ (DataGridViewPaintParts.ContentForeground|DataGridViewPaintParts.ContentBackground);
	//    //    base.Paint(gfx, clip_rect, cell_rect, row_index, cell_state, value, formatted_value, error_text, cell_style, advanced_border_style, pp);
	//    //    //using (Pen pen = new Pen(SystemColors.ControlDark))
	//    //    //{
	//    //    //    const float phi = 1.618f;
	//    //    //    Rectangle rc = new Rectangle(cell_rect.X + 8, cell_rect.Y + 3, cell_rect.Width - (int)(cell_rect.Width * phi / 8), cell_rect.Height - (int)(cell_rect.Height * phi / 4));
	//    //    //    gfx.FillRectangle(new SolidBrush(Color.FromArgb(int.Parse(formatted_value.ToString()))), rc);
	//    //    //    gfx.DrawRectangle(pen, rc);
	//    //    //}
	//    //}
	//}
	//public class DataGridViewColorColumn :DataGridViewColumn
	//{
	//    public DataGridViewColorColumn() :base(new DataGridViewColorCell()) {}
	//    public override DataGridViewCell CellTemplate
	//    {
	//        get { return base.CellTemplate; }
	//        set
	//        {
	//            if (value != null && !value.GetType().IsAssignableFrom(typeof(DataGridViewColorCell))) throw new InvalidCastException("Must be a DataGridViewColorCell");
	//            base.CellTemplate = value;
	//        }
	//    }
	//};
}
