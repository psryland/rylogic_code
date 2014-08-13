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
using pr.gfx;
using pr.util;

namespace TestCS
{
	public class LdrEditorUI :Form
	{
		private View3d.Editor m_editor;

		public LdrEditorUI()
		{
			//m_editor = new View3d.Editor(Handle, 200, 200);
			
			InitializeComponent();
			m_host_editor.Child = new View3d.HostableEditor();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_editor);
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
			// LdrEditorUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Controls.Add(this.m_host_editor);
			this.Name = "LdrEditorUI";
			this.Text = "ldr_editor_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
