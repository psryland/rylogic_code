namespace FarPointer
{
	partial class ConnectForm
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
			this.m_btn_connect = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_hostname_or_ip = new System.Windows.Forms.Label();
			this.m_lbl_port = new System.Windows.Forms.Label();
			this.m_edit_port = new System.Windows.Forms.TextBox();
			this.m_combo_hostname = new System.Windows.Forms.ComboBox();
			this.m_lbl_name = new System.Windows.Forms.Label();
			this.m_edit_name = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_btn_connect
			// 
			this.m_btn_connect.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_connect.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_connect.Location = new System.Drawing.Point(108, 80);
			this.m_btn_connect.Name = "m_btn_connect";
			this.m_btn_connect.Size = new System.Drawing.Size(75, 23);
			this.m_btn_connect.TabIndex = 3;
			this.m_btn_connect.Text = "Connect";
			this.m_btn_connect.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(189, 80);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 4;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_hostname_or_ip
			// 
			this.m_lbl_hostname_or_ip.AutoSize = true;
			this.m_lbl_hostname_or_ip.Location = new System.Drawing.Point(4, 35);
			this.m_lbl_hostname_or_ip.Name = "m_lbl_hostname_or_ip";
			this.m_lbl_hostname_or_ip.Size = new System.Drawing.Size(83, 13);
			this.m_lbl_hostname_or_ip.TabIndex = 3;
			this.m_lbl_hostname_or_ip.Text = "Hostname or IP:";
			// 
			// m_lbl_port
			// 
			this.m_lbl_port.AutoSize = true;
			this.m_lbl_port.Location = new System.Drawing.Point(58, 59);
			this.m_lbl_port.Name = "m_lbl_port";
			this.m_lbl_port.Size = new System.Drawing.Size(29, 13);
			this.m_lbl_port.TabIndex = 4;
			this.m_lbl_port.Text = "Port:";
			// 
			// m_edit_port
			// 
			this.m_edit_port.Location = new System.Drawing.Point(93, 56);
			this.m_edit_port.Name = "m_edit_port";
			this.m_edit_port.Size = new System.Drawing.Size(59, 20);
			this.m_edit_port.TabIndex = 2;
			// 
			// m_combo_hostname
			// 
			this.m_combo_hostname.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_hostname.FormattingEnabled = true;
			this.m_combo_hostname.Location = new System.Drawing.Point(93, 32);
			this.m_combo_hostname.Name = "m_combo_hostname";
			this.m_combo_hostname.Size = new System.Drawing.Size(171, 21);
			this.m_combo_hostname.TabIndex = 1;
			// 
			// m_lbl_name
			// 
			this.m_lbl_name.AutoSize = true;
			this.m_lbl_name.Location = new System.Drawing.Point(49, 9);
			this.m_lbl_name.Name = "m_lbl_name";
			this.m_lbl_name.Size = new System.Drawing.Size(38, 13);
			this.m_lbl_name.TabIndex = 5;
			this.m_lbl_name.Text = "Name:";
			// 
			// m_edit_name
			// 
			this.m_edit_name.Location = new System.Drawing.Point(93, 6);
			this.m_edit_name.Name = "m_edit_name";
			this.m_edit_name.Size = new System.Drawing.Size(171, 20);
			this.m_edit_name.TabIndex = 0;
			// 
			// ConnectForm
			// 
			this.AcceptButton = this.m_btn_connect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(276, 115);
			this.Controls.Add(this.m_edit_name);
			this.Controls.Add(this.m_lbl_name);
			this.Controls.Add(this.m_combo_hostname);
			this.Controls.Add(this.m_edit_port);
			this.Controls.Add(this.m_lbl_port);
			this.Controls.Add(this.m_lbl_hostname_or_ip);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_connect);
			this.Name = "ConnectForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Connect to Far Pointer server";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btn_connect;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Label m_lbl_hostname_or_ip;
		private System.Windows.Forms.Label m_lbl_port;
		private System.Windows.Forms.TextBox m_edit_port;
		private System.Windows.Forms.ComboBox m_combo_hostname;
		private System.Windows.Forms.Label m_lbl_name;
		private System.Windows.Forms.TextBox m_edit_name;
	}
}