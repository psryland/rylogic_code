using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace RyLogViewer
{
	public class NewVersionForm :Form
	{
		#region UI Elements
		private LinkLabel m_link_website;
		private Label m_lbl_new_version;
		private LinkLabel m_link_download;
		private Button m_btn_ok;
		private Panel m_panel;
		private Label m_lbl_download;
		private Label m_lbl_website;
		private Label m_lbl_latest_version;
		private Label m_lbl_current_version;
		private PictureBox m_pic;
		private Label m_edit_latest_version;
		private Label m_edit_current_version;
		private Button m_btn_install;
		private ToolTip m_tt;
		#endregion

		public NewVersionForm(string current_version, string latest_version, string website_url, string download_url, IWebProxy proxy)
		{
			InitializeComponent();
			CurrentVersion = current_version;
			LatestVersion  = latest_version;
			WebsiteUrl     = website_url;
			DownloadUrl    = download_url;
			Proxy          = proxy;

			m_link_website.Click  += (s,a) => Process.Start(WebsiteUrl);
			m_link_download.Click += (s,a) => Process.Start(DownloadUrl);

			m_btn_install.Click += (s,a) => Install();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			m_edit_current_version.Text = CurrentVersion;
			m_edit_latest_version.Text  = LatestVersion;
			m_link_website.ToolTip(m_tt, WebsiteUrl);
			m_link_download.ToolTip(m_tt, DownloadUrl);
		}

		/// <summary>The current version number to display</summary>
		public string CurrentVersion { get; private set; }

		/// <summary>The latest available version number</summary>
		public string LatestVersion { get; private set; }

		/// <summary>The URL that clicking the website link will start</summary>
		public string WebsiteUrl { get; private set; }

		/// <summary>The URL that clicking the download link will start</summary>
		public string DownloadUrl { get; private set; }

		/// <summary>The web proxy to use</summary>
		public IWebProxy Proxy { get; private set; }

		/// <summary>Download and install the new version</summary>
		private void Install()
		{
			// Generate a temporary file for the installed
			var uri = new Uri(DownloadUrl);
			var installer_filepath = Path.Combine(Path.GetTempPath(), Path.GetFileName(DownloadUrl) ?? "RyLogViewerInstaller.exe");

			try
			{
				var dlg = new ProgressForm("Upgrading...", "Downloading latest version", Icon, ProgressBarStyle.Continuous, (form,args,cb) =>
				{
					// Download the installer for the new version
					using (var web = new WebClient{Proxy = Proxy})
					using (var got = new ManualResetEvent(false))
					{
						Exception error = null;
						web.DownloadFileCompleted += (s,a) =>
						{
							try
							{
								error = a.Error;
								if (a.Cancelled) form.CancelSignal.Set();
							}
							catch {}
							finally { got.Set(); }
						};
						
						web.DownloadFileAsync(uri, installer_filepath);
						
						// Wait for the download while polling the cancel pending flag
						while (!got.WaitOne(100))
							if (form.CancelPending)
								web.CancelAsync();
						
						if (form.CancelPending)
							throw new OperationCanceledException();
						if (error != null)
							throw new Exception("Failed to download installer for latest version", error);
					}
				});
				using (dlg)
					if (dlg.ShowDialog(this) != DialogResult.OK)
						return;

				// Run the installer
				Process.Start(installer_filepath);

				// Shutdown this app
				Application.Exit(new CancelEventArgs());
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Auto updated did not complete");
				MsgBox.Show(this, "Automatic updated failed\r\nReason: {0}".Fmt(ex.MessageFull()), "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewVersionForm));
			this.m_link_website = new System.Windows.Forms.LinkLabel();
			this.m_lbl_new_version = new System.Windows.Forms.Label();
			this.m_link_download = new System.Windows.Forms.LinkLabel();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_edit_latest_version = new System.Windows.Forms.Label();
			this.m_edit_current_version = new System.Windows.Forms.Label();
			this.m_pic = new System.Windows.Forms.PictureBox();
			this.m_lbl_download = new System.Windows.Forms.Label();
			this.m_lbl_website = new System.Windows.Forms.Label();
			this.m_lbl_latest_version = new System.Windows.Forms.Label();
			this.m_lbl_current_version = new System.Windows.Forms.Label();
			this.m_btn_install = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_panel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).BeginInit();
			this.SuspendLayout();
			// 
			// m_link_website
			// 
			this.m_link_website.AutoSize = true;
			this.m_link_website.Location = new System.Drawing.Point(185, 87);
			this.m_link_website.Name = "m_link_website";
			this.m_link_website.Size = new System.Drawing.Size(126, 13);
			this.m_link_website.TabIndex = 0;
			this.m_link_website.TabStop = true;
			this.m_link_website.Text = "Visit website for more info";
			// 
			// m_lbl_new_version
			// 
			this.m_lbl_new_version.AutoSize = true;
			this.m_lbl_new_version.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_new_version.Location = new System.Drawing.Point(99, 21);
			this.m_lbl_new_version.Name = "m_lbl_new_version";
			this.m_lbl_new_version.Size = new System.Drawing.Size(159, 13);
			this.m_lbl_new_version.TabIndex = 1;
			this.m_lbl_new_version.Text = "A new version is available!";
			// 
			// m_link_download
			// 
			this.m_link_download.AutoSize = true;
			this.m_link_download.Location = new System.Drawing.Point(186, 109);
			this.m_link_download.Name = "m_link_download";
			this.m_link_download.Size = new System.Drawing.Size(138, 13);
			this.m_link_download.TabIndex = 2;
			this.m_link_download.TabStop = true;
			this.m_link_download.Text = "Download the latest version";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(328, 161);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "Later";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_panel
			// 
			this.m_panel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel.BackColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_panel.Controls.Add(this.m_edit_latest_version);
			this.m_panel.Controls.Add(this.m_edit_current_version);
			this.m_panel.Controls.Add(this.m_pic);
			this.m_panel.Controls.Add(this.m_lbl_download);
			this.m_panel.Controls.Add(this.m_lbl_website);
			this.m_panel.Controls.Add(this.m_lbl_latest_version);
			this.m_panel.Controls.Add(this.m_lbl_current_version);
			this.m_panel.Controls.Add(this.m_lbl_new_version);
			this.m_panel.Controls.Add(this.m_link_website);
			this.m_panel.Controls.Add(this.m_link_download);
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(417, 150);
			this.m_panel.TabIndex = 4;
			// 
			// m_edit_latest_version
			// 
			this.m_edit_latest_version.AutoSize = true;
			this.m_edit_latest_version.Location = new System.Drawing.Point(185, 65);
			this.m_edit_latest_version.Name = "m_edit_latest_version";
			this.m_edit_latest_version.Size = new System.Drawing.Size(85, 13);
			this.m_edit_latest_version.TabIndex = 9;
			this.m_edit_latest_version.Text = "Current Version: ";
			// 
			// m_edit_current_version
			// 
			this.m_edit_current_version.AutoSize = true;
			this.m_edit_current_version.Location = new System.Drawing.Point(185, 43);
			this.m_edit_current_version.Name = "m_edit_current_version";
			this.m_edit_current_version.Size = new System.Drawing.Size(85, 13);
			this.m_edit_current_version.TabIndex = 8;
			this.m_edit_current_version.Text = "Current Version: ";
			// 
			// m_pic
			// 
			this.m_pic.BackgroundImage = global::RyLogViewer.Properties.Resources.important;
			this.m_pic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.m_pic.ErrorImage = null;
			this.m_pic.InitialImage = null;
			this.m_pic.Location = new System.Drawing.Point(12, 21);
			this.m_pic.Name = "m_pic";
			this.m_pic.Size = new System.Drawing.Size(71, 63);
			this.m_pic.TabIndex = 7;
			this.m_pic.TabStop = false;
			// 
			// m_lbl_download
			// 
			this.m_lbl_download.AutoSize = true;
			this.m_lbl_download.Location = new System.Drawing.Point(126, 109);
			this.m_lbl_download.Name = "m_lbl_download";
			this.m_lbl_download.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_download.TabIndex = 6;
			this.m_lbl_download.Text = "Download:";
			// 
			// m_lbl_website
			// 
			this.m_lbl_website.AutoSize = true;
			this.m_lbl_website.Location = new System.Drawing.Point(135, 87);
			this.m_lbl_website.Name = "m_lbl_website";
			this.m_lbl_website.Size = new System.Drawing.Size(49, 13);
			this.m_lbl_website.TabIndex = 5;
			this.m_lbl_website.Text = "Website:";
			// 
			// m_lbl_latest_version
			// 
			this.m_lbl_latest_version.AutoSize = true;
			this.m_lbl_latest_version.Location = new System.Drawing.Point(104, 65);
			this.m_lbl_latest_version.Name = "m_lbl_latest_version";
			this.m_lbl_latest_version.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_latest_version.TabIndex = 4;
			this.m_lbl_latest_version.Text = "Latest Version: ";
			// 
			// m_lbl_current_version
			// 
			this.m_lbl_current_version.AutoSize = true;
			this.m_lbl_current_version.Location = new System.Drawing.Point(99, 43);
			this.m_lbl_current_version.Name = "m_lbl_current_version";
			this.m_lbl_current_version.Size = new System.Drawing.Size(85, 13);
			this.m_lbl_current_version.TabIndex = 3;
			this.m_lbl_current_version.Text = "Current Version: ";
			// 
			// m_btn_install
			// 
			this.m_btn_install.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.m_btn_install.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_install.Location = new System.Drawing.Point(247, 161);
			this.m_btn_install.Name = "m_btn_install";
			this.m_btn_install.Size = new System.Drawing.Size(75, 23);
			this.m_btn_install.TabIndex = 0;
			this.m_btn_install.Text = "Install Now";
			this.m_btn_install.UseVisualStyleBackColor = true;
			// 
			// NewVersionForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(415, 196);
			this.Controls.Add(this.m_btn_install);
			this.Controls.Add(this.m_panel);
			this.Controls.Add(this.m_btn_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "NewVersionForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "New Version Available";
			this.m_panel.ResumeLayout(false);
			this.m_panel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_pic)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
