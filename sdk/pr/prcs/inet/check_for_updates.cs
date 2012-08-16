using System;
using System.Net;
using System.Threading;
using System.Xml.Linq;

namespace pr.inet
{
	public static partial class INet
	{
		/// <summary>The result of a 'CheckForUpdate' call</summary>
		public class CheckForUpdateResult
		{
			/// <summary>The version number of the latest version according to the remote data</summary>
			public string Version;
			
			/// <summary>The location to download the latest version from</summary>
			public string DownloadURL;

			/// <summary>The location containing info about the latest version</summary>
			public string InfoURL;
		}

		/// <summary>Data related to a specific async check for updates call</summary>
		private class CheckForUpdateAsyncData :IAsyncResult
		{
			private readonly CheckForUpdateResult m_result = new CheckForUpdateResult();
			private ManualResetEvent m_wait;
			private int m_cancel;
			
			/// <summary>Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</summary>
			public WaitHandle AsyncWaitHandle
			{
				get { return m_wait ?? (m_wait = new ManualResetEvent(false)); }
			}

			/// <summary>Gets a value that indicates whether the asynchronous operation has completed.</summary>
			public bool IsCompleted
			{
				get { return AsyncWaitHandle.WaitOne(0); }
			}

			/// <summary>Gets a user-defined object that qualifies or contains information about an asynchronous operation.</summary>
			public object AsyncState
			{
				get { return null; }
			}

			/// <summary>Gets a value that indicates whether the asynchronous operation completed synchronously.</summary>
			public bool CompletedSynchronously
			{
				get { return false; }
			}
			
			/// <summary>Signals cancel to the check for updates</summary>
			internal bool CancelPending
			{
				get { return m_cancel != 0; }
				set { if (value) Interlocked.CompareExchange(ref m_cancel, 1, 0); }
			}

			/// <summary>The result of the async check for updates</summary>
			internal CheckForUpdateResult Result { get { return m_result; } }

			/// <summary>Any error that occurred during the update check</summary>
			internal Exception Error;
		}

		/// <summary>Begin an asynchronous check for update. Follows the standard Begin/End IAsyncResult pattern</summary>
		/// <param name="identifier">The identifier for the application whose version is being checked</param>
		/// <param name="url">The URL of the version xml file</param>
		/// <param name="callback">A callback called (on success or failure) with the results of the version check</param>
		/// <param name="proxy">A optional proxy server</param>
		public static IAsyncResult BeginCheckForUpdate(string identifier, string url, Action<IAsyncResult> callback, IWebProxy proxy)
		{
			CheckForUpdateAsyncData async = new CheckForUpdateAsyncData();
			
			// Do the version check as a background task
			WaitCallback version_check = x =>
			{
				try
				{
					// Check the update url is valid
					Uri uri = new Uri(url);
					if (!Uri.IsWellFormedUriString(uri.AbsoluteUri, UriKind.Absolute))
						throw new ArgumentException("Update URL is invalid");
					
					// Create a web client and grab the version xml data
					string latest_version_xml = "";
					Exception error = null;
					using (WebClient web = new WebClient{Proxy = proxy})
					using (ManualResetEvent got = new ManualResetEvent(false))
					{
						// ReSharper disable AccessToDisposedClosure
						web.DownloadStringCompleted += (s,a) =>
						{
							try
							{
								error = a.Error;
								if (!a.Cancelled && a.Error == null)
									latest_version_xml = a.Result;
							}
							catch { }
							finally { got.Set(); }
						};
						web.DownloadStringAsync(uri);
						// ReSharper restore AccessToDisposedClosure
						
						// Wait for the download while polling the cancel pending flag
						while (!got.WaitOne(100))
							if (async.CancelPending)
								web.CancelAsync();
						
						if (async.CancelPending)
							throw new OperationCanceledException();
						if (error != null)
							throw new Exception("Failed to read latest version info", error);
					}
					
					// Load the version info string
					XElement root = XDocument.Parse(latest_version_xml).Root;
					XElement info = root != null ? root.Element(identifier) : null;
					if (info != null)
					{
						XElement elem;
						if ((elem = info.Element("version" )) != null) async.Result.Version     = elem.Value;
						if ((elem = info.Element("dl_url"  )) != null) async.Result.DownloadURL = elem.Value;
						if ((elem = info.Element("info_url")) != null) async.Result.InfoURL     = elem.Value;
					}
				}
				catch (Exception ex) { async.Error = ex; }
				((ManualResetEvent)async.AsyncWaitHandle).Set();
				if (callback != null) callback(async);
			};

			// Execute the async operation
			ThreadPool.QueueUserWorkItem(version_check, async);
			return async;
		}
		public static IAsyncResult BeginCheckForUpdate(string identifier, string url, Action<IAsyncResult> callback)
		{
			return BeginCheckForUpdate(identifier, url, callback, WebRequest.DefaultWebProxy);
		}
		
		/// <summary>Signal to the async check for updates that is should cancel</summary>
		public static IAsyncResult CancelCheckForUpdate(IAsyncResult ar)
		{
			CheckForUpdateAsyncData async = (CheckForUpdateAsyncData)ar;
			async.CancelPending = true;
			return async;
		}

		/// <summary>Complete an async check for updates call</summary>
		public static CheckForUpdateResult EndCheckForUpdate(IAsyncResult ar)
		{
			CheckForUpdateAsyncData async = (CheckForUpdateAsyncData)ar;
			try
			{
				ar.AsyncWaitHandle.WaitOne();
				if (async.Error != null) throw async.Error;
				return async.Result;
			}
			finally
			{
				async.AsyncWaitHandle.Close();
			}
		}
	}
}
