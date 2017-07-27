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
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class Model :IDisposable
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
			Settings = new Settings(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", "settings.xml"));

			UI = main_ui;
			m_dispatcher = Dispatcher.CurrentDispatcher;
			m_main_loop_exit = new ManualResetEvent(false);
			ShutdownToken  = new CancellationTokenSource();

			// Start the log
			Log = new Logger("", new LogToFile(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", "log.txt"), append:false));
			Log.TimeZero = Log.TimeZero - Log.TimeZero .TimeOfDay;
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			Coins         = new CoinDataTable(this);
			Exchanges     = new BindingSource<Exchange>     { DataSource = new BindingListEx<Exchange>() };
			Pairs         = new BindingSource<TradePair>    { DataSource = new BindingListEx<TradePair>() };
			Loops         = new BindingSource<Loop>         { DataSource = new BindingListEx<Loop>() };
			Fishing       = new BindingSource<Fishing>      { DataSource = new BindingListEx<Fishing>() };
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
			MainLoop(ShutdownToken.Token);
		}
		public virtual void Dispose()
		{
			// Main loops needs to have shutdown before here
			Debug.Assert(ShutdownToken.IsCancellationRequested);
			Debug.Assert(m_main_loop_exit.IsSignalled());
			Exchanges = null;
			Log = null;
		}

		/// <summary>The main UI</summary>
		public MainUI UI { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

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

		/// <summary>True when the set of loops needs regenerating</summary>
		public bool RebuildLoops
		{
			get { return m_rebuild_loops; }
			set
			{
				m_rebuild_loops = value;
				if (value) ++m_rebuild_loops_issue;
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
					{
						var amount = bal.Coin.NormalisedValue * bal.Total;
						worth += amount;
					}
				}
				return worth;
			}
		}

		/// <summary>Return the sum across all exchanges of the given coin</summary>
		public decimal SumOf(string sym)
		{
			var sum = 0m;
			foreach (var exch in Exchanges.Except(CrossExchange))
			{
				var coin = exch.Coins[sym];
				sum += coin?.Balance.Total ?? 0;
			}
			return sum;
		}

		/// <summary>Meta data for the known coins</summary>
		public CoinDataTable Coins { [DebuggerStepThrough] get; private set; }
		public class CoinDataTable :BindingSource<CoinData>
		{
			private readonly Model Model;
			public CoinDataTable(Model model)
			{
				Model = model;
				DataSource = new BindingListEx<CoinData>(Model.Settings.Coins.ToList());
				Model.RebuildLoops = true;
			}
			public CoinData this[string sym]
			{
				get
				{
					var idx = this.IndexOf(x => x.Symbol == sym);
					return idx >= 0 ? this[idx] : this.Add2(new CoinData(sym, 1m));
				}
			}
			protected override void OnListChanging(object sender, ListChgEventArgs<CoinData> args)
			{
				if (args.IsDataChanged)
				{
					// Record the coins in the settings
					Model.Settings.Coins = this.ToArray();

					// Flag that the COI have changed, we'll need to rebuild
					// pairs and loops.
					Model.RebuildLoops = true;
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

			RebuildLoops = true;
		
			// Update the bindings to the current exchange
			if (e.Item == Exchanges.Current)
				UpdateExchangeDetails();
		}
		private void HandleCurrentExchangeChanged(object sender = null, PositionChgEventArgs e = null)
		{
			UpdateExchangeDetails();
		}

		/// <summary>The special case cross exchange</summary>
		public CrossExchange CrossExchange { get; private set; }

		/// <summary>Debugging flag for detecting misuse of the market data</summary>
		internal bool MarketDataLocked { get; private set; }

		/// <summary>A cancellation token for graceful shutdown</summary>
		public CancellationTokenSource ShutdownToken { get; private set; }

		/// <summary>True if the Model can be closed gracefully</summary>
		public bool CanShutdown
		{
			get { return m_main_loop_exit.WaitOne(0); }
		}

		/// <summary>Async shutdown</summary>
		public Task Shutdown()
		{
			// Signal shutdown
			ShutdownToken.Cancel();

			// Wait for async methods to exit
			return Task.Run(() => m_main_loop_exit.WaitOne());
		}

		/// <summary>Application main loop
		private async void MainLoop(CancellationToken shutdown)
		{
			using (Scope.Create(null, () => m_main_loop_exit.Set()))
			{
				// Infinite loop till shutdown
				for (;;)
				{
					try
					{
						var exit = await Task.Run(() => shutdown.WaitHandle.WaitOne(Settings.MainLoopPeriod), shutdown);
						if (exit) break;

						shutdown.ThrowIfCancellationRequested();

						// When the coins of interest, available pairs, etc change, rebuild the collection of loops
						for (; RebuildLoops;)
						{
							var rebuild_loops_issue = m_rebuild_loops_issue;

							// Get each exchange to update it's available pairs/coins
							var coi = Coins.Where(x => x.OfInterest).ToHashSet(x => x.Symbol);
							await Task.WhenAll(Exchanges.Except(CrossExchange).Select(x => x.UpdatePairs(coi)));
							await CrossExchange.UpdatePairs(coi);
							Debug.Assert(AssertMainThread());

							// If the loops have been invalidated in the meantime, go round again
							if (rebuild_loops_issue != m_rebuild_loops_issue)
								continue;

							// Merge data in the main thread
							IntegrateMarketUpdates();

							// Copy the pairs from each exchange to the Model's collection
							Pairs.Clear();
							foreach (var exch in Exchanges.Where(x => x.Active))
								Pairs.AddRange(exch.Pairs.Values.Where(x => coi.Contains(x.Base) && coi.Contains(x.Quote)));

							// Update the collection of loops
							await FindLoops();

							// If the loops have been invalidated in the meantime, go round again
							if (rebuild_loops_issue != m_rebuild_loops_issue)
								continue;

							RebuildLoops = false;
						}

						// Process any pending market data updates
						IntegrateMarketUpdates();

						// Test the loops for profitability, and execute the best
						if (RunLoopFinder)
						{
							using (Scope.Create(() => MarketDataLocked = true, () => MarketDataLocked = false))
							{
								// Check the loops collection for profitable loops
								var profitable_loops = await FindProfitableLoops();

								// Execute the most profitable loop
								foreach (var loop in profitable_loops)
								{
									if (loop.TradeScale == 0)
									{
										var exchanges = string.Join(",", loop.Coins.Select(x => x.Exchange.Name).Distinct());
										var missing = string.Join(",", loop.Insufficient.Select(x => x.Coin.SymbolWithExchange));
										Log.Write(ELogLevel.Info, "Potential Loop {0} ({1}). Profit Ratio: {2:G6}. Missing Currencies: {3}".Fmt(loop.LoopDescription, exchanges, loop.ProfitRatio, missing));
										continue;
									}

									// Execute the loop
									await ExecuteLoop(loop);

									// Execute one loop only
									break;
								}
							}
						}
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
		private ManualResetEvent m_main_loop_exit;

		/// <summary>The available trade pairs (the model does not own the pairs, the exchanges do)</summary>
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
				m_balances.AllowSort = true;
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
				m_positions.AllowSort = true;
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
				m_history.AllowSort = true;
			}
		}
		private BindingSource<PositionFill> m_history;

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

		/// <summary>Fishing instances</summary>
		public BindingSource<Fishing> Fishing
		{
			get { return m_fishing; }
			private set
			{
				if (m_fishing == value) return;
				m_fishing = value;
			}
		}
		private BindingSource<Fishing> m_fishing;

		/// <summary>Pending market data updates awaiting integration at the right time</summary>
		public BlockingCollection<Action> MarketUpdates { get; private set; }

		/// <summary>Find the available trade loops</summary>
		private async Task FindLoops()
		{
			// Local copy of shared variables
			var pairs = Pairs.ToList();
			var max_loop_count = Settings.MaximumLoopCount;
			Log.Write(ELogLevel.Info, "Finding loops .....");

			// A collection of complete loops
			var result = new ConcurrentDictionary<string, Loop>();

			// Run the background process
			await Task.Run(() =>
			{
				// Create a producer/consumer queue of partial loops
				// Don't use CrossExchange pairs to start the loops to guarantee all
				// loops have at least one non-CrossExchange pair in them
				var queue = new BlockingCollection<Loop>();
				foreach (var pair in pairs.Where(x => !(x.Exchange is CrossExchange)))
					queue.Add(new Loop(pair));

				// Stop when there are no more partial loops to process
				var tasks = new List<Task>();
				for (;;)
				{
					// Take a partial loop from the queue
					if (!queue.TryTake(out var loop, TimeSpan.FromSeconds(1.0)))
					{
						// If there are no partial loops to process and
						// no running tasks we must be done.
						tasks.RemoveIf(x => x.IsCompleted);
						if (tasks.Count == 0) break;
						continue;
					}

					// If the loop is too long, ignore it
					if (loop.Coins.Count >= max_loop_count)
						continue;

					// Process the partial loop in a worker thread
					tasks.Add(Task.Run(() =>
					{
						// Look for a pair that completes or extends the loop
						var existing_pairs = loop.Pairs.ToHashSet();
						foreach (var pair in pairs.Where(x => !existing_pairs.Contains(x)))
						{
							// Does 'pair' close the loop?
							if (((pair.Base == loop.Beg && pair.Quote == loop.End) ||
								 (pair.Base == loop.End && pair.Quote == loop.Beg)) &&
								 !loop.Pairs.Contains(pair))
							{
								// Yes, save the complete loop to the results
								var l = new Loop(loop);
								l.Pairs.Add(pair);
								l.Canonicalise();
								result.TryAdd(l.LoopDescription, l);
								continue;
							}

							// Does 'pair' extend the partial loop
							var bk = false;
							var coin = (Coin)null;
							if (pair.Base  == loop.Beg && !loop.Coins.Contains(pair.Quote)) { bk = false; coin = pair.Quote; }
							if (pair.Base  == loop.End && !loop.Coins.Contains(pair.Quote)) { bk = true;  coin = pair.Quote; }
							if (pair.Quote == loop.Beg && !loop.Coins.Contains(pair.Base )) { bk = false; coin = pair.Base; }
							if (pair.Quote == loop.End && !loop.Coins.Contains(pair.Base )) { bk = true;  coin = pair.Base; }
							if (coin != null)
							{
								// Yes, add the slightly longer loop to the 'queue' collection
								var l = new Loop(loop);
								l.Pairs.Insert(bk ? l.Pairs.Count : 0, pair);
								l.Coins.Insert(bk ? l.Coins.Count : 0, coin);
								queue.Add(l);
								continue;
							}
						}
					}, ShutdownToken.Token));
				}
			}, ShutdownToken.Token);
			Debug.Assert(AssertMainThread());

			// Reduce the bag to a unique set of loops
			var loops = result.Values.ToArray();
			Loops.Clear();
			Loops.AddRange(loops);
			Log.Write(ELogLevel.Info, "{0} trading pair loops found".Fmt(result.Count));
		}

		/// <summary>Check the loops collection for profitable loops</summary>
		private async Task<List<Loop>> FindProfitableLoops()
		{
			// Find the potentially profitable loops ignoring account balances and fees.
			// Test each loop in parallel.
			var bag = new ConcurrentBag<Loop>();
			var tasks = new List<Task>();
			foreach (var loop in Loops)
			{
				loop.ProfitRatio = 0;
				//tasks.Add(Task.Run(() => AddLoopIfProfitable(loop, true , bag), ShutdownToken.Token));
				//tasks.Add(Task.Run(() => AddLoopIfProfitable(loop, false, bag), ShutdownToken.Token));

				AddLoopIfProfitable(loop, true , bag);
				AddLoopIfProfitable(loop, false, bag);
			}
			await Task.WhenAll(tasks);

			// Return the profitable loops in order of most profitable
			return bag.OrderByDescending(x => x.ProfitRatio).ToList();
		}

		/// <summary>Determine if executing trades in 'loop' should result in a profit</summary>
		private void AddLoopIfProfitable(Loop src_loop, bool forward, ConcurrentBag<Loop> profitable_loops) // Worker thread context
		{
			// How to think about this:
			// - We want to see what happens if we convert some currency to each of the coins
			//   in the loop, ending up back at the initial currency. If the result is more than
			//   we started with, then it's a profitable loop.
			// - We can go in either direction around the loop.
			// - We want to execute each trade around a profitable loop at the same time, so we're
			//   limited to the smallest balance for the coins in the loop.
			// - The rate by volume does not depend on our account balance. We calculate the effective
			//   rate at each of the offered volumes then determine if any of those volumes are profitable
			//   and whether we have enough balance for the given volumes.
			// - The 'Bid' table contains the amounts of base currency people want to buy, ordered by price.
			// - The 'Ask' table contains the amounts of base currency people want to sell, ordered by price.
			var coin = src_loop.Beg;
			var dir = forward ? +1 : -1;

			// Debugging helper
			if (src_loop.CoinsString(+1) == TrapLoopDescription ||
				src_loop.CoinsString(-1) == TrapLoopDescription)
				Debugger.Break();

			// Construct an "order book" of volumes and complete-loop prices (e.g. BTC to BTC price for each volume)
			var obk = new OrderBook(coin, coin){ new Order(1m, decimal.MaxValue._(coin.Symbol)) };
			foreach (var pair in src_loop.EnumPairs(dir))
			{
				// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
				var bal = coin.Balance;

				// Note: the trade prices are in quote currency
				if (pair.Base == coin)
					obk = MergeRates(obk, pair.B2Q, bal.Available, invert:false);
				else
					obk = MergeRates(obk, pair.Q2B, bal.Available, invert:true);

				// Get the next coin in the loop
				coin = pair.OtherCoin(coin);
			}
			if (obk.Count == 0)
				return;

			// Save the best profit ratio for this loop (as an indication)
			src_loop.ProfitRatio = Math.Max(src_loop.ProfitRatio, obk[0].Price);

			// Look for any volumes that have a nett gain
			var volume_gain = obk.Where(x => x.Price > 1).Sum(x => x.Price * x.VolumeBase);
			if (volume_gain == 0)
				return;

			// Create a copy of the loop for editing (with the direction set)
			var loop = new Loop(src_loop, obk, dir);

			// Find the maximum profitable volume to trade
			Debug.Assert(coin == loop.Beg);
			var volume = 0m._(coin);
			foreach (var ordr in loop.Rate.Where(x => x.Price > 1))
				volume += ordr.VolumeBase;

			// Calculate the effective fee in initial coin currency.
			// Do all trades assuming no fee, but accumulate the fee separately
			var fee = 0m._(coin);
			var initial_volume = volume;

			// Trade each pair in the loop (in the given direction) to check 
			// that the trade is still profitable after fees. Record each trade
			// so that we can determine the trade scale
			var trades = new List<Trade>();
			foreach (var pair in loop.EnumPairs(loop.Direction))
			{
				// If we trade 'volume' using 'pair' that will result in a new volume
				// in the new currency. There will also be a fee charged (in quote currency).
				// If we're trading to quote currency, the new volume is reduced by the fee.
				// If we're trading to base currency, the cost is increased by the fee.

				// Calculate the result of the trade
				var new_coin = pair.OtherCoin(coin);
				var trade = pair.Base == coin
					? pair.BaseToQuote(volume)
					: pair.QuoteToBase(volume);

				// Record the trade amount.
				trades.Add(trade);

				// Convert the fee so far to the new coin using the effective rate,
				// and add on the fee for this trade.
				var rate = trade.VolumeOut / trade.VolumeIn;
				fee = fee * rate + trade.VolumeOut * pair.Fee;

				// Advance to the next pair
				coin = new_coin;
				volume = trade.VolumeOut;
			}

			// Record the volume to trade, the scale, and the expected profit.
			// If the new volume is greater than the initial volume, WIN!
			// Update the profitability of the loop now we've accounted for fees.
			loop.TradeVolume = initial_volume;
			loop.Profit = (volume - fee) - initial_volume;
			loop.ProfitRatio = (volume - fee) / initial_volume;
			src_loop.ProfitRatio = loop.ProfitRatio;
			if (loop.ProfitRatio <= 1m)
				return;

			// Determine the trade scale based on the available balances
			loop.TradeScale = 1m;
			foreach (var trade in trades)
			{
				var pair = trade.Pair;

				// Get the balance available for this trade and determine a trade scaling factor.
				// Increase the required volume to allow for the fee
				// Reduce the available balance slightly to deal with rounding errors
				var bal = trade.CoinIn.Balance.Available * 0.999m;
				var req = trade.VolumeIn * (1 + pair.Fee);
				var scale = Maths.Clamp((decimal)(bal / req), 0m, 1m);
				if (scale < loop.TradeScale)
				{
					loop.TradeScale = Maths.Clamp(scale, 0, loop.TradeScale);
					loop.LimitingCoin = trade.CoinIn;
				}
			}

			// Check that all traded volumes are within the limits
			var all_trades_valid = Trade.EValidation.Valid;
			foreach (var trade in trades)
			{
				// Check the unscaled amount, if that's too small we'll ignore this loop
				all_trades_valid |= trade.Validate();

				// Check the scaled amount, if that's too small we'll display a potential loop
				var valid = new Trade(trade, loop.TradeScale).Validate();
				if (valid.HasFlag(Trade.EValidation.VolumeInOutOfRange))
					loop.Insufficient.Add(new InsufficientCoin(trade.CoinIn, "Not enough to trade"));
				if (valid.HasFlag(Trade.EValidation.VolumeOutOutOfRange))
					loop.Insufficient.Add(new InsufficientCoin(trade.CoinOut, "Trade result too small"));
			}

			// If the volume to trade, multiplied by the trade scale, is outside the allowed range of
			// trading volume, set the scale to zero. This is to prevent loops being traded where part
			// of the loop would be rejected.
			if (loop.Insufficient.Count != 0)
				loop.TradeScale = 0m;

			// Save the profitable loop (even if scaled to 0)
			if (all_trades_valid == Trade.EValidation.Valid)
				profitable_loops.Add(loop);
		}
		private static string TrapLoopDescription = string.Empty;

		/// <summary>Determine the exchange rate based on volume</summary>
		private static OrderBook MergeRates(OrderBook rates, OrderBook orders, Unit<decimal> balance, bool invert)
		{
			// 'rates' is a table of volumes in the current coin currency (i.e. the current
			// coin in the loop) along with the accumulated exchange rate for each volume.
			// 'orders' is a table of 'Base' currency volumes and the offer prices for
			// converting those volumes to 'Quote' currency.
			// If 'invert' is true, the 'orders' table is the offers for converting Quote
			// currency to Base currency, however the volumes and prices are still in Base
			// and Quote respectively.
			var new_coin = invert ? orders.Base : orders.Quote;
			var ret = new OrderBook(new_coin, new_coin);

			// Volume accumulators for the 'rates' and 'orders' order books.
			var R_vol = 0m._(rates.Base.Symbol);
			var O_vol = 0m._(rates.Base.Symbol);

			// The maximum volume available to trade is the minimum of the 'rates' and 'orders'
			// volumes, hence this loop ends when the last order in either set is reached.
			for (int r = 0, o = 0; r != rates.Count && o != orders.Count;)
			{
				var rate = rates[r];
				var ordr = orders[o];

				// Get the offer price and volume to convert to the current coin currency
				var price = invert ? 1m/ordr.Price : ordr.Price;
				var volume = invert ? ordr.Price * ordr.VolumeBase : ordr.VolumeBase;

				// Get the volume available to be traded at 'price'
				var vol0 = rate.VolumeBase < volume ? rate.VolumeBase : volume;

				// Convert this volume to the new currency using 'price'
				var vol1 = vol0 * price;

				// Record the volume and the combined rate
				ret.Add(new Order(rate.Price * price, vol1), validate:false);

				// Move to the next order in the set with the lowest accumulative volume.
				// If the same accumulative volume is in both sets, move to the next order in both sets.
				// Need to be careful with overflow, because special case values use decimal.MaxValue.
				// Only advance to the next order if the accumulative volume is less than MaxValue.
				var adv_R =
					(R_vol < decimal.MaxValue - rate.VolumeBase) && // if 'R_vol + rate.VolumeBase' does not overflow
					(R_vol - O_vol <= volume - rate.VolumeBase);    // and 'R_vol + rate.VolumeBase' <= 'O_vol + volume'
				var adv_O =
					(O_vol < decimal.MaxValue - volume) &&          // if 'O_vol + volume' does not overflow
					(R_vol - O_vol >= volume - rate.VolumeBase);    // and 'R_vol + rate.VolumeBase' >= 'O_vol + volume'
				if (adv_R) { ++r; R_vol += rate.VolumeBase; }
				if (adv_O) { ++o; O_vol += volume; }
				if (!adv_R && !adv_O)
					break;

				// Don't bother calculating for volumes that exceed the current balance
				if (rates.Count > 5 && R_vol > balance)
					break;
			}

			return ret;
		}

		/// <summary>Execute a profitable loop</summary>
		private async Task ExecuteLoop(Loop loop)
		{
			try
			{
				Log.Write(ELogLevel.Warn, "Executing Loop: {0}  Profit Ratio: {1:G6}".Fmt(loop.LoopDescription, loop.ProfitRatio));

				// Output a description of what would happen
				var sb = new StringBuilder();
				 sb.AppendLine(loop.CoinsString(loop.Direction));

				// Add notes about the scaling
				sb.Append("Trade Scale: {0:N8}".Fmt(loop.TradeScale));
				if (loop.LimitingCoin != null) sb.Append(" (due to {0})".Fmt(loop.LimitingCoin.SymbolWithExchange));
				sb.AppendLine();

				// Calculate the effective fee in initial coin currency.
				// Do all trades assuming no fee, but accumulate the fee separately
				var coin = loop.Beg;
				var fee = 0m._(coin);
				var volume = loop.TradeVolume * loop.TradeScale;
				var initial_volume = volume;

				// Trade each pair in the loop (in the given direction).
				var tasks = new List<Task>();
				foreach (var pair in loop.EnumPairs(loop.Direction))
				{
					var new_coin = pair.OtherCoin(coin);
					var new_volume = 0m._(new_coin);
					if (pair.Base == coin)
					{
						var trade = pair.BaseToQuote(volume);
						tasks.Add(trade.CreateOrder());
						new_volume = trade.VolumeOut;

						// Trade 'coin' to 'Quote'
						sb.AppendLine("   Trade {0} {1} => {2} {3} @ {4}".Fmt(volume.ToString("G6"), coin, new_volume.ToString("G6"), new_coin, trade.Price.ToString("G6")));
					}
					else
					{
						var trade = pair.QuoteToBase(volume);
						tasks.Add(trade.CreateOrder());
						new_volume = trade.VolumeOut;

						// Trade 'coin' to 'Base'
						sb.AppendLine("   Trade {0} {1} => {2} {3} @ {4}".Fmt(volume.ToString("G6"), coin, new_volume.ToString("G6"), new_coin, trade.Price.ToString("G6")));
					}

					// Convert the fee to the new coin using the effective rate, and add on the fee
					var rate = new_volume / volume;
					fee = fee * rate + new_volume * pair.Fee;

					coin = new_coin;
					volume = new_volume;
				}

				// Return the nett profit
				var gross = volume - initial_volume;
				sb.AppendLine(" Gross: {0} {1}.".Fmt(gross.ToString("G6"), coin));
				sb.AppendLine(" Fee:   {0} {1}.".Fmt((-fee).ToString("G6"), coin));
				sb.AppendLine(" Nett:  {0} {1}.".Fmt((gross - fee).ToString("G6"), coin));

				// Wait for all orders to complete
				await Task.WhenAll(tasks);

				// Log the trades
				Log.Write(ELogLevel.Info, sb.ToString());

//HACK
AllowTrades = false;
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Trade Loop aborted.");
				AllowTrades = false;
				RunLoopFinder = false;
			}
		}

		/// <summary>Process any pending market data updates</summary>
		private void IntegrateMarketUpdates()
		{
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



#if false
		/// <summary>Determine if executing trades in 'loop' should result in a profit</summary>
		private Task AddLoopIfProfitable(Loop loop, ConcurrentBag<Loop> profitable_loops)
		{
			return Task.Run(() =>
			{
				// How to think about this:
				// - We want to see what happens if we convert some currency to each of the coins
				//   in the loop, ending up back at the initial currency. If the result is more than
				//   we started with, then it's a profitable loop.
				// - We can go in either direction around the loop.
				// - We want to execute each trade around a profitable loop at the same time, so we're
				//   limited to the smallest balance for the coins in the loop.
				// - The rate by volume does not depend on our account balance. We calculate the effective
				//   rate at each of the offered volumes then determine any of those volumes are profitable
				//   and whether we have enough balance for the given volumes
				// - The 'Bid' table contains the amounts of base currency people want to buy, ordered by price.
				// - The 'Ask' table contains the amounts of base currency people want to sell, ordered by price.
				var coin = loop.Beg;
				loop.ProfitRatio = 0;

				// Forward pass around the loop.
				{
					var fwd = new OrderBook(coin, coin);
					fwd.Add(new Order(1m._(""), decimal.MaxValue._(coin.Symbol)));
					foreach (var pair in loop.EnumPairs(+1))
					{
						// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
						var bal = coin.Balance;

						// Note: the trade prices are in quote currency
						if (pair.Base == coin)
							fwd = MergeRates(fwd, pair.Bid, bal.Available, invert:false);
						else
							fwd = MergeRates(fwd, pair.Ask, bal.Available, invert:true);

						// Get the next coin in the loop
						coin = pair.OtherCoin(coin);
					}
					loop.ProfitRatio = fwd.Count != 0 ? Math.Max(loop.ProfitRatio, fwd[0].Price) : 0;

					// Test for profitability
					var fwd_profit = fwd.Where(x => x.Price > 1).Sum(x => x.Price * x.VolumeBase);
					if (fwd_profit != 0)
						profitable_loops.Add(new Loop(loop, fwd, +1, fwd_profit));
				}

				coin = loop.Beg;

				// Backward pass around the loop.
				{
					var bck = new OrderBook(coin, coin);
					bck.Add(new Order(1m._(""),  decimal.MaxValue._(coin.Symbol)));
					foreach (var pair in loop.EnumPairs(-1))
					{
						// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
						var bal = coin.Balance;

						// Note: the trade prices are in quote currency
						if (pair.Base == coin)
							bck = MergeRates(bck, pair.Bid, bal.Available, invert:false);
						else
							bck = MergeRates(bck, pair.Ask, bal.Available, invert:true);

						// Get the next coin in the loop
						coin = pair.OtherCoin(coin);
					}
					loop.ProfitRatio = bck.Count != 0 ? Math.Max(loop.ProfitRatio, bck[0].Price) : 0;

					// Test for profitability
					var bck_profit = bck.Where(x => x.Price > 1).Sum(x => x.Price * x.VolumeBase);
					if (bck_profit != 0)
						profitable_loops.Add(new Loop(loop, bck, -1, bck_profit));
				}
			});
		}
#endif

#if false
			// Simulate each loop based on current balances and fees to determine actual profitability
			var profitable = new List<Loop>();
			foreach (var loop in bag.ToArray())
			{
				var coin = loop.Beg;

				// Find the maximum profitable volume to trade
				var volume = 0m._(coin);
				foreach (var ordr in loop.Rate.Where(x => x.Price > 1))
					volume += ordr.VolumeBase;

				// Initialise the maximum profitable trade volume and scaling
				// factor that allows for the available balance.
				loop.TradeVolume = volume;
				loop.TradeScale = 1m;

				// Calculate the effective fee in initial coin currency.
				// Do all trades assuming no fee, but accumulate the fee separately
				var fee = 0m._(coin);
				var initial_volume = volume;

				// Trade each pair in the loop (in the given direction) to check we have enough
				// balance for each trade, and that the trade is still profitable after fees.
				foreach (var pair in loop.EnumPairs(loop.Direction))
				{
					// If we trade 'volume' using 'pair' that will result in a new volume
					// in the new currency. There will also be a fee charged (in quote currency).
					// If we're trading to quote currency, the new volume is reduced by the fee.
					// If we're trading to base currency, the cost is increased by the fee.

					// Calculate the result of the trade
					var new_coin = pair.OtherCoin(coin);
					var converted = pair.Base == coin
						? pair.BaseToQuote(volume)
						: pair.QuoteToBase(volume);

					// Get the balance available for this trade and determine a trade scaling factor.
					// Reduce the available balance by the fee so that we know there's enough balance for the trade.
					var bal = coin.Balance.Available * (1 - pair.Fee);
					if (bal < volume)
					{
						// Scale the trade volume
						var scale = (decimal)(bal / volume);
						loop.TradeScale = Maths.Clamp(scale, 0, loop.TradeScale);

						// If the volume to trade multiplied by the trade scale is outside the
						// allowed range of trading volume, set the scale to zero. This is to
						// prevent loops being traded where part of the loop would be rejected.
						if (pair.Base == coin)
						{
							if (!(pair.VolumeRangeBase?.Contains(volume * scale) ?? true))
							{
								loop.Insufficient.Add(new InsufficientCoin(coin, 0));
								loop.TradeScale = 0m;
							}
							if (!(pair.VolumeRangeQuote?.Contains(converted.VolumeBase * scale) ?? true))
							{
								loop.Insufficient.Add(new InsufficientCoin(new_coin, 0));
								loop.TradeScale = 0m;
							}
						}
						else
						{
							if (!(pair.VolumeRangeQuote?.Contains(volume * scale) ?? true))
							{
								loop.Insufficient.Add(new InsufficientCoin(new_coin, 0));
								loop.TradeScale = 0m;
							}
							if (!(pair.VolumeRangeBase?.Contains(converted.VolumeBase * scale) ?? true))
							{
								loop.Insufficient.Add(new InsufficientCoin(coin, 0));
								loop.TradeScale = 0m;
							}
						}

						// Apply the price limits. I don't really expect this to ever be hit because we're trading
						// existing offers that must have valid prices already.
						if (!(pair.PriceRange?.Contains(converted.Price) ?? true))
							loop.TradeScale = 0m;
					}

					// Convert the fee to the new coin using the effective rate, and add on the fee
					var rate = converted.VolumeBase / volume;
					fee = fee * rate + converted.VolumeBase * pair.Fee;

					coin = new_coin;
					volume = converted.VolumeBase;
				}

				// If the new volume is greater than the initial volume, WIN!
				// Update the profitability of the loop now we've accounted for fees.
				loop.Profit = (volume - fee) - initial_volume;
				loop.ProfitRatio = (volume - fee) / initial_volume;
				if (loop.ProfitRatio > 1m)
					profitable.Add(loop);
			}

			// Sort the loops by profitability by available balances
			profitable.Sort(new Comparison<Loop>((l,r) => -l.ProfitRatio.CompareTo(r.ProfitRatio)));

			return profitable;
#endif
