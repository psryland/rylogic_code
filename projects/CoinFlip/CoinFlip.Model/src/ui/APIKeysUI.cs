using System;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class APIKeysUI :Form
	{
		#region UI Elements
		private Label m_lbl_instructions;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private TextBox m_tb_secret;
		private Label m_lbl_api_secret;
		private TextBox m_tb_key;
		private Label m_lbl_api_key;
		#endregion

		public APIKeysUI()
		{
			InitializeComponent();
			SetupUI();
			UpdateUI();
			CreateHandle();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary></summary>
		public string APIKey
		{
			get { return m_tb_key.Text; }
			set { m_tb_key.Text = value; }
		}

		/// <summary></summary>
		public string APISecret
		{
			get { return m_tb_secret.Text; }
			set { m_tb_secret.Text = value; }
		}

		/// <summary></summary>
		public string Desc
		{
			get { return m_lbl_instructions.Text; }
			set { m_lbl_instructions.Text = value; }
		}

		/// <summary></summary>
		private void SetupUI()
		{
			m_tb_key.TextChanged += UpdateUI;
			m_tb_secret.TextChanged += UpdateUI;
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_btn_ok.Enabled =
				m_tb_key.Text.HasValue() &&
				m_tb_secret.Text.HasValue();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_lbl_instructions = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_tb_secret = new System.Windows.Forms.TextBox();
			this.m_lbl_api_secret = new System.Windows.Forms.Label();
			this.m_tb_key = new System.Windows.Forms.TextBox();
			this.m_lbl_api_key = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_lbl_instructions
			// 
			this.m_lbl_instructions.AutoSize = true;
			this.m_lbl_instructions.Location = new System.Drawing.Point(28, 9);
			this.m_lbl_instructions.Name = "m_lbl_instructions";
			this.m_lbl_instructions.Size = new System.Drawing.Size(310, 26);
			this.m_lbl_instructions.TabIndex = 13;
			this.m_lbl_instructions.Text = "Enter the API key and secret for your account on this exchange.\r\nThis will be sto" +
    "red in an encrypted file in your user data directory.";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(279, 178);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 12;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(360, 178);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 11;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_tb_secret
			// 
			this.m_tb_secret.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_secret.Location = new System.Drawing.Point(31, 145);
			this.m_tb_secret.Name = "m_tb_secret";
			this.m_tb_secret.Size = new System.Drawing.Size(404, 20);
			this.m_tb_secret.TabIndex = 10;
			// 
			// m_lbl_api_secret
			// 
			this.m_lbl_api_secret.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_api_secret.AutoSize = true;
			this.m_lbl_api_secret.Location = new System.Drawing.Point(12, 129);
			this.m_lbl_api_secret.Name = "m_lbl_api_secret";
			this.m_lbl_api_secret.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_api_secret.TabIndex = 9;
			this.m_lbl_api_secret.Text = "API Secret";
			// 
			// m_tb_key
			// 
			this.m_tb_key.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_key.BackColor = System.Drawing.Color.White;
			this.m_tb_key.ForeColor = System.Drawing.Color.Black;
			this.m_tb_key.Location = new System.Drawing.Point(31, 106);
			this.m_tb_key.Name = "m_tb_key";
			this.m_tb_key.Size = new System.Drawing.Size(404, 20);
			this.m_tb_key.TabIndex = 8;
			// 
			// m_lbl_api_key
			// 
			this.m_lbl_api_key.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_api_key.AutoSize = true;
			this.m_lbl_api_key.Location = new System.Drawing.Point(12, 90);
			this.m_lbl_api_key.Name = "m_lbl_api_key";
			this.m_lbl_api_key.Size = new System.Drawing.Size(48, 13);
			this.m_lbl_api_key.TabIndex = 7;
			this.m_lbl_api_key.Text = "API Key:";
			// 
			// APIKeysUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(447, 213);
			this.Controls.Add(this.m_lbl_instructions);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_tb_secret);
			this.Controls.Add(this.m_lbl_api_secret);
			this.Controls.Add(this.m_tb_key);
			this.Controls.Add(this.m_lbl_api_key);
			this.Name = "APIKeysUI";
			this.Text = "Set API Keys";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
