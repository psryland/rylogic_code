using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using pr.extn;

namespace CoinFlip
{
	/// <summary>A closed loop series of trades </summary>
	[DebuggerDisplay("{Beg}->{End} [{Coins.Count}]")]
	public class Loop
	{
		public Loop(TradePair pair, int index)
		{
			Pairs = new List<TradePair>();
			Coins = new List<Coin>();
			Pairs.Add(pair);
			Coins.Add(pair.Base);
			Coins.Add(pair.Quote);
			LastPairIndex = index;
		}
		public Loop(Loop loop)
		{
			Pairs = loop.Pairs.ToList();
			Coins = loop.Coins.ToList();
			LastPairIndex = loop.LastPairIndex;
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

		/// <summary>The index of the last pair added to 'Pairs'. (Note, not the index of Pairs.Back() because the collection is ordered)</summary>
		internal int LastPairIndex
		{
			get;
			set;
		}
	}

}
