using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
	public abstract class Exchange :IDisposable, INotifyPropertyChanged, IComparable<Exchange>, IComparable
	{
		// Notes:
		//  - Exchanges are 'below' the Model, i.e. they are unaware of the Model class.
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
				Transfers = new TransfersCollection(this);
				Shutdown = CancellationTokenSource.CreateLinkedTokenSource(shutdown);
				Dispatcher = Dispatcher.CurrentDispatcher;
				m_update_thread_step = new AutoResetEvent(false);

				// Initialise the trade history table
				InitTradeHistoryTables();

				// Run after the exchange is fully constructed
				Misc.RunOnMainThread(async () =>
				{
					// Perform async initialisation
					try { await ExchangeApi.InitAsync(); }
					catch (OperationCanceledException) { return; };

					// Start the exchange if enabled in the settings
					if (!ExchSettings.Active)
						return;

					// Initialise currencies and balances
					if (!(this is CrossExchange))
					{
						// Get the set of coins from the settings
						var coins = SettingsData.Settings.Coins.ToHashSet(x => x.Symbol);
						await UpdatePairs(coins);
						await UpdateBalances(coins);
					}

					// Start the Exchange's main loop after the pairs and balances have been integrated.
					Model.DataUpdates.Add(() =>
					{
						// Attach observers to Pairs changed
						Pairs.CollectionChanged += delegate { UpdateValuationPaths(); };
						Pairs.CollectionChanged += delegate { SignalPopulateTradeHistory(); };
						PopulateTradeHistory();
						OrdersUpdateRequired = true;
						MarketDataUpdateRequired = true;
						TransfersUpdateRequired = true;
						UpdateThreadActive = true;
					});
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
				var status = EExchangeStatus.Offline;
				if (UpdateThreadActive) status |= EExchangeStatus.Connected;
				if (Model.BackTesting) status |= EExchangeStatus.Simulated;
				if (LastRequestFailed) status |= EExchangeStatus.Error;
				if (ExchSettings.PublicAPIOnly) status |= EExchangeStatus.PublicAPIOnly;
				if (!Enabled) status |= EExchangeStatus.Stopped;
				return status;
			}
		}

		/// <summary>Settings for this exchange</summary>
		public IExchangeSettings ExchSettings
		{
			get => m_exch_settings;
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

		/// <summary>For invoke calls on the main thread</summary>
		protected Dispatcher Dispatcher { get; }

		/// <summary>True if this exchange is to be used</summary>
		public bool Enabled
		{
			get { return ExchSettings.Active; }
			set
			{
				if (Enabled == value) return;
				ExchSettings.Active = value;
				UpdateThreadActive = value;

				// Notify status changed
				NotifyPropertyChanged(nameof(Status));
			}
		}

		/// <summary>Get/Set the simulation. A non-null simulation implies 'BackTesting'</summary>
		public SimExchange Sim
		{
			get { return m_sim; }
			set
			{
				if (m_sim == value) return;
				m_sim = value;

				// Reconnect to the appropriate trade history database
				InitTradeHistoryTables();
				PopulateTradeHistory();

				// Notify status changed
				NotifyPropertyChanged(nameof(Status));
			}
		}
		private SimExchange m_sim;

		/// <summary>Enable/Disable the exchange update thread</summary>
		public bool UpdateThreadActive
		{
			get => m_update_thread != null;
			set
			{
				if (UpdateThreadActive == value) return;
				Debug.Assert(Misc.AssertMainThread());

				// Don't enable inactive exchanges
				if (value && !Enabled)
					throw new Exception("Don't enable inactive exchanges");

				// Don't enable the update thread while back testing
				if (value && Model.BackTesting)
					throw new Exception("Don't enable when back testing");

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
					OrdersUpdateRequired = true;

					// Set the request rate limit
					ExchangeApi.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;

					// Start the thread
					m_update_thread_exit = false;
					Shutdown = CancellationTokenSource.CreateLinkedTokenSource(m_main_shutdown);
					m_update_thread.Start();
				}

				// Notify status changed
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

								// Get the set of coins from the settings
								var coins = SettingsData.Settings.Coins.ToHashSet(x => x.Symbol);

								// Update pairs
								if (PairsUpdateRequired)
								{
									PairsUpdateRequired = false;
									await UpdatePairs(coins);
								}

								// Update the balances
								const double BalanceUpdatePeriodMS = 1000;
								if (BalanceUpdateRequired || (Model.UtcNow - Balance.LastUpdated).TotalMilliseconds > BalanceUpdatePeriodMS)
								{
									BalanceUpdateRequired = false;
									if (!ExchSettings.PublicAPIOnly)
										await UpdateBalances(coins);
								}

								// Update market data
								const int MarketDataUpdatePeriodMS = 100;
								if (MarketDataUpdateRequired || (Model.UtcNow - Pairs.LastUpdated).TotalMilliseconds > MarketDataUpdatePeriodMS)
								{
									MarketDataUpdateRequired = false;
									await UpdateMarketData();
								}

								// Update orders/history
								const int OrdersUpdatePeriodMS = 1000;
								if (OrdersUpdateRequired || (Model.UtcNow - Orders.LastUpdated).TotalMilliseconds > OrdersUpdatePeriodMS)
								{
									OrdersUpdateRequired = false;
									if (!ExchSettings.PublicAPIOnly)
										await UpdateOrdersAndHistory(coins);
								}

								// Update funds transfers
								const int TransfersUpdatePeriodMS = 60000;
								if (TransfersUpdateRequired || (Model.UtcNow - Transfers.LastUpdated).TotalMilliseconds > TransfersUpdatePeriodMS)
								{
									TransfersUpdateRequired = false;
									if (!ExchSettings.PublicAPIOnly)
										await UpdateTransfers(coins);
								}
							}
							catch (OperationCanceledException) { break; }
						}
					}
					catch (Exception ex)
					{
						Model.Log.Write(ELogLevel.Error, ex, $"Exchange {Name} update thread exit");
						await Misc.RunOnMainThread(() => UpdateThreadActive = false);
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
		public bool OrdersUpdateRequired
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

		/// <summary>The percentage fee charged when performing exchanges</summary>
		public double Fee => ExchSettings.TransactionFee;

		/// <summary>The value of all coins held on this exchange</summary>
		public double NettWorth
		{
			get
			{
				if (this is CrossExchange) return 0;
				return Balance.Values.Sum(x => (double)x.Coin.ValueOf(x.NettTotal));
			}
		}

		/// <summary>The number of coins available on this exchange</summary>
		public int CoinsAvailable => Coins.Count;

		/// <summary>The number of trading pairs available</summary>
		public int PairsAvailable => Pairs.Count;

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

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		public OrdersCompletedCollection History { get; }

		/// <summary>Funds transfers on this exchange</summary>
		public TransfersCollection Transfers { get; }

		/// <summary>Update the collections of coins and pairs</summary>
		public async Task UpdatePairs(HashSet<string> coins) // Worker thread context
		{
			// Allow update pairs in back testing mode as well

			try
			{
				Debug.Assert(Misc.AssertBackgroundThread());

				await UpdatePairsInternal(coins);
				LastRequestFailed = false;
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdatePairs), ex);
			}
		}
		protected virtual Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// This method should wait for server responses in worker threads,
			// collect data in local buffers, then use 'RunOnMainThread' to copy
			// data to the Exchange's main collections.
			return Task.CompletedTask;
		}

		/// <summary>Update the account balances</summary>
		protected async Task UpdateBalances(HashSet<string> coins)  // Worker thread context
		{
			try
			{
				Debug.Assert(Misc.AssertBackgroundThread());

				await UpdateBalancesInternal(coins);
				LastRequestFailed = false;

			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateBalances), ex);
			}
		}
		protected virtual Task UpdateBalancesInternal(HashSet<string> coins)  // Worker thread context
		{
			Balance.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Update all open orders and completed trades</summary>
		protected async Task UpdateOrdersAndHistory(HashSet<string> coins) // Worker thread context
		{
			try
			{
				Debug.Assert(Misc.AssertBackgroundThread());

				// Do orders and history together so there's no chance of a
				// position being filled without it appearing in the history.
				await UpdateOrdersAndHistoryInternal(coins);
				LastRequestFailed = false;
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateOrdersAndHistory), ex);
			}
		}
		protected virtual Task UpdateOrdersAndHistoryInternal(HashSet<string> coins) // Worker thread context
		{
			Orders.LastUpdated = Model.UtcNow;
			History.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async Task UpdateMarketData() // Worker thread context
		{
			try
			{
				Debug.Assert(Misc.AssertBackgroundThread());

				await UpdateDataInternal();
				LastRequestFailed = false;
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateMarketData), ex);
			}
		}
		protected virtual Task UpdateDataInternal() // Worker thread context
		{
			Pairs.LastUpdated = Model.UtcNow;
			return Task.CompletedTask;
		}

		/// <summary>Update all deposits and withdrawals made on this exchange</summary>
		protected async Task UpdateTransfers(HashSet<string> coins) // Worker thread context
		{
			try
			{
				Debug.Assert(Misc.AssertBackgroundThread());

				await UpdateTransfersInternal(coins);
				LastRequestFailed = false;
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdatePairs), ex);
			}
		}
		protected virtual Task UpdateTransfersInternal(HashSet<string> coins) // Worker thread context
		{
			return Task.CompletedTask;
		}

		/// <summary>Return the candle data for a given pair, over a given time range</summary>
		public async Task<List<Candle>> CandleData(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			try
			{
				if (Model.BackTesting)
					throw new Exception("Shouldn't require candle data while back testing");

				using (Scope.Create(() => CandleDataUpdateInProgress = true, () => CandleDataUpdateInProgress = false))
					return await CandleDataInternal(pair, timeframe, time_beg, time_end, cancel);
			}
			catch (Exception ex)
			{
				HandleException(nameof(CandleData), ex, $"PriceData {pair.NameWithExchange} get chart data failed.");
				return null;
			}
		}
		protected virtual Task<List<Candle>> CandleDataInternal(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			return Task.FromResult(new List<Candle>());
		}
		public bool CandleDataUpdateInProgress { get; private set; }

		/// <summary>Place an order on the exchange to buy/sell 'amount' (currency depends on 'tt')</summary>
		public async Task<OrderResult> CreateOrder(Fund fund, TradePair pair, ETradeType tt, EOrderType ot, Unit<double> amount_in, Unit<double> amount_out, CancellationToken cancel, string creator_name)
		{
			using (Scope.Create(() => ++m_in_create_order, () => --m_in_create_order))
			{
				// Sanity checks
				if (m_in_create_order != 1)
					throw new Exception("Re-entrant call to CreateOrder");
				if (!Model.AllowTrades && !Model.BackTesting)
					throw new Exception($"Cannot place orders when Trading is disabled");
				if (pair.Exchange != this)
					throw new Exception($"Pair {pair} is not provided by this exchange");
				if (amount_in <= 0.0._(tt.CoinIn(pair)))
					throw new Exception($"Invalid trade 'in' amount: {amount_in}");
				if (amount_out <= 0.0._(tt.CoinOut(pair)))
					throw new Exception($"Invalid trade 'out' amount: {amount_out}");
				if (amount_in > fund[tt.CoinIn(pair)].Available)
					throw new Exception($"Order amount ({amount_in}) is greater than the current available balance: {fund[tt.CoinIn(pair)].Available}");

				// Get the creation time of the order
				var now = Model.UtcNow;

				// Put a hold on the balance we're about to trade to prevent a race condition
				// with other trades being placed while we wait for this one to go through.
				// Don't need to account for the fee in the hold because the fee is taken from
				// the received amount (usually).
				var bal = fund[tt.CoinIn(pair)];
				var hold_amount = amount_in;
				var hold = bal.Holds.Create(hold_amount, local: true);
//				using hold;

				// Make the trade
				// This can have the following results:
				// 1) the entire trade is added to the order book for the pair -> a single order number is returned.
				// 2) the entire trade can be met by existing orders in the order book -> a collection of trade IDs is returned.
				// 3) some of the trade can be met by existing orders -> a single order number and a collection of trade IDs are returned.
				var result =
					Sim != null ? Sim.CreateOrderInternal(pair, tt, ot, amount_in, amount_out) :
					Model.AllowTrades ? await CreateOrderInternal(pair, tt, ot, amount_in, amount_out, cancel) :
					throw new Exception("Cannot create order, trading not enabled");

				// Log the event
				var price_q2b = ot == EOrderType.Market ? pair.SpotPrice[tt].Value : tt.PriceQ2B(amount_out / amount_in);
				Model.Log.Write(ELogLevel.Info, $"{Name}: (id={result.OrderId}) {amount_in.ToString(8, true)} → {amount_out.ToString(8, true)} @ {price_q2b.ToString(8, true)}");

				// Save the extra details about the live order.
				UpsertLiveOrder(result.OrderId, fund, creator_name);

				// The order may have been completed or partially filled. Add the filled orders to the trade history.
				foreach (var fill in result.Trades)
				{
					var order_completed = History.GetOrAdd(result.OrderId, x => new OrderCompleted(x, fund, pair, tt));
					order_completed.Trades[fill.TradeId] = new TradeCompleted(order_completed, fill.TradeId, fill.AmountIn, fill.AmountOut, fill.Commission, fill.CommissionCoin, now, now);
				}

				// Add the order to the Orders collection so that there is no race condition between
				// placing an order and checking 'Orders' for the existence of the order just placed.
				if (!result.Filled)
				{
					// Add a 'Position' to the collection, this will be overwritten on the next update.
					var filled_in = result.Trades.Sum(x => x.AmountIn)._(tt.CoinIn(pair));
					var order = new Order(result.OrderId, fund, pair, ot, tt, amount_in, amount_out, amount_in - filled_in, now, now);
					Orders.AddOrUpdate(order);

					// Update the hold with the order id.
					// Holds need to exist for the life of the order so we know how much
					// of the exchange held amount to attributed to each fund.
					hold.OrderId = result.OrderId;
				}
				else
				{
					// If the order was immediately completely filled, apply the changes to the associated fund.
					// There was never any amount held on the exchange in this case.
					bal.Holds.Remove(hold);
					ApplyCompletedOrderToFund(History[result.OrderId]);
				}

				// Remove entries from the order book that this order should have filled.
				// This will be overwritten with the next update.
				var amount_base = tt.AmountBase(price_q2b, amount_in, amount_out);
				pair.MarketDepth.Consume(pair, tt, ot, price_q2b, amount_base, out var _);

				// Trigger updates
				MarketDataUpdateRequired = true;
				OrdersUpdateRequired = true;
				BalanceUpdateRequired = true;
				return result;
			}
		}
		protected abstract Task<OrderResult> CreateOrderInternal(TradePair pair, ETradeType tt, EOrderType ot, Unit<double> amount_in, Unit<double> amount_out, CancellationToken cancel);
		private int m_in_create_order;

		/// <summary>Cancel an existing order</summary>
		public async Task<bool> CancelOrder(TradePair pair, long order_id, CancellationToken cancel)
		{
			var result =
				Sim != null ? Sim.CancelOrderInternal(pair, order_id) :
				Model.AllowTrades ? await CancelOrderInternal(pair, order_id, cancel) :
				true;

			// Remove the position from the orders collection so that there is no
			// race condition while waiting for the orders to update from the server.
			if (Orders.TryGetValue(order_id, out var order))
			{
				Orders.Remove(order_id);

				var bal = Balance[order.CoinIn];
				var fund = bal[order.Fund];

				// Release any holds associated with 'order_id'.
				if (fund.Holds.TryGet(order_id, out var hold))
				{
					// Remove the hold
					fund.Holds.Remove(order_id: order_id);

					// If the hold is not local only, adjust the held amount so that a new
					// trade can be placed without having to wait for the next balance update.
					if (!hold.Local)
						bal.ExchangeUpdate(bal.ExchTotal, bal.ExchHeld - order.AmountIn, bal.LastUpdated);
				}
			}

			// Trigger a positions and balances update
			OrdersUpdateRequired = true;
			BalanceUpdateRequired = true;
			return result;
		}
		protected abstract Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel);

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
				LastRequestFailed = true;
			}

			// Log all other error types
			Model.Log.Write(ELogLevel.Error, ex, $"{GetType().Name} {method_name} failed. {msg ?? string.Empty}");
		}

		/// <summary>True when an error occurs with an API request</summary>
		private bool LastRequestFailed
		{
			get { return m_last_request_failed; }
			set
			{
				if (m_last_request_failed == value) return;
				m_last_request_failed = value;
				NotifyPropertyChanged(nameof(Status));
			}
		}
		private bool m_last_request_failed;

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
		private string DBFilepath => Model.BackTesting
			? Misc.ResolveUserPath("Sim", "History", $"TradeHistory-{Name}.db")
			: Misc.ResolveUserPath("History", $"TradeHistory-{Name}.db");

		/// <summary>Initialise the trade history table</summary>
		private void InitTradeHistoryTables()
		{
			// Release the connection to the DB
			DB = null;

			// Ensure the directory that contains the trade history DB's exists
			Path_.CreateDirs(Path_.Directory(DBFilepath));

			// In back testing mode, delete the history first
			if (Model.BackTesting)
				Path_.DelFile(DBFilepath, fail_if_missing: false);

			// Connect
			DB = new SQLiteConnection($"Data Source={DBFilepath};Version=3;journal mode=Memory;synchronous=Off;foreign_keys=On");

			// Ensure the LiveOrders table exists
			DB.Execute(
				$"create table if not exists {Table.LiveOrders} (\n" +
				$"  [{nameof(OrderRecord.OrderId)}] integer unique primary key,\n" +
				$"  [{nameof(OrderRecord.FundId)}] text,\n" +
				$"  [{nameof(OrderRecord.CreatorName)}] text\n" +
				$")");

			// Ensure the OrderComplete table exists
			DB.Execute(
				$"create table if not exists {Table.OrderComplete} (\n" +
				$"  [{nameof(OrderCompletedRecord.OrderId)}] integer unique primary key,\n" +
				$"  [{nameof(OrderCompletedRecord.FundId)}] text,\n" +
				$"  [{nameof(OrderCompletedRecord.TradeType)}] text,\n" +
				$"  [{nameof(OrderCompletedRecord.Pair)}] text\n" +
				$")");

			// Ensure the TradeComplete table exists
			DB.Execute(
				$"create table if not exists {Table.TradeComplete} (\n" +
				$"  [{nameof(TradeCompletedRecord.TradeId)}] integer unique primary key,\n" +
				$"  [{nameof(TradeCompletedRecord.OrderId)}] integer,\n" +
				$"  [{nameof(TradeCompletedRecord.Created)}] integer,\n" +
				$"  [{nameof(TradeCompletedRecord.Updated)}] integer,\n" +
				$"  [{nameof(TradeCompletedRecord.AmountIn)}] real,\n" +
				$"  [{nameof(TradeCompletedRecord.AmountOut)}] real,\n" +
				$"  [{nameof(TradeCompletedRecord.Commission)}] real,\n" +
				$"  [{nameof(TradeCompletedRecord.CommissionCoin)}] text,\n" +
				$"\n" +
				$"  foreign key ({nameof(TradeCompletedRecord.OrderId)}) references {Table.OrderComplete}([{nameof(OrderCompletedRecord.OrderId)}])\n" +
				$"     on update cascade\n" +
				$"     on delete cascade\n" +
				$")");

			// Reset the interval of available history.
			HistoryInterval = new Range(
				DateTimeOffset_.UnixEpoch.Ticks,
				DateTimeOffset_.UnixEpoch.Ticks);

			// Reset the interval of available funds transfer history
			TransfersInterval = new Range(
				DateTimeOffset_.UnixEpoch.Ticks,
				DateTimeOffset_.UnixEpoch.Ticks);

			// Note: the 'History' Collection cannot be populated
			// here because at this point the Trade pairs may not be known.
		}

		/// <summary>Ensure the valuation paths for the current coins are up to date</summary>
		public void UpdateValuationPaths()
		{
			foreach (var coin in Coins.Values)
				coin.UpdateValuationPaths();
		}

		/// <summary>Synchronise the Orders collection with 'orders'</summary>
		public void SynchroniseOrders(IList<Order> live_orders, DateTimeOffset timestamp)
		{
			// Notes:
			//  - This method should be called after the history has been updated so
			//    that it can detect the difference between a filled and cancelled order.

			var live_order_ids = live_orders.ToHashSet(x => x.OrderId);

			// Remove all orders that are no longer live
			foreach (var order in Orders.Values.Where(x => !live_order_ids.Contains(x.OrderId)).ToArray())
			{
				// 'order' is newer than 'timestamp', so it can stay
				if (order.Created >= timestamp)
					continue;

				// The order is no longer on the exchange, so remove it from 'Orders'
				Orders.Remove(order.OrderId);

				// Remove any holds associated with this order
				order.Fund[order.CoinIn].Holds.Remove(order_id: order.OrderId);

				// Remove the live order details
				DeleteLiveOrderDetails(order.OrderId);

				// If the order is no longer in the live orders list and is now in the history,
				// then it has been filled. Apply the trade amounts to the associated fund.
				// Note: this only happens when the order is fully filled because if it wasn't
				// filled it'd still be in the live orders list.
				var was_filled = History.TryGetValue(order.OrderId, out var his);
				if (was_filled)
					ApplyCompletedOrderToFund(his);
			}

			// Add/Update new orders in the orders collection
			foreach (var order in live_orders)
			{
				Orders.AddOrUpdate(order);

				// Ensure a fund hold exists for each live order
				var holds = order.Fund[order.CoinIn].Holds;
				if (holds.TryGet(order.OrderId, out var hold))
				{
					hold.Amount = order.AmountIn;
					hold.Local = false;
				}
				else
				{
					holds.Create(order.AmountIn, order_id: order.OrderId, local: false);
				}
			}
		}

		/// <summary>Add a new or modified OrderCompleted to the trade history DB</summary>
		public void AddToTradeHistory(OrderCompleted order)
		{
			// Notes:
			// - This doesn't happen when 'Model.History' is accessed/added to by derived exchanges
			//   because adding 'OrderCompleted' instances to a 'OrderCompleted' would not raise notifications.
			//   Also, 'OrderCompleted' instances are added as empty instances and then populated.
			// - Adding an 'OrderCompleted' does not imply the associated order is filled. Orders can be
			//   partially filled. AddToTradeHistory is called for each partial fill of an order.

			if (order.Exchange != this)
				throw new Exception("This position fill did not occur on this exchange");

			// Update the completed orders
			using (var transaction = DB.BeginTransaction())
			{
				// Get the trades not already in the DB
				UpsertOrderCompleted(order, transaction);
				foreach (var trade in order.Trades.Values)
					UpsertTradeCompleted(trade, transaction);

				transaction.Commit();
			}

			// Update the history range
			HistoryInterval = new Range(
				Math.Min(HistoryInterval.Beg, order.Created.Ticks),
				Math.Max(HistoryInterval.End, order.Created.Ticks));
		}

		/// <summary>Update or insert a live order</summary>
		private void UpsertLiveOrder(long order_id, Fund fund, string creator_name, IDbTransaction transaction = null)
		{
			var rec = new OrderRecord(order_id, fund.Id, creator_name);
			DB.Execute(
				$"insert or replace into {Table.LiveOrders} (\n" +
				$"  [{nameof(OrderRecord.OrderId)}],\n" +
				$"  [{nameof(OrderRecord.FundId)}],\n" +
				$"  [{nameof(OrderRecord.CreatorName)}]\n" +
				$") values (\n" +
				$"  @order_id, @fund_id, @creator_name\n" +
				$")",
				new
				{
					order_id = rec.OrderId,
					fund_id = rec.FundId,
					creator_name = rec.CreatorName,
				},
				transaction);
		}

		/// <summary>Remove the live order details from the DB associated with 'order_id'</summary>
		private void DeleteLiveOrderDetails(long order_id, IDbTransaction transaction = null)
		{
			DB.Execute(
				$"delete from {Table.LiveOrders}\n" +
				$"where [{nameof(OrderRecord.OrderId)}] = @order_id",
				new
				{
					order_id
				},
				transaction);
		}

		/// <summary>Update or insert an 'OrderCompleted'</summary>
		private void UpsertOrderCompleted(OrderCompleted order, IDbTransaction transaction = null)
		{
			var rec = new OrderCompletedRecord(order);
			DB.Execute(
				$"insert or replace into {Table.OrderComplete} (\n" +
				$"  [{nameof(OrderCompletedRecord.OrderId)}],\n" +
				$"  [{nameof(OrderCompletedRecord.FundId)}],\n" +
				$"  [{nameof(OrderCompletedRecord.TradeType)}],\n" +
				$"  [{nameof(OrderCompletedRecord.Pair)}]\n" +
				$") values (\n" +
				$"  @order_id, @fund_id, @trade_type, @pair\n" +
				$")",
				new
				{
					order_id = rec.OrderId,
					fund_id = rec.FundId,
					trade_type = rec.TradeType,
					pair = rec.Pair,
				},
				transaction);
		}

		/// <summary>Update or insert a 'TradeCompleted'</summary>
		private void UpsertTradeCompleted(TradeCompleted trade, IDbTransaction transaction = null)
		{
			var rec = new TradeCompletedRecord(trade);
			DB.Execute(
				$"insert or replace into {Table.TradeComplete} (\n" +
				$"  [{nameof(TradeCompletedRecord.TradeId)}],\n" +
				$"  [{nameof(TradeCompletedRecord.OrderId)}],\n" +
				$"  [{nameof(TradeCompletedRecord.Created)}],\n" +
				$"  [{nameof(TradeCompletedRecord.Updated)}],\n" +
				$"  [{nameof(TradeCompletedRecord.AmountIn)}],\n" +
				$"  [{nameof(TradeCompletedRecord.AmountOut)}],\n" +
				$"  [{nameof(TradeCompletedRecord.Commission)}],\n" +
				$"  [{nameof(TradeCompletedRecord.CommissionCoin)}]\n" +
				$") values (\n" +
				$"  @trade_id, @order_id, @created, @updated, @amount_in, @amount_out, @commission, @commission_coin\n" +
				$")",
				new
				{
					trade_id = rec.TradeId,
					order_id = rec.OrderId,
					created = rec.Created,
					updated = rec.Updated,
					amount_in = rec.AmountIn,
					amount_out = rec.AmountOut,
					commission = rec.Commission,
					commission_coin = rec.CommissionCoin,
				},
				transaction);
		}

		/// <summary>Return the fund id associated with an order id (from the order details)</summary>
		public Fund OrderIdToFund(long order_id)
		{
			var fund_id = DB.QuerySingleOrDefault<string>(
				$"select [{nameof(OrderRecord.FundId)}] from {Table.LiveOrders}\n" +
				$"where [{nameof(OrderRecord.OrderId)}] = @order_id",
				new { order_id });

			return new Fund(fund_id ?? Fund.Main);
		}

		/// <summary>Adjust the balances of a fund based on a completed order</summary>
		private void ApplyCompletedOrderToFund(OrderCompleted his)
		{
			foreach (var trade in his.Trades.Values)
			{
				{// Debt from CoinIn
					var bal = Balance[trade.CoinIn];
					bal.ChangeFundBalance(his.Fund, -trade.AmountIn);
				}
				{// Credit to CoinOut
					var bal = Balance[trade.CoinOut];
					bal.ChangeFundBalance(his.Fund, +trade.AmountOut);
				}
				if (trade.CommissionCoin != null) // Debt the commission
				{
					var bal = Balance[trade.CommissionCoin];
					bal.ChangeFundBalance(his.Fund, -trade.Commission);
				}
			}
		}

		/// <summary>Update the trade history from the DB</summary>
		private void PopulateTradeHistory()
		{
			Debug.Assert(Misc.AssertMainThread());
			m_populate_history_signalled = false;
			if (DB == null) return;

			var history = new Dictionary<long, OrderCompleted>();
			var history_first = DateTimeOffset.MaxValue;
			var history_last = Misc.CryptoCurrencyEpoch;
			var transfers_last = Misc.CryptoCurrencyEpoch;
			var history_last_id = 0L;

			// Get the completed orders
			var orders = DB.Query<OrderCompletedRecord>($"select * from {Table.OrderComplete}");
			foreach (var order in orders)
			{
				var pair = Pairs[order.Pair];
				if (pair != null)
					history.Add(order.OrderId, new OrderCompleted(order.OrderId, order.Fund, pair, Enum<ETradeType>.Parse(order.TradeType)));
			}

			// Get the trades that make up the completed orders
			var trades = DB.Query<TradeCompletedRecord>($"select * from {Table.TradeComplete}");
			foreach (var trade in trades)
			{
				// Get the completed order that 'trade' belongs to
				if (history.TryGetValue(trade.OrderId, out var order_completed))
				{
					var pair = order_completed.Pair;
					var amount_in = trade.AmountIn._(order_completed.CoinIn);
					var amount_out = trade.AmountOut._(order_completed.CoinOut);
					var commission_coin = Coins[trade.CommissionCoin];
					var commission = trade.Commission._(commission_coin);
					var created = new DateTimeOffset(trade.Created, TimeSpan.Zero);
					var updated = new DateTimeOffset(trade.Updated, TimeSpan.Zero);
					order_completed.Trades.Add(new TradeCompleted(order_completed, trade.TradeId, amount_in, amount_out, commission, commission_coin, created, updated));

					// Record the range of completed orders
					if (created < history_first)
					{
						history_first = created;
					}
					if (created > history_last)
					{
						history_last = created;
						history_last_id = trade.TradeId;
					}
				}
			}

			// Assign to the collection
			History.Clear();
			History.AddRange(history);

			// Set the time to get history from
			m_history_last = history_last;
			m_transfers_last = transfers_last;
			m_history_last_id = history_last_id;

			// Set the range to span the available history
			if (history_first <= history_last)
				HistoryInterval = new Range(history_first.Ticks, history_last.Ticks);
		}
		private void SignalPopulateTradeHistory()
		{
			if (m_populate_history_signalled) return;
			m_populate_history_signalled = true;
			Misc.RunOnMainThread(PopulateTradeHistory);
		}
		private bool m_populate_history_signalled;

		/// <summary>The time range that the completed orders history covers (in ticks)</summary>
		public Range HistoryInterval { get; protected set; }
		protected DateTimeOffset m_history_last; // Worker thread context only
		protected long m_history_last_id; // TradeId. Worker thread context only

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

		/// <summary>Trade history table names</summary>
		private static class Table
		{
			public const string LiveOrders = "LiveOrders";
			public const string OrderComplete = "OrderComplete";
			public const string TradeComplete = "TradeComplete";
		}

		/// <summary>Colours for exchanges</summary>
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