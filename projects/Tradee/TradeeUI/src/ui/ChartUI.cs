using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;
using pr.extn;

namespace Tradee
{
	/// <summary>Base class for charts</summary>
	public class ChartUI :BaseUI
	{
		#region UI Elements
		private pr.gui.ToolStripContainer m_tsc;
		private pr.gui.ChartControl m_chart;
		#endregion

		public ChartUI(MainModel model, string symbol) :base(model, symbol != null ? "Chart - {0}".Fmt(symbol) : "Chart")
		{
			InitializeComponent();
			Symbol = symbol;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The symbol displayed on the chart (or empty string if none)</summary>
		public string Symbol { get; private set; }

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			m_chart.XAxis.ShowGridLines = true;
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_chart = new pr.gui.ChartControl();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_chart);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(668, 648);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(668, 673);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_chart
			// 
			this.m_chart.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_chart.Location = new System.Drawing.Point(43, 40);
			this.m_chart.Name = "m_chart";
			this.m_chart.Size = new System.Drawing.Size(581, 558);
			this.m_chart.TabIndex = 0;
			// 
			// ChartUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_tsc);
			this.Name = "ChartUI";
			this.Size = new System.Drawing.Size(668, 673);
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
