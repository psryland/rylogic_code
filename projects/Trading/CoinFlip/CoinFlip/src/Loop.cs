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
		public Loop(TradePair pair)
		{
			Pairs = new List<TradePair>();
			Coins = new List<Coin>();
			Insufficient = new List<InsufficientCoin>();
			Rate = null;
			TradeScale = 1m;
			TradeVolume = 0m._();
			Direction = 0;
			ProfitRatio = 0;

			Pairs.Add(pair);
			Coins.Add(pair.Base);
			Coins.Add(pair.Quote);
		}
		public Loop(Loop rhs)
		{
			Pairs        = rhs.Pairs.ToList();
			Coins        = rhs.Coins.ToList();
			Insufficient = rhs.Insufficient.ToList();
			Rate         = rhs.Rate;
			TradeScale   = rhs.TradeScale;
			TradeVolume  = rhs.TradeVolume;
			Direction    = rhs.Direction;
			ProfitRatio  = rhs.ProfitRatio;
		}
		public Loop(Loop loop, OrderBook rate, int direction, decimal profitability)
			:this(loop)
		{
			Rate = rate;
			Direction = direction;
			ProfitRatio = profitability;
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

		/// <summary>The gain in volume of this loop</summary>
		public Unit<decimal> Profit { get; set; }

		/// <summary>The profitability of this loop as a ratio (volume final / volume initial)</summary>
		public decimal ProfitRatio { get; set; }

		/// <summary>The currencies that are preventing the loop executing</summary>
		public List<InsufficientCoin> Insufficient { get; set; }
		public class InsufficientCoin
		{
			public InsufficientCoin(Coin coin, Unit<decimal> required)
			{
				Coin = coin;
				Required = required;
			}

			/// <summary>The coin that there isn't enough of if the TradeScale is 0</summary>
			public Coin Coin { get; set; }

			/// <summary>The volume that would be required to execute the loop</summary>
			public Unit<decimal> Required { get; set; }
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

		#region Equals
		public bool Equals(Loop rhs)
		{
			if (rhs == null) return false;
			if (ReferenceEquals(this, rhs)) return true;
			if (Coins.Count != rhs.Coins.Count) return false;
			Debug.Assert(Coins.Count >= 2);

			// If the loops are equal, rhs should have the same coins
			var ofs = rhs.Coins.IndexOf(Coins[0]);
			if (ofs == -1)
				return false;

			// Two Loops are equal if they cycle through the
			// same sequence of coins (in either direction).
			int i,count = Coins.Count;
			for (i = 1; i != count; ++i)
			{
				if (Coins[i] == rhs.Coins[(i + ofs) % count]) continue;
				break;
			}
			if (i == count) return true;
			for (i = 1; i != count; ++i)
			{
				if (Coins[count-i] == rhs.Coins[(i + ofs) % count]) continue;
				break;
			}
			if (i == count) return true;
			return false;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Loop);
		}
		public override int GetHashCode()
		{
			return new { Coins, Pairs }.GetHashCode();
		}
		#endregion
	}
}
