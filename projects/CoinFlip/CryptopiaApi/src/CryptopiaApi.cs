using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Cryptopia.API
{
	public class CryptopiaApi :IDisposable
	{
		private string UrlBaseAddress;
		private readonly string m_key;
		private readonly string m_secret;
		private JsonSerializer m_json;
		private CancellationToken m_cancel_token;

		public CryptopiaApi(CancellationToken cancel_token, string base_address = "https://www.cryptopia.co.nz/")
			:this(null, null, cancel_token, base_address)
		{}
		public CryptopiaApi(string key, string secret, CancellationToken cancel_token, string base_address = "https://www.cryptopia.co.nz/")
		{
			m_key = key;
			m_secret = secret;
			m_cancel_token = cancel_token;
			UrlBaseAddress = base_address;
			ServerRequestRateLimit = 10;
			Client = new HttpClient();
			m_json = new JsonSerializer { NullValueHandling = NullValueHandling.Ignore };
			Hasher = m_secret != null ? new HMACSHA256(Convert.FromBase64String(m_secret)) : null;
			m_request_sw = new Stopwatch();
			m_request_sw.Start();
		}
		public virtual void Dispose()
		{
			Client = null;
		}

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit { get; set; }

		/// <summary>Hasher</summary>
		private HMACSHA256 Hasher { get; set; }

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
					m_client.BaseAddress = new Uri(UrlBaseAddress);
					m_client.Timeout = TimeSpan.FromSeconds(10);
				}
			}
		}
		private HttpClient m_client;

		#region Public

		/// <summary>Return all available currencies</summary>
		public CurrenciesResponse GetCurrencies(CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetCurrencies
			return GetData<CurrenciesResponse>(HttpMethod.Get, "GetCurrencies", cancel);
		}

		/// <summary>Return all available trading pairs</summary>
		public TradePairsResponse GetTradePairs(CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetTradePairs
			return GetData<TradePairsResponse>(HttpMethod.Get, "GetTradePairs", cancel);
		}

		/// <summary>Return the available market data for all markets (or markets with 'base_currency' as the base)</summary>
		public MarketsResponse GetMarkets(string base_currency = null, int? hours = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarkets
			// https://www.cryptopia.co.nz/api/GetMarkets/BTC
			// https://www.cryptopia.co.nz/api/GetMarkets/12
			// https://www.cryptopia.co.nz/api/GetMarkets/BTC/12
			var command = "GetMarkets";
			if (base_currency != null) command += $"/{base_currency}";
			if (hours         != null) command += $"/{hours.Value}";
			return GetData<MarketsResponse>(HttpMethod.Get, command, cancel);
		}

		/// <summary>Returns market data for the specified trade pair</summary>
		public MarketResponse GetMarket(int trade_pair_id, int? hours = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarket/100
			// https://www.cryptopia.co.nz/api/GetMarket/100/6
			var command = $"GetMarket/{trade_pair_id}";
			if (hours != null) command += $"/{hours.Value}";
			return GetData<MarketResponse>(HttpMethod.Get, command, cancel);
		}
		public MarketResponse GetMarket(CurrencyPair pair, int? hours = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarket/DOT_BTC
			// https://www.cryptopia.co.nz/api/GetMarket/DOT_BTC/6
			var command = $"GetMarket/{pair.Id}";
			if (hours != null) command += $"/{hours.Value}";
			return GetData<MarketResponse>(HttpMethod.Get, command, cancel);
		}

		/// <summary>Returns the market history data for the specified trade pair. 'hours' defaults to 24 if not given</summary>
		public MarketHistoryResponse GetMarketHistory(int trade_pair_id, int? hours = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketHistory/100
			// https://www.cryptopia.co.nz/api/GetMarketHistory/100/48
			var command = $"GetMarketHistory/{trade_pair_id}";
			if (hours != null) command += $"/{hours.Value}";
			return GetData<MarketHistoryResponse>(HttpMethod.Get, command, cancel);
		}
		public MarketHistoryResponse GetMarketHistory(CurrencyPair pair, int? hours = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketHistory/DOT_BTC/48
			// https://www.cryptopia.co.nz/api/GetMarketHistory/DOT_BTC
			var command = $"GetMarketHistory/{pair.Id}";
			if (hours != null) command += $"/{hours.Value}";
			return GetData<MarketHistoryResponse>(HttpMethod.Get, command, cancel);
		}

		/// <summary>Returns the open buy and sell orders for the specified trade pair. 'order_count' defaults to 100 if not given</summary>
		public MarketOrdersResponse GetMarketOrders(int trade_pair_id, int? order_count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketOrders/100
			// https://www.cryptopia.co.nz/api/GetMarketOrders/100/50
			var command = $"GetMarketOrders/{trade_pair_id}";
			if (order_count != null) command += $"{order_count.Value}";
			return GetData<MarketOrdersResponse>(HttpMethod.Get, command, cancel);
		}
		public MarketOrdersResponse GetMarketOrders(CurrencyPair pair, int? order_count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketOrders/DOT_BTC
			// https://www.cryptopia.co.nz/api/GetMarketOrders/DOT_BTC/50
			var command = $"GetMarketOrders/{pair.Id}";
			if (order_count != null) command += $"{order_count.Value}";
			return GetData<MarketOrdersResponse>(HttpMethod.Get, command, cancel);
		}

		/// <summary>Returns the open buy and sell orders for the specified markets. 'order_count' defaults to 100 if not given</summary>
		public MarketOrderGroupsResponse GetMarketOrderGroups(IEnumerable<int> trade_pair_ids, int? order_count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketOrderGroups/100-101-102-103
			// https://www.cryptopia.co.nz/api/GetMarketOrderGroups/100-101-102-103/50
			var command = $"GetMarketOrderGroups/{string.Join("-", trade_pair_ids)}";
			if (order_count != null) command += $"/{order_count.Value}";
			return GetData<MarketOrderGroupsResponse>(HttpMethod.Get, command, cancel);
		}
		public MarketOrderGroupsResponse GetMarketOrderGroups(IEnumerable<CurrencyPair> pairs, int? order_count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetMarketOrderGroups/DOT_BTC-DOT_LTC-DOT_DOGE-DOT_UNO
			// https://www.cryptopia.co.nz/api/GetMarketOrderGroups/DOT_BTC-DOT_LTC-DOT_DOGE-DOT_UNO/50
			var command = $"GetMarketOrderGroups/{string.Join("-", pairs.Select(x => x.Id))}";
			if (order_count != null) command += $"/{order_count.Value}";
			return GetData<MarketOrderGroupsResponse>(HttpMethod.Get, command, cancel);
		}

		#endregion

		#region Account

		/// <summary>Returns all balances or a specific currency balance</summary>
		public BalanceResponse GetBalances(CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetBalance 
			return GetData<BalanceResponse>(HttpMethod.Post, "GetBalance", cancel,
				new KV("Currency", null),
				new KV("CurrencyId", null));
		}
		public BalanceResponse GetBalances(string currency, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetBalance 
			return GetData<BalanceResponse>(HttpMethod.Post, "GetBalance", cancel,
				new KV("Currency", currency),
				new KV("CurrencyId", null));
		}
		public BalanceResponse GetBalances(int currency_id, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetBalance 
			return GetData<BalanceResponse>(HttpMethod.Post, "GetBalance", cancel,
				new KV("Currency", null),
				new KV("CurrencyId", currency_id));
		}

		/// <summary>Returns a list of open orders for all trade pairs or specified trade pair. 'count' defaults to 100 if not given</summary>
		public OpenOrdersResponse GetOpenOrders(int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetOpenOrders
			return GetData<OpenOrdersResponse>(HttpMethod.Post, "GetOpenOrders", cancel,
				new KV("Market", null),
				new KV("TradePairId", null),
				new KV("Count", count));
		}
		public OpenOrdersResponse GetOpenOrders(CurrencyPair pair, int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetOpenOrders
			return GetData<OpenOrdersResponse>(HttpMethod.Post, "GetOpenOrders", cancel,
				new KV("Market", pair.Id),
				new KV("TradePairId", null),
				new KV("Count", count));
		}
		public OpenOrdersResponse GetOpenOrders(int trade_pair_id, int? count = null, CancellationToken? cancel = null)
		{
			return GetData<OpenOrdersResponse>(HttpMethod.Post, "GetOpenOrders", cancel,
				new KV("Market", null),
				new KV("TradePairId", trade_pair_id),
				new KV("Count", count));
		}

		/// <summary>Cancels a single order, all orders for a trade pair or all open orders</summary>
		public CancelTradeResponse CancelTrade(ECancelTradeType type, int? order_id = null, int? trade_pair_id = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/CancelTrade
			return GetData<CancelTradeResponse>(HttpMethod.Post, "CancelTrade", cancel,
				new KV("Type", type.ToString()),
				new KV("TradePairId", trade_pair_id),
				new KV("OrderId", order_id));
		}

		/// <summary>Submits a new trade order</summary>
		public SubmitTradeResponse SubmitTrade(EOrderType type, CurrencyPair pair, decimal amount, decimal rate, CancellationToken? cancel = null)
		{
			return GetData<SubmitTradeResponse>(HttpMethod.Post, "SubmitTrade", cancel,
				new KV("Type", type.ToString()),
				new KV("Market", pair.Id),
				new KV("TradePairId", null),
				new KV("Amount", amount),
				new KV("Rate", rate));
		}
		public SubmitTradeResponse SubmitTrade(EOrderType type, int trade_pair_id, decimal amount, decimal rate, CancellationToken? cancel = null)
		{
			return GetData<SubmitTradeResponse>(HttpMethod.Post, "SubmitTrade", cancel,
				new KV("Type", type.ToString()),
				new KV("Market", null),
				new KV("TradePairId", trade_pair_id),
				new KV("Amount", amount),
				new KV("Rate", rate));
		}

		/// <summary>Returns a list of trade history for all trade pairs or specified trade pair</summary>
		public TradeHistoryResponse GetTradeHistory(int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetTradeHistory
			return GetData<TradeHistoryResponse>(HttpMethod.Post, "GetTradeHistory", cancel,
				new KV("Market", null),
				new KV("TradePairId", null),
				new KV("Count", count));
		}
		public TradeHistoryResponse GetTradeHistory(CurrencyPair pair, int? count = null, CancellationToken? cancel = null)
		{
			return GetData<TradeHistoryResponse>(HttpMethod.Post, "GetTradeHistory", cancel,
				new KV("Market", pair.Id),
				new KV("TradePairId", null),
				new KV("Count", count));
		}
		public TradeHistoryResponse GetTradeHistory(int trade_pair_id, int? count = null, CancellationToken? cancel = null)
		{
			return GetData<TradeHistoryResponse>(HttpMethod.Post, "GetTradeHistory", cancel,
				new KV("Market", null),
				new KV("TradePairId", trade_pair_id),
				new KV("Count", count));
		}

		/// <summary>Returns a list of transactions. 'count' defaults to 100 if not given</summary>
		public TransactionResponse GetTransactions(ETransactionType type, int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetTransactions
			return GetData<TransactionResponse>(HttpMethod.Post, "GetTransactions", cancel,
				new KV("Type", type.ToString()),
				new KV("Count", count));
		}

		/// <summary>Creates or returns a deposit address for the specified currency</summary>
		public DepositAddressResponse GetDepositAddress(string currency, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetDepositAddress
			return GetData<DepositAddressResponse>(HttpMethod.Post, "GetDepositAddress", cancel,
				new KV("Currency", currency),
				new KV("CurrencyId", null));
		}
		public DepositAddressResponse GetDepositAddress(int currency_id, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetDepositAddress
			return GetData<DepositAddressResponse>(HttpMethod.Post, "GetDepositAddress", cancel,
				new KV("Currency", null),
				new KV("CurrencyId", currency_id));
		}

		/// <summary>Submits a withdrawal request</summary>
		public SubmitWithdrawResponse SubmitWithdraw(string currency, decimal amount, string address, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/SubmitWithdraw
			return GetData<SubmitWithdrawResponse>(HttpMethod.Post, "SubmitWithdraw", cancel,
				new KV("Currency", currency),
				new KV("CurrencyId", null),
				new KV("Amount", amount),
				new KV("Address", address));
		}
		public SubmitWithdrawResponse SubmitWithdraw(int currency_id, decimal amount, string address, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/SubmitWithdraw
			return GetData<SubmitWithdrawResponse>(HttpMethod.Post, "SubmitWithdraw", cancel,
				new KV("Currency",  null),
				new KV("CurrencyId", currency_id),
				new KV("Amount", amount),
				new KV("Address", address));
		}

		/// <summary>The history of deposits and withdrawals</summary>
		public TransactionResponse GetDeposits(int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetTransactions
			return GetData<TransactionResponse>(HttpMethod.Post, "GetTransactions", cancel,
				new KV("Type","Deposit"),
				new KV("Count", count));
		}
		public TransactionResponse GetWithdrawals(int? count = null, CancellationToken? cancel = null)
		{
			// https://www.cryptopia.co.nz/api/GetTransactions
			return GetData<TransactionResponse>(HttpMethod.Post, "GetTransactions", cancel,
				new KV("Type","Withdraw"),
				new KV("Count", count));
		}

		#endregion

		/// <summary>Helper for GETs</summary>
		private T GetData<T>(HttpMethod method, string command, CancellationToken? cancel, params KV[] parameters)
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
					var url = $"{UrlBaseAddress}api/{command}";

					// Construct the request
					var req = new HttpRequestMessage(method, url);

					// Add parameters as string content
					if (parameters.Length != 0)
						req.Content = new StringContent(Misc.JsonEncode(parameters), Encoding.UTF8, "application/json");

					// Add authentication data for Posts
					if (method == HttpMethod.Post)
					{
						// Hashing the request body
						var content_hash_b64 = string.Empty;
						if (parameters.Length != 0)
						{
							using (var md5 = MD5.Create())
							{
								var content = req.Content.ReadAsByteArrayAsync().Result;
								var content_hash = md5.ComputeHash(content);
								content_hash_b64 = Convert.ToBase64String(content_hash);
							}
						}

						// Create random nonce for each request
						var nonce = Misc.Nonce;

						// Create the signature
						var uri = HttpUtility.UrlEncode(req.RequestUri.AbsoluteUri.ToLower());
						var signature = $"{m_key}POST{uri}{nonce}{content_hash_b64}";
						var signature_bytes = Encoding.UTF8.GetBytes(signature);
						var signature_hash = Hasher.ComputeHash(signature_bytes);

						// Setting the values in the Authorization header using custom scheme 'amx'
						req.Headers.Authorization = new AuthenticationHeaderValue("amx", $"{m_key}:{Convert.ToBase64String(signature_hash)}:{nonce}");
					}

					// Submit the request
					var response = Client.SendAsync(req, cancel_token).Result;
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
		private int m_blocked_count;

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
}
