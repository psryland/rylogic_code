using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using ExchApi.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poloniex.API.DomainObjects;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Poloniex.API
{
	public class PoloniexApi :IDisposable
	{
		// Notes:
		//  - IP restrictions on the Poloniex API key don't seem to work. You have to use Unrestricted.

		private readonly string m_key;
		private readonly string m_secret;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;

		public PoloniexApi(string key, string secret, CancellationToken cancel_token, string base_address = "https://poloniex.com/", string wss_address = "wss://api.poloniex.com/")
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
			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Client = null;
		}

		/// <summary></summary>
		public string UrlBaseAddress { get; }

		/// <summary></summary>
		public string UrlWssAddress { get; }

		/// <summary>The Http client for RESTful requests</summary>
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

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }

		/// <summary>For marshalling to the main thread</summary>
		private Dispatcher Dispatcher { get; set; }
		
		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

		#region Public

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public async Task<Dictionary<string, PriceData>> GetTradePairs(CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTicker
			var data = await GetData<Dictionary<string, PriceData>>("returnTicker", cancel);
			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public async Task<Dictionary<string, OrderBook>> GetOrderBook(int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=all&depth=10
			var data = await GetData<Dictionary<string, OrderBook>>("returnOrderBook", cancel,
				new KV("currencyPair","all"),
				new KV("depth",depth));
			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for the given pair</summary>
		public async Task<OrderBook> GetOrderBook(CurrencyPair pair, int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=BTC_NXT&depth=10
			var ob = await GetData<OrderBook>("returnOrderBook", cancel,
				new KV("currencyPair", pair.Id),
				new KV("depth", depth));
			ob.Pair = pair;
			return ob;
		}

		/// <summary>Return the trade history for the given pair</summary>
		public async Task<List<Trade>> GetTradeHistory(CurrencyPair pair, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT
			return await GetData<List<Trade>>("returnTradeHistory", cancel,
				new KV("currencyPair", pair.Id));
		}
		public async Task<List<Trade>> GetTradeHistory(CurrencyPair pair, DateTimeOffset start_time, DateTimeOffset end_time, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT&start=1410158341&end=1410499372
			return await GetData<List<Trade>>("returnTradeHistory", cancel,
				new KV("currencyPair", pair.Id),
				new KV("start", start_time.ToUnixTimeMilliseconds()),
				new KV("end", end_time.ToUnixTimeMilliseconds()));
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
				var data = await GetData<List<MarketChartData>>("returnChartData", cancel,
					new KV("currencyPair", pair.Id),
					new KV("start", time_beg.ToUnixTimeMilliseconds()),
					new KV("end", time_end.ToUnixTimeMilliseconds()),
					new KV("period", (int)period));

				// Poloniex returns a single invalid candle if there is no data within the range
				return
					data.Count == 0 ? new List<MarketChartData>() :
					data.Count == 1 && data[0].Invalid ? new List<MarketChartData>() :
					data;
			}
			catch (Exception ex)
			{
				if (ex.Message.Contains("too much data"))
					throw new PoloniexException(EErrorCode.TooMuchDataRequested, "Too much chart data was requested");
				throw;
			}
		}

		#endregion

		#region Account

		/// <summary>Return the balances for the account</summary>
		public async Task<Dictionary<string, Balance>> GetBalances(CancellationToken? cancel = null)
		{
			return await PostData<Dictionary<string, Balance>>("returnCompleteBalances", cancel);
		}

		/// <summary>Get currently open orders</summary>
		public async Task<Dictionary<string, List<Order>>> GetOpenOrders(CancellationToken? cancel = null)
		{
			var positions = await PostData<Dictionary<string, List<Order>>>("returnOpenOrders", cancel,
				new KV("currencyPair", "all"));

			foreach (var pos in positions)
				foreach (var p in pos.Value)
					p.Pair = CurrencyPair.Parse(pos.Key);

			return positions;
		}
		public async Task<List<Order>> GetOpenOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			var positions = await PostData<List<Order>>("returnOpenOrders", cancel,
				new KV("currencyPair", pair.Id));

			foreach (var pos in positions)
				pos.Pair = pair;

			return positions;
		}

		/// <summary>Get the trade history</summary>
		public async Task<Dictionary<string, List<TradeCompleted>>> GetTradeHistory(DateTimeOffset? beg = null, DateTimeOffset? end = null, CancellationToken? cancel = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", "all") };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", beg.Value.ToUnixTimeMilliseconds()));
				parms.Add(new KV("end", end.Value.ToUnixTimeMilliseconds()));
			}

			var history = await PostData<Dictionary<string, List<TradeCompleted>>>("returnTradeHistory", cancel, parms.ToArray());
			foreach (var his in history)
				foreach (var h in his.Value)
					h.Pair = CurrencyPair.Parse(his.Key);

			return history;
		}
		public async Task<List<TradeCompleted>> GetTradeHistory(CurrencyPair pair, DateTimeOffset? beg = null, DateTimeOffset? end = null, CancellationToken? cancel = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", pair.Id) };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", beg.Value.ToUnixTimeMilliseconds()));
				parms.Add(new KV("end", end.Value.ToUnixTimeMilliseconds()));
			}

			var history = await PostData<List<TradeCompleted>>("returnTradeHistory", cancel, parms.ToArray());
			foreach (var his in history)
				his.Pair = pair;

			return history;
		}

		/// <summary>Get the history of deposits and withdrawals</summary>
		public async Task<FundsTransfer> GetTransfers(DateTimeOffset beg, DateTimeOffset end, CancellationToken? cancel = null)
		{
			var parms = new List<KV>
			{
				new KV("start", beg.ToUnixTimeMilliseconds()),
				new KV("end", end.ToUnixTimeMilliseconds()),
			};
			return await PostData<FundsTransfer>("returnDepositsWithdrawals", cancel, parms.ToArray());
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public async Task<TradeResult> SubmitTrade(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_base, CancellationToken? cancel = null)
		{
			return await PostData<TradeResult>(Conv.ToString(type), cancel,
				new KV("currencyPair", pair.Id),
				new KV("rate", price_per_coin),
				new KV("amount", volume_base));
		}

		/// <summary>Cancel an order</summary>
		public async Task<bool> CancelTrade(CurrencyPair pair, long order_id, CancellationToken? cancel = null)
		{
			var res = await PostData<JObject>("cancelOrder", cancel,
				new KV("currencyPair", pair.Id),
				new KV("orderNumber", order_id));

			return res.Value<byte>("success") == 1;
		}

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
					var kv = new List<KV>{new KV("command", command)};
					kv.AddRange(parameters);

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}public{Misc.UrlEncode(kv)}";

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
					var kv = new List<KV> { new KV("command", command), new KV("nonce", Misc.Nonce) };
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
		private SemaphoreSlim m_lock = new SemaphoreSlim(1,1);
		private int m_blocked_count;

		/// <summary>Interpret a JSON reply that may be empty, an error, or valid data</summary>
		private T ParseJsonReply<T>(string reply)
		{
			using (var tr = new JsonTextReader(new StringReader(reply)))
			{
				if (reply == "[]")
					return Activator.CreateInstance<T>();
						
				if (reply.StartsWith("{\"error\""))
					throw new PoloniexException(EErrorCode.Failure, m_json.Deserialize<ErrorResult>(tr).Message);

				return m_json.Deserialize<T>(tr);
			}
		}

		/// <summary>Blocking method for throttling requests</summary>
		private void RequestThrottle()
		{
			var request_period_ms = 1000 / ServerRequestRateLimit;
			for (;;)
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

	#region Event Args
	public class TickerEventArgs :EventArgs
	{
		internal TickerEventArgs(CurrencyPair pair, PriceData pd)
		{
			Pair = pair;
			PriceData = pd;
		}

		/// <summary>The currency pair that was updated</summary>
		public CurrencyPair Pair { get; private set; }

		/// <summary>The latest price data for 'pair'</summary>
		public PriceData PriceData { get; private set; }
	}
	public class OrderBookChangedEventArgs :EventArgs
	{
		internal OrderBookChangedEventArgs(CurrencyPair pair, OrderBookUpdate update)
		{
			Pair = pair;
			Update = update;
		}

		/// <summary>The currency pair that was updated</summary>
		public CurrencyPair Pair { get; private set; }

		/// <summary>The new data</summary>
		public OrderBookUpdate Update { get; private set; }
	}
	#endregion
}
