using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	// Notes:
	//  - Pairs should be given as Base/Quote.
	//  - Orders have the price in Quote currency and the Volume in Base currency.
	//  - Typically, the more common currency is the Quote currency,
	//      e.g NZDUSD => price = 0.72USD/1NZD, volume in NZD
	//      "buy 1000NZD = Base*Price = 720USD"
	//  - Exchanges update their data asynchronously. Merging the exchange data into
	//    the main data must be synchronised however, because the Model is continuously
	//    looking for profitable loops using parallel searches.
	//  - All exchange market data writes should be done by the main thread. Cross-thread
	//    reads are allowed because the main thread carefully controls when reading happens.

	/// <summary>Base class for exchanges</summary>
	public abstract class Exchange :IDisposable
	{
		public Exchange(Model model, decimal transaction_fee, int poll_period)
		{
			Model = model;
			TransactionFee = transaction_fee;
			PollPeriod = poll_period;
			Coins = new CoinCollection(this);
			Pairs = new PairCollection(this);
			Balance = new BalanceCollection(this);
			Positions = new PositionsCollection(this);
			Status = EStatus.Offline;
		}
		public virtual void Dispose()
		{
			Active = false;
			Model = null;
		}

		/// <summary>The name of this exchange</summary>
		public string Name
		{
			get { return GetType().Name; }
		}

		/// <summary>The connection status of the exchange</summary>
		public EStatus Status
		{
			get { return m_status; }
			protected set
			{
				if (m_status == value) return;
				m_status = value;
				OnStatusChanged();
			}
		}
		private EStatus m_status;

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				SetModel(value);
			}
		}
		protected virtual void SetModel(Model model)
		{
			if (m_model != null)
			{
				Debug.Assert(Active == false, "Should not be nulling 'Model' when the thread is running");
			}
			m_model = model;
			if (m_model != null)
			{
			}
		}
		private Model m_model;

		/// <summary>Enable/Disable the exchange</summary>
		public bool Active
		{
			get { return m_heart != null; }
			set
			{
				if (Active == value) return;
				SetActive(value);
			}
		}
		protected virtual void SetActive(bool active)
		{
			// If previously active
			if (Active)
			{
				// Stop the thread
				m_heart_thread_exit = true;
				if (m_heart.IsAlive)
					m_heart.Join();

				// Invalidate the loops when an exchange is disabled
				Model.RebuildLoops = true;
			}

			// Start the heart beat thread, if active
			Status = active ? EStatus.Connecting : EStatus.Stopped;
			m_heart = active ? new Thread(new ThreadStart(HeartBeatThreadEntry)) : null;

			// On enable...
			if (Active)
			{
				Debug.Assert(Model != null, "Should not be starting the thread when 'Model' is null");

				// Invalidate the loops when an exchange is enabled
				Model.RebuildLoops = true;

				m_heart_thread_exit = false;
				m_heart.Start();
			}
		}
		private async void HeartBeatThreadEntry()
		{
			try
			{
				Thread.CurrentThread.Name = Name;
				for (;!m_heart_thread_exit;)
				{
					await UpdateData();
					if (Model.ShutdownToken.IsCancellationRequested) break;
					Thread.Sleep(PollPeriod);
				}
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} heart beat thread exit");
				Active = false;
			}
		}
		private Thread m_heart;
		private bool m_heart_thread_exit;

		/// <summary>The rate that 'Heart' beats at</summary>
		public int PollPeriod
		{
			get { return m_poll_period; }
			set
			{
				if (m_poll_period == value) return;
				SetPollPeriod(value);
			}
		}
		protected virtual void SetPollPeriod(int period)
		{
			m_poll_period = period;
		}
		private int m_poll_period;

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public decimal TransactionFee
		{
			get;
			private set;
		}

		/// <summary>The number of coins available on this exchange</summary>
		public int CoinsAvailable
		{
			get { return Coins.Count; }
		}

		/// <summary>The number of trading pairs available</summary>
		public int PairsAvailable
		{
			get { return Pairs.Count; }
		}

		/// <summary>The coins associated with this exchange</summary>
		public CoinCollection Coins { get; private set; }
		public class CoinCollection :BindingDict<string, Coin>
		{
			private readonly Exchange m_exch;
			public CoinCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.Symbol;
			}
			public Coin GetOrAdd(string sym)
			{
				Debug.Assert(m_exch.Model.AssertMarketDataWrite());
				return this.GetOrAdd(sym, k => new Coin(k, m_exch));
			}
			public override Coin this[string sym]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());
					return TryGetValue(sym, out var coin) ? coin : null;
				}
				set
				{
					Debug.Assert(m_exch.Model.AssertMarketDataWrite());
					base[sym] = value;
				}
			}
		}

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs { get; private set; }
		public class PairCollection :BindingDict<string, TradePair>
		{
			private readonly Exchange m_exch;
			public PairCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.UniqueKey;
			}

			/// <summary>Return a pair involving the given symbols (in either order)</summary>
			public TradePair this[string sym0, string sym1]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());

					TradePair pair;
					if (TryGetValue(TradePair.MakeKey(sym0, sym1), out pair)) return pair;
					if (TryGetValue(TradePair.MakeKey(sym1, sym0), out pair)) return pair;
					return null;
				}
			}

			/// <summary>Return a pair involving the given symbol and the two exchanges (in either order)</summary>
			public TradePair this[string sym, Exchange exch0, Exchange exch1]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());

					TradePair pair;
					if (TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out pair)) return pair;
					if (TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out pair)) return pair;
					return null;
				}
			}
		}

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance { get; private set; }
		public class BalanceCollection :BindingDict<Coin, Balance>
		{
			private readonly Exchange m_exch;
			public BalanceCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.Coin;
				SupportsSorting = false;
			}
			public Balance GetOrAdd(Coin coin)
			{
				Debug.Assert(m_exch.Model.AssertMarketDataWrite());
				return this.GetOrAdd(coin, x => new Balance(x));
			}
			public override Balance this[Coin coin]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());
					if (coin.Exchange != m_exch && !(m_exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
					return TryGetValue(coin, out var bal) ? bal : new Balance(coin);
				}
				set
				{
					Debug.Assert(m_exch.Model.AssertMarketDataWrite());
					if (coin.Exchange != m_exch && !(m_exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
					base[coin] = value;
				}
			}
			// Don't provide this, the Coin implicit cast is favoured over the overloaded method
			//public Balance this[string sym]
			//{
			//	get { return base[m_exch.Coins[sym]]; }
			//	set { base[m_exch.Coins[sym]] = value; }
			//}
		}

		/// <summary>Open positions held on this exchange, keyed on order ID</summary>
		public PositionsCollection Positions { get; private set; }
		public class PositionsCollection :BindingDict<ulong, Position>
		{
			private readonly Exchange m_exch;
			public PositionsCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.OrderId;
			}
			public override Position this[ulong key]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());
					return base[key];
				}
				set
				{
					Debug.Assert(m_exch.Model.AssertMarketDataWrite());
					base[key]=value;
				}
			}
		}

		/// <summary>Raised when the 'Status' value changes</summary>
		public event EventHandler StatusChanged;
		protected virtual void OnStatusChanged()
		{
			// Status change can happen from any thread
			Model.RunOnGuiThread(() =>
			{
				if (Model == null) return;
				StatusChanged.Raise(this);
				Model.Exchanges.ResetItem(this, ignore_missing:true);
			});
		}

		/// <summary>Place an order to convert 'volume' (base currency) to quote currency at 'price' (Quote/Base)</summary>
		public async Task CreateB2QOrder(TradePair pair, Unit<decimal> volume_base, Unit<decimal>? price = null)
		{
			// Check units
			if (volume_base < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume_base));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair.
			// We're selling base currency, at the most competitive asking price.
			price = price ?? pair.Ask.FirstOrDefault().Price;

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so B2Q is a sell of base currency
			await CreateOrder(pair, ETradeType.Sell, volume_base, price.Value);
		}

		/// <summary>Place an order to convert 'volume' (quote currency) to base currency at 'price' (Base/Quote)</summary>
		public async Task CreateQ2BOrder(TradePair pair, Unit<decimal> volume_quote, Unit<decimal>? price = null)
		{
			// Check units
			if (volume_quote < 0m._(pair.Quote))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume_quote));
			if (price != null && price <= 0m._(pair.Base)/1m._(pair.Quote))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair.
			// We're making a bid to buy base currency, at the most competitive bid price.
			price = price ?? (1m / pair.Bid.FirstOrDefault().Price);

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so Q2B is a buy of base currency
			await CreateOrder(pair, ETradeType.Buy, volume_quote * price.Value, 1m / price.Value);
		}

		/// <summary>Place an order on the exchange to buy or sell 'volume' (in base currency). i.e. Sell = Base->Quote, Buy = Quote->Base</summary>
		public async Task CreateOrder(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal>? price = null)
		{
			if (pair.Exchange != this)
				throw new Exception("Pair {0} is not provided by this exchange".Fmt(pair));
			if (volume < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If price isn't given, get it from the pair
			if (price == null)
			{
				// If we're selling Base, we want the highest price we can get.
				//  => Get the most competitive price from the *increasing* price orders (i.e. 'Ask')
				//  => We're "Asking" 'price' for our Base currency
				// If we're buying Base, we want to pay the lowest price possible.
				//  => Get the most competitive price from the *decreasing* price orders (i.e. 'Bid')
				//  => We're "Biding" at 'price' to buy Base currency.
				if (tt == ETradeType.Sell) price = pair.Ask.FirstOrDefault().Price;
				if (tt == ETradeType.Buy ) price = pair.Bid.FirstOrDefault().Price;
				if (price == null || price == 0m._(price.Value))
					throw new Exception("Market rate could not be determined");
			}

			// Place the order
			if (volume > 0m._(volume))
			{
				await CreateOrderInternal(pair, tt, volume, price.Value);

				// Log the event
				Model.Log.Write(ELogLevel.Info, "{0} - {1} Order Created: {2} {3} -> {4} {5} @ {6}".Fmt(
					Name, tt,
					tt == ETradeType.Sell ? volume    : volume * price.Value,
					tt == ETradeType.Sell ? pair.Base : pair.Quote,
					tt == ETradeType.Buy  ? volume    : volume * price.Value,
					tt == ETradeType.Buy  ? pair.Base : pair.Quote,
					price.Value));

				// Update the balances immediately to prevent accidental use of funds that aren't available
				// because of the trade we just placed. Only reduce balances, don't assume the trade has completed.
				// Also, remove the orders we've filled from the order book.
				if (tt == ETradeType.Sell)
				{
					pair.Base.Balance.Hold(volume);
					pair.Ask.RemoveOrders(volume, price.Value);
				}
				else
				{
					pair.Quote.Balance.Hold(volume * price.Value);
					pair.Bid.RemoveOrders(volume, price.Value);
				}
			}
		}
		protected abstract Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate);

		/// <summary>Update the collections of coins and pairs</summary>
		public virtual Task UpdatePairs(HashSet<string> coins_of_interest) // Worker thread context
		{
			// This method should await server responses, collect data in local buffers
			// then use 'RunOnGuiThread' to copy data to the Exchange's main collections.
			Model.RunOnGuiThread(() => { }, block:true);
			return Misc.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected virtual Task UpdateData() // Worker thread context
		{
			// This method should await all server responses, then add an action
			// to the Model.MarketUpdates collection for integration at a suitable time.
			return Misc.CompletedTask;
		}

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}
	}
}

