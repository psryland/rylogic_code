using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace pr.gui
{
	[ProvideProperty("ColumnSpan", typeof(ToolStripItem))]
	[ProvideProperty("RowSpan", typeof(ToolStripItem))]
	[ProvideProperty("Anchor", typeof(ToolStripItem))]
	[ProvideProperty("Dock", typeof(ToolStripItem))]
	class ToolStripTable :ToolStrip, IExtenderProvider
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
}