using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	public class Model :IDisposable
	{
		static Model()
		{
			Log = new Logger("CF", new LogToFile(Misc.ResolveUserPath("Logs\\log.txt"), append: false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			MarketUpdates = new BlockingCollection<Action>();
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

				// Run these steps after construction is complete
				Misc.RunOnMainThread(() =>
				{
					//// Create Bots listed in the settings
					//CreateBotsFromSettings();

				});

				// Enable settings auto save after everything is up and running
				SettingsData.Settings.AutoSaveOnChanges = true;

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
		public static bool AllowTrades { get; private set; }

		/// <summary>True if back testing is enabled</summary>
		public static bool BackTesting { get; private set; }

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
						IntegrateMarketUpdates();

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

					// On a new user, reconnect to all of the exchanges
					string apikey, secret;
					if (m_user.GetKeys(nameof(Poloniex), out apikey, out secret) == User.EResult.Success)
						Exchanges.Add(new Poloniex(apikey, secret, Shutdown.Token));
					//if (LoadAPIKeys(user, nameof(Bitfinex), out key, out secret)) Exchanges.Add(new Bitfinex(this, key, secret));
					//if (LoadAPIKeys(user, nameof(Bittrex), out key, out secret)) Exchanges.Add(new Bittrex(this, key, secret));
					//if (LoadAPIKeys(user, nameof(Cryptopia), out key, out secret)) Exchanges.Add(new Cryptopia(this, key, secret));
					//if (LoadAPIKeys(user, nameof(Binance), out key, out secret)) Exchanges.Add(new Binance(this, key, secret));
					Exchanges.Insert(0, new CrossExchange(Exchanges, Shutdown.Token));

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
				if (m_exchanges != null)
				{
					m_exchanges.CollectionChanged -= HandleCollectionChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CollectionChanged += HandleCollectionChanged;
				}

				// Handler
				void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					
				}
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

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange => Exchanges.OfType<CrossExchange>().FirstOrDefault();

		/// <summary>All exchanges except the CrossExchange</summary>
		public IEnumerable<Exchange> TradingExchanges => Exchanges.Where(x => !(x is CrossExchange));

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public static BlockingCollection<Action> MarketUpdates { get; }

		/// <summary>Raised when market data changes, i.e. before and after 'IntegrateMarketUpdates' is called</summary>
		public event EventHandler<MarketDataChangingEventArgs> MarketDataChanging;

		/// <summary>Process any pending market data updates</summary>
		private void IntegrateMarketUpdates()
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(m_in_integrate_market_updates == 0);

			// Ignore updates when back testing.
			if (BackTesting)
				return;

			// Notify market data about to update
			MarketDataChanging?.Invoke(this, new MarketDataChangingEventArgs(done: false));

			// Lock the data while we're changing it
			//using (LockMarketData())
			//using (Positions.PreservePosition())
			//using (History.PreservePosition())
			//using (Balances.PreservePosition())
			using (Scope.Create(() => ++m_in_integrate_market_updates, () => --m_in_integrate_market_updates))
			{
				//Positions.Position = -1;
				//History.Position = -1;
				//Balances.Position = -1;

				// Pull a task from the queue and execute it
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

			// Notify market data update complete
			MarketDataChanging?.Invoke(this, new MarketDataChangingEventArgs(done: true));
		}
		private int m_in_integrate_market_updates;
	}

	#region EventArgs
	public class MarketDataChangingEventArgs : EventArgs
	{
		public MarketDataChangingEventArgs(bool done)
		{
			Done = done;
		}
		public bool Done { get; private set; }
	}
	//public class AddToUIEventArgs : EventArgs
	//{
	//	public AddToUIEventArgs(IDockable dockable, DockContainer.DockLocation dock_location = null)
	//	{
	//		Dockable = dockable;
	//		DockLocation = dock_location;
	//	}
	//	public AddToUIEventArgs(ToolStrip toolbar)
	//	{
	//		Toolbar = toolbar;
	//	}

	//	/// <summary>Dockable content</summary>
	//	public IDockable Dockable { get; private set; }
	//	public DockContainer.DockLocation DockLocation { get; private set; }

	//	/// <summary>Tool bar UI element</summary>
	//	public ToolStrip Toolbar { get; private set; }
	//}
	//public class EditTradeEventArgs : EventArgs
	//{
	//	public EditTradeEventArgs(Trade trade, ulong? existing_order_id)
	//	{
	//		Trade = trade;
	//		ExistingOrderId = existing_order_id;
	//	}

	//	/// <summary>The trade to edit/create</summary>
	//	public Trade Trade { get; private set; }

	//	/// <summary>The ID of the existing position this trade represents</summary>
	//	public ulong? ExistingOrderId { get; private set; }
	//}
	#endregion
}
