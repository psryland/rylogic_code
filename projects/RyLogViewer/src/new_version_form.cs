using System;
using System.Diagnostics;
using System.Windows.Forms;
using pr.util;

namespace RyLogViewer
{
	public partial class NewVersionForm :Form
	{
		private readonly ToolTip m_tt;
		/// <summary>The current version number to display</summary>
		public string CurrentVersion { get; set; }
		
		/// <summary>The latest available version number</summary>
		public string LatestVersion { get; set; }
		
		/// <summary>The url that clicking the website link will start</summary>
		public string WebsiteUrl { get; set; }
		
		/// <summary>The Url that clicking the download link will start</summary>
		public string DownloadUrl { get; set; }
		
		public NewVersionForm()
		{
			InitializeComponent();
			m_tt = new ToolTip();
			m_link_website.Click  += (s,a) => Process.Start(WebsiteUrl);
			m_link_download.Click += (s,a) => Process.Start(DownloadUrl);
			
			Shown += (s,a)=>
				{
					m_edit_current_version.Text = CurrentVersion;
					m_edit_latest_version.Text  = LatestVersion;
					m_link_website.ToolTip(m_tt, WebsiteUrl);
					m_link_download.ToolTip(m_tt, DownloadUrl);
				};
		}
	}
}
