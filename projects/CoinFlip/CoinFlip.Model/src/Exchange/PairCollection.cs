using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class PairCollection :CollectionBase<string, TradePair>
	{
		// Notes:
		//  - Don't allow remove from this collection, other objects hold references to
		//    pair instances. Pairs should live for the life of the application.

		public PairCollection(Exchange exch)
			: base(exch, x => x.UniqueKey)
		{}

		/// <summary></summary>
		private BindingDict<string, TradePair> Pairs => m_data;

		/// <summary>Add a pair to this collection</summary>
		public TradePair Add(TradePair pair)
		{
			if (pair.Base.Symbol == "USDC" && pair.Quote.Symbol == "BTC")
				Debugger.Break();

			return Pairs.Add2(pair);
		}

		/// <summary>Get or add the pair associated with the given symbols on this exchange</summary>
		public TradePair GetOrAdd(string @base, string quote, int? trade_pair_id = null)
		{
			var coinB = Exchange.Coins.GetOrAdd(@base);
			var coinQ = Exchange.Coins.GetOrAdd(quote);
			return this[@base, quote] ?? Add(new TradePair(coinB, coinQ, Exchange, trade_pair_id));
		}

		/// <summary>Get/Set the pair</summary>
		public TradePair this[string key]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Pairs.TryGetValue(key, out var pair) ? pair : null;
			}
		}

		/// <summary>Return a pair involving the given symbols (in either order)</summary>
		public TradePair this[string sym0, string sym1]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				if (Pairs.TryGetValue(TradePair.MakeKey(sym0, sym1), out var pair0)) return pair0;
				if (Pairs.TryGetValue(TradePair.MakeKey(sym1, sym0), out var pair1)) return pair1;
				return null;
			}
		}

		/// <summary>Return a pair involving the given symbol and the two exchanges (in either order)</summary>
		public TradePair this[string sym, Exchange exch0, Exchange exch1]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				if (Pairs.TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out var pair0)) return pair0;
				if (Pairs.TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out var pair1)) return pair1;
				return null;
			}
		}
	}
}
