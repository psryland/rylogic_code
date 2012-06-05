namespace RyLogViewer
{
	partial class FindUI
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FindUI));
			this.m_pattern = new RyLogViewer.PatternUI();
			this.m_btn_find_next = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_find_prev = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_pattern
			// 
			this.m_pattern.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_pattern.Location = new System.Drawing.Point(8, 12);
			this.m_pattern.MinimumSize = new System.Drawing.Size(336, 92);
			this.m_pattern.Name = "m_pattern";
			this.m_pattern.Size = new System.Drawing.Size(363, 136);
			this.m_pattern.TabIndex = 0;
			// 
			// m_btn_find_next
			// 
			this.m_btn_find_next.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_next.Location = new System.Drawing.Point(186, 160);
			this.m_btn_find_next.Name = "m_btn_find_next";
			this.m_btn_find_next.Size = new System.Drawing.Size(89, 23);
			this.m_btn_find_next.TabIndex = 1;
			this.m_btn_find_next.Text = "Find Next";
			this.m_btn_find_next.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(281, 160);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(89, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_find_prev
			// 
			this.m_btn_find_prev.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_find_prev.Location = new System.Drawing.Point(91, 160);
			this.m_btn_find_prev.Name = "m_btn_find_prev";
			this.m_btn_find_prev.Size = new System.Drawing.Size(89, 23);
			this.m_btn_find_prev.TabIndex = 3;
			this.m_btn_find_prev.Text = "Find Previous";
			this.m_btn_find_prev.UseVisualStyleBackColor = true;
			// 
			// find_ui
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(383, 195);
			this.Controls.Add(this.m_btn_find_prev);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_find_next);
			this.Controls.Add(this.m_pattern);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(366, 190);
			this.Name = "FindUi";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "Find...";
			this.ResumeLayout(false);

		}

		#endregion

		private PatternUI m_pattern;
		private System.Windows.Forms.Button m_btn_find_next;
		private System.Windows.Forms.Button m_btn_cancel;
		private System.Windows.Forms.Button m_btn_find_prev;
	}
}