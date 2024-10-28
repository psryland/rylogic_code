namespace pr
{
	partial class AddPlot
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
			this.m_lbl_plot_title = new System.Windows.Forms.Label();
			this.m_lbl_xlabel = new System.Windows.Forms.Label();
			this.m_lbl_ylabel = new System.Windows.Forms.Label();
			this.m_edit_plot_title = new System.Windows.Forms.TextBox();
			this.m_edit_xlabel = new System.Windows.Forms.TextBox();
			this.m_edit_ylabel = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_lbl_plot_title
			// 
			this.m_lbl_plot_title.AutoSize = true;
			this.m_lbl_plot_title.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_plot_title.Name = "m_lbl_plot_title";
			this.m_lbl_plot_title.Size = new System.Drawing.Size(51, 13);
			this.m_lbl_plot_title.TabIndex = 0;
			this.m_lbl_plot_title.Text = "Plot Title:";
			// 
			// m_lbl_xlabel
			// 
			this.m_lbl_xlabel.AutoSize = true;
			this.m_lbl_xlabel.Location = new System.Drawing.Point(17, 38);
			this.m_lbl_xlabel.Name = "m_lbl_xlabel";
			this.m_lbl_xlabel.Size = new System.Drawing.Size(46, 13);
			this.m_lbl_xlabel.TabIndex = 1;
			this.m_lbl_xlabel.Text = "X Label:";
			// 
			// m_lbl_ylabel
			// 
			this.m_lbl_ylabel.AutoSize = true;
			this.m_lbl_ylabel.Location = new System.Drawing.Point(17, 64);
			this.m_lbl_ylabel.Name = "m_lbl_ylabel";
			this.m_lbl_ylabel.Size = new System.Drawing.Size(46, 13);
			this.m_lbl_ylabel.TabIndex = 2;
			this.m_lbl_ylabel.Text = "Y Label:";
			// 
			// m_edit_plot_title
			// 
			this.m_edit_plot_title.Location = new System.Drawing.Point(69, 6);
			this.m_edit_plot_title.Name = "m_edit_plot_title";
			this.m_edit_plot_title.Size = new System.Drawing.Size(203, 20);
			this.m_edit_plot_title.TabIndex = 3;
			// 
			// m_edit_xlabel
			// 
			this.m_edit_xlabel.Location = new System.Drawing.Point(69, 35);
			this.m_edit_xlabel.Name = "m_edit_xlabel";
			this.m_edit_xlabel.Size = new System.Drawing.Size(203, 20);
			this.m_edit_xlabel.TabIndex = 4;
			// 
			// m_edit_ylabel
			// 
			this.m_edit_ylabel.Location = new System.Drawing.Point(69, 61);
			this.m_edit_ylabel.Name = "m_edit_ylabel";
			this.m_edit_ylabel.Size = new System.Drawing.Size(203, 20);
			this.m_edit_ylabel.TabIndex = 5;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(197, 87);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 6;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(116, 87);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 7;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// AddPlot
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(284, 121);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_ylabel);
			this.Controls.Add(this.m_edit_xlabel);
			this.Controls.Add(this.m_edit_plot_title);
			this.Controls.Add(this.m_lbl_ylabel);
			this.Controls.Add(this.m_lbl_xlabel);
			this.Controls.Add(this.m_lbl_plot_title);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AddPlot";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Add Plot";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lbl_plot_title;
		private System.Windows.Forms.Label m_lbl_xlabel;
		private System.Windows.Forms.Label m_lbl_ylabel;
		private System.Windows.Forms.TextBox m_edit_plot_title;
		private System.Windows.Forms.TextBox m_edit_xlabel;
		private System.Windows.Forms.TextBox m_edit_ylabel;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
	}
}