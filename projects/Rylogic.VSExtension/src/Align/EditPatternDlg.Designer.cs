namespace Rylogic.VSExtension
{
	partial class EditPatternDlg
	{
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_pattern_ui = new pr.gui.PatternUI();
			this.m_edit_comment = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_pattern_ui
			// 
			this.m_pattern_ui.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_pattern_ui.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_pattern_ui.Location = new System.Drawing.Point(0, 0);
			this.m_pattern_ui.Margin = new System.Windows.Forms.Padding(0);
			this.m_pattern_ui.MinimumSize = new System.Drawing.Size(357, 138);
			this.m_pattern_ui.Name = "m_pattern_ui";
			this.m_pattern_ui.Size = new System.Drawing.Size(444, 218);
			this.m_pattern_ui.TabIndex = 1;
			this.m_pattern_ui.TestText = "Enter text here to test your pattern";
			this.m_pattern_ui.Touched = false;
			// 
			// m_edit_comment
			// 
			this.m_edit_comment.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_comment.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_edit_comment.Location = new System.Drawing.Point(0, 221);
			this.m_edit_comment.Multiline = true;
			this.m_edit_comment.Name = "m_edit_comment";
			this.m_edit_comment.Size = new System.Drawing.Size(444, 57);
			this.m_edit_comment.TabIndex = 2;
			// 
			// EditPatternDlg
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(444, 279);
			this.Controls.Add(this.m_edit_comment);
			this.Controls.Add(this.m_pattern_ui);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Name = "EditPatternDlg";
			this.Text = "Edit Alignment Pattern";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private pr.gui.PatternUI m_pattern_ui;
		private System.Windows.Forms.TextBox m_edit_comment;
	}
}