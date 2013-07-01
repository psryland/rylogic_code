namespace RyLogViewer
{
	partial class MonitorModeUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorModeUI));
			this.m_track_opacity = new System.Windows.Forms.TrackBar();
			this.m_btn_enable = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_transparency = new System.Windows.Forms.Label();
			this.m_check_click_thru = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_check_always_on_top = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.m_track_opacity)).BeginInit();
			this.SuspendLayout();
			// 
			// m_track_opacity
			// 
			this.m_track_opacity.Location = new System.Drawing.Point(11, 23);
			this.m_track_opacity.Maximum = 100;
			this.m_track_opacity.Name = "m_track_opacity";
			this.m_track_opacity.Size = new System.Drawing.Size(243, 45);
			this.m_track_opacity.TabIndex = 0;
			this.m_track_opacity.TickFrequency = 10;
			this.m_track_opacity.Value = 100;
			// 
			// m_btn_enable
			// 
			this.m_btn_enable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_enable.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_enable.Location = new System.Drawing.Point(98, 155);
			this.m_btn_enable.Name = "m_btn_enable";
			this.m_btn_enable.Size = new System.Drawing.Size(75, 23);
			this.m_btn_enable.TabIndex = 2;
			this.m_btn_enable.Text = "Enable";
			this.m_btn_enable.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(179, 155);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_transparency
			// 
			this.m_lbl_transparency.AutoSize = true;
			this.m_lbl_transparency.Location = new System.Drawing.Point(8, 8);
			this.m_lbl_transparency.Name = "m_lbl_transparency";
			this.m_lbl_transparency.Size = new System.Drawing.Size(75, 13);
			this.m_lbl_transparency.TabIndex = 4;
			this.m_lbl_transparency.Text = "Transparency:";
			// 
			// m_check_click_thru
			// 
			this.m_check_click_thru.AutoSize = true;
			this.m_check_click_thru.Location = new System.Drawing.Point(12, 87);
			this.m_check_click_thru.Name = "m_check_click_thru";
			this.m_check_click_thru.Size = new System.Drawing.Size(127, 17);
			this.m_check_click_thru.TabIndex = 1;
			this.m_check_click_thru.Text = "\"Click-through\" mode";
			this.m_check_click_thru.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(38, 115);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(180, 26);
			this.label1.TabIndex = 5;
			this.label1.Text = "Exit \'monitor mode\' by clicking on the\r\n RyLogViewer icon in the system tray";
			// 
			// m_check_always_on_top
			// 
			this.m_check_always_on_top.AutoSize = true;
			this.m_check_always_on_top.Location = new System.Drawing.Point(11, 64);
			this.m_check_always_on_top.Name = "m_check_always_on_top";
			this.m_check_always_on_top.Size = new System.Drawing.Size(92, 17);
			this.m_check_always_on_top.TabIndex = 6;
			this.m_check_always_on_top.Text = "Always on top";
			this.m_check_always_on_top.UseVisualStyleBackColor = true;
			// 
			// MonitorModeUI
			// 
			this.AcceptButton = this.m_btn_enable;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(267, 191);
			this.Controls.Add(this.m_check_always_on_top);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_check_click_thru);
			this.Controls.Add(this.m_lbl_transparency);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_enable);
			this.Controls.Add(this.m_track_opacity);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MonitorModeUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Monitor Mode";
			((System.ComponentModel.ISupportInitialize)(this.m_track_opacity)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TrackBar m_track_opacity;
		private System.Windows.Forms.Button m_btn_enable;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Label m_lbl_transparency;
		private System.Windows.Forms.CheckBox m_check_click_thru;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckBox m_check_always_on_top;
	}
}