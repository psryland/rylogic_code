using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.VisualStudio.DebuggerVisualizers;
using pr.gui;

[assembly: System.Diagnostics.DebuggerVisualizer(
	typeof(pr.StringVisualiser.Visualiser),
	typeof(pr.StringVisualiser.Source),
	Target = typeof(System.String),
	Description = "Rylogic String Visualiser")]

namespace pr
{
	public class StringVisualiser :ToolForm
	{
		public class Visualiser :VisualiserBase<string, StringVisualiser> {}
		public class Source     :VisualiserObjectSourceBase<string> {}

		private gui.RichTextBox m_rtb;
	
		public StringVisualiser(string obj)
		{
			InitializeComponent();
			m_rtb.Text = obj;
		}

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(StringVisualiser));
			this.m_rtb = new pr.gui.RichTextBox();
			this.SuspendLayout();
			// 
			// m_rtb
			// 
			this.m_rtb.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_rtb.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_rtb.Location = new System.Drawing.Point(0, 0);
			this.m_rtb.Name = "m_rtb";
			this.m_rtb.Size = new System.Drawing.Size(343, 292);
			this.m_rtb.TabIndex = 0;
			this.m_rtb.Text = "";
			// 
			// StringVisualiser
			// 
			this.ClientSize = new System.Drawing.Size(343, 292);
			this.Controls.Add(this.m_rtb);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "StringVisualiser";
			this.ResumeLayout(false);

		}
	}
}
