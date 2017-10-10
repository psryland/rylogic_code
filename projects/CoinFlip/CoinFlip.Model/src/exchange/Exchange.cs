using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
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
			MarketDataUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			BalanceUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			PositionUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			TradeHistoryUpdatedTime = new ConditionVariable<DateTimeOffset>(DateTimeOffset.Now);
			m_update_thread_stop = new CancellationTokenSource();
			m_update_thread_step = new AutoResetEvent(false);
			TradeHistoryUseful = false;

			var history_start = (DateTimeOffset.Now - TimeSpan.FromDays(5)).Ticks;
			HistoryInterval = new Range(history_start, history_start);
			m_history_last = HistoryInterval.End;

			Status = EStatus.Offline;

			// Start the exchange if enabled in the settings
			if (Settings.Active)
				Model.RunOnGuiThread(() => UpdateThreadActive = true);

		}
		public virtual void Dispose()
		{
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
			get { return m_status; }
			protected set
			{
				// Status change can happen from any thread
				if (m_status == value) return;
				Model.RunOnGuiThread(() =>
				{
					m_status = value;
					OnStatusChanged();

					// Disable in the event of an error
					// Don't do this because it stops the bot
					//if (m_status == EStatus.Error)
					//	Active = false;
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
						UpdateThreadActive = true;
				}
			}
		}
		private Model m_model;

		/// <summary>Settings for this exchange</summary>
		public IExchangeSettings Settings { get; private set; }

		/// <summary>True if this exchange is to be used</summary>
		public bool Active
		{
			get { return Settings.Active; }
			set
			{
				if (Active == value) return;
				Settings.Active = value;
				UpdateThreadActive = value;
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Active)));
			}
		}

		/// <summary>Enable/Disable the exchange</summary>
		private bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Model.AssertMainThread());

				// Don't enable the update thread while back testing
				if (value && Model.BackTesting)
					return;

				// If previously active
				if (m_update_thread != null)
				{
					// Stop the thread
					m_update_thread_exit = true;
					m_update_thread_stop.Cancel();
					m_update_thread_step.Set();
					if (m_update_thread.IsAlive)
						m_update_thread.Join();

					Util.Dispose(ref m_update_thread_stop);
				}

				// Start the heart beat thread, if active
				Status = value ? EStatus.Connecting : EStatus.Stopped;
				m_update_thread = (value && !Model.BackTesting) ? new Thread(new ThreadStart(UpdateThreadEntryPoint)) : null;

				// On enable...
				if (m_update_thread != null)
				{
					// Trigger a positions and balances update
					PositionUpdateRequired = true;
					BalanceUpdateRequired = true;

					m_update_thread_exit = false;
					m_update_thread_stop = new CancellationTokenSource();
					m_update_thread.Start();
				}
			}
		}
		private Thread m_update_thread;
		private bool m_update_thread_exit;
		private AutoResetEvent m_update_thread_step;
		private CancellationTokenSource m_update_thread_stop;

		/// <summary>Thread entry point for the exchange update thread</summary>
		private void UpdateThreadEntryPoint()
		{
			try
			{
				Thread.CurrentThread.Name = Name;
				Model.Log.Write(ELogLevel.Debug, $"Exchange {Name} update thread started");

				var sw = new Stopwatch().Start2();
				var last_balance_update = 0L;
				var last_market_update = 0L;
				var last_position_update = 0L;
				var last_trade_history_update = 0L;

				// Create a cancel token that watches shutdown or exit thread
				var cancel_or_shutdown_src = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown, m_update_thread_stop.Token);
				var cancel_or_shutdown = cancel_or_shutdown_src.Token;

				var tasks = new List<Task>();
				for (;!m_update_thread_exit;) try
				{
					Status = EStatus.Connected;

					// Wait for the step period. If triggered early, retest exit flag
					const int MainLoopPeriodMS = 100;
					if (cancel_or_shutdown.IsCancellationRequested) break;
					if (m_update_thread_step.WaitOne(MainLoopPeriodMS))
						continue;

					// Create a collection of update tasks
					tasks.Clear();

					// Update the balances
					const int BalanceUpdatePeriodMS = 1000;
					if (BalanceUpdateRequired || (sw.ElapsedMilliseconds - last_balance_update) > BalanceUpdatePeriodMS)
					{
						BalanceUpdateRequired = false;
						tasks.Add(Task.Run(() => UpdateBalances(), cancel_or_shutdown));
						last_balance_update = sw.ElapsedMilliseconds;
					}

					// Update the existing positions
					const int PositionUpdatePeriodMS = 1000;
					if (PositionUpdateRequired || (sw.ElapsedMilliseconds - last_position_update) > PositionUpdatePeriodMS)
					{
						PositionUpdateRequired = false;
						tasks.Add(Task.Run(() => UpdatePositions(), cancel_or_shutdown));
						last_position_update = sw.ElapsedMilliseconds;
					}

					// Update trade history
					const int TradeHistoryUpdatePeriodMS = 1000;
					if (HistoryUpdateRequired || (sw.ElapsedMilliseconds - last_trade_history_update) > TradeHistoryUpdatePeriodMS)
					{
						HistoryUpdateRequired = false;
						tasks.Add(Task.Run(() => UpdateTradeHistory(), cancel_or_shutdown));
						last_trade_history_update = sw.ElapsedMilliseconds;
					}

					// Update market data at the polling rate
					if (MarketDataUpdateRequired || (sw.ElapsedMilliseconds - last_market_update) > PollPeriod)
					{
						MarketDataUpdateRequired = false;
						tasks.Add(Task.Run(() => UpdateData(), cancel_or_shutdown));
						last_market_update = sw.ElapsedMilliseconds;
					}

					Task.WhenAll(tasks).Wait(cancel_or_shutdown);
				}
				catch (OperationCanceledException) { break; }

				Status = EStatus.Stopped;
				Model.Log.Write(ELogLevel.Debug, $"Exchange {Name} update thread stopped");
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} heart beat thread exit");
				Status = EStatus.Error;
				Model.RunOnGuiThread(() => UpdateThreadActive = false);
			}
		}

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
				if (value) m_update_thread_step.Set();
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
				if (value) m_update_thread_step.Set();
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
		public bool HistoryUpdateRequired
		{
			get { return m_trade_history_update_required; }
			set
			{
				if (m_trade_history_update_required == value) return;
				m_trade_history_update_required = value;
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_trade_history_update_required;
		public Task TradeHistoryUpdated()
		{
			var now = DateTimeOffset.Now;
			HistoryUpdateRequired = true;
			return Task.Run(() => TradeHistoryUpdatedTime.Wait(ts => ts > now));
		}
		protected ConditionVariable<DateTimeOffset> TradeHistoryUpdatedTime { get; private set; }

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
			}
		}

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
		public CoinCollection Coins
		{
			get { return Model.BackTesting ? Model.Simulation[this].Coins : m_coins; }
			private set { m_coins = value; }
		}
		private CoinCollection m_coins;

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs
		{
			get { return Model.BackTesting ? Model.Simulation[this].Pairs : m_pairs; }
			private set { m_pairs = value; }
		}
		private PairCollection m_pairs;

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance
		{
			get { return Model.BackTesting ? Model.Simulation[this].Balance : m_balance; }
			private set { m_balance = value; }
		}
		private BalanceCollection m_balance;

		/// <summary>Open positions held on this exchange, keyed on order ID</summary>
		public PositionsCollection Positions
		{
			get { return Model.BackTesting ? Model.Simulation[this].Positions : m_positions; }
			private set { m_positions = value; }
		}
		private PositionsCollection m_positions;

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public HistoryCollection History
		{
			get { return Model.BackTesting ? Model.Simulation[this].History : m_history; }
			private set { m_history = value; }
		}
		private HistoryCollection m_history;

		/// <summary>Raised when the 'Status' value changes</summary>
		public event EventHandler StatusChanged;
		protected virtual void OnStatusChanged()
		{
			if (Model == null || Model.Exchanges == null) return;
			StatusChanged.Raise(this);
			Model.Exchanges.ResetItem(this, ignore_missing:true);
		}

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

			// Convert the volume to base currency and the price to quote/base
			var volume = tt == ETradeType.B2Q ? volume_ : (price_ * volume_);
			var price  = tt == ETradeType.B2Q ? price_  : (1m / price_);
			var now = DateTimeOffset.Now;

			// Set the Id for this fake order
			if (!Model.AllowTrades)
				++m_fake_order_number;

			// Put a hold on the balance we're about to trade to prevent a race condition
			// with other trades being placed while we wait for this one to go through.
			// Only reduce balances, don't assume the trade has completed.
			// If we're live trading, remove the balance until the next balance update is received.
			// If not live trading, hold the balance until the fake order is removed.
			var bal = tt == ETradeType.B2Q ? pair.Base.Balance : pair.Quote.Balance;
			var hold = tt == ETradeType.B2Q ? volume : (volume * price);
			if (Model.AllowTrades)
				bal.Hold(hold);
			else
				bal.Hold(hold, b => Positions[m_fake_order_number] != null);

			// Make the trade
			// This can have the following results:
			// 1) the entire trade is added to the order book for the pair -> a single order number is returned
			// 2) the entire trade can be met by existing orders in the order book -> a collection of trade IDs is returned
			// 3) some of the trade can be met by existing orders -> a single order number and a collection of trade IDs are returned.
			var order_result = Model.AllowTrades // Obey the global trade switch
				? CreateOrderInternal(pair, tt, volume, price)
				: new TradeResult(pair, m_fake_order_number, new ulong[0]);

			// Simulate the delay of submitting a trade when 'AllowTrades' is not enabled
			if (!Model.AllowTrades)
				Thread.Sleep(800);

			// Log the event
			Model.Log.Write(ELogLevel.Info, "{0}: (id={1}) {2} {3} → {4} {5} @ {6} {7}".Fmt(
				Name, order_result.OrderId,
				(volume_         ).ToString("G6"), tt == ETradeType.B2Q ? pair.Base  : pair.Quote,
				(volume_ * price_).ToString("G6"), tt == ETradeType.B2Q ? pair.Quote : pair.Base,
				price.ToString("G6"), pair.RateUnits));

			// Add the position to the Positions collection so that there is no race condition
			// between placing an order and checking 'Positions' for the order just placed.
			if (order_result.OrderId != 0)
			{
				// It is possible for the 'Positions' collection to be updated between 'CreateOrderInternal'
				// and here, therefore we can't use 'Add' because the key may already be in the dictionary
				var pos = new Position(order_result.OrderId, pair, tt, price, volume, volume, now, now, fake:!Model.AllowTrades);
				Positions[order_result.OrderId] = pos;
			}

			// The order may have also been completed or partially filled. Add the filled orders to the trade history
			foreach (var tid in order_result.TradeIds)
			{
				//hack - all 'order_results' should return an order id, except Cryptopia doesn't when the order is
				// filled immediately. As a work around, use the ids of the filled trades as the order id. This
				// means we can't match orders to the trades that filled them though.
				var order_id = order_result.OrderId != 0 ? order_result.OrderId : tid;
				var fill = History.GetOrAdd(order_id, tt, pair);
				fill.Trades[tid] = new Historic(order_id, tid, pair, tt, price, volume, volume*price, volume*price*Fee, now, now);
			}

			// Remove any orders we might have filled from the order book.
			if (tt == ETradeType.B2Q)
				pair.B2Q.RemoveOrders(-1, volume, price);
			else
				pair.Q2B.RemoveOrders(+1, volume, price);

			// Trigger updates of market data
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
			HistoryUpdateRequired = true;

			return order_result;
		}
		protected abstract TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price);
		private ulong m_fake_order_number;

		/// <summary>Cancel an existing position</summary>
		public void CancelOrder(TradePair pair, ulong order_id)
		{
			// Obey the global trade switch
			if (Model.AllowTrades)
				CancelOrderInternal(pair, order_id);

			// Remove the position from the Positions collection so that there is no race condition
			Positions.RemoveIf(x => x.Pair == pair && x.OrderId == order_id);

			// Trigger a positions and balances update
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
		}
		protected abstract void CancelOrderInternal(TradePair pair, ulong order_id);

		/// <summary>Returns the time frames for which chart data is available for 'pair'</summary>
		public virtual IEnumerable<ETimeFrame> ChartDataAvailable(TradePair pair)
		{
			yield break;
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		public List<Candle> ChartData(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end)
		{
			try
			{
				return ChartDataInternal(pair, timeframe, time_beg, time_end);
			}
			catch (Exception ex)
			{
				HandleException(nameof(Exchange.ChartData), ex, $"PriceData {pair.NameWithExchange} get chart data failed.");
				return null;
			}
		}
		protected virtual List<Candle> ChartDataInternal(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end)
		{
			return new List<Candle>();
		}

		/// <summary>Return the order book for 'pair' to a depth of 'depth'</summary>
		public MarketDepth MarketDepth(TradePair pair, int depth)
		{
			return MarketDepthInternal(pair, depth);
		}
		protected virtual MarketDepth MarketDepthInternal(TradePair pair, int count)
		{
			return new MarketDepth(pair.Base, pair.Quote);
		}

		/// <summary>Update the collections of coins and pairs</summary>
		public void UpdatePairs(HashSet<string> coins_of_interest) // Worker thread context
		{
			// Only update pairs when not back testing. Back testing uses the pairs and
			// coins that were available at the time back testing is enabled.
			if (!Model.BackTesting)
				UpdatePairsInternal(coins_of_interest);
		}

		/// <summary>Update the collections of coins and pairs available on this exchange</summary>
		protected virtual void UpdatePairsInternal(HashSet<string> coins_of_interest)
		{
			// This method should await server responses, collect data in local buffers
			// then use 'RunOnGuiThread' to copy data to the Exchange's main collections.
			Model.RunOnGuiThread(() => { }, block:true);
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected virtual void UpdateData() // Worker thread context
		{
			// This method should await all server responses, then add an action
			// to the Model.MarketUpdates collection for integration at a suitable time.
			lock (MarketDataUpdatedTime)
				MarketDataUpdatedTime.NotifyOne(MarketDataUpdatedTime, DateTimeOffset.Now);
		}

		/// <summary>Update the account balances</summary>
		protected virtual void UpdateBalances()  // Worker thread context
		{
			BalanceUpdatedTime.NotifyOne(DateTimeOffset.Now);
		}

		/// <summary>Update all open positions</summary>
		protected virtual void UpdatePositions() // Worker thread context
		{
			PositionUpdatedTime.NotifyOne(DateTimeOffset.Now);
		}

		/// <summary>Update the trade history</summary>
		protected virtual void UpdateTradeHistory() // Worker thread context
		{
			TradeHistoryUpdatedTime.NotifyOne(DateTimeOffset.Now);
		}

		/// <summary>Handle an exception during an update call</summary>
		public void HandleException(string method_name, Exception ex, string msg = null)
		{
			for (; ex is AggregateException ae && ae.InnerExceptions.Count == 1; )
			{
				ex = ae.InnerExceptions[0];
			}
			if (ex is OperationCanceledException || ex is TaskCanceledException)
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

					Status = EStatus.Offline;
					return;
				}
			}

			// Log all other error types
			Model.Log.Write(ELogLevel.Error, ex, $"{GetType().Name} {method_name} failed. {msg ?? string.Empty}");
			//Status = EStatus.Error;
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

		/// <summary>Set the maximum number of requests per second to the exchange server</summary>
		protected abstract void SetServerRequestRateLimit(float limit);

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
		private readonly Exchange m_exch;
		public CollectionBase(Exchange exch)
		{
			m_exch = exch;
		}
		public CollectionBase(CollectionBase<TKey,TValue> rhs)
			:base(rhs)
		{
			m_exch = rhs.m_exch;
		}
		public Exchange Exch
		{
			get { return m_exch; }
		}
		public Model Model
		{
			get { return m_exch.Model; }
		}
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
			return this.GetOrAdd(coin, x => new Balance(x));
		}

		/// <summary>Get/Set the balance for the given coin. Returns zero balance for unknown coins</summary>
		public override Balance this[Coin coin]
		{
			get
			{
				Debug.Assert(Model.AssertMarketDataRead());
				if (coin.Exchange != Exch && !(Exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				return TryGetValue(coin, out var bal) ? bal : new Balance(coin);
			}
			set
			{
				Debug.Assert(Model.AssertMarketDataWrite());
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
