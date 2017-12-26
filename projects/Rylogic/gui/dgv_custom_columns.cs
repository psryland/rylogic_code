using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Windows32;

namespace Rylogic.Gui
{
	/// <summary>A base class with default implementations of a cell edit control.</summary>
	public abstract class DataGridViewCellEditBase :Control ,IDataGridViewEditingControl
	{
		// There is usually only one editor for a column type per grid

		/// <summary>When data in your custom control changes, call "NotifyValueChanged"</summary>
		public abstract object Value { get; set; }

		/// <summary>Derived classes should call this when values change in the control</summary>
		protected void NotifyValueChanged(object sender = null, EventArgs args = null)
		{
			// On top of implementing the IDataGridViewEditingControl interface, an editing control typically needs to forward the
			// fact that its content changed to the grid via the DataGridView.NotifyCurrentCellDirty(...) method. Often an editing
			// control must override protected virtual methods to be notified of the content changes and be able to forward the
			// information to the grid. In this particular case, the DataGridViewNumericUpDownEditingControl overrides two methods:
			//   protected override void OnKeyPress(KeyPressEventArgs e)
			//   protected override void OnValueChanged(EventArgs e)
			EditingControlValueChanged = true;
			if (EditingControlDataGridView != null)
				EditingControlDataGridView.NotifyCurrentCellDirty(true);
		}

		/// <summary>Called when the control is first display *and* whenever the control is refreshed</summary>
		public virtual void PrepareEditingControlForEdit(bool select_all) {}

		/// <summary>caches the grid that uses this editing control</summary>
		public virtual System.Windows.Forms.DataGridView EditingControlDataGridView { get; set; }

		/// <summary>represents the row in which the editing control resides</summary>
		public virtual int EditingControlRowIndex { get; set; }
		
		/// <summary>Indicates whether the value of the editing control has changed or not</summary>
		public virtual bool EditingControlValueChanged { get; set; }
		
		/// <summary>Represents the current formatted value of the editing control</summary>
		public virtual object EditingControlFormattedValue { get { return Value; } set { Value = value; } }

		/// <summary>Returns the current value of the editing control.</summary>
		public virtual object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) { return EditingControlFormattedValue; }
		
		/// <summary>Method called by the grid before the editing control is shown so it can adapt to the provided cell style.</summary>
		public virtual void ApplyCellStyleToEditingControl(DataGridViewCellStyle style) {}

		/// <summary>Method called by the grid on keystrokes to determine if the editing control is interested in the key or not.</summary>
		public virtual bool EditingControlWantsInputKey(Keys key_data, bool wants_input_key) { return !wants_input_key; }
		
		/// <summary>Indicates whether the editing control needs to be repositioned when its value changes.</summary>
		public virtual bool RepositionEditingControlOnValueChange { get { return false; } }

		/// <summary>Determines which cursor must be used for the editing panel, i.e. the parent of the editing control.</summary>
		public virtual Cursor EditingPanelCursor { get { return base.Cursor; } }
	}

	/// <summary>An editor for when not edit control is needed</summary>
	public class NullCellEditor :DataGridViewCellEditBase
	{
		public override object Value { get; set; }
	}

	/// <summary>Base class for a custom grid column</summary>
	public class DataGridViewCustomColumnBase<TCell> :DataGridViewColumn where TCell:DataGridViewCell
	{
		public DataGridViewCustomColumnBase(TCell template_cell) :base(template_cell)
		{}
		public override DataGridViewCell CellTemplate
		{
			get { return (TCell)base.CellTemplate; }
			set
			{
				if (value as TCell == null) throw new InvalidCastException("Must be a " + typeof(TCell).Name);
				base.CellTemplate = value;
			}
		}
	}

	/// <summary>Base class for custom grid cells</summary>
	public class DataGridViewCustomCellBase<TEditor> :DataGridViewCell where TEditor:DataGridViewCellEditBase
	{
		// From MSDN: http://msdn.microsoft.com/en-us/library/aa730881(v=vs.80).aspx
		// Key methods to override:
		// When developing a custom cell type, it is also critical to check if the following virtual methods need to be overridden.
		// The Clone() method:
		//   The DataGridViewCell base class implements the ICloneable interface. Each custom cell type typically needs to override
		//   the Clone() method to copy its custom properties. Cells are cloneable because a particular cell instance can be used for
		//   multiple rows in the grid. This is the case when the cell belongs to a shared row. When a row gets unshared, its cells
		//   need to be cloned.
		// The KeyEntersEditMode(KeyEventArgs) method:
		//   Cell types that have a complex editing experience (i.e., cells that use an editing control) need to override this method
		//   to define which keystrokes force the editing control to be shown (when the grid's EditMode is EditOnKeystrokeOrF2). For
		//   the DataGridViewNumericUpDownCell type, digits and the negative sign need to bring up the editing control.
		// The InitializeEditingControl(int, object, DataGridViewCellStyle) method:
		//   This method is called by the grid control when the editing control is about to be shown. This occurs only for cells that
		//   have a complex editing experience of course. This gives the cell a chance to initialize its editing control based on its
		//   own properties and the formatted value provided.
		// The PositionEditingControl(bool, bool, Rectangle, Rectangle, DataGridViewCellStyle, bool, bool, bool, bool) method:
		//   Cells control the positioning and sizing of their editing control. They typically do that based on their size and style
		//   (particularly the alignment settings). The grid control calls this method whenever the editing control needs to be
		//   repositioned or sized.
		// The DetachEditingControl() method:
		//   This method is called by the grid control whenever the editing control is no longer needed because the editing session
		//   is ending. Custom cells may want to override this method to do some clean-up work.
		// The GetFormattedValue(object, int, ref DataGridViewCellStyle, TypeConverter, TypeConverter, DataGridViewDataErrorContexts) method:
		//   This method is called by the grid control whenever it needs to access the formatted representation of a cell's value.
		//   For the DataGridViewNumericUpDownCell, the Value is of type System.Decimal and the FormattedValue is of type System.String.
		//   So this method needs to convert a decimal into a string. The base implementation does this conversion, but the
		//   DataGridViewNumericUpDownCell still needs to override the default behavior to make sure the string returned corresponds
		//   exactly to what a NumericUpDown control would display.
		// The GetPreferredSize(Graphics, DataGridViewCellStyle, int, Size) method:
		//   Whenever a grid auto-sizing feature is invoked, some cells are asked to determine their preferred height, width, or size.
		//   Custom cell types can override the GetPreferredSize method to calculate their preferred dimensions based on their content,
		//   style, properties, etc. Since the DataGridViewNumericUpDownCell cell only differs slightly from its base class, the
		//   DataGridViewTextBoxCell, as far as its representation on the screen is concerned, it calls the base implementation and
		//   returns a corrected value by taking into account the up/down buttons.
		// The GetErrorIconBounds(Graphics, DataGridViewCellStyle, int) method:
		//   Custom cell types can override this method to customize the location of their error icon. By default the error icon is
		//   shown close to the right edge of the cell. But because the DataGridViewNumericUpDownCell has up/down buttons close to that
		//   right edge, it overrides the method such that the error icon and buttons don't overlap.
		// The Paint(Graphics, Rectangle, Rectangle, int, DataGridViewElementStates, object, object, string, DataGridViewCellStyle,
		//  DataGridViewAdvancedBorderStyle, DataGridViewPaintParts) method:
		//   This method is a critical one for any cell type. It is responsible for painting the cell. The DataGridViewNumericUpDownCell's
		//   Paint implementation uses a NumericUpDown control to do the painting. It sets various properties on the control and calls the
		//   NumericUpDown.DrawToBitmap(...) function, then the Graphics.DrawImage(...) one. This is a convenient approach for cell types
		//   that directly imitate a particular Control. However it is not very efficient and won't work for some controls like the
		//   RichTextBox or custom controls that don't support WM_PRINT. A more efficient alternative would be to paint the cell piece by
		//   piece so that it looks like a NumericUpDown control.

		// Note, it might be easier to use an existing cell type such as DataGridViewTextBoxCell.
		// This base class is intended as a helper for subclassing from DataGridViewCell directly.

		// Cell state flags. This behaviour is copied from the TextBoxCell source
		private const byte Flags_IgnoreNextMouseClick = 1 << 0;
		private byte m_flags;

		/// <summary>The cell editor type</summary>
		public override Type EditType
		{
			get { return typeof(TEditor) != typeof(NullCellEditor) ? typeof(TEditor) : null; }
		}

		// Start/stop editing
		public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
		{
			System.Diagnostics.Debug.Assert(ReferenceEquals(this, DataGridView[ColumnIndex, rowIndex]), ""); // 'this' cell is the template cell?, not the cell being edited
			base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
			var ctrl = DataGridView.EditingControl as TEditor;
			if (ctrl != null)
				SetInitialValue(ctrl, initialFormattedValue);
		}

		/// <summary>Set the initial state of the editing control</summary>
		protected virtual void SetInitialValue(TEditor ctrl, object formatted_value)
		{
			ctrl.Value = formatted_value;
		}

		// BeginEdit is triggered on the second click of the current cell. Not from a double click.
		protected override void OnEnter(int rowIndex, bool throughMouseClick)
		{
			base.OnEnter(rowIndex, throughMouseClick);
			if (throughMouseClick) m_flags = (byte)(m_flags | Flags_IgnoreNextMouseClick);
		}
		protected override void OnLeave(int rowIndex, bool throughMouseClick)
		{
			base.OnLeave(rowIndex, throughMouseClick);
			if (throughMouseClick) m_flags = (byte)(m_flags & ~Flags_IgnoreNextMouseClick);
		}
		protected override void OnMouseClick(DataGridViewCellMouseEventArgs e)
		{
			if (DataGridView == null)
				return;

			System.Diagnostics.Debug.Assert(e.ColumnIndex == ColumnIndex);
			var addr = DataGridView.CurrentCellAddress;
			if (addr.X == e.ColumnIndex && addr.Y == e.RowIndex && e.Button == MouseButtons.Left)
			{
				if ((m_flags & Flags_IgnoreNextMouseClick) != 0x00)
				{
					m_flags = (byte)(m_flags & ~Flags_IgnoreNextMouseClick);
				}
				else if (DataGridView.EditMode != DataGridViewEditMode.EditProgrammatically)
				{
					DataGridView.BeginEdit(true);
				}
			}
		}
	}

	#region

	/// <summary>Combo box cells</summary>
	public class DataGridViewComboBoxColumn :System.Windows.Forms.DataGridViewComboBoxColumn
	{
		public DataGridViewComboBoxColumn()
			:base()
		{}

		/// <summary>The data source for the combo box items. Remember to set 'ValueMember' and 'DisplayMember' if not using strings</summary>
		public new object DataSource
		{
			// Notes:
			// Binding to DGV combo box is fuct for anything other than strings.
			// For non-strings, you need to set 3 members: DataSource, ValueMember, and DisplayMember
			//  Set 'DataSource' to the IList of 'Thing's.
			//  Set 'DisplayMember' to 'nameof(Thing.Name)'
			//  Set 'ValueMember' to 'nameof(Thing.This)' where 'This' is a property such as "public Thing This { get { return this; } }"
			// The reason is basically because the DGV combo box cell is a broken implementation.
			get { return base.DataSource; }
			set { base.DataSource = value; }
		}
	}
	#endregion

	#region Rich Text Box Cell/Column

	/// <summary>A user control for editing rich text box cells</summary>
	public class RichTextBoxEditor :DataGridViewCellEditBase
	{
		public RichTextBoxEditor()
		{
			InitializeComponent();

			m_rtb.TextChanged += NotifyValueChanged;
		}

		/// <summary>Access the RTB control</summary>
		public RichTextBox RichTextBox { get { return m_rtb; } }
		private RichTextBox m_rtb;

		/// <summary>Get/Set the current editor value</summary>
		public override object Value
		{
			get { return m_rtb.Rtf; }
			set { m_rtb.Rtf = (string)value; }
		}

		// Overrides
		public override void PrepareEditingControlForEdit(bool select_all)
		{
			m_rtb.Focus();
			if (select_all)
				m_rtb.SelectAll();
		}
		public override bool EditingControlWantsInputKey(Keys key_data, bool wants_input_key)
		{
			var key_code = (Keys)(key_data & Keys.KeyCode);
			if (key_code == Keys.Return) return true;
			return base.EditingControlWantsInputKey(key_data, wants_input_key);
		}
		protected override bool ProcessKeyPreview(ref Message m)
		{
			//if (m.Msg == Win32.WM_KEYDOWN)
			//{}
			return base.ProcessKeyPreview(ref m);
		}
		protected override bool ProcessDialogKey(Keys key_data)
		{
			//var key_code = (Keys)(key_data & Keys.KeyCode);
			//if (key_code == Keys.Tab)
			//{}
			return base.ProcessDialogKey(key_data);
		}
		private void InitializeComponent()
		{
			this.m_rtb = new RichTextBox();
			this.SuspendLayout();
			// 
			// m_rtb
			// 
			this.m_rtb.Dock = DockStyle.Fill;
			this.m_rtb.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_rtb.Location = new System.Drawing.Point(0, 0);
			this.m_rtb.Margin = new System.Windows.Forms.Padding(0);
			this.m_rtb.AcceptsTab = false;
			this.m_rtb.Multiline = true;
			this.m_rtb.Name = "m_rtb";
			this.m_rtb.Size = new System.Drawing.Size(150, 14);
			this.m_rtb.TabIndex = 0;
			// 
			// RichTextBoxEditor
			// 
			this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.Controls.Add(this.m_rtb);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "RichTextBoxEditor";
			this.Size = new System.Drawing.Size(150, 30);
			this.ResumeLayout(false);
			this.PerformLayout();
		}
	}

	/// <summary>Column type for value pairs</summary>
	public class DataGridViewRichTextBoxColumn :DataGridViewCustomColumnBase<DataGridViewRichTextBoxCell>
	{
		public DataGridViewRichTextBoxColumn()
			:base(new DataGridViewRichTextBoxCell())
		{}
	}

	/// <summary>DataGridView cell for displaying pairs of values</summary>
	public class DataGridViewRichTextBoxCell :DataGridViewCustomCellBase<RichTextBoxEditor>
	{
		public override Type   ValueType          { get { return typeof(string); } }
		public override Type   FormattedValueType { get { return typeof(string); } }
		public override object DefaultNewRowValue { get { return string.Empty; } }

		/// <summary>Set the initial state of the editing control</summary>
		protected override void SetInitialValue(RichTextBoxEditor ctrl, object formatted_value)
		{
			try
			{
				ctrl.RichTextBox.Rtf = (string)formatted_value;
			}
			catch (ArgumentException)
			{
				ctrl.Text = (string)formatted_value;
			}
		}

		public override bool KeyEntersEditMode(KeyEventArgs e)
		{
			return e.KeyCode == Keys.Return || base.KeyEntersEditMode(e);
		}
		protected override Rectangle GetContentBounds(Graphics graphics, DataGridViewCellStyle cellStyle, int rowIndex)
		{
			var rtf = (string)DataGridView.Rows[rowIndex].DataBoundItem;
			try   { Rtb.Rtf  = rtf; }
			catch { Rtb.Text = rtf; }
			return new Rectangle(Point.Empty, Rtb.PreferredSize);
			
			//return base.GetContentBounds(graphics, cellStyle, rowIndex);
		}
		protected override void Paint(Graphics gfx, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			base.Paint(gfx, clip_bounds, cell_bounds, row_index, cell_state, value, formatted_value, error_text, cell_style, advanced_border_style, paint_parts);

			try   { Rtb.Rtf  = (string)formatted_value; }
			catch { Rtb.Text = (string)formatted_value; }
			Rtb.BackColor = Selected ? cell_style.SelectionBackColor : cell_style.BackColor;
			Rtb.ForeColor = Selected ? cell_style.SelectionForeColor : cell_style.ForeColor;
			var img = RichTextBox.Print(Rtb, cell_bounds.Width, cell_bounds.Height);
			if (img != null)
				gfx.DrawImage(img, cell_bounds.Left, cell_bounds.Top);
			//var text = (string)value;
			//var col = OwningColumn.As<DataGridViewRichTextBoxColumn>();

			//using (var bsh = new SolidBrush(Selected ? cell_style.SelectionBackColor : cell_style.BackColor))
			//	gfx.FillRectangle(bsh, cell_bounds);
			//PaintBorder(gfx, clip_bounds, cell_bounds, cell_style, advanced_border_style);
			//using (var bsh = new SolidBrush(Selected ? col.ValueColour0.Lerp(cell_style.SelectionForeColor, 0.8f) : col.ValueColour0))
			//	gfx.DrawString(pair.Item1.ToString(), cell_style.Font, bsh, cell_bounds.X, cell_bounds.Y + 1);
			//using (var bsh = new SolidBrush(Selected ? col.ValueColour1.Lerp(cell_style.SelectionForeColor, 0.8f) : col.ValueColour1))
			//	gfx.DrawString(pair.Item2.ToString(), cell_style.Font, bsh, cell_bounds.X, cell_bounds.Y + 15);
		}

		/// <summary>Instance used for rendering</summary>
		private static RichTextBox Rtb { get { return m_rtb ?? (m_rtb = new RichTextBox()); } }
		private static RichTextBox m_rtb;
	}

	#endregion

	#region Value Pair Cell/Column

	/// <summary>A user control for editing value pairs</summary>
	public class ValuePairEditCtrl<T0,T1> :DataGridViewCellEditBase
	{
		private TextBox m_value0;
		private TextBox m_value1;

		public ValuePairEditCtrl()
		{
			InitializeComponent();

			m_value0.Validating += (s,a) => a.Cancel = !Validate(0, m_value0.Text);
			m_value1.Validating += (s,a) => a.Cancel = !Validate(1, m_value1.Text);
			m_value0.TextChanged += NotifyValueChanged;
			m_value1.TextChanged += NotifyValueChanged;
		}

		// Convert the text in the value text boxes into the value types
		protected virtual T0 ConvertT0(string value)
		{
			try { return (T0)Convert.ChangeType(value, typeof(T0)); } catch { return default(T0); }
		}
		protected virtual T1 ConvertT1(string value)
		{
			try { return (T1)Convert.ChangeType(value, typeof(T1)); } catch { return default(T1); }
		}

		/// <summary>Validate 'value' as correct for 'ValueN' (N = index)</summary>
		protected virtual bool Validate(int index, string value)
		{
			if (index == 0) try { Convert.ChangeType(value, typeof(T0)); return true; } catch { return false; }
			if (index == 1) try { Convert.ChangeType(value, typeof(T1)); return true; } catch { return false; }
			throw new Exception("Unknown value index '{0}' in ValuePairEditCtrl.Validate()".Fmt(index));
		}

		/// <summary>Returns the current editor value</summary>
		public override object Value
		{
			get { return Tuple.Create(ConvertT0(m_value0.Text), ConvertT1(m_value1.Text)); }
			set
			{
				var pair = (Tuple<T0,T1>)value;
				m_value0.Text = pair.Item1.ToString();
				m_value1.Text = pair.Item2.ToString();
			}
		}

		// Overrides
		public override void PrepareEditingControlForEdit(bool select_all)
		{
			m_value0.Focus();
			if (select_all)
				m_value0.SelectAll();
		}
		public override bool EditingControlWantsInputKey(Keys key_data, bool wants_input_key)
		{
			var key_code = (Keys)(key_data & Keys.KeyCode);
			switch (key_code)
			{
			case Keys.Left:
				// Left/Right navigates within the values, to the next value, or to the next cell depending on caret position
				if (m_value0.Focused && m_value0.SelectionStart != 0) return true;
				if (m_value1.Focused) return true;
				break;

			case Keys.Right:
				// Left/Right navigates within the values, to the next value, or to the next cell depending on caret position
				if (m_value0.Focused) return true;
				if (m_value1.Focused && m_value1.SelectionStart != m_value1.TextLength) return true;
				break;

			case Keys.Down:
				// If the top value is selected, down moves to the next value, otherwise let the grid handle it.
				if (m_value0.Focused) return true;
				break;

			case Keys.Up:
				// If the bottom value is selected, up moves to the previous value, otherwise let the grid handle it.
				if (m_value1.Focused) return true;
				break;
			}
			return base.EditingControlWantsInputKey(key_data, wants_input_key);
		}
		protected override bool ProcessKeyPreview(ref Message m)
		{
			if (m.Msg == Win32.WM_KEYDOWN)
			{
				var key_data = (int)m.WParam;
				var key_code = (Keys)(key_data & (int)Keys.KeyCode);
				switch (key_code)
				{
				case Keys.Left:
					if (m_value1.Focused && m_value1.SelectionStart == 0)
					{
						Focus(m_value0);
						return true;
					}
					break;

				case Keys.Right:
					if (m_value0.Focused && m_value0.SelectionStart == m_value0.TextLength)
					{
						Focus(m_value1);
						return true;
					}
					break;

				case Keys.Down:
					if (m_value0.Focused)
					{
						Focus(m_value1);
						return true;
					}
					break;

				case Keys.Up:
					if (m_value1.Focused)
					{
						Focus(m_value0);
						return true;
					}
					break;
				}
			}
			return base.ProcessKeyPreview(ref m);
		}
		protected override bool ProcessDialogKey(Keys key_data)
		{
			var key_code = (Keys)(key_data & Keys.KeyCode);
			if (key_code == Keys.Tab)
			{
				if (m_value0.Focused && ((key_data & Keys.Shift) == 0))
				{
					Focus(m_value1);
					return true;
				}
				if (m_value1.Focused && ((key_data & Keys.Shift) != 0))
				{
					Focus(m_value0);
					return true;
				}
			}
			return base.ProcessDialogKey(key_data);
		}
		private void Focus(TextBox tb)
		{
			tb.Focus();
			tb.SelectAll();
		}
		private void InitializeComponent()
		{
			this.m_value0 = new TextBox();
			this.m_value1 = new TextBox();
			this.SuspendLayout();
			// 
			// m_value0
			// 
			this.m_value0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_value0.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_value0.Location = new System.Drawing.Point(0, 0);
			this.m_value0.Margin = new System.Windows.Forms.Padding(0);
			this.m_value1.AcceptsTab = false;
			this.m_value0.Multiline = true;
			this.m_value0.Name = "m_value0";
			this.m_value0.Size = new System.Drawing.Size(150, 14);
			this.m_value0.TabIndex = 0;
			// 
			// m_value1
			// 
			this.m_value1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_value1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_value1.Location = new System.Drawing.Point(0, 15);
			this.m_value1.Margin = new System.Windows.Forms.Padding(0);
			this.m_value1.AcceptsTab = false;
			this.m_value1.Multiline = true;
			this.m_value1.Name = "m_value1";
			this.m_value1.Size = new System.Drawing.Size(150, 14);
			this.m_value1.TabIndex = 1;
			// 
			// ValuePairEditor
			// 
			this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
			this.Controls.Add(this.m_value1);
			this.Controls.Add(this.m_value0);
			this.Margin = new System.Windows.Forms.Padding(0);
			this.Name = "ValuePairEditor";
			this.Size = new System.Drawing.Size(150, 30);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
	
	/// <summary>Column type for value pairs</summary>
	public class DataGridViewValuePairColumn<T0,T1> :DataGridViewCustomColumnBase<DataGridViewValuePairCell<T0,T1>>
	{
		public DataGridViewValuePairColumn()
			:base(new DataGridViewValuePairCell<T0,T1>())
		{
			ValueColour0 = Color.Black;
			ValueColour1 = Color.Black;
		}

		/// <summary>Colour for the first value</summary>
		public Color ValueColour0 { get; set; }
		
		/// <summary>Colour for the second value</summary>
		public Color ValueColour1 { get; set; }
	}

	/// <summary>DataGridView cell for displaying pairs of values</summary>
	public class DataGridViewValuePairCell<T0,T1> :DataGridViewCustomCellBase<ValuePairEditCtrl<T0,T1>>
	{
		public override Type   ValueType          { get { return typeof(Tuple<T0,T1>); } }
		public override Type   FormattedValueType { get { return typeof(Tuple<T0,T1>); } }
		public override object DefaultNewRowValue { get { return Tuple.Create(default(T0),default(T1)); } }

		// Start/stop editing
		public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
		{
			System.Diagnostics.Debug.Assert(ReferenceEquals(this, DataGridView[ColumnIndex, rowIndex]), ""); // 'this' cell is the template cell?, not the cell being edited
			base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
			var ctrl = DataGridView.EditingControl as ValuePairEditCtrl<T0,T1>;
			if (ctrl != null) ctrl.Value = initialFormattedValue;
		}
		public override bool KeyEntersEditMode(KeyEventArgs e)
		{
			return e.KeyCode == Keys.Return || base.KeyEntersEditMode(e);
		}
		protected override void Paint(Graphics gfx, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			var pair = (Tuple<T0,T1>)value;
			var col = (DataGridViewValuePairColumn<T0,T1>)OwningColumn;

			// Paint the cell background
			if (paint_parts.HasFlag(DataGridViewPaintParts.Background))
			{
				using (var bsh = new SolidBrush(Selected ? cell_style.SelectionBackColor : cell_style.BackColor))
					gfx.FillRectangle(bsh, cell_bounds);
			}

			// Paint the cell border
			if (paint_parts.HasFlag(DataGridViewPaintParts.Border))
			{
				PaintBorder(gfx, clip_bounds, cell_bounds, cell_style, advanced_border_style);
			}

			// Paint the content
			if (paint_parts.HasFlag(DataGridViewPaintParts.ContentForeground))
			{
				using (var bsh = new SolidBrush(Selected ? col.ValueColour0.Lerp(cell_style.SelectionForeColor, 0.8f) : col.ValueColour0))
					gfx.DrawString(pair.Item1.ToString(), cell_style.Font, bsh, cell_bounds.X, cell_bounds.Y + 1);
				using (var bsh = new SolidBrush(Selected ? col.ValueColour1.Lerp(cell_style.SelectionForeColor, 0.8f) : col.ValueColour1))
					gfx.DrawString(pair.Item2.ToString(), cell_style.Font, bsh, cell_bounds.X, cell_bounds.Y + 15);
			}
		}
	}

	#endregion

	#region Check Mark Cell/Column

	/// <summary>A column of check marks</summary>
	public class DataGridViewCheckMarkColumn :DataGridViewCustomColumnBase<DataGridViewCheckMarkCell>
	{
		public DataGridViewCheckMarkColumn()
			:base(new DataGridViewCheckMarkCell())
		{
			ImageChecked = Resources.check_accept;
			ImageUnchecked = Resources.check_reject;
			Alignment = HorizontalAlignment.Center;
		}

		/// <summary>The image to use when checked</summary>
		public Image ImageChecked { get; set; }

		/// <summary>The image to use when unchecked</summary>
		public Image ImageUnchecked { get; set; }

		/// <summary>Alignment of the images within the column</summary>
		public HorizontalAlignment Alignment { get; set; }
	}

	/// <summary>DataGridView cell for displaying a check mark</summary>
	public class DataGridViewCheckMarkCell :DataGridViewCustomCellBase<NullCellEditor>
	{
		public override Type   ValueType          { get { return typeof(bool); } }
		public override Type   FormattedValueType { get { return typeof(bool); } }
		public override object DefaultNewRowValue { get { return false; } }

		// Start/stop editing
		public override void InitializeEditingControl(int rowIndex, object initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
		{}
		public override bool KeyEntersEditMode(KeyEventArgs e)
		{
			return false;
		}
		protected override void Paint(Graphics gfx, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			var chk = (bool)value;
			var col = (DataGridViewCheckMarkColumn)OwningColumn;
			var sel = cell_state.HasFlag(DataGridViewElementStates.Selected);

			// Paint the cell background
			if ((paint_parts.HasFlag(DataGridViewPaintParts.Background         ) && !sel) ||
				(paint_parts.HasFlag(DataGridViewPaintParts.SelectionBackground) && sel))
			{
				using (var bsh = new SolidBrush(sel ? cell_style.SelectionBackColor : cell_style.BackColor))
					gfx.FillRectangle(bsh, cell_bounds);
			}

			// Paint the cell border
			if (paint_parts.HasFlag(DataGridViewPaintParts.Border))
			{
				PaintBorder(gfx, clip_bounds, cell_bounds, cell_style, advanced_border_style);
			}

			// Paint the checked/unchecked image
			if (paint_parts.HasFlag(DataGridViewPaintParts.ContentForeground))
			{
				var img = chk ? col.ImageChecked : col.ImageUnchecked;
				if (img != null)
				{
					int x = 0, y = 0, w = 0, h = 0;
					if (cell_bounds.Width * img.Height < img.Width * cell_bounds.Height) // width bound
					{
						w = Math.Min(cell_bounds.Width, img.Width);
						h = w * img.Height / img.Width;
					}
					else // height bound
					{
						h = Math.Min(cell_bounds.Height, img.Height);
						w = h * img.Width / img.Height;
					}
					switch (col.Alignment) {
					case HorizontalAlignment.Left:   x = cell_bounds.Left;                               y = cell_bounds.Top;                                break;
					case HorizontalAlignment.Center: x = cell_bounds.Left + (cell_bounds.Width - w) / 2; y = cell_bounds.Top + (cell_bounds.Height - h) / 2; break;
					case HorizontalAlignment.Right:  x = cell_bounds.Left + (cell_bounds.Width - w);     y = cell_bounds.Top + (cell_bounds.Height - h);   ; break;
					}
					gfx.DrawImage(img, x, y, w, h);
				}
			}
		}
		protected override void OnDoubleClick(DataGridViewCellEventArgs e)
		{
			base.OnDoubleClick(e);
			Value = !(bool)Value;
		}
	}

	#endregion

	#region TrackBar Cell/Column

	/// <summary>Editing control for editing the track bar in a track bar cell</summary>
	public class DataGridViewTrackBarEditCtrl :DataGridViewCellEditBase
	{
		public const int m_btn_width = 12;
		private static readonly Brush m_gray0 = new SolidBrush(Color.DarkGray);
		private static readonly Brush m_gray1 = new SolidBrush(Color.Gray);
		private static readonly Brush m_gray2 = new SolidBrush(Color.LightGray);

		/// <summary>The cell to live update</summary>
		private DataGridViewTrackBarCell m_cell;

		/// <summary>Returns the current track position value</summary>
		private int Position
		{
			get { return m_impl_value; }
			set
			{
				value = (int)Math_.Clamp(value, Range.Beg, Range.End);
				if (Equals(m_impl_value, value) || m_in_set_position) return;
				using (Scope.Create(() => m_in_set_position = true, () => m_in_set_position = false))
				{
					m_impl_value = value;
					if (m_cell != null) m_cell.Value = m_impl_value;
					NotifyValueChanged();
				}
			}
		}
		public override object Value
		{
			get { return Position; }
			set { Position = (int)value; }
		}
		private int m_impl_value;
		private bool m_in_set_position;

		/// <summary>The current track bar range</summary>
		private Range Range { get; set; }

		internal void Init(int value, int min, int max, DataGridViewTrackBarCell cell)
		{
			// Note, order is important here, because this editor control is shared.
			// We need to change the cell before setting the new position, or we'll affect the old cell.
			m_cell = cell;
			Range = new Range(min, max);
			Position = value;
		}
		public override bool EditingControlWantsInputKey(Keys key_data, bool wants_input_key)
		{
			return key_data == Keys.Left || key_data == Keys.Right || base.EditingControlWantsInputKey(key_data, wants_input_key);
		}
		protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
		{
			e.IsInputKey |= e.KeyCode == Keys.Left || e.KeyCode == Keys.Right;
			base.OnPreviewKeyDown(e);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Right) { Position = Position + 1; e.Handled = true; Refresh(); }
			if (e.KeyCode == Keys.Left ) { Position = Position - 1; e.Handled = true; Refresh(); }
			base.OnKeyDown(e);
		}
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			Refresh();
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			var value = Convert.ToDouble(Position);
			e.Graphics.FillRectangle(Brushes.LightBlue, Bounds); // Paint background to show "in-edit-mode"
			PaintTrackBar(e.Graphics, Bounds, value, Range.Beg, Range.End);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			Position = ReadValueAt(e.X);
			Refresh();
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			Position = ReadValueAt(e.X);
			Refresh();
		}

		/// <summary>Return the value at pixel position 'x'</summary>
		private int ReadValueAt(int x)
		{
			var frac = Math_.Clamp(Math_.Frac(m_btn_width/2, x, Width - m_btn_width/2), 0f, 1f);
			return Math_.Lerp(Range.Begi, Range.Endi, frac);
		}

		/// <summary>Paint the track bar</summary>
		public static void PaintTrackBar(Graphics gfx, Rectangle cell_bounds, double pos, double min_pos, double max_pos)
		{
			var frac = (float)Math_.Clamp(Math_.Frac(min_pos, pos, max_pos), 0f, 1f);

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

	/// <summary>
	/// A column of track bars.
	/// The min/max values for the track bars can be set in two ways; either set
	/// the min/max values for the entire column, or use the Min/MaxValuePropertyName
	/// properties to data bind to properties on the underlying type.</summary>
	public class DataGridViewTrackBarColumn :DataGridViewCustomColumnBase<DataGridViewTrackBarCell>
	{
		public DataGridViewTrackBarColumn()
			:base(new DataGridViewTrackBarCell{Value=0})
		{
			LiveUpdate = true;
			MinValue = 0;
			MaxValue = 100;
		}

		/// <summary>Get/Set whether the track bars adjust the cell value during edit</summary>
		public bool LiveUpdate { get; set; }

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
	public class DataGridViewTrackBarCell :DataGridViewCustomCellBase<DataGridViewTrackBarEditCtrl>
	{
		public override Type   FormattedValueType { get { return typeof(int); } }
		public override Type   ValueType          { get { return typeof(int); } }
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

		private int GetMinValueInternal(int row_index)
		{
			// If no binding property has been given, return the column min value
			var col = (DataGridViewTrackBarColumn)OwningColumn;
			if (col.MinValuePropertyName == null) return col.MinValue;

			// Invalid row index, return a default
			if (row_index < 0 || row_index >= DataGridView.RowCount)
				return 0;

			var obj = DataGridView.Rows[row_index].DataBoundItem;
			return GetMinValue(col, obj);
		}
		private int GetMaxValueInternal(int row_index)
		{
			// If no binding property has been given, return the column min value
			var col = (DataGridViewTrackBarColumn)OwningColumn;
			if (col.MaxValuePropertyName == null) return col.MaxValue;

			// Invalid row index, return a default
			if (row_index < 0 || row_index >= DataGridView.RowCount)
				return 100;

			var obj = DataGridView.Rows[row_index].DataBoundItem;
			return GetMaxValue(col, obj);
		}

		public int MinValue { get { return GetMinValueInternal(RowIndex); } }
		public int MaxValue { get { return GetMaxValueInternal(RowIndex); } }

		/// <summary>Set the initial state of the editing control</summary>
		protected override void SetInitialValue(DataGridViewTrackBarEditCtrl ctrl, object formatted_value)
		{
			var col = (DataGridViewTrackBarColumn)OwningColumn;
			ctrl.Init((int)formatted_value, MinValue, MaxValue, col.LiveUpdate ? this : null);
		}
		protected override void Paint(Graphics gfx, Rectangle clip_bounds, Rectangle cell_bounds, int row_index, DataGridViewElementStates cell_state, object value, object formatted_value, string error_text, DataGridViewCellStyle cell_style, DataGridViewAdvancedBorderStyle advanced_border_style, DataGridViewPaintParts paint_parts)
		{
			// Paint the cell background
			if (paint_parts.HasFlag(DataGridViewPaintParts.Background))
			{
				using (var bsh = new SolidBrush(Selected ? cell_style.SelectionBackColor : cell_style.BackColor))
					gfx.FillRectangle(bsh, cell_bounds);
			}

			// Paint the cell border
			if (paint_parts.HasFlag(DataGridViewPaintParts.Border))
			{
				PaintBorder(gfx, clip_bounds, cell_bounds, cell_style, advanced_border_style);
			}

			// Paint the cell content
			if (paint_parts.HasFlag(DataGridViewPaintParts.ContentForeground))
			{
				var v  = Convert.ToDouble(formatted_value);
				var mn = GetMinValueInternal(row_index);
				var mx = GetMaxValueInternal(row_index);
				DataGridViewTrackBarEditCtrl.PaintTrackBar(gfx, cell_bounds, v, mn, mx);
			}
		}
	}

	#endregion

	#region Colour Picker
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
	#endregion
}
