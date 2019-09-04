using System;
using System.Diagnostics;
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
			: base(exch)
		{
			KeyFrom = x => x.Coin;
			SupportsSorting = false;
		}

		/// <summary>Get or add a coin type that there is a balance for on the exchange</summary>
		public Balances GetOrAdd(Coin coin)
		{
			Debug.Assert(Misc.AssertMarketDataWrite());
			return this.GetOrAdd(coin, x => new Balances(x, DateTimeOffset.MinValue));
		}

		/// <summary>Get/Set the balance for the given coin. Returns zero balance for unknown coins</summary>
		public override Balances this[Coin coin]
		{
			get
			{
				Debug.Assert(Misc.AssertMarketDataRead());
				if (coin.Exchange != Exchange && !(Exchange is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				return TryGetValue(coin, out var bal) ? bal : new Balances(coin, Model.UtcNow);
			}
		}

		/// <summary>Get the balance by coin symbol name</summary>
		public Balances Get(string sym)
		{
			// Don't provide this: 'public Balance this[string sym]', the Coin implicit cast is favoured over the overloaded method
			var coin = Exchange.Coins[sym];
			return this[coin];
		}

		/// <summary>Apply an update from an exchange for 'coin'</summary>
		public void ExchangeUpdate(Coin coin, Unit<double> total, Unit<double> held, DateTimeOffset update_time)
		{
			Debug.Assert(Misc.AssertMainThread());

			// Check the assigned balance info is for this exchange
			if (coin.Exchange != Exchange && !(Exchange is CrossExchange))
				throw new Exception("Currency not associated with this exchange");

			// Get the balances associated with 'coin'
			var balances = GetOrAdd(coin);
			balances.ExchangeUpdate(total, held, update_time);

			// Invalidate bindings
			ResetItem(balances);
		}
	}
}
