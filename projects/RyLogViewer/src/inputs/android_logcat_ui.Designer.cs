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
			this.m_listbox_devices = new ListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_edit_adb_command = new System.Windows.Forms.TextBox();
			this.m_lbl_logcat_command = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_group = new System.Windows.Forms.GroupBox();
			this.m_btn_connect = new System.Windows.Forms.Button();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_combo_output_file = new ComboBox();
			this.m_lbl_log_format = new System.Windows.Forms.Label();
			this.m_combo_log_format = new ComboBox();
			this.m_lbl_filterspec = new System.Windows.Forms.Label();
			this.m_grid_filterspec = new DataGridView();
			this.m_listbox_log_buffers = new ListBox();
			this.m_lbl_log_buffers = new System.Windows.Forms.Label();
			this.m_btn_browse_output_file = new System.Windows.Forms.Button();
			this.m_check_capture_to_log = new System.Windows.Forms.CheckBox();
			this.m_btn_refresh = new System.Windows.Forms.Button();
			this.m_text_adb_status = new System.Windows.Forms.Label();
			this.m_btn_resetadb = new System.Windows.Forms.Button();
			this.m_group.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filterspec)).BeginInit();
			this.SuspendLayout();
			// 
			// m_edit_adb_fullpath
			// 
			this.m_edit_adb_fullpath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_adb_fullpath.Location = new System.Drawing.Point(63, 12);
			this.m_edit_adb_fullpath.Name = "m_edit_adb_fullpath";
			this.m_edit_adb_fullpath.Size = new System.Drawing.Size(289, 20);
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
			this.m_btn_browse_adb.Location = new System.Drawing.Point(358, 10);
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
			this.m_listbox_devices.IntegralHeight = false;
			this.m_listbox_devices.Location = new System.Drawing.Point(9, 29);
			this.m_listbox_devices.Name = "m_listbox_devices";
			this.m_listbox_devices.Size = new System.Drawing.Size(125, 71);
			this.m_listbox_devices.TabIndex = 3;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 13);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(104, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Connected Devices:";
			// 
			// m_edit_adb_command
			// 
			this.m_edit_adb_command.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_adb_command.Location = new System.Drawing.Point(12, 241);
			this.m_edit_adb_command.Multiline = true;
			this.m_edit_adb_command.Name = "m_edit_adb_command";
			this.m_edit_adb_command.Size = new System.Drawing.Size(386, 34);
			this.m_edit_adb_command.TabIndex = 5;
			// 
			// m_lbl_logcat_command
			// 
			this.m_lbl_logcat_command.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_logcat_command.AutoSize = true;
			this.m_lbl_logcat_command.Location = new System.Drawing.Point(9, 227);
			this.m_lbl_logcat_command.Name = "m_lbl_logcat_command";
			this.m_lbl_logcat_command.Size = new System.Drawing.Size(79, 13);
			this.m_lbl_logcat_command.TabIndex = 6;
			this.m_lbl_logcat_command.Text = "Adb Command:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(242, 281);
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
			this.m_btn_cancel.Location = new System.Drawing.Point(323, 281);
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
			this.m_group.Controls.Add(this.m_btn_connect);
			this.m_group.Controls.Add(this.m_check_append);
			this.m_group.Controls.Add(this.m_combo_output_file);
			this.m_group.Controls.Add(this.m_lbl_log_format);
			this.m_group.Controls.Add(this.m_combo_log_format);
			this.m_group.Controls.Add(this.m_lbl_filterspec);
			this.m_group.Controls.Add(this.m_grid_filterspec);
			this.m_group.Controls.Add(this.m_listbox_log_buffers);
			this.m_group.Controls.Add(this.m_lbl_log_buffers);
			this.m_group.Controls.Add(this.m_btn_browse_output_file);
			this.m_group.Controls.Add(this.m_check_capture_to_log);
			this.m_group.Controls.Add(this.m_listbox_devices);
			this.m_group.Controls.Add(this.label2);
			this.m_group.Location = new System.Drawing.Point(12, 51);
			this.m_group.Name = "m_group";
			this.m_group.Size = new System.Drawing.Size(385, 173);
			this.m_group.TabIndex = 9;
			this.m_group.TabStop = false;
			// 
			// m_btn_connect
			// 
			this.m_btn_connect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_connect.Location = new System.Drawing.Point(9, 104);
			this.m_btn_connect.Name = "m_btn_connect";
			this.m_btn_connect.Size = new System.Drawing.Size(125, 23);
			this.m_btn_connect.TabIndex = 22;
			this.m_btn_connect.Text = "Connect...";
			this.m_btn_connect.UseVisualStyleBackColor = true;
			// 
			// m_check_append
			// 
			this.m_check_append.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(304, 129);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(79, 17);
			this.m_check_append.TabIndex = 21;
			this.m_check_append.Text = "Append file";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_combo_output_file
			// 
			this.m_combo_output_file.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_output_file.FormattingEnabled = true;
			this.m_combo_output_file.Location = new System.Drawing.Point(27, 148);
			this.m_combo_output_file.Name = "m_combo_output_file";
			this.m_combo_output_file.Size = new System.Drawing.Size(306, 21);
			this.m_combo_output_file.TabIndex = 20;
			// 
			// m_lbl_log_format
			// 
			this.m_lbl_log_format.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_log_format.AutoSize = true;
			this.m_lbl_log_format.Location = new System.Drawing.Point(210, 109);
			this.m_lbl_log_format.Name = "m_lbl_log_format";
			this.m_lbl_log_format.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_log_format.TabIndex = 19;
			this.m_lbl_log_format.Text = "Log Format:";
			// 
			// m_combo_log_format
			// 
			this.m_combo_log_format.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_log_format.FormattingEnabled = true;
			this.m_combo_log_format.Location = new System.Drawing.Point(279, 106);
			this.m_combo_log_format.Name = "m_combo_log_format";
			this.m_combo_log_format.Size = new System.Drawing.Size(100, 21);
			this.m_combo_log_format.TabIndex = 18;
			// 
			// m_lbl_filterspec
			// 
			this.m_lbl_filterspec.AutoSize = true;
			this.m_lbl_filterspec.Location = new System.Drawing.Point(207, 13);
			this.m_lbl_filterspec.Name = "m_lbl_filterspec";
			this.m_lbl_filterspec.Size = new System.Drawing.Size(65, 13);
			this.m_lbl_filterspec.TabIndex = 17;
			this.m_lbl_filterspec.Text = "Filter Specs:";
			// 
			// m_grid_filterspec
			// 
			this.m_grid_filterspec.AllowUserToResizeRows = false;
			this.m_grid_filterspec.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid_filterspec.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_filterspec.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_filterspec.Location = new System.Drawing.Point(210, 29);
			this.m_grid_filterspec.Name = "m_grid_filterspec";
			this.m_grid_filterspec.RowHeadersWidth = 24;
			this.m_grid_filterspec.Size = new System.Drawing.Size(169, 71);
			this.m_grid_filterspec.TabIndex = 16;
			// 
			// m_listbox_log_buffers
			// 
			this.m_listbox_log_buffers.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.m_listbox_log_buffers.FormattingEnabled = true;
			this.m_listbox_log_buffers.IntegralHeight = false;
			this.m_listbox_log_buffers.Location = new System.Drawing.Point(140, 29);
			this.m_listbox_log_buffers.Name = "m_listbox_log_buffers";
			this.m_listbox_log_buffers.SelectionMode = System.Windows.Forms.SelectionMode.MultiSimple;
			this.m_listbox_log_buffers.Size = new System.Drawing.Size(61, 71);
			this.m_listbox_log_buffers.TabIndex = 15;
			// 
			// m_lbl_log_buffers
			// 
			this.m_lbl_log_buffers.AutoSize = true;
			this.m_lbl_log_buffers.Location = new System.Drawing.Point(137, 13);
			this.m_lbl_log_buffers.Name = "m_lbl_log_buffers";
			this.m_lbl_log_buffers.Size = new System.Drawing.Size(64, 13);
			this.m_lbl_log_buffers.TabIndex = 14;
			this.m_lbl_log_buffers.Text = "Log Buffers:";
			// 
			// m_btn_browse_output_file
			// 
			this.m_btn_browse_output_file.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output_file.Location = new System.Drawing.Point(339, 146);
			this.m_btn_browse_output_file.Name = "m_btn_browse_output_file";
			this.m_btn_browse_output_file.Size = new System.Drawing.Size(40, 23);
			this.m_btn_browse_output_file.TabIndex = 13;
			this.m_btn_browse_output_file.Text = "...";
			this.m_btn_browse_output_file.UseVisualStyleBackColor = true;
			// 
			// m_check_capture_to_log
			// 
			this.m_check_capture_to_log.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_check_capture_to_log.AutoSize = true;
			this.m_check_capture_to_log.Location = new System.Drawing.Point(9, 133);
			this.m_check_capture_to_log.Name = "m_check_capture_to_log";
			this.m_check_capture_to_log.Size = new System.Drawing.Size(156, 17);
			this.m_check_capture_to_log.TabIndex = 5;
			this.m_check_capture_to_log.Text = "Capture logcat output to file";
			this.m_check_capture_to_log.UseVisualStyleBackColor = true;
			// 
			// m_btn_refresh
			// 
			this.m_btn_refresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_refresh.Location = new System.Drawing.Point(15, 281);
			this.m_btn_refresh.Name = "m_btn_refresh";
			this.m_btn_refresh.Size = new System.Drawing.Size(75, 23);
			this.m_btn_refresh.TabIndex = 10;
			this.m_btn_refresh.Text = "Refresh";
			this.m_btn_refresh.UseVisualStyleBackColor = true;
			// 
			// m_text_adb_status
			// 
			this.m_text_adb_status.AutoSize = true;
			this.m_text_adb_status.ForeColor = System.Drawing.SystemColors.ControlDark;
			this.m_text_adb_status.Location = new System.Drawing.Point(61, 35);
			this.m_text_adb_status.Name = "m_text_adb_status";
			this.m_text_adb_status.Size = new System.Drawing.Size(175, 13);
			this.m_text_adb_status.TabIndex = 12;
			this.m_text_adb_status.Text = "Android Debug Bridge version 0.0.0";
			this.m_text_adb_status.Visible = false;
			// 
			// m_btn_resetadb
			// 
			this.m_btn_resetadb.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_resetadb.Location = new System.Drawing.Point(96, 281);
			this.m_btn_resetadb.Name = "m_btn_resetadb";
			this.m_btn_resetadb.Size = new System.Drawing.Size(75, 23);
			this.m_btn_resetadb.TabIndex = 13;
			this.m_btn_resetadb.Text = "Reset Adb";
			this.m_btn_resetadb.UseVisualStyleBackColor = true;
			// 
			// AndroidLogcatUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(410, 316);
			this.Controls.Add(this.m_btn_resetadb);
			this.Controls.Add(this.m_btn_refresh);
			this.Controls.Add(this.m_group);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_adb_command);
			this.Controls.Add(this.m_btn_browse_adb);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_edit_adb_fullpath);
			this.Controls.Add(this.m_text_adb_status);
			this.Controls.Add(this.m_lbl_logcat_command);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(357, 346);
			this.Name = "AndroidLogcatUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Android Logcat";
			this.m_group.ResumeLayout(false);
			this.m_group.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_filterspec)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_edit_adb_fullpath;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btn_browse_adb;
		private ListBox m_listbox_devices;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox m_edit_adb_command;
		private System.Windows.Forms.Label m_lbl_logcat_command;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.GroupBox m_group;
		private System.Windows.Forms.Button m_btn_refresh;
		private System.Windows.Forms.Label m_text_adb_status;
		private System.Windows.Forms.CheckBox m_check_capture_to_log;
		private System.Windows.Forms.Button m_btn_browse_output_file;
		private System.Windows.Forms.Label m_lbl_log_buffers;
		private ListBox m_listbox_log_buffers;
		private System.Windows.Forms.Label m_lbl_filterspec;
		private DataGridView m_grid_filterspec;
		private System.Windows.Forms.Label m_lbl_log_format;
		private ComboBox m_combo_log_format;
		private ComboBox m_combo_output_file;
		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.Button m_btn_connect;
		private System.Windows.Forms.Button m_btn_resetadb;
	}
}