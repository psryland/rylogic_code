using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ExchApi.Common;
using Newtonsoft.Json.Linq;
using Poloniex.API.DomainObjects;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Poloniex.API
{
	public class PoloniexApi :ExchangeApi<HMACSHA512>
	{
		// Notes:
		//  - IP restrictions on the Poloniex API key don't seem to work. You have to use Unrestricted.

		public PoloniexApi(string key, string secret, CancellationToken shutdown, Logger log)
			:base(key, secret, shutdown, 6f, "https://poloniex.com/", "wss://api.poloniex.com/")
		{
			Log = new Logger("PoloniexApi", log);
			Client.DefaultRequestHeaders.Add("Key", Key);
		}
		public override void Dispose()
		{
			Log = Util.Dispose(Log);
			base.Dispose();
		}

		/// <summary>Static log instance</summary>
		public static Logger Log { get; private set; }

		#region Public

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public async Task<Dictionary<string, PriceData>> GetTradePairs(CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTicker
			var jtok = await GetData(Method.Public, "returnTicker", cancel);
			var data = ParseJsonReply<Dictionary<string, PriceData>>(jtok);

			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for all pairs</summary>
		public async Task<Dictionary<string, OrderBook>> GetOrderBook(int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=all&depth=10
			var jtok = await GetData(Method.Public, "returnOrderBook", cancel, new Params
			{
				{ "currencyPair", "all" },
				{ "depth", depth },
			});

			var data = ParseJsonReply<Dictionary<string, OrderBook>>(jtok);
			foreach (var kv in data)
				kv.Value.Pair = CurrencyPair.Parse(kv.Key);

			return data;
		}

		/// <summary>Return the current offers to buy and sell for the given pair</summary>
		public async Task<OrderBook> GetOrderBook(CurrencyPair pair, int depth, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnOrderBook&currencyPair=BTC_NXT&depth=10
			var jtok = await GetData(Method.Public, "returnOrderBook", cancel, new Params
			{
				{ "currencyPair", pair.Id },
				{ "depth", depth },
			});

			var ob = ParseJsonReply<OrderBook>(jtok);
			ob.Pair = pair;
			return ob;
		}

		/// <summary>Return the trade history for the given pair</summary>
		public async Task<List<Trade>> GetTradeHistory(CurrencyPair pair, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT
			var jtok = await GetData(Method.Public, "returnTradeHistory", cancel, new Params
			{
				{ "currencyPair", pair.Id }
			});

			return ParseJsonReply<List<Trade>>(jtok);
		}
		public async Task<List<Trade>> GetTradeHistory(CurrencyPair pair, UnixSec start_time, UnixSec end_time, CancellationToken? cancel = null)
		{
			// https://poloniex.com/public?command=returnTradeHistory&currencyPair=BTC_NXT&start=1410158341&end=1410499372
			var jtok = await GetData(Method.Public, "returnTradeHistory", cancel, new Params
			{
				{ "currencyPair", pair.Id },
				{ "start", start_time },
				{ "end", end_time },
			});

			return ParseJsonReply<List<Trade>>(jtok);
		}

		/// <summary></summary>
		public async Task<List<MarketChartData>> GetChartData(CurrencyPair pair, EMarketPeriod period, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel = null)
		{
			try
			{
				var jtok = await GetData(Method.Public, "returnChartData", cancel, new Params
				{
					{ "currencyPair", pair.Id },
					{ "start", time_beg.Value },
					{ "end", time_end.Value },
					{ "period", (int)period },
				});

				var data = ParseJsonReply<List<MarketChartData>>(jtok);

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
			var jtok = await PostData(Method.Account, "returnCompleteBalances", cancel);
			return ParseJsonReply<Dictionary<string, Balance>>(jtok);
		}

		/// <summary>Get currently open orders</summary>
		public async Task<Dictionary<string, List<Order>>> GetOpenOrders(CancellationToken? cancel = null)
		{
			var jtok = await PostData(Method.Account, "returnOpenOrders", cancel, new Params
			{
				{ "currencyPair", "all" },
			});

			var positions = ParseJsonReply<Dictionary<string, List<Order>>>(jtok);
			foreach (var pos in positions)
				foreach (var p in pos.Value)
					p.Pair = CurrencyPair.Parse(pos.Key);

			return positions;
		}
		public async Task<List<Order>> GetOpenOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			var jtok = await PostData(Method.Account, "returnOpenOrders", cancel, new Params
			{
				{ "currencyPair", pair.Id },
			});

			var positions = ParseJsonReply<List<Order>>(jtok);
			foreach (var pos in positions)
				pos.Pair = pair;

			return positions;
		}

		/// <summary>Get the trade history</summary>
		public async Task<Dictionary<string, List<TradeCompleted>>> GetTradeHistory(UnixSec? beg = null, UnixSec? end = null, CancellationToken? cancel = null)
		{
			var parms = new Params{ { "currencyPair", "all" } };
			if (beg != null && end != null)
			{
				parms["start"] = beg.Value.Value;
				parms["end"] = end.Value.Value;
			}

			var jtok = await PostData(Method.Account, "returnTradeHistory", cancel, parms);
			var history = ParseJsonReply<Dictionary<string, List<TradeCompleted>>>(jtok);
			foreach (var his in history)
				foreach (var h in his.Value)
					h.Pair = CurrencyPair.Parse(his.Key);

			return history;
		}
		public async Task<List<TradeCompleted>> GetTradeHistory(CurrencyPair pair, UnixSec? beg = null, UnixSec? end = null, CancellationToken? cancel = null)
		{
			var parms = new Params{ { "currencyPair", pair.Id } };
			if (beg != null && end != null)
			{
				parms["start"] = beg.Value.Value;
				parms["end"] = end.Value.Value;
			}

			var jtok = await PostData(Method.Account, "returnTradeHistory", cancel, parms);
			var history = ParseJsonReply<List<TradeCompleted>>(jtok);
			foreach (var his in history)
				his.Pair = pair;

			return history;
		}

		/// <summary>Get the history of deposits and withdrawals</summary>
		public async Task<FundsTransfer> GetTransfers(UnixSec beg, UnixSec end, CancellationToken? cancel = null)
		{
			var parms = new Params
			{
				{ "start", beg.Value },
				{ "end", end.Value },
			};
			var jtok = await PostData(Method.Account, "returnDepositsWithdrawals", cancel, parms);
			return ParseJsonReply<FundsTransfer>(jtok);
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public async Task<TradeResult> SubmitTrade(CurrencyPair pair, EOrderSide type, decimal price_per_coin, decimal volume_base, CancellationToken? cancel = null)
		{
			var jtok = await PostData(Method.Account, Conv.ToString(type), cancel, new Params
			{
				{ "currencyPair", pair.Id },
				{ "rate", price_per_coin },
				{ "amount", volume_base },
			});

			return ParseJsonReply<TradeResult>(jtok);
		}

		/// <summary>Cancel an order</summary>
		public async Task<bool> CancelTrade(CurrencyPair pair, long order_id, CancellationToken? cancel = null)
		{
			var jtok = await PostData(Method.Account, "cancelOrder", cancel, new Params
			{
				{ "currencyPair", pair.Id },
				{ "orderNumber", order_id },
			});

			return jtok is JObject jobj && jobj.Value<byte>("success") == 1;
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private async Task<JToken> GetData(string method, string command, CancellationToken? cancel, Params parameters = null)
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

					// Add the command to the parameters
					parameters = parameters ?? new Params();
					parameters["command"] = command;

					// Create the URL for the command + parameters
					var url = $"{UrlRestAddress}{method}{Http_.UrlEncode(parameters)}";

					// Submit the request
					var response = await Client.GetAsync(url, cancel_token);
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = await response.Content.ReadAsStringAsync();
					return JToken.Parse(reply);
				}
			}
		}

		/// <summary>Helper for POSTs</summary>
		private async Task<JToken> PostData(string method, string command, CancellationToken? cancel, Params parameters = null)
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
					parameters["command"] = command;
					parameters["nonce"] = Misc.Nonce;

					// Create the content to POST
					var post_data_string = Http_.UrlEncode(parameters).TrimStart('?');
					var content = new StringContent(post_data_string, Encoding.UTF8, "application/x-www-form-urlencoded");
					var msg_hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(post_data_string));
					var signature = Misc.ToStringHex(msg_hash);
					content.Headers.Add("Sign", signature);

					var url = $"{Client.BaseAddress}{method}{Http_.UrlEncode(parameters)}";

					// Submit the request
					// Result Codes:
					//  - 422 Un-processable Entity:
					//    Status code is directly reported by Poloniex server. It means the server understands the content type of the request entity,
					//    and the syntax of the request entity is correct, but was unable to process the contained instructions.
					var response = await Client.PostAsync(url, content, cancel_token);
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
				throw new PoloniexException(EErrorCode.Failure, error);

			// Parse the result
			return jtok.ToObject<T>();
		}

		private static class Method
		{
			public const string Public = "public";
			public const string Account = "tradingApi";
		}
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
