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
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_address;
			ServerRequestRateLimit = 10;
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
		public MarketsResponse GetMarkets()
		{
			// https://bittrex.com/api/v1.1/public/getmarkets
			return GetData<MarketsResponse>(Method.Public, "getmarkets");
		}
		public Task<MarketsResponse> GetMarketsAsync()
		{
			return Task.Run(() => GetMarkets(), m_cancel_token);
		}

		/// <summary>Return the order book for a market</summary>
		public OrderBookResponse GetOrderBook(CurrencyPair pair, EGetOrderBookType type = EGetOrderBookType.Both, int depth = 20)
		{
			// https://bittrex.com/api/v1.1/public/getorderbook?market=BTC-LTC&type=both&depth=50  
			var response = GetData<OrderBookResponse>(Method.Public, "getorderbook",
				new KV("market", pair.Id),
				new KV("type", type.ToString().ToLowerInvariant()),
				new KV("depth", depth));
			if (response.Data != null)
				response.Data.Pair = pair;

			return response;
		}
		public Task<OrderBookResponse> GetOrderBookAsync(CurrencyPair pair, EGetOrderBookType type = EGetOrderBookType.Both, int depth = 20)
		{
			return Task.Run(() => GetOrderBook(pair, type, depth), m_cancel_token);
		}

		#endregion

		#region Account

		/// <summary>Return the balances for the account</summary>
		public BalanceResponse GetBalances()
		{
			// https://bittrex.com/api/v1.1/account/getbalances?apikey=API_KEY
			return GetData<BalanceResponse>(Method.Account, "getbalances");
		}
		public Task<BalanceResponse> GetBalancesAsync()
		{
			return Task.Run(() => GetBalances(), m_cancel_token);
		}

		/// <summary>Return the history of trades made on the account</summary>
		public PositionHistoryResponse GetTradeHistory()
		{
			// https://bittrex.com/api/v1.1/account/getorderhistory
			return GetData<PositionHistoryResponse>(Method.Account, "getorderhistory");
		}
		public Task<PositionHistoryResponse> GetTradeHistoryAsync()
		{
			return Task.Run(() => GetTradeHistory(), m_cancel_token);
		}

		#endregion

		#region Market

		/// <summary>Return the orders currently opened</summary>
		public PositionsResponse GetOpenOrders()
		{
			// https://bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY
			return GetData<PositionsResponse>(Method.Market, "getopenorders");
		}
		public PositionsResponse GetOpenOrders(CurrencyPair pair)
		{
			// https://bittrex.com/api/v1.1/market/getopenorders?apikey=API_KEY&market=BTC-LTC
			return GetData<PositionsResponse>(Method.Market, "getopenorders", new KV("market", pair.Id));
		}
		public Task<PositionsResponse> GetOpenOrdersAsync()
		{
			return Task.Run(() => GetOpenOrders(), m_cancel_token);
		}
		public Task<PositionsResponse> GetOpenOrdersAsync(CurrencyPair pair)
		{
			return Task.Run(() => GetOpenOrders(pair), m_cancel_token);
		}
		
		/// <summary>Cancel an order</summary>
		public CancelTradeResponse CancelTrade(CurrencyPair pair, Guid uuid)
		{
			// https://bittrex.com/api/v1.1/market/cancel?apikey=API_KEY&uuid=ORDER_UUID    
			return GetData<CancelTradeResponse>(Method.Market, "cancel", new KV("uuid", uuid));
		}
		public Task<CancelTradeResponse> CancelTradeAsync(CurrencyPair pair, Guid uuid)
		{
			return Task.Run(() => CancelTrade(pair, uuid), m_cancel_token);
		}

		/// <summary>Create an order to buy/sell. Returns a unique order ID</summary>
		public SubmitTradeResponse SubmitTrade(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_base)
		{
			// https://bittrex.com/api/v1.1/market/selllimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3  
			// https://bittrex.com/api/v1.1/market/buylimit?apikey=API_KEY&market=BTC-LTC&quantity=1.2&rate=1.3 
			return GetData<SubmitTradeResponse>(Method.Market, Misc.ToString(type),
				new KV("market", pair.Id),
				new KV("rate", price_per_coin),
				new KV("quantity", volume_base));
		}
		public Task<SubmitTradeResponse> SubmitTradeAsync(CurrencyPair pair, EOrderType type, decimal price_per_coin, decimal volume_base)
		{
			return Task.Run(() => SubmitTrade(pair, type, price_per_coin, volume_base), m_cancel_token);
		}


		#endregion

		/// <summary>Helper for GETs</summary>
		private T GetData<T>(string method, string command, params KV[] parameters)
		{
			Debug.Assert(!m_cancel_token.IsCancellationRequested, "Shouldn't be making new requests when shutdown is signalled");

			lock (m_lock)
			{
				m_cancel_token.ThrowIfCancellationRequested();

				// Limit requests to the required rate
				var request_period_ms = 1000 / ServerRequestRateLimit;
				for (; m_request_sw.ElapsedMilliseconds - m_last_request_ms < request_period_ms; Thread.Yield()){}
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
				var response = Task.Run(() => m_client.SendAsync(req, m_cancel_token), m_cancel_token).Result;
				if (!response.IsSuccessStatusCode)
					throw new HttpException((int)response.StatusCode, response.ReasonPhrase);

				// Interpret the reply
				var reply = response.Content.ReadAsStringAsync().Result;
				using (var tr = new JsonTextReader(new StringReader(reply)))
					return m_json.Deserialize<T>(tr);
			}
		}
		private object m_lock = new object();
		private Stopwatch m_request_sw;
		private long m_last_request_ms;

		#endregion
	}
}
