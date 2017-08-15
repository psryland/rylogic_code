using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public partial class Model :IDisposable, IShutdownAsync
	{
		// Notes:
		//  - The model builds the loops collection whenever new pairs are added.
		//  - Each exchange updates its data independently.
		//  - Each exchange owns the coins and pairs it provides.
		//  - The CrossExchange is a special exchange that links coins of the same currency
		//    between exchanges.
		//
		// The Model main loop (heartbeat) does the following:
		//    If the coins of interest have changed:
		//        For each exchange (in parallel):
		//            Update pairs, coins
		//        Copy pairs for COI to Model
		//        Update collection of Loops
		//    For each loop (in parallel):
		//        Test for profitability (pairs, coins, balances in the Model are read only)
		//    Execute most profitable loop (if any)
		//
		// Exchange main loop (heartbeat):
		//    Query server
		//    Collect data in staging buffers
		//    lock exchange
		//    Copy staged data to Exchange data
		//

		public Model(MainUI main_ui)
		{
			// Ensure the app data directory exists
			Directory.CreateDirectory(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip"));
			Settings = new Settings(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", Util.IsDebug ? "settings_debug.xml" : "settings.xml"));

			UI = main_ui;
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_main_loop_step = new AutoResetEvent(false);
			m_shutdown_token_source = new CancellationTokenSource();

			// Start the log
			Log = new Logger("", new LogToFile(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", Util.IsDebug ? "log_debug.txt" : "log.txt"), append:false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero .TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			Coins         = new CoinDataTable(this);
			Exchanges     = new BindingSource<Exchange>     { DataSource = new BindingListEx<Exchange>() };
			Pairs         = new BindingSource<TradePair>    { DataSource = new BindingListEx<TradePair>() };
			Loops         = new BindingSource<Loop>         { DataSource = new BindingListEx<Loop>() };
			Fishing       = new BindingSource<Fishing>      { DataSource = new BindingListEx<Fishing>(Settings.Fishing.Select(x => new Fishing(this, x)).ToList()) };
			Balances      = new BindingSource<Balance>      { DataSource = null, AllowNoCurrent = true };
			Positions     = new BindingSource<Position>     { DataSource = null, AllowNoCurrent = true };
			History       = new BindingSource<PositionFill> { DataSource = null, AllowNoCurrent = true };
			MarketUpdates = new BlockingCollection<Action>();
	
			// Add exchanges
			//Exchanges.Add(new TestExchange(this));
			Exchanges.Add(new Poloniex(this));
			Exchanges.Add(new Bittrex(this));
			Exchanges.Add(new Cryptopia(this));
			Exchanges.Add(CrossExchange = new CrossExchange(this));

			RunLoopFinder = false;
			AllowTrades = false;

			// Run the async main loop
			MainLoop(ShutdownToken);
		}
		public virtual void Dispose()
		{
			// Main loops needs to have shutdown before here
			Debug.Assert(ShutdownToken.IsCancellationRequested);
			Debug.Assert(!Running);
			Loops = null;
			Fishing = null;
			Positions = null;
			History = null;
			Balances = null;
			Pairs = null;
			Exchanges = null;
			Log = null;
		}

		/// <summary>Async shutdown</summary>
		public async Task ShutdownAsync()
		{
			// Signal shutdown
			m_shutdown_token_source.Cancel();

			// Shutdown fishing instances
			await Task.WhenAll(Fishing.Select(x => x.ShutdownAsync()));

			// Wait for async methods to exit
			await Task_.WaitWhile(() => Running);
		}

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationToken ShutdownToken
		{
			get { return m_shutdown_token_source.Token; }
		}
		private CancellationTokenSource m_shutdown_token_source;

		/// <summary>Application main loop
		private async void MainLoop(CancellationToken shutdown)
		{
			using (Scope.Create(() => ++m_main_loop_running, () => --m_main_loop_running))
			{
				// Infinite loop till shutdown
				for (;;)
				{
					try
					{
						await Task.Run(() => m_main_loop_step.WaitOne(Settings.MainLoopPeriod), shutdown);
						if (shutdown.IsCancellationRequested) break;

						// Update the trading pairs
						await UpdatePairsAsync(shutdown);

						// Process any pending market data updates
						IntegrateMarketUpdates();

						// Do trading tasks
						using (Scope.Create(() => MarketDataLocked = true, () => MarketDataLocked = false))
						{
							// Test the loops for profitability, and execute the best
							if (RunLoopFinder)
								await ExecuteProfitableLoops(shutdown);
						}

						// Simulate fake orders being filled
						if (!AllowTrades)
							await SimulateFakeOrders();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { break; }
						else Log.Write(ELogLevel.Error, ex, "Error during main loop.");
					}
				}
			}
		}
		private AutoResetEvent m_main_loop_step;
		private int m_main_loop_running;

		/// <summary>True if the Model can be closed gracefully</summary>
		public bool Running
		{
			get { return m_main_loop_running != 0; }
		}

		/// <summary>The main UI</summary>
		public MainUI UI { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

		/// <summary>The current time (in UTC).</summary>
		public DateTimeOffset UtcNow
		{
			get { return DateTimeOffset.UtcNow; } // Might use this for back-testing one day
		}

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

		/// <summary>True when pairs need updating</summary>
		public bool UpdatePairs
		{
			get { return m_update_pairs; }
			set
			{
				m_update_pairs = value;
				if (m_update_pairs)
				{
					++m_update_pairs_issue;
					RebuildLoops = true;
					m_main_loop_step.Set();
				}
			}
		}
		private bool m_update_pairs;
		private int m_update_pairs_issue;

		/// <summary>True when the set of loops needs regenerating</summary>
		public bool RebuildLoops
		{
			get { return m_rebuild_loops; }
			set
			{
				m_rebuild_loops = value;
				if (m_rebuild_loops)
				{
					++m_rebuild_loops_issue;
					m_main_loop_step.Set();
				}
			}
		}
		private bool m_rebuild_loops;
		private int m_rebuild_loops_issue;

		/// <summary>A global switch to control actually placing orders</summary>
		public bool AllowTrades
		{
			get { return m_allow_trades; }
			set
			{
				if (m_allow_trades == value) return;
				m_allow_trades = value;
				ResetFakeCash();
				AllowTradesChanged.Raise(this);
			}
		}
		public event EventHandler AllowTradesChanged;
		private bool m_allow_trades;

		/// <summary>Start/Stop finding loops</summary>
		public bool RunLoopFinder
		{
			get { return m_run; }
			set
			{
				if (m_run == value) return;
				m_run = value;
				RunChanged.Raise(this);
			}
		}
		public event EventHandler RunChanged;
		private bool m_run;

		/// <summary>Return the sum of all balances, weighted by their values</summary>
		public decimal NettWorth
		{
			get
			{
				var worth = 0m;
				foreach (var exch in Exchanges.Except(CrossExchange))
				{
					foreach (var bal in exch.Balance.Values)
						worth += bal.Coin.Value(bal.Total);
				}
				return worth;
			}
		}

		/// <summary>Return the sum of all tokens</summary>
		public decimal TokenTotal
		{
			get
			{
				var sum = 0m;
				foreach (var exch in Exchanges.Except(CrossExchange))
				{
					foreach (var bal in exch.Balance.Values)
					{
						var amount = bal.Total;
						sum += amount;
					}
				}
				return sum;
			}
		}

		/// <summary>Return the sum across all exchanges of the total coin 'sym'</summary>
		public decimal SumOfTotal(string sym)
		{
			var sum = 0m;
			foreach (var exch in Exchanges.Except(CrossExchange))
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Total ?? 0;
			}
			return sum;
		}

		/// <summary>Return the sum across all exchanges of the available coin 'sym'</summary>
		public decimal SumOfAvailable(string sym)
		{
			var sum = 0m;
			foreach (var exch in Exchanges.Except(CrossExchange))
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Available ?? 0;
			}
			return sum;
		}

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange { get; private set; }

		/// <summary>Debugging flag for detecting misuse of the market data</summary>
		internal bool MarketDataLocked { get; private set; }

		/// <summary>Meta data for the known coins</summary>
		public CoinDataTable Coins { [DebuggerStepThrough] get; private set; }
		public class CoinDataTable :BindingSource<Settings.CoinData>
		{
			private readonly Model Model;
			public CoinDataTable(Model model)
			{
				Model = model;
				DataSource = new BindingListEx<Settings.CoinData>(Model.Settings.Coins.ToList());
				Model.UpdatePairs = true;
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

					// Flag that the COI have changed, we'll need to update pairs.
					Model.UpdatePairs = true;
				}
				base.OnListChanging(sender, args);
			}
		}

		/// <summary>The exchanges</summary>
		public BindingSource<Exchange> Exchanges
		{
			[DebuggerStepThrough] get { return m_exchanges; }
			private set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.ListChanging -= HandleExchangesListChanging;
					m_exchanges.PositionChanged -= HandleCurrentExchangeChanged;
					Util.DisposeAll(ref m_exchanges);
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.PositionChanged += HandleCurrentExchangeChanged;
					m_exchanges.ListChanging += HandleExchangesListChanging;
				}
			}
		}
		private BindingSource<Exchange> m_exchanges;
		private void HandleExchangesListChanging(object sender, ListChgEventArgs<Exchange> e)
		{
			if (e.ChangeType == ListChg.ItemAdded)
				Log.Write(ELogLevel.Info, "Exchange Added: {0}".Fmt(e.Item.Name));
			if (e.ChangeType == ListChg.ItemRemoved)
				Log.Write(ELogLevel.Info, "Exchange Removed: {0}".Fmt(e.Item.Name));

			UpdatePairs = true;
		
			// Update the bindings to the current exchange
			if (e.Item == Exchanges.Current)
				UpdateExchangeDetails();
		}
		private void HandleCurrentExchangeChanged(object sender = null, PositionChgEventArgs e = null)
		{
			UpdateExchangeDetails();
		}

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
			}
		}
		private BindingSource<TradePair> m_pairs;
		private void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
		{
			// Ensure the list is unique
			Debug.Assert(e.ChangeType != ListChg.ItemAdded || Pairs.Count(x => x == e.Item) == 1);
		}

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
				m_history = value;
				if (m_history != null)
				{
					m_history.AllowSort = true;
				}
			}
		}
		private BindingSource<PositionFill> m_history;

		/// <summary>Fishing instances</summary>
		public BindingSource<Fishing> Fishing
		{
			get { return m_fishing; }
			private set
			{
				if (m_fishing == value) return;
				if (m_fishing != null)
				{
					m_fishing.ListChanging -= HandleFishingListChanging;
					Util.DisposeAll(m_fishing);
				}
				m_fishing = value;
				if (m_fishing != null)
				{
					m_fishing.ListChanging += HandleFishingListChanging;
				}
			}
		}
		private BindingSource<Fishing> m_fishing;
		private void HandleFishingListChanging(object sender, ListChgEventArgs<Fishing> e)
		{
			if (e.IsDataChanged)
				Settings.Fishing = Fishing.Select(x => x.Settings).ToArray();
		}

		/// <summary>Trade pair loops</summary>
		public BindingSource<Loop> Loops
		{
			get { return m_loops; }
			private set
			{
				if (m_loops == value) return;
				m_loops = value;
			}
		}
		private BindingSource<Loop> m_loops;

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public BlockingCollection<Action> MarketUpdates { get; private set; }

		/// <summary>Process any pending market data updates</summary>
		private void IntegrateMarketUpdates()
		{
			Debug.Assert(AssertMainThread());

			// Notify market data updating
			MarketDataChanging.Raise(this, new MarketDataChangingEventArgs(done:false));

			using (Positions.PreservePosition())
			using (History  .PreservePosition())
			using (Balances .PreservePosition())
			{
				Positions.Position = -1;
				History  .Position = -1;
				Balances .Position = -1;
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

		/// <summary>Raised when market data changes</summary>
		public event EventHandler<MarketDataChangingEventArgs> MarketDataChanging;

		/// <summary>Update the collections of pairs</summary>
		public async Task UpdatePairsAsync(CancellationToken shutdown)
		{
			var sw = new Stopwatch().Start2();
			for (; UpdatePairs;)
			{
				Log.Write(ELogLevel.Info, "Updating pairs ...");
				var update_pairs_issue = m_update_pairs_issue;
				if (shutdown.IsCancellationRequested) return;

				// Get the coins of interest
				var coi = Coins.Where(x => x.OfInterest).ToHashSet(x => x.Symbol);

				// Get each exchange to update it's available pairs/coins
				await Task.WhenAll(Exchanges.Except(CrossExchange).Where(x => x.Active).Select(x => x.UpdatePairs(coi)));
				if (CrossExchange.Active) await CrossExchange.UpdatePairs(coi);
				IntegrateMarketUpdates();

				// If the pairs have been invalidated in the meantime, give up
				if (update_pairs_issue != m_update_pairs_issue || shutdown.IsCancellationRequested)
					continue;

				// Copy the pairs from each exchange to the Model's collection
				Pairs.Clear();
				foreach (var exch in Exchanges.Where(x => x.Active))
					Pairs.AddRange(exch.Pairs.Values.Where(x => coi.Contains(x.Base) && coi.Contains(x.Quote)));

				// Clear the dirty flag
				UpdatePairs = false;
				Log.Write(ELogLevel.Info, $"Trading pairs updated ... (Taking {sw.Elapsed.TotalSeconds} seconds)");
			}
		}

		/// <summary>Look for fake orders that would be filled by the current price levels</summary>
		private async Task SimulateFakeOrders()
		{
			foreach (var exch in Exchanges)
			{
				foreach (var pos in exch.Positions.Values.Where(x => x.Fake).ToArray())
				{
					if (pos.TradeType == ETradeType.B2Q && pos.Pair.QuoteToBase(pos.VolumeQuote).PriceQ2B > pos.Price * 1.00000000000001m)
						await pos.FillFakeOrder();
					if (pos.TradeType == ETradeType.Q2B && pos.Pair.BaseToQuote(pos.VolumeBase).PriceQ2B < pos.Price * 0.999999999999999m)
						await pos.FillFakeOrder();
				}
			}
		}

		/// <summary>Remove the fake cash from all exchange balances</summary>
		private void ResetFakeCash()
		{
			foreach (var bal in Exchanges.SelectMany(x => x.Balance.Values))
				bal.FakeCash.Clear();
		}

		/// <summary>Find the maximum price for the given currency on the available exchanges</summary>
		public decimal MaxLiveValue(string sym)
		{
			var value = 0m._(sym);
			foreach (var exch in Exchanges.Except(CrossExchange))
			{
				var coin = exch.Coins[sym];
				if (coin == null) continue;
				value = Math.Max(value, coin.Value(1m._(sym)));
			}
			return value;
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

		/// <summary>Assert that it is valid to read market data in the current thread</summary>
		public bool AssertMarketDataRead()
		{
			if (Thread.CurrentThread.ManagedThreadId == m_dispatcher.Thread.ManagedThreadId) return true;
			if (MarketDataLocked) return true;
			throw new Exception("Invalid access to market data");
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
		public async void Test()
		{
			try
			{
				await Misc.CompletedTask;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Test Failed");
			}
		}
	}

	#region EventArgs
	public class MarketDataChangingEventArgs :EventArgs
	{
		public MarketDataChangingEventArgs(bool done)
		{
			Done = done;
		}
		public bool Done { get; private set; }
	}
	#endregion
}