using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;


namespace Poloniex.API
{
	public class PoloniexApiPublic :IDisposable
	{
		public PoloniexApiPublic(CancellationToken cancel_token, string base_address = "https://poloniex.com/", string wss_address = "wss://api.poloniex.com/")
		{
			m_cancel_token      = cancel_token;
			UrlBaseAddress      = base_address;
			m_client            = HttpClientFactory.Create();
			m_json              = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			m_request_sw        = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");


			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}



		#region REST API Functions


		private Stopwatch m_request_sw;
		private long m_last_request_ms;

		#endregion
	}


}
