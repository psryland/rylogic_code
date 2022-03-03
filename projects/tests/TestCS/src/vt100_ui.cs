using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Gui.WinForms;
using Rylogic.Scintilla;
using Util = Rylogic.Utility.Util;

namespace TestCS
{
	public class VT100UI :Form
	{
		public VT100UI()
		{
			InitializeComponent();

			Sci.LoadDll(".\\lib\\$(platform)\\$(config)");

			var settings = new VT100.Settings();
			var buffer = new VT100.Buffer(settings);
			var term = new VT100Display(buffer) {Dock = DockStyle.Fill};

			buffer.Output(VT100.Buffer.TestConsoleString0);
			buffer.Output(VT100.Buffer.TestConsoleString1);
			buffer.Output("This string should be deleted " + "\x1b[0G\x1b[K" + "Replaced by this string");

			Controls.Add(term);
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
			this.SuspendLayout();
			// 
			// VT100UI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(641, 619);
			this.Name = "VT100UI";
			this.Text = "vt100_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
