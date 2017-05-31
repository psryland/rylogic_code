using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pr.extn;
using Cryptopia.API;
using Cryptopia.API.DataObjects;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using System.Net.Http.Headers;
using System.Web;
using System.Diagnostics;

namespace CoinFlip
{
	/// <summary>Base class for exchanges</summary>
	public abstract class Exchange
	{
		public Exchange(Model model)
		{
			Model = model;
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>Exchanges update their trade data and account balances</summary>
		public abstract Task UpdateTradeDataAsync();

		/// <summary>Return the account balance for the given coin type</summary>
		public abstract Balance Balance(Coin coin);

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}
	}

	/// <summary>Cryptopia Exchange</summary>
	public class Cryptopia :Exchange
	{
		private const string ApiKey    = "429162d7138f4275b5e9dd09ff6362fd";
		private const string ApiSecret = "Dt6k0ZC3zqbNCxpDSsHqX7VIR21PEPO7vNeKDbQIKcI=";

		private CryptopiaApiPublic m_pub;
		private CryptopiaApiPrivate m_priv;
		private Dictionary<Coin, Balance> m_balance;

		public Cryptopia(Model model)
			:base(model)
		{
			m_pub = new CryptopiaApiPublic();
			m_priv = new CryptopiaApiPrivate(ApiKey, ApiSecret);
			m_balance = new Dictionary<Coin, Balance>();
			Task.Run(() => PopulateAsync());
		}

		/// <summary>Get the trading pairs</summary>
		private async void PopulateAsync()
		{
			// Get all available trading pairs
			var msg = await m_pub.GetTradePairs();
			if (!msg.Success)
				throw new Exception("Cryptopia: Failed to read available trading pairs. {0}".Fmt(msg.Error));

			// Build a set of the pairs that involve the coins of interest
			var coi = Model.CoinsOfInterest.ToHashSet(x => x);
			var pairs = msg.Data.Where(x => coi.Contains(x.Symbol) && coi.Contains(x.BaseSymbol)).ToArray();

			// Create the trade pairs and associated coins
			var coins = new Dictionary<string, Coin>();
			var instr = new List<TradePair>();
			foreach (var pair in pairs)
			{
				// Get the coins involved in the pair.
				// Note: Cryptopia has Base/Quote backwards
				var quote = coins.GetOrAdd(pair.BaseSymbol, k => new Coin(k, this));
				var base_ = coins.GetOrAdd(pair.Symbol    , k => new Coin(k, this));

				// Add the trade pair
				instr.Add(new TradePair(base_, quote){ TradePairId = pair.Id });
			}

			// Add the coins and pairs to the model
			Model.Coins.AddRange(coins.Values);
			Model.Pairs.AddRange(instr);
		}

		/// <summary>Exchanges update their trade data and account balances</summary>
		public async override Task UpdateTradeDataAsync()
		{
			// Get the pairs associated with this exchange, no pairs, then don't need other data either
			var pairs = Model.Pairs.Where(x => x.Base.Exchange == this && x.Quote.Exchange == this).ToArray();
			if (pairs.Length == 0)
				return;

			// Request the market data for all of the pairs
			var market_data = m_pub.GetMarketOrderGroups(new MarketOrderGroupsRequest(pairs.Select(x => x.TradePairId).ToArray()));

			// Request the account data
			//var balances_data = await m_priv.GetBalances(new BalanceRequest(100));
			//		JsonQueryAsync<int>("https://www.cryptopia.co.nz/Api/GetBalance", new{ Currency = "DOT" });

			/// <summary>Make a private API web request</summary>
			{
				// Create a request message
				var url = "https://www.cryptopia.co.nz/Api/GetBalance";
				var req = new HttpRequestMessage(HttpMethod.Post, url);

				using (var hmac = new HMACSHA256(Convert.FromBase64String(ApiSecret)))
				using (var md5 = MD5.Create())
				{
					// Set the POST parameters
					////	req.Content = new System.Net.Http.ObjectContent(typeof(object), post_data, new JsonMediaTypeFormatter());
					//string requestContentBase64String = string.Empty;
					//if (req.Content != null)
					//{
					//	// Hash content to ensure message integrity
					//	using (var md5 = MD5.Create())
					//		requestContentBase64String = Convert.ToBase64String(md5.ComputeHash(await req.Content.ReadAsByteArrayAsync()));
					//}
					var post_params = string.Empty;
					var hashed_post_params = md5.ComputeHash(Encoding.UTF8.GetBytes(post_params));

					// Create a random nonce for each request
					var nonce = Guid.NewGuid().ToString("N");

					// Create the request signature
					var sig = ApiKey + "POST" + url + nonce + Convert.ToBase64String(hashed_post_params);
					var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(sig));

					// Authentication
					var parm = "{0}:{1}:{2}".Fmt(ApiKey, Convert.ToBase64String(signature), nonce);
					req.Headers.Authorization = new AuthenticationHeaderValue("amx", parm);
				}

				// Serialise the message to a string so we can calculate the signature
				//var url = HttpUtility.UrlEncode(req.RequestUri.AbsoluteUri.ToLower());


				// Send Request
				using (var client = new HttpClient())
				{
					var res = await client.SendAsync(req);
					if (!res.IsSuccessStatusCode) throw new Exception("Cryptopia: Private API request failed");
					//return await Deserialise<T>(res);
					var content = await res.Content.ReadAsStringAsync();
					Debug.WriteLine(content);
				}
			}


			// Process the market data and update the pairs
			#region
			{
				var msg = await market_data;
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to update trading pairs. {0}".Fmt(msg.Error));

				// Update the market orders
				foreach (var orders in msg.Data)
				{
					// Get the currencies involved in the pair
					var coin = orders.Market.Split('_');

					// Find the pair to update
					var pair = pairs.First(x =>
						(x.Base == coin[0] && x.Quote == coin[1]) ||
						(x.Base == coin[1] && x.Quote == coin[0]));

					// Update the depth of market data
					pair.Ask.Offers.Clear();
					foreach (var sell in orders.Sell)
						pair.Ask.Offers.Add(new Orders.Offer(sell.Price, sell.Volume));

					// Update the depth of market data
					pair.Bid.Offers.Clear();
					foreach (var buy in orders.Buy)
						pair.Bid.Offers.Add(new Orders.Offer(buy.Price, buy.Volume));
				}
			}
			#endregion

			// Process the account data and update the balances
			#region
			//{
			//	var msg = await balances_data;
			//	if (!msg.Success)
			//		throw new Exception("Cryptopia: Failed to update account balances pairs. {0}".Fmt(msg.Error));

			//	// Update the account balance
			//	foreach (var b in msg.Data)
			//	{
			//		// Find the currency that this balance is for
			//		var coin = Model.Coins.First(x => x.Exchange == this && x.Symbol == b.Symbol);

			//		// Update the balance
			//		var bal = new Balance(coin, b.Total, b.Available, b.Unconfirmed, b.HeldForTrades, b.PendingWithdraw);
			//		m_balance[coin] = bal;
			//	}
			//}
			#endregion
		}

		/// <summary>Return the account balance for the given coin type</summary>
		public override Balance Balance(Coin coin)
		{
			if (coin.Exchange != this)
				throw new Exception("Currency not associated with this exchange");

			Balance bal;
			return m_balance.TryGetValue(coin, out bal) ? bal : new Balance(coin, 0, 0, 0, 0, 0);
		}
	}

	/// <summary>Poloniex Exchange</summary>
	public class Poloniex :Exchange
	{
		public Poloniex(Model model)
			:base(model)
		{

		}

		/// <summary>Exchanges update their trade data</summary>
		public async override Task UpdateTradeDataAsync()
		{
		//	var msg = await JsonQueryAsync<>("https://www.cryptopia.co.nz/api/GetMarketOrders/100");
		}

		/// <summary>Return the account balance for the given coin type</summary>
		public override Balance Balance(Coin coin)
		{
			return new Balance(coin, 0, 0, 0, 0, 0);
		}
	}
}



#if false

		#region Messages

		///// <summary>Make a web request and return the response as an object</summary>
		//private async Task<T> JsonQueryAsync<T>(string request) where T:IMsg
		//{
		//	var req = new HttpRequestMessage(HttpMethod.Get, request);
		//	using (var client = new HttpClient())
		//	{
		//		var res = await client.SendAsync(req);
		//		if (!res.IsSuccessStatusCode) throw new Exception("Cryptopia: Public API request failed");
		//		return await Deserialise<T>(res);
		//	}
		//}

		///// <summary>Make a private API web request</summary>
		//private async Task<T> JsonQueryAsync<T>(string request, object post_data) where T:IMsg // var post_data = new{ Currency = "DOT" };
		//{
		//	// Create a request message
		//	var req = new HttpRequestMessage(HttpMethod.Post, request);
		////	req.Content = new System.Net.Http.ObjectContent(typeof(object), post_data, new JsonMediaTypeFormatter());

		////	// Authentication
		////	string requestContentBase64String = string.Empty;
		////	if (req.Content != null)
		////	{
		////		// Hash content to ensure message integrity
		////		using (var md5 = MD5.Create())
		////			requestContentBase64String = Convert.ToBase64String(md5.ComputeHash(await req.Content.ReadAsByteArrayAsync()));
		////	}

		////	// Create a random nonce for each request
		////	var nonce = Guid.NewGuid().ToString("N");

		////	// Serialise the message to a string so we can calculate the signature
		////	var signature = Encoding.UTF8.GetBytes(string.Concat(ApiKey, HttpMethod.Post, HttpUtility.UrlEncode(req.RequestUri.AbsoluteUri.ToLower()), nonce, requestContentBase64String));
		////	using (var hmac = new HMACSHA256(Convert.FromBase64String(ApiSecret)))
		////		req.Headers.Authorization = new AuthenticationHeaderValue("amx", "{0}:{1}:{2}".Fmt(ApiKey, Convert.ToBase64String(hmac.ComputeHash(signature)), nonce));

		//	// Send Request
		//	using (var client = new HttpClient())
		//	{
		//		var res = await client.SendAsync(req);
		//		if (!res.IsSuccessStatusCode) throw new Exception("Cryptopia: Private API request failed");
		//		return await Deserialise<T>(res);
		//	}
		//}

		///// <summary>Unpack a JSON message to an object</summary>
		//private async Task<T> Deserialise<T>(HttpResponseMessage res) where T:IMsg
		//{
		//	// Deserialise the JSON into the object 'T'
		//	var serializer = new JsonSerializer();
		//	using (var sr = new StreamReader(await res.Content.ReadAsStreamAsync()))
		//	using (var jr = new JsonTextReader(sr))
		//	{
		//		var msg = serializer.Deserialize<T>(jr);
		//		if (!msg.Success)
		//			throw new Exception("Cryptopia: Failed to read response of type {0}. {1}".Fmt(typeof(T).Name, msg.Message));

		//		// Return the de-serialised message
		//		return msg;
		//	}
		//}

		///// <summary>Common interface for all message response types</summary>
		//private interface IMsg
		//{
		//	bool Success { get; }
		//	string Message { get; }
		//}

		///// <summary>Response to the GetCurrencies method</summary>
		//private class MsgCurrencies :IMsg
		//{
		//	[JsonProperty] public bool Success { get; set; }
		//	[JsonProperty] public string Message { get; set; }
		//	[JsonProperty] public List<Currency> Data { get; set; }

		//	public class Currency
		//	{
		//		[JsonProperty] public int Id { get; set; }
		//		[JsonProperty] public string Name { get; set; }
		//		[JsonProperty] public string Symbol { get; set; }
		//		[JsonProperty] public string Algorithm { get; set; }
		//		[JsonProperty] public decimal WithdrawFee { get; set; }
		//		[JsonProperty] public decimal MinWithdraw { get; set; }
		//		[JsonProperty] public decimal MinBaseTrade { get; set; }
		//		[JsonProperty] public bool IsTipEnabled { get; set; }
		//		[JsonProperty] public decimal MinTip { get; set; }
		//		[JsonProperty] public int DepositConfirmations { get; set; }
		//		[JsonProperty] public string Status { get; set; }
		//		[JsonProperty] public string StatusMessage { get; set; }
		//		[JsonProperty] public string ListingStatus { get; set; }
		//	}
		//}

		///// <summary>Response to the GetTradePairs method</summary>
		//private class MsgTradePairs :IMsg
		//{
		//	[JsonProperty] public bool Success { get; set; }
		//	[JsonProperty] public string Message { get; set; }
		//	[JsonProperty] public List<Pair> Data { get; set; }
		//	[JsonProperty] public string Error { get; set; }

		//	public class Pair
		//	{
		//		[JsonProperty] public int Id { get; set; }
		//		[JsonProperty] public string Label { get; set; }
		//		[JsonProperty] public string Currency { get; set; }
		//		[JsonProperty] public string Symbol { get; set; }
		//		[JsonProperty] public string BaseCurrency { get; set; }
		//		[JsonProperty] public string BaseSymbol { get; set; }
		//		[JsonProperty] public string Status { get; set; }
		//		[JsonProperty] public string StatusMessage { get; set; }
		//		[JsonProperty] public decimal TradeFee { get; set; }
		//		[JsonProperty] public decimal MinimumTrade { get; set; }
		//		[JsonProperty] public decimal MaximumTrade { get; set; }
		//		[JsonProperty] public decimal MinimumBaseTrade { get; set; }
		//		[JsonProperty] public decimal MaximumBaseTrade { get; set; }
		//		[JsonProperty] public decimal MinimumPrice { get; set; }
		//		[JsonProperty] public decimal MaximumPrice { get; set; }
		//	}
		//}

		///// <summary>Response to the GetMarketOrders method</summary>
		//private class MsgMarketOrders :IMsg
		//{
		//	[JsonProperty] public bool Success { get; set; }
		//	[JsonProperty] public string Message { get; set; }
		//	[JsonProperty] public List<Orders> Data { get; set; }
		//	[JsonProperty] public string Error { get; set; }

		//	public class Orders
		//	{
		//		[JsonProperty] public int TradePairId { get; set; }
		//		[JsonProperty] public string Market { get; set; }
		//		[JsonProperty] public List<Buy> Buy { get; set; }
		//		[JsonProperty] public List<Sell> Sell { get; set; }
		//	}
		//	public class Buy
		//	{
		//		[JsonProperty] public int TradePairId { get; set; }
		//		[JsonProperty] public string Label { get; set; }
		//		[JsonProperty] public decimal Price { get; set; }
		//		[JsonProperty] public decimal Volume { get; set; }
		//		[JsonProperty] public decimal Total { get; set; }
		//	}
		//	public class Sell
		//	{
		//		[JsonProperty] public int TradePairId { get; set; }
		//		[JsonProperty] public string Label { get; set; }
		//		[JsonProperty] public decimal Price { get; set; }
		//		[JsonProperty] public decimal Volume { get; set; }
		//		[JsonProperty] public decimal Total { get; set; }
		//	}
		//}
		#endregion

			Connect();

			m_btn.Click += (s,a) =>
			{
				Subscribe();
			};

			UpdateUI();

		/// <summary>Connection to the data source</summary>
		private IWampRealmProxy Realm
		{
			get { return m_realm; }
			set
			{
				if (m_realm == value) return;
				if (m_realm != null)
				{
				}
				m_realm = value;
				if (m_realm != null)
				{
				}
				UpdateUI();
			}
		}
		private IWampRealmProxy m_realm;

		/// <summary></summary>
		private async void Connect()
		{
			try
			{
				var factory = new DefaultWampChannelFactory();
				var channel = factory.CreateJsonChannel("wss://api.poloniex.com", "realm1");
				await channel.Open();

				Realm = channel.RealmProxy;
			}
			catch (Exception ex)
			{
				Debug.Write(ex.Message);
			}
		}

		private async void Subscribe()
		{
			if (Realm == null)
				return;

			//var sub = Realm.Services.RegisterSubscriber(new Ticker());
			//var dis = await sub;
			////await dis.DisposeAsync();

			var count = 0;
			var subject = Realm.Services.GetSubject("ticker");
			var subscription = (IDisposable)null;
			subject.Subscribe(x =>
				{
					if (++count >= 10)
						Util.Dispose(ref subscription);
					else
						foreach (var arg in x.Arguments)
							Debug.Write(arg.Deserialize<dynamic>().ToString());
						Debug.WriteLine("");
				});

				//var channel = factory.CreateMsgpackChannel("wss://api.poloniex.com:443", "realm1");
				//await channel.Open();

				//var realmProxy = channel.RealmProxy;
				//Console.WriteLine("Connection established");

				//int received = 0;
				//IDisposable subscription = null;

				//subscription = realmProxy.Services.GetSubject("ticker")
				//	.Subscribe(x =>
				//	{
				//		Console.WriteLine("Got Event: " + x);
				//		received++;
				//		if (received > 5)
				//		{
				//			Console.WriteLine("Closing ..");
				//			subscription.Dispose();
				//		}
				//	});
		}

		/// <summary></summary>
		private void UpdateUI()
		{
			m_btn.Enabled = Realm != null;
		}
	public interface ITicker
	{
		[WampTopic("ticker")]
		void OnTick(PriceTick price_tick);
	}

	public class Ticker :ITicker
	{
		public void OnTick(PriceTick price_tick)
		{
			Debug.WriteLine("Tick: {0}".Fmt(price_tick));
		}
	}
#endif

