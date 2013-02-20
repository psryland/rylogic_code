namespace RyLogViewer
{
	partial class AndroidLogcatUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AndroidLogcatUI));
			this.m_edit_adb_fullpath = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_btn_browse_adb = new System.Windows.Forms.Button();
			this.m_listbox_devices = new System.Windows.Forms.ListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.m_lbl_logcat_command = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_group = new System.Windows.Forms.GroupBox();
			this.m_group.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_edit_adb_fullpath
			// 
			this.m_edit_adb_fullpath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_adb_fullpath.Location = new System.Drawing.Point(63, 12);
			this.m_edit_adb_fullpath.Name = "m_edit_adb_fullpath";
			this.m_edit_adb_fullpath.Size = new System.Drawing.Size(300, 20);
			this.m_edit_adb_fullpath.TabIndex = 0;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(9, 15);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(54, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Adb Path:";
			// 
			// m_btn_browse_adb
			// 
			this.m_btn_browse_adb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_adb.Location = new System.Drawing.Point(369, 10);
			this.m_btn_browse_adb.Name = "m_btn_browse_adb";
			this.m_btn_browse_adb.Size = new System.Drawing.Size(40, 23);
			this.m_btn_browse_adb.TabIndex = 2;
			this.m_btn_browse_adb.Text = "...";
			this.m_btn_browse_adb.UseVisualStyleBackColor = true;
			// 
			// m_listbox_devices
			// 
			this.m_listbox_devices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.m_listbox_devices.FormattingEnabled = true;
			this.m_listbox_devices.Location = new System.Drawing.Point(9, 32);
			this.m_listbox_devices.Name = "m_listbox_devices";
			this.m_listbox_devices.Size = new System.Drawing.Size(125, 173);
			this.m_listbox_devices.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 16);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(49, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Devices:";
			// 
			// textBox2
			// 
			this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox2.Location = new System.Drawing.Point(12, 280);
			this.textBox2.Multiline = true;
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(397, 52);
			this.textBox2.TabIndex = 5;
			// 
			// m_lbl_logcat_command
			// 
			this.m_lbl_logcat_command.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_logcat_command.AutoSize = true;
			this.m_lbl_logcat_command.Location = new System.Drawing.Point(12, 264);
			this.m_lbl_logcat_command.Name = "m_lbl_logcat_command";
			this.m_lbl_logcat_command.Size = new System.Drawing.Size(93, 13);
			this.m_lbl_logcat_command.TabIndex = 6;
			this.m_lbl_logcat_command.Text = "Logcat Command:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(253, 338);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 7;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(334, 338);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 8;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_group
			// 
			this.m_group.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_group.Controls.Add(this.m_listbox_devices);
			this.m_group.Controls.Add(this.label2);
			this.m_group.Location = new System.Drawing.Point(12, 39);
			this.m_group.Name = "m_group";
			this.m_group.Size = new System.Drawing.Size(396, 222);
			this.m_group.TabIndex = 9;
			this.m_group.TabStop = false;
			// 
			// AndroidLogcatUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(421, 373);
			this.Controls.Add(this.m_group);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_logcat_command);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(this.m_btn_browse_adb);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_edit_adb_fullpath);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AndroidLogcatUI";
			this.Text = "Android Logcat";
			this.m_group.ResumeLayout(false);
			this.m_group.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_edit_adb_fullpath;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btn_browse_adb;
		private System.Windows.Forms.ListBox m_listbox_devices;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.Label m_lbl_logcat_command;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.GroupBox m_group;
	}
}