using System.Windows.Forms;
using Rylogic.Gui;
using Rylogic.Scintilla;
using Rylogic.Utility;

namespace TestCS
{
	public class ScintillaUI :Form
	{
		private ScintillaCtrl m_text;
		public ScintillaUI()
		{
			InitializeComponent();
			Sci.LoadDll(".\\lib\\$(platform)\\debug");
			Controls.Add(m_text = new ScintillaCtrl() { Dock = DockStyle.Fill });

			m_text.Text = "Hello World!";
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

	
		#region Windows Form Designer generated code

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.SuspendLayout();
			// 
			// ScintillaUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 261);
			this.Name = "ScintillaUI";
			this.Text = "scintilla_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
