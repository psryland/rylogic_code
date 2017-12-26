using System;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Utility;

namespace LDraw
{
	public class AboutUI :Form
	{
		#region UI Elements
		private TextBox m_tb_desc;
		private Button m_btn_ok;
		private PictureBox m_pic_logo;
		#endregion

		public AboutUI()
		{
			InitializeComponent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			m_tb_desc.Text =
				$"{Application.ProductName} - Scripted 3D object viewer.\r\n" +
				$"Version: {Application.ProductVersion}\r\n" +
				$"{Util.AppCopyright}\r\n";
			m_tb_desc.SelectionLength = 0;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutUI));
			this.m_tb_desc = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_pic_logo = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.m_pic_logo)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tb_desc
			// 
			this.m_tb_desc.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_desc.BackColor = System.Drawing.SystemColors.Window;
			this.m_tb_desc.Location = new System.Drawing.Point(81, 12);
			this.m_tb_desc.Multiline = true;
			this.m_tb_desc.Name = "m_tb_desc";
			this.m_tb_desc.ReadOnly = true;
			this.m_tb_desc.Size = new System.Drawing.Size(183, 64);
			this.m_tb_desc.TabIndex = 0;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(189, 87);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_pic_logo
			// 
			this.m_pic_logo.Image = ((System.Drawing.Image)(resources.GetObject("m_pic_logo.Image")));
			this.m_pic_logo.Location = new System.Drawing.Point(12, 12);
			this.m_pic_logo.Name = "m_pic_logo";
			this.m_pic_logo.Size = new System.Drawing.Size(64, 64);
			this.m_pic_logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.m_pic_logo.TabIndex = 2;
			this.m_pic_logo.TabStop = false;
			// 
			// AboutUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(276, 122);
			this.Controls.Add(this.m_pic_logo);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_tb_desc);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AboutUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "About LDraw";
			((System.ComponentModel.ISupportInitialize)(this.m_pic_logo)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
