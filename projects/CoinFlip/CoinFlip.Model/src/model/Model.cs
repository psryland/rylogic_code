using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public partial class Model :IDisposable
	{
		public Model(Form main_ui, Settings settings, User user)
		{
			UI = main_ui;
			Settings = settings;
			User = user;
			m_dispatcher = Dispatcher.CurrentDispatcher;
			Shutdown = new CancellationTokenSource();
			MarketDataLock = new Mutex();

			// Start the log
			Log = new Logger(Application.ProductName, new LogToFile(Misc.ResolveUserPath("Logs\\log.txt"), append:false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			// Start the win log
			WinLog = new Logger(Application.ProductName, new LogToFile(Misc.ResolveUserPath("Logs\\win_log.txt"), append:true));

			// Create collections
			Exchanges     = new BindingSource<Exchange>     { DataSource = new BindingListEx<Exchange>(), PerItem = true };
			Pairs         = new BindingSource<TradePair>    { DataSource = new BindingListEx<TradePair>() };
			Bots          = new BindingSource<IBot>         { DataSource = new BindingListEx<IBot>(), PerItem = true };
			Balances      = new BindingSource<Balance>      { DataSource = null, AllowNoCurrent = true };
			Positions     = new BindingSource<Position>     { DataSource = null, AllowNoCurrent = true };
			History       = new BindingSource<PositionFill> { DataSource = null, AllowNoCurrent = true };
			MarketUpdates = new BlockingCollection<Action>();
			Coins         = new CoinDataTable(this);
			PriceData     = new PriceDataMap(this);

			// Add exchanges
			string key, secret;
			if (LoadAPIKeys(user, nameof(Poloniex ), out key, out secret)) Exchanges.Add(new Poloniex(this, key, secret));
			if (LoadAPIKeys(user, nameof(Bittrex  ), out key, out secret)) Exchanges.Add(new Bittrex(this, key, secret));
			if (LoadAPIKeys(user, nameof(Cryptopia), out key, out secret)) Exchanges.Add(new Cryptopia(this, key, secret));
			Exchanges.Add(CrossExchange = new CrossExchange(this));
			//Exchanges.Add(new TestExchange(this));

			// Update the available pairs
			TriggerPairsUpdate();

			// Create Bots listed in the settings
			RunOnGuiThread(() => CreateBotsFromSettings());
 
			// Run the main loop
			MainLoopRunning = true;
		}
		public virtual void Dispose()
		{
			Shutdown = null;
			MainLoopRunning = false;
			Simulation = null;
			Bots = null;
			Exchanges = null;
			Positions = null;
			History = null;
			Balances = null;
			Pairs = null;
			PriceData = null;
			Coins = null;
			MarketDataLock = null;
			WinLog = null;
			Log = null;
		}

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationTokenSource Shutdown
		{
			get { return m_shutdown; }
			private set
			{
				if (m_shutdown == value) return;
				if (m_shutdown != null)
				{
					if (!m_shutdown.IsCancellationRequested)
						throw new Exception();

					Util.Dispose(ref m_shutdown);
				}
				m_shutdown = value;
			}
		}
		private CancellationTokenSource m_shutdown;

		/// <summary>A main thread timer that simply updates the 'UtcNow' time to the current time (or back-testing clock)</summary>
		public bool MainLoopRunning
		{
			get { return m_clock_poller != null; }
			set
			{
				if (MainLoopRunning == value) return;
				Debug.Assert(AssertMainThread());

				if (MainLoopRunning)
				{
					m_clock_poller.Stop();
					Log.Write(ELogLevel.Debug, "MainLoop stopped");
				}
				m_clock_poller = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (MainLoopRunning)
				{
					Log.Write(ELogLevel.Debug, "MainLoop started");
					m_clock_poller.Start();
					HandleTick();
				}

				// Handlers
				void HandleTick(object sender = null, EventArgs e = null)
				{
					try
					{
						// Process any pending market data updates
						IntegrateMarketUpdates();

						// Simulate fake orders being filled
						if (!AllowTrades && !BackTesting)
							SimulateFakeOrders();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) return;
						else Log.Write(ELogLevel.Error, ex, "Error during main loop.");
					}
				}
			}
		}
		private DispatcherTimer m_clock_poller;

		/// <summary>The "current time" (in UTC). When back-testing, this is a time in the past</summary>
		public DateTimeOffset UtcNow
		{
			get { return BackTesting ? Simulation.Clock : DateTimeOffset.UtcNow; }
		}

		/// <summary>A global switch to control actually placing orders</summary>
		public bool AllowTrades { [DebuggerStepThrough] get { return m_allow_trades; } }
		private bool m_allow_trades;

		/// <summary>Enable/Disable live trades</summary>
		public void SetAllowTrades(bool enabled)
		{
			// Don't allow trades when 'BackTesting' is enabled
			if (AllowTrades == enabled || BackTesting)
				return;

			// Notify about to change allow trades
			AllowTradesChanging.Raise(this, new PrePostEventArgs(after:false));

			// Stop any running bots
			foreach (var bot in Bots)
				bot.Active = false;

			// Reset any fake cash
			ResetFakeCash();

			// Enable/Disable
			m_allow_trades = enabled;
			Log.Write(ELogLevel.Debug, enabled ? "Live trades allowed" : "Live trades disabled");

			// Notify allow trades changed
			AllowTradesChanging.Raise(this, new PrePostEventArgs(after:true));
		}

		/// <summary>Raised before and after 'AllowTrades' is changed</summary>
		public event EventHandler<PrePostEventArgs> AllowTradesChanging;

		/// <summary>The main UI</summary>
		public Form UI { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>The logged on user</summary>
		public User User { get; private set; }

		/// <summary>Application log</summary>
		public Logger Log
		{
			[DebuggerStepThrough] get { return m_log; }
			private set
			{
				if (m_log == value) return;
				Util.Dispose(ref m_log);
				m_log = value;
			}
		}
		private Logger m_log;

		/// <summary>Log for profitable trades</summary>
		public Logger WinLog
		{
			[DebuggerStepThrough] get { return m_win_log; }
			private set
			{
				if (m_win_log == value) return;
				Util.Dispose(ref m_win_log);
				m_win_log = value;
			}
		}
		private Logger m_win_log;

		/// <summary>"Real" exchanges that are flagged as active</summary>
		public IEnumerable<Exchange> TradingExchanges
		{
			get { return Exchanges.Except(CrossExchange).Where(x => x.Enabled); }
		}

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange { get; private set; }

		/// <summary>Meta data for the known coins</summary>
		public CoinDataTable Coins
		{
			[DebuggerStepThrough] get { return m_coins; }
			private set
			{
				if (m_coins == value) return;
				m_coins = value;
			}
		}
		private CoinDataTable m_coins;

		/// <summary>The price data collectors</summary>
		public PriceDataMap PriceData
		{
			[DebuggerStepThrough] get { return m_price_data; }
			private set
			{
				if (m_price_data == value) return;
				Util.Dispose(ref m_price_data);
				m_price_data = value;
			}
		}
		private PriceDataMap m_price_data;

		/// <summary>The exchanges</summary>
		public BindingSource<Exchange> Exchanges
		{
			[DebuggerStepThrough] get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.Clear();
					m_exchanges.ListChanging -= HandleExchangesListChanging;
					m_exchanges.PositionChanged -= HandleCurrentExchangeChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.PositionChanged += HandleCurrentExchangeChanged;
					m_exchanges.ListChanging += HandleExchangesListChanging;
				}

				// Handlers
				void HandleExchangesListChanging(object sender, ListChgEventArgs<Exchange> e)
				{
					// Don't trigger pair updates on the exchanges collection changing,
					// do it after all exchanges have been added
					switch (e.ChangeType)
					{
					case ListChg.ItemAdded:
						{
							Log.Write(ELogLevel.Info, "Exchange Added: {0}".Fmt(e.Item.Name));
							e.Item.PropertyChanged += HandleExchangePropertyChanged;
							break;
						}
					case ListChg.ItemPreRemove:
						{
							e.Item.PropertyChanged -= HandleExchangePropertyChanged;
							if (e.Item == Exchanges.Current)
								Exchanges.Current = Exchanges.Except(e.Item).FirstOrDefault();
							break;
						}
					case ListChg.ItemRemoved:
						{
							Log.Write(ELogLevel.Info, "Exchange Removed: {0}".Fmt(e.Item.Name));
							Util.Dispose(e.Item);
							break;
						}
					}
				}
				void HandleCurrentExchangeChanged(object sender = null, PositionChgEventArgs e = null)
				{
					UpdateExchangeDetails();
				}
				void HandleExchangePropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					if (e.PropertyName == nameof(Exchange.Enabled))
					{
						Exchanges.ResetItem((Exchange)sender);
						TriggerPairsUpdate();
					}
				}
			}
		}
		private BindingSource<Exchange> m_exchanges;

		/// <summary>The trade pairs associated with the coins of interest. Note: the model does not own the pairs, the exchanges do</summary>
		public BindingSource<TradePair> Pairs
		{
			[DebuggerStepThrough] get { return m_pairs; }
			private set
			{
				if (m_pairs == value) return;
				if (m_pairs != null)
				{
					m_pairs.ListChanging -= HandlePairsListChanging;
				}
				m_pairs = value;
				if (m_pairs != null)
				{
					m_pairs.ListChanging += HandlePairsListChanging;
				}

				// Handlers
				void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
				{
					// Ensure the list is unique
					Debug.Assert(e.ChangeType != ListChg.ItemAdded || Pairs.Count(x => x == e.Item) == 1);
				}
			}
		}
		private BindingSource<TradePair> m_pairs;

		/// <summary>The balances on the current exchange</summary>
		public BindingSource<Balance> Balances
		{
			get { return m_balances; }
			private set
			{
				if (m_balances == value) return;
				m_balances = value;
				if (m_balances != null)
				{
					m_balances.AllowSort = true;
				}
			}
		}
		private BindingSource<Balance> m_balances;

		/// <summary>The positions on the current exchange</summary>
		public BindingSource<Position> Positions
		{
			get { return m_positions; }
			private set
			{
				if (m_positions == value) return;
				m_positions = value;
				if (m_positions != null)
				{
					m_positions.AllowSort = true;
				}
			}
		}
		private BindingSource<Position> m_positions;

		/// <summary>The historic trades on the current exchange</summary>
		public BindingSource<PositionFill> History
		{
			get { return m_history; }
			private set
			{
				if (m_history == value) return;
				if (m_history != null)
				{
				}
				m_history = value;
				if (m_history != null)
				{
					m_history.AllowSort = true;
				}
			}
		}
		private BindingSource<PositionFill> m_history;

		/// <summary>Bot instances</summary>
		public BindingSource<IBot> Bots
		{
			get { return m_bots; }
			private set
			{
				if (m_bots == value) return;
				if (m_bots != null)
				{
					// Dispose each bot
					using (Scope.Create(() => m_suspend_saving_bots = true, () => m_suspend_saving_bots = false))
						m_bots.Clear();

					m_bots.ListChanging -= HandleBotsListChanging;
				}
				m_bots = value;
				if (m_bots != null)
				{
					m_bots.ListChanging += HandleBotsListChanging;
				}

				// Handlers
				void HandleBotsListChanging(object sender, ListChgEventArgs<IBot> e)
				{
					switch (e.ChangeType)
					{
					case ListChg.ItemAdded:
						{
							e.Item.PropertyChanged += HandleBotPropertyChanged;
							break;
						}
					case ListChg.ItemPreRemove:
						{
							e.Item.PropertyChanged -= HandleBotPropertyChanged;
							break;
						}
					case ListChg.ItemRemoved:
						{
							Util.Dispose(e.Item);
							break;
						}
					}
					if (e.IsDataChanged && !m_suspend_saving_bots)
						Settings.Bots = Bots.Select(x => new Settings.BotData(x)).ToArray();
				}
				void HandleBotPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					Bots.ResetItem((IBot)sender);
				}
			}
		}
		private BindingSource<IBot> m_bots;
		private bool m_suspend_saving_bots;

		/// <summary>Mutex for multi-threaded access to market data</summary>
		private Mutex MarketDataLock
		{
			get { return m_market_data_lock; }
			set
			{
				if (m_market_data_lock == value) return;
				Util.Dispose(ref m_market_data_lock);
				m_market_data_lock = value;
			}
		}
		private Mutex m_market_data_lock;

		/// <summary>Synchronise access to the market data</summary>
		public Scope<bool> LockMarketData(int timeout = Timeout.Infinite)
		{
			return Scope.Create(
				() => MarketDataLock.WaitOne(timeout),
				 b => MarketDataLock.ReleaseMutex());
		}

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public BlockingCollection<Action> MarketUpdates { get; private set; }

		/// <summary>Raised when market data changes. I.e. after 'IntegrateMarketUpdates' is called</summary>
		public event EventHandler<MarketDataChangingEventArgs> MarketDataChanging;

		/// <summary>Process any pending market data updates</summary>
		private void IntegrateMarketUpdates()
		{
			Debug.Assert(AssertMainThread());
			Debug.Assert(m_in_integrate_market_updates == 0);

			// Ignore updates when back testing.
			if (BackTesting)
				return;

			// Notify market data updating
			MarketDataChanging.Raise(this, new MarketDataChangingEventArgs(done:false));

			// Lock the data while we're changing it
			using (LockMarketData())
			using (Positions.PreservePosition())
			using (History.PreservePosition())
			using (Balances.PreservePosition())
			using (Scope.Create(() => ++m_in_integrate_market_updates, () => --m_in_integrate_market_updates))
			{
				Positions.Position = -1;
				History.Position = -1;
				Balances.Position = -1;
				for (; MarketUpdates.TryTake(out var update);)
				{
					try
					{
						update();
					}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, "Market data integration error");
					}
				}
			}

			// Notify market data updating
			MarketDataChanging.Raise(this, new MarketDataChangingEventArgs(done:true));
		}
		private int m_in_integrate_market_updates;

		/// <summary>Raised to notify that the trade history has changed</summary>
		public event EventHandler TradeHistoryChanged;
		public void RaiseTradeHistoryChanged()
		{
			TradeHistoryChanged.Raise(this);
		}

		/// <summary>Call to kick-off an update of the pairs</summary>
		public void TriggerPairsUpdate()
		{
			// If an update is already pending, ignore
			if (m_pairs_update.Pending) return;
			m_pairs_update.Signal();

			// Run the update on the next message
			RunOnGuiThread(UpdatePairs);
		}
		private Trigger m_pairs_update;

		/// <summary>Get each exchange to update it's pairs</summary>
		private void UpdatePairs()
		{
			// Allow update pairs in back testing mode
			Debug.Assert(AssertMainThread());

			// Record the update issue number that this update is for
			Log.Write(ELogLevel.Info, "Updating pairs ...");
			m_pairs_update.Actioned();
			bool Abort() { return m_pairs_update.Pending || Shutdown.IsCancellationRequested; }
			if (Abort())
				return;

			// Local copy of shared variables
			var coi = Coins.Where(x => x.OfInterest).ToHashSet(x => x.Symbol);
			var exchanges = TradingExchanges.ToList();
			var sw = new Stopwatch().Start2();

			// Query for the pairs data on a worker thread
			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					// Get each exchange to update it's available pairs/coins
					foreach (var exch in exchanges)
						exch.UpdatePairs(coi);

					// Update the cross exchange after the other exchanges
					if (CrossExchange.Enabled)
						CrossExchange.UpdatePairs(coi);

					// Merge the results
					MarketUpdates.Add(() =>
					{
						// If the pairs have been invalidated in the meantime, give up
						if (Abort())
							return;

						// Update the Model's collection of pairs
						var pairs = new HashSet<TradePair>();
						foreach (var exch in Exchanges.Where(x => x.Enabled))
							pairs.AddRange(exch.Pairs.Values.Where(x => coi.Contains(x.Base) && coi.Contains(x.Quote)));

						// Update the collection of pairs in the model
						Pairs.Merge(pairs);

						// Done
						Log.Write(ELogLevel.Info, $"Trading pairs updated ... ({Pairs.Count} pairs, taking {sw.Elapsed.TotalSeconds} seconds)");

						// Notify pairs updated
						OnPairsUpdated();
					});
				}
				catch (Exception ex)
				{
					if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
					if (ex is OperationCanceledException) { return; }
					else Log.Write(ELogLevel.Error, ex, "Error updating pairs.");
				}
			});
		}

		/// <summary>Raised when the trading pairs are updated</summary>
		private void OnPairsUpdated()
		{
			PairsUpdated.Raise(this);
		}
		public event EventHandler PairsUpdated;

		/// <summary>Find the pair with the given name on 'exch' (or return null if not available)</summary>
		public TradePair FindPairOnExchange(string pair_name, Exchange exch)
		{
			return exch.Pairs.Values.FirstOrDefault(x => x.Name == pair_name);
		}

		/// <summary>Find the pair with the given name on the currently selected exchange (or return null if not available)</summary>
		public TradePair FindPairOnCurrentExchange(string pair_name)
		{
			return FindPairOnExchange(pair_name, Exchanges.Current);
		}

		/// <summary>Return a pair and time frame that has candle data and is as close as possible to 'pair' and 'time_frame'</summary>
		public bool FindCandleData(TradePair pair, ETimeFrame time_frame, out TradePair pair_out, out ETimeFrame time_frame_out)
		{
			pair_out = null;
			time_frame_out = ETimeFrame.None;

			// Find a pair with candle data
			var available = new ETimeFrame[0];
			foreach (var p in GetPairs())
			{
				available = p.CandleDataAvailable.ToArray();
				if (available.Length == 0) continue;
				pair_out = p;
				break;
			}

			// Find the nearest time frame in the available candle data
			if (available.Length != 0)
			{
				Debug.Assert(available.IsOrdered());
				var idx = available.BinarySearch(x => x.CompareTo(time_frame), find_insert_position:true);
				time_frame_out = available[Maths.Clamp(idx, 0, available.Length-1)];
			}

			return pair_out != null && time_frame_out != ETimeFrame.None;

			// Enumerate pairs of the same instrument
			IEnumerable<TradePair> GetPairs()
			{
				yield return pair;
				foreach (var p in Pairs.Except(pair).Where(x => x.Name == pair.Name))
					yield return p;
			}
		}

		/// <summary>Execute 'action' in the GUI thread context</summary>
		public void RunOnGuiThread(Action action, bool block = false)
		{
			if (block)
				m_dispatcher.Invoke(action);
			else
				m_dispatcher.BeginInvoke(action);
		}
		private Dispatcher m_dispatcher;

		/// <summary>Update the data sources for the exchange specific data</summary>
		private void UpdateExchangeDetails()
		{
			var exch = Exchanges.Current;
			Balances.DataSource = exch?.Balance;
			Positions.DataSource = exch?.Positions;
			History.DataSource = exch?.History;
		}

		/// <summary>Show a list of bots to choose from</summary>
		public void ShowAddBotUI()
		{
			var existing = Bots.ToHashSet(x => x.GetType());

			// Enumerate the available bots
			var bot_types = Plugins<IBot>.Enumerate(Misc.BotDirectory, SearchOption.TopDirectoryOnly, regex_filter:Misc.BotRegexFilter)
				.Where(x => !x.Unique || !existing.Contains(x.Type))
				.ToArray();

			// Show a list of available bots to pick from
			var dlg = new ListUI
			{
				Title = "Create Bot Instance",
				PromptText = "Select the Bot type to create",
				StartPosition = FormStartPosition.CenterParent,
				SelectionMode = SelectionMode.One,
				ShowInTaskbar = false,
				Size = new Size(260,240),
				Icon = UI.Icon,
			};
			using (dlg)
			{
				dlg.Items = bot_types;
				if (dlg.ShowDialog(UI) != DialogResult.OK)
					return;

				// Get the selected bot
				var plugin = (PluginFile)dlg.SelectedItems.FirstOrDefault();
				if (plugin == null)
					return;

				// Create the bot
				CreateBotInstance(plugin.Type, null);
			}
		}

		/// <summary>Create a new bot instance</summary>
		private void CreateBotInstance(Type bot_type, XElement settings_xml)
		{
			try
			{
				// Create a new instance of the bot
				var bot = (IBot)Activator.CreateInstance(bot_type, new object[] { this, settings_xml });
				Bots.Add(bot);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, $"Failed to create an instance of Bot '{bot_type.Name}'");
				MsgBox.Show(UI, $"Failed to create an instance of Bot '{bot_type.Name}'\r\n{ex.MessageFull()}", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		/// <summary>Create the bots given in the settings</summary>
		private void CreateBotsFromSettings()
		{
			using (Scope.Create(() => m_suspend_saving_bots = true, () => m_suspend_saving_bots = false))
			{
				var available_bots = Plugins<IBot>.Enumerate(Misc.BotDirectory, SearchOption.TopDirectoryOnly, Misc.BotRegexFilter).ToArray();
				foreach (var bot_data in Settings.Bots)
				{
					var bot = available_bots.FirstOrDefault(x => x.Type.FullName == bot_data.BotType);
					if (bot != null)
						CreateBotInstance(bot.Type, bot_data.Config);
					else
						Log.Write(ELogLevel.Warn, $"Could not load Bot type {bot_data.BotType}. The plugin is no longer available");
				}
			}
		}

		/// <summary>Add a dockable to the UI</summary>
		public void AddToUI(IDockable content, DockContainer.DockLocation site = null)
		{
			OnAddToUI.Raise(this, new AddToUIEventArgs(content, site));
		}
		public void AddToUI(ToolStrip ts)
		{
			OnAddToUI.Raise(this, new AddToUIEventArgs(ts));
		}
		public event EventHandler<AddToUIEventArgs> OnAddToUI;

		/// <summary>Launch UI to edit a trade</summary>
		public void EditTrade(Trade trade, ulong? existing_order_id)
		{
			OnEditTrade.Raise(this, new EditTradeEventArgs(trade, existing_order_id));
		}
		public void EditTrade(Position pos)
		{
			EditTrade(new Trade(pos), pos.OrderId);
		}
		public event EventHandler<EditTradeEventArgs> OnEditTrade;

		/// <summary>Return the chart settings for 'pair'</summary>
		public Settings.ChartSettings ChartSettings(TradePair pair)
		{
			var settings = Settings.Charts.FirstOrDefault(x => x.SymbolCode == pair.Name);
			if (settings == null)
			{
				// Create new settings for this pair, and save them
				settings = new Settings.ChartSettings(pair.Name);
				Settings.Charts = Settings.Charts.Concat(settings).ToArray();
			}

			settings.Inherit = Settings.ChartTemplate;
			return settings;
		}

		/// <summary>Assert that it is valid to read market data in the current thread</summary>
		public bool AssertMarketDataRead()
		{
			// If this is the main thread, any access is valid. There could be problems with async methods tho
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId)
				return true;

			// If this is not the main thread, then the caller should be holding 'MarketDataLock'
			// If not, then 'lck.Value' will be false (because the main thread has it)
			using (var lck = LockMarketData(0))
			{
				if (lck.Value && m_in_integrate_market_updates == 0) return true;
				throw new Exception("Invalid access to market data");
			}
		}

		/// <summary>Assert that it is valid to write market data in the current thread</summary>
		public bool AssertMarketDataWrite()
		{
			// Only the main thread can write to the market data
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Invalid access to market data");
		}

		/// <summary>Assert that the current thread is the main thread</summary>
		public bool AssertMainThread()
		{
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			throw new Exception("Cross-thread call detected");
		}

		/// <summary>Debugging test</summary>
		public void Test()
		{
			try
			{
				var pair = Pairs.FirstOrDefault(x => x.Name == "BTC/USDT");
				using (var dlg = new MsgBox())
				using (var instr = new Instrument("TEST", PriceData[pair, ETimeFrame.Min30]))
					dlg.ShowDialog(UI);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Test Failed");
			}
		}
	}

	#region Containers
	public class CoinDataTable :BindingSource<Settings.CoinData>
	{
		private readonly Model Model;
		public CoinDataTable(Model model)
		{
			Model = model;
			DataSource = new BindingListEx<Settings.CoinData>(Model.Settings.Coins.ToList());
		}
		public Settings.CoinData this[string sym]
		{
			get
			{
				var idx = this.IndexOf(x => x.Symbol == sym);
				return idx >= 0 ? this[idx] : this.Add2(new Settings.CoinData(sym, 1m));
			}
		}
		protected override void OnListChanging(object sender, ListChgEventArgs<Settings.CoinData> args)
		{
			if (args.IsDataChanged)
			{
				// Record the coins in the settings
				Model.Settings.Coins = this.ToArray();

				// The COI have changed, we'll need to update pairs.
				Model.TriggerPairsUpdate();
			}
			base.OnListChanging(sender, args);
		}
	}
	public class PriceDataMap :IDisposable
	{
		public class TFMap :Dictionary<ETimeFrame, PriceData> { }
		public class TPMap :Dictionary<TradePair, TFMap> { }

		private readonly Model m_model;
		public PriceDataMap(Model model)
		{
			m_model = model;
			Pairs = new TPMap();
		}
		public void Dispose()
		{
			foreach (var tf_map in Pairs.Values)
				foreach (var pd in tf_map.Values)
					Util.Dispose(pd);

			Pairs.Clear();
		}
		public TPMap Pairs { get; private set; }
		public PriceData this[TradePair pair, ETimeFrame time_frame]
		{
			get
			{
				if (pair == null)
					throw new ArgumentNullException(nameof(pair));
				if (time_frame == ETimeFrame.None)
					throw new Exception("TimeFrame is None. No price data available");
				if (!pair.Exchange.Enabled)
					throw new Exception("Requesting a trading pair on an inactive exchange");

				var tf_map = Pairs.TryGetValue(pair, out var tf) ? tf : Pairs.Add2(pair, new TFMap());
				return tf_map.TryGetValue(time_frame, out var pd) ? pd : tf_map.Add2(time_frame, new PriceData(m_model, pair, time_frame));
			}
		}
	}
	#endregion

	#region EventArgs
	public class MarketDataChangingEventArgs :EventArgs
	{
		public MarketDataChangingEventArgs(bool done)
		{
			Done = done;
		}
		public bool Done { get; private set; }
	}
	public class AddToUIEventArgs :EventArgs
	{
		public AddToUIEventArgs(IDockable dockable, DockContainer.DockLocation dock_location = null)
		{
			Dockable = dockable;
			DockLocation = dock_location;
		}
		public AddToUIEventArgs(ToolStrip toolbar)
		{
			Toolbar = toolbar;
		}

		/// <summary>Dockable content</summary>
		public IDockable Dockable { get; private set; }
		public DockContainer.DockLocation DockLocation { get; private set; }

		/// <summary>Tool bar UI element</summary>
		public ToolStrip Toolbar { get; private set; }
	}
	public class EditTradeEventArgs :EventArgs
	{
		public EditTradeEventArgs(Trade trade, ulong? existing_order_id)
		{
			Trade = trade;
			ExistingOrderId = existing_order_id;
		}

		/// <summary>The trade to edit/create</summary>
		public Trade Trade { get; private set; }

		/// <summary>The ID of the existing position this trade represents</summary>
		public ulong? ExistingOrderId { get; private set; }
	}
	#endregion
}