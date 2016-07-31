using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;

namespace TestCS
{
	public class ChartUI :Form
	{
		private ChartControl m_chart;
		private View3d.Object m_obj0;

		public ChartUI()
		{
			InitializeComponent();
			m_chart = Controls.Add2(new ChartControl { Dock = DockStyle.Fill });

			m_obj0 = new View3d.Object("*box nice_box FF00FF00 { 0.3 0.2 0.4 }", false);
			m_chart.ChartRendering += (s,a) =>
			{
				a.AddToScene(m_obj0);
			};
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_obj0);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ChartUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(456, 482);
			this.Name = "ChartUI";
			this.Text = "ChartUI";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
