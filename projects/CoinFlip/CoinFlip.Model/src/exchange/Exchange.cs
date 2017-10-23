using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	// Notes:
	//  - Pairs should be given as Base/Quote.
	//  - Orders have the price in Quote/Base and the Volume in Base currency.
	//  - Typically, the more common currency is the Quote currency,
	//      e.g NZDUSD => price = 0.72USD/1NZD, volume in NZD
	//      "buy 1000NZD = Base*Price = 720USD"
	//  - Exchanges update their data asynchronously. Merging the exchange data into
	//    the main data must be synchronised however.
	//  - All exchange market data writes should be done by the main thread.
	//  - Cross-thread market data reads require ownership of the 'Model.MarketDataLock'.
	//  - When back testing is enabled, 'Exchange' diverts calls to 'Model.Simulation',
	//    sub-classes should not receive calls while back testing is enabled.
	//  - All public methods should be non-virtual, forwarding to virtual protected
	//    methods, or the 'Simulation' object when back testing is enabled

	/// <summary>Base class for exchanges</summary>
	public abstract class Exchange :IDisposable ,INotifyPropertyChanged
	{
		public Exchange(Model model, IExchangeSettings settings)
		{
			Model = model;
			Settings = settings;
			Colour = Colours[m_colour_index++ % Colours.Length];
			Coins = new CoinCollection(this);
			Pairs = new PairCollection(this);
			Balance = new BalanceCollection(this);
			Positions = new PositionsCollection(this);
			History = new HistoryCollection(this);
			Shutdown = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown.Token);
			m_update_thread_step = new AutoResetEvent(false);
			TradeHistoryUseful = false;

			// Limit the amount of history to retrieve
			var history_start = (Model.UtcNow - TimeSpan.FromDays(5)).Ticks;
			HistoryInterval = new Range(history_start, history_start);
			m_history_last = HistoryInterval.End;

			// Start the exchange if enabled in the settings
			if (Settings.Active)
				Model.RunOnGuiThread(() => UpdateThreadActive = true);
		}
		public virtual void Dispose()
		{
			Shutdown = null;
			UpdateThreadActive = false;
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
			get
			{
				return
					!Enabled ? EStatus.Stopped :
					Model.BackTesting ? EStatus.Simulated :
					UpdateThreadActive ? EStatus.Connected :
					EStatus.Offline;
			}
		}

		/// <summary>Settings for this exchange</summary>
		public IExchangeSettings Settings { get; private set; }

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					if (UpdateThreadActive) throw new Exception("Should not be nulling 'Model' when the thread is running");
					m_model.BackTestingChanging -= HandleBackTestingChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.BackTestingChanging += HandleBackTestingChanged;
				}

				// Handlers
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					// If back testing is about to be enabled, turn off the update thread
					if (!Model.BackTesting && e.Before)
						UpdateThreadActive = false;

					// If back testing is just been disabled, turn on the update thread
					if (!Model.BackTesting && e.After)
						UpdateThreadActive = Enabled;

					RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Status)));
				}
			}
		}
		private Model m_model;

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationTokenSource Shutdown
		{
			get { return m_shutdown; }
			private set
			{
				if (m_shutdown == value) return;
				m_shutdown?.Cancel();
				m_shutdown = value;
			}
		}
		private CancellationTokenSource m_shutdown;

		/// <summary>True if this exchange is to be used</summary>
		public bool Enabled
		{
			get { return Settings.Active; }
			set
			{
				if (Enabled == value) return;
				Settings.Active = value;
				UpdateThreadActive = value;
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Enabled)));
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Status)));
			}
		}

		/// <summary>Enable/Disable the exchange update thread</summary>
		public bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			private set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Model.AssertMainThread());

				// Don't enable inactive exchanges
				if (value && !Enabled)
					throw new Exception("Don't enable inactive exchanges");

				// Don't enable the update thread while back testing
				if (value && Model.BackTesting)
					return;

				// If previously active
				if (m_update_thread != null)
				{
					// Stop the thread
					m_update_thread_exit = true;
					Shutdown.Cancel();
					m_update_thread_step.Set();
					if (m_update_thread.IsAlive)
						m_update_thread.Join();
				}

				// Start the heart beat thread, if active
				m_update_thread = value ? new Thread(new ThreadStart(UpdateThreadEntryPoint)) : null;

				// On enable...
				if (m_update_thread != null)
				{
					// Trigger updates
					BalanceUpdateRequired = true;
					PositionUpdateRequired = true;

					// Start the thread
					m_update_thread_exit = false;
					Shutdown = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown.Token);
					m_update_thread.Start();
				}

				// Notify updating
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Status)));

				/// <summary>Thread entry point for the exchange update thread</summary>
				void UpdateThreadEntryPoint()
				{
					try
					{
						Thread.CurrentThread.Name = Name;
						Model.Log.Write(ELogLevel.Debug, $"Exchange {Name} update thread started");

						// Note: there is no point in trying to run the updates in parallel because
						// the 'nonce' system requires each query to be sequential.
						for (;!m_update_thread_exit;) try
						{
							// Wait for the step period. If triggered early, retest exit flag
							const int MainLoopPeriodMS = 100;
							if (Shutdown.IsCancellationRequested) break;
							if (m_update_thread_step.WaitOne(MainLoopPeriodMS))
								continue;

							// Update the balances
							const double BalanceUpdatePeriodMS = 1000;
							if (BalanceUpdateRequired || (Model.UtcNow - Balance.LastUpdated).TotalMilliseconds > BalanceUpdatePeriodMS)
							{
								BalanceUpdateRequired = false;
								UpdateBalances();
							}

							// Update positions/history
							const int PositionUpdatePeriodMS = 1000;
							if (PositionUpdateRequired || (Model.UtcNow - Positions.LastUpdated).TotalMilliseconds > PositionUpdatePeriodMS)
							{
								PositionUpdateRequired = false;
								UpdatePositionsAndHistory();
							}

							// Update market data
							const int MarketDataUpdatePeriodMS = 100;
							if (MarketDataUpdateRequired || (Model.UtcNow - Pairs.LastUpdated).TotalMilliseconds > MarketDataUpdatePeriodMS)
							{
								MarketDataUpdateRequired = false;
								UpdateData();
							}
						}
						catch (OperationCanceledException) { break; }
						Model.Log.Write(ELogLevel.Debug, $"Exchange {Name} update thread stopped");
					}
					catch (Exception ex)
					{
						Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} heart beat thread exit");
						Model.RunOnGuiThread(() => UpdateThreadActive = false);
					}
				}
			}
		}
		private Thread m_update_thread;
		private AutoResetEvent m_update_thread_step;
		private bool m_update_thread_exit;

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
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_market_data_update_required;

		/// <summary>Dirty flag for account balance data</summary>
		public bool BalanceUpdateRequired
		{
			get { return m_balance_update_required; }
			set
			{
				if (m_balance_update_required == value) return;
				m_balance_update_required = value;
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_balance_update_required;

		/// <summary>Dirty flag for existing positions data</summary>
		public bool PositionUpdateRequired
		{
			get { return m_position_update_required; }
			set
			{
				if (m_position_update_required == value) return;
				m_position_update_required = value;
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_position_update_required;

		/// <summary>True if TradeHistory can be mapped to previous order id's</summary>
		public bool TradeHistoryUseful { get; protected set; }

		/// <summary>The maximum number of requests per second to the exchange server</summary>
		public float ServerRequestRateLimit
		{
			get { return Settings.ServerRequestRateLimit; }
			set
			{
				if (ServerRequestRateLimit == value) return;
				Settings.ServerRequestRateLimit = value;
				SetServerRequestRateLimit(value);
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(ServerRequestRateLimit)));
			}
		}
		protected abstract void SetServerRequestRateLimit(float limit);

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public decimal Fee
		{
			get { return Settings.TransactionFee; }
		}

		/// <summary>The value of all coins held on this exchange</summary>
		public decimal NettWorth
		{
			get
			{
				if (this is CrossExchange) return 0m;
				return Balance.Values.Sum(x => x.Coin.Value(x.Total));
			}
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

		/// <summary>The coins associated with this exchange</summary>
		public CoinCollection Coins { get; private set; }

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs { get; private set; }

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance { get; private set; }

		/// <summary>Open positions held on this exchange, keyed on order ID</summary>
		public PositionsCollection Positions { get; private set; }

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public HistoryCollection History { get; private set; }

		/// <summary>Place an order to convert 'volume' (base currency) to quote currency at 'price' (Quote/Base)</summary>
		public TradeResult CreateB2QOrder(TradePair pair, Unit<decimal> volume_base, Unit<decimal>? price = null)
		{
			// Check units
			if (volume_base < 0m._(pair.Base))
				throw new Exception("Invalid trade volume: {0}".Fmt(volume_base));
			if (price != null && price <= 0m._(pair.Quote)/1m._(pair.Base))
				throw new Exception("Invalid exchange rate: {0}".Fmt(price.Value));

			// If the price isn't given, get it from the pair.
			// We're selling base currency, at the most competitive asking price.
			price = price ?? pair.QuoteToBase(0m._(pair.Quote)).Price;

			// Place the order on the exchange.
			// Exchanges buy/sell base currency, so B2Q is a sell of base currency
			return CreateOrder(ETradeType.B2Q, pair, volume_base, price.Value);
		}

		/// <summary>Place an order to convert 'volume' (quote currency) to base currency at 'price' (Base/Quote)</summary>
		public TradeResult CreateQ2BOrder(TradePair pair, Unit<decimal> volume_quote, Unit<decimal>? price = null)
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
		public TradeResult CreateOrder(ETradeType tt, TradePair pair, Unit<decimal> volume_, Unit<decimal> price_)
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

			// Fake positions when not back testing and not live trading.
			// Back testing looks like live trading because of the emulated exchanges.
			var fake = Model.AllowTrades == false && Model.BackTesting == false;

			// Convert the volume to base currency and the price to quote/base
			var volume = tt.VolumeIn(volume_, price_);
			var price  = tt.PriceQ2B(price_);
			var now = Model.UtcNow;

			// Put a hold on the balance we're about to trade to prevent a race condition
			// with other trades being placed while we wait for this one to go through.
			// Only reduce balances, don't assume the trade has completed.
			// If we're live trading, remove the balance until the next balance update is received.
			// If not live trading, hold the balance until the fake order is removed (hold is updated below)
			var bal = tt.CoinIn(pair).Balance;
			var hold = tt.VolumeIn(volume, price);
			var hold_id = bal.Hold(hold);

			// Make the trade
			// This can have the following results:
			// 1) the entire trade is added to the order book for the pair -> a single order number is returned
			// 2) the entire trade can be met by existing orders in the order book -> a collection of trade IDs is returned
			// 3) some of the trade can be met by existing orders -> a single order number and a collection of trade IDs are returned.
			var order_result =
				Model.AllowTrades ? CreateOrderInternal(pair, tt, volume, price) :
				Model.BackTesting ? Model.Simulation[this].CreateOrderInternal(pair, tt, volume, price) :
				new TradeResult(pair, ++m_fake_order_number, new ulong[0]);

			// Log the event
			Model.Log.Write(ELogLevel.Info, $"{Name}: (id={order_result.OrderId}) {volume_.ToString("G6",true)} → {(volume_ * price_).ToString("G6",true)} @ {price.ToString("G6",true)}");

			// Add the position to the Positions collection so that there is no race condition
			// between placing an order and checking 'Positions' for the order just placed.
			if (order_result.OrderId != 0)
			{
				// It is possible for the 'Positions' collection to be updated between 'CreateOrderInternal'
				// and here, therefore we can't use 'Add' because the key may already be in the dictionary
				var pos = new Position(order_result.OrderId, pair, tt, price, volume, volume, now, now, fake:fake);
				Positions[order_result.OrderId] = pos;

				// Update the hold with a 'StillNeeded' function
				if (fake)
					bal.Hold(hold_id, b => Positions[order_result.OrderId] != null);
			}

			// The order may have also been completed or partially filled. Add the filled orders to the trade history
			foreach (var tid in order_result.TradeIds)
			{
				//hack - all 'order_results' should return an order id, except Cryptopia doesn't when the order is
				// filled immediately. As a work around, use the ids of the filled trades as the order id. This
				// means we can't match orders to the trades that filled them though.
				var order_id = order_result.OrderId != 0 ? order_result.OrderId : tid;
				var fill = History.GetOrAdd(order_id, tt, pair);
				fill.Trades[tid] = new Historic(order_id, tid, pair, tt, price, volume, price*volume*Fee, now, now);
			}

			// Remove any orders we might have filled from the order book.
			tt.OrderBook(pair).Consume(pair, price, volume, out var remaining);

			// Trigger updates of market data
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;

			return order_result;
		}
		protected abstract TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price);
		private ulong m_fake_order_number;

		/// <summary>Cancel an existing position</summary>
		public bool CancelOrder(TradePair pair, ulong order_id)
		{
			// Obey the global trade switch
			var result = 
				Model.AllowTrades ? CancelOrderInternal(pair, order_id) :
				Model.BackTesting ? Model.Simulation[this].CancelOrderInternal(pair, order_id) :
				true;

			// Remove the position from the Positions collection so that there is no race condition
			Positions.RemoveIf(x => x.Pair == pair && x.OrderId == order_id);

			// Trigger a positions and balances update
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
			return result;
		}
		protected abstract bool CancelOrderInternal(TradePair pair, ulong order_id);

		/// <summary>Returns the time frames for which candle data is available for 'pair'</summary>
		public IEnumerable<ETimeFrame> CandleDataAvailable(TradePair pair)
		{
			if (pair.Exchange != this)
				throw new Exception($"Trade pair {pair.NameWithExchange} is not provided by this exchange ({Name})");

			// Back-testing sim exchanges provide the same time frames as the real exchanges
			return CandleDataAvailableInternal(pair);
		}
		protected virtual IEnumerable<ETimeFrame> CandleDataAvailableInternal(TradePair pair)
		{
			yield break;
		}

		/// <summary>Return the candle data for a given pair, over a given time range</summary>
		public List<Candle> CandleData(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end, CancellationToken? cancel)
		{
			try
			{
				if (Model.BackTesting) throw new Exception("Shouldn't require candle data while back testing");
				using (Scope.Create(() => CandleDataUpdateInProgress = true, () => CandleDataUpdateInProgress = false))
					return CandleDataInternal(pair, timeframe, time_beg, time_end, cancel);
			}
			catch (Exception ex)
			{
				HandleException(nameof(Exchange.CandleData), ex, $"PriceData {pair.NameWithExchange} get chart data failed.");
				return null;
			}
		}
		protected virtual List<Candle> CandleDataInternal(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end, CancellationToken? cancel)
		{
			return new List<Candle>();
		}
		public bool CandleDataUpdateInProgress { get; private set; }

		/// <summary>Return the order book for 'pair' to a depth of 'depth'</summary>
		public MarketDepth MarketDepth(TradePair pair, int depth)
		{
			return Model.BackTesting
				? Model.Simulation[this].MarketDepthInternal(pair, depth)
				: MarketDepthInternal(pair, depth);
		}
		protected virtual MarketDepth MarketDepthInternal(TradePair pair, int count)
		{
			return new MarketDepth(pair.Base, pair.Quote);
		}

		/// <summary>Update the collections of coins and pairs</summary>
		public void UpdatePairs(HashSet<string> coins_of_interest) // Worker thread context
		{
			// Allow update pairs in back testing mode as well
			UpdatePairsInternal(coins_of_interest);
		}
		protected virtual void UpdatePairsInternal(HashSet<string> coins_of_interest)
		{
			// This method should wait for server responses in worker threads,
			// collect data in local buffers, then use 'RunOnGuiThread' to copy
			// data to the Exchange's main collections.
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected virtual void UpdateData() // Worker thread context
		{
			Pairs.LastUpdated = Model.UtcNow;
		}

		/// <summary>Update the account balances</summary>
		protected virtual void UpdateBalances()  // Worker thread context
		{
			Balance.LastUpdated = Model.UtcNow;
		}

		/// <summary>Update all open positions</summary>
		protected virtual void UpdatePositionsAndHistory() // Worker thread context
		{
			// Do history and positions together so there's no change of a position being
			// filled without it appearing in the history
			History.LastUpdated = Model.UtcNow;
			Positions.LastUpdated = Model.UtcNow;
		}

		/// <summary>Handle an exception during an update call</summary>
		public void HandleException(string method_name, Exception ex, string msg = null)
		{
			for (; ex is AggregateException ae && ae.InnerExceptions.Count == 1; )
			{
				ex = ae.InnerExceptions[0];
			}
			if (ex is OperationCanceledException)
			{
				// Ignore operation cancelled
				return;
			}
			if (ex is HttpException he)
			{
				if (he.GetHttpCode() == (int)HttpStatusCode.ServiceUnavailable)
				{
					if (Status != EStatus.Offline)
						Model.Log.Write(ELogLevel.Warn, $"{GetType().Name} Service Unavailable");

					return;
				}
			}

			// Log all other error types
			Model.Log.Write(ELogLevel.Error, ex, $"{GetType().Name} {method_name} failed. {msg ?? string.Empty}");
		}

		/// <summary>Remove positions that are older than 'timestamp' and not in 'order_ids'</summary>
		protected void RemovePositionsNotIn(HashSet<ulong> order_ids, DateTimeOffset timestamp)
		{
			// Remove any positions that are no longer valid.
			foreach (var pos in Positions.Values.Where(x => !order_ids.Contains(x.OrderId)).ToArray())
			{
				if (pos.Created >= timestamp) continue;
				if (Model.AllowTrades == false && pos.OrderId < 100) continue; // Hack for fake positions
				Positions.Remove(pos.OrderId);
			}
		}

		/// <summary>The time range that the position history covers (in ticks)</summary>
		public Range HistoryInterval { get; protected set; }
		protected long m_history_last; // Worker thread context only

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged(PropertyChangedEventArgs args)
		{
			PropertyChanged.Raise(this, args);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}

		#region Colours
		private static Color[] Colours = new[]{ Color.DarkBlue, Color.DarkGreen, Color.DarkRed, Color.DarkMagenta, Color.DarkOrange, Color.DarkOrchid, Color.DarkKhaki };
		private static int m_colour_index;
		#endregion
	}

	#region Collections
	public class CollectionBase<TKey,TValue> :BindingDict<TKey, TValue>
	{
		public CollectionBase(Exchange exch)
		{
			Exch = exch;
			Updated = new ConditionVariable<DateTimeOffset>(Model.UtcNow);
		}
		public CollectionBase(CollectionBase<TKey,TValue> rhs)
			:this(rhs.Exch)
		{}

		/// <summary>The owning exchange</summary>
		public Exchange Exch { get; private set; }

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Exch.Model; }
		}

		/// <summary>The time when data in this collection was last updated. Note: *NOT* when collection changed, when the elements in the collection changed</summary>
		public DateTimeOffset LastUpdated
		{
			get { return m_last_updated; }
			set
			{
				m_last_updated = value;
				Updated.NotifyAll(value);
			}
		}
		private DateTimeOffset m_last_updated;

		/// <summary>Wait-able object for update notification</summary>
		public ConditionVariable<DateTimeOffset> Updated { get; private set; }
	}
	public class CoinCollection :CollectionBase<string, Coin>
	{
		public CoinCollection(Exchange exch)
			:base(exch)
		{
			KeyFrom = x => x.Symbol;
		}
		public CoinCollection(CoinCollection rhs)
			:base(rhs)
		{}

		/// <summary>Get or add a coin by symbol name</summary>
		public Coin GetOrAdd(string sym)
		{
			Debug.Assert(Model.AssertMarketDataWrite());
			return this.GetOrAdd(sym, k => new Coin(k, Exch));
		}

		/// <summary>Get/Set a coin by symbol name. Get returns null if 'sym' not in the collection</summary>
		public override Coin this[string sym]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				return TryGetValue(sym, out var coin) ? coin : null;
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
				base[sym] = value;
			}
		}
	}
	public class PairCollection :CollectionBase<string, TradePair>
	{
		public PairCollection(Exchange exch)
			:base(exch)
		{
			KeyFrom = x => x.UniqueKey;
		}
		public PairCollection(PairCollection rhs)
			:base(rhs)
		{}

		/// <summary>Get or add the pair associated with the given symbols</summary>
		public TradePair GetOrAdd(string base_, string quote, int? trade_pair_id = null)
		{
			var coinB = Exch.Coins.GetOrAdd(base_);
			var coinQ = Exch.Coins.GetOrAdd(quote);
			return this[base_, quote] ?? this.Add2(new TradePair(coinB, coinQ, Exch, trade_pair_id));
		}

		/// <summary>Get/Set the pair</summary>
		public override TradePair this[string key]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				return TryGetValue(key, out var pair) ? pair : null;
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
				if (ContainsKey(key))
					base[key].Update(value);
				else
					base[key] = value;
			}
		}

		/// <summary>Return a pair involving the given symbols (in either order)</summary>
		public TradePair this[string sym0, string sym1]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());

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
				Debug.Assert(Model.AssertMarketDataRead());

				TradePair pair;
				if (TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out pair)) return pair;
				if (TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out pair)) return pair;
				return null;
			}
		}

		/// <summary>Safety check that we're not removing Pairs that are in use</summary>
		protected override void RemoveItemCore(string key, int index)
		{
			throw new Exception("Don't remove pairs, references are being held");
		}
		protected override void SetItemCore(string key, TradePair value)
		{
			throw new Exception("Don't replace existing pairs, references are being held");
		}
	}
	public class BalanceCollection :CollectionBase<Coin, Balance>
	{
		public BalanceCollection(Exchange exch)
			:base(exch)
		{
			KeyFrom = x => x.Coin;
			SupportsSorting = false;
		}
		public BalanceCollection(BalanceCollection rhs)
			:base(rhs)
		{}

		/// <summary>Get or add a coin type that there is a balance for on the exchange</summary>
		public Balance GetOrAdd(Coin coin)
		{
			Debug.Assert(Model.AssertMarketDataWrite());
			return this.GetOrAdd(coin, x => new Balance(x, Model.UtcNow));
		}

		/// <summary>Get/Set the balance for the given coin. Returns zero balance for unknown coins</summary>
		public override Balance this[Coin coin]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				if (coin.Exchange != Exch && !(Exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				return TryGetValue(coin, out var bal) ? bal : new Balance(coin, Model.UtcNow);
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
				Debug.Assert(value.AssertValid());
				if (coin.Exchange != Exch && !(Exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				if (TryGetValue(coin, out var bal) && bal.TimeStamp > value.TimeStamp) return; // Ignore out of date data
				if (bal != null)
					bal.Update(value);
				else
					base[coin] = value;
			}
		}

		/// <summary>Get the balance by coin symbol name</summary>
		public Balance Get(string sym)
		{
			// Don't provide this, the Coin implicit cast is favoured over the overloaded method
			//@"public Balance this[string sym]"
			var coin = Exch.Coins[sym];
			return this[coin];
		}
	}
	public class PositionsCollection :CollectionBase<ulong, Position>
	{
		public PositionsCollection(Exchange exch)
			:base(exch)
		{
			KeyFrom = x => x.OrderId;
		}
		public PositionsCollection(PositionsCollection rhs)
			:base(rhs)
		{}

		/// <summary>Get/Set a position by order id</summary>
		public override Position this[ulong key]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				return TryGetValue(key, out var pos) ? pos : null;
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
				if (this[key]?.Updated> value.Updated) return; // Ignore out of date data
				base[key] = value;
			}
		}
	}
	public class HistoryCollection :CollectionBase<ulong, PositionFill>
	{
		public HistoryCollection(Exchange exch)
			:base(exch)
		{
			KeyFrom = x => x.OrderId;
		}
		public HistoryCollection(HistoryCollection rhs)
			:base(rhs)
		{}

		/// <summary>Get or Add a history entry with order id 'key' for 'pair'</summary>
		public PositionFill GetOrAdd(ulong key, ETradeType tt, TradePair pair)
		{
			Debug.Assert(Model.AssertMarketDataWrite());
			return this.GetOrAdd(key, x => new PositionFill(key, tt, pair));
		}

		/// <summary>Get/Set a history entry by order id. Returns null if 'key' is not in the collection</summary>
		public override PositionFill this[ulong key]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				return TryGetValue(key, out var pos) ? pos : null;
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
				base[key] = value;
			}
		}
	}
	#endregion
}
