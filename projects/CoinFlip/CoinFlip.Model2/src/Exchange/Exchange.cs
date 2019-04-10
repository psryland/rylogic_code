using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Threading;
using CoinFlip.Settings;
using Dapper;
using ExchApi.Common;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Base class for exchanges</summary>
	[DebuggerDisplay("{Name,nq}")]
	public abstract class Exchange : IDisposable, INotifyPropertyChanged, IComparable<Exchange>, IComparable
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

		public Exchange(IExchangeSettings exch_settings, CoinDataList coin_data, CancellationToken shutdown)
		{
			try
			{
				ExchSettings = exch_settings;
				m_main_shutdown = shutdown;
				Colour = Colours[m_colour_index++ % Colours.Length];
				Coins = new CoinCollection(this, coin_data);
				Pairs = new PairCollection(this);
				Balance = new BalanceCollection(this);
				Orders = new OrdersCollection(this);
				History = new OrdersCompletedCollection(this);
				OrderIdtoFundId = new OrderIdtoFundIdMap();
				Transfers = new TransfersCollection(this);
				Shutdown = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
				m_update_thread_step = new AutoResetEvent(false);
				TradeHistoryUseful = false;

				// Initialise the trade history table
				InitTradeHistoryTable();

				// Run after the exchange is fully constructed
				Misc.RunOnMainThread(() =>
				{
					// Set the request rate limit
					ExchangeApi.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;

					// Start the exchange if enabled in the settings
					if (ExchSettings.Active)
					{
						if (!(this is CrossExchange))
						{
							PairsUpdateRequired = true;
							BalanceUpdateRequired = true;
							PositionUpdateRequired = true;
							TransfersUpdateRequired = true;
							MarketDataUpdateRequired = true;
						}
						UpdateThreadActive = true;
					}
				});
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public virtual void Dispose()
		{
			UpdateThreadActive = false;
			Shutdown = null;
			DB = null;
		}

		/// <summary>The name of this exchange</summary>
		public string Name => GetType().Name;

		/// <summary>The current status of the exchange</summary>
		public EExchangeStatus Status
		{
			get
			{
				var conn_status = 
					!Enabled ? EExchangeStatus.Stopped :
					Model.BackTesting ? EExchangeStatus.Simulated :
					UpdateThreadActive ? EExchangeStatus.Connected :
					EExchangeStatus.Offline;
				var api_status =
					ExchSettings.PublicAPIOnly ? EExchangeStatus.PublicAPIOnly : 0;
				return conn_status | api_status;
			}
		}

		/// <summary>Settings for this exchange</summary>
		public IExchangeSettings ExchSettings
		{
			get { return m_exch_settings; }
			private set
			{
				if (m_exch_settings == value) return;
				if (m_exch_settings != null)
				{
					m_exch_settings.SettingChange -= HandleSettingChange;
				}
				m_exch_settings = value;
				if (m_exch_settings != null)
				{
					m_exch_settings.SettingChange += HandleSettingChange;
				}

				// Handler
				void HandleSettingChange(object sender, SettingChangeEventArgs e)
				{
					switch (e.Key)
					{
					case nameof(IExchangeSettings.Active):
						{
							NotifyPropertyChanged(nameof(Enabled));
							NotifyPropertyChanged(nameof(Status));
							break;
						}
					case nameof(IExchangeSettings.PublicAPIOnly):
						{
							NotifyPropertyChanged(nameof(Status));
							break;
						}
					case nameof(IExchangeSettings.PollPeriod):
						{
							NotifyPropertyChanged(nameof(PollPeriod));
							break;
						}
					case nameof(IExchangeSettings.ServerRequestRateLimit):
						{
							ExchangeApi.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
							break;
						}
					}
				}
			}
		}
		private IExchangeSettings m_exch_settings;

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
		private CancellationToken m_main_shutdown;

		/// <summary>The common interface for all exchange API objects</summary>
		protected abstract IExchangeApi ExchangeApi { get; }

		/// <summary>True if this exchange is to be used</summary>
		public bool Enabled
		{
			get { return ExchSettings.Active; }
			set
			{
				if (Enabled == value) return;
				ExchSettings.Active = value;
				UpdateThreadActive = value;
			}
		}

		/// <summary>True when in back testing mode</summary>
		public bool BackTesting => Sim != null;

		/// <summary>Get/Set the simulation. A non-null simulation implies 'BackTesting'</summary>
		public SimExchange Sim
		{
			get { return m_sim; }
			set
			{
				if (m_sim == value) return;
				if (m_sim != null)
				{
				}
				m_sim = value;
				if (m_sim != null)
				{

				}

				// The update thread only runs when not back testing and enabled
				UpdateThreadActive = Sim == null && Enabled;

				// Reconnect to the appropriate trade history database
				InitTradeHistoryTable();

				// Notify back testing changed
				NotifyPropertyChanged(nameof(BackTesting));
			}
		}
		private SimExchange m_sim;

		/// <summary>Enable/Disable the exchange update thread</summary>
		public bool UpdateThreadActive
		{
			get { return m_update_thread != null; }
			private set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Misc.AssertMainThread());

				// Don't enable inactive exchanges
				if (value && !Enabled)
					throw new Exception("Don't enable inactive exchanges");

				// Don't enable the update thread while back testing
				if (value && BackTesting)
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
					Shutdown = CancellationTokenSource.CreateLinkedTokenSource(m_main_shutdown);
					m_update_thread.Start();
				}

				// Notify updating
				NotifyPropertyChanged(nameof(Status));

				/// <summary>Thread entry point for the exchange update thread</summary>
				async void UpdateThreadEntryPoint()
				{
					try
					{
						Thread.CurrentThread.Name = Name;
						Model.Log.Write(ELogLevel.Debug, $"Exchange {Name} update thread started");

						// Note: there is no point in trying to run the updates in parallel because
						// the 'nonce' system requires each query to be sequential.
						for (; !m_update_thread_exit;)
						{
							try
							{
								// Wait for the step period. If triggered early, retest exit flag
								const int MainLoopPeriodMS = 100;
								if (Shutdown.IsCancellationRequested) break;
								if (m_update_thread_step.WaitOne(MainLoopPeriodMS))
									continue;

								// Update pairs
								if (PairsUpdateRequired)
								{
									PairsUpdateRequired = false;
									await UpdatePairs();
								}

								// Update market data
								const int MarketDataUpdatePeriodMS = 100;
								if (MarketDataUpdateRequired || (Model.UtcNow - Pairs.LastUpdated).TotalMilliseconds > MarketDataUpdatePeriodMS)
								{
									MarketDataUpdateRequired = false;
									await UpdateData();
								}

								// Update the balances
								const double BalanceUpdatePeriodMS = 1000;
								if (!ExchSettings.PublicAPIOnly && (BalanceUpdateRequired || (Model.UtcNow - Balance.LastUpdated).TotalMilliseconds > BalanceUpdatePeriodMS))
								{
									BalanceUpdateRequired = false;
									await UpdateBalances();
								}

								// Update funds transfers
								const int TransfersUpdatePeriodMS = 60000;
								if (!ExchSettings.PublicAPIOnly && (TransfersUpdateRequired || (Model.UtcNow - Transfers.LastUpdated).TotalMilliseconds > TransfersUpdatePeriodMS))
								{
									TransfersUpdateRequired = false;
									await UpdateTransfers();
								}

								// Update positions/history
								const int PositionUpdatePeriodMS = 1000;
								if (!ExchSettings.PublicAPIOnly && (PositionUpdateRequired || (Model.UtcNow - Orders.LastUpdated).TotalMilliseconds > PositionUpdatePeriodMS))
								{
									PositionUpdateRequired = false;
									await UpdatePositionsAndHistory();
								}
							}
							catch (OperationCanceledException) { break; }
						}
					}
					catch (Exception ex)
					{
						Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} update thread exit");
						Misc.RunOnMainThread(() => UpdateThreadActive = false);
					}
				}
			}
		}
		private Thread m_update_thread;
		private AutoResetEvent m_update_thread_step;
		private volatile bool m_update_thread_exit;

		/// <summary>The rate that 'Heart' beats at</summary>
		public int PollPeriod => ExchSettings.PollPeriod;

		/// <summary>Dirty flag for updating pairs</summary>
		public bool PairsUpdateRequired
		{
			get { return m_pairs_update_required; }
			set
			{
				if (m_pairs_update_required == value) return;
				m_pairs_update_required = value;
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_pairs_update_required;

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

		/// <summary>Dirty flag for fund transfers data</summary>
		public bool TransfersUpdateRequired
		{
			get { return m_transfers_update_required; }
			set
			{
				if (m_transfers_update_required == value) return;
				m_transfers_update_required = value;
				if (value) m_update_thread_step.Set();
			}
		}
		private bool m_transfers_update_required;

		/// <summary>True if TradeHistory can be mapped to previous order id's</summary>
		public bool TradeHistoryUseful { get; protected set; }

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public decimal Fee => ExchSettings.TransactionFee;

		/// <summary>The value of all coins held on this exchange</summary>
		public decimal NettWorth
		{
			get
			{
				if (this is CrossExchange) return 0m;
				return Balance.Values.Sum(x => (decimal)x.Coin.ValueOf(x.NettTotal));
			}
		}

		/// <summary>The number of coins available on this exchange</summary>
		public int CoinsAvailable => Coins.Count;

		/// <summary>The number of trading pairs available</summary>
		public int PairsAvailable => Pairs.Count;

		/// <summary>Return the coins available on this exchange that are coins of interest</summary>
		public IEnumerable<Coin> CoinsOfInterest => Coins.Values.Where(x => x.OfInterest);

		/// <summary>An identifying colour for the exchange</summary>
		public uint Colour { get; set; }

		/// <summary>The coins associated with this exchange</summary>
		public CoinCollection Coins { get; }

		/// <summary>The pairs associated with this exchange</summary>
		public PairCollection Pairs { get; }

		/// <summary>The balance of the given coin on this exchange</summary>
		public BalanceCollection Balance { get; }

		/// <summary>Open orders held on this exchange, keyed on order ID</summary>
		public OrdersCollection Orders { get; }

		/// <summary>Funds transfers on this exchange</summary>
		public TransfersCollection Transfers { get; }

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public OrdersCompletedCollection History { get; }

		/// <summary>A map from order id to the context id that created the order</summary>
		public OrderIdtoFundIdMap OrderIdtoFundId { get; }

		/// <summary>Update the collections of coins and pairs</summary>
		public async Task UpdatePairs() // Worker thread context
		{
			// Allow update pairs in back testing mode as well

			// Create a set of coins to find pairs for. Include any intermediate
			// coins needed to find the live price of coins as well
			var coins = new HashSet<string>();
			foreach (var coin in SettingsData.Settings.Coins)
			{
				coins.Add(coin.Symbol);
				coins.AddRange(coin.LivePriceSymbolsArray);
			}

			await UpdatePairsInternal(coins);
		}
		protected virtual Task UpdatePairsInternal(HashSet<string> coins)
		{
			// This method should wait for server responses in worker threads,
			// collect data in local buffers, then use 'RunOnMainThread' to copy
			// data to the Exchange's main collections.
			return Task.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected virtual Task UpdateData() // Worker thread context
		{
			Pairs.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Return the order book for 'pair' to a depth of 'depth'</summary>
		public async Task<MarketDepth> MarketDepth(TradePair pair, int depth) // Worker thread context
		{
			return BackTesting
				? m_sim.MarketDepthInternal(pair, depth)
				: await MarketDepthInternal(pair, depth);
		}
		protected virtual Task<MarketDepth> MarketDepthInternal(TradePair pair, int count) // Worker thread context
		{
			return Task.FromResult(new MarketDepth(pair.Base, pair.Quote));
		}

		/// <summary>Return the candle data for a given pair, over a given time range</summary>
		public async Task<List<Candle>> CandleData(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			try
			{
				if (BackTesting)
					throw new Exception("Shouldn't require candle data while back testing");

				using (Scope.Create(() => CandleDataUpdateInProgress = true, () => CandleDataUpdateInProgress = false))
					return await CandleDataInternal(pair, timeframe, time_beg, time_end, cancel);
			}
			catch (Exception ex)
			{
				HandleException(nameof(Exchange.CandleData), ex, $"PriceData {pair.NameWithExchange} get chart data failed.");
				return null;
			}
		}
		protected virtual Task<List<Candle>> CandleDataInternal(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			return Task.FromResult(new List<Candle>());
		}
		public bool CandleDataUpdateInProgress { get; private set; }

		/// <summary>Update the account balances</summary>
		protected virtual Task UpdateBalances()  // Worker thread context
		{
			Balance.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Update all open positions</summary>
		protected virtual Task UpdatePositionsAndHistory() // Worker thread context
		{
			// Do history and positions together so there's no change of a position being
			// filled without it appearing in the history
			History.LastUpdated = Model.UtcNow;
			Orders.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Update all deposits and withdrawals made on this exchange</summary>
		protected virtual Task UpdateTransfers() // Worker thread context
		{
			return Task.CompletedTask;
		}

		/// <summary>Cancel an existing order</summary>
		public async Task<bool> CancelOrder(TradePair pair, long order_id)
		{
			// Obey the global trade switch
			var result =
				BackTesting ? m_sim.CancelOrderInternal(pair, order_id) :
				Model.AllowTrades ? await CancelOrderInternal(pair, order_id) :
				true;

			// Remove the position from the Positions collection so that there is no race condition
			Orders.RemoveIf(x => x.Pair == pair && x.OrderId == order_id);

			// Trigger a positions and balances update
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
			return result;
		}
		protected abstract Task<bool> CancelOrderInternal(TradePair pair, long order_id);

		/// <summary>Place an order on the exchange to buy/sell 'amount' (currency depends on 'tt')</summary>
		public async Task<OrderResult> CreateOrder(string fund_id, ETradeType tt, TradePair pair, Unit<decimal> amount_, Unit<decimal> price_)
		{
			// 'fund_id' is the context id of the entity creating the trade

			// Sanity checks
			if (pair.Exchange != this)
			{
				throw new Exception($"Pair {pair} is not provided by this exchange");
			}
			if (tt == ETradeType.B2Q)
			{
				if (amount_ <= 0m._(pair.Base))
					throw new Exception($"Invalid trade amount: {amount_}");
				if (price_ <= 0m._(pair.Quote) / 1m._(pair.Base))
					throw new Exception($"Invalid exchange rate: {price_}");
				if (amount_ > Balance[pair.Base][fund_id].Available)
					throw new Exception($"Order amount is greater than the current balance: {Balance[pair.Base][fund_id].Available}");
			}
			if (tt == ETradeType.Q2B)
			{
				if (amount_ <= 0m._(pair.Quote))
					throw new Exception($"Invalid trade amount: {amount_}");
				if (price_ <= 0m._(pair.Base) / 1m._(pair.Quote))
					throw new Exception($"Invalid exchange rate: {price_}");
				if (amount_ > Balance[pair.Quote][fund_id].Available)
					throw new Exception($"Order amount is greater than the current balance: {Balance[pair.Quote][fund_id].Available}");
			}

			// Convert the amount to base currency and the price to quote/base
			var amount = tt.AmountIn(amount_, price_);
			var price = tt.PriceQ2B(price_);
			var now = Model.UtcNow;

			// Put a hold on the balance we're about to trade to prevent a race condition
			// with other trades being placed while we wait for this one to go through.
			// Only reduce balances, don't assume the trade has completed.
			// If we're live trading, remove the balance until the next balance update is received.
			// If not live trading, hold the balance until the fake order is removed (hold is updated below)
			var bal = tt.CoinIn(pair).Balances[fund_id];
			var hold = tt.AmountIn(amount, price);
			var hold_id = bal.Hold(hold);

			// Fake positions when not back testing and not live trading.
			// Back testing looks like live trading because of the emulated exchanges.
			var fake = Model.AllowTrades == false && BackTesting == false;

			// Make the trade
			// This can have the following results:
			// 1) the entire trade is added to the order book for the pair -> a single order number is returned
			// 2) the entire trade can be met by existing orders in the order book -> a collection of trade IDs is returned
			// 3) some of the trade can be met by existing orders -> a single order number and a collection of trade IDs are returned.
			var result =
				BackTesting ? m_sim.CreateOrderInternal(pair, tt, amount, price) :
				Model.AllowTrades ? await CreateOrderInternal(pair, tt, amount, price) :
				new OrderResult(pair, ++m_fake_order_number, filled: false);

			// Log the event
			Model.Log.Write(ELogLevel.Info, $"{Name}: (id={result.OrderId}) {amount_.ToString("G6", true)} → {(amount_ * price_).ToString("G6", true)} @ {price.ToString("G6", true)}");

			// Save a mapping from order id to fund id. This records who was responsible for creating the order.
			OrderIdtoFundId[result.OrderId] = fund_id;

			// Add the position to the Positions collection so that there is no race condition
			// between placing an order and checking 'Positions' for the order just placed.
			if (!result.Filled)
			{
				// Add a 'Position' to the collection, this will be overwritten on the next update.
				var order = new Order(fund_id, result.OrderId, pair, tt, price, amount, amount, now, now, fake: fake);
				Orders[result.OrderId] = order;

				// Update the hold with a 'StillNeeded' function
				if (fake)
					bal.Hold(hold_id, b => Orders[result.OrderId] != null);
			}

			// The order may have also been completed or partially filled. Add the filled orders to the trade history.
			foreach (var tid in result.TradeIds)
			{
				var fill = History.GetOrAdd(result.OrderId, tt, pair);
				fill.Trades[tid] = new TradeCompleted(result.OrderId, tid, pair, tt, price, amount, price * amount * Fee, now, now);
			}

			// Remove entries from the order book that this order should have filled.
			// This will be overwritten with the next update.
			pair.OrderBook(tt).Consume(pair, price, amount, out var remaining);

			// Trigger updates
			MarketDataUpdateRequired = true;
			PositionUpdateRequired = true;
			BalanceUpdateRequired = true;
			return result;
		}
		protected abstract Task<OrderResult> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price);
		private long m_fake_order_number;

		/// <summary>Enumerate all candle data and time frames provided by this exchange</summary>
		public IEnumerable<PairAndTF> EnumAvailableCandleData(TradePair pair = null)
		{
			if (pair != null && pair.Exchange != this)
				throw new Exception($"Trade pair {pair.NameWithExchange} is not provided by this exchange ({Name})");

			return EnumAvailableCandleDataInternal(pair);
		}
		protected virtual IEnumerable<PairAndTF> EnumAvailableCandleDataInternal(TradePair pair)
		{
			yield break;
		}

		/// <summary>Handle an exception during an update call</summary>
		public void HandleException(string method_name, Exception ex, string msg = null)
		{
			for (; ex is AggregateException ae && ae.InnerExceptions.Count == 1;)
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
				if (he.GetHttpCode() == (int)HttpStatusCode.Forbidden)
				{
					Model.Log.Write(ELogLevel.Warn, $"{Name} reported 'Forbidden'. Switching to public only API");
					ExchSettings.PublicAPIOnly = true;
					NotifyPropertyChanged(nameof(Status));
					return;
				}
				if (he.GetHttpCode() == (int)HttpStatusCode.ServiceUnavailable)
				{
					if (Status != EExchangeStatus.Offline)
						Model.Log.Write(ELogLevel.Warn, $"{Name} Exchange Unavailable");

					return;
				}
			}

			// Log all other error types
			Model.Log.Write(ELogLevel.Error, ex, $"{GetType().Name} {method_name} failed. {msg ?? string.Empty}");
		}

		/// <summary>Remove positions that are older than 'timestamp' and not in 'order_ids'</summary>
		protected void RemovePositionsNotIn(HashSet<long> order_ids, DateTimeOffset timestamp)
		{
			// Remove any positions that are no longer valid.
			foreach (var pos in Orders.Values.Where(x => !order_ids.Contains(x.OrderId)).ToArray())
			{
				if (pos.Created >= timestamp) continue;
				if (Model.AllowTrades == false && pos.OrderId < 100) continue; // Hack for fake positions
				Orders.Remove(pos.OrderId);
			}
		}

		/// <summary>Database connection</summary>
		private SQLiteConnection DB
		{
			[DebuggerStepThrough]
			get { return m_db; }
			set
			{
				if (m_db == value) return;
				if (m_db != null)
				{
					m_db.Close();
					Util.Dispose(ref m_db);
				}
				m_db = value;
				if (m_db != null)
				{
					m_db.Open();
				}
			}
		}
		private SQLiteConnection m_db;

		/// <summary>The table name for the trade history</summary>
		private string DBFilepath => BackTesting
			? Misc.ResolveUserPath("Sim", "History", $"TradeHistory-{Name}.db")
			: Misc.ResolveUserPath("History",$"TradeHistory-{Name}.db");

		/// <summary>DB table name for the trade history</summary>
		private string DBHistoryTableName => "History";

		/// <summary>Initialise the trade history table</summary>
		private void InitTradeHistoryTable()
		{
			// Release the connection to the DB
			DB = null;

			// In back testing mode, delete the history first
			if (BackTesting)
				Path_.DelFile(DBFilepath);

			// Connect
			DB = new SQLiteConnection($"Data Source={DBFilepath};Version=3;journal mode=Memory;synchronous=Off");

			// Ensure the trade history table exists
			DB.Execute(
				$"create table if not exists {DBHistoryTableName} (\n" +
				$"  [{nameof(TradeRecord.TradeId)}] integer unique primary key,\n" +
				$"  [{nameof(TradeRecord.OrderId)}] integer unique,\n" +
				$"  [{nameof(TradeRecord.Created)}] integer,\n" +
				$"  [{nameof(TradeRecord.Pair)}] text,\n" +
				$"  [{nameof(TradeRecord.TradeType)}] text,\n" +
				$"  [{nameof(TradeRecord.PriceQ2B)}] real,\n" +
				$"  [{nameof(TradeRecord.AmountBase)}] real,\n" +
				$"  [{nameof(TradeRecord.CommissionQuote)}] real\n" +
				$")");

			// Set the interval of available history.
			HistoryInterval = new Range(
				DateTimeOffset_.UnixEpoch.Ticks,
				DateTimeOffset_.UnixEpoch.Ticks);

			// Set up the cache of known trade ids
			TradeHistoryTradeIds = DB.Query<long>(
				$"select [{nameof(TradeCompleted.TradeId)}] from {DBHistoryTableName}")
				.ToHashSet();

			// Set the interval of available funds transfer history
			TransfersInterval = new Range(
				DateTimeOffset_.UnixEpoch.Ticks,
				DateTimeOffset_.UnixEpoch.Ticks);

			// Set the time to get history from
			m_history_last = new DateTimeOffset(HistoryInterval.End, TimeSpan.Zero);
			m_transfers_last = new DateTimeOffset(TransfersInterval.End, TimeSpan.Zero);
		}

		/// <summary>The set of known trade ids</summary>
		private HashSet<long> TradeHistoryTradeIds { get; set; }

		/// <summary>Add a new or modified PositionFill to the trade history DB</summary>
		public void AddToTradeHistory(OrderCompleted fill)
		{
			// Notes:
			// - This doesn't happen when 'Model.History' is accessed/added to by derived exchanges
			//   because adding 'OrderCompleted' instances to a 'PositionFill' would not raise notifications.
			//   Also, 'PositionFill' instances are added as empty instances and then populated.

			if (fill.Exchange != this)
				throw new Exception("This position fill did not occur on this exchange");

			// Get the trades not already in the DB
			var trades = fill.Trades.Values.Where(x => !TradeHistoryTradeIds.Contains(x.TradeId)).ToList();
			if (trades.Count != 0)
			{
				// Add each new trade
				using (var transaction = DB.BeginTransaction())
				{
					foreach (var his in trades)
					{
						UpsertTradeCompleted(his, transaction);

						// Record in the cache of known trades
						TradeHistoryTradeIds.Add(his.TradeId);
					}

					transaction.Commit();
				}

				//// Notify that the trade history has changed
				//todo - Caller's responsibility => Model.RaiseTradeHistoryChanged();
			}
		}

		/// <summary>Update or insert an 'OrderCompleted'</summary>
		private void UpsertTradeCompleted(TradeCompleted trade, IDbTransaction transaction = null)
		{
			var rec = new TradeRecord(trade);
			DB.Execute(
				$"insert or replace into {DBHistoryTableName} (\n" +
				$"  [{nameof(TradeRecord.TradeId)}],\n" +
				$"  [{nameof(TradeRecord.OrderId)}],\n" +
				$"  [{nameof(TradeRecord.Created)}],\n" +
				$"  [{nameof(TradeRecord.Pair)}],\n" +
				$"  [{nameof(TradeRecord.TradeType)}],\n" +
				$"  [{nameof(TradeRecord.PriceQ2B)}],\n" +
				$"  [{nameof(TradeRecord.AmountBase)}],\n" +
				$"  [{nameof(TradeRecord.CommissionQuote)}]\n" +
				$") values (\n" +
				$"  @trade_id, @order_id, @timestamp, @pair, @trade_type, @price, @amount, @commission\n" +
				$")",
				new
				{
					trade_id = rec.TradeId,
					order_id = rec.OrderId,
					timestamp = rec.Created,
					pair = rec.Pair,
					trade_type = rec.TradeType,
					price = rec.PriceQ2B,
					amount = rec.AmountBase,
					commission = rec.CommissionQuote,
				},
				transaction); 
		}

		/// <summary>The time range that the position history covers (in ticks)</summary>
		public Range HistoryInterval { get; protected set; }
		protected DateTimeOffset m_history_last; // Worker thread context only

		/// <summary>The time range that the transfer history covers (in ticks)</summary>
		public Range TransfersInterval { get; protected set; }
		protected DateTimeOffset m_transfers_last; // Worker thread context only

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		protected void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary></summary>
		public override string ToString()
		{
			return GetType().Name;
		}

		/// <summary></summary>
		public int CompareTo(Exchange other)
		{
			return Name.CompareTo(other.Name);
		}
		public int CompareTo(object obj)
		{
			return CompareTo((Exchange)obj);
		}

		/// <summary>Tuple of 'Pair' and 'TimeFrame'</summary>
		public struct PairAndTF
		{
			public PairAndTF(TradePair pair, ETimeFrame tf)
			{
				Pair = pair;
				TimeFrame = tf;
			}
			public TradePair Pair;
			public ETimeFrame TimeFrame;
		}

		private static uint[] Colours = new[]{ 0xFF000080, 0xFF008000, 0xFF800000, 0xFF808000, 0xFF800080, 0xFF008080, 0xFF80FF80 };
		private static int m_colour_index;
	}

}



///// <summary>App logic</summary>
//public Model Model
//{
//	[DebuggerStepThrough] get { return m_model; }
//	private set
//	{
//		if (m_model == value) return;
//		if (m_model != null)
//		{
//			if (UpdateThreadActive) throw new Exception("Should not be nulling 'Model' when the thread is running");
//			m_model.BackTestingChanging -= HandleBackTestingChanged;
//			m_model.SimReset            -= HandleSimReset;
//			m_model.Funds.ListChanging  -= HandleFundsListChanging;
//		}
//		m_model = value;
//		if (m_model != null)
//		{
//			m_model.Funds.ListChanging  += HandleFundsListChanging;
//			m_model.SimReset            += HandleSimReset;
//			m_model.BackTestingChanging += HandleBackTestingChanged;
//		}

//		// Handlers
//		void HandleBackTestingChanged(object sender, PrePostEventArgs e)
//		{
//			// If back testing is about to be enabled...
//			if (!Model.BackTesting && e.Before)
//			{
//				// Turn off the update thread
//				UpdateThreadActive = false;
//			}

//			// If back testing has just been enabled...
//			if (Model.BackTesting && e.After)
//			{
//				// Reinitialise the history DB
//				InitTradeHistoryDB();
//			}

//			// If back testing is about to be disabled...
//			if (Model.BackTesting && e.Before)
//			{
//			}

//			// If back testing has just been disabled...
//			if (!Model.BackTesting && e.After)
//			{
//				// Reinitialise the history DB
//				InitTradeHistoryDB();

//				// Turn on the update thread
//				UpdateThreadActive = Enabled;
//			}

//			RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Status)));
//		}
//		void HandleSimReset(object sender, SimResetEventArgs e)
//		{
//			// Reset the trade history DB when the sim resets
//			InitTradeHistoryDB();
//		}
//		void HandleFundsListChanging(object sender, ListChgEventArgs<Fund> e)
//		{
//			// When the funds container changes, update all balances
//			switch (e.ChangeType)
//			{
//			case ListChg.Reset:
//			case ListChg.ItemAdded:
//			case ListChg.ItemRemoved:
//				foreach (var bal in Balance.Values)
//					bal.UpdateBalancePartitions();
//				break;
//			}
//		}
//	}
//}
//private Model m_model;