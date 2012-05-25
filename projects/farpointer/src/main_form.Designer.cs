namespace FarPointer
{
	partial class MainForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.m_list_connected = new System.Windows.Forms.ListBox();
			this.m_lbl_connected_pointers = new System.Windows.Forms.Label();
			this.m_btn_connect = new System.Windows.Forms.Button();
			this.m_tray_notify = new System.Windows.Forms.NotifyIcon(this.components);
			this.m_tray_contextmenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.m_traymenu_options = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_traymenu_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_tray_contextmenu.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_list_connected
			// 
			this.m_list_connected.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_list_connected.FormattingEnabled = true;
			this.m_list_connected.IntegralHeight = false;
			this.m_list_connected.Location = new System.Drawing.Point(3, 49);
			this.m_list_connected.Name = "m_list_connected";
			this.m_list_connected.Size = new System.Drawing.Size(102, 144);
			this.m_list_connected.TabIndex = 0;
			// 
			// m_lbl_connected_pointers
			// 
			this.m_lbl_connected_pointers.AutoSize = true;
			this.m_lbl_connected_pointers.Location = new System.Drawing.Point(0, 33);
			this.m_lbl_connected_pointers.Name = "m_lbl_connected_pointers";
			this.m_lbl_connected_pointers.Size = new System.Drawing.Size(103, 13);
			this.m_lbl_connected_pointers.TabIndex = 1;
			this.m_lbl_connected_pointers.Text = "Connected Pointers:";
			// 
			// m_btn_connect
			// 
			this.m_btn_connect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_connect.Location = new System.Drawing.Point(3, 5);
			this.m_btn_connect.Name = "m_btn_connect";
			this.m_btn_connect.Size = new System.Drawing.Size(103, 25);
			this.m_btn_connect.TabIndex = 2;
			this.m_btn_connect.Text = "Connect";
			this.m_btn_connect.UseVisualStyleBackColor = true;
			// 
			// m_tray_notify
			// 
			this.m_tray_notify.BalloonTipTitle = "Far Pointer";
			this.m_tray_notify.ContextMenuStrip = this.m_tray_contextmenu;
			this.m_tray_notify.Icon = ((System.Drawing.Icon)(resources.GetObject("m_tray_notify.Icon")));
			this.m_tray_notify.Text = "Far Pointer";
			this.m_tray_notify.Visible = true;
			// 
			// m_tray_contextmenu
			// 
			this.m_tray_contextmenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_traymenu_options,
            this.toolStripSeparator2,
            this.m_traymenu_exit});
			this.m_tray_contextmenu.Name = "m_tray_contextmenu";
			this.m_tray_contextmenu.Size = new System.Drawing.Size(117, 54);
			// 
			// m_traymenu_options
			// 
			this.m_traymenu_options.Name = "m_traymenu_options";
			this.m_traymenu_options.Size = new System.Drawing.Size(116, 22);
			this.m_traymenu_options.Text = "&Options";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(113, 6);
			// 
			// m_traymenu_exit
			// 
			this.m_traymenu_exit.Name = "m_traymenu_exit";
			this.m_traymenu_exit.Size = new System.Drawing.Size(116, 22);
			this.m_traymenu_exit.Text = "E&xit";
			// 
			// MainForm
			// 
			this.AcceptButton = this.m_btn_connect;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(108, 197);
			this.Controls.Add(this.m_btn_connect);
			this.Controls.Add(this.m_lbl_connected_pointers);
			this.Controls.Add(this.m_list_connected);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainForm";
			this.Text = "Far Pointer";
			this.m_tray_contextmenu.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ListBox m_list_connected;
		private System.Windows.Forms.Label m_lbl_connected_pointers;
		private System.Windows.Forms.Button m_btn_connect;
		private System.Windows.Forms.NotifyIcon m_tray_notify;
		private System.Windows.Forms.ContextMenuStrip m_tray_contextmenu;
		private System.Windows.Forms.ToolStripMenuItem m_traymenu_options;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem m_traymenu_exit;
	}
}

