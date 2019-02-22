using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Rylogic
{
	public static class Http_
	{
		/// <summary>Cancel-able web request get response</summary>
		public static async Task<HttpWebResponse> GetResponseAsync(this HttpWebRequest request, CancellationToken cancel)
		{
			using (cancel.Register(() => request.Abort(), useSynchronizationContext: false))
			{
				try
				{
					var response = await request.GetResponseAsync();
					return (HttpWebResponse)response;
				}
				catch (WebException ex)
				{
					// WebException is thrown when request.Abort() is called, but there may be
					// many other reasons, propagate the WebException to the caller correctly
					if (cancel.IsCancellationRequested)
					{
						// the WebException will be available as Exception.InnerException
						throw new OperationCanceledException(ex.Message, ex, cancel);
					}

					// cancellation hasn't been requested, rethrow the original WebException
					throw;
				}
			}
		}
	}
}
