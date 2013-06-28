namespace RyLogViewer
{
	partial class NewVersionForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewVersionForm));
			this.m_link_website = new System.Windows.Forms.LinkLabel();
			this.m_lbl_new_version = new System.Windows.Forms.Label();
			this.m_link_download = new System.Windows.Forms.LinkLabel();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_edit_latest_version = new System.Windows.Forms.Label();
			this.m_edit_current_version = new System.Windows.Forms.Label();
			this.m_pic = new System.Windows.Forms.PictureBox();
			this.m_lbl_download = new System.Windows.Forms.Label();
			this.m_lbl_website = new System.Windows.Forms.Label();
			this.m_lbl_latest_version = new System.Windows.Forms.Label();
			this.m_lbl_current_version = new System.Windows.Forms.Label();
			this.m_btn_install = new System.Windows.Forms.Button();
			this.m_panel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).BeginInit();
			this.SuspendLayout();
			// 
			// m_link_website
			// 
			this.m_link_website.AutoSize = true;
			this.m_link_website.Location = new System.Drawing.Point(185, 87);
			this.m_link_website.Name = "m_link_website";
			this.m_link_website.Size = new System.Drawing.Size(126, 13);
			this.m_link_website.TabIndex = 0;
			this.m_link_website.TabStop = true;
			this.m_link_website.Text = "Visit website for more info";
			// 
			// m_lbl_new_version
			// 
			this.m_lbl_new_version.AutoSize = true;
			this.m_lbl_new_version.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_new_version.Location = new System.Drawing.Point(99, 21);
			this.m_lbl_new_version.Name = "m_lbl_new_version";
			this.m_lbl_new_version.Size = new System.Drawing.Size(159, 13);
			this.m_lbl_new_version.TabIndex = 1;
			this.m_lbl_new_version.Text = "A new version is available!";
			// 
			// m_link_download
			// 
			this.m_link_download.AutoSize = true;
			this.m_link_download.Location = new System.Drawing.Point(186, 109);
			this.m_link_download.Name = "m_link_download";
			this.m_link_download.Size = new System.Drawing.Size(138, 13);
			this.m_link_download.TabIndex = 2;
			this.m_link_download.TabStop = true;
			this.m_link_download.Text = "Download the latest version";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(328, 161);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 3;
			this.m_btn_ok.Text = "Later";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_panel.Controls.Add(this.m_edit_latest_version);
			this.m_panel.Controls.Add(this.m_edit_current_version);
			this.m_panel.Controls.Add(this.m_pic);
			this.m_panel.Controls.Add(this.m_lbl_download);
			this.m_panel.Controls.Add(this.m_lbl_website);
			this.m_panel.Controls.Add(this.m_lbl_latest_version);
			this.m_panel.Controls.Add(this.m_lbl_current_version);
			this.m_panel.Controls.Add(this.m_lbl_new_version);
			this.m_panel.Controls.Add(this.m_link_website);
			this.m_panel.Controls.Add(this.m_link_download);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(417, 150);
			this.m_panel.TabIndex = 4;
			// 
			// m_edit_latest_version
			// 
			this.m_edit_latest_version.AutoSize = true;
			this.m_edit_latest_version.Location = new System.Drawing.Point(185, 65);
			this.m_edit_latest_version.Name = "m_edit_latest_version";
			this.m_edit_latest_version.Size = new System.Drawing.Size(85, 13);
			this.m_edit_latest_version.TabIndex = 9;
			this.m_edit_latest_version.Text = "Current Version: ";
			// 
			// m_edit_current_version
			// 
			this.m_edit_current_version.AutoSize = true;
			this.m_edit_current_version.Location = new System.Drawing.Point(185, 43);
			this.m_edit_current_version.Name = "m_edit_current_version";
			this.m_edit_current_version.Size = new System.Drawing.Size(85, 13);
			this.m_edit_current_version.TabIndex = 8;
			this.m_edit_current_version.Text = "Current Version: ";
			// 
			// m_pic
			// 
			this.m_pic.BackgroundImage = global::RyLogViewer.Properties.Resources.important;
			this.m_pic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.m_pic.ErrorImage = null;
			this.m_pic.InitialImage = null;
			this.m_pic.Location = new System.Drawing.Point(12, 21);
			this.m_pic.Name = "m_pic";
			this.m_pic.Size = new System.Drawing.Size(71, 63);
			this.m_pic.TabIndex = 7;
			this.m_pic.TabStop = false;
			// 
			// m_lbl_download
			// 
			this.m_lbl_download.AutoSize = true;
			this.m_lbl_download.Location = new System.Drawing.Point(126, 109);
			this.m_lbl_download.Name = "m_lbl_download";
			this.m_lbl_download.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_download.TabIndex = 6;
			this.m_lbl_download.Text = "Download:";
			// 
			// m_lbl_website
			// 
			this.m_lbl_website.AutoSize = true;
			this.m_lbl_website.Location = new System.Drawing.Point(135, 87);
			this.m_lbl_website.Name = "m_lbl_website";
			this.m_lbl_website.Size = new System.Drawing.Size(49, 13);
			this.m_lbl_website.TabIndex = 5;
			this.m_lbl_website.Text = "Website:";
			// 
			// m_lbl_latest_version
			// 
			this.m_lbl_latest_version.AutoSize = true;
			this.m_lbl_latest_version.Location = new System.Drawing.Point(104, 65);
			this.m_lbl_latest_version.Name = "m_lbl_latest_version";
			this.m_lbl_latest_version.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_latest_version.TabIndex = 4;
			this.m_lbl_latest_version.Text = "Latest Version: ";
			// 
			// m_lbl_current_version
			// 
			this.m_lbl_current_version.AutoSize = true;
			this.m_lbl_current_version.Location = new System.Drawing.Point(99, 43);
			this.m_lbl_current_version.Name = "m_lbl_current_version";
			this.m_lbl_current_version.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_current_version.TabIndex = 3;
			this.m_lbl_current_version.Text = "Current Version: ";
			// 
			// m_btn_install
			// 
			this.m_btn_install.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.m_btn_install.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_install.Location = new System.Drawing.Point(247, 161);
			this.m_btn_install.Name = "m_btn_install";
			this.m_btn_install.Size = new System.Drawing.Size(75, 23);
			this.m_btn_install.TabIndex = 5;
			this.m_btn_install.Text = "Install Now";
			this.m_btn_install.UseVisualStyleBackColor = true;
			// 
			// NewVersionForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(415, 196);
			this.Controls.Add(this.m_btn_install);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NewVersionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Version Available";
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.LinkLabel m_link_website;
		private System.Windows.Forms.Label m_lbl_new_version;
		private System.Windows.Forms.LinkLabel m_link_download;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Panel m_panel;
		private System.Windows.Forms.Label m_lbl_download;
		private System.Windows.Forms.Label m_lbl_website;
		private System.Windows.Forms.Label m_lbl_latest_version;
		private System.Windows.Forms.Label m_lbl_current_version;
		private System.Windows.Forms.PictureBox m_pic;
		private System.Windows.Forms.Label m_edit_latest_version;
		private System.Windows.Forms.Label m_edit_current_version;
		private System.Windows.Forms.Button m_btn_install;
	}
}