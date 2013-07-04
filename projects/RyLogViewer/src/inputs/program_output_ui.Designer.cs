namespace RyLogViewer
{
	partial class ProgramOutputUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProgramOutputUI));
			this.m_btn_browse_exec = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_check_capture_stdout = new System.Windows.Forms.CheckBox();
			this.m_check_capture_stderr = new System.Windows.Forms.CheckBox();
			this.m_combo_launch_cmdline = new ComboBox();
			this.m_lbl_cmdline = new System.Windows.Forms.Label();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_edit_arguments = new System.Windows.Forms.TextBox();
			this.m_lbl_arguments = new System.Windows.Forms.Label();
			this.m_lbl_working_dir = new System.Windows.Forms.Label();
			this.m_edit_working_dir = new System.Windows.Forms.TextBox();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_check_show_window = new System.Windows.Forms.CheckBox();
			this.m_combo_output_filepath = new ComboBox();
			this.SuspendLayout();
			// 
			// m_btn_browse_exec
			// 
			this.m_btn_browse_exec.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_exec.Location = new System.Drawing.Point(234, 33);
			this.m_btn_browse_exec.Name = "m_btn_browse_exec";
			this.m_btn_browse_exec.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_exec.TabIndex = 1;
			this.m_btn_browse_exec.Text = "...";
			this.m_btn_browse_exec.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(112, 251);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 10;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(193, 251);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 11;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_check_capture_stdout
			// 
			this.m_check_capture_stdout.AutoSize = true;
			this.m_check_capture_stdout.Location = new System.Drawing.Point(32, 200);
			this.m_check_capture_stdout.Name = "m_check_capture_stdout";
			this.m_check_capture_stdout.Size = new System.Drawing.Size(121, 17);
			this.m_check_capture_stdout.TabIndex = 6;
			this.m_check_capture_stdout.Text = "Log standard output";
			this.m_check_capture_stdout.UseVisualStyleBackColor = true;
			// 
			// m_check_capture_stderr
			// 
			this.m_check_capture_stderr.AutoSize = true;
			this.m_check_capture_stderr.Location = new System.Drawing.Point(32, 223);
			this.m_check_capture_stderr.Name = "m_check_capture_stderr";
			this.m_check_capture_stderr.Size = new System.Drawing.Size(112, 17);
			this.m_check_capture_stderr.TabIndex = 7;
			this.m_check_capture_stderr.Text = "Log standard error";
			this.m_check_capture_stderr.UseVisualStyleBackColor = true;
			// 
			// m_combo_launch_cmdline
			// 
			this.m_combo_launch_cmdline.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_launch_cmdline.FormattingEnabled = true;
			this.m_combo_launch_cmdline.Location = new System.Drawing.Point(32, 33);
			this.m_combo_launch_cmdline.Name = "m_combo_launch_cmdline";
			this.m_combo_launch_cmdline.Size = new System.Drawing.Size(196, 21);
			this.m_combo_launch_cmdline.TabIndex = 0;
			// 
			// m_lbl_cmdline
			// 
			this.m_lbl_cmdline.AutoSize = true;
			this.m_lbl_cmdline.Location = new System.Drawing.Point(12, 14);
			this.m_lbl_cmdline.Name = "m_lbl_cmdline";
			this.m_lbl_cmdline.Size = new System.Drawing.Size(130, 13);
			this.m_lbl_cmdline.TabIndex = 10;
			this.m_lbl_cmdline.Text = "Application command line:";
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(14, 155);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 12;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(234, 170);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 5;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_edit_arguments
			// 
			this.m_edit_arguments.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_arguments.Location = new System.Drawing.Point(32, 80);
			this.m_edit_arguments.Name = "m_edit_arguments";
			this.m_edit_arguments.Size = new System.Drawing.Size(196, 20);
			this.m_edit_arguments.TabIndex = 2;
			// 
			// m_lbl_arguments
			// 
			this.m_lbl_arguments.AutoSize = true;
			this.m_lbl_arguments.Location = new System.Drawing.Point(12, 61);
			this.m_lbl_arguments.Name = "m_lbl_arguments";
			this.m_lbl_arguments.Size = new System.Drawing.Size(114, 13);
			this.m_lbl_arguments.TabIndex = 15;
			this.m_lbl_arguments.Text = "Application arguments:";
			// 
			// m_lbl_working_dir
			// 
			this.m_lbl_working_dir.AutoSize = true;
			this.m_lbl_working_dir.Location = new System.Drawing.Point(12, 108);
			this.m_lbl_working_dir.Name = "m_lbl_working_dir";
			this.m_lbl_working_dir.Size = new System.Drawing.Size(95, 13);
			this.m_lbl_working_dir.TabIndex = 17;
			this.m_lbl_working_dir.Text = "Working Directory:";
			// 
			// m_edit_working_dir
			// 
			this.m_edit_working_dir.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_working_dir.Location = new System.Drawing.Point(32, 126);
			this.m_edit_working_dir.Name = "m_edit_working_dir";
			this.m_edit_working_dir.Size = new System.Drawing.Size(196, 20);
			this.m_edit_working_dir.TabIndex = 3;
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(159, 200);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 8;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_check_show_window
			// 
			this.m_check_show_window.AutoSize = true;
			this.m_check_show_window.Location = new System.Drawing.Point(159, 223);
			this.m_check_show_window.Name = "m_check_show_window";
			this.m_check_show_window.Size = new System.Drawing.Size(92, 17);
			this.m_check_show_window.TabIndex = 9;
			this.m_check_show_window.Text = "Show window";
			this.m_check_show_window.UseVisualStyleBackColor = true;
			// 
			// m_combo_output_filepath
			// 
			this.m_combo_output_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_output_filepath.FormattingEnabled = true;
			this.m_combo_output_filepath.Location = new System.Drawing.Point(32, 171);
			this.m_combo_output_filepath.Name = "m_combo_output_filepath";
			this.m_combo_output_filepath.Size = new System.Drawing.Size(196, 21);
			this.m_combo_output_filepath.TabIndex = 4;
			// 
			// ProgramOutputUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(280, 286);
			this.Controls.Add(this.m_combo_output_filepath);
			this.Controls.Add(this.m_check_show_window);
			this.Controls.Add(this.m_check_append);
			this.Controls.Add(this.m_lbl_working_dir);
			this.Controls.Add(this.m_edit_working_dir);
			this.Controls.Add(this.m_lbl_arguments);
			this.Controls.Add(this.m_edit_arguments);
			this.Controls.Add(this.m_btn_browse_output);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_lbl_cmdline);
			this.Controls.Add(this.m_combo_launch_cmdline);
			this.Controls.Add(this.m_check_capture_stderr);
			this.Controls.Add(this.m_check_capture_stdout);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_browse_exec);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(2000, 324);
			this.MinimumSize = new System.Drawing.Size(292, 324);
			this.Name = "ProgramOutputUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Log Program Output";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btn_browse_exec;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.CheckBox m_check_capture_stdout;
		private System.Windows.Forms.CheckBox m_check_capture_stderr;
		private ComboBox m_combo_launch_cmdline;
		private System.Windows.Forms.Label m_lbl_cmdline;
		private System.Windows.Forms.Label m_lbl_output_file;
		private System.Windows.Forms.Button m_btn_browse_output;
		private System.Windows.Forms.TextBox m_edit_arguments;
		private System.Windows.Forms.Label m_lbl_arguments;
		private System.Windows.Forms.Label m_lbl_working_dir;
		private System.Windows.Forms.TextBox m_edit_working_dir;
		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.CheckBox m_check_show_window;
		private ComboBox m_combo_output_filepath;
	}
}