using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rylogic.Utility;

namespace SolarHotWater.Common
{
	public sealed class FroniusAPI :IDisposable
	{
		public FroniusAPI(string ip, CancellationToken shutdown)
		{
			Url = $"http://{ip}/";
			Shutdown = shutdown;
			Client = new HttpClient();
		}
		public void Dispose()
		{
			Client = null!;
		}

		/// <summary></summary>
		private string Url { get; }

		/// <summary>Shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>The Http client for REST requests</summary>
		private HttpClient Client
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					Util.Dispose(ref field!);
				}
				field = value;
				if (field != null)
				{
					field.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		} = null!;

		/// <summary>Get the real time data from the solar inverter</summary>
		public async Task<SolarData> RealTimeData(CancellationToken? cancel = null)
		{
			var url = $"{Url}solar_api/v1/GetInverterRealtimeData.cgi?Scope=System";
			var req = new HttpRequestMessage(HttpMethod.Get, url);

			// Submit the request
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jtok = JToken.Parse(reply);

			return jtok.ToObject<SolarData>() ?? throw new Exception("SolarData not available");
		}
	}
}
