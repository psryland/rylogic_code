namespace RyLogViewer
{
	partial class SwizzleUI
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
			this.m_lbl_to = new System.Windows.Forms.Label();
			this.m_lbl_from = new System.Windows.Forms.Label();
			this.m_edit_output = new System.Windows.Forms.TextBox();
			this.m_edit_source = new System.Windows.Forms.TextBox();
			this.m_edit_result = new System.Windows.Forms.TextBox();
			this.m_lbl_result = new System.Windows.Forms.Label();
			this.m_lbl_test = new System.Windows.Forms.Label();
			this.m_edit_test = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_help = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_lbl_to
			// 
			this.m_lbl_to.AutoSize = true;
			this.m_lbl_to.Location = new System.Drawing.Point(6, 59);
			this.m_lbl_to.Name = "m_lbl_to";
			this.m_lbl_to.Size = new System.Drawing.Size(42, 13);
			this.m_lbl_to.TabIndex = 19;
			this.m_lbl_to.Text = "Output:";
			// 
			// m_lbl_from
			// 
			this.m_lbl_from.AutoSize = true;
			this.m_lbl_from.Location = new System.Drawing.Point(2, 37);
			this.m_lbl_from.Name = "m_lbl_from";
			this.m_lbl_from.Size = new System.Drawing.Size(44, 13);
			this.m_lbl_from.TabIndex = 18;
			this.m_lbl_from.Text = "Source:";
			// 
			// m_edit_output
			// 
			this.m_edit_output.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_output.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_edit_output.Location = new System.Drawing.Point(48, 56);
			this.m_edit_output.Name = "m_edit_output";
			this.m_edit_output.Size = new System.Drawing.Size(219, 20);
			this.m_edit_output.TabIndex = 2;
			// 
			// m_edit_source
			// 
			this.m_edit_source.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_source.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_edit_source.Location = new System.Drawing.Point(48, 34);
			this.m_edit_source.Name = "m_edit_source";
			this.m_edit_source.Size = new System.Drawing.Size(219, 20);
			this.m_edit_source.TabIndex = 1;
			// 
			// m_edit_result
			// 
			this.m_edit_result.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_result.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_edit_result.Location = new System.Drawing.Point(48, 78);
			this.m_edit_result.Name = "m_edit_result";
			this.m_edit_result.ReadOnly = true;
			this.m_edit_result.Size = new System.Drawing.Size(219, 20);
			this.m_edit_result.TabIndex = 3;
			// 
			// m_lbl_result
			// 
			this.m_lbl_result.AutoSize = true;
			this.m_lbl_result.Location = new System.Drawing.Point(6, 81);
			this.m_lbl_result.Name = "m_lbl_result";
			this.m_lbl_result.Size = new System.Drawing.Size(40, 13);
			this.m_lbl_result.TabIndex = 17;
			this.m_lbl_result.Text = "Result:";
			// 
			// m_lbl_test
			// 
			this.m_lbl_test.AutoSize = true;
			this.m_lbl_test.Location = new System.Drawing.Point(15, 15);
			this.m_lbl_test.Name = "m_lbl_test";
			this.m_lbl_test.Size = new System.Drawing.Size(31, 13);
			this.m_lbl_test.TabIndex = 15;
			this.m_lbl_test.Text = "Test:";
			// 
			// m_edit_test
			// 
			this.m_edit_test.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_test.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_edit_test.Location = new System.Drawing.Point(48, 12);
			this.m_edit_test.Name = "m_edit_test";
			this.m_edit_test.Size = new System.Drawing.Size(219, 20);
			this.m_edit_test.TabIndex = 0;
			this.m_edit_test.Text = "Swizzle mapping test text";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(139, 108);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(220, 108);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 6;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_help
			// 
			this.m_btn_help.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_help.Location = new System.Drawing.Point(273, 12);
			this.m_btn_help.Name = "m_btn_help";
			this.m_btn_help.Size = new System.Drawing.Size(22, 21);
			this.m_btn_help.TabIndex = 4;
			this.m_btn_help.Text = "?";
			this.m_btn_help.UseVisualStyleBackColor = true;
			// 
			// SwizzleUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(307, 143);
			this.Controls.Add(this.m_btn_help);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_to);
			this.Controls.Add(this.m_lbl_from);
			this.Controls.Add(this.m_edit_output);
			this.Controls.Add(this.m_edit_source);
			this.Controls.Add(this.m_edit_result);
			this.Controls.Add(this.m_lbl_result);
			this.Controls.Add(this.m_lbl_test);
			this.Controls.Add(this.m_edit_test);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SwizzleUI";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configure Swizzle";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lbl_to;
		private System.Windows.Forms.Label m_lbl_from;
		private System.Windows.Forms.TextBox m_edit_output;
		private System.Windows.Forms.TextBox m_edit_source;
		private System.Windows.Forms.TextBox m_edit_result;
		private System.Windows.Forms.Label m_lbl_result;
		private System.Windows.Forms.Label m_lbl_test;
		private System.Windows.Forms.TextBox m_edit_test;
		private System.Windows.Forms.Button m_btn_ok;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Button m_btn_help;
	}
}