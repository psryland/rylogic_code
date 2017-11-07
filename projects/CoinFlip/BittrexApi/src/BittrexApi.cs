using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class BittrexApi :IDisposable
	{
		private string UrlBaseAddress;
		private HttpClient m_client;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;
		private readonly string m_key;
		private readonly string m_secret;

		public BittrexApi(CancellationToken cancel_token, string base_address = "https://bittrex.com/")
			:this(null, null, cancel_token, base_address)
		{}
		public BittrexApi(string key, string secret, CancellationToken cancel_token, string base_address = "https://bittrex.com/")
		{
			m_key = key;
			m_secret = secret;
			UrlBaseAddress = base_address;
			ServerRequestRateLimit = 10;
			m_cancel_token = cancel_token;
			m_client = new HttpClient { BaseAddress = new Uri(UrlBaseAddress), Timeout = TimeSpan.FromSeconds(10) };
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			Hasher = m_secret != null ? new HMACSHA512(Encoding.ASCII.GetBytes(m_secret)) : null;
			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			if (m_client != null)
			{
				m_client.Dispose();
				m_client = null;
			}
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }

		/// <summary>Hasher</summary>
		private HMACSHA512 Hasher { get; set; }

		#region REST API Functions

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

		#region Public

		//https://bittrex.com/api/v1.1/public/getcurrencies 

		/// <summary>Return all available trading pairs and their latest price data</summary>
		public MarketsResponse GetMarkets(CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/public/getmarkets
			return GetData<MarketsResponse>(Method.Public, "getmarkets", cancel);
		}

		/// <summary>Return the order book for a market</summary>
		public OrderBookResponse GetOrderBook(CurrencyPair pair, EGetOrderBookType type = EGetOrderBookType.Both, int depth = 20, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/public/getorderbook?market=BTC-LTC&type=both&depth=50  
			var response = GetData<OrderBookResponse>(Method.Public, "getorderbook", cancel,
				new KV("market", pair.Id),
				new KV("type", type.ToString().ToLowerInvariant()),
				new KV("depth", depth));

			// Prevent null being returned
			if (response.Data != null)
			{
				response.Data.Pair = pair;
				response.Data.BuyOrders = response.Data.BuyOrders ?? new List<OrderBook.Order>();
				response.Data.SellOrders = response.Data.SellOrders ?? new List<OrderBook.Order>();
			}

			return response;
		}

		#endregion

		#region Account

		/// <summary>Return the balances for the account</summary>
		public BalanceResponse GetBalances(CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/account/getbalances?apikey=API_KEY
			return GetData<BalanceResponse>(Method.Account, "getbalances", cancel);
		}

		/// <summary>Return the history of trades made on the account</summary>
		public PositionHistoryResponse GetTradeHistory(CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/account/getorderhistory
			return GetData<PositionHistoryResponse>(Method.Account, "getorderhistory", cancel);
		}

		/// <summary>Get the history of deposits</summary>
		public TransferHistoryResponse GetDeposits(string currency = null, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/account/getdeposithistory?currency=BTC
			var parms = new List<KV>{ };
			if (currency != null)
				parms.Add(new KV("currency", currency));

			return GetData<TransferHistoryResponse>(Method.Account, "getdeposithistory", cancel, parms.ToArray());
		}

		/// <summary>Get the history of withdrawals</summary>
		public TransferHistoryResponse GetWithdrawals(string currency = null, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/account/getwithdrawalhistory?currency=BTC
			var parms = new List<KV>{ };
			if (currency != null)
				parms.Add(new KV("currency", currency));

			return GetData<TransferHistoryResponse>(Method.Account, "getwithdrawalhistory", cancel, parms.ToArray());
		}

		#endregion

		#region Market

		/// <summary>Return the orders currently opened</summary>
		public PositionsResponse GetOpenOrders(CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY
			return GetData<PositionsResponse>(Method.Market, "getopenorders", cancel);
		}
		public PositionsResponse GetOpenOrders(CurrencyPair pair, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY&market=BTC-LTC
			return GetData<PositionsResponse>(Method.Market, "getopenorders", cancel,
				new KV("market", pair.Id));
		}
		
		/// <summary>Cancel an order</summary>
		public CancelTradeResponse CancelTrade(CurrencyPair pair, Guid uuid, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/market/cancel?apikey=API_KEY&uuid=ORDER_UUID    
			return GetData<CancelTradeResponse>(Method.Market, "cancel", cancel,
				new KV("uuid", uuid));
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public SubmitTradeResponse SubmitTrade(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_base, CancellationToken? cancel = null)
		{
			// https://bittrex.com/api/v1.1/market/selllimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3  
			// https://bittrex.com/api/v1.1/market/buylimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3 
			return GetData<SubmitTradeResponse>(Method.Market, Misc.ToString(type), cancel,
				new KV("market", pair.Id),
				new KV("rate", price_per_coin),
				new KV("quantity", volume_base));
		}


		#endregion

		/// <summary>Helper for GETs</summary>
		private T GetData<T>(string method, string command, CancellationToken? cancel, params KV[] parameters)
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
					var request_period_ms = 1000 / ServerRequestRateLimit;
					for (; m_request_sw.ElapsedMilliseconds - m_last_request_ms < request_period_ms; Thread.Yield()) { }
					m_last_request_ms = m_request_sw.ElapsedMilliseconds;

					// Add the API key for non-public methods
					var kv = new List<KV>();
					if (method != Method.Public)
					{
						kv.Add(new KV("apikey", m_key));
						kv.Add(new KV("nonce", Misc.Nonce));
					}
					kv.AddRange(parameters);

					// Create the URL for the command + parameters
					var url = $"{UrlBaseAddress}api/v1.1/{method}/{command}{Misc.UrlEncode(kv)}";

					// Construct the GET request
					var req = new HttpRequestMessage(HttpMethod.Get, url);
					if (method != Method.Public)
					{
						var hash = Hasher.ComputeHash(Encoding.UTF8.GetBytes(url));
						req.Headers.Add("apisign", Misc.ToStringHex(hash));
					}

					// Submit the request
					var response = m_client.SendAsync(req, cancel_token).Result;
					if (!response.IsSuccessStatusCode)
						throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

					// Interpret the reply
					var reply = response.Content.ReadAsStringAsync().Result;
					using (var tr = new JsonTextReader(new StringReader(reply)))
						return m_json.Deserialize<T>(tr);
				}
			}
		}
		private SemaphoreSlim m_lock = new SemaphoreSlim(1,1);
		private Stopwatch m_request_sw;
		private long m_last_request_ms;
		private int m_blocked_count;

		#endregion
	}
}
