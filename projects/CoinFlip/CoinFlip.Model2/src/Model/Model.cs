﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Model : IDisposable
	{
		static Model()
		{
			Log = new Logger("CF", new LogToFile(Misc.ResolveUserPath("Logs\\log.txt"), append: false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			DataUpdates = new BlockingCollection<Action>();
		}
		public Model()
		{
			try
			{
				Shutdown = new CancellationTokenSource();
				Exchanges = new ExchangeContainer();
				Coins = new CoinDataList();
				Funds = new FundContainer();
				PriceData = new PriceDataMap(Shutdown.Token);
				Charts = new ObservableCollection<IChartView>();
				SelectedOpenOrders = new ObservableCollection<Order>();
				SelectedCompletedOrders = new ObservableCollection<OrderCompleted>();

				// Enable settings auto save after everything is up and running
				SettingsData.Settings.AutoSaveOnChanges = true;

				AllowTradesChanged += HandleAllowTradesChanged;
				BackTestingChanged += HandleBackTestingChanged;

				// Run the internal loop
				MainLoopRunning = true;
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
			MainLoopRunning = false;

			BackTestingChanged -= HandleBackTestingChanged;
			AllowTradesChanged -= HandleAllowTradesChanged;

			PriceData = null;
			Funds = null;
			User = null;
			Shutdown = null;
			Util.Dispose(Log);
		}

		/// <summary>Logging</summary>
		public static Logger Log { get; }

		/// <summary>The current time. This might be in the past during a simulation</summary>
		public static DateTimeOffset UtcNow => BackTesting ? SimClock : DateTimeOffset.UtcNow;
		public static DateTimeOffset SimClock { get; private set; }

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
				s_back_testing = value;
				BackTestingChanged?.Invoke(null, EventArgs.Empty);
			}
		}
		public static event EventHandler BackTestingChanged;
		private void HandleBackTestingChanged(object sender, EventArgs e)
		{
			// Create/Destroy the simulation manager
			Simulation = BackTesting ? new Simulation(TradingExchanges) : null;
		}
		private static bool s_back_testing;

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
						// Process any pending market data updates
						IntegrateDataUpdates();

						// Simulate fake orders being filled
						if (!AllowTrades && !BackTesting)
						{ }// todo SimulateFakeOrders();
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
		private DispatcherTimer m_main_loop_timer;

		/// <summary>The logged on user</summary>
		public User User
		{
			get { return m_user; }
			set
			{
				if (m_user == value) return;
				if (m_user != null)
				{
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

					// Create Exchange instances with this user's API keys
					CreateExchanges();

					// Save the last user name
					SettingsData.Settings.LastUser = m_user.Name;
				}
			}
		}
		private User m_user;

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
				Util.DisposeAll(m_exchanges);
				m_exchanges = value;
			}
		}
		private ExchangeContainer m_exchanges;

		/// <summary>Data for each supported currency</summary>
		public CoinDataList Coins
		{
			get { return m_coins; }
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
					// Refresh the coins in each exchange
					foreach (var exch in Exchanges)
						exch.PairsUpdateRequired = true;
				}
			}
		}
		private CoinDataList m_coins;

		/// <summary>The sub-accounts used to partition balances on each exchange</summary>
		public FundContainer Funds
		{
			get { return m_funds; }
			private set
			{
				if (m_funds == value) return;
				m_funds = value;
			}
		}
		private FundContainer m_funds;

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
				Util.Dispose(ref m_simulation);
				m_simulation = value;
			}
		}
		private Simulation m_simulation;

		/// <summary>The available charts</summary>
		public ObservableCollection<IChartView> Charts { get; }

		/// <summary>Open orders that are 'selected'</summary>
		public ObservableCollection<Order> SelectedOpenOrders { get; }

		/// <summary>Completed orders that are 'selected'</summary>
		public ObservableCollection<OrderCompleted> SelectedCompletedOrders { get; }

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange => Exchanges.OfType<CrossExchange>().FirstOrDefault();

		/// <summary>All exchanges except the CrossExchange</summary>
		public IEnumerable<Exchange> TradingExchanges => Exchanges.Where(x => !(x is CrossExchange));

		/// <summary>Pending data updates awaiting integration at the right time</summary>
		public static BlockingCollection<Action> DataUpdates { get; }

		/// <summary>Raised when market data changes, i.e. before and after 'IntegrateDataUpdates' is called</summary>
		public event EventHandler<DataChangingEventArgs> DataChanging;

		/// <summary>Create Exchange instances with this user's API keys</summary>
		private void CreateExchanges()
		{
			// Remove any previous exchanges
			Util.DisposeAll(Exchanges);
			Exchanges.Clear();

			// Create Exchange instances
			string apikey, secret;
			Exchanges.Add(User.GetKeys(nameof(Poloniex), out apikey, out secret) == User.EResult.Success
				? new Poloniex(apikey, secret, Coins, Shutdown.Token)
				: new Poloniex(Coins, Shutdown.Token));
			Exchanges.Add(User.GetKeys(nameof(Binance), out apikey, out secret) == User.EResult.Success
				? new Binance(apikey, secret, Coins, Shutdown.Token)
				: new Binance(Coins, Shutdown.Token));
			Exchanges.Add(User.GetKeys(nameof(Bittrex), out apikey, out secret) == User.EResult.Success
				? new Bittrex(apikey, secret, Coins, Shutdown.Token)
				: new Bittrex(Coins, Shutdown.Token));

			// Create the Cross-Exchange after all others have been created
			Exchanges.Insert(0, new CrossExchange(Exchanges, Coins, Shutdown.Token));
		}

		/// <summary>Process any pending data updates</summary>
		private void IntegrateDataUpdates()
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(m_in_integrate_data_updates == 0);

			// Ignore updates when back testing.
			if (BackTesting)
				return;

			// Notify market data about to update
			DataChanging?.Invoke(this, new DataChangingEventArgs(done: false));

			// Lock the data while we're changing it
			//using (LockMarketData())
			//using (Positions.PreservePosition())
			//using (History.PreservePosition())
			//using (Balances.PreservePosition())
			using (Scope.Create(() => ++m_in_integrate_data_updates, () => --m_in_integrate_data_updates))
			{
				//Positions.Position = -1;
				//History.Position = -1;
				//Balances.Position = -1;

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
			DataChanging?.Invoke(this, new DataChangingEventArgs(done: true));
		}
		private int m_in_integrate_data_updates;
	}

	#region EventArgs
	public class DataChangingEventArgs : EventArgs
	{
		public DataChangingEventArgs(bool done)
		{
			Done = done;
		}
		public bool Done { get; private set; }
	}
	#endregion
}