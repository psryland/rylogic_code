using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using pr.extn;
using pr.gfx;
using pr.util;

namespace TestCS
{
	public class LdrEditorUI :Form
	{
		private View3d m_view3d;
		private StatusStrip m_status_strip;
		private ToolStripStatusLabel m_status;
		private View3d.Editor m_editor;

		static LdrEditorUI()
		{
			View3d.LoadDll(@"..\..\..\..\lib\$(platform)\$(config)\");
		}
		public LdrEditorUI()
		{
			InitializeComponent();
			m_view3d = new View3d();
			
			var editor = new View3d.HostableEditor(0x1000);
			editor.TextChanged += (s,a) => m_status.SetStatusMessage(msg:editor.Text.Summary(50), on_click:(ss,aa) => editor.Text = Text.Summary(50));
			m_host_editor.Child = editor;
			
			//m_editor = new View3d.Editor(Handle, 200, 200);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_host_editor);
			Util.Dispose(ref m_editor);
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private ElementHost m_host_editor;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.m_host_editor = new System.Windows.Forms.Integration.ElementHost();
			this.m_status_strip = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_status_strip.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_host_editor
			// 
			this.m_host_editor.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_host_editor.Location = new System.Drawing.Point(0, 0);
			this.m_host_editor.Name = "m_host_editor";
			this.m_host_editor.Size = new System.Drawing.Size(284, 261);
			this.m_host_editor.TabIndex = 0;
			this.m_host_editor.Text = "elementHost1";
			this.m_host_editor.Child = null;
			// 
			// m_status_strip
			// 
			this.m_status_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_status_strip.Location = new System.Drawing.Point(0, 239);
			this.m_status_strip.Name = "m_status_strip";
			this.m_status_strip.Size = new System.Drawing.Size(284, 22);
			this.m_status_strip.TabIndex = 1;
			this.m_status_strip.Text = "statusStrip1";
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(118, 17);
			this.m_status.Text = "toolStripStatusLabel1";
			// 
			// LdrEditorUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_status_strip);
			this.Controls.Add(this.m_host_editor);
			this.Name = "LdrEditorUI";
			this.Text = "ldr_editor_ui";
			this.m_status_strip.ResumeLayout(false);
			this.m_status_strip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
