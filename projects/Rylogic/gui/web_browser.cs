using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <remarks>
	/// The standard winforms WebBrowser control is basically unusable except for
	/// standard web page navigation. This replacement is intended for "virtual-mode"
	/// use where content is supplied on demand.
	/// </remarks>

	///<summary>Web browser that supports local content as well as web pages</summary>
	public class WebBrowser :Panel
	{
		public static readonly Uri AboutBlankUrl = new Uri("about:blank");

		/// <summary>The web browser used to do the rendering</summary>
		private System.Windows.Forms.WebBrowser m_wb;

		/// <summary>The index of the url in the history that is to be shown</summary>
		private int m_url_index;

		public WebBrowser()
		{
			m_url_index = -1;

			m_wb = new System.Windows.Forms.WebBrowser{Dock = DockStyle.Fill};
			Controls.Add(m_wb);

			// Setup the URL history
			m_url_history = new BindingListEx<Visit>();
			UrlHistory = new BindingSource<Visit>{DataSource = m_url_history};
			UrlHistory.PositionChanged += (s,a) =>
				{
					OnCanGoBackChanged(EventArgs.Empty);
					OnCanGoForwardChanged(EventArgs.Empty);
				};

			// Setup handlers on the web browser
			m_wb.Navigating += HandleNavigating;
			m_wb.StatusTextChanged += (s,a) => StatusTextChanged.Raise(this, a);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref m_wb);
			base.Dispose(disposing);
		}

		/// <summary>Get/Set whether link-click navigation is allowed</summary>
		public bool AllowNavigation
		{
			get { return m_wb.AllowNavigation; }
			set { m_wb.AllowNavigation = value; }
		}

		/// <summary>The list if visited Urls</summary>
		[Browsable(false)]
		public BindingSource<Visit> UrlHistory { get; private set; }
		private BindingListEx<Visit> m_url_history;
		public class Visit
		{
			public Visit(Uri url = null, string content = null) { Url = url; Content = content; }
			public Uri Url { get; set; }
			public string Content { get; set; }
			public override string ToString() { return Url.ToString(); }
		}

		/// <summary>Navigate to the given url</summary>
		public void Navigate(string url)
		{
			Navigate(new Uri(url));
		}
		public void Navigate(Uri to)
		{
			Navigate(to, true);
		}
		protected void Navigate(Uri to, bool erase_forward)
		{
			// Clear the forward history
			if (erase_forward && UrlHistory.Position >= 0)
			{
				m_url_history.RemoveToEnd(UrlHistory.Position);
				m_url_index = UrlHistory.Count - 1;
			}

			// Navigate to the given url
			m_wb.Navigate(to);
		}

		/// <summary>Reload the current visit</summary>
		public void Reload()
		{
			if (UrlHistory.Current == null)
				Navigate(AboutBlankUrl);
			else
				Navigate(UrlHistory.Current.Url, true);
		}

		/// <summary>Raised when the status text changes</summary>
		[Browsable(false)]
		public event EventHandler StatusTextChanged;

		/// <summary>The browser status</summary>
		[Browsable(false)]
		public string StatusText
		{
			get { return m_wb.StatusText; }
		}

		/// <summary>Load the previous content</summary>
		public virtual bool GoBack()
		{
			if (!CanGoBack) return false;
			m_url_index = UrlHistory.Position - 2;
			Navigate(UrlHistory[UrlHistory.Position - 1].Url, false);
			return true;
		}

		/// <summary>Load the next content</summary>
		public virtual bool GoForward()
		{
			if (!CanGoForward) return false;
			m_url_index = UrlHistory.Position;
			Navigate(UrlHistory[UrlHistory.Position + 1].Url, false);
			return true;
		}

		/// <summary>True if we can navigate backwards</summary>
		public bool CanGoBack
		{
			get { return UrlHistory.Position > 0; }
		}

		/// <summary>True if we can navigate forwards</summary>
		public bool CanGoForward
		{
			get { return UrlHistory.Position < UrlHistory.Count - 1; }
		}

		/// <summary>Raised when the CanGoBack property might have a different value</summary>
		public event EventHandler CanGoBackChanged;
		protected virtual void OnCanGoBackChanged(EventArgs e)
		{
			CanGoBackChanged.Raise(this, e);
		}

		/// <summary>Raised when the CanGoForward property might have a different value</summary>
		public event EventHandler CanGoForwardChanged;
		protected virtual void OnCanGoForwardChanged(EventArgs e)
		{
			CanGoForwardChanged.Raise(this, e);
		}

		/// <summary>Handle navigation events</summary>
		private void HandleNavigating(object sender, WebBrowserNavigatingEventArgs args)
		{
			// Each navigation advances the url index. Internal redirects pre-decrement this.
			if (++m_url_index == UrlHistory.Position)
				return;

			// Add the selected url to the history
			if (m_url_index >= UrlHistory.Count)
			{
				if (m_url_index > UrlHistory.Count)
					throw new Exception("url index should be in the range [0,history.Count]");

				// Resolve the Url to content
				var a = new ResolveContentEventArgs(args.Url);
				OnResolveContent(a);
				UrlHistory.Add(new Visit(a.Url, a.Content));
			}
			else if (args.Url != UrlHistory[m_url_index].Url)
			{
				// Resolve the Url to content
				var a = new ResolveContentEventArgs(args.Url);
				OnResolveContent(a);

				// Remove the future history and set the current visit
				m_url_history.RemoveToEnd(m_url_index + 1);
				UrlHistory[m_url_index].Url = a.Url;
				UrlHistory[m_url_index].Content = a.Content;
			}

			// Display the content
			UrlHistory.Position = m_url_index;
			var content = UrlHistory.Current.Content;
			if (content != null)
			{
				--m_url_index;
				m_wb.DocumentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
				args.Cancel = true;
			}
		}

		/// <summary>Raised to resolve the content for a given url</summary>
		public event EventHandler<ResolveContentEventArgs> ResolveContent;
		public class ResolveContentEventArgs :EventArgs
		{
			public ResolveContentEventArgs(Uri url)
			{
				Url = url;
				Content = null;
			}

			/// <summary>The url for which content is required</summary>
			public Uri Url { get; private set; }

			/// <summary>The html content for the given url. Leave as null for normal web page navigation</summary>
			public string Content { get; set; }
		}
		protected virtual void OnResolveContent(ResolveContentEventArgs args)
		{
			ResolveContent.Raise(this, args);
		}
	}
}
