using System;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using Util = Rylogic.Utility.Util;

namespace CoinFlip
{
	public class LogInUI :Form
	{
		#region UI Elements
		private Label m_lbl_username;
		private ValueBox m_tb_username;
		private Label m_lbl_password;
		private TextBox m_tb_password;
		private Button m_btn_cancel;
		private Label m_lbl_instructions;
		private Button m_btn_ok;
		#endregion

		public LogInUI()
		{
			InitializeComponent();
			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary></summary>
		public string Username
		{
			get { return (string)m_tb_username.Value; }
			set { m_tb_username.Value = value; }
		}

		/// <summary></summary>
		public string Password
		{
			get { return m_tb_password.Text; }
			set { m_tb_password.Text = value; }
		}

		/// <summary></summary>
		private void SetupUI()
		{
			m_tb_username.ValueType = typeof(string);
			m_tb_username.ValidateText = t => Path_.IsValidFilepath(t, false);
			m_tb_username.TextChanged += UpdateUI;
			m_tb_password.TextChanged += UpdateUI;
		}

		/// <summary></summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			var exists = Path_.FileExists(Misc.ResolveUserPath($"{Username}.keys"));
			m_btn_ok.Text = exists ? "Log On" : "Create";
			m_btn_ok.Enabled =
				m_tb_username.Text.HasValue() &&
				m_tb_password.Text.HasValue();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_lbl_username = new System.Windows.Forms.Label();
			this.m_tb_username = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_password = new System.Windows.Forms.Label();
			this.m_tb_password = new System.Windows.Forms.TextBox();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_instructions = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_lbl_username
			// 
			this.m_lbl_username.AutoSize = true;
			this.m_lbl_username.Location = new System.Drawing.Point(12, 47);
			this.m_lbl_username.Name = "m_lbl_username";
			this.m_lbl_username.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_username.TabIndex = 0;
			this.m_lbl_username.Text = "Username:";
			// 
			// m_tb_username
			// 
			this.m_tb_username.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_username.BackColor = System.Drawing.Color.White;
			this.m_tb_username.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_username.BackColorValid = System.Drawing.Color.White;
			this.m_tb_username.CommitValueOnFocusLost = true;
			this.m_tb_username.ForeColor = System.Drawing.Color.Black;
			this.m_tb_username.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_username.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_username.Location = new System.Drawing.Point(31, 63);
			this.m_tb_username.Name = "m_tb_username";
			this.m_tb_username.Size = new System.Drawing.Size(194, 20);
			this.m_tb_username.TabIndex = 1;
			this.m_tb_username.Text = "user name";
			this.m_tb_username.UseValidityColours = true;
			this.m_tb_username.Value = null;
			// 
			// m_lbl_password
			// 
			this.m_lbl_password.AutoSize = true;
			this.m_lbl_password.Location = new System.Drawing.Point(12, 86);
			this.m_lbl_password.Name = "m_lbl_password";
			this.m_lbl_password.Size = new System.Drawing.Size(56, 13);
			this.m_lbl_password.TabIndex = 2;
			this.m_lbl_password.Text = "Password:";
			// 
			// m_tb_password
			// 
			this.m_tb_password.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_password.Location = new System.Drawing.Point(31, 102);
			this.m_tb_password.Name = "m_tb_password";
			this.m_tb_password.PasswordChar = '*';
			this.m_tb_password.Size = new System.Drawing.Size(194, 20);
			this.m_tb_password.TabIndex = 3;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(150, 138);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 4;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(69, 138);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_instructions
			// 
			this.m_lbl_instructions.AutoSize = true;
			this.m_lbl_instructions.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_instructions.Name = "m_lbl_instructions";
			this.m_lbl_instructions.Size = new System.Drawing.Size(219, 26);
			this.m_lbl_instructions.TabIndex = 6;
			this.m_lbl_instructions.Text = "Log on or create a new user. \r\nEach user has separate exchanges API keys";
			// 
			// LogInUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(237, 173);
			this.Controls.Add(this.m_lbl_instructions);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_tb_password);
			this.Controls.Add(this.m_lbl_password);
			this.Controls.Add(this.m_tb_username);
			this.Controls.Add(this.m_lbl_username);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "LogInUI";
			this.Text = "Log In";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
