using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using pr.container;
using pr.util;

namespace CoinFlip
{
	public class Model :IDisposable
	{
		public Model(MainUI main_ui)
		{
			UI = main_ui;

			CoinsOfInterest  = new BindingSource<string> { DataSource = new BindingListEx<string>() };
			Exchanges        = new BindingSource<Exchange> { DataSource = new BindingListEx<Exchange>() };
			Coins            = new BindingSource<Coin> { DataSource = new BindingListEx<Coin>() };
			Pairs            = new BindingSource<TradePair> { DataSource = new BindingListEx<TradePair>() };
			Loops            = new List<Loop>();
			MaximumLoopCount = 20;

			CoinsOfInterest.Add("BTC");
			CoinsOfInterest.Add("ETC");
			CoinsOfInterest.Add("LTC");

			// Add exchanges
			Exchanges.Add(new Cryptopia(this));

			// Create cross-exchange "TradePairs"
			AddCrossExchangePairs();
		}
		public virtual void Dispose()
		{
			Util.DisposeAll(Pairs);
		}

		/// <summary>The main UI</summary>
		public MainUI UI { get; private set; }

		/// <summary>The coins to search</summary>
		public BindingSource<string> CoinsOfInterest
		{
			get;
			private set;
		}

		/// <summary>The maximum number of hops in a loop</summary>
		public int MaximumLoopCount
		{
			get;
			set;
		}

		/// <summary>The exchanges</summary>
		public BindingSource<Exchange> Exchanges
		{
			get;
			private set;
		}

		/// <summary>The available coins</summary>
		public BindingSource<Coin> Coins
		{
			get;
			private set;
		}

		/// <summary>The available trade pairs</summary>
		public BindingSource<TradePair> Pairs
		{
			get;
			private set;
		}

		/// <summary>Profitable trade loops</summary>
		public List<Loop> Loops { get; set; }

		/// <summary>Set up trade pairs between exchanges</summary>
		private void AddCrossExchangePairs()
		{
			// todo
		}

		/// <summary>Run one tick</summary>
		public void Step()
		{
			// Update ask/bid prices and the account balances
			UpdateExchangeData();

			// Find profitable loops
			FindLoops();

			// Execute loops
			foreach (var loop in Loops)
			{
			}

			// Clear for next time round
			Loops.Clear();
		}

		/// <summary>Get each exchange to update its trade pairs</summary>
		private async void UpdateExchangeData()
		{
			// Kick off updates for all exchanges
			var update_tasks = Exchanges.Select(x => x.UpdateTradeDataAsync()).ToArray();
			await Task.WhenAll(update_tasks);
		}

		/// <summary>Find profitable loops</summary>
		private void FindLoops()
		{
			// The trading pairs are the edges in an undirected graph.
			// We want to identify all of the cycles in the graph and
			// test these for profitable loops.

			// Use two buffers, one buffer contains the partial loops
			// from the previous round, the other collects the partial
			// (but bigger) loops for the next round.
			var loops0 = new List<Loop>();
			var loops1 = new List<Loop>();

			// Start each loop with a single edge, one for each trading pair
			for (var i = 0; i != Pairs.Count; ++i)
				loops0.Add(new Loop(Pairs[i], i));

			// Find closed loops within the graph, starting with the smallest
			// and building up to the largest.
			for (var k = 0;; ++k)
			{
				var prev = (k%2) == 0 ? loops0 : loops1;
				var next = (k%2) == 0 ? loops1 : loops0;

				// Quit when there are no more partial loops
				// or the maximum loop length is exceeded.
				if (prev.Count == 0 || k == MaximumLoopCount) break;
				next.Clear();

				// Extend the partial loops
				for (var j = 0; j != prev.Count; ++j)
				{
					var loop = prev[j];

					// Starting from the next pair, see if it can be appended to
					// the partial loop. If 'pair' closes the loop, add it to the
					// loops to be tested for profit.
					for (var i = loop.LastPairIndex + 1; i < Pairs.Count; ++i)
					{
						var pair = Pairs[i];

						// Does 'pair' close the loop?
						if ((pair.Base == loop.Beg && pair.Quote == loop.End) ||
							(pair.Base == loop.End && pair.Quote == loop.Beg))
						{
							loop.Pairs.Add(pair);
							loop.LastPairIndex = i;
							AddLoopIfProfitable(loop);
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
		}

		/// <summary>Determine if executing trades in 'loop' should result in a profit</summary>
		private void AddLoopIfProfitable(Loop loop)
		{
			var buy = new Orders(+1);
			var sel = new Orders(-1);

			// Start with 'coin'. It shouldn't matter where in the loop we start, but
			// we need to buy or sell depending on the order of the pair.
			var coin = loop.Beg;

			// Determine the effective exchange rate when cycling around the currency pairs
			// The rate depends on the amount traded, so we need to calculate all rates up
			// to the amount available for each coin within the account on each exchange.
			foreach (var pair in loop.Pairs)
			{
				// Determine the amount of 'coin' available
				var balance = coin.Exchange.Balance(coin);

				if (pair.Base == coin)
				{
					//
				}
				else
				{
					//Debug.Assert(pair.Quote == coin);
				}
			}
		}

	}
}
