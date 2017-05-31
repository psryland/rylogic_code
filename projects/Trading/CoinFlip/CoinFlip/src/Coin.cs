using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinFlip
{
	[DebuggerDisplay("{Symbol} ({Exchange})")]
	public class Coin
	{
		public Coin(string symbol, Exchange exch)
		{
			Pairs = new HashSet<TradePair>();
			Symbol = symbol;
			Exchange = exch;
		}

		/// <summary>Coin name</summary>
		public string Symbol { get; private set; }

		/// <summary>The Exchange trading this coin</summary>
		public Exchange Exchange { get; private set; }

		/// <summary>Trade pairs involving this coin</summary>
		public HashSet<TradePair> Pairs
		{
			get;
			private set;
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Symbol;
		}

		#region Equals
		public static bool operator == (Coin coin, string symbol)
		{
			return coin?.Symbol == symbol;
		}
		public static bool operator != (Coin coin, string symbol)
		{
			return !(coin == symbol);
		}
		public bool Equals(Coin coin)
		{
			return
				Symbol == coin.Symbol &&
				Exchange == coin.Exchange;
		}
		public override bool Equals(object obj)
		{
			return obj is Coin && Equals((Coin)obj);
		}
		public override int GetHashCode()
		{
			return new { Symbol, Exchange }.GetHashCode();
		}
		#endregion
	}
}
