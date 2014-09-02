using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using pr.extn;

namespace pr.gui
{
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
			DropDown.As<ToolStripDropDownSingleSelect>().Selected = e.ClickedItem;
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
}
