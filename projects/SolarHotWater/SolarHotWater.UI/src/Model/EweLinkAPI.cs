using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Printing;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class EweLinkAPI :IDisposable
	{
		// Notes:
		//  - REST interface to the eWeLink API
		private const string APP_ID = "YzfeftUVcZ6twZw1OoVKPRFYTrGEg01Q";
		private const string APP_SECRET = "4G91qSoboqYO4Y0XJ0LPPKIsq8reHdfa";

		public EweLinkAPI(CancellationToken shutdown)
		{
			Shutdown = shutdown;
			Client = new HttpClient();
			Hasher = new HMACSHA256(Encoding.UTF8.GetBytes(APP_SECRET));
		}
		public void Dispose()
		{
			Client = null!;
		}

		/// <summary>EweLink REST API</summary>
		private string Url => "https://us-api.coolkit.cc:8080/";

		/// <summary>Shutdown token</summary>
		private CancellationToken Shutdown { get; }

		/// <summary>The Http client for REST requests</summary>
		private HttpClient Client
		{
			get => m_client;
			set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					Util.Dispose(ref m_client!);
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.BaseAddress = new Uri(Url);
					m_client.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		}
		private HttpClient m_client = null!;

		/// <summary></summary>
		private HMACSHA256 Hasher { get; }

		/// <summary>EweLink credentials</summary>
		public EweCred? Cred { get; set; }

		/// <summary>Get the login credentials</summary>
		public async Task Login(string username, string password, CancellationToken? cancel = null)
		{
			var parms = new Params
			{
				{ "appid", APP_ID },
				{ "email", username },
				{ "phoneNumber", "" },
				{ "password", password },
				{ "ts", DateTimeOffset.UtcNow.ToUnixTimeSeconds() },
				{ "version", 8 },
				{ "nonce", Nonce },
			};

			var body = JObject.FromObject(parms).ToString();
			var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(body));
			var sign = Convert.ToBase64String(hash);

			var url = $"{Url}api/user/login";
			var req = new HttpRequestMessage(HttpMethod.Post, url);
			req.Headers.Authorization = new AuthenticationHeaderValue("Sign", sign);
			req.Content = new StringContent(body);

			// Submit the request
			var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;
			var response = await Client.SendAsync(req, cancel_token);
			var reply = await response.Content.ReadAsStringAsync();
			var jtok = JToken.Parse(reply);

			Cred = jtok.ToObject<EweCred>() ?? throw new Exception("Credentials not available");
		}

		/// <summary>Return a list of the available devices</summary>
		public async Task<EweDevice[]> Devices(EweCred cred, CancellationToken? cancel = null)
		{
			await Task.Delay(1);
			return Array.Empty<EweDevice>();
		}

		/// <summary>Helper for generating a 'nonce'</summary>
		private static string Nonce
		{
			get
			{
				lock (m_nonce_lock)
				{
					var nonce = DateTimeOffset.UtcNow.Ticks;
					m_nonce = Math.Max(m_nonce + 1, nonce);
					return m_nonce.ToString();
				}
			}
		}
		private static long m_nonce = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
		private static object m_nonce_lock = new object();

		/// <summary></summary>
		public class Params :Dictionary<string, object> { }
	}
}
