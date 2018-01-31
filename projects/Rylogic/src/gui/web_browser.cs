using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	/// <remarks>
	/// The standard win forms WebBrowser control is basically unusable except for
	/// standard web page navigation. This replacement is intended for "virtual-mode"
	/// use where content is supplied on demand.
	/// </remarks>

	///<summary>Web browser that supports local content as well as web pages</summary>
	public class WebBrowser :Panel
	{
		public static readonly Uri AboutBlankUrl = new Uri("about:blank");

		/// <summary>The web browser used to do the rendering</summary>
		private System.Windows.Forms.WebBrowser m_wb;

		public WebBrowser()
		{
			m_wb = new System.Windows.Forms.WebBrowser{Dock = DockStyle.Fill};
			Controls.Add(m_wb);

			// Set up the URL history
			m_url_history = new BindingListEx<Visit>();
			UrlHistory = new BindingSource<Visit>{DataSource = m_url_history};
			UrlHistory.PositionChanged += (s,a) =>
			{
				OnCanGoBackChanged(EventArgs.Empty);
				OnCanGoForwardChanged(EventArgs.Empty);
			};

			// Set up handlers on the web browser
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

		/// <summary>The list if visited URLs</summary>
		[Browsable(false)]
		public BindingSource<Visit> UrlHistory { get; private set; }
		private BindingListEx<Visit> m_url_history;
		public class Visit
		{
			public Visit(Uri url = null, string content = null) { Url = url; Content = content; }
			public Visit(string url = null, string content = null) :this(new Uri(url), content) {}
			public Uri Url { get; set; }
			public string Content { get; set; }
			public override string ToString() { return Url.ToString(); }
		}

		/// <summary>Navigate to the given URL</summary>
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
				m_url_history.RemoveToEnd(UrlHistory.Position);

			// Navigate to the given URL
			m_wb.Navigate(to);
		}

		/// <summary>Reload the current visit</summary>
		public void Reload()
		{
			// If there's no URL history, load 'blank'
			if (UrlHistory.Current == null)
				Navigate(AboutBlankUrl, true);
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
			m_fwd_or_back = true;
			UrlHistory.Position -= 2;
			Navigate(UrlHistory[UrlHistory.Position + 1].Url, false); // Don't use GoForward because it's virtual
			return true;
		}

		/// <summary>Load the next content</summary>
		public virtual bool GoForward()
		{
			if (!CanGoForward) return false;
			m_fwd_or_back = true;
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
			// Ignore recursive calls (and navigation events that happen after
			// this method returns but are due to the assignment of DocumentStream)
			if (m_rdr_content || args.Url == AboutBlankUrl) return;
			using (Scope.Create(() => m_rdr_content = true, () => m_rdr_content = false))
			{
				var idx = UrlHistory.Position + 1;

				// Add the selected URL to the history
				if (idx == UrlHistory.Count)
				{
					// Resolve the URL to content
					var a = new ResolveContentEventArgs(args.Url);
					OnResolveContent(a);

					// Add to the history
					UrlHistory.Add(new Visit(a.Url, a.Content));
				}
				else if (!m_fwd_or_back || args.Url != UrlHistory[idx].Url)
				{
					// Resolve the URL to content
					var a = new ResolveContentEventArgs(args.Url);
					OnResolveContent(a);

					// Set the current visit and remove the future history
					UrlHistory[idx] = new Visit(a.Url, a.Content);
					m_url_history.RemoveToEnd(idx + 1);
				}
	
				UrlHistory.Position = idx;

				// Display the content
				var content = UrlHistory.Current.Content;
				if (content != null)
				{
					// Setting DocumentStream is considered a navigation (to about:blank)
					// This may or may not result in a recursive call to this method
					m_wb.DocumentStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
					args.Cancel = true;
				}
				m_fwd_or_back = false;
			}

			System.Diagnostics.Debug.WriteLine($"{UrlHistory.Position} : {string.Join("->", UrlHistory.Select(x => x.Url.ToString()))}");
		}
		private bool m_rdr_content;
		private bool m_fwd_or_back;

		/// <summary>Raised to resolve the content for a given URL</summary>
		public event EventHandler<ResolveContentEventArgs> ResolveContent;
		public class ResolveContentEventArgs :EventArgs
		{
			public ResolveContentEventArgs(Uri url)
			{
				Url = url;
				Content = null;
			}

			/// <summary>The URL for which content is required</summary>
			public Uri Url { get; private set; }

			/// <summary>The html content for the given URL. Leave as null for normal web page navigation</summary>
			public string Content { get; set; }
		}
		protected virtual void OnResolveContent(ResolveContentEventArgs args)
		{
			ResolveContent.Raise(this, args);
		}
	}
}
