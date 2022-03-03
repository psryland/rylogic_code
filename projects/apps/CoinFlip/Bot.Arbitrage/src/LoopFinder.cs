using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip;
using Rylogic.Utility;

namespace Bot.Arbitrage
{
	public class LoopFinder
	{
		// Notes:
		//  - This is a class for finding arbitrage loops

		public LoopFinder(Model model)
		{
			Model = model;
			Result = new ConcurrentDictionary<int, Loop>();
			Queue = new BlockingCollection<Loop>();

			// Make local readonly copies of the pairs/exchange for use in worker threads
		}

		/// <summary>The application model</summary>
		private Model Model { get; }

		/// <summary>The found (profitable?) loops</summary>
		public ConcurrentDictionary<int, Loop> Result { get; }

		/// <summary>A Producer/Consumer queue of loops to be "grown" into closed loops</summary>
		private BlockingCollection<Loop> Queue { get; }

		/// <summary>Find the available trade loops</summary>
		public void Start(CancellationToken cancel)
		{
			Debug.Assert(Misc.AssertMainThread());
#if false//todo
			// Create a map from coins to pairs involving those coins
			var map = new Dictionary<Coin, List<TradePair>>();
			foreach (var pair in pairs)
			{
				map.GetOrAdd(pair.Base, k => new List<TradePair>()).Add(pair);
				map.GetOrAdd(pair.Quote, k => new List<TradePair>()).Add(pair);
			}

			// Don't use CrossExchange pairs to start the loops to guarantee all
			// loops have at least one non-CrossExchange pair in them.
			Queue
			foreach (var exch in Model.TradingExchanges)
			{
				// Create a seed loop from each pair on each exchange
				foreach (var pair in exch.Pairs)
					Queue.Add(new Loop(pair));
			}







			// Local copy of shared variables
			//var pairs = Model.Pairs.ToList();
			//var x_exch = Model.CrossExchange;
			//var max_loop_count = Settings.MaximumLoopCount;
			//var sw = new Stopwatch().StartNow();

			// Record the update issue number
			Model.Log.Write(ELogLevel.Info, "Finding arbitrage loops ...");
			m_rebuild_loops_pending = false;
			bool Abort() => m_rebuild_loops_pending || Shutdown.IsCancellationRequested;
			if (Abort())
				return;

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
						map.GetOrAdd(pair.Base, k => new List<TradePair>()).Add(pair);
						map.GetOrAdd(pair.Quote, k => new List<TradePair>()).Add(pair);
					}

					// Stop when there are no more partial loops to process
					var workers = new List<Completion>();
					for (; ; )
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

		/// <summary></summary>
		private void GrowLoop(Loop loop)
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

				// Special case for single-pair loops, allow for the first pair to be reversed.
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
#endif
		}
	}
}
