namespace test.test.ui
{
	partial class ProfileTest
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
			this.m_edit = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_edit
			// 
			this.m_edit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_edit.Location = new System.Drawing.Point(0, 0);
			this.m_edit.Multiline = true;
			this.m_edit.Name = "m_edit";
			this.m_edit.Size = new System.Drawing.Size(284, 262);
			this.m_edit.TabIndex = 0;
			// 
			// ProfileTest
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.m_edit);
			this.Name = "ProfileTest";
			this.Text = "profile_test";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_edit;
	}
}