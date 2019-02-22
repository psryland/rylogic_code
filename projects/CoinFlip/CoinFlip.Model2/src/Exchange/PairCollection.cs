using System;
using System.Diagnostics;
using Rylogic.Extn;

namespace CoinFlip
{
	public class PairCollection : CollectionBase<string, TradePair>
	{
		public PairCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.UniqueKey;
		}
		public PairCollection(PairCollection rhs)
			: base(rhs)
		{ }

		/// <summary>Get or add the pair associated with the given symbols</summary>
		public TradePair GetOrAdd(string @base, string quote, int? trade_pair_id = null)
		{
			var coinB = Exch.Coins.GetOrAdd(@base);
			var coinQ = Exch.Coins.GetOrAdd(quote);
			return this[@base, quote] ?? this.Add2(new TradePair(coinB, coinQ, Exch, trade_pair_id));
		}

		/// <summary>Get/Set the pair</summary>
		public override TradePair this[string key]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				return TryGetValue(key, out var pair) ? pair : null;
			}
			set
			{
				Debug.Assert(Misc.AssertMarketDataWrite());
				if (ContainsKey(key))
					base[key].Update(value);
				else
					base[key] = value;
			}
		}

		/// <summary>Return a pair involving the given symbols (in either order)</summary>
		public TradePair this[string sym0, string sym1]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				if (TryGetValue(TradePair.MakeKey(sym0, sym1), out var pair0)) return pair0;
				if (TryGetValue(TradePair.MakeKey(sym1, sym0), out var pair1)) return pair1;
				return null;
			}
		}

		/// <summary>Return a pair involving the given symbol and the two exchanges (in either order)</summary>
		public TradePair this[string sym, Exchange exch0, Exchange exch1]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				if (TryGetValue(TradePair.MakeKey(sym, sym, exch0, exch1), out var pair0)) return pair0;
				if (TryGetValue(TradePair.MakeKey(sym, sym, exch1, exch0), out var pair1)) return pair1;
				return null;
			}
		}

		/// <summary>Safety check that we're not removing Pairs that are in use</summary>
		protected override void RemoveItemCore(string key, int index)
		{
			throw new Exception("Don't remove pairs, references are being held");
		}
		protected override void SetItemCore(string key, TradePair value)
		{
			throw new Exception("Don't replace existing pairs, references are being held");
		}
	}
}
