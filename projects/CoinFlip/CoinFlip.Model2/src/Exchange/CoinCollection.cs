﻿using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinCollection : CollectionBase<string, Coin>
	{
		// Notes:
		//  - The coins associated with an exchange

		private readonly CoinDataList m_coin_data;
		public CoinCollection(Exchange exch, CoinDataList coin_data)
			: base(exch)
		{
			m_coin_data = coin_data;
			KeyFrom = x => x.Symbol;
		}

		/// <summary>Get or add a coin by symbol name</summary>
		public Coin GetOrAdd(string sym)
		{
			Debug.Assert(Misc.AssertMainThread());
			return this.GetOrAdd(sym, CreateCoin);
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

		/// <summary>Implicitly create a new coin</summary>
		private Coin CreateCoin(string sym)
		{
			var coin = new Coin(sym, Exch);
			if (!m_coin_data.Contains(coin.Meta))
				m_coin_data.Add(coin.Meta);

			return coin;
		}
	}
}
