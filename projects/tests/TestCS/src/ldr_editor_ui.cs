using System.Windows.Forms;
using Rylogic.Gfx;
using Rylogic.Gui.WinForms;
using Rylogic.Scintilla;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class LdrEditorUI :Form
	{
		private View3d m_view3d;
		private StatusStrip m_status_strip;
		private ScintillaCtrl m_sci;
		private ToolStripStatusLabel m_status;

		static LdrEditorUI()
		{
			Sci.LoadDll(@"..\..\..\..\lib\$(platform)\$(config)\");
			View3d.LoadDll(@"..\..\..\..\lib\$(platform)\$(config)\");
		}
		public LdrEditorUI()
		{
			InitializeComponent();
			m_view3d = View3d.Create();
			m_sci.InitLdrStyle();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_view3d);
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.</summary>
		private void InitializeComponent()
		{
			this.m_status_strip = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_sci = new Rylogic.Gui.WinForms.ScintillaCtrl();
			this.m_status_strip.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_status_strip
			// 
			this.m_status_strip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_status_strip.Location = new System.Drawing.Point(0, 325);
			this.m_status_strip.Name = "m_status_strip";
			this.m_status_strip.Size = new System.Drawing.Size(339, 22);
			this.m_status_strip.TabIndex = 1;
			this.m_status_strip.Text = "statusStrip1";
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(118, 17);
			this.m_status.Text = "toolStripStatusLabel1";
			// 
			// m_sci
			// 
			this.m_sci.Location = new System.Drawing.Point(12, 12);
			this.m_sci.Name = "m_sci";
			this.m_sci.Size = new System.Drawing.Size(315, 310);
			this.m_sci.TabIndex = 2;
			// 
			// LdrEditorUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(339, 347);
			this.Controls.Add(this.m_sci);
			this.Controls.Add(this.m_status_strip);
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
