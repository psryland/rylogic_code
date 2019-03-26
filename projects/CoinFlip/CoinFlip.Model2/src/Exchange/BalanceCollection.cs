using System;
using System.Diagnostics;
using Rylogic.Extn;

namespace CoinFlip
{
	public class BalanceCollection : CollectionBase<Coin, Balances>
	{
		// Notes:
		//  - BalanceCollection is all the balances of the coins associated with an exchange.
		//  - Balances are per-coin and are the amounts assigned to each fund.
		//  - FundBalance is the single balance for a coin in a given fund.
		//  - Fund is a string ID for a named partition of money. Funds are used by
		//    bots to give predictability to account balances.

		public BalanceCollection(Exchange exch)
			: base(exch)
		{
			KeyFrom = x => x.Coin;
			SupportsSorting = false;
		}
		public BalanceCollection(BalanceCollection rhs)
			: base(rhs)
		{ }

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
				if (coin.Exchange != Exch && !(Exch is CrossExchange)) throw new Exception("Currency not associated with this exchange");
				return TryGetValue(coin, out var bal) ? bal : new Balances(coin, Model.UtcNow);
			}
			set
			{
				Debug.Assert(Misc.AssertMarketDataWrite());
				Debug.Assert(value != null && value.AssertValid());

				if (coin.Exchange != Exch && !(Exch is CrossExchange))
					throw new Exception("Currency not associated with this exchange");

				// Ignore out-of-date data
				if (TryGetValue(coin, out var balances) && balances.LastUpdated > value.LastUpdated)
					return;

				// Add the balances for 'coin'
				if (balances != null)
				{
					balances.Update(value);
					ResetItem(balances);
				}
				else
				{
					base[coin] = value;
				}

				// Broadcast that the balance of this coin has changed
				coin.Meta.NotifyBalanceChanged();
			}
		}

		/// <summary>Get the balance by coin symbol name</summary>
		public Balances Get(string sym)
		{
			// Don't provide this, the Coin implicit cast is favoured over the overloaded method
			//@"public Balance this[string sym]"
			var coin = Exch.Coins[sym];
			return this[coin];
		}
	}
}
