using System;
using System.Diagnostics;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class BalanceCollection : CollectionBase<Coin, Balances>
	{
		// Notes:
		//  - BalanceCollection is all the balances of the coins associated with an exchange.
		//  - Balances are per-coin and are the amounts assigned to each fund.
		//  - IBalance is the single balance for a coin in a given fund.
		//  - Fund is a string ID for a named partition of money. Funds are used by bots to give predictability to account balances.

		public BalanceCollection(Exchange exch)
			: base(exch, x => x.Coin)
		{
			Balances.SupportsSorting = false;
		}

		/// <summary></summary>
		private BindingDict<Coin, Balances> Balances => m_data;

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			Balances.Clear();
		}

		/// <summary>Add to this collection</summary>
		public Balances Add(Balances balances)
		{
			return Balances.Add2(balances);
		}

		/// <summary>Get or add a coin type that there is a balance for on the exchange</summary>
		public Balances GetOrAdd(Coin coin)
		{
			Debug.Assert(Misc.AssertMainThread());
			return Balances.GetOrAdd(coin, x => new Balances(x, DateTimeOffset.MinValue));
		}

		/// <summary>Get/Set the balance for the given coin. Returns zero balance for unknown coins</summary>
		public Balances this[Coin coin]
		{
			get
			{
				Debug.Assert(Misc.AssertMainThread());
				if (coin.Exchange != Exchange && !(Exchange is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				return Balances.TryGetValue(coin, out var bal) ? bal : new Balances(coin, Model.UtcNow);
			}
		}

		/// <summary>Get the balance by coin symbol name</summary>
		public Balances? Find(string sym)
		{
			// Don't provide this: 'public Balance this[string sym]', the Coin implicit cast is favoured over the overloaded method
			var coin = Exchange.Coins[sym];
			return coin != null ? this[coin] : null;
		}

		/// <summary>Apply an update from an exchange for 'coin'</summary>
		public void ExchangeUpdate(Coin coin, Unit<decimal> total, Unit<decimal> held, DateTimeOffset update_time)
		{
			Debug.Assert(Misc.AssertMainThread());

			// Check the assigned balance info is for this exchange
			if (coin.Exchange != Exchange && !(Exchange is CrossExchange))
				throw new Exception("Currency not associated with this exchange");

			// Get the balances associated with 'coin'
			var balances = GetOrAdd(coin);
			balances.ExchangeUpdate(total, held, update_time);

			// Invalidate bindings
			Balances.ResetItem(balances);
		}
	}
}
