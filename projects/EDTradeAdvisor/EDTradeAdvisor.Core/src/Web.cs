﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace EDTradeAdvisor
{
	public class Web :IDisposable
	{
		// Notes:
		//  - This class handles interaction with online sources. It basically wraps HttpClient
		//    and provides functions for downloaded data files.

		public Web(CancellationToken shutdown)
		{
			Client = new HttpClient();
			Client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
			Shutdown = shutdown;
		}
		public void Dispose()
		{
			Client = Util.Dispose(Client);
		}

		/// <summary>True when one or more downloads are active</summary>
		public bool Downloading
		{
			get { return m_downloading != 0; }
			private set
			{
				var inc = value ? +1 : -1;
				m_downloading += inc;
				if (m_downloading == 0 || m_downloading - inc == 0)
					DownloadingChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		private int m_downloading;

		/// <summary>Raised whenever 'Downloading' changes</summary>
		public event EventHandler DownloadingChanged;

		/// <summary>A http client for web requests</summary>
		private HttpClient Client { get; set; }

		/// <summary>App shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>Pull 'filename' from EDDB. Returns true if the file was downloaded, and the output filepath</summary>
		public async Task<DownloadFileResult> DownloadFile(string file_url, bool only_if_newer = true)
		{
			var filename = Path_.FileName(file_url);
			var output_path = Path_.CombinePath(Settings.Instance.DataPath, filename);
			using (StatusStack.NewStatusMessage($"Downloading '{filename}'..."))
			{
				try
				{
					HttpRequestMessage req;
					HttpResponseMessage resp;

					// Request the head information about the target file
					Advisor.Log.Write(ELogLevel.Info, $"Checking size and timestamp of '{filename}'");
					req = new HttpRequestMessage(HttpMethod.Head, file_url);
					resp = await Client.SendAsync(req, Shutdown);
					if (!resp.IsSuccessStatusCode)
					{
						Advisor.Log.Write(ELogLevel.Error, $"Downloading information for '{filename}' failed: {resp.StatusCode} {resp.ReasonPhrase}");
						throw new HttpRequestException($"{resp.ReasonPhrase} ({resp.StatusCode})");
					}

					// Only download if the server version is newer.
					if (only_if_newer && Path_.FileExists(output_path))
					{
						var time_diff = new FileInfo(output_path).LastWriteTime - resp.Content.Headers.LastModified;
						if (time_diff > -Settings.Instance.DataAge)
						{
							Advisor.Log.Write(ELogLevel.Info, $"Local copy of '{filename}' is less than {Settings.Instance.DataAge.ToPrettyString(trailing_zeros:false)} older than the latest version");
							return new DownloadFileResult(file_url, output_path, false);
						}
					}

					// Get the download size (remember it might be compressed)
					var length = resp.Content.Headers.ContentLength;

					// The server version is newer, download the whole file
					Advisor.Log.Write(ELogLevel.Info, $"Downloading '{filename}' ({length} bytes)");
					using (Scope.Create(() => Downloading = true, () => Downloading = false))
					{
						// Make the web request
						req = new HttpRequestMessage(HttpMethod.Get, file_url);
						resp = await Client.SendAsync(req, Shutdown);
						if (!resp.IsSuccessStatusCode)
						{
							Advisor.Log.Write(ELogLevel.Error, $"Downloading '{filename}' failed: {resp.StatusCode} {resp.ReasonPhrase}");
							throw new HttpRequestException($"{resp.ReasonPhrase} ({resp.StatusCode})");
						}

						// Read the response content into a file
						using (var file = new FileStream(output_path, FileMode.Create, FileAccess.Write, FileShare.Read))
						{
							// Decompress if the content is compressed
							if (resp.Content.Headers.ContentEncoding.Any(x => x == "gzip"))
							{
								using (var content = await resp.Content.ReadAsStreamAsync())
								using (var gzip = new GZipStream(content, CompressionMode.Decompress))
									await gzip.CopyToAsync(file);
							}
							else
							{
								await resp.Content.CopyToAsync(file);
							}

							Advisor.Log.Write(ELogLevel.Info, $"Download complete '{filename}'");
							return new DownloadFileResult(file_url, output_path, true);
						}
					}
				}
				catch
				{
					Advisor.Log.Write(ELogLevel.Error, $"Data file '{filename}' was not available from {file_url}.");
					return new DownloadFileResult(file_url, output_path, false);
				}
			}
		}
		public struct DownloadFileResult
		{
			public DownloadFileResult(string file_url, string output_filepath, bool downloaded)
			{
				FileUrl = file_url;
				OutputFilepath = output_filepath;
				Downloaded = downloaded;
			}
			public string FileUrl;
			public string OutputFilepath;
			public bool Downloaded;
		}
	}
}