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
		public Loop(TradePair pair, int index)
		{
			Pairs = new List<TradePair>();
			Coins = new List<Coin>();
			Rate = null;
			TradeScale = 1m;
			TradeVolume = 0m._();
			Direction = 0;
			Profitability = 0;
			LastPairIndex = index;

			Pairs.Add(pair);
			Coins.Add(pair.Base);
			Coins.Add(pair.Quote);
		}
		public Loop(Loop rhs)
		{
			Pairs         = rhs.Pairs.ToList();
			Coins         = rhs.Coins.ToList();
			Rate          = rhs.Rate;
			TradeScale    = rhs.TradeScale;
			TradeVolume   = rhs.TradeVolume;
			Direction     = rhs.Direction;
			Profitability = rhs.Profitability;
			LastPairIndex = rhs.LastPairIndex;
		}
		public Loop(Loop loop, OrderBook rate, int direction, decimal profitability)
			:this(loop)
		{
			Rate = rate;
			Direction = direction;
			Profitability = profitability;
		}

		/// <summary>An ordered sequence of trading pairs</summary>
		public List<TradePair> Pairs
		{
			get;
			private set;
		}

		/// <summary>An ordered sequence of the currencies cycled through in this loop</summary>
		public List<Coin> Coins
		{
			get;
			private set;
		}

		/// <summary>The first coin in the (partial) loop</summary>
		public Coin Beg
		{
			get { return Coins.Front(); }
		}

		/// <summary>The last coin in the (partial) loop</summary>
		public Coin End
		{
			get { return Coins.Back(); }
		}

		/// <summary>A scaling factor for this loop based on the available balances</summary>
		public decimal TradeScale { get; set; }

		/// <summary>The maximum profitable trade volume (in the starting currency)</summary>
		public Unit<decimal> TradeVolume { get; set; }

		/// <summary>The accumulated exchange rate when cycling forward/backward around this loop (indicated by Rate.Sign)</summary>
		public OrderBook Rate { get; private set; }

		/// <summary>The direction to go around the loop</summary>
		public int Direction { get; private set; }

		/// <summary>The volume weighted profitability of this loop</summary>
		public decimal Profitability { get; set; }

		/// <summary>The index of the last pair added to 'Pairs'. (Note, not the index of Pairs.Back() because the collection is ordered)</summary>
		internal int LastPairIndex
		{
			get;
			set;
		}

		/// <summary>Enumerate the coins forward or backward around the loop</summary>
		public IEnumerable<Coin> EnumCoins(int direction)
		{
			if (direction >= 0)
				for (int i = 0; i != Coins.Count; ++i)
					yield return Coins[i];
			else
				for (int i = 0; i != Coins.Count; ++i)
					yield return Coins[(Coins.Count - i) % Coins.Count];
		}

		/// <summary>Enumerate the pairs forward or backward around the loop</summary>
		public IEnumerable<TradePair> EnumPairs(int direction)
		{
			if (direction > 0)
				for (int i = 0; i != Pairs.Count; ++i)
					yield return Pairs[i];
			else
				for (int i = Pairs.Count; i-- != 0;)
					yield return Pairs[i];
		}

		/// <summary>The loop described as a string</summary>
		public string LoopDescription
		{
			get { return CoinsString(Direction); }
		}

		/// <summary>Return a string describing the loop</summary>
		public string CoinsString(int direction)
		{
			return string.Join("→", EnumCoins(direction).Select(x => x.Symbol).Concat(Beg.Symbol));
		}
	}
}
