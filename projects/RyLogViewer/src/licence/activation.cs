using System.Drawing;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.common;
using pr.util;

namespace RyLogViewer
{
	public class Activation :Form
	{
		private readonly Licence m_licence;
		private readonly ToolTip m_tt;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private TextBox m_edit_activation_code;
		private Label m_lbl_activation_code;
		private TextBox m_edit_company;
		private Label m_lbl_company;
		private TextBox m_edit_name;
		private Label m_lbl_name;
		private Label m_lbl_valid;
		
		public Activation()
		{
			InitializeComponent();
			m_licence = new Licence();
			m_tt = new ToolTip();
			string tt;
			
			// licence holder
			tt = "The name of the licence holder";
			m_lbl_name.ToolTip(m_tt, tt);
			m_edit_name.ToolTip(m_tt, tt);
			m_edit_name.Text = m_licence.LicenceHolder;
			m_edit_name.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					m_licence.LicenceHolder = m_edit_name.Text;
					UpdateUI();
				};

			// Company
			tt = "The name of the company associated with this licence (optional)";
			m_lbl_company.ToolTip(m_tt, tt);
			m_edit_company.ToolTip(m_tt, tt);
			m_edit_company.Text = m_licence.Company;
			m_edit_company.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					m_licence.Company = m_edit_company.Text;
					UpdateUI();
				};
			
			// Activation code
			tt = "Copy and paste your activation code here";
			m_lbl_activation_code.ToolTip(m_tt, tt);
			m_edit_activation_code.ToolTip(m_tt,tt);
			m_edit_activation_code.Text = m_licence.ActivationCode;
			m_edit_activation_code.TextChanged += (s,a)=>
				{
					if (!((TextBox)s).Modified) return;
					m_licence.ActivationCode = m_edit_activation_code.Text;
					UpdateUI();
				};
			
			UpdateUI();
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI()
		{
			bool crcpass = ActivationCode.CheckCrc(m_edit_activation_code.Text);
			bool valid = ActivationCode.Validate(m_edit_activation_code.Text, Resources.public_key);
			m_lbl_valid.Text = valid ? "Licence Accepted" : crcpass ? "Code Invalid" : "Evaluation Licence";
			m_lbl_valid.BackColor = valid ? Color.LightGreen : Color.LightSalmon;
			m_btn_ok.Enabled = valid;
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
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

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Activation));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_edit_activation_code = new System.Windows.Forms.TextBox();
			this.m_lbl_activation_code = new System.Windows.Forms.Label();
			this.m_edit_company = new System.Windows.Forms.TextBox();
			this.m_lbl_company = new System.Windows.Forms.Label();
			this.m_edit_name = new System.Windows.Forms.TextBox();
			this.m_lbl_name = new System.Windows.Forms.Label();
			this.m_lbl_valid = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(130, 193);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 15;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(211, 193);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 14;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_edit_activation_code
			// 
			this.m_edit_activation_code.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_activation_code.Location = new System.Drawing.Point(12, 116);
			this.m_edit_activation_code.Multiline = true;
			this.m_edit_activation_code.Name = "m_edit_activation_code";
			this.m_edit_activation_code.Size = new System.Drawing.Size(274, 70);
			this.m_edit_activation_code.TabIndex = 13;
			// 
			// m_lbl_activation_code
			// 
			this.m_lbl_activation_code.AutoSize = true;
			this.m_lbl_activation_code.Location = new System.Drawing.Point(9, 100);
			this.m_lbl_activation_code.Name = "m_lbl_activation_code";
			this.m_lbl_activation_code.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_activation_code.TabIndex = 12;
			this.m_lbl_activation_code.Text = "Activation Code:";
			// 
			// m_edit_company
			// 
			this.m_edit_company.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_company.Location = new System.Drawing.Point(12, 72);
			this.m_edit_company.Name = "m_edit_company";
			this.m_edit_company.Size = new System.Drawing.Size(274, 20);
			this.m_edit_company.TabIndex = 11;
			// 
			// m_lbl_company
			// 
			this.m_lbl_company.AutoSize = true;
			this.m_lbl_company.Location = new System.Drawing.Point(9, 56);
			this.m_lbl_company.Name = "m_lbl_company";
			this.m_lbl_company.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_company.TabIndex = 10;
			this.m_lbl_company.Text = "Company Name:";
			// 
			// m_edit_name
			// 
			this.m_edit_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_name.Location = new System.Drawing.Point(12, 28);
			this.m_edit_name.Name = "m_edit_name";
			this.m_edit_name.Size = new System.Drawing.Size(274, 20);
			this.m_edit_name.TabIndex = 9;
			// 
			// m_lbl_name
			// 
			this.m_lbl_name.AutoSize = true;
			this.m_lbl_name.Location = new System.Drawing.Point(9, 12);
			this.m_lbl_name.Name = "m_lbl_name";
			this.m_lbl_name.Size = new System.Drawing.Size(138, 13);
			this.m_lbl_name.TabIndex = 8;
			this.m_lbl_name.Text = "This software is licenced to:";
			// 
			// m_lbl_valid
			// 
			this.m_lbl_valid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_valid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_valid.Location = new System.Drawing.Point(12, 193);
			this.m_lbl_valid.Margin = new System.Windows.Forms.Padding(20);
			this.m_lbl_valid.Name = "m_lbl_valid";
			this.m_lbl_valid.Size = new System.Drawing.Size(100, 23);
			this.m_lbl_valid.TabIndex = 16;
			this.m_lbl_valid.Text = "Licence Accepted";
			this.m_lbl_valid.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// Activation
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(301, 224);
			this.Controls.Add(this.m_lbl_valid);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_edit_activation_code);
			this.Controls.Add(this.m_lbl_activation_code);
			this.Controls.Add(this.m_edit_company);
			this.Controls.Add(this.m_lbl_company);
			this.Controls.Add(this.m_edit_name);
			this.Controls.Add(this.m_lbl_name);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "Activation";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Activation";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
