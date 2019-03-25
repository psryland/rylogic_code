using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using Binance.API.DomainObjects;
using ExchApi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Binance.API
{
    public class BinanceApi :IDisposable
    {
		// Notes:
		//   - API Info:
		//     https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md

		private readonly string m_key;
		private readonly string m_secret;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;

		public BinanceApi(string key, string secret, CancellationToken cancel_token, string base_address = "https://api.binance.com/", string wss_address = "wss://stream.binance.com:9443/")
		{
			m_key = key;
			m_secret = secret;
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_address;
			UrlWssAddress = wss_address;
			ServerRequestRateLimit = 6f;
			Dispatcher = Dispatcher.CurrentDispatcher;
			Hasher = new HMACSHA512(Encoding.ASCII.GetBytes(m_secret));
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			Client = new HttpClient();
			Subscriptions = new Dictionary<string, MarketDataSubscription>();
			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Util.DisposeRange(Subscriptions.Values);
			Subscriptions.Clear();
			Client = null;
		}

		/// <summary>URL for REST API</summary>
		public string UrlBaseAddress { get; }

		/// <summary>URL for web sockets API</summary>
		public string UrlWssAddress { get; }

		/// <summary>The Http client for REST requests</summary>
		private HttpClient Client
		{
			get { return m_client; }
			set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					m_client.Dispose();
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.BaseAddress = new Uri(UrlBaseAddress + "tradingApi");
					m_client.Timeout = TimeSpan.FromSeconds(10);
					m_client.DefaultRequestHeaders.Add("Key", m_key);
				}
			}
		}
		private HttpClient m_client;

		/// <summary>For marshalling to the main thread</summary>
		private Dispatcher Dispatcher { get; set; }

		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }

		/// <summary>Active market data subscriptions</summary>
		private Dictionary<string, MarketDataSubscription> Subscriptions { get; }

		/// <summary>Receive market data for 'pair'</summary>
		public void Subscribe(CurrencyPair pair)
		{
			// Remove any previous subscription
			if (Subscriptions.TryGetValue(pair.Id, out var sub))
				sub.Dispose();

			var stream = $"{UrlWssAddress}ws/{pair.Id}@depth";
			Subscriptions[pair.Id] = new MarketDataSubscription(pair, stream, m_cancel_token);
		}

		#region Public

		/// <summary>Test connectivity</summary>
		public async Task Ping(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/ping
			await GetData<object>("api/v1/ping", cancel);
		}

		/// <summary>Test connectivity</summary>
		public async Task<DateTimeOffset> ServerTime(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/time
			var server_time = await GetData<JObject>("api/v1/time", cancel);
			return new DateTimeOffset(server_time["serverTime"].Value<long>(), TimeSpan.Zero);
		}

		/// <summary>Test connectivity</summary>
		public async Task<ServerRulesData> ServerRules(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/exchangeInfo
			return await GetData<ServerRulesData>("api/v1/exchangeInfo", cancel);
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public async Task<OrderBook> GetOrderBook(CurrencyPair pair, int depth, CancellationToken? cancel = null)
		{
			// Round the 'depth' value to the nearest valid depth
			var valid_depth_values = new[] { 5, 10, 20, 50, 100, 500, 1000 };
			var valid_depth = valid_depth_values[0];
			foreach (var d in valid_depth_values)
			{
				if (Math.Abs(d - depth) >= Math.Abs(valid_depth - depth)) continue;
				valid_depth = d;
			}

			// https://api.binance.com/api/v1/depth?symbol=IOTABTC&limit=5
			var data = await GetData<OrderBook>("api/v1/depth", cancel,
				new KV("symbol", pair.Id),
				new KV("limit", valid_depth));

			data.Pair = pair;
			return data;
		}

		/// <summary></summary>
		public async Task<List<MarketChartData>> GetChartData(CurrencyPair pair, EMarketPeriod period, long time_beg, long time_end, CancellationToken? cancel = null)
		{
			return await GetChartData(pair, period, new DateTimeOffset(time_beg, TimeSpan.Zero), new DateTimeOffset(time_end, TimeSpan.Zero), cancel);
		}
		public async Task<List<MarketChartData>> GetChartData(CurrencyPair pair, EMarketPeriod period, DateTimeOffset time_beg, DateTimeOffset time_end, CancellationToken? cancel = null)
		{
			try
			{
				var data = await GetData<List<JArray>>("api/v1/klines", cancel,
					new KV("symbol", pair.Id),
					new KV("interval", period.Assoc<string>("tag")),
					new KV("startTime", time_beg.ToUnixTimeMilliseconds()),
					new KV("endTime", time_end.ToUnixTimeMilliseconds()));

				return data.Select(x => new MarketChartData(x)).ToList();
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("too much data"))
					throw new BinanceException(EErrorCode.TooMuchDataRequested, "Too much chart data was requested");
				throw;
			}
		}

		#endregion

		#region Account
		#endregion


		/// <summary>Helper for GETs</summary>
		private async Task<T> GetData<T>(string command, CancellationToken? cancel, params KV[] parameters)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Misc.NoSyncContext())
			using (Scope.Create(() => ++m_blocked_count, () => --m_blocked_count))
			{
				// Poloniex requires the 'nonce' values to be strictly increasing.
				// That means all POSTs must be serialised to avoid a race condition
				// when POSTing two messages in quick succession.
				var cancel_token = cancel ?? m_cancel_token;
				using (m_lock.Lock(cancel_token))
				{
					// Test the cancel token after the lock, because lots of threads will end up
					// waiting at the lock, and we want to cancel them all once cancel is signalled
					cancel_token.ThrowIfCancellationRequested();

					// Limit requests to the required rate
					RequestThrottle();

					// Add the command to the parameters
					var kv = new List<KV> {};
					kv.AddRange(parameters);

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}{command}{Misc.UrlEncode(kv)}";

					// Submit the request
					var response = await Client.GetAsync(url, cancel_token);
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					return ParseJsonReply<T>(reply);
				}
			}
		}

		/// <summary>Helper for POSTs</summary>
		private async Task<T> PostData<T>(string command, CancellationToken? cancel, params KV[] parameters)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Misc.NoSyncContext())
			using (Scope.Create(() => ++m_blocked_count, () => --m_blocked_count))
			{
				// Poloniex requires the 'nonce' values to be strictly increasing.
				// That means all POSTs must be serialised to avoid a race condition
				// when POSTing two messages in quick succession.
				var cancel_token = cancel ?? m_cancel_token;
				using (m_lock.Lock(cancel_token))
				{
					// Limit requests to the required rate
					RequestThrottle();

					// Add the command parameter
					var kv = new List<KV> { new KV("nonce", Misc.Nonce) };
					kv.AddRange(parameters);

					// Create the post data
					var post_data_string = Misc.UrlEncode(kv).TrimStart('?');

					// Create the content to POST
					var content = new StringContent(post_data_string, Encoding.UTF8, "application/x-www-form-urlencoded");
					var msg_hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(post_data_string));
					var signature = Misc.ToStringHex(msg_hash);
					content.Headers.Add("Sign", signature);

					// Submit the request
					// Result Codes:
					//  - 422 Un-processable Entity:
					//    Status code is directly reported by Poloniex server. It means the server understands the content type of the request entity,
					//    and the syntax of the request entity is correct, but was unable to process the contained instructions.
					var response = await Client.PostAsync(Client.BaseAddress, content, cancel_token);
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					return ParseJsonReply<T>(reply);
				}
			}
		}
		private SemaphoreSlim m_lock = new SemaphoreSlim(1, 1);
		private int m_blocked_count;

		/// <summary>Interpret a JSON reply that may be empty, an error, or valid data</summary>
		private T ParseJsonReply<T>(string reply)
		{
			using (var tr = new JsonTextReader(new StringReader(reply)))
			{
				if (reply == "[]")
					return Activator.CreateInstance<T>();

				if (reply.StartsWith("{\"error\""))
					throw new BinanceException(EErrorCode.Failure, m_json.Deserialize<ErrorResult>(tr).Message);

				return m_json.Deserialize<T>(tr);
			}
		}

		/// <summary>Blocking method for throttling requests</summary>
		private void RequestThrottle()
		{
			var request_period_ms = 1000 / ServerRequestRateLimit;
			for (; ; )
			{
				var delta = m_request_sw.ElapsedMilliseconds - m_last_request_ms;
				if (delta < 0 || delta > request_period_ms) break;
				Thread.Sleep((int)(request_period_ms - delta));
			}
			m_last_request_ms = m_request_sw.ElapsedMilliseconds;
		}
		private Stopwatch m_request_sw;
		private long m_last_request_ms;
	}
}
