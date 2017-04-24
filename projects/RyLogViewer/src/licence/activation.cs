using System;
using System.Drawing;
using System.IO;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.gui;
using pr.inet;
using pr.util;

namespace RyLogViewer
{
	public class Activation :Form
	{
		private readonly Main m_main;

		#region UI Elements
		private TextBox m_tb_versions;
		private TextBox m_tb_company;
		private TextBox m_tb_email;
		private TextBox m_tb_name;
		private Button m_btn_locate_licence;
		private Label m_lbl_have_a_licence;
		private Label m_lbl_name;
		private Label m_lbl_email;
		private Label m_lbl_company;
		private Label m_lbl_versions;
		private Label m_lbl_valid;
		private LinkLabel m_lnk_purchase;
		private Button m_btn_ok;
		private ToolTip m_tt;
		#endregion

		public Activation(Main main)
		{
			InitializeComponent();
			m_main = main;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Locate button
			m_btn_locate_licence.ToolTip(m_tt, "Browse to the licence file you received after purchasing RyLogViewer");
			m_btn_locate_licence.Click += (s,a) =>
			{
				LocateLicenceFile();
				UpdateUI();
			};

			// Licence holder name
			m_tb_name.ToolTip(m_tt, "The name of the licence holder as given in the licence information");

			// Associated email address
			m_tb_email.ToolTip(m_tt, "The email address associated with the licence");

			// Optional company name
			m_tb_company.ToolTip(m_tt, "The company name associated with the licence. (Optional)");

			// Version mask
			m_tb_versions.ToolTip(m_tt, "The versions that the licence is valid for");

			// Purchase licence link
			m_lnk_purchase.LinkClicked += HandlePurchaseLink;
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			var lic = new Licence(m_main.StartupOptions.LicenceFilepath);

			m_tb_name    .Text = lic.LicenceHolder;
			m_tb_email   .Text = lic.EmailAddress;
			m_tb_company .Text = lic.Company;
			m_tb_versions.Text = lic.VersionMask;

			m_lbl_valid.Text =
				lic.Valid ? "Valid Licence" :
				lic.IsFreeLicence ? "Free Licence" :
				lic.NotForThisVersion ? "Old Licence" :
				"Invalid licence";

			m_lbl_valid.BackColor =
				lic.Valid ? Color.LightGreen :
				lic.IsFreeLicence ? Color.LightGreen :
				Color.LightSalmon;
		}

		/// <summary>Browse to the licence file</summary>
		private void LocateLicenceFile()
		{
			// Prompt for the user to find the licence file
			var filepath = (string)null;
			using (var dlg = new OpenFileDialog { Title = "Locate Licence File", Filter = "RyLogViewer Licence File|licence.xml" })
			{
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				filepath = dlg.FileName;
			}

			try
			{
				// Check the licence
				var lic = new Licence(filepath);
				if (lic.Valid)
				{
					// If there is an existing licence file, confirm overwrite
					if (Path_.FileExists(m_main.StartupOptions.LicenceFilepath))
					{
						// Read the existing licence
						var existing_lic = (Licence)null;
						try { existing_lic = new Licence(m_main.StartupOptions.LicenceFilepath); } catch { }
						if (existing_lic != null && existing_lic.Valid)
						{
							// Prompt if about to override an existing valid licence
							var res = MsgBox.Show(this, Str.Build(
								"An existing valid licence already exists:\r\n",
								"Licence Holder: {0}\r\n".Fmt(existing_lic.LicenceHolder),
								"Email Address: {0}\r\n".Fmt(existing_lic.EmailAddress),
								"\r\n",
								"Do you want to replace this licence?"),
								Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);
							if (res != DialogResult.Yes)
								return;
						}

						// Write the licence to the expected location
						lic.WriteLicenceFile(m_main.StartupOptions.LicenceFilepath);
						UpdateUI();

						// Say thank you
						MsgBox.Show(this,
							"Thank you for activating "+Application.ProductName+".\r\n" +
							"Your support is greatly appreciated."
							,"Activation Successful"
							,MessageBoxButtons.OK ,MessageBoxIcon.Information);
					}
					return;
				}

				// Valid licence but not for this version
				if (lic.NotForThisVersion)
				{
					MsgBox.Show(this,
						"This licence is for an older version of "+Application.ProductName+".\r\n" +
						"\r\n" +
						"Please consider purchasing a new licence for this version.\r\n" +
						"If you believe this to be an error, please contact '"+Constants.SupportEmail+"' for support."
						,"Licence Out of Date"
						,MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				// Do nothing with free licences
				if (lic.IsFreeLicence)
				{
					MsgBox.Show(this,
						"This licence is the free edition licence.\r\n" +
						"\r\n" +
						"Please consider purchasing a licence for this version.\r\n" +
						"If you believe this to be an error, please contact '"+Constants.SupportEmail+"' for support."
						,"Free Edition Licence"
						,MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}

				// Licence is invalid
				MsgBox.Show(this,
					"This licence file is invalid.\r\n" +
					"\r\n" +
					"If you believe this to be an error, please contact '"+Constants.SupportEmail+"' for support."
					,"Activation Failed"
					,MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			catch (Exception ex)
			{
				MsgBox.Show(this, "Failed to locate and import a valid licence file\r\n{0}".Fmt(ex.Message), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Navigate the 'purchase a licence' link</summary>
		private void HandlePurchaseLink(object sender, LinkLabelLinkClickedEventArgs e)
		{
			m_main.VisitWebSite();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Activation));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_company = new System.Windows.Forms.Label();
			this.m_lbl_name = new System.Windows.Forms.Label();
			this.m_lbl_valid = new System.Windows.Forms.Label();
			this.m_lbl_email = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_lbl_versions = new System.Windows.Forms.Label();
			this.m_btn_locate_licence = new System.Windows.Forms.Button();
			this.m_lbl_have_a_licence = new System.Windows.Forms.Label();
			this.m_lnk_purchase = new System.Windows.Forms.LinkLabel();
			this.m_tb_versions = new System.Windows.Forms.TextBox();
			this.m_tb_company = new System.Windows.Forms.TextBox();
			this.m_tb_email = new System.Windows.Forms.TextBox();
			this.m_tb_name = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(238, 274);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_company
			// 
			this.m_lbl_company.AutoSize = true;
			this.m_lbl_company.Location = new System.Drawing.Point(9, 137);
			this.m_lbl_company.Name = "m_lbl_company";
			this.m_lbl_company.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_company.TabIndex = 10;
			this.m_lbl_company.Text = "Company Name:";
			// 
			// m_lbl_name
			// 
			this.m_lbl_name.AutoSize = true;
			this.m_lbl_name.Location = new System.Drawing.Point(9, 47);
			this.m_lbl_name.Name = "m_lbl_name";
			this.m_lbl_name.Size = new System.Drawing.Size(137, 13);
			this.m_lbl_name.TabIndex = 8;
			this.m_lbl_name.Text = "This software is licensed to:";
			// 
			// m_lbl_valid
			// 
			this.m_lbl_valid.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lbl_valid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lbl_valid.Location = new System.Drawing.Point(12, 274);
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
			this.m_lbl_email.Location = new System.Drawing.Point(9, 91);
			this.m_lbl_email.Name = "m_lbl_email";
			this.m_lbl_email.Size = new System.Drawing.Size(76, 13);
			this.m_lbl_email.TabIndex = 17;
			this.m_lbl_email.Text = "Email Address:";
			// 
			// m_lbl_versions
			// 
			this.m_lbl_versions.AutoSize = true;
			this.m_lbl_versions.Location = new System.Drawing.Point(9, 185);
			this.m_lbl_versions.Name = "m_lbl_versions";
			this.m_lbl_versions.Size = new System.Drawing.Size(50, 13);
			this.m_lbl_versions.TabIndex = 21;
			this.m_lbl_versions.Text = "Versions:";
			// 
			// m_btn_locate_licence
			// 
			this.m_btn_locate_licence.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_locate_licence.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_locate_licence.Location = new System.Drawing.Point(197, 12);
			this.m_btn_locate_licence.Name = "m_btn_locate_licence";
			this.m_btn_locate_licence.Size = new System.Drawing.Size(119, 23);
			this.m_btn_locate_licence.TabIndex = 23;
			this.m_btn_locate_licence.Text = "Locate Licence File...";
			this.m_btn_locate_licence.UseVisualStyleBackColor = true;
			// 
			// m_lbl_have_a_licence
			// 
			this.m_lbl_have_a_licence.AutoSize = true;
			this.m_lbl_have_a_licence.Location = new System.Drawing.Point(9, 9);
			this.m_lbl_have_a_licence.Name = "m_lbl_have_a_licence";
			this.m_lbl_have_a_licence.Size = new System.Drawing.Size(182, 26);
			this.m_lbl_have_a_licence.TabIndex = 24;
			this.m_lbl_have_a_licence.Text = "Have a licence file for RyLogViewer?\r\nLocate it now to activate";
			// 
			// m_lbl_forgotten
			// 
			this.m_lnk_purchase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lnk_purchase.AutoSize = true;
			this.m_lnk_purchase.Location = new System.Drawing.Point(12, 241);
			this.m_lnk_purchase.Name = "m_lbl_forgotten";
			this.m_lnk_purchase.Size = new System.Drawing.Size(238, 13);
			this.m_lnk_purchase.TabIndex = 25;
			this.m_lnk_purchase.TabStop = true;
			this.m_lnk_purchase.Text = "To purchase a licence, please visit the web store";
			// 
			// m_tb_versions
			// 
			this.m_tb_versions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_versions.Location = new System.Drawing.Point(12, 201);
			this.m_tb_versions.Name = "m_tb_versions";
			this.m_tb_versions.ReadOnly = true;
			this.m_tb_versions.Size = new System.Drawing.Size(300, 20);
			this.m_tb_versions.TabIndex = 26;
			// 
			// m_tb_company
			// 
			this.m_tb_company.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_company.Location = new System.Drawing.Point(12, 153);
			this.m_tb_company.Name = "m_tb_company";
			this.m_tb_company.ReadOnly = true;
			this.m_tb_company.Size = new System.Drawing.Size(300, 20);
			this.m_tb_company.TabIndex = 27;
			// 
			// m_tb_email
			// 
			this.m_tb_email.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_email.Location = new System.Drawing.Point(12, 107);
			this.m_tb_email.Name = "m_tb_email";
			this.m_tb_email.ReadOnly = true;
			this.m_tb_email.Size = new System.Drawing.Size(300, 20);
			this.m_tb_email.TabIndex = 28;
			// 
			// m_tb_name
			// 
			this.m_tb_name.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_name.Location = new System.Drawing.Point(12, 63);
			this.m_tb_name.Name = "m_tb_name";
			this.m_tb_name.ReadOnly = true;
			this.m_tb_name.Size = new System.Drawing.Size(300, 20);
			this.m_tb_name.TabIndex = 29;
			// 
			// Activation
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(328, 305);
			this.Controls.Add(this.m_tb_name);
			this.Controls.Add(this.m_tb_email);
			this.Controls.Add(this.m_tb_company);
			this.Controls.Add(this.m_tb_versions);
			this.Controls.Add(this.m_lnk_purchase);
			this.Controls.Add(this.m_lbl_have_a_licence);
			this.Controls.Add(this.m_btn_locate_licence);
			this.Controls.Add(this.m_lbl_versions);
			this.Controls.Add(this.m_lbl_email);
			this.Controls.Add(this.m_lbl_valid);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_company);
			this.Controls.Add(this.m_lbl_name);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(344, 306);
			this.Name = "Activation";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Activation";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
