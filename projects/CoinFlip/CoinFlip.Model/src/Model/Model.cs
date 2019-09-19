using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using CoinFlip.Bots;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Model : IDisposable
	{
		// Notes:
		//  Startup Process:
		// - The application is idle until a user is assigned. Setting the user allows
		//   the exchanges to be created (with API keys), followed by the main loop
		//   starting up.
		// - Each exchange constructor creates an instance of its API object. There
		//   shouldn't be any async/API calls during construction, instead a dispatcher
		//   action is queued by each exchange to run the InitAsync() method of each
		//   API object. Following that, the updates are called once to initialise 
		//   data, and then the 'UpdateThreadActive' is set true (if active according
		//   to the settings).
		// - Exchanges should use the 'IntegrateDataUpdates' mechanism for merging new
		//   live exchange data because it is controllable when switching to back testing
		//   mode.

		static Model()
		{
			Log = new Logger("CoinFlip", new LogToFile(Misc.ResolveUserPath("Logs\\log.txt"), append: false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			DataUpdates = new BlockingCollection<Action>();
		}
		public Model(Func<IChartView> create_chart_cb)
		{
			try
			{
				Shutdown = new CancellationTokenSource();
				Exchanges = new ExchangeContainer();
				Coins = new CoinDataList();
				Funds = new FundContainer();
				Bots = new BotContainer(this);
				PriceData = new PriceDataMap(Shutdown.Token);
				Charts = new ChartContainer(create_chart_cb);
				Indicators = new IndicatorContainer();
				SelectedOpenOrders = new ObservableCollection<Order>();
				SelectedCompletedOrders = new ObservableCollection<OrderCompleted>();
				SelectedTransfers = new ObservableCollection<Transfer>();
				EditTradeContexts = new List<EditTradeContext>();

				// Enable settings auto save after everything is up and running
				SettingsData.Settings.AutoSaveOnChanges = true;

				AllowTradesChanged += HandleAllowTradesChanged;
				BackTestingChange += HandleBackTestingChange;
			}
			catch
			{
				Shutdown?.Cancel();
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Bots?.Clear();
			Indicators?.Clear();

			MainLoopRunning = false;

			BackTestingChange -= HandleBackTestingChange;
			AllowTradesChanged -= HandleAllowTradesChanged;

			PriceData = null;
			User = null;
			Shutdown = null;
			Util.Dispose(Log);
		}

		/// <summary>The logged on user</summary>
		public User User
		{
			get { return m_user; }
			set
			{
				if (m_user == value) return;
				UserChange?.Invoke(this, new PrePostEventArgs(after: false));
				if (m_user != null)
				{
					// Run the internal loop
					MainLoopRunning = false;

					// Drop connections to exchanges
					Util.DisposeAll(Exchanges);
					Exchanges.Clear();
				}
				m_user = value;
				if (m_user != null)
				{
					// Ensure the keys file exists
					if (!Path_.FileExists(m_user.KeysFilepath))
						m_user.NewKeys();

					// Save the last user name
					SettingsData.Settings.LastUser = m_user.Name;

					// Create Exchange instances with this user's API keys
					CreateExchanges();

					// Create the bot instances
					Bots.LoadFromSettings();

					// Run the internal loop
					MainLoopRunning = true;
				}
				UserChange?.Invoke(this, new PrePostEventArgs(after: true));
			}
		}
		private User m_user;
		public event EventHandler<PrePostEventArgs> UserChange;

		/// <summary>A main thread timer that simply updates the 'UtcNow' time to the current time (or back-testing clock)</summary>
		public bool MainLoopRunning
		{
			get { return m_main_loop_timer != null; }
			set
			{
				if (MainLoopRunning == value) return;
				Debug.Assert(Misc.AssertMainThread());

				if (MainLoopRunning)
				{
					m_main_loop_timer.Stop();
					Log.Write(ELogLevel.Debug, "MainLoop stopped");
				}
				m_main_loop_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (MainLoopRunning)
				{
					Log.Write(ELogLevel.Debug, "MainLoop started");
					m_main_loop_timer.Start();
					HandleTick();
				}

				// Handlers
				void HandleTick(object sender = null, EventArgs e = null)
				{
					try
					{
						// Ignore messages after the timer is stopped
						if (!MainLoopRunning)
							return;

						// Process any pending market data updates
						if (!BackTesting)
							IntegrateDataUpdates();

						// Update the nett worth value
						UpdateNettWorth();

						// Notify Heartbeat 
						MainLoopTick?.Invoke(this, EventArgs.Empty);
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
		public event EventHandler MainLoopTick;
		private DispatcherTimer m_main_loop_timer;

		/// <summary>Logging</summary>
		public static Logger Log { get; }

		/// <summary>The current time. This might be in the past during a simulation</summary>
		public static DateTimeOffset UtcNow => BackTesting ? SimClock : DateTimeOffset.UtcNow;
		public static DateTimeOffset SimClock { get; internal set; }

		/// <summary>True if live trading</summary>
		public static bool AllowTrades
		{
			get { return s_allow_trades; }
			set
			{
				if (s_allow_trades == value) return;
				s_allow_trades = value;
				AllowTradesChanged?.Invoke(null, EventArgs.Empty);
			}
		}
		public static event EventHandler AllowTradesChanged;
		private void HandleAllowTradesChanged(object sender, EventArgs e)
		{
			//...
		}
		private static bool s_allow_trades;

		/// <summary>True if back testing is enabled</summary>
		public static bool BackTesting
		{
			get { return s_back_testing; }
			set
			{
				if (s_back_testing == value) return;
				BackTestingChange?.Invoke(null, new PrePostEventArgs(after: false));
				s_back_testing = value;
				BackTestingChange?.Invoke(null, new PrePostEventArgs(after: true));
			}
		}
		public static event EventHandler<PrePostEventArgs> BackTestingChange;
		private void HandleBackTestingChange(object sender, PrePostEventArgs e)
		{
			// If back testing is about to be enabled...
			if (!BackTesting && e.Before)
			{
				// Turn off the update thread for each exchange
				foreach (var exch in Exchanges)
					exch.UpdateThreadActive = false;

				// Turn off the update thread for each price data instance
				foreach (var pd in PriceData)
					pd.UpdateThreadActive = false;

				// Deactivate all live trading bots (actually, deactivate all of them)
				Bots.Clear();

				// Integrate market updates so that updates from live data don't end up in back testing data.
				IntegrateDataUpdates();

				// Save and reset the current Fund container
				Funds.SaveToSettings(Exchanges);
				Funds.AssignFunds(new FundData[0]);

				// Save and reset the indicator container
				Indicators.Save();
				Indicators.Clear();

				// Disable live trading
				AllowTrades = false;
			}

			// If back testing has just been enabled...
			if (BackTesting && e.After)
			{
				// Create the fund container from the back testing settings
				Funds.AssignFunds(SettingsData.Settings.BackTesting.TestFunds);

				// Create the back testing bots
				Bots.LoadFromSettings();

				// Reset the balance collections in each exchange
				foreach (var exch in Exchanges)
					exch.Balance.Clear();

				// Create the simulation manager
				Simulation = new Simulation(TradingExchanges, PriceData, Bots);

				// Enable backtesting bots based on their settings
				foreach (var bot in Bots.Where(x => x.BackTesting))
					bot.Active = bot.BotData.Active;

				// Restore the backtesting indicators
				Indicators.Load();
			}

			// If back testing is about to be disabled...
			if (BackTesting && e.Before)
			{
				// Deactivate all back testing bots (actually, deactivate all of them)
				Bots.Clear();

				// Remove (without saving) the current fund container
				Funds.AssignFunds(new FundData[0]);

				// Save and reset the indicator container
				Indicators.Save();
				Indicators.Clear();
			}

			// If back testing has just been disabled...
			if (!BackTesting && e.After)
			{
				// Clean up the simulation
				Simulation = null;

				// Restore the fund container from settings
				Funds.AssignFunds(SettingsData.Settings.LiveFunds);

				// Restore the indicators
				Indicators.Load();

				// Restore the live trading bots
				Bots.LoadFromSettings();

				// Reset the balance collections in each exchange
				foreach (var exch in Exchanges)
					exch.Balance.Clear();

				// Turn on the update thread for each price data instance
				foreach (var pd in PriceData)
					pd.UpdateThreadActive = pd.RefCount != 0;

				// Turn on the update thread for each exchange
				foreach (var exch in Exchanges)
					exch.UpdateThreadActive = exch.Enabled;

				// Enable live trading bots based on their settings
				foreach (var bot in Bots.Where(x => !x.BackTesting))
					bot.Active = bot.BotData.Active;
			}
		}
		private static bool s_back_testing;

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationTokenSource Shutdown
		{
			[DebuggerStepThrough]
			get { return m_shutdown; }
			private set
			{
				if (m_shutdown == value) return;
				if (m_shutdown != null && !m_shutdown.IsCancellationRequested)
					throw new Exception("Shouldn't dispose a cancellation token before cancelling it");

				Util.Dispose(ref m_shutdown);
				m_shutdown = value;
			}
		}
		private CancellationTokenSource m_shutdown;

		/// <summary>The supported exchanges</summary>
		public ExchangeContainer Exchanges
		{
			get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.CollectionChanged -= HandleExchangeCollectionChanged;
					Util.DisposeAll(m_exchanges);
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CollectionChanged += HandleExchangeCollectionChanged;
				}

				// Handlers
				void HandleExchangeCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					switch (e.Action)
					{
					case NotifyCollectionChangedAction.Add:
						{
							foreach (var exch in e.NewItems.Cast<Exchange>())
							{
								exch.Coins.ListChanging += HandleCoinsChanging;
								exch.Orders.ListChanging += HandleOrdersChanging;
								exch.History.ListChanging += HandleHistoryChanging;
							}
							break;
						}
					case NotifyCollectionChangedAction.Remove:
						{
							foreach (var exch in e.OldItems.Cast<Exchange>())
							{
								exch.History.ListChanging -= HandleHistoryChanging;
								exch.Orders.ListChanging -= HandleOrdersChanging;
								exch.Coins.ListChanging -= HandleCoinsChanging;
							}
							break;
						}
					}
				}
				void HandleCoinsChanging(object sender, ListChgEventArgs<Coin> e)
				{
					var exchange = ((CoinCollection)sender).Exchange;
					CoinChanging?.Invoke(exchange, e);
				}
				void HandleOrdersChanging(object sender, ListChgEventArgs<Order> e)
				{
					var exchange = ((OrdersCollection)sender).Exchange;
					OrdersChanging?.Invoke(exchange, e);
				}
				void HandleHistoryChanging(object sender, ListChgEventArgs<OrderCompleted> e)
				{
					var exchange = ((OrdersCompletedCollection)sender).Exchange;
					HistoryChanging?.Invoke(exchange, e);
				}
			}
		}
		private ExchangeContainer m_exchanges;

		/// <summary>Data for each supported currency</summary>
		public CoinDataList Coins
		{
			get => m_coins;
			private set
			{
				if (m_coins == value) return;
				if (m_coins != null)
				{
					m_coins.CollectionChanged -= HandleCollectionChanged;
				}
				m_coins = value;
				if (m_coins != null)
				{
					m_coins.CollectionChanged += HandleCollectionChanged;
				}

				// Handlers
				void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					// Refresh the pairs in each exchange
					foreach (var exch in Exchanges)
						exch.PairsUpdateRequired = true;
				}
			}
		}
		private CoinDataList m_coins;

		/// <summary>The sub-accounts used to partition balances on each exchange</summary>
		public FundContainer Funds { get; }

		/// <summary>The trading bot instances</summary>
		public BotContainer Bots { get; }

		/// <summary>The repository of candle data for all pairs and time frames</summary>
		public PriceDataMap PriceData
		{
			get { return m_price_data; }
			private set
			{
				if (m_price_data == value) return;
				Util.Dispose(ref m_price_data);
				m_price_data = value;
			}
		}
		private PriceDataMap m_price_data;

		/// <summary>The simulation manager. Not null while 'BackTesting' is enabled</summary>
		public Simulation Simulation
		{
			get { return m_simulation; }
			private set
			{
				if (m_simulation == value) return;
				if (m_simulation != null)
				{
					m_simulation.SimPropertyChanged -= HandleSimPropertyChanged;
					Util.Dispose(ref m_simulation);
				}
				m_simulation = value;
				if (m_simulation != null)
				{
					m_simulation.SimPropertyChanged += HandleSimPropertyChanged;
				}

				// Handler
				void HandleSimPropertyChanged(object sender, EventArgs e)
				{
					SimPropertyChanged?.Invoke(Simulation, e);
				}
			}
		}
		private Simulation m_simulation;

		/// <summary>The available charts</summary>
		public ChartContainer Charts { get; }

		/// <summary>The indicators associated with each pair</summary>
		public IndicatorContainer Indicators { get; }

		/// <summary>Open orders that are 'selected'</summary>
		public ObservableCollection<Order> SelectedOpenOrders { get; }

		/// <summary>Completed orders that are 'selected'</summary>
		public ObservableCollection<OrderCompleted> SelectedCompletedOrders { get; }

		/// <summary>Transfers that are 'selected'</summary>
		public ObservableCollection<Transfer> SelectedTransfers { get; }
		
		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange { get; private set; }

		/// <summary>All exchanges except the CrossExchange</summary>
		public IEnumerable<Exchange> TradingExchanges => Exchanges.Where(x => !(x is CrossExchange));

		/// <summary>Pending data updates awaiting integration at the right time</summary>
		public static BlockingCollection<Action> DataUpdates { get; }

		/// <summary>Raised when market data changes, i.e. before and after 'IntegrateDataUpdates' is called</summary>
		public event EventHandler<DataChangingEventArgs> DataChanging;

		/// <summary>Raised when a coin is added or removed from an exchange</summary>
		public event EventHandler<ListChgEventArgs<Coin>> CoinChanging;

		/// <summary>Raised when an order is added or removed from an exchange</summary>
		public event EventHandler<ListChgEventArgs<Order>> OrdersChanging;

		/// <summary>Raised when a completed order is added or updated from an exchange</summary>
		public event EventHandler<ListChgEventArgs<OrderCompleted>> HistoryChanging;

		/// <summary>Notify when the NettWorth value changes</summary>
		public event EventHandler<ValueChangedEventArgs<Unit<decimal>>> NettWorthChanged;

		/// <summary>Raised when a property of the simulation changes</summary>
		public event EventHandler SimPropertyChanged;

		/// <summary>Total holdings value across all exchanges and all currencies</summary>
		public Unit<decimal> NettWorth
		{
			get => m_nett_worth;
			set
			{
				if (m_nett_worth == value) return;
				var args = new ValueChangedEventArgs<Unit<decimal>>(value, m_nett_worth);
				m_nett_worth = value;
				NettWorthChanged?.Invoke(this, args);
			}
		}
		private Unit<decimal> m_nett_worth;

		/// <summary>Persist the current fund balances to settings</summary>
		public void SaveFundBalances()
		{
			Funds.SaveToSettings(Exchanges);
		}

		/// <summary>Create Exchange instances with this user's API keys</summary>
		private void CreateExchanges()
		{
			// Remove any previous exchanges
			Util.DisposeAll(Exchanges);
			Exchanges.Clear();

			// Create Exchange instances
			string apikey, secret;
			Exchanges.Add(User.GetKeys(nameof(Binance), out apikey, out secret) == User.EResult.Success
				? new Binance(apikey, secret, Coins, Shutdown.Token)
				: new Binance(Coins, Shutdown.Token));
			
			//hack
			//Exchanges.Add(User.GetKeys(nameof(Poloniex), out apikey, out secret) == User.EResult.Success
			//	? new Poloniex(apikey, secret, Coins, Shutdown.Token)
			//	: new Poloniex(Coins, Shutdown.Token));
			//Exchanges.Add(User.GetKeys(nameof(Bittrex), out apikey, out secret) == User.EResult.Success
			//	? new Bittrex(apikey, secret, Coins, Shutdown.Token)
			//	: new Bittrex(Coins, Shutdown.Token));

			// Create the Cross-Exchange after all others have been created
			//CrossExchange = Exchanges.Insert2(0, new CrossExchange(Exchanges, Coins, Shutdown.Token));
		}

		/// <summary>Process any pending data updates</summary>
		public void IntegrateDataUpdates()
		{
			// Notes: do do 'other' jobs in here, this is just for data integration.
			// Put 'main loop' tasks in the HandleTick of the main loop.

			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(m_in_integrate_data_updates == 0);
			Debug.Assert(!BackTesting);

			// Notify market data about to update
			DataChanging?.Invoke(this, new DataChangingEventArgs(after: false));

			using (Scope.Create(() => ++m_in_integrate_data_updates, () => --m_in_integrate_data_updates))
			{
				// Pull a task from the queue and execute it
				for (; DataUpdates.TryTake(out var update);)
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

			// Notify market data update complete
			DataChanging?.Invoke(this, new DataChangingEventArgs(after: true));
		}
		private int m_in_integrate_data_updates;

		/// <summary>Set the latest value of 'NettWorth'</summary>
		private void UpdateNettWorth()
		{
			var worth = 0m._(SettingsData.Settings.ValuationCurrency);
			foreach (var exch in Exchanges)
			{
				foreach (var bal in exch.Balance)
					worth += bal.NettValue;
			}
			NettWorth = worth;
		}

		/// <summary>Edit a new or existing trade</summary>
		public void EditTrade(Trade trade)
		{
			// Initiate a trade edit
			var args = EditTradeContexts.Add2(new EditTradeContext(trade, this));
			args.Closed += delegate { EditTradeContexts.Remove(args); };

			// Start editing 'trade'
			EditingTrade?.Invoke(this, args);
		}
		private List<EditTradeContext> EditTradeContexts { get; }
		public event EventHandler<EditTradeContext> EditingTrade;

		/// <summary>True if 'trade' is open in an editor somewhere</summary>
		public bool IsOpenInEditor(Trade trade)
		{
			return trade is Order ord && EditTradeContexts.Any(x => x.OrderId == ord.OrderId);
		}

	}

	#region EventArgs
	public class DataChangingEventArgs : PrePostEventArgs
	{
		public DataChangingEventArgs(bool after)
			:base(after)
		{}
	}
	public class EditTradeContext : EventArgs
	{
		// Notes:
		//  - Observers sign up to the 'EditingTrade' event. When the event fires, observers attach
		//    handlers to this instance. It is expected that some user interface element will call
		//    CancelTrade(), CommitChanges(), or EndEdit() which will signal to all observers the
		//    outcome of the edit and this dispose this instance.

		public EditTradeContext(Trade original, Model model)
		{
			Original = original;
			Trade = new Trade(original);
			Model = model;
		}

		/// <summary>The order of the order being editted (null if the trade is not an existing order)</summary>
		public long? OrderId => (Original as Order)?.OrderId;

		/// <summary>The existing, unmodified trade or order</summary>
		public Trade Original { get; }

		/// <summary>The trade to edit</summary>
		public Trade Trade { get; }

		/// <summary>The app model</summary>
		public Model Model { get; }

		/// <summary></summary>
		public async Task Commit()
		{
			// If the original trade is a live order...
			if (Original is Order order)
			{
				// If it hasn't actually changed don't bother modifying the order.
				if (Original.Equals(Trade))
					return;

				// Cancel the previous order
				await order.CancelOrder(Model.Shutdown.Token);
			}

			// Create a new trade, or update the existing one
			await Trade.CreateOrder(Model.Shutdown.Token);
		}

		/// <summary>Notify that editing is finished</summary>
		public void EndEdit()
		{
			// Notify of closing, then drop all handlers
			Closed?.Invoke(this, this);
			Closed = null;
		}

		/// <summary>Raised when editting ends</summary>
		public event EventHandler Closed;

	}
	#endregion
}
