using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Bittrex.API.DomainObjects;
using Bittrex.API.Subscriptions;
using ExchApi.Common;
using Newtonsoft.Json.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.INet;
using Rylogic.Utility;

namespace Bittrex.API
{
	public class BittrexApi :ExchangeApi<HMACSHA512>
	{
		public BittrexApi(string key, string secret, CancellationToken shutdown, Logger log)
			:base(key, secret, shutdown, 10, "https://api.bittrex.com/", "https://socket.bittrex.com/signalr")
		{
			try
			{
				Log = new Logger("BittrexApi", log);
				WebSocket = new BittrexWebSocket(UrlSocketAddress, shutdown);
				MarketData = new MarketDataCache(this);
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public override void Dispose()
		{
			MarketData = null;
			WebSocket = null;
			Log = Util.Dispose(Log);
			base.Dispose();
		}
		public override async Task InitAsync()
		{
			// Start the web socket
			await WebSocket.InitAsync();

			// Authenticate the web socket if a key/secret is provided
			if (Key == null || Secret == null) return;
			await WebSocket.Authenticate(Key, Secret);
		}

		/// <summary>Static log instance</summary>
		public static Logger Log { get; private set; }

		#region WebSocket API

		/// <summary></summary>
		public BittrexWebSocket WebSocket
		{
			get { return m_web_socket; }
			private set
			{
				if (m_web_socket == value) return;
				Util.Dispose(ref m_web_socket);
				m_web_socket = value;
			}
		}
		private BittrexWebSocket m_web_socket;

		/// <summary>A local copy of the state of markets</summary>
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

		#endregion

		#region Public

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public async Task<List<MarketData>> GetMarkets(CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/public/getmarkets
			var jtok = await GetData(Method.Public, "getmarkets", cancel);
			return ParseJsonReply<List<MarketData>>(jtok);
		}

		/// <summary>Return the order book for a market</summary>
		public async Task<OrderBook> GetOrderBook(CurrencyPair pair, EGetOrderBookType type = EGetOrderBookType.Both, int depth = 20, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/public/getorderbook?market=BTC-LTC&type=both&depth=50  
			var jtok = await GetData(Method.Public, "getorderbook", cancel, new Params
			{
				{ "market", pair.Id },
				{ "type", type.ToString().ToLowerInvariant() },
				{ "depth", depth },
			});

			// Prevent null being returned
			var book = ParseJsonReply<OrderBook>(jtok);
			if (book != null)
			{
				book.Pair = pair;
				book.BuyOffers = book.BuyOffers ?? new List<OrderBook.Offer>();
				book.SellOffers = book.SellOffers ?? new List<OrderBook.Offer>();
			}

			return book;
		}

		#endregion

		#region Account

		/// <summary>Return the balances for the account</summary>
		public async Task<List<Balance>> GetBalances(CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/account/getbalances?apikey=API_KEY
			var jtok = await GetData(Method.Account, "getbalances", cancel);
			return ParseJsonReply<List<Balance>>(jtok);
		}

		/// <summary>Return the orders currently opened</summary>
		public async Task<List<Order>> GetOpenOrders(CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY
			var jtok = await GetData(Method.Market, "getopenorders", cancel);
			return ParseJsonReply<List<Order>>(jtok);
		}
		public async Task<List<Order>> GetOpenOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY&market=BTC-LTC
			var jtok = await GetData(Method.Market, "getopenorders", cancel, new Params
			{
				{ "market", pair.Id },
			});

			return ParseJsonReply<List<Order>>(jtok);
		}
		
		/// <summary>Return the history of trades made on the account</summary>
		public async Task<List<Trade>> GetTradeHistory(CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/account/getorderhistory
			var jtok = await GetData(Method.Account, "getorderhistory", cancel);
			return ParseJsonReply<List<Trade>>(jtok);
		}

		/// <summary>Get the history of deposits</summary>
		public async Task<List<Transfer>> GetDeposits(string currency = null, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/account/getdeposithistory?currency=BTC
			var parms = new Params();
			if (currency != null) parms["currency"] = currency;
			var jtok = await GetData(Method.Account, "getdeposithistory", cancel, parms);
			return ParseJsonReply<List<Transfer>>(jtok);
		}

		/// <summary>Get the history of withdrawals</summary>
		public async Task<List<Transfer>>GetWithdrawals(string currency = null, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/account/getwithdrawalhistory?currency=BTC
			var parms = new Params();
			if (currency != null) parms["currency"] = currency;
			var jtok = await GetData(Method.Account, "getwithdrawalhistory", cancel, parms);
			return ParseJsonReply<List<Transfer>>(jtok);
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public async Task<TradeResult> SubmitTrade(CurrencyPair pair, EOrderSide type, double price_q2b, double amount_base, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/market/selllimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3
			// https://api.bittrex.com/api/v1.1/market/buylimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3
			var jtok = await GetData(Method.Market, Conv.ToString(type), cancel, new Params
			{
				{ "market", pair.Id },
				{ "rate", price_q2b },
				{ "quantity", amount_base },
			});
			return ParseJsonReply<TradeResult>(jtok);
		}

		/// <summary>Cancel an order</summary>
		public async Task<TradeResult> CancelTrade(CurrencyPair pair, Guid uuid, CancellationToken? cancel = null)
		{
			// https://api.bittrex.com/api/v1.1/market/cancel?apikey=API_KEY&uuid=ORDER_UUID    
			var jtok = await GetData(Method.Market, "cancel", cancel, new Params
			{
				{ "uuid", uuid },
			});

			// Return an empty trade result if there is no order with that Id
			if (jtok is JObject jobj &&
				jobj["success"].Value<bool>() == false &&
				jobj["message"].Value<string>() == "ORDER_NOT_OPEN")
				return new TradeResult();

			return ParseJsonReply<TradeResult>(jtok);
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private async Task<JToken> GetData(string method, string command, CancellationToken? cancel, Params parameters = null)
		{
			// If called from the UI thread, disable the SynchronisationContext
			// to prevent deadlocks when waiting for Async results.
			using (Task_.NoSyncContext())
			{
				var cancel_token = CancellationTokenSource.CreateLinkedTokenSource(Shutdown, cancel ?? CancellationToken.None).Token;
				using (RequestThrottle.Lock(cancel_token)) // Limit requests to the required rate
				{
					await RequestThrottle.Wait(cancel_token);

					// Add the API key for non-public methods
					parameters = parameters ?? new Params();
					if (method != Method.Public)
					{
						parameters["apikey"] = Key;
						parameters["nonce"] = Misc.Nonce;
					}

					// Create the URL for the command + parameters
					var url = $"{UrlRestAddress}api/v1.1/{method}/{command}{Http_.UrlEncode(parameters)}";

					// Construct the GET request
					var req = new HttpRequestMessage(HttpMethod.Get, url);
					if (method != Method.Public)
					{
						var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(url));
						req.Headers.Add("apisign", Misc.ToStringHex(hash));
					}

					// Submit the request
					var response = await Client.SendAsync(req, cancel_token);
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = await response.Content.ReadAsStringAsync();
					return JToken.Parse(reply);
				}
			}
		}

		/// <summary>Interpret a JSON reply</summary>
		private T ParseJsonReply<T>(JToken jtok)
		{
			if (!(jtok is JObject jobj))
				throw new BittrexException(EErrorCode.ReplyWasNotAJsonObject, "The reply did not contain a json object");

			// Check for success
			if (jobj["success"].Value<bool>() != true)
			{
				var message = jobj["message"]?.Value<string>();
				throw new BittrexException(EErrorCode.Failure, message);
			}

			// Parse the result
			return jobj["result"].ToObject<T>();
		}

		private static class Method
		{
			public const string Public = "public";
			public const string Account = "account";
			public const string Market = "market";
		}
		public enum EGetOrderBookType
		{
			Buy,
			Sell,
			Both,
		}
	}
}
