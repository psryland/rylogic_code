using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace TestCS
{
	using RichTextBox = pr.gui.RichTextBox;

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
