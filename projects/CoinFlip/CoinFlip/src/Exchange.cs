using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
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
		public Exchange(Model model, IExchangeSettings settings)
		{
			Model = model;
			Settings = settings;
			Colour = Colours[m_colour_index++ % Colours.Length];
			m_pace_maker = new AutoResetEvent(false);
			Coins = new CoinCollection(this);
			Pairs = new PairCollection(this);
			Balance = new BalanceCollection(this);
			Positions = new PositionsCollection(this);
			History = new HistoryCollection(this);
			MarketDataUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			BalanceUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			PositionUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			TradeHistoryUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);

			var history_start = (DateTimeOffset.Now - TimeSpan.FromDays(5)).Ticks;
			HistoryInterval = new Range(history_start, history_start);

			Status = EStatus.Offline;
		}
		public virtual void Dispose()
		{
			m_disposing = true;
			Active = false;
			Model = null;
		}
		private bool m_disposing;

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
				Model.RunOnGuiThread(() =>
				{
					m_status = value;
					OnStatusChanged();

					// Disable in the event of an error
					if (m_status == EStatus.Error)
						Active = false;
				});
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

		/// <summary>Settings for this exchange</summary>
		public IExchangeSettings Settings { get; private set; }

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
			Debug.Assert(Model.AssertMainThread());

			// If previously active
			if (Active)
			{
				// Stop the thread
				m_heart_thread_exit = true;
				m_pace_maker.Set();
				if (m_heart.IsAlive)
					m_heart.Join();

				// Invalidate the loops when an exchange is disabled
				Model.RebuildLoops = true;
			}

			// Start the heart beat thread, if active
			Status = active ? EStatus.Connecting : EStatus.Stopped;
			m_heart = active ? new Thread(new ThreadStart(HeartBeatThreadEntry)) : null;
			if (!m_disposing) Settings.Active = active;

			// On enable...
			if (Active)
			{
				Debug.Assert(Model != null, "Should not be starting the thread when 'Model' is null");

				// Invalidate the loops when an exchange is enabled
				Model.RebuildLoops = true;

				// Trigger a positions and balances update
				PositionUpdateRequired = true;
				BalanceUpdateRequired = true;

				m_heart_thread_exit = false;
				m_heart.Start();
			}
		}
		private void HeartBeatThreadEntry()
		{
			try
			{
				Thread.CurrentThread.Name = Name;

				var sw = new Stopwatch();
				var last_balance_update = 0L;
				var last_market_update = 0L;
				var last_position_update = 0L;
				var last_trade_history_update = 0L;

				var tasks = new List<Task>();
				const int MainLoopPeriodMS = 100;
				for (sw.Start(); !m_heart_thread_exit; m_pace_maker.WaitOne(MainLoopPeriodMS))
				{
					Status = EStatus.Connected;
					if (Model.ShutdownToken.IsCancellationRequested) break;
					tasks.Clear();

					// Update the balances
					const int BalanceUpdatePeriodMS = 1000;
					if (BalanceUpdateRequired || (sw.ElapsedMilliseconds - last_balance_update) > BalanceUpdatePeriodMS)
					{
						BalanceUpdateRequired = false;
						tasks.Add(UpdateBalances());
						last_balance_update = sw.ElapsedMilliseconds;
					}

					// Update the existing positions
					const int PositionUpdatePeriodMS = 1000;
					if (PositionUpdateRequired || (sw.ElapsedMilliseconds - last_position_update) > PositionUpdatePeriodMS)
					{
						PositionUpdateRequired = false;
						tasks.Add(UpdatePositions());
						last_position_update = sw.ElapsedMilliseconds;
					}

					// Update trade history
					const int TradeHistoryUpdatePeriodMS = 1000;
					if (TradeHistoryUpdateRequired || (sw.ElapsedMilliseconds - last_trade_history_update) > TradeHistoryUpdatePeriodMS)
					{
						TradeHistoryUpdateRequired = false;
						tasks.Add(UpdateTradeHistory());
						last_trade_history_update = sw.ElapsedMilliseconds;
					}

					// Update market data at the polling rate
					if (MarketDataUpdateRequired || (sw.ElapsedMilliseconds - last_market_update) > PollPeriod)
					{
						MarketDataUpdateRequired = false;
						tasks.Add(UpdateData());
						last_market_update = sw.ElapsedMilliseconds;
					}

					Task.WhenAll(tasks).Wait();
				}
				Status = EStatus.Stopped;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} heart beat thread exit");
				Status = EStatus.Error;
				Active = false;
			}
		}
		private AutoResetEvent m_pace_maker;
		private bool m_heart_thread_exit;
		private Thread m_heart;

		/// <summary>The rate that 'Heart' beats at</summary>
		public int PollPeriod
		{
			get { return Settings.PollPeriod; }
			set { Settings.PollPeriod = value; }
		}

		/// <summary>Dirty flag for market data</summary>
		public bool MarketDataUpdateRequired
		{
			get { return m_market_data_update_required; }
			set
			{
				if (m_market_data_update_required == value) return;
				m_market_data_update_required = value;
				if (value) m_pace_maker.Set();
			}
		}
		private bool m_market_data_update_required;
		public Task MarketDataUpdated()
		{
			var now = DateTimeOffset.Now;
			MarketDataUpdateRequired = true;
			return Task.Run(() => MarketDataUpdatedTime.Wait(ts => ts > now));
		}
		protected ConditionVariable<DateTimeOffset> MarketDataUpdatedTime { get; private set; }

		/// <summary>Dirty flag for account balance data</summary>
		public bool BalanceUpdateRequired
		{
			get { return m_balance_update_required; }
			set
			{
				if (m_balance_update_required == value) return;
				m_balance_update_required = value;
				if (value) m_pace_maker.Set();
			}
		}
		private bool m_balance_update_required;
		public Task BalanceUpdated()
		{
			var now = DateTimeOffset.Now;
			BalanceUpdateRequired = true;
			return Task.Run(() => BalanceUpdatedTime.Wait(ts => ts > now));
		}
		protected ConditionVariable<DateTimeOffset> BalanceUpdatedTime { get; private set; }

		/// <summary>Dirty flag for existing positions data</summary>
		public bool PositionUpdateRequired
		{
			get { return m_position_update_required; }
			set
			{
				if (m_position_update_required == value) return;
				m_position_update_required = value;
				if (value) m_pace_maker.Set();
			}
		}
		private bool m_position_update_required;
		public Task PositionUpdated()
		{
			var now = DateTimeOffset.Now;
			PositionUpdateRequired = true;
			return Task.Run(() => PositionUpdatedTime.Wait(ts => ts > now));
		}
		protected ConditionVariable<DateTimeOffset> PositionUpdatedTime { get; private set; }

		/// <summary>Dirty flag for updating trade history data</summary>
		public bool TradeHistoryUpdateRequired
		{
			get { return m_trade_history_update_required; }
			set
			{
				if (m_trade_history_update_required == value) return;
				m_trade_history_update_required = value;
				if (value) m_pace_maker.Set();
			}
		}
		private bool m_trade_history_update_required;
		public Task TradeHistoryUpdated()
		{
			var now = DateTimeOffset.Now;
			TradeHistoryUpdateRequired = true;
			return Task.Run(() => TradeHistoryUpdatedTime.Wait(ts => ts > now));
		}
		protected ConditionVariable<DateTimeOffset> TradeHistoryUpdatedTime { get; private set; }

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public decimal TransactionFee
		{
			get { return Settings.TransactionFee; }
		}

		/// <summary>The value of all coins held on this exchange</summary>
		public decimal NettWorth
		{
			get { return !(this is CrossExchange) ? Balance.Values.Sum(x => x.Total * x.Coin.NormalisedValue) : 0m; }
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

		/// <summary>An identifying colour for the exchange</summary>
		public Color Colour { get; set; }
		private static Color[] Colours = new[]{ Color.DarkBlue, Color.DarkGreen, Color.DarkRed, Color.DarkMagenta, Color.DarkOrange, Color.DarkOrchid, Color.DarkKhaki };
		private static int m_colour_index;

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
					if (TryGetValue(coin, out var bal) && bal.TimeStamp > value.TimeStamp) return; // Ignore out of date data
					base[coin] = value;
				}
			}
			public Balance Get(string sym)
			{
				var coin = m_exch.Coins[sym];
				return this[coin];
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
					return TryGetValue(key, out var pos) ? pos : null;
				}
				set
				{
					Debug.Assert(m_exch.Model.AssertMarketDataWrite());
					if (TryGetValue(key, out var pos) && pos.UpdatedTime > value.UpdatedTime) return; // Ignore out of date data
					base[key] = value;
				}
			}
		}

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public HistoryCollection History { get; private set; }
		public class HistoryCollection :BindingDict<ulong, PositionFill>
		{
			private readonly Exchange m_exch;
			public HistoryCollection(Exchange exch)
			{
				m_exch = exch;
				KeyFrom = x => x.OrderId;
			}
			public PositionFill GetOrAdd(ulong key, ETradeType tt, TradePair pair, DateTimeOffset created)
			{
				Debug.Assert(m_exch.Model.AssertMarketDataWrite());
				return this.GetOrAdd(key, x => new PositionFill(key, tt, pair, created));
			}
			public override PositionFill this[ulong key]
			{
				get
				{
					Debug.Assert(m_exch.Model.AssertMarketDataRead());
					return TryGetValue(key, out var pos) ? pos : null;
				}
				set
				{
					Debug.Assert(m_exch.Model.AssertMarketDataWrite());
					base[key] = value;
				}
			}
		}

		/// <summary>Raised when the 'Status' value changes</summary>
		public event EventHandler StatusChanged;
		protected virtual void OnStatusChanged()
		{
			// Status change can happen from any thread
			if (Model == null) return;
			StatusChanged.Raise(this);
			Model.Exchanges.ResetItem(this, ignore_missing:true);
		}

		/// <summary>Place an order to convert 'volume' (base currency) to quote currency at 'price' (Quote/Base)</summary>
		public Task<TradeResult> CreateB2QOrder(TradePair pair, Unit<decimal> volume_base, Unit<decimal>? price = null)
		{
			// Check units
			if (volume_base < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume_base));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair.
			// We're selling base currency, at the most competitive asking price.
			price = price ?? pair.Q2B.FirstOrDefault().Price;

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so B2Q is a sell of base currency
			return CreateOrder(ETradeType.B2Q, pair, volume_base, price.Value);
		}

		/// <summary>Place an order to convert 'volume' (quote currency) to base currency at 'price' (Base/Quote)</summary>
		public Task<TradeResult> CreateQ2BOrder(TradePair pair, Unit<decimal> volume_quote, Unit<decimal>? price = null)
		{
			// Check units
			if (volume_quote < 0m._(pair.Quote))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume_quote));
			if (price != null && price <= 0m._(pair.Base)/1m._(pair.Quote))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair.
			// We're making a bid to buy base currency, at the most competitive bid price.
			price = price ?? (1m / pair.B2Q.FirstOrDefault().Price);

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so Q2B is a buy of base currency
			return CreateOrder(ETradeType.Q2B, pair, volume_quote, price.Value);
		}

		/// <summary>Place an order on the exchange to buy/sell 'volume' (currency depends on 'tt')</summary>
		public async Task<TradeResult> CreateOrder(ETradeType tt, TradePair pair, Unit<decimal> volume_, Unit<decimal> price_)
		{
			// Sanity checks
			if (pair.Exchange != this)
			{
				throw new Exception($"Pair {pair} is not provided by this exchange");
			}
			if (tt == ETradeType.B2Q)
			{
				if (volume_ <= 0m._(pair.Base))
					throw new Exception($"Invalid trade volume: {volume_}");
				if (price_ <= 0m._(pair.Quote)/1m._(pair.Base))
					throw new Exception($"Invalid exchange rate: {price_}");
				if (volume_ > Balance[pair.Base].Available)
					throw new Exception($"Order volume is greater than the current balance: {Balance[pair.Base].Available}");
			}
			if (tt == ETradeType.Q2B)
			{
				if (volume_ <= 0m._(pair.Quote))
					throw new Exception($"Invalid trade volume: {volume_}");
				if (price_ <= 0m._(pair.Base)/1m._(pair.Quote))
					throw new Exception($"Invalid exchange rate: {price_}");
				if (volume_ > Balance[pair.Quote].Available)
					throw new Exception($"Order volume is greater than the current balance: {Balance[pair.Quote].Available}");
			}

			// Convert the volume to base currency and the price to quote/base
			var volume = tt == ETradeType.B2Q ? volume_ : (price_ * volume_);
			var price  = tt == ETradeType.B2Q ? price_  : (1m / price_);

			// Make the trade
			// This can have the following results:
			// 1) the entire trade is added to the order book for the pair -> a single order number is returned
			// 2) the entire trade can be met by existing orders in the order book -> a collection of trade IDs is returned
			// 3) some of the trade can be met by existing orders -> a single order number and a collection of trade IDs are returned.
			var order_result = Model.AllowTrades // Obey the global trade switch
				? await CreateOrderInternal(pair, tt, volume, price)
				: new TradeResult(++m_fake_order_number, new ulong[0]);

			// Log the event
			Model.Log.Write(ELogLevel.Info, "{0}: (id={1}) {2} {3} → {4} {5} @ {6} {7}".Fmt(
				Name, order_result.OrderId ?? 0,
				(volume_         ).ToString("G6"), tt == ETradeType.B2Q ? pair.Base  : pair.Quote,
				(volume_ * price_).ToString("G6"), tt == ETradeType.B2Q ? pair.Quote : pair.Base,
				price.ToString("G6"), pair.RateUnits));

			// Add the position to the Positions collection so that there is no race condition
			// between placing an order and checking 'Positions' for the order just placed.
			if (order_result.OrderId != null)
				Positions.Add(new Position(order_result.OrderId.Value, 0, pair, tt, price, volume, volume, DateTimeOffset.Now, DateTimeOffset.Now));

			// The order may have also been completed or partially filled. Add the filled orders to the trade history
			foreach (var tid in order_result.TradeIds)
			{
				//HACK
				var order_id = order_result.OrderId ?? tid;
				var fill = History.GetOrAdd(order_id, tt, pair, DateTimeOffset.Now);
				fill.Trades[tid] = new Position(order_id, tid, pair, tt, price, volume, volume, DateTimeOffset.Now, DateTimeOffset.Now);
			}

			// Update the balances immediately to prevent accidental use of funds that aren't available
			// because of the trade we just placed. Only reduce balances, don't assume the trade has completed.
			// Also, remove any orders we might have filled from the order book.
			if (tt == ETradeType.B2Q)
			{
				pair.Base.Balance.Hold(volume);
				pair.B2Q.RemoveOrders(-1, volume, price);
			}
			else
			{
				pair.Quote.Balance.Hold(volume * price);
				pair.Q2B.RemoveOrders(+1, volume, price);
			}

			// Trigger a positions and balances update
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;

			return order_result;
		}
		protected abstract Task<TradeResult> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price);
		private ulong m_fake_order_number;

		/// <summary>Cancel an existing position</summary>
		public async Task CancelOrder(TradePair pair, ulong order_id)
		{
			if (Model.AllowTrades) // Obey the global trade switch
				await CancelOrderInternal(pair, order_id);

			// Remove the position from the Positions collection so that there is no race condition
			Positions.RemoveIf(x => x.Pair == pair && x.OrderId == order_id);

			// Trigger a positions and balances update
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
		}
		protected abstract Task CancelOrderInternal(TradePair pair, ulong order_id);

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
			lock (MarketDataUpdatedTime)
				MarketDataUpdatedTime.NotifyOne(MarketDataUpdatedTime, DateTimeOffset.Now);
			return Misc.CompletedTask;
		}

		/// <summary>Update the account balances</summary>
		protected virtual Task UpdateBalances()  // Worker thread context
		{
			BalanceUpdatedTime.NotifyOne(DateTimeOffset.Now);
			return Misc.CompletedTask;
		}

		/// <summary>Update all open positions</summary>
		protected virtual Task UpdatePositions() // Worker thread context
		{
			PositionUpdatedTime.NotifyOne(DateTimeOffset.Now);
			return Misc.CompletedTask;
		}

		/// <summary>Update the trade history</summary>
		protected virtual Task UpdateTradeHistory() // Worker thread context
		{
			TradeHistoryUpdatedTime.NotifyOne(DateTimeOffset.Now);
			return Misc.CompletedTask;
		}

		/// <summary>Remove positions that are older than 'timestamp' and not in 'order_ids'</summary>
		protected void RemovePositionsNotIn(HashSet<ulong> order_ids, DateTimeOffset timestamp)
		{
			// Remove any positions that are no longer valid.
			foreach (var pos in Positions.Values.Where(x => !order_ids.Contains(x.OrderId)).ToArray())
			{
				if (pos.CreationTime >= timestamp) continue;
				if (Model.AllowTrades == false && pos.OrderId < 100) continue; // Hack for fake positions
				Positions.Remove(pos.OrderId);
			}
		}

		/// <summary>The time range that the position history covers (in ticks)</summary>
		public Range HistoryInterval { get; protected set; }

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}
	}
}


			//// If price isn't given, get it from the pair
			//if (price == null)
			//{
			//	// If we're selling Base, we want the highest price we can get.
			//	//  => Get the most competitive price from the *increasing* price orders (i.e. 'Ask')
			//	//  => We're "Asking" 'price' for our Base currency
			//	// If we're buying Base, we want to pay the lowest price possible.
			//	//  => Get the most competitive price from the *decreasing* price orders (i.e. 'Bid')
			//	//  => We're "Biding" at 'price' to buy Base currency.
			//	if (tt == ETradeType.B2Q) price = pair.Q2B.FirstOrDefault().Price;
			//	if (tt == ETradeType.Q2B) price = pair.B2Q.FirstOrDefault().Price;
			//	if (price == null || price == 0m._(price.Value))
			//		throw new Exception("Market rate could not be determined");
			//}