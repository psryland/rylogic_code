namespace imager
{
	partial class About
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(About));
			this.m_lbl_title = new System.Windows.Forms.Label();
			this.m_pic = new System.Windows.Forms.PictureBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_copyright = new System.Windows.Forms.Label();
			this.m_lbl_version_check = new System.Windows.Forms.Label();
			this.m_lbl_credits = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).BeginInit();
			this.SuspendLayout();
			// 
			// m_lbl_title
			// 
			this.m_lbl_title.AutoSize = true;
			this.m_lbl_title.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_title.Location = new System.Drawing.Point(9, 9);
			this.m_lbl_title.Name = "m_lbl_title";
			this.m_lbl_title.Size = new System.Drawing.Size(68, 24);
			this.m_lbl_title.TabIndex = 2;
			this.m_lbl_title.Text = "Imager";
			// 
			// m_pic
			// 
			this.m_pic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_pic.Image = global::imager.Properties.Resources.Camera256;
			this.m_pic.InitialImage = global::imager.Properties.Resources.Camera256;
			this.m_pic.Location = new System.Drawing.Point(317, 9);
			this.m_pic.Name = "m_pic";
			this.m_pic.Size = new System.Drawing.Size(64, 64);
			this.m_pic.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
			this.m_pic.TabIndex = 3;
			this.m_pic.TabStop = false;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_ok.Location = new System.Drawing.Point(312, 75);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 4;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_copyright
			// 
			this.m_lbl_copyright.AutoSize = true;
			this.m_lbl_copyright.Location = new System.Drawing.Point(10, 36);
			this.m_lbl_copyright.Name = "m_lbl_copyright";
			this.m_lbl_copyright.Size = new System.Drawing.Size(51, 13);
			this.m_lbl_copyright.TabIndex = 5;
			this.m_lbl_copyright.Text = "Copyright";
			// 
			// m_lbl_version_check
			// 
			this.m_lbl_version_check.AutoSize = true;
			this.m_lbl_version_check.Location = new System.Drawing.Point(10, 80);
			this.m_lbl_version_check.Name = "m_lbl_version_check";
			this.m_lbl_version_check.Size = new System.Drawing.Size(35, 13);
			this.m_lbl_version_check.TabIndex = 6;
			this.m_lbl_version_check.Text = "label1";
			// 
			// m_lbl_credits
			// 
			this.m_lbl_credits.AutoSize = true;
			this.m_lbl_credits.Location = new System.Drawing.Point(10, 64);
			this.m_lbl_credits.Name = "m_lbl_credits";
			this.m_lbl_credits.Size = new System.Drawing.Size(35, 13);
			this.m_lbl_credits.TabIndex = 7;
			this.m_lbl_credits.Text = "label1";
			// 
			// About
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_ok;
			this.ClientSize = new System.Drawing.Size(397, 103);
			this.Controls.Add(this.m_lbl_credits);
			this.Controls.Add(this.m_lbl_version_check);
			this.Controls.Add(this.m_lbl_copyright);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_pic);
			this.Controls.Add(this.m_lbl_title);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "About";
			this.Text = "About";
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox m_pic;
		private System.Windows.Forms.Label m_lbl_title;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Label m_lbl_copyright;
		private System.Windows.Forms.Label m_lbl_version_check;
		private System.Windows.Forms.Label m_lbl_credits;
	}
}