namespace RyLogViewer
{
	partial class ExportUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExportUI));
			this.m_lbl_desc = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_radio_whole_file = new System.Windows.Forms.RadioButton();
			this.m_radio_selection = new System.Windows.Forms.RadioButton();
			this.m_radio_range = new System.Windows.Forms.RadioButton();
			this.m_spinner_range_min = new System.Windows.Forms.NumericUpDown();
			this.m_spinner_range_max = new System.Windows.Forms.NumericUpDown();
			this.m_btn_range_to_start = new System.Windows.Forms.Button();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_range_to_end = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_range_min)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_range_max)).BeginInit();
			this.SuspendLayout();
			// 
			// m_lbl_desc
			// 
			this.m_lbl_desc.AutoSize = true;
			this.m_lbl_desc.Location = new System.Drawing.Point(17, 17);
			this.m_lbl_desc.Name = "m_lbl_desc";
			this.m_lbl_desc.Size = new System.Drawing.Size(274, 13);
			this.m_lbl_desc.TabIndex = 0;
			this.m_lbl_desc.Text = "Export the current file, applying filters and transformations";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(203, 117);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "Export";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(284, 117);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_radio_whole_file
			// 
			this.m_radio_whole_file.AutoSize = true;
			this.m_radio_whole_file.Location = new System.Drawing.Point(24, 42);
			this.m_radio_whole_file.Name = "m_radio_whole_file";
			this.m_radio_whole_file.Size = new System.Drawing.Size(72, 17);
			this.m_radio_whole_file.TabIndex = 3;
			this.m_radio_whole_file.TabStop = true;
			this.m_radio_whole_file.Text = "Whole file";
			this.m_radio_whole_file.UseVisualStyleBackColor = true;
			// 
			// m_radio_selection
			// 
			this.m_radio_selection.AutoSize = true;
			this.m_radio_selection.Location = new System.Drawing.Point(24, 65);
			this.m_radio_selection.Name = "m_radio_selection";
			this.m_radio_selection.Size = new System.Drawing.Size(104, 17);
			this.m_radio_selection.TabIndex = 4;
			this.m_radio_selection.TabStop = true;
			this.m_radio_selection.Text = "Current selection";
			this.m_radio_selection.UseVisualStyleBackColor = true;
			// 
			// m_radio_range
			// 
			this.m_radio_range.AutoSize = true;
			this.m_radio_range.Location = new System.Drawing.Point(24, 88);
			this.m_radio_range.Name = "m_radio_range";
			this.m_radio_range.Size = new System.Drawing.Size(93, 17);
			this.m_radio_range.TabIndex = 5;
			this.m_radio_range.TabStop = true;
			this.m_radio_range.Text = "Specific range";
			this.m_radio_range.UseVisualStyleBackColor = true;
			// 
			// m_spinner_range_min
			// 
			this.m_spinner_range_min.Location = new System.Drawing.Point(148, 86);
			this.m_spinner_range_min.Name = "m_spinner_range_min";
			this.m_spinner_range_min.Size = new System.Drawing.Size(94, 20);
			this.m_spinner_range_min.TabIndex = 6;
			// 
			// m_spinner_range_max
			// 
			this.m_spinner_range_max.Location = new System.Drawing.Point(246, 86);
			this.m_spinner_range_max.Name = "m_spinner_range_max";
			this.m_spinner_range_max.Size = new System.Drawing.Size(87, 20);
			this.m_spinner_range_max.TabIndex = 7;
			// 
			// m_btn_range_to_start
			// 
			this.m_btn_range_to_start.ImageIndex = 0;
			this.m_btn_range_to_start.ImageList = this.m_image_list;
			this.m_btn_range_to_start.Location = new System.Drawing.Point(123, 84);
			this.m_btn_range_to_start.Name = "m_btn_range_to_start";
			this.m_btn_range_to_start.Size = new System.Drawing.Size(24, 23);
			this.m_btn_range_to_start.TabIndex = 8;
			this.m_btn_range_to_start.UseVisualStyleBackColor = true;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "player_start.png");
			this.m_image_list.Images.SetKeyName(1, "player_end.png");
			// 
			// m_btn_range_to_end
			// 
			this.m_btn_range_to_end.ImageIndex = 1;
			this.m_btn_range_to_end.ImageList = this.m_image_list;
			this.m_btn_range_to_end.Location = new System.Drawing.Point(335, 84);
			this.m_btn_range_to_end.Name = "m_btn_range_to_end";
			this.m_btn_range_to_end.Size = new System.Drawing.Size(24, 23);
			this.m_btn_range_to_end.TabIndex = 9;
			this.m_btn_range_to_end.UseVisualStyleBackColor = true;
			// 
			// ExportUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(371, 152);
			this.Controls.Add(this.m_btn_range_to_end);
			this.Controls.Add(this.m_btn_range_to_start);
			this.Controls.Add(this.m_spinner_range_max);
			this.Controls.Add(this.m_spinner_range_min);
			this.Controls.Add(this.m_radio_range);
			this.Controls.Add(this.m_radio_selection);
			this.Controls.Add(this.m_radio_whole_file);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_desc);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ExportUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Export";
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_range_min)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_range_max)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lbl_desc;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.RadioButton m_radio_whole_file;
		private System.Windows.Forms.RadioButton m_radio_selection;
		private System.Windows.Forms.RadioButton m_radio_range;
		private System.Windows.Forms.NumericUpDown m_spinner_range_min;
		private System.Windows.Forms.NumericUpDown m_spinner_range_max;
		private System.Windows.Forms.Button m_btn_range_to_start;
		private System.Windows.Forms.ImageList m_image_list;
		private System.Windows.Forms.Button m_btn_range_to_end;
	}
}