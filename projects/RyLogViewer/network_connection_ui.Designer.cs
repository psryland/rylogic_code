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
			this.m_check_use_proxy = new System.Windows.Forms.CheckBox();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_lbl_proxy_hostname = new System.Windows.Forms.Label();
			this.m_edit_proxy_hostname = new System.Windows.Forms.TextBox();
			this.m_lbl_port = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_edit_output_file = new System.Windows.Forms.TextBox();
			this.m_lbl_hostname = new System.Windows.Forms.Label();
			this.m_combo_hostname = new System.Windows.Forms.ComboBox();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_proxy_port = new System.Windows.Forms.Label();
			this.m_lbl_protocol_type = new System.Windows.Forms.Label();
			this.m_combo_protocol_type = new System.Windows.Forms.ComboBox();
			this.m_spinner_port = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_proxy_port = new System.Windows.Forms.NumericUpDown();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_port)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_proxy_port)).BeginInit();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_check_use_proxy
			// 
			this.m_check_use_proxy.AutoSize = true;
			this.m_check_use_proxy.Location = new System.Drawing.Point(15, 19);
			this.m_check_use_proxy.Name = "m_check_use_proxy";
			this.m_check_use_proxy.Size = new System.Drawing.Size(74, 17);
			this.m_check_use_proxy.TabIndex = 4;
			this.m_check_use_proxy.Text = "Use Proxy";
			this.m_check_use_proxy.UseVisualStyleBackColor = true;
			this.m_check_use_proxy.Visible = false;
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(28, 130);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 10;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_lbl_proxy_hostname
			// 
			this.m_lbl_proxy_hostname.AutoSize = true;
			this.m_lbl_proxy_hostname.Location = new System.Drawing.Point(18, 40);
			this.m_lbl_proxy_hostname.Name = "m_lbl_proxy_hostname";
			this.m_lbl_proxy_hostname.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_proxy_hostname.TabIndex = 3;
			this.m_lbl_proxy_hostname.Text = "Proxy Hostname:";
			this.m_lbl_proxy_hostname.Visible = false;
			// 
			// m_edit_proxy_hostname
			// 
			this.m_edit_proxy_hostname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_proxy_hostname.Location = new System.Drawing.Point(38, 56);
			this.m_edit_proxy_hostname.Name = "m_edit_proxy_hostname";
			this.m_edit_proxy_hostname.Size = new System.Drawing.Size(0, 20);
			this.m_edit_proxy_hostname.TabIndex = 4;
			this.m_edit_proxy_hostname.Visible = false;
			// 
			// m_lbl_port
			// 
			this.m_lbl_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_port.AutoSize = true;
			this.m_lbl_port.Location = new System.Drawing.Point(205, 16);
			this.m_lbl_port.Name = "m_lbl_port";
			this.m_lbl_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_port.TabIndex = 32;
			this.m_lbl_port.Text = "Port:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(226, 103);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 7;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(8, 89);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 31;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_edit_output_file
			// 
			this.m_edit_output_file.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_output_file.Location = new System.Drawing.Point(28, 105);
			this.m_edit_output_file.Name = "m_edit_output_file";
			this.m_edit_output_file.Size = new System.Drawing.Size(192, 20);
			this.m_edit_output_file.TabIndex = 6;
			// 
			// m_lbl_hostname
			// 
			this.m_lbl_hostname.AutoSize = true;
			this.m_lbl_hostname.Location = new System.Drawing.Point(8, 16);
			this.m_lbl_hostname.Name = "m_lbl_hostname";
			this.m_lbl_hostname.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_hostname.TabIndex = 30;
			this.m_lbl_hostname.Text = "Hostname:";
			// 
			// m_combo_hostname
			// 
			this.m_combo_hostname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_hostname.FormattingEnabled = true;
			this.m_combo_hostname.Location = new System.Drawing.Point(28, 32);
			this.m_combo_hostname.Name = "m_combo_hostname";
			this.m_combo_hostname.Size = new System.Drawing.Size(184, 21);
			this.m_combo_hostname.TabIndex = 0;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(185, 154);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 12;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(104, 154);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 11;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_proxy_port
			// 
			this.m_lbl_proxy_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_lbl_proxy_port.AutoSize = true;
			this.m_lbl_proxy_port.Location = new System.Drawing.Point(-28, 40);
			this.m_lbl_proxy_port.Name = "m_lbl_proxy_port";
			this.m_lbl_proxy_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_proxy_port.TabIndex = 38;
			this.m_lbl_proxy_port.Text = "Port:";
			this.m_lbl_proxy_port.Visible = false;
			// 
			// m_lbl_protocol_type
			// 
			this.m_lbl_protocol_type.AutoSize = true;
			this.m_lbl_protocol_type.Location = new System.Drawing.Point(25, 59);
			this.m_lbl_protocol_type.Name = "m_lbl_protocol_type";
			this.m_lbl_protocol_type.Size = new System.Drawing.Size(76, 13);
			this.m_lbl_protocol_type.TabIndex = 40;
			this.m_lbl_protocol_type.Text = "Protocol Type:";
			// 
			// m_combo_protocol_type
			// 
			this.m_combo_protocol_type.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_protocol_type.FormattingEnabled = true;
			this.m_combo_protocol_type.Location = new System.Drawing.Point(107, 56);
			this.m_combo_protocol_type.Name = "m_combo_protocol_type";
			this.m_combo_protocol_type.Size = new System.Drawing.Size(105, 21);
			this.m_combo_protocol_type.TabIndex = 2;
			// 
			// m_spinner_port
			// 
			this.m_spinner_port.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_spinner_port.Location = new System.Drawing.Point(218, 32);
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
			this.m_spinner_proxy_port.Location = new System.Drawing.Point(-15, 56);
			this.m_spinner_proxy_port.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.m_spinner_proxy_port.Name = "m_spinner_proxy_port";
			this.m_spinner_proxy_port.Size = new System.Drawing.Size(51, 20);
			this.m_spinner_proxy_port.TabIndex = 5;
			this.m_spinner_proxy_port.Value = new decimal(new int[] {
            65535,
            0,
            0,
            0});
			this.m_spinner_proxy_port.Visible = false;
			// 
			// groupBox1
			// 
			this.groupBox1.Controls.Add(this.m_check_use_proxy);
			this.groupBox1.Controls.Add(this.m_spinner_proxy_port);
			this.groupBox1.Controls.Add(this.m_lbl_proxy_port);
			this.groupBox1.Controls.Add(this.m_edit_proxy_hostname);
			this.groupBox1.Controls.Add(this.m_lbl_proxy_hostname);
			this.groupBox1.Location = new System.Drawing.Point(104, 4);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(74, 22);
			this.groupBox1.TabIndex = 41;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Proxy UI";
			this.groupBox1.Visible = false;
			// 
			// NetworkConnectionUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(276, 190);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.m_spinner_port);
			this.Controls.Add(this.m_lbl_hostname);
			this.Controls.Add(this.m_combo_hostname);
			this.Controls.Add(this.m_lbl_port);
			this.Controls.Add(this.m_lbl_protocol_type);
			this.Controls.Add(this.m_combo_protocol_type);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_edit_output_file);
			this.Controls.Add(this.m_btn_browse_output);
			this.Controls.Add(this.m_check_append);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(236, 228);
			this.Name = "NetworkConnectionUI";
			this.Text = "Log Network Connection";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_port)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_proxy_port)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox m_check_use_proxy;
		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.Label m_lbl_proxy_hostname;
		private System.Windows.Forms.TextBox m_edit_proxy_hostname;
		private System.Windows.Forms.Label m_lbl_port;
		private System.Windows.Forms.Button m_btn_browse_output;
		private System.Windows.Forms.Label m_lbl_output_file;
		private System.Windows.Forms.TextBox m_edit_output_file;
		private System.Windows.Forms.Label m_lbl_hostname;
		private System.Windows.Forms.ComboBox m_combo_hostname;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Label m_lbl_proxy_port;
		private System.Windows.Forms.Label m_lbl_protocol_type;
		private System.Windows.Forms.ComboBox m_combo_protocol_type;
		private System.Windows.Forms.NumericUpDown m_spinner_port;
		private System.Windows.Forms.NumericUpDown m_spinner_proxy_port;
		private System.Windows.Forms.GroupBox groupBox1;
	}
}