using System;
using System.Collections.Generic;
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

		public BinanceApi(string key, string secret, CancellationToken shutdown)
			: base(key, secret, shutdown, 6, "https://api.binance.com/", "wss://stream.binance.com:9443/")
		{
			//Subscriptions = new Dictionary<string, MarketDataSubscription>();
		}
		public override void Dispose()
		{
			//Util.DisposeRange(Subscriptions.Values);
			//Subscriptions.Clear();
			base.Dispose();
		}

		/// <summary>Return a timestamp for a request in Unix MS</summary>
		private long RequestTimestamp => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ServerTimeOffsetMS;

		/// <summary>The offset between our clock and the time reported by the server (in ms)</summary>
		private long ServerTimeOffsetMS { get; set; }

		///// <summary>Active market data subscriptions</summary>
		//private Dictionary<string, MarketDataSubscription> Subscriptions { get; }

		///// <summary>Receive market data for 'pair'</summary>
		//public void Subscribe(CurrencyPair pair)
		//{
		//	// Remove any previous subscription
		//	if (Subscriptions.TryGetValue(pair.Id, out var sub))
		//		sub.Dispose();

		//	var stream = $"{UrlWssAddress}ws/{pair.Id}@depth";
		//	Subscriptions[pair.Id] = new MarketDataSubscription(pair, stream, Shutdown);
		//}

		#region Public

		/// <summary>Test connectivity</summary>
		public async Task Ping(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/ping
			await GetData(ESecurityType.PUBLIC, "api/v1/ping", cancel);
		}

		/// <summary>Test connectivity</summary>
		public async Task<DateTimeOffset> ServerTime(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/time
			var jtok = await GetData(ESecurityType.PUBLIC, "api/v1/time", cancel);
			var jobj = (JObject)jtok;
			var server_time = jobj["serverTime"].Value<long>();
			return DateTimeOffset.FromUnixTimeMilliseconds(server_time);
		}

		/// <summary>Test connectivity</summary>
		public async Task<ServerRulesData> ServerRules(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v1/exchangeInfo
			var jtok = await GetData(ESecurityType.PUBLIC, "api/v1/exchangeInfo", cancel);
			var rules = ParseJsonReply<ServerRulesData>(jtok);
			ServerTimeOffsetMS = (long)(rules.ServerTime - DateTimeOffset.UtcNow).TotalMilliseconds;
			return rules;
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
			var jtok = await GetData(ESecurityType.PUBLIC, "api/v1/depth", cancel, new Params
			{
				{ "symbol", pair.Id },
				{ "limit", valid_depth },
			});

			var data = ParseJsonReply<OrderBook>(jtok);
			data.Pair = pair;
			return data;
		}

		/// <summary></summary>
		public async Task<List<MarketChartData>> GetChartData(CurrencyPair pair, EMarketPeriod period, UnixMSec time_beg, UnixMSec time_end, CancellationToken? cancel = null)
		{
			try
			{
				var jtok = await GetData(ESecurityType.PUBLIC, "api/v1/klines", cancel, new Params
				{
					{ "symbol", pair.Id },
					{ "interval", period.Assoc<string>("tag") },
					{ "startTime", time_beg.Value },
					{ "endTime", time_end.Value },
				});

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

		/// <summary>Return the balances for the account</summary>
		public async Task<BalancesData> GetBalances(CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/account
			var jtok = await GetData(ESecurityType.USER_DATA, "api/v3/account", cancel, new Params
			{
				{ "timestamp", RequestTimestamp },
			});

			return ParseJsonReply<BalancesData>(jtok);
		}

		/// <summary>Get currently open orders. Not rate limiter increases for each unique pair that has an order</summary>
		public async Task<List<Order>> GetOpenOrders(CurrencyPair? pair = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/openOrders
			var parms = new Params { };
			if (pair != null) parms["symbol"] = pair.Value.Id;
			parms["timestamp"] = RequestTimestamp;

			// Get the orders
			var jtok = await GetData(ESecurityType.USER_DATA, "api/v3/openOrders", cancel, parms);
			var orders = ParseJsonReply<List<Order>>(jtok);

			// Convert the currency pair strings
			foreach (var order in orders)
				order.Pair = ParseCurrencyPair(order.PairInternal);

			return orders;
		}

		/// <summary>Get all open, cancelled, or filled orders</summary>
		public async Task<List<Order>> GetAllOrders(CurrencyPair? pair = null, UnixMSec? beg = null, UnixMSec? end = null, long? min_order_id = null, CancellationToken? cancel = null)
		{
			// https://api.binance.com/api/v3/allOrders
			var parms = new Params{ };
			if (pair != null) parms["symbol"] = pair.Value.Id;
			if (min_order_id != null) parms["orderId"] = min_order_id.Value;
			if (beg != null) parms["startTime"] = beg.Value.Value;
			if (end != null) parms["endTime"] = end.Value.Value;
			parms["timestamp"] = RequestTimestamp;

			var jtok = await GetData(ESecurityType.USER_DATA, "api/v3/allOrders", cancel, parms);
			return ParseJsonReply<List<Order>>(jtok);
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private async Task<JToken> GetData(ESecurityType security, string command, CancellationToken? cancel, Params parameters = null)
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
					if (security != ESecurityType.PUBLIC)
					{
						var query_string = Http_.UrlEncode(parameters).TrimStart('?');
						var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(query_string));
						parameters["signature"] = Misc.ToStringHex(hash);
					}

					// Create the request
					var url = $"{UrlRestAddress}{command}{Http_.UrlEncode(parameters)}";
					var req = new HttpRequestMessage(HttpMethod.Get, url);
					if (security != ESecurityType.PUBLIC)
					{
						req.Headers.Add("X-MBX-APIKEY", Key);
					}

					// Submit the request
					var response = await Client.SendAsync(req, cancel_token);
					var reply = await response.Content.ReadAsStringAsync();

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

		/// <summary>Helper for POSTs</summary>
		private async Task<JToken> PostData(string command, CancellationToken? cancel, Params parameters = null)
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

					// Add the command parameter
					parameters = parameters ?? new Params();
					parameters["nonce"] = Misc.Nonce;

					// Create the post data
					var post_data_string = Http_.UrlEncode(parameters).TrimStart('?');

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
					var reply = await response.Content.ReadAsStringAsync();
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

		/// <summary>Turn a string back into a currency pair</summary>
		public CurrencyPair ParseCurrencyPair(string pair)
		{
			// Since there is no delimiter in the currency pair, we can't reliably convert
			// back to base/quote. Use a bunch of heuristics to and get the base/quote values.
			// Assume the minimum symbol code length is 3.
			for (int i = 3, iend = pair.Length - 3; i <= iend; ++i)
			{
				var base_ = pair.Substring(0, i);
				var quote = pair.Substring(i);
				if (KnownCoins.Contains(base_) &&
					KnownCoins.Contains(quote))
					return new CurrencyPair(base_, quote);
			}
			throw new Exception($"Failed to determine currency pair from code: {pair}");
		}

		/// <summary>All coin names reported by the server</summary>
		private HashSet<string> KnownCoins
		{
			get
			{
				if (m_known_coins == null)
				{
					// Query the server for the known coin
					m_known_coins = new HashSet<string>();
					var res = ServerRules().Result;
					foreach (var sym in res.Symbols)
					{
						m_known_coins.Add(sym.BaseAsset);
						m_known_coins.Add(sym.QuoteAsset);
					}
				}
				return m_known_coins;
			}
		}
		private HashSet<string> m_known_coins;
	}
}
