using System.Diagnostics;
using CoinFlip.Settings;
using Rylogic.Container;
using Rylogic.Extn;

namespace CoinFlip
{
	public class CoinCollection : CollectionBase<string, Coin>
	{
		// Notes:
		//  - The coins associated with an exchange

		private readonly CoinDataList m_coin_data;
		public CoinCollection(Exchange exch, CoinDataList coin_data)
			: base(exch, x => x.Symbol)
		{
			m_coin_data = coin_data;
		}

		/// <summary></summary>
		private BindingDict<string, Coin> Coins => m_data;

		/// <summary>Add a coin</summary>
		public void Add(string sym, Coin coin)
		{
			Coins.Add(sym, coin);
		}

		/// <summary>Get or add a coin by symbol name</summary>
		public Coin GetOrAdd(string sym)
		{
			Debug.Assert(Misc.AssertMainThread());
			Debug.Assert(m_coin_data.IndexOf(x => x.Symbol == sym) != -1, "Don't add coins that are not in the settings");
			return Coins.GetOrAdd(sym, CreateCoin);
		}

		/// <summary>Get/Set a coin by symbol name. Get returns null if 'sym' not in the collection</summary>
		public Coin? this[string sym]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				return Coins.TryGetValue(sym, out var coin) ? coin : null;
			}
		}

		/// <summary>Implicitly create a new coin</summary>
		private Coin CreateCoin(string sym)
		{
			var coin = new Coin(sym, Exchange);
			if (!m_coin_data.Contains(coin.Meta))
				m_coin_data.Add(coin.Meta);

			CoinData.NotifyBalanceChanged(coin);
			return coin;
		}
	}
}

