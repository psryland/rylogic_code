using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.gfx;
using pr.maths;
using pr.util;

namespace TestCS
{
	public class DiagramControlUI :Form
	{
		private DiagramControl m_diag;
		private IMessageFilter m_filter;

		static DiagramControlUI()
		{
			View3d.SelectDll(Environment.Is64BitProcess
				? @"\sdk\pr\lib\x64\debug\view3d.dll"
				: @"\sdk\pr\lib\x86\debug\view3d.dll");
		}

		private StatusStrip statusStrip1;
		private ToolStripStatusLabel m_status_mouse_pos;
	
		public DiagramControlUI()
		{
			InitializeComponent();

			ClientSize = new Size(640,480);

			var node0 = new DiagramControl.BoxNode("Node0",100,30){Position = m4x4.Translation(50,10,0)};
			var node1 = new DiagramControl.BoxNode{Text = "Node1", Position = m4x4.Translation(-100,-30,0)};
			m_diag.Elements.Add(node0);
			m_diag.Elements.Add(node1);

			var connector1 = new DiagramControl.Connector(node0, node1);
			m_diag.Elements.Add(connector1);

			m_diag.ResetView();

			m_filter = new Filter(this);
			Load += (s,a) => Application.AddMessageFilter(m_filter);
			FormClosed += (s,a) => Application.RemoveMessageFilter(m_filter);
		}

		private class Filter :IMessageFilter
		{
			private readonly DiagramControlUI m_form;
			public Filter(DiagramControlUI form) { m_form = form; }
			public bool PreFilterMessage(ref Message msg)
			{
				switch ((uint)msg.Msg)
				{
				case Win32.WM_MOUSEMOVE:
					if (m_form.m_diag != null)
					{
						var pt = m_form.m_diag.PointToDiagram(m_form.m_diag.PointToClient(MousePosition));
						m_form.m_status_mouse_pos.Text = "Pos: {0} {1}".Fmt(pt.x.ToString("F3"), pt.y.ToString("F3"));
					}
					break;
				}
				return false;
			}
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_diag = new pr.gui.DiagramControl();
			this.statusStrip1 = new System.Windows.Forms.StatusStrip();
			this.m_status_mouse_pos = new System.Windows.Forms.ToolStripStatusLabel();
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).BeginInit();
			this.statusStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_diag
			// 
			this.m_diag.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_diag.Location = new System.Drawing.Point(0, 0);
			this.m_diag.Name = "m_diag";
			this.m_diag.Size = new System.Drawing.Size(552, 566);
			this.m_diag.TabIndex = 0;
			// 
			// statusStrip1
			// 
			this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status_mouse_pos});
			this.statusStrip1.Location = new System.Drawing.Point(0, 544);
			this.statusStrip1.Name = "statusStrip1";
			this.statusStrip1.Size = new System.Drawing.Size(552, 22);
			this.statusStrip1.TabIndex = 1;
			this.statusStrip1.Text = "statusStrip1";
			// 
			// m_status_mouse_pos
			// 
			this.m_status_mouse_pos.Name = "m_status_mouse_pos";
			this.m_status_mouse_pos.Size = new System.Drawing.Size(118, 17);
			this.m_status_mouse_pos.Text = "toolStripStatusLabel1";
			// 
			// DiagramControlUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(552, 566);
			this.Controls.Add(this.statusStrip1);
			this.Controls.Add(this.m_diag);
			this.Name = "DiagramControlUI";
			this.Text = "DiagramControl";
			((System.ComponentModel.ISupportInitialize)(this.m_diag)).EndInit();
			this.statusStrip1.ResumeLayout(false);
			this.statusStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
