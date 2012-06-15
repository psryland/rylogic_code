namespace RyLogViewer
{
	partial class LogProgramOutputUI
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
			this.m_radio_launch = new System.Windows.Forms.RadioButton();
			this.m_radio_attach = new System.Windows.Forms.RadioButton();
			this.m_edit_launch_cmdline = new System.Windows.Forms.TextBox();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.m_combo_processes = new System.Windows.Forms.ComboBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_check_capture_stdout = new System.Windows.Forms.CheckBox();
			this.m_check_capture_stderr = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// m_radio_launch
			// 
			this.m_radio_launch.AutoSize = true;
			this.m_radio_launch.Location = new System.Drawing.Point(12, 12);
			this.m_radio_launch.Name = "m_radio_launch";
			this.m_radio_launch.Size = new System.Drawing.Size(116, 17);
			this.m_radio_launch.TabIndex = 0;
			this.m_radio_launch.TabStop = true;
			this.m_radio_launch.Text = "Launch Application";
			this.m_radio_launch.UseVisualStyleBackColor = true;
			// 
			// m_radio_attach
			// 
			this.m_radio_attach.AutoSize = true;
			this.m_radio_attach.Location = new System.Drawing.Point(12, 63);
			this.m_radio_attach.Name = "m_radio_attach";
			this.m_radio_attach.Size = new System.Drawing.Size(109, 17);
			this.m_radio_attach.TabIndex = 1;
			this.m_radio_attach.TabStop = true;
			this.m_radio_attach.Text = "Attach to Process";
			this.m_radio_attach.UseVisualStyleBackColor = true;
			// 
			// m_edit_launch_cmdline
			// 
			this.m_edit_launch_cmdline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_launch_cmdline.Location = new System.Drawing.Point(32, 35);
			this.m_edit_launch_cmdline.Name = "m_edit_launch_cmdline";
			this.m_edit_launch_cmdline.Size = new System.Drawing.Size(228, 20);
			this.m_edit_launch_cmdline.TabIndex = 2;
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(266, 33);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse.TabIndex = 3;
			this.m_btn_browse.Text = "...";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// m_combo_processes
			// 
			this.m_combo_processes.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_processes.FormattingEnabled = true;
			this.m_combo_processes.Location = new System.Drawing.Point(32, 86);
			this.m_combo_processes.Name = "m_combo_processes";
			this.m_combo_processes.Size = new System.Drawing.Size(228, 21);
			this.m_combo_processes.TabIndex = 4;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(144, 159);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(225, 159);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 6;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_check_capture_stdout
			// 
			this.m_check_capture_stdout.AutoSize = true;
			this.m_check_capture_stdout.Location = new System.Drawing.Point(32, 113);
			this.m_check_capture_stdout.Name = "m_check_capture_stdout";
			this.m_check_capture_stdout.Size = new System.Drawing.Size(121, 17);
			this.m_check_capture_stdout.TabIndex = 7;
			this.m_check_capture_stdout.Text = "Log standard output";
			this.m_check_capture_stdout.UseVisualStyleBackColor = true;
			// 
			// m_check_capture_stderr
			// 
			this.m_check_capture_stderr.AutoSize = true;
			this.m_check_capture_stderr.Location = new System.Drawing.Point(32, 136);
			this.m_check_capture_stderr.Name = "m_check_capture_stderr";
			this.m_check_capture_stderr.Size = new System.Drawing.Size(112, 17);
			this.m_check_capture_stderr.TabIndex = 8;
			this.m_check_capture_stderr.Text = "Log standard error";
			this.m_check_capture_stderr.UseVisualStyleBackColor = true;
			// 
			// LogProgramOutputUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(312, 194);
			this.Controls.Add(this.m_check_capture_stderr);
			this.Controls.Add(this.m_check_capture_stdout);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_combo_processes);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_edit_launch_cmdline);
			this.Controls.Add(this.m_radio_attach);
			this.Controls.Add(this.m_radio_launch);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "LogProgramOutputUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Log Program Output";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton m_radio_launch;
		private System.Windows.Forms.RadioButton m_radio_attach;
		private System.Windows.Forms.TextBox m_edit_launch_cmdline;
		private System.Windows.Forms.Button m_btn_browse;
		private System.Windows.Forms.ComboBox m_combo_processes;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.CheckBox m_check_capture_stdout;
		private System.Windows.Forms.CheckBox m_check_capture_stderr;
	}
}