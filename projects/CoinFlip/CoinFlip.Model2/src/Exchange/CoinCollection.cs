using System.Diagnostics;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinCollection : CollectionBase<string, Coin>
	{
		// Notes:
		//  - The coins associated with an exchange

		public CoinCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.Symbol;
		}
		public CoinCollection(CoinCollection rhs)
			: base(rhs)
		{ }

		/// <summary>Get or add a coin by symbol name</summary>
		public Coin GetOrAdd(string sym)
		{
			Debug.Assert(Misc.AssertMainThread());
			return this.GetOrAdd(sym, k => new Coin(k, Exch));
		}

		/// <summary>Get/Set a coin by symbol name. Get returns null if 'sym' not in the collection</summary>
		public override Coin this[string sym]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return TryGetValue(sym, out var coin) ? coin : null;
			}
		}
	}
}

