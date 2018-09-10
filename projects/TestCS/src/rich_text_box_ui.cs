using System.Windows.Forms;
using Rylogic.Utility;

namespace TestCS
{
	using RichTextBox = Rylogic.Gui.WinForms.RichTextBox;

	public class RichTextBoxUI :Form
	{
		public RichTextBoxUI()
		{
			InitializeComponent();
			var rtb = new RichTextBox{Dock = DockStyle.Fill};
			Controls.Add(rtb);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Text = "rich_text_box";
		}

		#endregion
	}
}
