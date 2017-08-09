using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>A closed loop series of trades</summary>
	[DebuggerDisplay("{CoinsString(Direction)}")]
	public class Loop
	{
		// Note: a closed loop is one that starts and ends at the same currency
		// but not necessarily on the same exchange.

		public Loop(TradePair pair)
		{
			Pairs          = new List<TradePair>();
			Rate           = null;
			Direction      = 0;
			TradeScale     = 1m;
			TradeVolume    = 0m._(pair.Base);
			Profit         = 0m._(pair.Base);
			ProfitRatioFwd = 0;
			ProfitRatioBck = 0;

			Pairs.Add(pair);
		}
		public Loop(Loop rhs)
		{
			Pairs          = rhs.Pairs.ToList();
			Rate           = rhs.Rate;
			Direction      = rhs.Direction;
			TradeScale     = rhs.TradeScale;
			TradeVolume    = rhs.TradeVolume;
			Profit         = rhs.Profit;
			ProfitRatioFwd = rhs.ProfitRatioFwd;
			ProfitRatioBck = rhs.ProfitRatioBck;
		}
		public Loop(Loop loop, OrderBook rate, int direction)
			:this(loop)
		{
			Rate = rate;
			Direction = direction;
		}

		/// <summary>An ordered sequence of trading pairs</summary>
		public List<TradePair> Pairs { get; private set; }

		/// <summary>The first coin in the (possibly partial) loop</summary>
		public Coin Beg
		{
			get { return Direction >= 0 ? BegInternal : EndInternal; }
		}
		private Coin BegInternal
		{
			get { return Pairs.Count > 1 ? Pairs.Front().OtherCoin(TradePair.CommonCoin(Pairs.Front(0), Pairs.Front(1))) : Pairs.Front().Base; }
		}

		/// <summary>The last coin in the (possibly partial) loop</summary>
		public Coin End
		{
			get { return Direction >= 0 ? EndInternal : BegInternal; }
		}
		private Coin EndInternal
		{
			get { return Pairs.Count > 1 ? Pairs.Back().OtherCoin(TradePair.CommonCoin(Pairs.Back(0), Pairs.Back(1))) : Pairs.Back().Quote; }
		}

		/// <summary>The accumulated exchange rate when cycling forward/backward around this loop (indicated by Direction)</summary>
		public OrderBook Rate { get; private set; }

		/// <summary>The direction to go around the loop</summary>
		public int Direction { get; private set; }

		/// <summary>A scaling factor for this loop based on the available balances</summary>
		public decimal TradeScale { get; set; }

		/// <summary>The maximum profitable trade volume (in the starting currency)</summary>
		public Unit<decimal> TradeVolume { get; set; }

		/// <summary>The gain in volume of this loop</summary>
		public Unit<decimal> Profit { get; set; }

		/// <summary>The profitability of this loop as a ratio when going forward around the loop (volume final / volume initial)</summary>
		public decimal ProfitRatioFwd { get; set; }

		/// <summary>The profitability of this loop as a ratio when going backward around the loop (volume final / volume initial)</summary>
		public decimal ProfitRatioBck { get; set; }

		/// <summary>The maximum profit ratio</summary>
		public decimal ProfitRatio
		{
			get { return Math.Max(ProfitRatioFwd, ProfitRatioBck); }
		}

		/// <summary>The trading pair that limits the volume </summary>
		public Coin LimitingCoin { get; set; }

		/// <summary>True if all the exchanges involved in this loop are active</summary>
		public bool AllExchangesActive
		{
			get { return Pairs.All(x => x.Exchange.Active); }
		}

		/// <summary>The loop described as a string</summary>
		public string Description
		{
			get { return CoinsString(Direction); }
		}

		/// <summary>A string description of why this loop is trade-able or not</summary>
		public string Tradeability { get; set; }

		/// <summary>Return a string describing the loop</summary>
		public string CoinsString(int direction)
		{
			return string.Join("→", EnumCoins(direction).Select(x => x.Symbol));
		}

		/// <summary>Enumerate the pairs forward or backward around the loop</summary>
		public IEnumerable<TradePair> EnumPairs(int direction)
		{
			if (direction >= 0)
				for (int i = 0; i != Pairs.Count; ++i)
					yield return Pairs[i];
			else
				for (int i = Pairs.Count; i-- != 0;)
					yield return Pairs[i];
		}

		///// <summary>Enumerate the coins forward or backward around the loop</summary>
		public IEnumerable<Coin> EnumCoins(int direction)
		{
			var c = direction >= 0 ? BegInternal : EndInternal;
			yield return c;
			foreach (var pair in EnumPairs(direction))
			{
				c = pair.OtherCoin(c);
				yield return c;
			}
		}

		/// <summary>Generate a unique key for all distinct loops</summary>
		public int HashKey
		{
			get
			{
				var hash0 = FNV1a.Hash(0);
				var hash1 = FNV1a.Hash(0);

				// Closed loops start and finish with the same coin on the same exchange.
				// "Open" loops start and finish with the same currency, but not on the same exchange.
				// We test for profitability in both directions around the loop.
				// Actually, all loops are closed because all open loops have an implicit cross-exchange
				// pair that links End -> Beg. This means all loops with the same order, in either direction
				// are equivalent.

				var closed = Beg == End;
				
				// Loop equivalents: [A→B→C] == [B→C→A] == [C→B→A]
				var len = Pairs.Count + (closed ? 0 : 1);
				var coins = EnumCoins(+1).Take(len).Select(x => x.SymbolWithExchange).ToArray();

				// Find the lexicographically lowest coin name
				var idx = coins.IndexOfMinBy(x => x);

				// Calculate the hash going in both directions and choose the lowest
				for (int i = 0; i != len; ++i)
				{
					var s0 = coins[(idx + i + 2*len) % len];
					var s1 = coins[(idx - i + 2*len) % len];
					hash0 = FNV1a.Hash(s0, hash0);
					hash1 = FNV1a.Hash(s1, hash1);
				}

				return Math.Min(hash0, hash1);
			}
		}
	}

	[DebuggerDisplay("{Coin} {Msg}")]
	public class InsufficientCoin
	{
		public InsufficientCoin(Coin coin, string msg)
		{
			Coin = coin;
			Msg = msg;
		}

		/// <summary>The coin that there isn't enough of if the TradeScale is 0</summary>
		public Coin Coin { get; set; }

		/// <summary>The reason the volume is insufficient</summary>
		public string Msg { get; set; }
	}
}
