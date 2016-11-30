using System;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace LDraw
{
	public class ScriptUI :BaseUI
	{
		#region UI Elements
		private ToolStripContainer m_tsc;
		private ElementHost m_element_host;
		private ToolStrip m_ts;
		#endregion

		public ScriptUI(Model model)
			:base(model, "Script")
		{
			InitializeComponent();

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>The editor containing Ldr script</summary>
		public View3d.HostableEditor Editor
		{
			get { return m_editor; }
			private set
			{
				if (m_editor == value) return;
				//Util.Dispose(ref m_editor); not needed, the ElementHost does the clean up
				m_editor = value;
			}
		}
		private View3d.HostableEditor m_editor;

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			Editor = new View3d.HostableEditor(m_element_host, true);
		}

		/// <summary>Update the state of UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
		}

		#region
		private void InitializeComponent()
		{
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_element_host = new System.Windows.Forms.Integration.ElementHost();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_tsc
			// 
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_element_host);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(495, 605);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.TabIndex = 0;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Location = new System.Drawing.Point(3, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(111, 25);
			this.m_ts.TabIndex = 0;
			// 
			// m_element_host
			// 
			this.m_element_host.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_element_host.Location = new System.Drawing.Point(0, 0);
			this.m_element_host.Name = "m_element_host";
			this.m_element_host.Size = new System.Drawing.Size(495, 605);
			this.m_element_host.TabIndex = 0;
			this.m_element_host.Text = "elementHost1";
			this.m_element_host.Child = null;
			// 
			// ScriptUI
			// 
			this.Controls.Add(this.m_tsc);
			this.Name = "ScriptUI";
			this.Size = new System.Drawing.Size(495, 630);
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
