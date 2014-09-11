using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class Activation :Form
	{
		private readonly Licence m_licence;
		private readonly ToolTip m_tt;
		private readonly Timer m_timer;
		private TextBox m_edit_name;
		private TextBox m_edit_email;
		private TextBox m_edit_company;
		private TextBox m_edit_activation_code;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Label m_lbl_company;
		private Label m_lbl_name;
		private Label m_lbl_email;
		private Label m_lbl_activation_code;
		private LinkLabel m_lbl_forgotten;
		private Label m_lbl_valid;

		public Activation(Licence licence)
		{
			InitializeComponent();
			m_licence = licence;
			m_tt = new ToolTip();

			// Initialise fields from the licence
			m_edit_name.ToolTip(m_tt, "The name of the license holder as given in the license information email");
			m_edit_name.Text = m_licence.LicenceHolder;
			m_edit_name.Validated += UpdateUI;

			m_edit_email.ToolTip(m_tt, "The email address associated with the license as given in the license information email");
			m_edit_email.Text = m_licence.EmailAddress;
			m_edit_email.Validated += UpdateUI;

			m_edit_company.ToolTip(m_tt, "The company name associated with the license");
			m_edit_company.Text = m_licence.Company;
			m_edit_company.Validated += UpdateUI;

			m_edit_activation_code.ToolTip(m_tt, "Copy and paste the activation code given in your licence information email here");
			m_edit_activation_code.Text = m_licence.ActivationCode;
			m_edit_activation_code.Validated += UpdateUI;

			m_lbl_forgotten.LinkClicked += HandleLostLink;

			// Start a timer to scan the clipboard for valid licence info
			m_timer = new Timer{Interval = 500, Enabled = true};
			m_timer.Tick += ScanClipboard;

			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			ScanClipboard();
		}

		/// <summary>Try to extract the license info from text data on the clipboard</summary>
		private void ScanClipboard(object sender = null, EventArgs args = null)
		{
			// Don't do anything if the license is valid
			if (m_licence.Valid)
				return;

			var lic = new Licence();
			var text    = Clipboard.GetText(TextDataFormat.Text);
			lic.LicenceHolder  = text.SubstringRegex(@"\s*License Holder:\s+", "($|<br/>)");
			lic.EmailAddress   = text.SubstringRegex(@"\s*Email:\s+", "($|<br/>)");
			lic.Company        = text.SubstringRegex(@"\s*Company:\s+", "($|<br/>)");
			lic.ActivationCode = text.SubstringRegex(@"\s*Activation Code:\s+", "($|<br/>)", RegexOptions.Multiline);
			if (lic.Valid)
			{
				m_edit_name.Text            = lic.LicenceHolder  ;
				m_edit_email.Text           = lic.EmailAddress   ;
				m_edit_company.Text         = lic.Company        ;
				m_edit_activation_code.Text = lic.ActivationCode ;
			}

			UpdateUI();
		}

		/// <summary>Navigate the 'request my license info again' page</summary>
		private void HandleLostLink(object sender, LinkLabelLinkClickedEventArgs e)
		{

		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			if (m_edit_activation_code.Text.Length == 0)
			{
				m_lbl_valid.Text      = "Free Licence";
				m_lbl_valid.BackColor = Color.LightGreen;
				m_btn_ok.Enabled      = true;
			}
			else
			{
				m_licence.LicenceHolder  = m_edit_name.Text           ;
				m_licence.EmailAddress   = m_edit_email.Text          ;
				m_licence.Company        = m_edit_company.Text        ;
				m_licence.ActivationCode = m_edit_activation_code.Text;

				var valid = m_licence.Valid;
				m_lbl_valid.Text      = valid ? "Valid Licence" : "Invalid Licence";
				m_lbl_valid.BackColor = valid ? Color.LightGreen : Color.LightSalmon;
				m_btn_ok.Enabled      = valid;
			}
		}

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Activation));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_edit_company = new System.Windows.Forms.TextBox();
			this.m_lbl_company = new System.Windows.Forms.Label();
			this.m_edit_name = new System.Windows.Forms.TextBox();
			this.m_lbl_name = new System.Windows.Forms.Label();
			this.m_lbl_valid = new System.Windows.Forms.Label();
			this.m_lbl_email = new System.Windows.Forms.Label();
			this.m_edit_email = new System.Windows.Forms.TextBox();
			this.m_edit_activation_code = new System.Windows.Forms.TextBox();
			this.m_lbl_activation_code = new System.Windows.Forms.Label();
			this.m_lbl_forgotten = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(166, 267);
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
			this.m_btn_cancel.Location = new System.Drawing.Point(248, 267);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 6;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_edit_company
			// 
			this.m_edit_company.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_company.Location = new System.Drawing.Point(13, 120);
			this.m_edit_company.Name = "m_edit_company";
			this.m_edit_company.Size = new System.Drawing.Size(310, 20);
			this.m_edit_company.TabIndex = 2;
			// 
			// m_lbl_company
			// 
			this.m_lbl_company.AutoSize = true;
			this.m_lbl_company.Location = new System.Drawing.Point(9, 102);
			this.m_lbl_company.Name = "m_lbl_company";
			this.m_lbl_company.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_company.TabIndex = 10;
			this.m_lbl_company.Text = "Company Name:";
			// 
			// m_edit_name
			// 
			this.m_edit_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_name.Location = new System.Drawing.Point(13, 28);
			this.m_edit_name.Name = "m_edit_name";
			this.m_edit_name.Size = new System.Drawing.Size(310, 20);
			this.m_edit_name.TabIndex = 0;
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
			this.m_lbl_valid.Location = new System.Drawing.Point(12, 267);
			this.m_lbl_valid.Margin = new System.Windows.Forms.Padding(20);
			this.m_lbl_valid.Name = "m_lbl_valid";
			this.m_lbl_valid.Size = new System.Drawing.Size(100, 23);
			this.m_lbl_valid.TabIndex = 4;
			this.m_lbl_valid.Text = "Licence Accepted";
			this.m_lbl_valid.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// m_lbl_email
			// 
			this.m_lbl_email.AutoSize = true;
			this.m_lbl_email.Location = new System.Drawing.Point(9, 56);
			this.m_lbl_email.Name = "m_lbl_email";
			this.m_lbl_email.Size = new System.Drawing.Size(76, 13);
			this.m_lbl_email.TabIndex = 17;
			this.m_lbl_email.Text = "Email Address:";
			// 
			// m_edit_email
			// 
			this.m_edit_email.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_email.Location = new System.Drawing.Point(13, 72);
			this.m_edit_email.Name = "m_edit_email";
			this.m_edit_email.Size = new System.Drawing.Size(310, 20);
			this.m_edit_email.TabIndex = 1;
			// 
			// m_edit_activation_code
			// 
			this.m_edit_activation_code.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_activation_code.Location = new System.Drawing.Point(13, 162);
			this.m_edit_activation_code.Multiline = true;
			this.m_edit_activation_code.Name = "m_edit_activation_code";
			this.m_edit_activation_code.Size = new System.Drawing.Size(310, 82);
			this.m_edit_activation_code.TabIndex = 18;
			this.m_edit_activation_code.Text = "<paste your licence information here>";
			// 
			// m_lbl_activation_code
			// 
			this.m_lbl_activation_code.AutoSize = true;
			this.m_lbl_activation_code.Location = new System.Drawing.Point(9, 146);
			this.m_lbl_activation_code.Name = "m_lbl_activation_code";
			this.m_lbl_activation_code.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_activation_code.TabIndex = 19;
			this.m_lbl_activation_code.Text = "Activation Code:";
			// 
			// m_lbl_forgotten
			// 
			this.m_lbl_forgotten.AutoSize = true;
			this.m_lbl_forgotten.Location = new System.Drawing.Point(12, 247);
			this.m_lbl_forgotten.Name = "m_lbl_forgotten";
			this.m_lbl_forgotten.Size = new System.Drawing.Size(259, 13);
			this.m_lbl_forgotten.TabIndex = 20;
			this.m_lbl_forgotten.TabStop = true;
			this.m_lbl_forgotten.Text = "Lost your license information? Request it again here...";
			// 
			// Activation
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(337, 298);
			this.Controls.Add(this.m_lbl_forgotten);
			this.Controls.Add(this.m_lbl_activation_code);
			this.Controls.Add(this.m_edit_activation_code);
			this.Controls.Add(this.m_edit_email);
			this.Controls.Add(this.m_lbl_email);
			this.Controls.Add(this.m_lbl_valid);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_edit_company);
			this.Controls.Add(this.m_lbl_company);
			this.Controls.Add(this.m_edit_name);
			this.Controls.Add(this.m_lbl_name);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
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
