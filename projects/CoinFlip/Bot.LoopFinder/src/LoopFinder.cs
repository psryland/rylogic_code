using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using CoinFlip;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Bot.LoopFinder
{
	[Plugin(typeof(IBot), Unique = true)]
	public class LoopFinder :IBot
	{
		// Notes:
		//  - The loops collection is rebuilt whenever new pairs are added.
		//  - Each exchange updates its data independently.
		//  - Each exchange owns the coins and pairs it provides.
		//  - The CrossExchange is a special exchange that links coins of the same currency between exchanges.
		//
		// The main loop (heartbeat) does the following:
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

		public LoopFinder(Model model, XElement settings_xml)
			:base("Loop Finder", model, new SettingsData(settings_xml))
		{
			Loops = new BindingSource<Loop>{ DataSource = new BindingListEx<Loop>() };
			LoopsUI = new LoopsUI(this, Model.UI);
		}
		protected override void Dispose(bool disposing)
		{
			LoopsUI = null;
			Loops = null;
			base.Dispose(disposing);
		}
		protected override void HandlePairsUpdated(object sender, EventArgs e)
		{
			TriggerLoopsUpdate();
		}

		/// <summary>Settings for this strategy</summary>
		public new SettingsData Settings
		{
			get { return (SettingsData)base.Settings; }
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

		/// <summary>The UI for displaying the loops</summary>
		public LoopsUI LoopsUI
		{
			get { return m_loops_ui; }
			private set
			{
				if (m_loops_ui == value) return;
				Util.Dispose(ref m_loops_ui);
				m_loops_ui = value;
			}
		}
		private LoopsUI m_loops_ui;

		/// <summary>Start the bot</summary>
		public override bool OnStart()
		{
			TriggerLoopsUpdate();
			return true;
		}

		/// <summary>Main loop step</summary>
		public override void Step()
		{
			// Test the loops for profitability, and execute the best
			ExecuteProfitableLoops();
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public override void CMenuItems(ContextMenuStrip cmenu)
		{
			cmenu.Items.AddSeparator();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Show Loops"));
				opt.Click += (s,a) =>
				{
					LoopsUI.Show(Model.UI);
				};
			}
		}

		/// <summary>Cause the set of loops to be regenerated</summary>
		public void TriggerLoopsUpdate()
		{
			// If an update is already pending, ignore
			if (m_rebuild_loops.Pending) return;
			m_rebuild_loops.Signal();
			Model.RunOnGuiThread(FindLoops);
		}
		private Trigger m_rebuild_loops;

		/// <summary>Find the available trade loops</summary>
		private void FindLoops()
		{
			Debug.Assert(Model.AssertMainThread());

			// Record the update issue number
			Log.Write(ELogLevel.Info, "Rebuilding loops ...");
			m_rebuild_loops.Actioned();
			bool Abort() { return m_rebuild_loops.Pending || Shutdown.IsCancellationRequested; }
			if (Abort())
				return;

			// Local copy of shared variables
			var pairs = Model.Pairs.ToList();
			var x_exch = Model.CrossExchange;
			var max_loop_count = Settings.MaximumLoopCount;
			var sw = new Stopwatch().Start2();

			// A collection of complete loops
			var result = new ConcurrentDictionary<int, Loop>();

			// Run the background process
			ThreadPool.QueueUserWorkItem(_ =>
			{
				try
				{
					// Create a producer/consumer queue of partial loops
					// Don't use CrossExchange pairs to start the loops to guarantee all
					// loops have at least one non-CrossExchange pair in them
					var queue = new BlockingCollection<Loop>();
					foreach (var pair in pairs.Where(x => x.Exchange != x_exch))
						queue.Add(new Loop(pair));

					// Create a map from coins to pairs involving those coins
					var map = new Dictionary<Coin, List<TradePair>>();
					foreach (var pair in pairs)
					{
						map.GetOrAdd(pair.Base , k => new List<TradePair>()).Add(pair);
						map.GetOrAdd(pair.Quote, k => new List<TradePair>()).Add(pair);
					}

					// Stop when there are no more partial loops to process
					var workers = new List<Completion>();
					for (;;)
					{
						// If the loops have been invalidated in the meantime, give up
						if (Abort())
							return;

						// Take a partial loop from the queue
						if (!queue.TryTake(out var loop, TimeSpan.FromMilliseconds(50)))
						{
							// If there are no partial loops to process and
							// no running workers we must be done.
							workers.RemoveIf(x => x.IsCompleted);
							if (workers.Count == 0) break;
							continue;
						}

						// Process the partial loop in a worker thread
						ThreadPool.QueueUserWorkItem(completion_ =>
						{
							// - A closed loop is a chain of pairs that go from one currency, through one or more other
							//   currencies, and back to the starting currency. Note: the ending currency does *NOT* have to
							//   be on the same exchange.
							// - Loops cannot start or end with a cross-exchange pair
							// - Don't allow sequential cross-exchange pairs
							var completion = (Completion)completion_;
							try
							{
								var beg = loop.Beg;
								var end = loop.End;

								// Special case for single-pair loops, allow for the first pair to be reversed
								if (loop.Pairs.Count == 1)
								{
									// Get all the pairs that match 'beg' or 'end' and don't close the loop
									var links = Enumerable.Concat(
										map[beg].Except(loop.Pairs[0]).Where(x => x.OtherCoin(beg).Symbol != end.Symbol),
										map[end].Except(loop.Pairs[0]).Where(x => x.OtherCoin(end).Symbol != beg.Symbol));

									foreach (var pair in links)
									{
										if (Abort())
											break;
	
										// Grow the loop
										var nue_loop = new Loop(loop);
										nue_loop.Pairs.Add(pair);
										queue.Add(nue_loop);
									}
								}
								else
								{
									// Look for a pair to add to the loop
									var existing_pairs = loop.Pairs.ToHashSet();
									foreach (var pair in map[end].Except(existing_pairs))
									{
										if (Abort())
											break;

										// Don't allow sequential cross-exchange pairs
										if (loop.Pairs.Back().Exchange == Model.CrossExchange && pair.Exchange == Model.CrossExchange)
											continue;

										// Does this pair close the loop?
										if (pair.OtherCoin(end).Symbol == beg.Symbol)
										{
											// Add a complete loop
											var nue_loop = new Loop(loop);
											nue_loop.Pairs.Add(pair);
											result.TryAdd(nue_loop.HashKey, nue_loop);
										}
										else if (loop.Pairs.Count < max_loop_count)
										{
											// Grow the loop
											var nue_loop = new Loop(loop);
											nue_loop.Pairs.Add(pair);
											queue.Add(nue_loop);
										}
									}
								}

								if (Abort())
									completion.Cancelled = true;
								else
									completion.Completed();
							}
							catch (Exception ex)
							{
								completion.Exception = ex;
							}
						}, workers.Add2(new Completion()));
					}

					// Update the loops collection
					Model.RunOnGuiThread(() =>
					{
						// If the loops have been invalidated in the meantime, give up
						if (Abort())
							return;

						Loops.Clear();
						Loops.AddRange(result.Values.ToArray());

						// Done
						Log.Write(ELogLevel.Info, $"{Loops.Count} trading pair loops found. (Taking {sw.Elapsed.TotalSeconds} seconds)");
					});
				}
				catch (Exception ex)
				{
					Log.Write(ELogLevel.Error, ex, "Unhandled exception in 'FindLoops'");
				}
			});
		}

		/// <summary>Check the loops collection for profitable loops</summary>
		private void ExecuteProfitableLoops()
		{
			ThreadPool.QueueUserWorkItem(async _ =>
			{
				// Find the potentially profitable loops ignoring account balances and fees.
				var tasks = new List<System.Threading.Tasks.Task>();
				var bag = new ConcurrentBag<Loop>();
				foreach (var loop in Loops)
				{
					tasks.Add(System.Threading.Tasks.Task.Run(() => AddLoopIfProfitable(loop, true , bag), Shutdown.Token));
					tasks.Add(System.Threading.Tasks.Task.Run(() => AddLoopIfProfitable(loop, false, bag), Shutdown.Token));
					//AddLoopIfProfitable(loop, true , bag);
					//AddLoopIfProfitable(loop, false, bag);
				}

				// Wait for the profitable loops be found
				await System.Threading.Tasks.Task.WhenAll(tasks);

				// Return the profitable loops in order of most profitable
				var profitable_loops = bag.OrderByDescending(x => x.ProfitRatio).ToList();

				// Execute the most profitable loop
				foreach (var loop in profitable_loops)
				{
					if (loop.TradeScale == 0)
					{
						var exchanges = string.Join(",", loop.Pairs.Select(x => x.Exchange.Name).Distinct());
						Log.Write(ELogLevel.Info, $"Potential Loop {loop.Description} ({exchanges}). Profit Ratio: {loop.ProfitRatio:G4}.\n{loop.Tradeability}");
						continue;
					}

					// Execute the loop
					ExecuteLoop(loop);

					// Execute one loop only
					break;
				}

				Model.RunOnGuiThread(() =>
				{
					// Sort loops by profitability
					using (Loops.PreservePosition())
						Loops.Sort(Cmp<Loop>.From((l, r) => -l.ProfitRatio.CompareTo(r.ProfitRatio)));
				});
			});
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

			// Construct an "order book" of volumes and complete-loop prices (e.g. BTC to BTC price for each volume)
			var dir = forward ? +1 : -1;
			var coin = forward ? src_loop.Beg : src_loop.End;
			var tt = forward ? ETradeType.B2Q : ETradeType.Q2B;
			var obk = new OrderBook(coin, coin, tt){ new OrderBook.Offer(1m, decimal.MaxValue._(coin.Symbol)) };
			foreach (var pair in src_loop.EnumPairs(dir))
			{
				// Limit the volume calculated, there's no point in calculating large volumes if we can't trade them
				Unit<decimal> bal;
				OrderBook b2q, q2b;
				using (Model.LockMarketData())
				{
					bal = coin.Balances[Fund].Available;
					b2q = new OrderBook(pair.B2Q);
					q2b = new OrderBook(pair.Q2B);
				}

				// Note: the trade prices are in quote currency
				if (pair.Base == coin)
					obk = MergeRates(obk, b2q, bal, invert:false);
				else if (pair.Quote == coin)
					obk = MergeRates(obk, q2b, bal, invert:true);
				else
					throw new Exception($"Pair {pair} does not include Coin {coin}. Loop is invalid.");

				// Get the next coin in the loop
				coin = pair.OtherCoin(coin);
			}
			if (obk.Count == 0)
				return;

			// Save the best profit ratio for this loop (as an indication)
			if (forward)
				src_loop.ProfitRatioFwd = obk[0].Price;
			else
				src_loop.ProfitRatioBck = obk[0].Price;

			// Look for any volumes that have a nett gain
			var volume_gain = obk.Where(x => x.Price > 1).Sum(x => x.Price * x.VolumeBase);
			if (volume_gain == 0)
				return;

			// Create a copy of the loop for editing (with the direction set)
			var loop = new Loop(src_loop, obk, dir);

			// Find the maximum profitable volume to trade
			var volume = 0m._(loop.Beg);
			foreach (var ordr in loop.Rate.Where(x => x.Price > 1))
				volume += ordr.VolumeBase;

			// Calculate the effective fee in initial coin currency.
			// Do all trades assuming no fee, but accumulate the fee separately
			var fee = 0m._(loop.Beg);
			var initial_volume = volume;

			// Trade each pair in the loop (in the given direction) to check 
			// that the trade is still profitable after fees. Record each trade
			// so that we can determine the trade scale
			coin = loop.Beg;
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
					? pair.BaseToQuote(Fund.Id, volume)
					: pair.QuoteToBase(Fund.Id, volume);

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
			loop.TradeScale = 1m;
			loop.Tradeability = string.Empty;
			loop.TradeVolume = initial_volume;
			loop.Profit = (volume - fee) - initial_volume;
			if (forward) loop.ProfitRatioFwd = src_loop.ProfitRatioFwd = (volume - fee) / initial_volume;
			else         loop.ProfitRatioBck = src_loop.ProfitRatioBck = (volume - fee) / initial_volume;
			if (loop.ProfitRatio <= 1m)
				return;

			// Determine the trade scale based on the available balances
			foreach (var trade in trades)
			{
				var pair = trade.Pair;

				// Get the balance available for this trade and determine a trade scaling factor.
				// Increase the required volume to allow for the fee
				// Reduce the available balance slightly to deal with rounding errors
				var bal = trade.CoinIn.Balances[Fund].Available * 0.999m;
				var req = trade.VolumeIn * (1 + pair.Fee);
				var scale = Math_.Clamp((decimal)(bal / req), 0m, 1m);
				if (scale < loop.TradeScale)
				{
					loop.TradeScale = Math_.Clamp(scale, 0, loop.TradeScale);
					loop.LimitingCoin = trade.CoinIn;
				}
			}

			// Check that all traded volumes are within the limits
			var all_trades_valid = EValidation.Valid;
			foreach (var trade in trades)
			{
				// Check the unscaled amount, if that's too small we'll ignore this loop
				var validation0 = trade.Validate();
				all_trades_valid |= validation0;

				// Record why the base trade isn't valid
				if (validation0 != EValidation.Valid)
					loop.Tradeability += $"{trade.Description} - {validation0}\n";

				// If the volume to trade, multiplied by the trade scale, is outside the
				// allowed range of trading volume, set the scale to zero. This is to prevent
				// loops being traded where part of the loop would be rejected.
				var validation1 = new Trade(trade, loop.TradeScale).Validate();
				if (validation1.HasFlag(EValidation.VolumeInOutOfRange))
					loop.Tradeability += $"Not enough {trade.CoinIn} to trade\n";
				if (validation1.HasFlag(EValidation.VolumeOutOutOfRange))
					loop.Tradeability += $"Trade result volume of {trade.CoinOut} is too small\n";
				if (validation1 != EValidation.Valid)
					loop.TradeScale = 0m;
			}

			// Save the profitable loop (even if scaled to 0)
			if (all_trades_valid == EValidation.Valid)
				profitable_loops.Add(loop);
		}

		/// <summary>Determine the exchange rate based on volume</summary>
		private static OrderBook MergeRates(OrderBook rates, OrderBook orders, Unit<decimal> balance, bool invert) // Worker thread context
		{
			// 'rates' is a table of volumes in the current coin currency (i.e. the current
			// coin in the loop) along with the accumulated exchange rate for each volume.
			// 'orders' is a table of 'Base' currency volumes and the offer prices for
			// converting those volumes to 'Quote' currency.
			// If 'invert' is true, the 'orders' table is the offers for converting Quote
			// currency to Base currency, however the volumes and prices are still in Base
			// and Quote respectively.
			var new_coin = invert ? orders.Base : orders.Quote;
			var ret = new OrderBook(new_coin, new_coin, rates.TradeType);

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
				ret.Add(new OrderBook.Offer(rate.Price * price, vol1), validate:false);

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
		private void ExecuteLoop(Loop loop) // Worker thread context
		{
			try
			{
				Log.Write(ELogLevel.Warn, $"Executing Loop: {loop.Description}  Profit Ratio: {loop.ProfitRatio:G6}");

				// Output a description of what would happen
				var sb = new StringBuilder();
				 sb.AppendLine(loop.CoinsString(loop.Direction));

				// Add notes about the scaling
				sb.Append($"Trade Scale: {loop.TradeScale:N8}");
				if (loop.LimitingCoin != null) sb.Append($" (due to {loop.LimitingCoin.SymbolWithExchange})");
				sb.AppendLine();

				// Calculate the effective fee in initial coin currency.
				// Do all trades assuming no fee, but accumulate the fee separately
				var coin = loop.Beg;
				var fee = 0m._(coin);
				var volume = loop.TradeVolume * loop.TradeScale;
				var initial_volume = volume;

				// Trade each pair in the loop (in the given direction).
				foreach (var pair in loop.EnumPairs(loop.Direction))
				{
					var new_coin = pair.OtherCoin(coin);
					var new_volume = 0m._(new_coin);
					if (pair.Base == coin)
					{
						var trade = pair.BaseToQuote(Fund.Id, volume);
						trade.CreateOrder();
						new_volume = trade.VolumeOut;

						// Trade 'coin' to 'Quote'
						sb.AppendLine($"   Trade {volume.ToString("G6")} {coin} => {new_volume.ToString("G6")} {new_coin} @ {trade.Price.ToString("G6")}");
					}
					else
					{
						var trade = pair.QuoteToBase(Fund.Id, volume);
						trade.CreateOrder();
						new_volume = trade.VolumeOut;

						// Trade 'coin' to 'Base'
						sb.AppendLine($"   Trade {volume.ToString("G6")} {coin} => {new_volume.ToString("G6")} {new_coin} @ {trade.Price.ToString("G6")}");
					}

					// Convert the fee to the new coin using the effective rate, and add on the fee
					var rate = new_volume / volume;
					fee = fee * rate + new_volume * pair.Fee;

					coin = new_coin;
					volume = new_volume;
				}

				// Return the nett profit
				var gross = volume - initial_volume;
				sb.AppendLine($" Gross: {gross.ToString("G6")} {coin}.");
				sb.AppendLine($" Fee:   {(-fee).ToString("G6")} {coin}.");
				sb.AppendLine($" Nett:  {(gross - fee).ToString("G6")} {coin}.");

				// Log the trades
				Log.Write(ELogLevel.Info, sb.ToString());
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Trade Loop aborted.");
				Model.RunOnGuiThread(() => Active = false);
			}
		}

		/// <summary>Loop Finder settings</summary>
		public class SettingsData :SettingsBase<SettingsData>
		{
			public SettingsData()
			{
				MaximumLoopCount = 5;
			}
			public SettingsData(SettingsData rhs)
				:base(rhs)
			{
				MaximumLoopCount = rhs.MaximumLoopCount;
			}
			public SettingsData(XElement node)
				:base(node)
			{}

			/// <summary>The maximum number of hops in a loop</summary>
			public int MaximumLoopCount
			{
				get { return get<int>(nameof(MaximumLoopCount)); }
				set { set(nameof(MaximumLoopCount), value); }
			}
		}
	}
}
