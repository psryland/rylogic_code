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

		/// <summary>Allow implicit conversion to string symbol name</summary>
		[DebuggerStepThrough] public static implicit operator string(Coin coin)
		{
			return coin?.Symbol;
		}

		#region Equals
		public bool Equals(Coin rhs)
		{
			return
				rhs != null &&
				Symbol == rhs.Symbol &&
				Exchange == rhs.Exchange;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Coin);
		}
		public override int GetHashCode()
		{
			return new { Symbol, Exchange }.GetHashCode();
		}
		#endregion
	}
}
