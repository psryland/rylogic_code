using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Poloniex.API
{
	public class PoloniexApiPrivate :IDisposable
	{
		private const int MaxRequestsPerSecond = 6;
		private string UrlBaseAddress;
		private HttpClient m_client;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;
		private readonly string m_key;
		private readonly string m_secret;

		public PoloniexApiPrivate(string key, string secret, CancellationToken cancel_token, string base_address = "https://poloniex.com/")
		{
			m_key = key;
			m_secret = secret;
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_address;
			Dispatcher = Dispatcher.CurrentDispatcher;
			Hasher = new HMACSHA512(Encoding.ASCII.GetBytes(m_secret));
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			m_client = new HttpClient { BaseAddress = new Uri(UrlBaseAddress + "tradingApi") };
			m_client.DefaultRequestHeaders.Add("Key", m_key);
			m_request_sw = new Stopwatch();
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

		/// <summary>For marshalling to the main thread</summary>
		private Dispatcher Dispatcher { get; set; }

		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

		#region REST API Functions

		/// <summary>Return the balances for the account</summary>
		public Task<Dictionary<string, Balance>> GetBalances()
		{
			return PostData<Dictionary<string, Balance>>("returnCompleteBalances");
		}

		/// <summary>Get all currently open orders</summary>
		public async Task<Dictionary<string, List<Position>>> GetOpenOrders()
		{
			var positions = await PostData<Dictionary<string, List<Position>>>("returnOpenOrders", new KV("currencyPair", "all"));
			foreach (var pos in positions)
				foreach (var p in pos.Value)
					p.Pair = CurrencyPair.Parse(pos.Key);
			return positions;
		}

		/// <summary>Get the currently open orders for 'pair'</summary>
		public async Task<List<Position>> GetOpenOrders(CurrencyPair pair)
		{
			var positions = await PostData<List<Position>>("returnOpenOrders", new KV("currencyPair", pair.Id));
			foreach (var pos in positions) pos.Pair = pair;
			return positions;
		}

		/// <summary>Get the trade history for all markets</summary>
		public async Task<Dictionary<string, List<Historic>>> GetTradeHistory(DateTimeOffset? beg = null, DateTimeOffset? end = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", "all") };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", Misc.ToUnixTime(beg.Value)));
				parms.Add(new KV("end", Misc.ToUnixTime(end.Value)));
			}

			var history = await PostData<Dictionary<string, List<Historic>>>("returnTradeHistory", parms.ToArray());
			foreach (var his in history)
				foreach (var h in his.Value)
					h.Pair = CurrencyPair.Parse(his.Key);

			return history;
		}

		/// <summary>Get the trade history for the given market</summary>
		public async Task<List<Historic>> GetTradeHistory(CurrencyPair pair, DateTimeOffset? beg = null, DateTimeOffset? end = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", pair.Id) };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", Misc.ToUnixTime(beg.Value)));
				parms.Add(new KV("end", Misc.ToUnixTime(end.Value)));
			}

			var history = await PostData<List<Historic>>("returnTradeHistory", parms.ToArray());
			foreach (var his in history) his.Pair = pair;
			return history;
		}

		/// <summary>Cancel an order</summary>
		public async Task<bool> CancelTrade(CurrencyPair pair, ulong order_id)
		{
			var res = await PostData<JObject>("cancelOrder",
				new KV("currencyPair", pair.Id),
				new KV("orderNumber", order_id));

			return res.Value<byte>("success") == 1;
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public async Task<ulong> SubmitTrade(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_quote)
		{
			var res = await PostData<JObject>(Misc.ToString(type),
				new KV("currencyPair", pair.Id),
				new KV("rate", price_per_coin),
				new KV("amount", volume_quote));

			return res.Value<ulong>("orderNumber");
		}

		/// <summary>Helper for POSTs</summary>
		private Task<T> PostData<T>(string command, params KV[] post_data)
		{
			Debug.Assert(!m_cancel_token.IsCancellationRequested, "Shouldn't be making new requests when shutdown is signalled");
			return Task.Run(() =>
			{
				// Poloniex requires the 'nonce' values to be strictly increasing.
				// That means all POSTs must be serialised to avoid a race condition
				// when POSTing two messages in quick succession.
				lock (m_post_lock)
				{
					m_cancel_token.ThrowIfCancellationRequested();

					// Create the post data
					var post_data_string = new StringBuilder();
					post_data_string.Append("&command=").Append(command);
					post_data_string.Append("&nonce=").Append(HttpPostNonce);
					foreach (var kv in post_data)
					{
						var value = (kv.Value as string)?.Replace(' ','+') ?? kv.Value;
						post_data_string.Append("&").Append(kv.Key).Append("=").Append(value);
					}

					// Create the content to POST
					var content = new StringContent(post_data_string.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded");
					var msg_hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(post_data_string.ToString()));
					var signature = Misc.ToStringHex(msg_hash);
					content.Headers.Add("Sign", signature);

					// Limit requests to the poloniex required rate
					var request_period_ms = 1000 / MaxRequestsPerSecond;
					for (; m_request_sw.ElapsedMilliseconds - m_last_request_ms < request_period_ms; Thread.Yield()){}
					m_last_request_ms = m_request_sw.ElapsedMilliseconds;

					// Submit the request
					var response = m_client.PostAsync(m_client.BaseAddress, content, m_cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new Exception(response.ReasonPhrase);

					// Interpret the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					try
					{
						if (reply == "[]") return Activator.CreateInstance<T>();
						using (var tr = new JsonTextReader(new StringReader(reply)))
							return m_json.Deserialize<T>(tr);
					}
					catch
					{
						using (var tr = new JsonTextReader(new StringReader(reply)))
							throw new Exception(m_json.Deserialize<ErrorResult>(tr).Message);
					}
				}
			}, m_cancel_token);
		}
		private object m_post_lock = new object();
		private Stopwatch m_request_sw;
		private long m_last_request_ms;

		/// <summary>Helper for generating "nonce"</summary>
		private static string HttpPostNonce
		{
			get
			{
				lock (m_nonce_lock)
				{
					var nonce = DateTimeOffset.UtcNow.Ticks;
					m_nonce = Math.Max(m_nonce + 1, nonce);
					return m_nonce.ToString(CultureInfo.InvariantCulture);
				}
			}
		}
		private static long m_nonce = (long)((DateTimeOffset.UtcNow - Misc.UnixEpochStart).TotalMilliseconds * 1000);
		private static object m_nonce_lock = new object();

		/// <summary>Helper for passing Key/Value pair parameters</summary>
		private struct KV
		{
			[DebuggerStepThrough] public KV(string key, object value) { Key = key; Value = value; }
			public string Key;
			public object Value;
		}

		/// <summary>Helper for decoding error responses</summary>
		private class ErrorResult
		{
			[JsonProperty("error")]
			public string Message { get; private set; }
		}

		#endregion
	}
}
