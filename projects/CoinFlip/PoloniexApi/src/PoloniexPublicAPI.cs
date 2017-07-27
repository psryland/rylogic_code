using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using WampSharp.V2;

namespace Poloniex.API
{
	public class PoloniexApiPublic :IDisposable
	{
		private const int MaxRequestsPerSecond = 6;
		private string UrlBaseAddress;
		private string UrlWssAddress;
		private HttpClient m_client;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;

		public PoloniexApiPublic(CancellationToken cancel_token, string base_address = "https://poloniex.com/", string wss_address = "wss://api.poloniex.com")
		{
			m_cancel_token      = cancel_token;
			UrlBaseAddress      = base_address;
			UrlWssAddress       = wss_address;
			m_client            = HttpClientFactory.Create();
			m_json              = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			m_request_sw        = new Stopwatch();
			ActiveSubscriptions = new Dictionary<string, Subscription>();
			Dispatcher          = Dispatcher.CurrentDispatcher;
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");

			Stop();
			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}

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

		#region REST API Functions

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public Task<Dictionary<string, PriceData>> GetTradePairs()
		{
			// https://poloniex.com/public?command=returnTicker
			return GetData<Dictionary<string, PriceData>>("returnTicker");
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public async Task<Dictionary<string, OrderBook>> GetOrderBook(int depth)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=all&depth=10
			var obs = await GetData<Dictionary<string, OrderBook>>("returnOrderBook", "currencyPair=all", $"depth={depth}");
			foreach (var ob in obs)
				ob.Value.Pair = CurrencyPair.Parse(ob.Key);
			return obs;
		}

		/// <summary>Return the current offers to buy and sell for the given pair</summary>
		public async Task<OrderBook> GetOrderBook(CurrencyPair pair, int depth)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=BTC_NXT&depth=10
			var ob = await GetData<OrderBook>("returnOrderBook", "currencyPair="+pair.Id, $"depth={depth}");
			ob.Pair = pair;
			return ob;
		}

		/// <summary>Return the trade history for the given pair</summary>
		public Task<List<Trade>> GetTradeHistory(CurrencyPair pair)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT
			return GetData<List<Trade>>("returnTradeHistory", "currencyPair="+pair.Id);
		}
		public Task<List<Trade>> GetTradeHistory(CurrencyPair pair, DateTimeOffset start_time, DateTimeOffset end_time)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT&start=1410158341&end=1410499372
			return GetData<List<Trade>>("returnTradeHistory", "currencyPair="+pair.Id, "start="+Misc.ToUnixTime(start_time), "end="+Misc.ToUnixTime(end_time));
		}

		/// <summary></summary>
		public Task<List<MarketChartData>> GetChartData(CurrencyPair pair, MarketPeriod period, DateTimeOffset start_time, DateTimeOffset end_time)
		{
			return GetData<List<MarketChartData>>("returnChartData", "currencyPair="+pair.Id, "start="+Misc.ToUnixTime(start_time), "end="+Misc.ToUnixTime(end_time), "period="+(int)period);
		}

		/// <summary>Helper for GETs</summary>
		private async Task<T> GetData<T>(string command, params object[] parameters)
		{
			Debug.Assert(!m_cancel_token.IsCancellationRequested, "Shouldn't be making new queries when shutdown is signalled");

			// Create the URL for the command + parameters
			var url = string.Format("{0}public?command={1}{2}", UrlBaseAddress, command, (parameters.Length != 0 ? "&" + string.Join("&", parameters) : string.Empty));

			// Limit requests to the poloniex required rate
			var request_period_ms = 1000 / MaxRequestsPerSecond;
			for (; m_request_sw.ElapsedMilliseconds - m_last_request_ms < request_period_ms; await Task.Yield()){}
			m_last_request_ms = m_request_sw.ElapsedMilliseconds;

			// Submit the request
			var response = await m_client.GetAsync(url, m_cancel_token);
			if (!response.IsSuccessStatusCode)
				throw new Exception(response.ReasonPhrase);

			// Interpret the reply
			var reply = await response.Content.ReadAsStringAsync();
			using (var tr = new JsonTextReader(new StringReader(reply)))
				return (T)m_json.Deserialize(tr, typeof(T));
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
