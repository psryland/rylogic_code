namespace RyLogViewer
{
	partial class NetworkConnectionUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkConnectionUI));
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_lbl_proxy_hostname = new System.Windows.Forms.Label();
			this.m_edit_proxy_hostname = new System.Windows.Forms.TextBox();
			this.m_lbl_port = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_lbl_hostname = new System.Windows.Forms.Label();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_proxy_port = new System.Windows.Forms.Label();
			this.m_lbl_protocol_type = new System.Windows.Forms.Label();
			this.m_spinner_port = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_proxy_port = new System.Windows.Forms.NumericUpDown();
			this.m_lbl_proxy_type = new System.Windows.Forms.Label();
			this.m_edit_proxy_username = new System.Windows.Forms.TextBox();
			this.m_edit_proxy_password = new System.Windows.Forms.TextBox();
			this.m_lbl_proxy_username = new System.Windows.Forms.Label();
			this.m_lbl_proxy_password = new System.Windows.Forms.Label();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.panel7 = new System.Windows.Forms.Panel();
			this.m_combo_output_filepath = new RyLogViewer.ComboBox();
			this.panel6 = new System.Windows.Forms.Panel();
			this.panel5 = new System.Windows.Forms.Panel();
			this.panel4 = new System.Windows.Forms.Panel();
			this.panel3 = new System.Windows.Forms.Panel();
			this.m_combo_proxy_type = new RyLogViewer.ComboBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_check_listener = new System.Windows.Forms.CheckBox();
			this.m_combo_protocol_type = new RyLogViewer.ComboBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_combo_hostname = new RyLogViewer.ComboBox();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_port)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_proxy_port)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.panel7.SuspendLayout();
			this.panel6.SuspendLayout();
			this.panel5.SuspendLayout();
			this.panel4.SuspendLayout();
			this.panel3.SuspendLayout();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(23, 44);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 3;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_lbl_proxy_hostname
			// 
			this.m_lbl_proxy_hostname.AutoSize = true;
			this.m_lbl_proxy_hostname.Location = new System.Drawing.Point(19, 2);
			this.m_lbl_proxy_hostname.Name = "m_lbl_proxy_hostname";
			this.m_lbl_proxy_hostname.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_proxy_hostname.TabIndex = 3;
			this.m_lbl_proxy_hostname.Text = "Proxy Hostname:";
			// 
			// m_edit_proxy_hostname
			// 
			this.m_edit_proxy_hostname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_proxy_hostname.Location = new System.Drawing.Point(33, 18);
			this.m_edit_proxy_hostname.Name = "m_edit_proxy_hostname";
			this.m_edit_proxy_hostname.Size = new System.Drawing.Size(145, 20);
			this.m_edit_proxy_hostname.TabIndex = 0;
			// 
			// m_lbl_port
			// 
			this.m_lbl_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_port.AutoSize = true;
			this.m_lbl_port.Location = new System.Drawing.Point(169, 3);
			this.m_lbl_port.Name = "m_lbl_port";
			this.m_lbl_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_port.TabIndex = 32;
			this.m_lbl_port.Text = "Port:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(202, 17);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 2;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(3, 3);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 0;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_lbl_hostname
			// 
			this.m_lbl_hostname.AutoSize = true;
			this.m_lbl_hostname.Location = new System.Drawing.Point(3, 3);
			this.m_lbl_hostname.Name = "m_lbl_hostname";
			this.m_lbl_hostname.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_hostname.TabIndex = 30;
			this.m_lbl_hostname.Text = "Hostname:";
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(174, 301);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 1;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(93, 301);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 0;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_proxy_port
			// 
			this.m_lbl_proxy_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_proxy_port.AutoSize = true;
			this.m_lbl_proxy_port.Location = new System.Drawing.Point(171, 2);
			this.m_lbl_proxy_port.Name = "m_lbl_proxy_port";
			this.m_lbl_proxy_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_proxy_port.TabIndex = 38;
			this.m_lbl_proxy_port.Text = "Port:";
			// 
			// m_lbl_protocol_type
			// 
			this.m_lbl_protocol_type.AutoSize = true;
			this.m_lbl_protocol_type.Location = new System.Drawing.Point(3, 6);
			this.m_lbl_protocol_type.Name = "m_lbl_protocol_type";
			this.m_lbl_protocol_type.Size = new System.Drawing.Size(76, 13);
			this.m_lbl_protocol_type.TabIndex = 40;
			this.m_lbl_protocol_type.Text = "Protocol Type:";
			// 
			// m_spinner_port
			// 
			this.m_spinner_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_port.Location = new System.Drawing.Point(185, 19);
			this.m_spinner_port.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.m_spinner_port.Name = "m_spinner_port";
			this.m_spinner_port.Size = new System.Drawing.Size(51, 20);
			this.m_spinner_port.TabIndex = 1;
			this.m_spinner_port.Value = new decimal(new int[] {
            5555,
            0,
            0,
            0});
			// 
			// m_spinner_proxy_port
			// 
			this.m_spinner_proxy_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_proxy_port.Location = new System.Drawing.Point(184, 18);
			this.m_spinner_proxy_port.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.m_spinner_proxy_port.Name = "m_spinner_proxy_port";
			this.m_spinner_proxy_port.Size = new System.Drawing.Size(51, 20);
			this.m_spinner_proxy_port.TabIndex = 1;
			this.m_spinner_proxy_port.Value = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			// 
			// m_lbl_proxy_type
			// 
			this.m_lbl_proxy_type.AutoSize = true;
			this.m_lbl_proxy_type.Location = new System.Drawing.Point(3, 6);
			this.m_lbl_proxy_type.Name = "m_lbl_proxy_type";
			this.m_lbl_proxy_type.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_proxy_type.TabIndex = 41;
			this.m_lbl_proxy_type.Text = "Proxy Type:";
			// 
			// m_edit_proxy_username
			// 
			this.m_edit_proxy_username.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_proxy_username.Location = new System.Drawing.Point(33, 19);
			this.m_edit_proxy_username.Name = "m_edit_proxy_username";
			this.m_edit_proxy_username.Size = new System.Drawing.Size(80, 20);
			this.m_edit_proxy_username.TabIndex = 0;
			// 
			// m_edit_proxy_password
			// 
			this.m_edit_proxy_password.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_proxy_password.Location = new System.Drawing.Point(18, 19);
			this.m_edit_proxy_password.Name = "m_edit_proxy_password";
			this.m_edit_proxy_password.Size = new System.Drawing.Size(96, 20);
			this.m_edit_proxy_password.TabIndex = 0;
			this.m_edit_proxy_password.UseSystemPasswordChar = true;
			// 
			// m_lbl_proxy_username
			// 
			this.m_lbl_proxy_username.AutoSize = true;
			this.m_lbl_proxy_username.Location = new System.Drawing.Point(17, 3);
			this.m_lbl_proxy_username.Name = "m_lbl_proxy_username";
			this.m_lbl_proxy_username.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_proxy_username.TabIndex = 45;
			this.m_lbl_proxy_username.Text = "Proxy Username:";
			// 
			// m_lbl_proxy_password
			// 
			this.m_lbl_proxy_password.AutoSize = true;
			this.m_lbl_proxy_password.Location = new System.Drawing.Point(2, 3);
			this.m_lbl_proxy_password.Name = "m_lbl_proxy_password";
			this.m_lbl_proxy_password.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_proxy_password.TabIndex = 46;
			this.m_lbl_proxy_password.Text = "Proxy Password:";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel1.Controls.Add(this.panel7, 0, 5);
			this.tableLayoutPanel1.Controls.Add(this.panel6, 1, 4);
			this.tableLayoutPanel1.Controls.Add(this.panel5, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.panel4, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.panel3, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.panel2, 0, 1);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 7);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(245, 288);
			this.tableLayoutPanel1.TabIndex = 47;
			// 
			// panel7
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel7, 2);
			this.panel7.Controls.Add(this.m_lbl_output_file);
			this.panel7.Controls.Add(this.m_check_append);
			this.panel7.Controls.Add(this.m_combo_output_filepath);
			this.panel7.Controls.Add(this.m_btn_browse_output);
			this.panel7.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel7.Location = new System.Drawing.Point(3, 215);
			this.panel7.Name = "panel7";
			this.panel7.Size = new System.Drawing.Size(239, 70);
			this.panel7.TabIndex = 48;
			// 
			// m_combo_output_filepath
			// 
			this.m_combo_output_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_output_filepath.FormattingEnabled = true;
			this.m_combo_output_filepath.Location = new System.Drawing.Point(23, 18);
			this.m_combo_output_filepath.Name = "m_combo_output_filepath";
			this.m_combo_output_filepath.Size = new System.Drawing.Size(173, 21);
			this.m_combo_output_filepath.TabIndex = 1;
			// 
			// panel6
			// 
			this.panel6.Controls.Add(this.m_lbl_proxy_password);
			this.panel6.Controls.Add(this.m_edit_proxy_password);
			this.panel6.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel6.Location = new System.Drawing.Point(125, 166);
			this.panel6.Name = "panel6";
			this.panel6.Size = new System.Drawing.Size(117, 43);
			this.panel6.TabIndex = 48;
			// 
			// panel5
			// 
			this.panel5.Controls.Add(this.m_lbl_proxy_username);
			this.panel5.Controls.Add(this.m_edit_proxy_username);
			this.panel5.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel5.Location = new System.Drawing.Point(3, 166);
			this.panel5.Name = "panel5";
			this.panel5.Size = new System.Drawing.Size(116, 43);
			this.panel5.TabIndex = 48;
			// 
			// panel4
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel4, 2);
			this.panel4.Controls.Add(this.m_lbl_proxy_hostname);
			this.panel4.Controls.Add(this.m_edit_proxy_hostname);
			this.panel4.Controls.Add(this.m_lbl_proxy_port);
			this.panel4.Controls.Add(this.m_spinner_proxy_port);
			this.panel4.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel4.Location = new System.Drawing.Point(3, 118);
			this.panel4.Name = "panel4";
			this.panel4.Size = new System.Drawing.Size(239, 42);
			this.panel4.TabIndex = 48;
			// 
			// panel3
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel3, 2);
			this.panel3.Controls.Add(this.m_combo_proxy_type);
			this.panel3.Controls.Add(this.m_lbl_proxy_type);
			this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel3.Location = new System.Drawing.Point(3, 85);
			this.panel3.Name = "panel3";
			this.panel3.Size = new System.Drawing.Size(239, 27);
			this.panel3.TabIndex = 48;
			// 
			// m_combo_proxy_type
			// 
			this.m_combo_proxy_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_proxy_type.FormattingEnabled = true;
			this.m_combo_proxy_type.Location = new System.Drawing.Point(72, 3);
			this.m_combo_proxy_type.Name = "m_combo_proxy_type";
			this.m_combo_proxy_type.Size = new System.Drawing.Size(105, 21);
			this.m_combo_proxy_type.TabIndex = 0;
			// 
			// panel1
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel1, 2);
			this.panel1.Controls.Add(this.m_check_listener);
			this.panel1.Controls.Add(this.m_combo_protocol_type);
			this.panel1.Controls.Add(this.m_lbl_protocol_type);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 3);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(239, 27);
			this.panel1.TabIndex = 48;
			// 
			// m_check_listener
			// 
			this.m_check_listener.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_check_listener.AutoSize = true;
			this.m_check_listener.Location = new System.Drawing.Point(182, 5);
			this.m_check_listener.Name = "m_check_listener";
			this.m_check_listener.Size = new System.Drawing.Size(54, 17);
			this.m_check_listener.TabIndex = 1;
			this.m_check_listener.Text = "Listen";
			this.m_check_listener.UseVisualStyleBackColor = true;
			// 
			// m_combo_protocol_type
			// 
			this.m_combo_protocol_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_protocol_type.FormattingEnabled = true;
			this.m_combo_protocol_type.Location = new System.Drawing.Point(84, 3);
			this.m_combo_protocol_type.Name = "m_combo_protocol_type";
			this.m_combo_protocol_type.Size = new System.Drawing.Size(93, 21);
			this.m_combo_protocol_type.TabIndex = 0;
			// 
			// panel2
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.panel2, 2);
			this.panel2.Controls.Add(this.m_lbl_hostname);
			this.panel2.Controls.Add(this.m_lbl_port);
			this.panel2.Controls.Add(this.m_combo_hostname);
			this.panel2.Controls.Add(this.m_spinner_port);
			this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel2.Location = new System.Drawing.Point(3, 36);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(239, 43);
			this.panel2.TabIndex = 48;
			// 
			// m_combo_hostname
			// 
			this.m_combo_hostname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_hostname.FormattingEnabled = true;
			this.m_combo_hostname.Location = new System.Drawing.Point(23, 19);
			this.m_combo_hostname.Name = "m_combo_hostname";
			this.m_combo_hostname.Size = new System.Drawing.Size(156, 21);
			this.m_combo_hostname.TabIndex = 0;
			// 
			// NetworkConnectionUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(258, 335);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(2000, 374);
			this.MinimumSize = new System.Drawing.Size(274, 374);
			this.Name = "NetworkConnectionUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Log Network Connection";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_port)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_proxy_port)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.panel7.ResumeLayout(false);
			this.panel7.PerformLayout();
			this.panel6.ResumeLayout(false);
			this.panel6.PerformLayout();
			this.panel5.ResumeLayout(false);
			this.panel5.PerformLayout();
			this.panel4.ResumeLayout(false);
			this.panel4.PerformLayout();
			this.panel3.ResumeLayout(false);
			this.panel3.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.Label m_lbl_proxy_hostname;
		private System.Windows.Forms.TextBox m_edit_proxy_hostname;
		private System.Windows.Forms.Label m_lbl_port;
		private System.Windows.Forms.Button m_btn_browse_output;
		private System.Windows.Forms.Label m_lbl_output_file;
		private System.Windows.Forms.Label m_lbl_hostname;
		private ComboBox m_combo_hostname;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Label m_lbl_proxy_port;
		private System.Windows.Forms.Label m_lbl_protocol_type;
		private ComboBox m_combo_protocol_type;
		private System.Windows.Forms.NumericUpDown m_spinner_port;
		private System.Windows.Forms.NumericUpDown m_spinner_proxy_port;
		private ComboBox m_combo_output_filepath;
		private System.Windows.Forms.Label m_lbl_proxy_type;
		private ComboBox m_combo_proxy_type;
		private System.Windows.Forms.TextBox m_edit_proxy_username;
		private System.Windows.Forms.TextBox m_edit_proxy_password;
		private System.Windows.Forms.Label m_lbl_proxy_username;
		private System.Windows.Forms.Label m_lbl_proxy_password;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Panel panel7;
		private System.Windows.Forms.Panel panel6;
		private System.Windows.Forms.Panel panel5;
		private System.Windows.Forms.Panel panel4;
		private System.Windows.Forms.Panel panel3;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.CheckBox m_check_listener;
	}
}