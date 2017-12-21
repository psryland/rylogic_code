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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WampSharp.V2;

namespace Poloniex.API
{
	public class PoloniexApi :IDisposable
	{
		private string UrlBaseAddress;
		private string UrlWssAddress;
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
			ActiveSubscriptions = new Dictionary<string, Subscription>();
			Hasher = new HMACSHA512(Encoding.ASCII.GetBytes(m_secret));
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			Client = new HttpClient();
			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");

			Stop();
			Client = null;
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }

		/// <summary>Raised when a connection is established</summary>
		public event EventHandler OnConnectionChanged;

		/// <summary>Raised when a ticker message is received</summary>
		public event EventHandler<TickerEventArgs> OnTicker;

		/// <summary>Raised when an update to the order book for a pair is received</summary>
		public event EventHandler<OrderBookChangedEventArgs> OnOrdersChanged;

		/// <summary>The WAMP connection channel</summary>
		private IWampChannel WampChannel
		{
			get { return m_wamp_channel; }
			set
			{
				if (m_wamp_channel == value) return;
				if (m_wamp_channel != null)
				{
					m_wamp_channel.RealmProxy.Monitor.ConnectionBroken -= HandleConnectionChanged;
					m_wamp_channel.RealmProxy.Monitor.ConnectionEstablished -= HandleConnectionChanged;
					Reconnector = null;
					m_wamp_channel.Close();
				}
				m_wamp_channel = value;
				if (m_wamp_channel != null)
				{
					m_wamp_channel.RealmProxy.Monitor.ConnectionBroken += HandleConnectionChanged;
					m_wamp_channel.RealmProxy.Monitor.ConnectionEstablished += HandleConnectionChanged;
				}
			}
		}
		private IWampChannel m_wamp_channel;

		/// <summary>Helper for maintaining a connection</summary>
		private WampChannelReconnector Reconnector
		{
			get { return m_reconnector; }
			set
			{
				if (m_reconnector == value) return;
				if (m_reconnector != null) m_reconnector.Dispose();
				m_reconnector = value;
			}
		}
		private WampChannelReconnector m_reconnector;

		/// <summary>For marshalling to the main thread</summary>
		private Dispatcher Dispatcher { get; set; }

		/// <summary>Active channel subscriptions</summary>
		private Dictionary<string, Subscription> ActiveSubscriptions { get; set; }
		private class Subscription :IDisposable
		{
			private readonly string m_stream_name;
			private readonly Action<ISerializedValue[]> m_msg_handler;
			private IDisposable m_handle;

			public Subscription(string stream_name, Action<ISerializedValue[]> message_handler)
			{
				m_stream_name = stream_name;
				m_msg_handler = message_handler;
			}
			public void Dispose()
			{
				Close();
			}

			/// <summary>Subscribe to the stream on 'channel'</summary>
			public void Open(IWampChannel channel)
			{
				Close();

				// Connect
				var subject = channel.RealmProxy.Services.GetSubject(m_stream_name);
				m_handle = subject.Subscribe(x => m_msg_handler(x.Arguments));
			}

			/// <summary>Subscribe from the stream</summary>
			public void Close()
			{
				if (m_handle != null)
				{
					m_handle.Dispose();
					m_handle = null;
				}
			}
		}

		/// <summary>Subscribe to the ticker stream</summary>
		public void SubscribeTicker()
		{
			const string key = "ticker";

			// Clean up old subscriptions
			if (ActiveSubscriptions.TryGetValue(key, out var sub))
				sub.Dispose();

			// Add a subscription to the ticker stream
			sub = ActiveSubscriptions[key] = new Subscription(key, args =>
			{
				if (OnTicker == null)
					return;

				// Deserialise the ticker data
				var currency_pair       = args[0].Deserialize<string>();
				var last_price          = args[1].Deserialize<decimal>();
				var order_top_sell      = args[2].Deserialize<decimal>();
				var order_top_buy       = args[3].Deserialize<decimal>();
				var price_change_pc     = args[4].Deserialize<decimal>();
				var volume_24hour_base  = args[5].Deserialize<decimal>();
				var volume_24hour_quote = args[6].Deserialize<decimal>();
				var is_frozen           = args[7].Deserialize<byte>();

				// Update the price data
				var price_data = new PriceData
				{
					PriceLast             = last_price,
					OrderTopSell          = order_top_sell,
					OrderTopBuy           = order_top_buy,
					PriceChangePercentage = price_change_pc,
					Volume24HourBase      = volume_24hour_base,
					Volume24HourQuote     = volume_24hour_quote,
					IsFrozenInternal      = is_frozen,
				};

				// Update the price data
				var pair = CurrencyPair.Parse(currency_pair);
				Dispatcher.BeginInvoke(new Action(() => OnTicker(this, new TickerEventArgs(pair, price_data))));
			});

			// If the WAMP channel is open, try to connect now
			if (IsConnected)
				sub.Open(WampChannel);
		}

		/// <summary>Subscribe to the order book stream for a pair</summary>
		public void SubscribePair(CurrencyPair pair)
		{
			var key = pair.Id;

			// Clean up old subscriptions
			if (ActiveSubscriptions.TryGetValue(key, out var sub))
				sub.Dispose();

			// Add a subscription to the order book for 'pair'
			sub = ActiveSubscriptions[key] = new Subscription(key, args =>
			{
				if (OnOrdersChanged == null)
					return;

				foreach (var arg in args)
				{
					// Update the order book
					var update = arg.Deserialize<OrderBookUpdate>();
					Dispatcher.BeginInvoke(new Action(() => OnOrdersChanged(this, new OrderBookChangedEventArgs(pair, update))));
				}
			});

			// If the WaMP channel is open, try to connect now
			if (IsConnected)
				sub.Open(WampChannel);
		}

		/// <summary>Connect to Poloniex</summary>
		public void Start()
		{
			// Create a channel
			WampChannel = new DefaultWampChannelFactory().CreateJsonChannel(UrlWssAddress, "realm1");

			// Connection helper
			Func<Task> connect = async () =>
			{
				// Initiate the order book update
				var order_book = GetOrderBook(depth:50);

				// Open the channel
				await WampChannel.Open();

				// Restore subscriptions
				foreach (var sub in ActiveSubscriptions.Values)
					sub.Open(WampChannel);
			};
			Reconnector = new WampChannelReconnector(WampChannel, connect);
			Reconnector.Start();
		}

		/// <summary></summary>
		public void Stop()
		{
			// Clean up channel subscriptions
			foreach (var sub in ActiveSubscriptions.Values) sub.Dispose();
			ActiveSubscriptions.Clear();

			// Close the channel
			WampChannel = null;
		}

		/// <summary>True if a connection is established</summary>
		public bool IsConnected
		{
			get { return WampChannel?.RealmProxy.Monitor.IsConnected ?? false; }
		}

		/// <summary>Handle connection state changed</summary>
		private void HandleConnectionChanged(object sender = null, EventArgs e = null)
		{
			if (OnConnectionChanged != null)
				OnConnectionChanged(this, EventArgs.Empty);
		}

		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

		#region REST API Functions

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

		#region Public

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public Dictionary<string, PriceData> GetTradePairs(CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTicker
			var data = GetData<Dictionary<string, PriceData>>("returnTicker", cancel);
			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public Dictionary<string, OrderBook> GetOrderBook(int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=all&depth=10
			var data = GetData<Dictionary<string, OrderBook>>("returnOrderBook", cancel,
				new KV("currencyPair","all"),
				new KV("depth",depth));
			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for the given pair</summary>
		public OrderBook GetOrderBook(CurrencyPair pair, int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=BTC_NXT&depth=10
			var ob = GetData<OrderBook>("returnOrderBook", cancel,
				new KV("currencyPair", pair.Id),
				new KV("depth", depth));
			ob.Pair = pair;
			return ob;
		}

		/// <summary>Return the trade history for the given pair</summary>
		public List<Trade> GetTradeHistory(CurrencyPair pair, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT
			return GetData<List<Trade>>("returnTradeHistory", cancel, new KV("currencyPair", pair.Id));
		}
		public List<Trade> GetTradeHistory(CurrencyPair pair, DateTimeOffset start_time, DateTimeOffset end_time, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT&start=1410158341&end=1410499372
			return GetData<List<Trade>>("returnTradeHistory", cancel,
				new KV("currencyPair", pair.Id),
				new KV("start", Misc.ToUnixTime(start_time)),
				new KV("end", Misc.ToUnixTime(end_time)));
		}

		/// <summary></summary>
		public List<MarketChartData> GetChartData(CurrencyPair pair, EMarketPeriod period, DateTimeOffset time_beg, DateTimeOffset time_end, CancellationToken? cancel = null)
		{
			return GetChartData(pair, period, time_beg.Ticks, time_end.Ticks, cancel);
		}
		public List<MarketChartData> GetChartData(CurrencyPair pair, EMarketPeriod period, long time_beg, long time_end, CancellationToken? cancel = null)
		{
			return GetData<List<MarketChartData>>("returnChartData", cancel,
				new KV("currencyPair", pair.Id),
				new KV("start", Misc.ToUnixTime(time_beg)),
				new KV("end", Misc.ToUnixTime(time_end)),
				new KV("period", (int)period));
		}

		#endregion

		#region Account

		/// <summary>Return the balances for the account</summary>
		public Dictionary<string, Balance> GetBalances(CancellationToken? cancel = null)
		{
			return PostData<Dictionary<string, Balance>>("returnCompleteBalances", cancel);
		}

		/// <summary>Get currently open orders</summary>
		public Dictionary<string, List<Order>> GetOpenOrders(CancellationToken? cancel = null)
		{
			var positions = PostData<Dictionary<string, List<Order>>>("returnOpenOrders", cancel,
				new KV("currencyPair", "all"));
			foreach (var pos in positions)
				foreach (var p in pos.Value)
					p.Pair = CurrencyPair.Parse(pos.Key);

			return positions;
		}
		public List<Order> GetOpenOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			var positions = PostData<List<Order>>("returnOpenOrders", cancel,
				new KV("currencyPair", pair.Id));
			foreach (var pos in positions) pos.Pair = pair;
			return positions;
		}

		/// <summary>Get the trade history</summary>
		public Dictionary<string, List<Historic>> GetTradeHistory(DateTimeOffset? beg = null, DateTimeOffset? end = null, CancellationToken? cancel = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", "all") };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", Misc.ToUnixTime(beg.Value)));
				parms.Add(new KV("end", Misc.ToUnixTime(end.Value)));
			}

			var history = PostData<Dictionary<string, List<Historic>>>("returnTradeHistory", cancel, parms.ToArray());
			foreach (var his in history)
				foreach (var h in his.Value)
					h.Pair = CurrencyPair.Parse(his.Key);

			return history;
		}
		public List<Historic> GetTradeHistory(CurrencyPair pair, DateTimeOffset? beg = null, DateTimeOffset? end = null, CancellationToken? cancel = null)
		{
			var parms = new List<KV>(){ new KV("currencyPair", pair.Id) };
			if (beg != null && end != null)
			{
				parms.Add(new KV("start", Misc.ToUnixTime(beg.Value)));
				parms.Add(new KV("end", Misc.ToUnixTime(end.Value)));
			}

			var history = PostData<List<Historic>>("returnTradeHistory", cancel, parms.ToArray());
			foreach (var his in history) his.Pair = pair;
			return history;
		}

		/// <summary>Get the history of deposits and withdrawals</summary>
		public FundsTransfer GetTransfers(DateTimeOffset beg, DateTimeOffset end, CancellationToken? cancel = null)
		{
			var parms = new List<KV>
			{
				new KV("start", Misc.ToUnixTime(beg)),
				new KV("end", Misc.ToUnixTime(end)),
			};
			return PostData<FundsTransfer>("returnDepositsWithdrawals", cancel, parms.ToArray());
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public TradeResult SubmitTrade(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_base, CancellationToken? cancel = null)
		{
			return PostData<TradeResult>(Misc.ToString(type), cancel,
				new KV("currencyPair", pair.Id),
				new KV("rate", price_per_coin),
				new KV("amount", volume_base));
		}

		/// <summary>Cancel an order</summary>
		public bool CancelTrade(CurrencyPair pair, ulong order_id, CancellationToken? cancel = null)
		{
			var res = PostData<JObject>("cancelOrder", cancel,
				new KV("currencyPair", pair.Id),
				new KV("orderNumber", order_id));

			return res.Value<byte>("success") == 1;
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private T GetData<T>(string command, CancellationToken? cancel, params KV[] parameters)
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
					var kv = new List<KV>();
					kv.Add(new KV("command", command));
					kv.AddRange(parameters);

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}public{Misc.UrlEncode(kv)}";

					// Submit the request
					var response = Client.GetAsync(url, cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					return ParseJsonReply<T>(reply);
				}
			}
		}

		/// <summary>Helper for POSTs</summary>
		private T PostData<T>(string command, CancellationToken? cancel, params KV[] parameters)
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
					var kv = new List<KV>();
					kv.Add(new KV("command", command));
					kv.Add(new KV("nonce", Misc.Nonce));
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
					var response = Client.PostAsync(Client.BaseAddress, content, cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new Exception(response.ReasonPhrase);

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
					throw new Exception(m_json.Deserialize<ErrorResult>(tr).Message);

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
		
		#endregion
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
