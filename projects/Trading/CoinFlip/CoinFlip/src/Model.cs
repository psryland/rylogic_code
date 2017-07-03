using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
using Timer = System.Windows.Forms.Timer;

namespace CoinFlip
{
	public class Model :IDisposable
	{
		// Notes:
		//  - The model builds the loops collection whenever new pairs are added
		//  - Exchanges update their coins and pairs when the coins of interest change.
		//  - Each exchange contains the coins and pairs it provides.
		//  - Each exchange updates its data independently.
		//  - Exchanges register their pairs with the Model
		//  - The CrossExchange is a special exchange that links coins of the same currency
		//    between exchanges. It automatically updates 

		public Model(MainUI main_ui)
		{
			// Ensure the app data directory exists
			Directory.CreateDirectory(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip"));

			UI = main_ui;
			Settings = new Settings(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", "settings.xml"));
			m_dispatcher = Dispatcher.CurrentDispatcher;

			// Start the log
			Log = new Logger("", new LogToFile(Util.ResolveUserDocumentsPath("Rylogic", "CoinFlip", "log.txt"), append:false));
			Log.Write(ELogLevel.Debug, "<<< Started >>>");

			CoinsOfInterest = new BindingSource<string> { DataSource = new BindingListEx<string>() };
			Exchanges       = new BindingSource<Exchange> { DataSource = new BindingListEx<Exchange>() };
			Pairs           = new BindingSource<TradePair> { DataSource = new BindingListEx<TradePair>() };
			Balances        = new BindingSource<Balance> { DataSource = null };
			Positions       = new BindingSource<Position> { DataSource = null };
			Loops           = new BindingSource<Loop> { DataSource = new BindingListEx<Loop>() };
			Heart           = new Timer();
	
			// Add exchanges
			//Exchanges.Add(new TestExchange(this));
			Exchanges.Add(new Cryptopia(this));
			Exchanges.Add(new Poloniex(this));
			Exchanges.Add(new CrossExchange(this));

			Run = false;
			AllowTrades = false;

			// Start each exchange
			foreach (var exch in Exchanges)
				exch.Start();
		}
		public virtual void Dispose()
		{
			Heart = null;
		}

		/// <summary>The main UI</summary>
		public MainUI UI { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings { get; private set; }

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

			// Update the loops on exchanges changed.
			// Run 'FindLoops' after all handlers of list changing have been processed
			if (e.IsDataChanged)
				RunOnGuiThread(FindLoops);

			// Update the bindings to the current exchange
			if (e.Item == Exchanges.Current)
				UpdateExchangeDetails();
		}
		private void HandleCurrentExchangeChanged(object sender = null, PositionChgEventArgs e = null)
		{
			UpdateExchangeDetails();
		}

		/// <summary>The coins to search</summary>
		public BindingSource<string> CoinsOfInterest
		{
			[DebuggerStepThrough] get { return m_coins_of_interest; }
			private set
			{
				if (m_coins_of_interest == value) return;
				if (m_coins_of_interest != null)
				{
					m_coins_of_interest.ListChanging -= HandleCOIChanging;
				}
				m_coins_of_interest = value;
				if (m_coins_of_interest != null)
				{
					// Set the coin types to consider for trading
					foreach (var coi in Settings.CoinsOfInterest)
						m_coins_of_interest.Add(coi);

					m_coins_of_interest.ListChanging += HandleCOIChanging;
				}
			}
		}
		public HashSet<string> CoinsOfInterestSet
		{
			get { return m_coins_of_interest_set ?? (m_coins_of_interest_set = CoinsOfInterest.ToHashSet()); }
			private set { m_coins_of_interest_set = null; }
		}
		private BindingSource<string> m_coins_of_interest;
		private HashSet<string> m_coins_of_interest_set;
		private void HandleCOIChanging(object sender, ListChgEventArgs<string> e)
		{
			if (e.IsDataChanged)
			{
				// Record the new coins of interest in the settings
				Settings.CoinsOfInterest = CoinsOfInterest.ToArray();

				// Invalidate the cached hash set
				CoinsOfInterestSet = null;

				// Remove pairs that no longer involve the coins of interest.
				// Exchanges will add new pairs that are of interest
				var coi = CoinsOfInterestSet;
				Pairs.RemoveIf(x => !coi.Contains(x.Base) || !coi.Contains(x.Quote));

				// Update exchange details
				UpdateExchangeDetails();
			}
		}

		/// <summary>The available trade pairs (the model does not own the pairs, the exchanges do)</summary>
		public BindingSource<TradePair> Pairs
		{
			get { return m_pairs; }
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

			// Update the loops on trade-pairs changed
			// Run 'FindLoops' after all handlers of list changing have been processed
			if (e.IsDataChanged)
				RunOnGuiThread(FindLoops);
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
				m_balances.PositionChanged += (s,a) =>
				{
					var i = a.NewIndex;
				};
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

		/// <summary>Start/Stop finding loops</summary>
		public bool Run
		{
			get { return m_run; }
			set
			{
				if (m_run == value) return;
				m_run = value;
				FindLoops();
			}
		}
		private bool m_run;

		/// <summary>A global switch to control actually placing orders</summary>
		public bool AllowTrades { get; set; }

		/// <summary>Heart beat</summary>
		private Timer Heart
		{
			get { return m_heart; }
			set
			{
				if (m_heart == value) return;
				if (m_heart != null)
				{
					m_heart.Tick -= HandleHeartBeatTick;
					Util.Dispose(ref m_heart);
				}
				m_heart = value;
				if (m_heart != null)
				{
					m_heart.Enabled = true;
					m_heart.Tick += HandleHeartBeatTick;
					m_heart.Interval = Settings.FindProfitableLoopsPeriod;
				}
			}
		}
		private Timer m_heart;
		private async void HandleHeartBeatTick(object sender = null, EventArgs args = null)
		{
			try
			{
				if (Run)
				{
					// Check the loops collection for profitable loops
					var profitable_loops = await FindProfitableLoops();

					// Execute the most profitable loop
					foreach (var loop in profitable_loops.Take(1))
						ExecuteLoop(loop);
				}

				// Notify heart beat
				HeartBeat.Raise(this);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Step failed");
			}
		}

		/// <summary>Raised on each heart beat</summary>
		public event EventHandler HeartBeat;

		/// <summary>Find the available trade loops</summary>
		private void FindLoops()
		{
			// The trading pairs are the edges in an undirected graph.
			// We want to identify all of the cycles in the graph. This
			// can take a while, so do it in a worker thread.
			var max_loop_count = Settings.MaximumLoopCount;
			var pairs = Pairs.ToList();
			Debug.Assert(pairs.Distinct().Count() == pairs.Count);

			// A worker thread for updating the loops collection
			ThreadPool.QueueUserWorkItem(x =>
			{
				var issue = (int)x;

				// Use two buffers, one buffer contains the partial loops
				// from the previous round, the other collects the partial
				// (but bigger) loops for the next round.
				var loops0 = new List<Loop>();
				var loops1 = new List<Loop>();
				var result = new List<Loop>();

				// Start each loop with a single edge, one for each trading pair
				for (var i = 0; i != pairs.Count; ++i)
					loops0.Add(new Loop(pairs[i], i));

				// Find closed loops within the graph, starting with the smallest
				// and building up to the largest.
				for (var k = 0;; ++k)
				{
					if (issue != m_find_loops_issue) return;
					var prev = (k%2) == 0 ? loops0 : loops1;
					var next = (k%2) == 0 ? loops1 : loops0;

					// Quit when there are no more partial loops
					// or the maximum loop length is exceeded.
					if (prev.Count == 0 || k == max_loop_count) break;
					next.Clear();

					// Extend the partial loops
					for (var j = 0; j != prev.Count; ++j)
					{
						if (issue != m_find_loops_issue) return;
						var loop = prev[j];

						// Starting from the next pair, see if it can be appended to
						// the partial loop. If 'pair' closes the loop, add it to the
						// loops to be tested for profit.
						for (var i = loop.LastPairIndex + 1; i < pairs.Count; ++i)
						{
							if (issue != m_find_loops_issue) return;
							var pair = pairs[i];

							// Does 'pair' close the loop?
							if ((pair.Base == loop.Beg && pair.Quote == loop.End) ||
								(pair.Base == loop.End && pair.Quote == loop.Beg))
							{
								// Yes, save the complete loop to the results
								var l = new Loop(loop);
								l.Pairs.Add(pair);
								l.LastPairIndex = i;
								Debug.Assert(l.Coins.Count >= 3);
								result.Add(l);
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
								// Yes, add the slightly longer loop to the 'next' collection
								var l = new Loop(loop);
								l.Pairs.Insert(bk ? l.Pairs.Count : 0, pair);
								l.Coins.Insert(bk ? l.Coins.Count : 0, coin);
								l.LastPairIndex = i;
								next.Add(l);
								continue;
							}
						}
					}
				}

				// Marshal the results back to the GUI thread
				if (issue != m_find_loops_issue) return;
				RunOnGuiThread(() =>
				{
					if (issue != m_find_loops_issue) return;

					// Update the loops collection
					Loops.Clear();
					Loops.AddRange(result);
					Log.Write(ELogLevel.Info, "{0} trading pair loops found".Fmt(result.Count));
				});
			}, ++m_find_loops_issue);
		}
		private int m_find_loops_issue;

		/// <summary>Check the loops collection for profitable loops</summary>
		private async Task<List<Loop>> FindProfitableLoops()
		{
			// Find the potentially profitable loops ignoring account balances and fees.
			// Test each loop in parallel.
			var bag = new ConcurrentBag<Loop>();
			await Task.WhenAll(Loops.Select(x => AddLoopIfProfitable(x, bag)));
			var potentials = bag.ToArray();

			// Simulate each loop based on current balances and fees to determine actual profitability
			var profitable = new List<Loop>();
			foreach (var loop in potentials)
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

					// Get the balance available for this trade and determine a trade scaling factor.
					// Reduce the available balance by the fee so that we know there's enough balance for the trade
					var bal = coin.Exchange.Balance[coin].Available * (1 - pair.Fee);
					if (bal < volume)
						loop.TradeScale = Maths.Clamp(bal / volume, 0, loop.TradeScale);

					// Calculate the result of the trade
					var new_coin = pair.OtherCoin(coin);
					var converted = pair.Base == coin
						? pair.BaseToQuote(volume)
						: pair.QuoteToBase(volume);

					// Convert the fee to the new coin using the effective rate, and add on the fee
					var rate = converted.VolumeBase / volume;
					fee = fee * rate + converted.VolumeBase * pair.Fee;

					coin = new_coin;
					volume = converted.VolumeBase;
				}

				// If the new volume is greater than the initial volume, WIN!
				// Update the profitability of the loop now we've accounted for fees.
				loop.Profitability = volume - initial_volume - fee;
				if (loop.Profitability > 0)
					profitable.Add(loop);
			}

			// Sort the loops by profitability
			profitable.Sort(new Comparison<Loop>((l,r) => -l.Profitability.CompareTo(r.Profitability)));

			return profitable;
		}

		/// <summary>Determine if executing trades in 'loop' should result in a profit</summary>
		private async Task AddLoopIfProfitable(Loop loop, ConcurrentBag<Loop> profitable_loops)
		{
			await Task.Run(() =>
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

				// Forward pass around the loop.
				{
					var fwd = new OrderBook(coin, coin);
					fwd.Add(new Order(1m._(""), decimal.MaxValue._(coin.Symbol)));
					foreach (var pair in loop.EnumPairs(+1))
					{
						// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
						var bal = coin.Exchange.Balance[coin];

						// Note: the trade prices are in quote currency
						if (pair.Base == coin)
							fwd = MergeRates(fwd, pair.Bid, bal.Available, invert:false);
						else
							fwd = MergeRates(fwd, pair.Ask, bal.Available, invert:true);

						// Get the next coin in the loop
						coin = pair.OtherCoin(coin);
					}

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
						var bal = coin.Exchange.Balance[coin];

						// Note: the trade prices are in quote currency
						if (pair.Base == coin)
							bck = MergeRates(bck, pair.Bid, bal.Available, invert:false);
						else
							bck = MergeRates(bck, pair.Ask, bal.Available, invert:true);

						// Get the next coin in the loop
						coin = pair.OtherCoin(coin);
					}

					// Test for profitability
					var bck_profit = bck.Where(x => x.Price > 1).Sum(x => x.Price * x.VolumeBase);
					if (bck_profit != 0)
						profitable_loops.Add(new Loop(loop, bck, -1, bck_profit));
				}
			});
		}

		/// <summary>Determine the exchange rate based on volume</summary>
		private OrderBook MergeRates(OrderBook rates, OrderBook orders, Unit<decimal> balance, bool invert)
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

			var R_vol = 0m._(rates.Base.Symbol);
			var D_vol = 0m._(rates.Base.Symbol);

			// The maximum volume available to trade is the minimum of the 'rates' and 'orders'
			// volumes, hence this loop ends when the last order in either set is reached.
			for (int r = 0, d = 0; r != rates.Count && d != orders.Count;)
			{
				var rate = rates[r];
				var ordr = orders[d];

				// Get the offer price and volume to convert to the current coin currency
				var price = invert ? 1m/ordr.Price : ordr.Price;
				var volume = invert ? ordr.Price * ordr.VolumeBase : ordr.VolumeBase;

				// Get the volume available to be traded at 'price'
				var vol0 = rate.VolumeBase < volume ? rate.VolumeBase : volume;

				// Convert this volume to the new currency using 'price'
				var vol1 = vol0 * price;

				// Record the volume and the combined rate
				ret.Add(new Order(rate.Price * price, vol1), validate:false);

				// If the same accumulative volume is in both sets, move to the next order in both sets.
				if (R_vol + rate.VolumeBase <= D_vol + volume) { ++r; R_vol += rate.VolumeBase; }
				if (R_vol + rate.VolumeBase >= D_vol + volume) { ++d; D_vol += volume; }

				// Don't both calculating for volumes that exceed the current balance
				if (rates.Count > 5 && R_vol > balance)
					break;
			}

			return ret;
		}

		/// <summary>Execute a profitable loop</summary>
		private void ExecuteLoop(Loop loop)
		{
			try
			{
				// Output a description of what would happen
				var sb = new StringBuilder();
				 sb.AppendLine(loop.CoinsString(loop.Direction));

				// Get the starting currency and the volume to trade (adjusted to allow for rounding)
				var coin = loop.Beg;
				var volume = loop.TradeVolume;

				// Calculate the effective fee in initial coin currency.
				// Do all trades assuming no fee, but accumulate the fee separately
				var fee = 0m._(coin);
				var initial_volume = volume;
				var scale = loop.TradeScale;
				if (scale != 1m) scale *= 0.999m;

				// Trade each pair in the loop (in the given direction).
				var tasks = new List<Task>();
				foreach (var pair in loop.EnumPairs(loop.Direction))
				{
					var new_coin = pair.OtherCoin(coin);
					var new_volume = 0m._(new_coin);
					if (pair.Base == coin)
					{
						var converted = pair.BaseToQuote(volume);
						tasks.Add(pair.CreateB2QOrder(volume * scale, converted.Price));
						new_volume = converted.VolumeBase;

						// Trade 'coin' to 'Quote'
						sb.AppendLine("   Trade {0} {1} => {2} {3}".Fmt(volume, coin, new_volume, new_coin));
					}
					else
					{
						var converted = pair.QuoteToBase(volume);
						tasks.Add(pair.CreateQ2BOrder(volume * scale, converted.Price));
						new_volume = converted.VolumeBase;

						// Trade 'coin' to 'Base'
						sb.AppendLine("   Trade {0} {1} => {2} {3}".Fmt(volume, coin, new_volume, new_coin));
					}

					// Convert the fee to the new coin using the effective rate, and add on the fee
					var rate = new_volume / volume;
					fee = fee * rate + new_volume * pair.Fee;

					coin = new_coin;
					volume = new_volume;
				}

				// Return the nett profit
				var gross = volume - initial_volume;
				sb.AppendLine(" Gross: {0} {1}.  (Trade Scale: {2:N3})".Fmt(gross, coin, scale));
				sb.AppendLine("   Fee: {0} {1}.".Fmt(-fee, coin));
				sb.AppendLine("  Nett: {0} {1}.".Fmt(gross - fee, coin));

				// Wait for all orders to complete
				Task.WhenAll(tasks).GetAwaiter().GetResult();

				// Log the trades
				Log.Write(ELogLevel.Info, sb.ToString());
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Trade Loop aborted.");
			}
		}

		/// <summary>Update the data sources for the exchange specific data</summary>
		private void UpdateExchangeDetails()
		{
			var exch = Exchanges.Current;
			Balances.DataSource = exch?.Balance;
			Positions.DataSource = exch?.Positions;
		}

		/// <summary>Debugging test</summary>
		public async void Test()
		{
			try
			{
				var exch = Exchanges.Current;
				if (exch == null) return;

				var pair = exch.Pairs["ETC","LTC"];
				if (pair == null) return;

				var tasks = new List<Task>();
				tasks.Add(pair.CreateOrder(ETradeType.Buy, 0.01m._("ETC"), price:0.01m._("LTC/ETC")));
				tasks.Add(pair.CreateOrder(ETradeType.Sell, 0.01m._("ETC"), price:1.0m._("LTC/ETC")));
				await Task.WhenAll(tasks);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Create Order Failed");
			}
		}

		/// <summary>Execute 'action' in the GUI thread context</summary>
		public void RunOnGuiThread(Action action)
		{
			m_dispatcher.BeginInvoke(action);
		}
		private Dispatcher m_dispatcher;
	}
}
