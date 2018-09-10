using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Utility;

namespace TestCS
{
	public class HelpUI :Form
	{
		public HelpUI()
		{
			InitializeComponent();

			m_btn_plaintext.Click += PlainText;
			m_btn_richtext .Click += RichText;
			m_btn_html     .Click += HtmlText;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		private void PlainText(object sender, EventArgs e)
		{
			var content = "This is a Plain text dialog";
			Rylogic.Gui.WinForms.HelpUI.ShowDialog(this, Rylogic.Gui.WinForms.HelpUI.EContent.Text, "Plain Text Help", content);
		}
		private void RichText(object sender, EventArgs e)
		{
			var rtf = new Rtf.Builder();
			rtf.TextStyle = new Rtf.TextStyle
			{
				FontIndex = rtf.FontIndex(Rtf.FontDesc.CourierNew),
				FontStyle = Rtf.EFontStyle.Bold|Rtf.EFontStyle.Underline,
				ForeColourIndex = rtf.ColourIndex(Color.Red),
				BackColourIndex = rtf.ColourIndex(Color.Green),
				FontSize = 18,
			};
			rtf.Append("Heading\n");
			rtf.TextStyle = Rtf.TextStyle.Default;
			rtf.Append("More Rtf text");

			var content = rtf.ToString();
			Rylogic.Gui.WinForms.HelpUI.ShowDialog(this, Rylogic.Gui.WinForms.HelpUI.EContent.Rtf, "RTF Help", content);
		}
		private void HtmlText(object sender, EventArgs e)
		{
			var content = 
			@"<html>
				<h3>Heading</h3>
				<p>paragraph paragraph paragraph</p>
				<a href='http://www.google.com'>Link to Google</a>
			</html>";
			Rylogic.Gui.WinForms.HelpUI.ShowDialog(this, Rylogic.Gui.WinForms.HelpUI.EContent.Html, "Html Help", content);
		}

		private Button m_btn_plaintext;
		private Button m_btn_richtext;
		private Button m_btn_html;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_btn_plaintext = new System.Windows.Forms.Button();
			this.m_btn_richtext = new System.Windows.Forms.Button();
			this.m_btn_html = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_btn_plaintext
			// 
			this.m_btn_plaintext.Location = new System.Drawing.Point(12, 12);
			this.m_btn_plaintext.Name = "m_btn_plaintext";
			this.m_btn_plaintext.Size = new System.Drawing.Size(122, 23);
			this.m_btn_plaintext.TabIndex = 0;
			this.m_btn_plaintext.Text = "Plain Text";
			this.m_btn_plaintext.UseVisualStyleBackColor = true;
			// 
			// m_btn_richtext
			// 
			this.m_btn_richtext.Location = new System.Drawing.Point(12, 41);
			this.m_btn_richtext.Name = "m_btn_richtext";
			this.m_btn_richtext.Size = new System.Drawing.Size(122, 23);
			this.m_btn_richtext.TabIndex = 1;
			this.m_btn_richtext.Text = "Rich Text";
			this.m_btn_richtext.UseVisualStyleBackColor = true;
			// 
			// m_btn_html
			// 
			this.m_btn_html.Location = new System.Drawing.Point(12, 70);
			this.m_btn_html.Name = "m_btn_html";
			this.m_btn_html.Size = new System.Drawing.Size(122, 23);
			this.m_btn_html.TabIndex = 2;
			this.m_btn_html.Text = "Html";
			this.m_btn_html.UseVisualStyleBackColor = true;
			// 
			// HelpUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(282, 253);
			this.Controls.Add(this.m_btn_html);
			this.Controls.Add(this.m_btn_richtext);
			this.Controls.Add(this.m_btn_plaintext);
			this.Name = "HelpUI";
			this.Text = "help_ui";
			this.ResumeLayout(false);

		}

		#endregion
	}
}
