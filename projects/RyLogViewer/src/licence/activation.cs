using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class Activation :Form
	{
		private readonly Licence m_licence;
		private readonly ToolTip m_tt;
		private TextBox m_edit_name;
		private TextBox m_edit_email;
		private TextBox m_edit_company;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Button m_btn_browse;
		private Label m_lbl_company;
		private Label m_lbl_name;
		private Label m_lbl_email;
		private Label m_lbl_valid;

		public Activation(Licence licence)
		{
			InitializeComponent();
			m_licence = licence;
			m_btn_browse.Click += BrowseForLicenceFile;

			UpdateUI();
		}

		/// <summary>Find the licence file, and load data from it</summary>
		private void BrowseForLicenceFile(object sender = null, EventArgs args = null)
		{
			var fd = new OpenFileDialog{Title = "Select your Licence File", Filter = Util.FileDialogFilter("Licence Files","*.xml"), CheckFileExists = true};
			if (fd.ShowDialog(this) != DialogResult.OK)
				return;

			// Rather than copying the file to the app directory, we load data from it,
			// and if the licence is valid, it will be written out to a new licence file
			var lic = new Licence(fd.FileName);
			m_licence.LicenceHolder  = lic.LicenceHolder;
			m_licence.EmailAddress   = lic.EmailAddress;
			m_licence.Company        = lic.Company;
			m_licence.ActivationCode = lic.ActivationCode;
			UpdateUI();
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI()
		{
			m_edit_name.Text    = m_licence.LicenceHolder;
			m_edit_email.Text   = m_licence.EmailAddress;
			m_edit_company.Text = m_licence.Company;

			if (m_licence.ActivationCode.Length == 0)
			{
				m_lbl_valid.Text      = "Free Licence";
				m_lbl_valid.BackColor = Color.LightGreen;
				m_btn_ok.Enabled      = true;
			}
			else
			{
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
			this.m_edit_company = new System.Windows.Forms.TextBox();
			this.m_lbl_company = new System.Windows.Forms.Label();
			this.m_edit_name = new System.Windows.Forms.TextBox();
			this.m_lbl_name = new System.Windows.Forms.Label();
			this.m_lbl_valid = new System.Windows.Forms.Label();
			this.m_lbl_email = new System.Windows.Forms.Label();
			this.m_edit_email = new System.Windows.Forms.TextBox();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(166, 201);
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
			this.m_btn_cancel.Location = new System.Drawing.Point(248, 201);
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
			this.m_edit_company.ReadOnly = true;
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
			this.m_edit_name.ReadOnly = true;
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
			this.m_lbl_valid.Location = new System.Drawing.Point(12, 201);
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
			this.m_edit_email.ReadOnly = true;
			this.m_edit_email.Size = new System.Drawing.Size(310, 20);
			this.m_edit_email.TabIndex = 1;
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(63, 153);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(211, 37);
			this.m_btn_browse.TabIndex = 18;
			this.m_btn_browse.Text = "Select Licence File";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// Activation
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(337, 232);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_edit_email);
			this.Controls.Add(this.m_lbl_email);
			this.Controls.Add(this.m_lbl_valid);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
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
