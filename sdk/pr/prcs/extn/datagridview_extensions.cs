//***************************************************
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.Windows.Forms;
using pr.maths;

namespace pr.extn
{
	/// <summary>
	/// Grid standard keyboard shortcuts
	/// Use: add for each function you want supported
	///   e.g.
	///   m_grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
	///   m_grid.KeyDown += DataGridView_Extensions.Cut;</summary>
	public static class DataGridView_Extensions
	{
		/// <summary>Combined handler for cut, copy, and paste replace functions</summary>
		public static void CutCopyPasteReplace(object sender, KeyEventArgs e)
		{
			SelectAll            (sender, e); if (e.Handled) return;
			Cut                  (sender, e); if (e.Handled) return;
			Copy                 (sender, e); if (e.Handled) return;
			PasteReplace         (sender, e); if (e.Handled) return;
			PasteReplaceSelected (sender, e); if (e.Handled) return;
		}

		/// <summary>Select all rows</summary>
		public static void SelectAll(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.A) return;
			dgv.SelectAll();
			e.Handled = true;
		}

		/// <summary>Delete the contents of the selected cells</summary>
		public static void Delete(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (e.KeyCode != Keys.Delete) return;
			foreach (DataGridViewCell c in dgv.SelectedCells) c.Value = null;
		}

		/// <summary>Cut the selected cells to the clipboard. Cut cells replaced with default values</summary>
		public static void Cut(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.X) return;
			DataObject d = dgv.GetClipboardContent(); if (d == null) return;
			Clipboard.SetDataObject(d);
			e.Handled = true;
			foreach (DataGridViewCell c in dgv.SelectedCells) 
				if (!c.ReadOnly) c.Value = c.DefaultNewRowValue;
		}

		/// <summary>Copy the selected cells to the clipboard</summary>
		public static void Copy(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.C) return;
			DataObject d = dgv.GetClipboardContent(); if (d == null) return;
			Clipboard.SetDataObject(d);
			e.Handled = true;
		}

		/// <summary>Paste over existing cells within the current size limits of the grid. Must be 1 cell selected only</summary>
		public static void PasteReplace(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (dgv.SelectedCells.Count != 1) return;

			// Read the lines from the clipboard
			string[] lines = Clipboard.GetText().Split('\n');

			int row = dgv.CurrentCell.RowIndex;
			int col = dgv.CurrentCell.ColumnIndex;
	
			for (int j = 0; j != lines.Length && row != dgv.RowCount; ++j, ++row)
			{
				// Skip blank lines
				if (lines[j].Length == 0) continue;

				string[] cells = lines[j].Split('\t',',',';');
				for (int i = 0; i != cells.Length && col != dgv.ColumnCount; ++i, ++col)
				{
					DataGridViewCell cell = dgv[col,row];
					if (cell.ReadOnly) continue;
					if (cells[i].Length == 0) continue;
					try { cell.Value = Convert.ChangeType(cells[i], cell.ValueType); }
					catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
				}
			}
			e.Handled = true;
		}

		/// <summary>Paste over selected cells. Only replaces those selected. Must be >= 2 cells selected</summary>
		public static void PasteReplaceSelected(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (dgv.SelectedCells.Count < 2) return;

			// Get a snapshot of the selected grid cells
			var selected_cells = dgv.SelectedCells;

			// Find the bounds of the selected cells
			Point min = new Point(selected_cells[0].ColumnIndex, selected_cells[0].RowIndex);
			Point max = min;
			foreach (DataGridViewCell item in selected_cells)
			{
				min.X = Math.Min(min.X, item.ColumnIndex);
				min.Y = Math.Min(min.Y, item.RowIndex);
				max.X = Math.Max(max.X, item.ColumnIndex+1);
				max.Y = Math.Max(max.Y, item.RowIndex+1);
			}

			// Read the lines from the clipboard
			string[] lines = Clipboard.GetText().Split('\n');
			string[][] cells = new string[lines.Length][]; // cache split rows from the clipboard data

			foreach (DataGridViewCell cell in selected_cells)
			{
				if (cell.ReadOnly) continue;

				int j = cell.RowIndex - min.Y;
				if (j >= lines.Length) continue;
				if (cells[j] == null) cells[j] = lines[j].Split('\t',',',';');

				int i = cell.ColumnIndex - min.X;
				if (i >= cells[j].Length) continue;

				try
				{
					if (cells[j][i].Length != 0)
						cell.Value = Convert.ChangeType(cells[j][i], cell.ValueType);
				}
				catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
			}
			e.Handled = true;
		}

		/// <summary>Paste from the first selected cell over anything in the way. Grow the grid if necessary</summary>
		public static void PasteGrow(object sender, KeyEventArgs e)
		{
			DataGridView dgv = (DataGridView)sender;
			if (!e.Control || e.KeyCode != Keys.V) return;
			if (dgv.SelectedCells.Count > 1) return;
			
			int row = dgv.CurrentCell.RowIndex;
			int col = dgv.CurrentCell.ColumnIndex;

			// Read the lines from the clipboard
			string[] lines = Clipboard.GetText().Split('\n');

			// Grow the dgv if necessary
			if (row + lines.Length > dgv.RowCount)
				dgv.RowCount = row + lines.Length;
			
			for (int j = 0; j != lines.Length; ++j)
			{
				// Skip blank lines
				if (lines[j].Length == 0) continue;

				string[] cells = lines[j].Split('\t', ',', ';');
				
				// Grow the grid if necessary
				if (col + cells.Length > dgv.ColumnCount)
					dgv.ColumnCount = col + cells.Length;

				for (int i = 0; i != cells.Length; ++i)
				{
					DataGridViewCell cell = dgv[i+col,j+row];
					if (cell.ReadOnly) continue;
					if (cells[i].Length == 0) continue;
					try { cell.Value = Convert.ChangeType(cells[i], cell.ValueType); }
					catch (FormatException) { cell.Value = cell.DefaultNewRowValue; }
				}
			}
			e.Handled = true;
		}
	
		/// <summary>Return a collection of the fill weights</summary>
		public static float[] FillWeights(this DataGridView grid)
		{
			float[] w = new float[grid.Columns.Count];
			for (int i = 0; i != w.Length; ++i) w[i] = grid.Columns[i].FillWeight;
			return w;
		}
		
		/// <summary>Return the first selected row, regardless of multi-select grids</summary>
		public static DataGridViewRow FirstSelectedRow(this DataGridView grid)
		{
			return grid.SelectedRows.Count != 0 ? grid.SelectedRows[0] : null;
		}
		
		/// <summary>Sets the selection to row 'index'. Clamps 'index' to [0,RowCount). Returns the row actually selected</summary>
		public static int SelectRow(this DataGridView grid, int index)
		{
			index = Math.Max(0, Math.Min(grid.RowCount, index));
			grid.ClearSelection();
			if (index >= 0 && index < grid.RowCount) grid.Rows[index].Selected = true;
			return index;
		}

		/// <summary>Return the index of the first selected row (or -1) if no rows are selected</summary>
		public static int FirstSelectedRowIndex(this DataGridView grid)
		{
			var row = FirstSelectedRow(grid);
			return row != null ? row.Index : -1;
		}
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

