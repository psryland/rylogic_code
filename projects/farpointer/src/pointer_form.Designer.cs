namespace FarPointer
{
	partial class PointerForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PointerForm));
			this.m_bm = new System.Windows.Forms.PictureBox();
			this.m_lbl_name = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_bm)).BeginInit();
			this.SuspendLayout();
			// 
			// m_bm
			// 
			this.m_bm.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_bm.Image = ((System.Drawing.Image)(resources.GetObject("m_bm.Image")));
			this.m_bm.Location = new System.Drawing.Point(0, 0);
			this.m_bm.Name = "m_bm";
			this.m_bm.Size = new System.Drawing.Size(78, 17);
			this.m_bm.TabIndex = 0;
			this.m_bm.TabStop = false;
			// 
			// m_lbl_name
			// 
			this.m_lbl_name.BackColor = System.Drawing.Color.Fuchsia;
			this.m_lbl_name.Location = new System.Drawing.Point(18, 2);
			this.m_lbl_name.Name = "m_lbl_name";
			this.m_lbl_name.Size = new System.Drawing.Size(51, 13);
			this.m_lbl_name.TabIndex = 1;
			this.m_lbl_name.Text = "unknown";
			// 
			// PointerForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.BackColor = System.Drawing.Color.Fuchsia;
			this.ClientSize = new System.Drawing.Size(78, 17);
			this.ControlBox = false;
			this.Controls.Add(this.m_lbl_name);
			this.Controls.Add(this.m_bm);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "PointerForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
			this.Text = "PointerForm";
			this.TopMost = true;
			this.TransparencyKey = System.Drawing.Color.Fuchsia;
			((System.ComponentModel.ISupportInitialize)(this.m_bm)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.PictureBox m_bm;
		private System.Windows.Forms.Label m_lbl_name;
	}
}