using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;

namespace TestCS
{
	public class ListBoxUI :Form
	{
		private pr.gui.ListBox m_lb;

		public ListBoxUI()
		{
			InitializeComponent();

			m_lb.DataSource = new[] {"Item1", "Item2", "Item3", "Item4", "Item5" };
			m_lb.DrawItem += (s,a) =>
			{
				if (a.Index < 0 || a.Index >= m_lb.Items.Count)
					return;

				var item = (string)m_lb.Items[a.Index];
				a.Graphics.FillRectangle(Bit.AllSet(a.State, DrawItemState.Selected) ? Brushes.WhiteSmoke : Brushes.White, a.Bounds);
				a.Graphics.DrawString(item, a.Font, Brushes.Blue, a.Bounds.TopLeft());
			};
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_lb = new pr.gui.ListBox();
			this.SuspendLayout();
			// 
			// listBox1
			// 
			this.m_lb.DisplayProperty = null;
			this.m_lb.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lb.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_lb.FormattingEnabled = true;
			this.m_lb.IntegralHeight = false;
			this.m_lb.ItemHeight = 48;
			this.m_lb.Location = new System.Drawing.Point(0, 0);
			this.m_lb.Name = "listBox1";
			this.m_lb.Size = new System.Drawing.Size(284, 261);
			this.m_lb.TabIndex = 0;
			// 
			// ListBoxUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_lb);
			this.Name = "ListBoxUI";
			this.Text = "list_box_ui";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
