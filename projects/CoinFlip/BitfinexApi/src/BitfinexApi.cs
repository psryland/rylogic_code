using System;
using System.Collections;
using System.Collections.Generic;
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bitfinex.API
{
	public class BitfinexApi :IDisposable
    {
		// Notes:
		//  - Channel Ids are unique per subscription

		private const string DefaultBaseAddress = @"https://api.bitfinex.com/";
		private const string DefaultWebSocketAddress = @"wss://api.bitfinex.com/ws/2";

		private string UrlBaseAddress;
		private string WebSocketAddress;
		private readonly string m_key;
		private readonly string m_secret;
		private CancellationToken m_cancel_token;
		private JsonSerializer m_json;
		private SemaphoreSlim m_lock;
		private int m_blocked_count;

		public BitfinexApi(CancellationToken cancel_token, string base_address = DefaultBaseAddress, string web_socket_address = DefaultWebSocketAddress)
			:this(null, null, cancel_token, base_address)
		{}
		public BitfinexApi(string key, string secret, CancellationToken cancel_token, string base_address = DefaultBaseAddress, string web_socket_address = DefaultWebSocketAddress)
		{
			Misc.AssertMainThread();
			UrlBaseAddress = base_address;
			WebSocketAddress = web_socket_address;
			m_key = key;
			m_secret = secret;
			m_cancel_token = cancel_token;
			m_ws_cancel_token_src = CancellationTokenSource.CreateLinkedTokenSource(m_cancel_token);
			Hasher = m_secret != null ? new HMACSHA384(Encoding.ASCII.GetBytes(m_secret)) : null;
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			m_lock = new SemaphoreSlim(1,1);
			Dispatcher = Dispatcher.CurrentDispatcher;

			Subscriptions = new SubscriptionContainer(this);
			Wallet  = new WalletData(this);
			Orders  = new OrdersData(this);
			History = new HistoryData(this);
			Market  = new MarketData(this);
			Candles = new ChartData(this);

			WebSocket = null;
			Client = new HttpClient();
			ServerRequestRateLimit = 10;

			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Debug.Assert(m_cancel_token.IsCancellationRequested, "Cancel should have been signalled before here");

			WebSocket = null;
			Client = null;
		}
 
		/// <summary>Report errors</summary>
		public event ErrorEventHandler OnError;
		internal void RaiseError(string message, Exception ex = null)
		{
			Trace.WriteLine(message);
			OnError?.Invoke(this, new ErrorEventArgs(new Exception(message, ex)));
		}

		/// <summary>The API key</summary>
		public string Key { get { return m_key; } }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }
 
		/// <summary>Hasher for signing</summary>
		public HMACSHA384 Hasher { get; private set; }

		/// <summary>For marshalling to the main thread</summary>
		public Dispatcher Dispatcher { get; private set; }

		/// <summary>The current wallet balances</summary>
		public WalletData Wallet { get; private set; }
		public event EventHandler WalletChanged;
		internal void RaiseWalletChanged()
		{
			WalletChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>The current live orders associated with this account</summary>
		public OrdersData Orders { get; private set; }
		public event EventHandler OrdersChanged;
		internal void RaiseOrdersChanged()
		{
			OrdersChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>The history of trades</summary>
		public HistoryData History { get; private set; }
		public event EventHandler HistoryChanged;
		internal void RaiseHistoryChanged()
		{
			HistoryChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>The current market data</summary>
		public MarketData Market { get; private set; }
		public event EventHandler MarketChanged;
		internal void RaiseMarketChanged()
		{
			MarketChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Chart data</summary>
		public ChartData Candles { get; private set; }
		public event EventHandler CandlesChanged;
		internal void RaiseCandlesChanged()
		{
			CandlesChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>True while the Bitfinex server is in maintenance mode</summary>
		public bool MaintenanceMode
		{
			get { return m_maintenance_mode; }
			private set
			{
				if (m_maintenance_mode == value) return;
				m_maintenance_mode = value;
				MaintenanceModeChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		public event EventHandler MaintenanceModeChanged;
		private bool m_maintenance_mode;

		#region WebSocket API

		/// <summary>Open the web-socket connection</summary>
		public void Start()
		{
			WebSocket = new ClientWebSocket();
		}

		/// <summary>Close the web-socket connection</summary>
		public void Stop()
		{
			WebSocket = null;
		}

		/// <summary>Subscriptions to web socket data streams. One per channel Id</summary>
		public SubscriptionContainer Subscriptions { get; private set; }

		/// <summary>The web socket client</summary>
		internal ClientWebSocket WebSocket
		{
			[DebuggerStepThrough] get { return m_web_socket; }
			private set
			{
				if (m_web_socket == value) return;

				// Should not be opening a web socket connection while 'm_cancel_token' says cancel.
				if (m_cancel_token.IsCancellationRequested && value != null)
					throw new Exception("Attempt to open a web socket connection during shutdown");

				if (m_web_socket != null)
				{
					m_ws_cancel_token_src.Cancel();
					Subscriptions.StopAll();
				}
				m_web_socket = value;
				if (m_web_socket != null)
				{
					m_ws_cancel_token_src = CancellationTokenSource.CreateLinkedTokenSource(m_cancel_token);
					ServiceWebSocket(m_web_socket, m_ws_cancel_token_src.Token);
				}

				// Handlers
				async void ServiceWebSocket(ClientWebSocket web_socket, CancellationToken cancel_token)
				{
					// Notes:
					// - 'web_socket' is passed in so that changing 'WebSocket' doesn't need to wait for this async method to exit.
					// - The WebSocket protocol says that after an error the connection is always dropped.
					//   Create a new WebSocket whenever an error occurs.
					var buf = new byte[4096];
					for (;;)
					{
						try
						{
							// Open the connection
							if (web_socket.State == WebSocketState.None)
							{
								var uri = new Uri(WebSocketAddress);
								await web_socket.ConnectAsync(uri, cancel_token);
								if (web_socket.State != WebSocketState.Open)
									throw new Exception("Connection failed");

								// Subscribe to channels
								Subscriptions.StartAll();
							}

							// Await messages
							var data = new ArraySegment<byte>(buf, 0, buf.Length);
							var r = await web_socket.ReceiveAsync(data, cancel_token);
							var count = r.Count;

							// Dynamically grow 'buf' to handle large messages
							for (;!r.EndOfMessage;)
							{
								const int MaxMessageSize = 10_000_000;
								Array.Resize(ref buf, Math.Min(buf.Length * 2, MaxMessageSize));
								data = new ArraySegment<byte>(buf, count, buf.Length - count);
								r = await web_socket.ReceiveAsync(data, cancel_token);
								count += r.Count;
							}

							// Handle the message
							switch (r.MessageType)
							{
							// Server closed the connection
							case WebSocketMessageType.Close:
								WebSocketClosedStatus = r.CloseStatus;
								WebSocketClosedReason = r.CloseStatusDescription;
								break;

							// Server sent a binary message
							case WebSocketMessageType.Binary:
								await web_socket.CloseAsync(WebSocketCloseStatus.ProtocolError, "Binary message received", cancel_token);
								break;

							// Server sent a text message
							case WebSocketMessageType.Text:
								OnMessageReceived(Encoding.UTF8.GetString(buf, 0, count));
								break;
							}
						}
						catch (Exception ex)
						{
							var cancel = ex is TaskCanceledException || ex is OperationCanceledException;
							if (!cancel)
							{
								RaiseError($"WebSocket error", ex);
								cancel_token.WaitHandle.WaitOne(1000); // Delay before reconnecting
							}
							break;
						}
					}

					// Close the connection
					web_socket.Dispose();

					// Reconnect unless stop has been called
					await Dispatcher.BeginInvoke(new Action(() =>
					{
						 if (WebSocket == null) return;
						 WebSocket = new ClientWebSocket();
					}));
				}
			}
		}
		private ClientWebSocket m_web_socket;

		/// <summary>Cancel token for WebSocket requests</summary>
		internal CancellationToken CancelToken
		{
			[DebuggerStepThrough] get { return m_ws_cancel_token_src.Token; }
		}
		private CancellationTokenSource m_ws_cancel_token_src;

		/// <summary>Process a message received from the server</summary>
		private void OnMessageReceived(string json_msg)
		{
			//Trace.WriteLine($"Recv {json_msg}");

			// Read the root json token
			var tok = JToken.Parse(json_msg);

			// Channel snapshots and updates are sent as JArrays
			if (tok is JArray jarr)
			{
				// The first item is the channel id, after that it depends on the subscription
				var channel_id = jarr[0].Value<int>();

				// Find the subscription object that this message is for
				var sub = Subscriptions[channel_id];
				if (sub == null) return;
				sub.ParseUpdate(jarr);
			}

			// Events are sent as JObjects
			if (tok is JObject jobj)
			{
				// Determine the message type by partially deserialising the event message
				var msg = jobj.ToObject<MsgBase>();

				// Handle the message
				switch (msg.EventType)
				{
				default:
					{
						RaiseError($"Unknown event type: {msg.EventType}\r\n{jobj}");
						break;
					}
				case EMsgType.Error:
					{
						var m = jobj.ToObject<MsgError>();
						RaiseError($"[{m.Code}] {m.Message}");
						break;
					}
				case EMsgType.Info:
					{
						// This is a response to opening a connection,
						// or spontaneous messages from the server
						var info = jobj.ToObject<MsgInfo>();

						// If a version number is included
						if (info.Version != null)
						{
							const int expected_version = 2;
							if (info.Version != expected_version)
							{
								RaiseError($"Unsupported API version. Got {info.Version}, Expected {expected_version}");
								WebSocket = null;
							}
						}

						// Handle the info message
						switch (info.Code)
						{
						default:
							RaiseError($"Unknown info message code: {info.Code}. {jobj}");
							break;
						case EInfoMessageCode.APIVersion:
							break;
						case EInfoMessageCode.ReconnectWebSocket:
							WebSocket = new ClientWebSocket();
							break;
						case EInfoMessageCode.MaintenanceModeBeg:
							MaintenanceMode = true;
							break;
						case EInfoMessageCode.MaintenanceModeEnd:
							MaintenanceMode = false;
							break;
						}
						break; 
					}
				case EMsgType.Auth:
					{
						var sub = Subscriptions.Find<SubscriptionAccount>();
						if (sub == null) return;
						sub.ParseResponse(jobj.ToObject<MsgAuth>());
						Subscriptions.Active[sub.ChannelId.Value] = sub;
						break;
					}
				case EMsgType.Subscribed:
					{
						// Handle the response to a subscription request
						switch (msg.ChannelName)
						{
						default:
							{
								Trace.WriteLine($"Unknown subscription response type: {msg.EventType}\r\n{jobj}");
								break;
							}
						case ChannelName.Book:
							{
								// Find the subscription that this response is associated with
								var resp = jobj.ToObject<MsgSubscribedBook>();
								var sub = Subscriptions.Find<SubscriptionOrderBook>(x => x.Pair.Id == resp.Symbol);
								if (sub == null) return;
								sub.ParseResponse(resp);
								Subscriptions.Active[sub.ChannelId.Value] = sub;
								break;
							}
						}
						break;
					}
				case EMsgType.Unsubscribed:
					{
						// Handle a response to an unsubscribe request
						Subscriptions.Remove(msg.ChannelId.Value);
						break;
					}
				}
			}
		}

		/// <summary>The status provided by the server when it last closed the connection</summary>
		public WebSocketCloseStatus? WebSocketClosedStatus { get; private set; }

		/// <summary>The message sent when the connection was last closed</summary>
		public string WebSocketClosedReason { get; private set; }

		#endregion

		#region REST API

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
					m_client.BaseAddress = new Uri(UrlBaseAddress);
					m_client.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		}
		private HttpClient m_client;

		#region Public

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public List<CurrencyPair> GetMarkets(CancellationToken? cancel = null)
		{
			// https://api.bitfinex.com/v1/symbols
			var reply = GetData("v1/symbols", cancel);
			var pairs = ParseJsonReply<List<string>>(reply);
			return pairs.Select(x => CurrencyPair.Parse(x)).ToList();
		}

		/// <summary>Return the order books for the currency pairs in 'Market'</summary>
		public OrderBook GetOrderBook(CurrencyPair pair, int depth, EPrecision precision, CancellationToken? cancel = null)
		{
			// https://api.bitfinex.com/v2/book/Symbol/Precision
			var reply = GetData($"v2/book/{pair.Id}/{precision}", cancel, new KV("len",depth));
			Market.ParseUpdate(pair, JArray.Parse(reply));
			return Market[pair];
		}

		/// <summary>Return candle data for the given time range</summary>
		public List<Candle> GetChartData(CurrencyPair pair, EMarketTimeFrames tf, DateTimeOffset time_beg, DateTimeOffset time_end, CancellationToken? cancel = null)
		{
			return GetChartData(pair, tf, time_beg.Ticks, time_end.Ticks, cancel);
		}
		public List<Candle> GetChartData(CurrencyPair pair, EMarketTimeFrames tf, long time_beg, long time_end, CancellationToken? cancel = null)
		{
			// https://api.bitfinex.com/v2/candles/trade:TimeFrame:Symbol/Section
			var reply = GetData($"v2/candles/trade:{Misc.ToRequestString(tf)}:{pair.Id}/hist", cancel,
				new KV("start", Misc.ToUnixTime(time_beg) * 1000),
				new KV("end", Misc.ToUnixTime(time_end) * 1000),
				new KV("sort", -1));

			// Update the chart data for the given pair
			Candles.ParseUpdate(pair, tf, JArray.Parse(reply));
			return Candles[pair, tf];
		}

		#endregion

		#region Account

		/// <summary>Return the wallet balances</summary>
		public WalletData GetWallets(CancellationToken? cancel = null)
		{
			var reply = PostData("v2/auth/r/wallets", cancel);
			Wallet.ParseUpdate(JArray.Parse(reply));
			RaiseWalletChanged();
			return Wallet;
		}

		/// <summary>Return the current orders</summary>
		public OrdersData GetOrders(CancellationToken? cancel = null)
		{
			var reply = PostData("v2/auth/r/orders", cancel);
			Orders.Clear();
			foreach (var order in JArray.Parse(reply))
				Orders.ParseUpdate(UpdateType.OrderNew, (JArray)order);
			return Orders;
		}
		public OrdersData GetOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			var reply = PostData($"v2/auth/r/orders/{pair.Id}", cancel);
			Orders.ParseUpdate(UpdateType.OrderUpdate, JArray.Parse(reply));
			return Orders;
		}

		/// <summary>Get the server to update the available balance</summary>
		public decimal CalcAvailableBalance(CurrencyPair pair, EOrderType tt, decimal price_q2b, CancellationToken? cancel = null)
		{
			var reply = PostData("v2/auth/calc/order/avail", cancel,
				new KV("symbol", pair.Id),
				new KV("dir", tt == EOrderType.Buy ? +1 : tt == EOrderType.Sell ? -1 : throw new Exception("Unknown order type")),
				new KV("rate", price_q2b),
				new KV("type", "EXCHANGE"));
			var avail = ParseJsonReply<List<decimal>>(reply);
			return avail[0];
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private string GetData(string command, CancellationToken? cancel, params KV[] parameters)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Misc.NoSyncContext())
			using (Scope.Create(() => ++m_blocked_count, () => --m_blocked_count))
			{
				var cancel_token = cancel ?? m_cancel_token;
				using (m_lock.Lock(cancel_token))
				{
					// Limit requests to the required rate
					RequestThrottle();

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}{command}{Misc.UrlEncode(parameters)}";

					// Construct the request
					var req = new HttpRequestMessage(HttpMethod.Get, url);

					if (command.StartsWith("v2"))
					{
					//	throw new NotImplementedException();
					}
					else if (command.StartsWith("v1"))
					{
					//// Add authentication data
					//if (api == EAPI.Auth)
					//{
					//	throw new NotImplementedException();
					//	var nonce = Misc.Nonce;
					//	//var body = Misc.JsonEncode(parameters);

					//	// Add the API key for non-public methods
					//	var kv = new List<KV>();
					//	kv.Add(new KV("request", command));
					//	kv.Add(new KV("nonce", nonce));
					//	kv.AddRange(parameters);

					//	// Set message headers
					//	var json = Misc.JsonEncode(kv);
					//	var json64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
					//	var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(json64));
					//	var signature_hex = Misc.ToStringHex(hash);
					//	req.Headers.Add("X-BFX-APIKEY", m_key);
					//	req.Headers.Add("X-BFX-PAYLOAD", json64);
					//	req.Headers.Add("X-BFX-SIGNATURE", signature_hex);

					//	// v2
					//	//var signature = $"/api/{command}{nonce}{body}";
					//	//var signature_bytes = Encoding.UTF8.GetBytes(signature);
					//	//var signature_hash = Hasher.ComputeHash(signature_bytes);
					//	//var signature_hex = Misc.ToStringHex(signature_hash);
					//	//// Set the message headers
					//	//req.Headers.Add("bfx-nonce", nonce);
					//	//req.Headers.Add("bfx-apikey", m_key);
					//	//req.Headers.Add("bfx-signature", signature_hex);
					//	//// Add the message body
					//	//req.Content = new StringContent(body, Encoding.UTF8, "application/json");
					//}
					}

					// Submit the request
					var response = m_client.SendAsync(req, cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Return the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					return reply;
				}
			}
		}

		/// <summary>Post request</summary>
		private string PostData(string command, CancellationToken? cancel, params KV[] parameters)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Misc.NoSyncContext())
			using (Scope.Create(() => ++m_blocked_count, () => --m_blocked_count))
			{
				var cancel_token = cancel ?? m_cancel_token;
				using (m_lock.Lock(cancel_token))
				{
					// Limit requests to the required rate
					RequestThrottle();

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}{command}";

					// Construct the request
					var req = new HttpRequestMessage(HttpMethod.Post, url);

					// Add authentication data
					var nonce = Misc.Nonce;

					// Handle both API versions
					if (command.StartsWith("v2"))
					{
						var body = Misc.JsonEncode(parameters);
						var signature = $"/api/{command}{nonce}{body}";
						var signature_hex = Misc.ToStringHex(Hasher.ComputeHash(Encoding.UTF8.GetBytes(signature)));

						// Set the message headers
						req.Headers.Add("bfx-nonce", nonce);
						req.Headers.Add("bfx-apikey", m_key);
						req.Headers.Add("bfx-signature", signature_hex);
						
						// Set the message body
						req.Content = new StringContent(body, Encoding.UTF8, "application/json");
					}
					else if (command.StartsWith("v1"))
					{
						throw new NotImplementedException();
					}
					else
					{
						throw new Exception("Unknown API version");
					}

					// Submit the request
					var response = m_client.SendAsync(req, cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Return the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					return reply;
				}
			}
		}

		/// <summary>Interpret a JSON reply that may be empty, an error, or valid data</summary>
		private T ParseJsonReply<T>(string reply)
		{
			using (var tr = new JsonTextReader(new StringReader(reply)))
			{
				if (reply == "[]")
					return Activator.CreateInstance<T>();

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

	#region Containers
	public class SubscriptionContainer
	{
		public SubscriptionContainer(BitfinexApi api)
		{
			Api = api;
			Subs = new HashSet<Subscription>();
			Active = new Dictionary<int, Subscription>();
		}

		/// <summary>The owning API instance</summary>
		private BitfinexApi Api;

		/// <summary>The subscription instances</summary>
		public HashSet<Subscription> Subs { get; private set; }

		/// <summary>Subscriptions that are active (i.e. have a channel id)</summary>
		public Dictionary<int, Subscription> Active { get; private set; }

		/// <summary>O(1) lookup of a subscription by channel id</summary>
		public Subscription this[int channel_id]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Active.TryGetValue(channel_id, out var sub) ? sub : null;
			}
		}

		/// <summary>Find a subscription of type 'T' that satisfies 'pred'</summary>
		public T Find<T>()
		{
			Debug.Assert(Misc.AssertMainThread());
			return Find<T>(x => true);
		}
		public T Find<T>(Func<T,bool> pred)
		{
			Debug.Assert(Misc.AssertMainThread());
			return Subs.OfType<T>().FirstOrDefault(pred);
		}

		/// <summary>Add a subscription instance</summary>
		public void Add(Subscription sub)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(sub.Api == null);
			sub.Api = Api;

			// If there are existing subscriptions equal to 'sub' stop and remove them
			var existing = Subs.Where(x => Equals(x, sub)).ToList();
			foreach (var s in existing)
				Remove(s);

			// Add 'sub' to the collection
			Subs.Add(sub);
			sub.Start();
		}

		/// <summary>Stop and remove a subscription</summary>
		public void Remove(Subscription sub)
		{
			Debug.Assert(Misc.AssertMainThread());

			// Stop if active
			sub.Stop();

			// Remove from the active subscriptions lookup table
			if (sub.ChannelId != null)
				Active.Remove(sub.ChannelId.Value);

			// Remove from the subs collection
			Subs.Remove(sub);
			sub.Api = null;
		}
		public void Remove(int channel_id)
		{
			if (Active.TryGetValue(channel_id, out var sub))
				Remove(sub);
		}
		public void RemoveIf(Func<Subscription, bool> pred)
		{
			foreach (var sub in Subs.Where(x => pred(x)).ToList())
				Remove(sub);
		}

		/// <summary>Stop and remove all subscriptions</summary>
		public void Clear()
		{
			StopAll();
			Subs.Clear();
			Active.Clear();
		}

		/// <summary>Start all subscriptions that are not already running</summary>
		public void StartAll()
		{
			Debug.Assert(Misc.AssertMainThread());
			foreach (var sub in Subs.Where(x => x.State == Subscription.EState.Initial))
				sub.Start();
		}
		public void StopAll()
		{
			Debug.Assert(Misc.AssertMainThread());
			foreach (var sub in Subs.Where(x => x.State == Subscription.EState.Running))
				sub.Stop();
		}
	}
	public class WalletData :IEnumerable<Balance>
	{
		private readonly BitfinexApi m_api;
		private Dictionary<string, Balance> m_data;
		public WalletData(BitfinexApi api)
		{
			m_api = api;
			m_data = new Dictionary<string, Balance>();
		}

		/// <summary>Return the balance of the given currency</summary>
		public Balance this[string currency]
		{
			get
			{
				if (currency == "USD") currency = "USDT";
				return m_data.TryGetValue(currency, out var balance) ? balance : new Balance(currency);
			}
			set
			{
				if (currency == "USD") currency = "USDT";
				m_data[currency] = value;
			}
		}

		/// <summary>Parse a wallet update message</summary>
		public void ParseUpdate(JArray data)
		{
			if (data.Count < 5)
				throw new Exception("Insufficient data for Wallet update");

			// Only care about exchange wallets for now
			var wtype = (EWalletType)Enum.Parse(typeof(EWalletType), data[0].Value<string>(), true);
			if (wtype != EWalletType.Exchange)
				return;

			// Get the currency that the update is for
			var sym = data[1].Value<string>();

			// Update the wallet data
			var bal = this[sym];
			bal.Total              = data[2].Value<decimal>();
			bal.UnsettledInterest  = data[3].Value<decimal>();
			bal.Available          = data[4].Value<decimal?>() ?? bal.Available;
			this[bal.Symbol] = bal;

			if (data[4].Value<decimal?>() == null)
			{
				// Send a message to say what we want calculated
				using (Misc.NoSyncContext())
					m_api.WebSocket.SendAsync($"[0, \"calc\", null, [ [\"wallet_exchange_{sym}\"] ] ]", m_api.CancelToken).Wait();
			}
		}

		#region IEnumerable
		public IEnumerator<Balance> GetEnumerator()
		{
			return m_data.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
	public class OrdersData :IEnumerable<Order>
	{
		private readonly BitfinexApi m_api;
		private Dictionary<ulong, Order> m_data;
		public OrdersData(BitfinexApi api)
		{
			m_api = api;
			m_data = new Dictionary<ulong, Order>();
		}

		/// <summary>Clear the collection of orders</summary>
		public void Clear()
		{
			m_data.Clear();
		}

		/// <summary>Access an order by order id</summary>
		public Order this[ulong order_id]
		{
			get { return m_data.TryGetValue(order_id, out var order) ? order : null; }
		}

		/// <summary>Parse an update to the current orders</summary>
		public void ParseUpdate(string update_type, JArray data)
		{
			if (data.Count < 26)
				throw new Exception($"Insufficient data for {update_type}");

			// Convert the json data to an order instance
			JToken placeholder;
			var order               = new Order();
			order.OrderId           = data[0].Value<ulong>();
			order.GroupId           = data[1].Value<ulong?>();
			order.ClientOrderId     = data[2].Value<ulong?>();
			order.Pair              = CurrencyPair.Parse(data[3].Value<string>());
			order.Created           = Misc.ToDateTimeOffset(data[4].Value<ulong>() / 1000);
			order.Updated           = Misc.ToDateTimeOffset(data[5].Value<ulong>() / 1000);
			order.Amount            = data[6].Value<decimal>();
			order.AmountInitial     = data[7].Value<decimal>();
			order.ExchOrderType     = Misc.ToExchOrderType(data[8].Value<string>()).Value;
			order.ExchOrderTypePrev = Misc.ToExchOrderType(data[9].Value<string>());
			placeholder             = data[10];
			placeholder             = data[11];
			order.Flags             = data[12].Value<int>();
			order.Status            = Misc.ToOrderStatus(data[13].Value<string>()).Value;
			placeholder             = data[14];
			placeholder             = data[15];
			order.Price             = data[16].Value<decimal>();
			order.PriceAverage      = data[17].Value<decimal>();
			order.PriceTrailing     = data[18].Value<decimal?>();
			order.PriceLimit        = data[19].Value<decimal?>();
			placeholder             = data[20];
			placeholder             = data[21];
			placeholder             = data[22];
			order.Notify            = data[23].Value<bool>();
			order.Hidden            = data[24].Value<bool>();
			order.PlacedId          = data[25].Value<int>();

			// Add the order to the collection
			switch (update_type)
			{
			default:
				throw new Exception($"Unknown order update type: {update_type}");
			case UpdateType.OrderNew:
				m_data.Add(order.OrderId, order);
				break;
			case UpdateType.OrderCancel:
				m_data.Remove(order.OrderId);
				break;
			case UpdateType.OrderUpdate:
				m_data[order.OrderId] = order;
				break;
			}
		}

		#region IEnumerable
		public IEnumerator<Order> GetEnumerator()
		{
			return m_data.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
	public class HistoryData :IEnumerable<Trade>
	{
		private readonly BitfinexApi m_api;
		private Dictionary<ulong, Trade> m_data;
		public HistoryData(BitfinexApi api)
		{
			m_api = api;
			m_data = new Dictionary<ulong, Trade>();
		}

		/// <summary>Clear the collection of orders</summary>
		public void Clear()
		{
			m_data.Clear();
		}

		/// <summary>Access an order by order id</summary>
		public Trade this[ulong order_id]
		{
			get { return m_data.TryGetValue(order_id, out var order) ? order : null; }
		}

		/// <summary>Parse an update to the trade history</summary>
		public void ParseUpdate(string update_type, JArray data)
		{
			var expected_count =
				update_type == UpdateType.TradeExecuted ? 9 :
				update_type == UpdateType.TradeUpdate ? 11 :
				throw new Exception($"Unknown trade history update type: {update_type}");
			if (data.Count < expected_count)
				throw new Exception($"Insufficient data for {update_type}");

			// Convert the json data to a trade instance
			var trade           = new Trade();
			trade.TradeId       = data[0].Value<ulong>();
			trade.Pair          = CurrencyPair.Parse(data[1].Value<string>());
			trade.Created       = Misc.ToDateTimeOffset(data[2].Value<ulong>() / 1000);
			trade.OrderId       = data[4].Value<ulong>();
			trade.Amount        = data[5].Value<decimal>();
			trade.Price         = data[6].Value<decimal>();
			trade.ExchOrderType = Misc.ToExchOrderType(data[7].Value<string>()).Value;
			trade.OrderPrice    = data[8].Value<decimal>();
			trade.Maker         = data[9].Value<bool>();
			if (update_type == UpdateType.TradeUpdate)
			{
				trade.Fee         = data[10].Value<decimal>();
				trade.FeeCurrency = data[11].Value<string>();
			}

			// Add the trade to the collection
			switch (update_type)
			{
			default:
				throw new Exception($"Unknown trade update type: {update_type}");
			case UpdateType.TradeExecuted:
				m_data.Add(trade.OrderId, trade);
				break;
			case UpdateType.TradeUpdate:
				m_data[trade.OrderId] = trade;
				break;
			}
		}

		#region IEnumerable
		public IEnumerator<Trade> GetEnumerator()
		{
			return m_data.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
	public class MarketData
	{
		private readonly BitfinexApi m_api;
		private Dictionary<CurrencyPair, OrderBook> m_data;
		public MarketData(BitfinexApi api)
		{
			m_api = api;
			m_data = new Dictionary<CurrencyPair, OrderBook>();
		}

		/// <summary>Return the order book for the given pair</summary>
		public OrderBook this[CurrencyPair pair]
		{
			get { return m_data.TryGetValue(pair, out var ob) ? ob : new OrderBook(pair); }
			set { m_data[pair] = value; }
		}

		/// <summary>Parse a market data update message</summary>
		public void ParseUpdate(CurrencyPair pair, JArray data)
		{
			// Get the order book to update
			var order_book = this[pair];

			// If the update contains an array of arrays, then this is a snapshot
			if (data[0].Type == JTokenType.Array)
			{
				order_book.BuyOrders.Clear();
				order_book.SellOrders.Clear();
				foreach (var upd in data)
				{
					var price  = upd[0].Value<decimal>();
					var count  = upd[1].Value<int>();
					var amount = upd[2].Value<decimal>();
					if (count == 0)
						throw new Exception("Market data snapshot containing a 'count' of zero doesn't make sense");

					// Get the associated book
					var book = amount > 0 ? order_book.BuyOrders : order_book.SellOrders;

					// Add the order
					var order = new OrderBook.Order(price, Math.Abs(amount), count);
					book.Add(order);
				}
			}
			// Otherwise it's a single update
			else
			{
				var price  = data[0].Value<decimal>();
				var count  = data[1].Value<int>();
				var amount = data[2].Value<decimal>();
				var sign   = Math.Sign(amount);

				// Get the associated book
				var book = amount > 0 ? order_book.BuyOrders : order_book.SellOrders;
				var order = new OrderBook.Order(price, Math.Abs(amount), count);

				// Find the index of 'price' in 'book'
				var cmp = Comparer<OrderBook.Order>.Create((l,r) => -sign * l.Price.CompareTo(r.Price));
				var idx = book.BinarySearch(order, cmp);

				// If 'count' is zero, remove the given price level
				if (count == 0)
				{
					if (idx >= 0)
						book.RemoveAt(idx);
				}
				// Otherwise, add/update the price level
				else
				{
					if (idx >= 0)
						book[idx] = order;
					else
						book.Insert(~idx, order);
				}
			}

			// Set the updated order book
			this[pair] = order_book;
		}
	}
	public class ChartData
	{
		private class TimeFramesToCandles :Dictionary<EMarketTimeFrames, List<Candle>> { }
		private class PairToTimeFrames :Dictionary<CurrencyPair, TimeFramesToCandles> { }

		private readonly BitfinexApi m_api;
		private PairToTimeFrames m_data;
		public ChartData(BitfinexApi api)
		{
			m_api = api;
			m_data = new PairToTimeFrames();
		}

		/// <summary>Remove all chart data</summary>
		public void Clear()
		{
			m_data.Clear();
		}

		/// <summary>Remove the chart data for the given pair</summary>
		public void Clear(CurrencyPair pair)
		{
			m_data.Remove(pair);
		}

		/// <summary>True if there is candle data for 'pair' at period 'tf'</summary>
		public bool Contains(CurrencyPair pair, EMarketTimeFrames tf)
		{
			return
				m_data.TryGetValue(pair, out var time_frames) &&
				time_frames.TryGetValue(tf, out var candles);
		}

		/// <summary>Return the candle data for the given pair</summary>
		public List<Candle> this[CurrencyPair pair, EMarketTimeFrames tf]
		{
			get
			{
				return
					!m_data.TryGetValue(pair, out var time_frames) ? new List<Candle>() :
					!time_frames.TryGetValue(tf, out var candles) ? new List<Candle>() :
					candles;
			}
			set
			{
				if (!m_data.TryGetValue(pair, out var time_frames))
					m_data.Add(pair, time_frames = new TimeFramesToCandles());

				time_frames[tf] = value;
			}
		}

		/// <summary>Parse a candle message</summary>
		public void ParseUpdate(CurrencyPair pair, EMarketTimeFrames tf, JArray data)
		{
			// Get the candle collection
			var candles = this[pair, tf];

			// If the update contains an array of arrays, then this is a history update
			if (data[0].Type == JTokenType.Array)
			{
				// Expect the data to be order from oldest to newest
				var new_candles = data.Select(x => new Candle((JArray)x)).ToList();

				// Insert 'new_candles' into 'candles'
				if (new_candles.Count != 0)
				{
					// Erase the range spanned by 'new_candles'
					var beg = new_candles[0];
					var end = new_candles[new_candles.Count-1];
					var ibeg = candles.BinarySearch(beg);
					var iend = candles.BinarySearch(end);
					if (ibeg < 0) ibeg = ~ibeg;
					if (iend < 0) iend = ~iend;
					candles.RemoveRange(ibeg, iend - ibeg);

					// Insert the new candles into 'candles'
					candles.InsertRange(ibeg, new_candles);
				}
			}
			// Otherwise, this is an update to the latest candle
			else
			{
				var new_candle = new Candle(data);

				// Insert/Update 'new_candle' in 'candles'
				var last = candles.Count != 0 ? candles[candles.Count - 1] : null;
				if (last == null || last.Timestamp < new_candle.Timestamp)
				{
					candles.Add(new_candle);
				}
				else if (last.Timestamp == new_candle.Timestamp)
				{
					candles[candles.Count-1] = new_candle;
				}
				else
				{
					var idx = candles.BinarySearch(new_candle);
					if (idx >= 0)
						candles[idx] = new_candle;
					else
						candles.Insert(~idx, new_candle);
				}
			}

			// Save the candle collection
			this[pair, tf] = candles;
		}
	}
	#endregion
}



