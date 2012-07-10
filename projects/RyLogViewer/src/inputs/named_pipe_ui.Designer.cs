namespace RyLogViewer
{
	partial class NamedPipeUI
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
			this.m_combo_output_filepath = new System.Windows.Forms.ComboBox();
			this.m_lbl_output_file = new System.Windows.Forms.Label();
			this.m_btn_browse_output = new System.Windows.Forms.Button();
			this.m_check_append = new System.Windows.Forms.CheckBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_pipe_name = new System.Windows.Forms.Label();
			this.m_combo_pipe_name = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// m_combo_output_filepath
			// 
			this.m_combo_output_filepath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_output_filepath.FormattingEnabled = true;
			this.m_combo_output_filepath.Location = new System.Drawing.Point(31, 76);
			this.m_combo_output_filepath.Name = "m_combo_output_filepath";
			this.m_combo_output_filepath.Size = new System.Drawing.Size(223, 21);
			this.m_combo_output_filepath.TabIndex = 1;
			// 
			// m_lbl_output_file
			// 
			this.m_lbl_output_file.AutoSize = true;
			this.m_lbl_output_file.Location = new System.Drawing.Point(11, 61);
			this.m_lbl_output_file.Name = "m_lbl_output_file";
			this.m_lbl_output_file.Size = new System.Drawing.Size(149, 13);
			this.m_lbl_output_file.TabIndex = 56;
			this.m_lbl_output_file.Text = "File to write program output to:";
			// 
			// m_btn_browse_output
			// 
			this.m_btn_browse_output.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse_output.Location = new System.Drawing.Point(260, 75);
			this.m_btn_browse_output.Name = "m_btn_browse_output";
			this.m_btn_browse_output.Size = new System.Drawing.Size(34, 23);
			this.m_btn_browse_output.TabIndex = 2;
			this.m_btn_browse_output.Text = "...";
			this.m_btn_browse_output.UseVisualStyleBackColor = true;
			// 
			// m_check_append
			// 
			this.m_check_append.AutoSize = true;
			this.m_check_append.Location = new System.Drawing.Point(31, 102);
			this.m_check_append.Name = "m_check_append";
			this.m_check_append.Size = new System.Drawing.Size(113, 17);
			this.m_check_append.TabIndex = 3;
			this.m_check_append.Text = "Append to existing";
			this.m_check_append.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(138, 125);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 4;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(219, 125);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 5;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_pipe_name
			// 
			this.m_lbl_pipe_name.AutoSize = true;
			this.m_lbl_pipe_name.Location = new System.Drawing.Point(11, 18);
			this.m_lbl_pipe_name.Name = "m_lbl_pipe_name";
			this.m_lbl_pipe_name.Size = new System.Drawing.Size(62, 13);
			this.m_lbl_pipe_name.TabIndex = 58;
			this.m_lbl_pipe_name.Text = "Pipe Name:";
			// 
			// m_combo_pipe_name
			// 
			this.m_combo_pipe_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_combo_pipe_name.FormattingEnabled = true;
			this.m_combo_pipe_name.Location = new System.Drawing.Point(31, 34);
			this.m_combo_pipe_name.Name = "m_combo_pipe_name";
			this.m_combo_pipe_name.Size = new System.Drawing.Size(263, 21);
			this.m_combo_pipe_name.TabIndex = 0;
			// 
			// NamedPipeUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(307, 162);
			this.Controls.Add(this.m_lbl_pipe_name);
			this.Controls.Add(this.m_combo_pipe_name);
			this.Controls.Add(this.m_combo_output_filepath);
			this.Controls.Add(this.m_lbl_output_file);
			this.Controls.Add(this.m_btn_browse_output);
			this.Controls.Add(this.m_check_append);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.MaximumSize = new System.Drawing.Size(2000, 200);
			this.MinimumSize = new System.Drawing.Size(323, 200);
			this.Name = "NamedPipeUI";
			this.Text = "Named Pipe Connection";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox m_combo_output_filepath;
		private System.Windows.Forms.Label m_lbl_output_file;
		private System.Windows.Forms.Button m_btn_browse_output;
		private System.Windows.Forms.CheckBox m_check_append;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Label m_lbl_pipe_name;
		private System.Windows.Forms.ComboBox m_combo_pipe_name;
	}
}