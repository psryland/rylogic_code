using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Binance.API.DomainObjects;
using ExchApi.Common;
using Newtonsoft.Json.Linq;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Binance.API
{
	public partial class BinanceApi :ExchangeApi<HMACSHA256>
    {
		// Notes:
		//   - API Info:
		//     https://github.com/binance-exchange/binance-official-api-docs/blob/master/rest-api.md

		public BinanceApi(string key, string secret, CancellationToken shutdown, Logger log)
			: base(key, secret, shutdown, 6, "https://api.binance.com/", "wss://stream.binance.com:9443/")
		{
			try
			{
				Log = new Logger("BinanceApi", log);
				TickerData = new TickerDataCache(this);
				MarketData = new MarketDataCache(this);
				CandleData = new CandleDataCache(this);
				UserData = new UserDataCache(this);
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public override void Dispose()
		{
			UserData = null;
			CandleData = null;
			MarketData = null;
			TickerData = null;
			Log = Util.Dispose(Log);
			base.Dispose();
		}
		public override async Task InitAsync()
		{
			// Populate the currency pair map.
			var rules = await ServerRules(Shutdown);

			// Record the current server time
			ServerTimeOffsetMS = (long)(rules.ServerTime - DateTimeOffset.UtcNow).TotalMilliseconds;

			// Update the mapping from 'Symbol' to currency pairs
			foreach (var sym in rules.Symbols)
				CurrencyPair.SymbolToPair[sym.Symbol] = new CurrencyPair(sym.BaseAsset, sym.QuoteAsset);

			// Initialise the throttle with the weight limits
			foreach (var limit in rules.RateLimits)
			{
				if (limit.RateLimitType == ERateLimitType.REQUEST_WEIGHT)
				{
					RequestThrottle.WeightLimit = limit.Limit;
					RequestThrottle.WeightInterval =
						limit.Interval == "SECOND" ? TimeSpan.FromSeconds(limit.IntervalNumber) :
						limit.Interval == "MINUTE" ? TimeSpan.FromMinutes(limit.IntervalNumber) :
						limit.Interval == "DAY" ? TimeSpan.FromDays(limit.IntervalNumber) :
						throw new Exception($"Unknown interval value: {limit.Interval}");
				}
			}
		}

		/// <summary>Static log instance</summary>
		public static Logger Log { get; private set; }

		/// <summary>Return a timestamp for a request in Unix MS</summary>
		private long RequestTimestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ServerTimeOffsetMS;

		/// <summary>The offset between our clock and the time reported by the server (in ms)</summary>
		private long ServerTimeOffsetMS { get; set; }

		#region WebSocket API

		/// <summary>A local cache of the last 24hr ticker data</summary>
		public TickerDataCache TickerData
		{
			get { return m_ticker_data; }
			private set
			{
				if (m_ticker_data == value) return;
				Util.Dispose(ref m_ticker_data);
				m_ticker_data = value;
			}
		}
		private TickerDataCache m_ticker_data;

		/// <summary>A local copy of the state of the markets</summary>
		public MarketDataCache MarketData
		{
			get { return m_market_data; }
			private set
			{
				if (m_market_data == value) return;
				Util.Dispose(ref m_market_data);
				m_market_data = value;
			}
		}
		private MarketDataCache m_market_data;
		
		/// <summary>A local mirror of the candle data</summary>
		public CandleDataCache CandleData
		{
			get { return m_candle_data; }
			set
			{
				if (m_candle_data == value) return;
				Util.Dispose(ref m_candle_data);
				m_candle_data = value;
			}
		}
		private CandleDataCache m_candle_data;

		/// <summary>A local mirror of the users trade history and orders</summary>
		public UserDataCache UserData
		{
			get { return m_user_data; }
			set
			{
				if (m_user_data == value) return;
				Util.Dispose(ref m_user_data);
				m_user_data = value;
			}
		}
		private UserDataCache m_user_data;

		#endregion

		#region Public

		/// <summary>Test connectivity</summary>
		public async Task Ping(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/ping
			await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/ping", cancel);
		}

		/// <summary>Test connectivity</summary>
		public async Task<DateTimeOffset> ServerTime(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/time
			var jtok = await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/time", cancel);
			var jobj = (JObject)jtok;
			var server_time = jobj["serverTime"].Value<long>();
			return DateTimeOffset.FromUnixTimeMilliseconds(server_time);
		}

		/// <summary>Test connectivity</summary>
		public async Task<ServerRulesData> ServerRules(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/exchangeInfo
			var jtok = await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/exchangeInfo", cancel);
			var rules = ParseJsonReply<ServerRulesData>(jtok);
			return rules;
		}

		/// <summary>Get the ticker price data for all or one currency pair</summary>
		public async Task<List<Ticker>> GetTicker(CurrencyPair? pair = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/ticker/24hr
			var jtok = await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/ticker/24hr", cancel);
			return ParseJsonReply<List<Ticker>>(jtok);
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public async Task<MarketDepth> GetOrderBook(CurrencyPair pair, int depth, CancellationToken? cancel = null)
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
			var parms = new Params
			{
				{ "symbol", pair.Id },
				{ "limit", valid_depth },
			};

			var jtok = await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/depth", cancel, parms);
			var data = ParseJsonReply<MarketDepth>(jtok);
			data.Pair = pair;
			return data;
		}

		/// <summary>Return candle data</summary>
		public async Task<List<MarketChartData>> GetChartData(CurrencyPair pair, EMarketPeriod period, UnixMSec? time_beg = null, UnixMSec? time_end = null, CancellationToken? cancel = null)
		{
			try
			{
				// https://api.binance.com/api/v1/klines?symbol=IOTABTC&interval=1d&limit=5
				var parms = new Params { };
				parms["symbol"] = pair.Id;
				parms["interval"] = period.Assoc<string>();
				if (time_beg != null) parms["startTime"] = time_beg.Value.Value;
				if (time_end != null) parms["endTime"] = time_end.Value.Value;

				var jtok = await GetData(HttpMethod.Get, ESecurityType.PUBLIC, "api/v1/klines", cancel, parms);
				var data = ParseJsonReply<List<JArray>>(jtok);
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

		/// <summary>Request the start of user data streamed via web sockets. Returns the 'ListenKey'</summary>
		internal async Task<string> StartUserDataStream(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/userDataStream
			var jtok = await GetData(HttpMethod.Post, ESecurityType.USER_STREAM, "api/v1/userDataStream", cancel);
			return jtok["listenKey"].Value<string>();
		}

		/// <summary>Kick the user data watch dog</summary>
		internal async Task KeepAliveUserDataStream(string listen_key, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/userDataStream
			var parms = new Params { };
			parms["listenKey"] = listen_key;
			await GetData(HttpMethod.Put, ESecurityType.USER_STREAM, "api/v1/userDataStream", cancel, parms);
		}

		/// <summary>Stop the user data stream</summary>
		internal async Task CloseUserDataStream(string listen_key, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/userDataStream
			var parms = new Params { };
			parms["listenKey"] = listen_key;
			await GetData(HttpMethod.Delete, ESecurityType.USER_STREAM, "api/v1/userDataStream", cancel, parms);
		}

		/// <summary>Return the balances for the account</summary>
		public async Task<BalancesData> GetBalances(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/account
			var jtok = await GetData(HttpMethod.Get, ESecurityType.USER_DATA, "api/v3/account", cancel, timestamp: true);
			return ParseJsonReply<BalancesData>(jtok);
		}

		/// <summary>Get currently open orders. Not rate limiter increases for each unique pair that has an order</summary>
		public async Task<List<Order>> GetOpenOrders(CurrencyPair? pair = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/openOrders
			var parms = new Params { };
			if (pair != null) parms["symbol"] = pair.Value.Id;

			// Get the orders
			var jtok = await GetData(HttpMethod.Get, ESecurityType.USER_DATA, "api/v3/openOrders", cancel, parms, timestamp: true);
			return ParseJsonReply<List<Order>>(jtok);
		}

		/// <summary>Get all completed orders</summary>
		public async Task<List<OrderFill>> GetTradeHistory(CurrencyPair pair, long? from_id = null, UnixMSec? beg = null, UnixMSec? end = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/myTrades
			var parms = new Params { };
			parms["symbol"] = pair.Id;
			if (from_id != null) parms["fromId"] = from_id.Value;
			if (beg != null) parms["startTime"] = beg.Value.Value;
			if (end != null) parms["endTime"] = end.Value.Value;

			// Get the history
			var jtok = await GetData(HttpMethod.Get, ESecurityType.USER_DATA, "api/v3/myTrades", cancel, parms, timestamp: true);
			return ParseJsonReply<List<OrderFill>>(jtok);
		}

		/// <summary>Get all order related actions, such as new orders, cancelled orders, etc...</summary>
		public async Task<List<Order>> GetAllOrders(CurrencyPair pair, UnixMSec? beg = null, UnixMSec? end = null, long? min_order_id = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/allOrders
			var parms = new Params{ };
			parms["symbol"] = pair.Id;
			if (beg != null) parms["startTime"] = beg.Value.Value;
			if (end != null) parms["endTime"] = end.Value.Value;
			if (min_order_id != null) parms["orderId"] = min_order_id.Value;

			// Get the state of all orders
			var jtok = await GetData(HttpMethod.Get, ESecurityType.USER_DATA, "api/v3/allOrders", cancel, parms, timestamp: true);
			return ParseJsonReply<List<Order>>(jtok);
		}

		/// <summary>Create an order to buy/sell</summary>
		public async Task<TradeResult> SubmitTrade(CurrencyPair pair, EOrderSide side, decimal? price, decimal amount_base, EOrderType type, ETimeInForce? time_in_force = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/order
			var parms = new Params { };
			parms["symbol"] = pair.Id;
			parms["side"] = side;
			parms["type"] = type;
			parms["quantity"] = amount_base;
			parms["newOrderRespType"] = "FULL";
			if (price != null) parms["price"] = price.Value;
			if (time_in_force != null) parms["timeInForce"] = time_in_force.Value;

			// Place the order
			var jtok = await GetData(HttpMethod.Post, ESecurityType.TRADE, "api/v3/order", cancel, parms, timestamp: true);
			return ParseJsonReply<TradeResult>(jtok);
		}

		/// <summary>Cancel an order</summary>
		public async Task<TradeResult> CancelTrade(CurrencyPair pair, long order_id, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/order
			var parms = new Params { };
			parms["symbol"] = pair.Id;
			parms["orderId"] = order_id;

			var jtok = await GetData(HttpMethod.Delete, ESecurityType.TRADE, "api/v3/order", cancel, parms, timestamp: true);
			return ParseJsonReply<TradeResult>(jtok);
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private async Task<JToken> GetData(HttpMethod method, ESecurityType security, string command, CancellationToken? cancel, Params parameters = null, bool timestamp = false)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Task_.NoSyncContext())
			{
				// Poloniex requires the 'nonce' values to be strictly increasing.
				// That means all POSTs must be serialised to avoid a race condition
				// when POSTing two messages in quick succession.
				var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;
				using (RequestThrottle.Lock(cancel_token)) // Limit requests to the required rate
				{
					await RequestThrottle.Wait(cancel_token);

					// Add fields to the request based on 'security'
					parameters = parameters ?? new Params();
					if (timestamp)
					{
						// Needs to be added after waiting on the throttle
						parameters["timestamp"] = RequestTimestamp;
					}
					if (security == ESecurityType.TRADE || security == ESecurityType.USER_DATA)
					{
						var query_string = Http_.UrlEncode(parameters).TrimStart('?');
						var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(query_string));
						parameters["signature"] = Misc.ToStringHex(hash);
					}

					// Create the request
					var url = $"{UrlRestAddress}{command}{Http_.UrlEncode(parameters)}";
					var req = new HttpRequestMessage(method, url);
					if (security == ESecurityType.TRADE || security == ESecurityType.USER_DATA || security == ESecurityType.USER_STREAM || security == ESecurityType.MARKET_DATA)
					{
						req.Headers.Add("X-MBX-APIKEY", Key);
					}

					// Submit the request
					var sw = new Stopwatch().StartNow();
					var response = await Client.SendAsync(req, cancel_token);
					var reply = await response.Content.ReadAsStringAsync();
					Log.Write(ELogLevel.Debug, $"Req time: {sw.Elapsed.ToPrettyString(min_unit: TimeSpan_.ETimeUnits.Milliseconds)} - {url}");

					// Check the API usage weight
					if (response.Headers.TryGetValues("X-MBX-USED-WEIGHT", out var weights))
					{
						RequestThrottle.UsedWeight = long.Parse(weights.First());
						if (RequestThrottle.UsedWeight > 0.5 * RequestThrottle.WeightLimit)
							Debug.Assert(false);
					}

					// Interpret the reply
					if (!response.IsSuccessStatusCode)
					{
						// Check for an error
						var jobj = reply != null ? JObject.Parse(reply) : null;
						if (jobj != null &&
							jobj["code"]?.Value<int>() is int code &&
							jobj["msg"]?.Value<string>() is string msg)
							throw new BinanceException((EErrorCode)code, msg);
						else
							throw new HttpException((int)response.StatusCode, response.ReasonPhrase);
					}

					return JToken.Parse(reply);
				}
			}
		}

		/// <summary>Interpret a JSON reply that may be empty, an error, or valid data</summary>
		private T ParseJsonReply<T>(JToken jtok)
		{
			// If an empty array is returned, return a default instance of T
			if (jtok is JArray jarr && jarr.Count == 0)
				return Activator.CreateInstance<T>();

			// Check for an error
			if (jtok is JObject jobj && jobj["error"]?.Value<string>() is string error)
				throw new BinanceException(EErrorCode.Failure, error);

			// Parse the result
			return jtok.ToObject<T>();
		}
	}
}
