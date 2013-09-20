using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace RyLogViewer
{
	public partial class NewVersionForm :Form
	{
		private readonly ToolTip m_tt;

		/// <summary>The current version number to display</summary>
		public string CurrentVersion { get; private set; }

		/// <summary>The latest available version number</summary>
		public string LatestVersion { get; private set; }

		/// <summary>The url that clicking the website link will start</summary>
		public string WebsiteUrl { get; private set; }

		/// <summary>The Url that clicking the download link will start</summary>
		public string DownloadUrl { get; private set; }

		/// <summary>The web proxy to use</summary>
		public IWebProxy Proxy { get; private set; }

		public NewVersionForm(string current_version, string latest_version, string website_url, string download_url, IWebProxy proxy)
		{
			InitializeComponent();
			CurrentVersion = current_version;
			LatestVersion  = latest_version;
			WebsiteUrl     = website_url;
			DownloadUrl    = download_url;
			Proxy          = proxy;

			m_tt = new ToolTip();
			m_link_website.Click  += (s,a) => Process.Start(WebsiteUrl);
			m_link_download.Click += (s,a) => Process.Start(DownloadUrl);

			m_btn_install.Click += (s,a) => Install();

			Shown += (s,a)=>
				{
					m_edit_current_version.Text = CurrentVersion;
					m_edit_latest_version.Text  = LatestVersion;
					m_link_website.ToolTip(m_tt, WebsiteUrl);
					m_link_download.ToolTip(m_tt, DownloadUrl);
				};
			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}

		/// <summary>Download and install the new version</summary>
		private void Install()
		{
			// Generate a temporary file for the installed
			var uri = new Uri(DownloadUrl);
			var installer_filepath = Path.Combine(Path.GetTempPath(), Path.GetFileName(DownloadUrl) ?? "RylogViewerSetup0.exe");

			var dlg = new ProgressForm("Upgrading...", "Downloading latest version", Icon, ProgressBarStyle.Continuous, (form,args,cb) =>
				{
					// Download the installer for the new version
					using (var web = new WebClient{Proxy = Proxy})
					using (var got = new ManualResetEvent(false))
					{
						Exception error = null;
						web.DownloadFileCompleted += (s,a) =>
						{
							// ReSharper disable AccessToDisposedClosure
							try
							{
								error = a.Error;
								if (a.Cancelled) form.CancelSignal.Set();
							}
							catch {}
							finally { got.Set(); }
							// ReSharper restore AccessToDisposedClosure
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
			try
			{
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
	}
}
