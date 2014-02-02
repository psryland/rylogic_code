using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using pr.maths;

namespace pr.gui
{
	/// <summary>A base class with default implementations for a cell edit control.</summary>
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
		protected void              ValueChanged()                                                         { EditingControlValueChanged = true; if (EditingControlDataGridView != null) EditingControlDataGridView.NotifyCurrentCellDirty(true); }

		/// <summary>Override to Get/Set the result of the editing control</summary>
		public object Value
		{
			get { return m_value; }
			set
			{
				if (value == m_value) return;
				m_value = value;
				ValueChanged();
			}
		}
		private object m_value;
	}

	/// <summary>
	/// A column of track bars.
	/// The min/max values for the track bars can be set in two ways; either set
	/// the min/max values for the entire column, or use the Min/MaxValuePropertyName
	/// properties to data bind to properties on the underlying type.</summary>
	public class DataGridViewTrackBarColumn :DataGridViewColumn
	{
		public DataGridViewTrackBarColumn()
			:base(new DataGridViewTrackBarCell{Value=0})
		{
			MinValue = 0;
			MaxValue = 100;
		}
		public new DataGridViewTrackBarCell CellTemplate
		{
			get { return (DataGridViewTrackBarCell)base.CellTemplate; }
		}

		/// <summary>
		/// The minimum value to use for all values in the column
		/// Applies only when MinValuePropertyName is null</summary>
		public int MinValue { get; set; }

		/// <summary>
		/// The maximum value to use for all values in the column
		/// Applies only when MaxValuePropertyName is null</summary>
		public int MaxValue { get; set; }

		/// <summary>The name of the property that provides the minimum allow value</summary>
		public string MinValuePropertyName { get; set; }

		/// <summary>The name of the property that provides the minimum allow value</summary>
		public string MaxValuePropertyName { get; set; }
	}

	/// <summary>A data grid view cell containing a track bar</summary>
	public class DataGridViewTrackBarCell :DataGridViewTextBoxCell
	{
		public override Type ValueType            { get { return typeof(int); } }
		public override Type EditType             { get { return typeof(DataGridViewTrackBarEditCtrl); } }
		public override object DefaultNewRowValue { get { return 0; } }

		private Func<DataGridViewTrackBarColumn, object, int> GetMinValue;
		private Func<DataGridViewTrackBarColumn, object, int> GetMaxValue;

		public DataGridViewTrackBarCell()
		{
			Value = 0;

			// Set up self optimising get min/max value functions
			GetMinValue = (col,obj) =>
				{
					var prop = obj.GetType().GetProperty(col.MinValuePropertyName, typeof(int));
					var method = prop.GetGetMethod();
					GetMinValue = (c,o) => (int)method.Invoke(o, null);
					return GetMinValue(col,obj);
				};
			GetMaxValue = (col,obj) =>
				{
					var prop = obj.GetType().GetProperty(col.MaxValuePropertyName, typeof(int));
					var method = prop.GetGetMethod();
					GetMaxValue = (c,o) => (int)method.Invoke(o, null);
					return GetMaxValue(col,obj);
				};
		}

		public int MinValue
		{
			get
			{
				// If no binding property has been given, return the column min value
				var col = (DataGridViewTrackBarColumn)OwningColumn;
				if (col.MinValuePropertyName == null) return col.MinValue;

				var obj = DataGridView.Rows[RowIndex].DataBoundItem;
				return GetMinValue(col, obj);
			}
		}

		public int MaxValue
		{
			get
			{
				var col = (DataGridViewTrackBarColumn)OwningColumn;
				if (col.MaxValuePropertyName == null) return col.MaxValue;

				var obj = DataGridView.Rows[RowIndex].DataBoundItem;
				return GetMaxValue(col,obj);
			}
		}

		public override object Clone()
		{
			var c = (DataGridViewTrackBarCell)base.Clone();
			c.Value = Value;
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
			var ctrl = DataGridView.EditingControl as DataGridViewTrackBarEditCtrl;
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
			var cell = (DataGridViewTrackBarCell)Value;
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
			var cell = (DataGridViewTrackBarCell)Value;
			e.Graphics.FillRectangle(Brushes.LightBlue, e.ClipRectangle);
			PaintTrackBar(e.Graphics, e.ClipRectangle, (int)cell.Value, cell.MinValue, cell.MaxValue);
		}
		private void ReadValue(int x)
		{
			var cell = (DataGridViewTrackBarCell)Value;
			var frac = Maths.Clamp(Maths.Frac(m_btn_width/2, x, Width - m_btn_width/2), 0f, 1f);
			cell.Value = Maths.Lerp(cell.MinValue, cell.MaxValue, frac);
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
			var frac = Maths.Clamp(Maths.Frac(min_pos, pos, max_pos), 0f, 1f);

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
