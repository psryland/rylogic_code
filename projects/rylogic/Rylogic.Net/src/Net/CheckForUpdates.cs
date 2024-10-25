using System;
using System.Net;
using System.Threading;

namespace Rylogic.INet
{
	public static partial class Net
	{
		/// <summary>Begin an asynchronous check for update. Follows the standard Begin/End IAsyncResult pattern</summary>
		/// <param name="identifier">The identifier for the application whose version is being checked</param>
		/// <param name="url">The URL of the version xml file</param>
		/// <param name="callback">A callback called (on success or failure) with the results of the version check</param>
		/// <param name="proxy">An optional proxy server</param>
		public static IAsyncResult BeginCheckForUpdate(string identifier, string url, Action<IAsyncResult> callback, IWebProxy? proxy)
		{
			throw new NotImplementedException();
#if false
			// Do the version check as a background task
			void version_check(object? x)
			{
				var async = (CheckForUpdateAsyncData)x!;
				try
				{
					// Check the update URL is valid
					var uri = new Uri(url);
					if (!Uri.IsWellFormedUriString(uri.AbsoluteUri, UriKind.Absolute))
						throw new ArgumentException("Update URL is invalid");

					// Create a web client and grab the version xml data
					var latest_version_xml = "";
					var error = (Exception?)null;
					using (var web = new WebClient { Proxy = proxy })
					using (var got = new ManualResetEvent(false))
					{
						web.DownloadStringCompleted += (s, a) =>
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

						// Wait for the download while polling the cancel pending flag
						while (!got.WaitOne(100))
							if (async.CancelPending)
								web.CancelAsync();

						if (async.CancelPending)
							throw new OperationCanceledException();
						if (error != null)
							throw new Exception("Failed to read latest version info", error);
					}

					// Check for a non-empty result
					if (string.IsNullOrEmpty(latest_version_xml))
						throw new Exception("No update information returned from server");

					// Load the version info string
					var root = XDocument.Parse(latest_version_xml).Root;
					var info = root != null ? root.Element(identifier) : null;
					if (info != null)
					{
						XElement elem;
						if ((elem = info.Element("version")) != null) async.Result.Version = elem.Value;
						if ((elem = info.Element("dl_url")) != null) async.Result.DownloadURL = elem.Value;
						if ((elem = info.Element("info_url")) != null) async.Result.InfoURL = elem.Value;
					}
				}
				catch (Exception ex) { async.Error = ex; }
				((ManualResetEvent)async.AsyncWaitHandle).Set();
				callback?.Invoke(async);
			}

			// Execute the async operation
			var async_data = new CheckForUpdateAsyncData();
			ThreadPool.QueueUserWorkItem(version_check, async_data);
			return async_data;
#endif
		}
		public static IAsyncResult BeginCheckForUpdate(string identifier, string url, Action<IAsyncResult> callback)
		{
			return BeginCheckForUpdate(identifier, url, callback, WebRequest.DefaultWebProxy);
		}

		/// <summary>Signal to the async check for updates that is should cancel</summary>
		public static IAsyncResult CancelCheckForUpdate(IAsyncResult ar)
		{
			var async = (CheckForUpdateAsyncData)ar;
			async.CancelPending = true;
			return async;
		}

		/// <summary>Complete an async check for updates call</summary>
		public static CheckForUpdateResult EndCheckForUpdate(IAsyncResult ar)
		{
			var async = (CheckForUpdateAsyncData)ar;
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

		/// <summary>The result of a 'CheckForUpdate' call</summary>
		public class CheckForUpdateResult
		{
			public CheckForUpdateResult()
			{
				Version = string.Empty;
				DownloadURL = string.Empty;
				InfoURL = string.Empty;
			}

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
			/// <summary>Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.</summary>
			public WaitHandle AsyncWaitHandle => m_wait ?? (m_wait = new ManualResetEvent(false));
			private ManualResetEvent? m_wait;

			/// <summary>Gets a value that indicates whether the asynchronous operation has completed.</summary>
			public bool IsCompleted => AsyncWaitHandle.WaitOne(0);

			/// <summary>Gets a user-defined object that qualifies or contains information about an asynchronous operation.</summary>
			public object? AsyncState => null;

			/// <summary>Gets a value that indicates whether the asynchronous operation completed synchronously.</summary>
			public bool CompletedSynchronously => false;

			/// <summary>Signals cancel to the check for updates</summary>
			internal bool CancelPending
			{
				get => m_cancel != 0;
				set { if (value) Interlocked.CompareExchange(ref m_cancel, 1, 0); }
			}
			private int m_cancel;

			/// <summary>The result of the async check for updates</summary>
			internal CheckForUpdateResult Result { get; } = new CheckForUpdateResult();

			/// <summary>Any error that occurred during the update check</summary>
			internal Exception? Error { get; set; }
		}
	}
}
