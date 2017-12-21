using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using pr.common;
using pr.extn;

namespace pr.gui
{
	#region ToolStripValueBox
	public class ToolStripValueBox :ToolStripControlHost
	{
		public ToolStripValueBox()
			:this(string.Empty)
		{ }
		public ToolStripValueBox(string name)
			:base(new ValueBox { Name = name }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = ValueBox.Name = value; }
		}

		/// <summary>The hosted combo box</summary>
		public ValueBox ValueBox
		{
			get { return (ValueBox)Control; }
		}
	}
	#endregion

	#region ToolStripComboBox
	/// <summary>
	/// A replacement for ToolStripComboBox that preserves the text selection across focus lost/gained
	/// and also doesn't throw a first chance exception when then the combo box data source is set to null</summary>
	public class ToolStripComboBox :ToolStripControlHost
	{
		public ToolStripComboBox()
			:this(string.Empty)
		{ }
		public ToolStripComboBox(string name)
			:base(new ComboBox { Name = name }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = ComboBox.Name = value; }
		}

		/// <summary>The hosted combo box</summary>
		public ComboBox ComboBox
		{
			get { return (ComboBox)Control; }
		}

		/// <summary>The items displayed in the combo box</summary>
		public ComboBox.ObjectCollection Items
		{
			get { return ComboBox.Items; }
		}

		/// <summary>Get/Set the selected item</summary>
		public int SelectedIndex
		{
			get { return ComboBox.SelectedIndex; }
			set { ComboBox.SelectedIndex = value; }
		}

		/// <summary>Raised when the selected index changes</summary>
		public event EventHandler SelectedIndexChanged
		{
			add { ComboBox.SelectedIndexChanged += value; }
			remove { ComboBox.SelectedIndexChanged -= value; }
		}

		/// <summary>The selected item</summary>
		public object SelectedItem
		{
			get { return ComboBox.SelectedItem; }
			set { ComboBox.SelectedItem = value; }
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public string SelectedText
		{
			get { return ComboBox.SelectedText; }
			set { ComboBox.SelectedText = value; }
		}

		/// <summary>Gets/Sets the style of the combo box</summary>
		public ComboBoxStyle DropDownStyle
		{
			get { return ComboBox.DropDownStyle; }
			set { ComboBox.DropDownStyle = value; }
		}

		/// <summary>Get/Set the appearance of the combo box</summary>
		public FlatStyle FlatStyle
		{
			get { return ComboBox.FlatStyle; }
			set { ComboBox.FlatStyle = value; }
		}
	}
	#endregion

	#region ToolStripTrackBar
	public class ToolStripTrackBar :ToolStripControlHost
	{
		public ToolStripTrackBar()
			:this(string.Empty)
		{ }
		public ToolStripTrackBar(string name)
			:base(new TrackBar{ Name = name, AutoSize = false, TickStyle = TickStyle.None, Size = new Size(80,18) }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = TrackBar.Name = value; }
		}

		/// <summary>The hosted combo box</summary>
		public TrackBar TrackBar
		{
			get { return (TrackBar)Control; }
		}

		/// <summary>The value of the track bar</summary>
		public int Value
		{
			get { return TrackBar.Value; }
			set { TrackBar.Value = value; }
		}
	}
	#endregion

	#region ToolStripDateTimePicker
	public class ToolStripDateTimePicker :ToolStripControlHost
	{
		public ToolStripDateTimePicker()
			:this(string.Empty)
		{}
		public ToolStripDateTimePicker(string name)
			:base(new DateTimePicker{Name = name}, name)
		{}

		/// <summary>The hosted control</summary>
		public DateTimePicker DateTimePicker
		{
			get { return (DateTimePicker)Control; }
		}

		/// <summary>The default DateTime.Kind</summary>
		public DateTimeKind Kind
		{
			get { return DateTimePicker.Kind; }
			set { DateTimePicker.Kind = value; }
		}

		/// <summary>Gets or sets the custom date/time format string.</summary>
		public string CustomFormat
		{
			get { return DateTimePicker.CustomFormat; }
			set { DateTimePicker.CustomFormat = value; }
		}

		/// <summary>Gets or sets the format of the date and time displayed in the control.</summary>
		public DateTimePickerFormat Format
		{
			get { return DateTimePicker.Format; }
			set { DateTimePicker.Format = value; }
		}

		///<summary>Gets or sets the date/time value assigned to the control.</summary>
		public DateTime Value
		{
			get { return DateTimePicker.Value; }
			set { DateTimePicker.Value = value; }
		}

		///<summary>Gets or sets the minimum date and time that can be selected in the control.</summary>
		public DateTime MinDate
		{
			get { return DateTimePicker.MinDate; }
			set { DateTimePicker.MinDate = value; }
		}

		///<summary>Gets or sets the maximum date and time that can be selected in the control.</summary>
		public DateTime MaxDate
		{
			get { return DateTimePicker.MaxDate; }
			set { DateTimePicker.MaxDate = value; }
		}
	}
	#endregion

	#region ToolStripDropDownSingleSelect
	public class ToolStripDropDownSingleSelect :ToolStripDropDown
	{
		/// <summary>Get the menu items in the drop down list</summary>
		public IEnumerable<ToolStripMenuItem> MenuItems
		{
			get { return Items.OfType<ToolStripMenuItem>(); }
		}

		/// <summary>Return the item currently selected in the drop down</summary>
		public ToolStripItem Selected
		{
			get { return MenuItems.FirstOrDefault(x => x.Checked); }
			set
			{
				foreach (var item in MenuItems)
					if (!ReferenceEquals(item,value))
						item.Checked = false;
			}
		}

		protected override void OnItemClicked(ToolStripItemClickedEventArgs e)
		{
			Selected = e.ClickedItem;
			base.OnItemClicked(e);
		}
	}
	#endregion

	#region ToolStripPatternFilter
	public class ToolStripPatternFilter :ToolStripControlHost
	{
		public ToolStripPatternFilter()
			:this(string.Empty)
		{ }
		public ToolStripPatternFilter(string name)
			:base(new PatternFilter { Name = name }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = PatternFilter.Name = value; }
		}

		/// <summary>The hosted control</summary>
		public PatternFilter PatternFilter
		{
			get { return (PatternFilter)Control; }
		}

		/// <summary>The current pattern</summary>
		public Pattern Pattern
		{
			get { return PatternFilter.Pattern; }
			set { PatternFilter.Pattern = value; }
		}

		/// <summary>The current pattern</summary>
		public Pattern[] History
		{
			get { return PatternFilter.History; }
			set { PatternFilter.History = value; }
		}

		/// <summary>Raised when the pattern is changed</summary>
		public event EventHandler PatternChanged
		{
			add { PatternFilter.PatternChanged += value; }
			remove { PatternFilter.PatternChanged -= value; }
		}
	}
	#endregion

	#region ToolStripSplitButtonCheckable
	public class ToolStripSplitButtonCheckable :ToolStripSplitButton
	{
		private bool m_checked;
		private static ProfessionalColorTable m_colour_table;

		public ToolStripSplitButtonCheckable()
		{
			DropDown       = new ToolStripDropDownSingleSelect();
			Checked        = false;
			CheckOnClicked = false;
		}

		/// <summary>Get/Set the checked state of the button</summary>
		public bool Checked
		{
			get { return m_checked; }
			set
			{
				if (m_checked == value) return;
				m_checked = value;
				CheckedChanged.Raise(this, EventArgs.Empty);
				Invalidate();
			}
		}

		/// <summary>Automatically check/uncheck the button on click</summary>
		public bool CheckOnClicked { get; set; }

		/// <summary>Raised when the 'Checked' property changes</summary>
		public event EventHandler CheckedChanged;

		protected override void OnDefaultItemChanged(EventArgs e)
		{
			base.OnDefaultItemChanged(e);
			
			// There is a retarded bug in .NET where the OnDefaultItemChanged method is called
			// before the default item is changed, so there's no way to see what the new default item is.
			this.BeginInvokeDelayed(0, () =>
			{
				if (DefaultItem != null)
				{
					Text  = DefaultItem.Text;
					Image = DefaultItem.Image;
				}
			});
		}
		protected override void OnDropDownItemClicked(ToolStripItemClickedEventArgs e)
		{
			DefaultItem = e.ClickedItem;
			((ToolStripDropDownSingleSelect)DropDown).Selected = e.ClickedItem;
			base.OnDropDownItemClicked(e);
		}
		protected override void OnButtonClick(System.EventArgs e)
		{
			if (CheckOnClicked && ButtonSelected) Checked = !Checked;
			base.OnButtonClick(e);
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			if (m_checked && Size != Size.Empty)
			{
				var g = e.Graphics;
				var bounds = new Rectangle(Point.Empty, Size);
				var use_system_colours = ColorTable.UseSystemColors || !ToolStripManager.VisualStylesEnabled;
				if (use_system_colours)
				{
					using (var b = new SolidBrush(ColorTable.ButtonCheckedHighlight))
						g.FillRectangle(b, bounds);
				}
				else
				{
					using (var b = new LinearGradientBrush(bounds, ColorTable.ButtonCheckedGradientBegin, ColorTable.ButtonCheckedGradientEnd, LinearGradientMode.Vertical))
						g.FillRectangle(b, bounds);
				}
				using (var p = new Pen(ColorTable.ButtonSelectedBorder))
					g.DrawRectangle(p, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
			}
			base.OnPaint(e);
		}

		private static ProfessionalColorTable ColorTable
		{
			get { return m_colour_table ?? (m_colour_table = new ProfessionalColorTable()); }
		}
	}
	#endregion

	#region ToolStripTable
	[ProvideProperty("ColumnSpan", typeof(ToolStripItem))]
	[ProvideProperty("RowSpan", typeof(ToolStripItem))]
	[ProvideProperty("Anchor", typeof(ToolStripItem))]
	[ProvideProperty("Dock", typeof(ToolStripItem))]
	public class ToolStripTable :ToolStrip, IExtenderProvider
	{
		public ToolStripTable()
		{
			LayoutStyle = ToolStripLayoutStyle.Table;
			RowCount = 3;
			ColumnCount = 3;
		}

		private TableLayoutSettings TableLayoutSettings
		{
			get { return LayoutSettings as TableLayoutSettings; }
		}

		[DefaultValue(3)]
		public int RowCount
		{
			get { return TableLayoutSettings != null ? TableLayoutSettings.RowCount : -1; }
			set
			{
				if (TableLayoutSettings == null) return;
				TableLayoutSettings.RowCount = value;
			}
		}

		[DefaultValue(3)]
		public int ColumnCount
		{
			get { return TableLayoutSettings != null ? TableLayoutSettings.ColumnCount : -1; }
			set
			{
				if (TableLayoutSettings == null) return;
				TableLayoutSettings.ColumnCount = value;
			}
		}

		public override Size GetPreferredSize(Size proposedSize)
		{
			// be friendly if there's no items left to  pin the control open.
			return Items.Count == 0 ? DefaultSize : base.GetPreferredSize(proposedSize);
		}

		[DefaultValue(1)]
		[DisplayName("ColumnSpan")]
		public int GetColumnSpan(object target)
		{
			return TableLayoutSettings.GetColumnSpan(target);
		}
		public void SetColumnSpan(object target, int value)
		{
			TableLayoutSettings.SetColumnSpan(target, value);
		}

		[DefaultValue(1)]
		[DisplayName("RowSpan")]
		public int GetRowSpan(object target)
		{
			return TableLayoutSettings != null ? TableLayoutSettings.GetRowSpan(target) : 1;
		}
		public void SetRowSpan(object target, int value)
		{
			if (TableLayoutSettings == null) return;
			TableLayoutSettings.SetRowSpan(target, value);
		}

		//[Editor(typeof(CollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TableLayoutColumnStyleCollection ColumnStyles
		{
			get { return TableLayoutSettings != null ? TableLayoutSettings.ColumnStyles : null; }
		}

		//[Editor(typeof(CollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public TableLayoutRowStyleCollection RowStyles
		{
			get { return TableLayoutSettings != null ? TableLayoutSettings.RowStyles : null; }
		}

		[DisplayName("Anchor")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public AnchorStyles GetAnchor(object target)
		{
			var tsi = target as ToolStripItem;
			return (tsi != null) ? tsi.Anchor : AnchorStyles.None;
		}
		public void SetAnchor(object target, AnchorStyles value)
		{
			var tsi = target as ToolStripItem;
			if (tsi != null)
			{
				tsi.Anchor = value;
			}
		}

		[DisplayName("Dock")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockStyle GetDock(object target)
		{
			var tsi = target as ToolStripItem;
			return (tsi != null) ? tsi.Dock : DockStyle.None;
		}
		public void SetDock(object target, DockStyle value)
		{
			var tsi = target as ToolStripItem;
			if (tsi != null) tsi.Dock = value;
		}

		bool IExtenderProvider.CanExtend(object extendee)
		{
			var tsi = extendee as ToolStripItem;
			return tsi != null && tsi.Owner == this;
		}
	}
	#endregion
}
