namespace RyLogViewer
{
	partial class SerialConnectionUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SerialConnectionUI));
			this.m_lbl_commport = new System.Windows.Forms.Label();
			this.m_combo_commport = new System.Windows.Forms.ComboBox();
			this.m_lbl_baudrate = new System.Windows.Forms.Label();
			this.m_lbl_databits = new System.Windows.Forms.Label();
			this.m_combo_databits = new System.Windows.Forms.ComboBox();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_parity = new System.Windows.Forms.Label();
			this.m_combo_parity = new System.Windows.Forms.ComboBox();
			this.m_lbl_stopbits = new System.Windows.Forms.Label();
			this.m_combo_stopbits = new System.Windows.Forms.ComboBox();
			this.m_lbl_flow_control = new System.Windows.Forms.Label();
			this.m_combo_flow_control = new System.Windows.Forms.ComboBox();
			this.m_combo_baudrate = new System.Windows.Forms.ComboBox();
			this.m_check_dtr_enable = new System.Windows.Forms.CheckBox();
			this.m_check_rts_enable = new System.Windows.Forms.CheckBox();
			this.m_combo_output_filepath = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// m_lbl_commport
			// 
			this.m_lbl_commport.AutoSize = true;
			this.m_lbl_commport.Location = new System.Drawing.Point(8, 15);
			this.m_lbl_commport.Name = "m_lbl_commport";
			this.m_lbl_commport.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_commport.TabIndex = 49;
			this.m_lbl_commport.Text = "Comm Port:";
			// 
			// m_combo_commport
			// 
			this.m_combo_commport.FormattingEnabled = true;
			this.m_combo_commport.Location = new System.Drawing.Point(28, 31);
			this.m_combo_commport.Name = "m_combo_commport";
			this.m_combo_commport.Size = new System.Drawing.Size(112, 21);
			this.m_combo_commport.TabIndex = 0;
			// 
			// m_lbl_baudrate
			// 
			this.m_lbl_baudrate.AutoSize = true;
			this.m_lbl_baudrate.Location = new System.Drawing.Point(157, 15);
			this.m_lbl_baudrate.Name = "m_lbl_baudrate";
			this.m_lbl_baudrate.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_baudrate.TabIndex = 51;
			this.m_lbl_baudrate.Text = "Baud Rate:";
			// 
			// m_lbl_databits
			// 
			this.m_lbl_databits.AutoSize = true;
			this.m_lbl_databits.Location = new System.Drawing.Point(8, 63);
			this.m_lbl_databits.Name = "m_lbl_databits";
			this.m_lbl_databits.Size = new System.Drawing.Size(53, 13);
			this.m_lbl_databits.TabIndex = 52;
			this.m_lbl_databits.Text = "Data Bits:";
			// 
			// m_combo_databits
			// 
			this.m_combo_databits.FormattingEnabled = true;
			this.m_combo_databits.Location = new System.Drawing.Point(67, 60);
			this.m_combo_databits.Name = "m_combo_databits";
			this.m_combo_databits.Size = new System.Drawing.Size(74, 21);
			this.m_combo_databits.TabIndex = 2;
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(8, 162);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 50;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(257, 176);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 7;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(28, 203);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 8;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(135, 245);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 9;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(216, 245);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 10;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_parity
			// 
			this.m_lbl_parity.AutoSize = true;
			this.m_lbl_parity.Location = new System.Drawing.Point(25, 91);
			this.m_lbl_parity.Name = "m_lbl_parity";
			this.m_lbl_parity.Size = new System.Drawing.Size(36, 13);
			this.m_lbl_parity.TabIndex = 53;
			this.m_lbl_parity.Text = "Parity:";
			// 
			// m_combo_parity
			// 
			this.m_combo_parity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_parity.FormattingEnabled = true;
			this.m_combo_parity.Location = new System.Drawing.Point(67, 88);
			this.m_combo_parity.Name = "m_combo_parity";
			this.m_combo_parity.Size = new System.Drawing.Size(74, 21);
			this.m_combo_parity.TabIndex = 3;
			// 
			// m_lbl_stopbits
			// 
			this.m_lbl_stopbits.AutoSize = true;
			this.m_lbl_stopbits.Location = new System.Drawing.Point(161, 63);
			this.m_lbl_stopbits.Name = "m_lbl_stopbits";
			this.m_lbl_stopbits.Size = new System.Drawing.Size(52, 13);
			this.m_lbl_stopbits.TabIndex = 55;
			this.m_lbl_stopbits.Text = "Stop Bits:";
			// 
			// m_combo_stopbits
			// 
			this.m_combo_stopbits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_stopbits.FormattingEnabled = true;
			this.m_combo_stopbits.Location = new System.Drawing.Point(216, 60);
			this.m_combo_stopbits.Name = "m_combo_stopbits";
			this.m_combo_stopbits.Size = new System.Drawing.Size(74, 21);
			this.m_combo_stopbits.TabIndex = 4;
			// 
			// m_lbl_flow_control
			// 
			this.m_lbl_flow_control.AutoSize = true;
			this.m_lbl_flow_control.Location = new System.Drawing.Point(145, 91);
			this.m_lbl_flow_control.Name = "m_lbl_flow_control";
			this.m_lbl_flow_control.Size = new System.Drawing.Size(68, 13);
			this.m_lbl_flow_control.TabIndex = 57;
			this.m_lbl_flow_control.Text = "Flow Control:";
			// 
			// m_combo_flow_control
			// 
			this.m_combo_flow_control.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_combo_flow_control.FormattingEnabled = true;
			this.m_combo_flow_control.Location = new System.Drawing.Point(216, 88);
			this.m_combo_flow_control.Name = "m_combo_flow_control";
			this.m_combo_flow_control.Size = new System.Drawing.Size(74, 21);
			this.m_combo_flow_control.TabIndex = 5;
			// 
			// m_combo_baudrate
			// 
			this.m_combo_baudrate.FormattingEnabled = true;
			this.m_combo_baudrate.Location = new System.Drawing.Point(178, 31);
			this.m_combo_baudrate.Name = "m_combo_baudrate";
			this.m_combo_baudrate.Size = new System.Drawing.Size(112, 21);
			this.m_combo_baudrate.TabIndex = 1;
			// 
			// m_check_dtr_enable
			// 
			this.m_check_dtr_enable.AutoSize = true;
			this.m_check_dtr_enable.Location = new System.Drawing.Point(28, 115);
			this.m_check_dtr_enable.Name = "m_check_dtr_enable";
			this.m_check_dtr_enable.Size = new System.Drawing.Size(85, 17);
			this.m_check_dtr_enable.TabIndex = 58;
			this.m_check_dtr_enable.Text = "DTR Enable";
			this.m_check_dtr_enable.UseVisualStyleBackColor = true;
			// 
			// m_check_rts_enable
			// 
			this.m_check_rts_enable.AutoSize = true;
			this.m_check_rts_enable.Location = new System.Drawing.Point(28, 138);
			this.m_check_rts_enable.Name = "m_check_rts_enable";
			this.m_check_rts_enable.Size = new System.Drawing.Size(84, 17);
			this.m_check_rts_enable.TabIndex = 59;
			this.m_check_rts_enable.Text = "RTS Enable";
			this.m_check_rts_enable.UseVisualStyleBackColor = true;
			// 
			// m_combo_output_filepath
			// 
			this.m_combo_output_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_output_filepath.FormattingEnabled = true;
			this.m_combo_output_filepath.Location = new System.Drawing.Point(28, 177);
			this.m_combo_output_filepath.Name = "m_combo_output_filepath";
			this.m_combo_output_filepath.Size = new System.Drawing.Size(223, 21);
			this.m_combo_output_filepath.TabIndex = 6;
			// 
			// SerialConnectionUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(307, 282);
			this.Controls.Add(this.m_combo_output_filepath);
			this.Controls.Add(this.m_check_rts_enable);
			this.Controls.Add(this.m_check_dtr_enable);
			this.Controls.Add(this.m_combo_baudrate);
			this.Controls.Add(this.m_combo_flow_control);
			this.Controls.Add(this.m_lbl_flow_control);
			this.Controls.Add(this.m_combo_stopbits);
			this.Controls.Add(this.m_lbl_stopbits);
			this.Controls.Add(this.m_combo_parity);
			this.Controls.Add(this.m_lbl_parity);
			this.Controls.Add(this.m_lbl_commport);
			this.Controls.Add(this.m_combo_commport);
			this.Controls.Add(this.m_lbl_baudrate);
			this.Controls.Add(this.m_lbl_databits);
			this.Controls.Add(this.m_combo_databits);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_btn_browse_output);
			this.Controls.Add(this.m_check_append);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(2000, 320);
			this.MinimumSize = new System.Drawing.Size(323, 320);
			this.Name = "SerialConnectionUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Serial Port Connection";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lbl_commport;
		private System.Windows.Forms.ComboBox m_combo_commport;
		private System.Windows.Forms.Label m_lbl_baudrate;
		private System.Windows.Forms.Label m_lbl_databits;
		private System.Windows.Forms.ComboBox m_combo_databits;
		private System.Windows.Forms.Label m_lbl_output_file;
		private System.Windows.Forms.Button m_btn_browse_output;
		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Label m_lbl_parity;
		private System.Windows.Forms.ComboBox m_combo_parity;
		private System.Windows.Forms.Label m_lbl_stopbits;
		private System.Windows.Forms.ComboBox m_combo_stopbits;
		private System.Windows.Forms.Label m_lbl_flow_control;
		private System.Windows.Forms.ComboBox m_combo_flow_control;
		private System.Windows.Forms.ComboBox m_combo_baudrate;
		private System.Windows.Forms.CheckBox m_check_dtr_enable;
		private System.Windows.Forms.CheckBox m_check_rts_enable;
		private System.Windows.Forms.ComboBox m_combo_output_filepath;
	}
}