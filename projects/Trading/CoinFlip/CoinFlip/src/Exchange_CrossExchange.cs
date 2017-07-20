using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using pr.container;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>A meta exchange that facilitates converting currency on one exchange to the same currency on another exchange</summary>
	public class CrossExchange :Exchange
	{
		public CrossExchange(Model model)
			:base(model, 0m, 1000)
		{
		}

		/// <summary>Open a trade</summary>
		protected override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			return Misc.CompletedTask;
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		public override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			Model.MarketUpdates.Add(() =>
			{
				// Remove any coins that don't involve the coins of interest
				var old_coins = Coins.Values.Where(x => !coi.Contains(x.Symbol)).ToArray();
				foreach (var coin in old_coins)
					Coins.Remove(coin);

				// Remove any existing pairs that don't involve coins of interest
				var old_pairs = Pairs.Values.Where(x => !coi.Contains(x.Base) || !coi.Contains(x.Quote)).ToArray();
				foreach (var pair in old_pairs)
					Pairs.Remove(pair);

				// Create cross-exchange pairs for each coin of interest
				foreach (var sym in Model.CoinsOfInterest)
				{
					// Find the exchanges that have this coin
					var exchanges = Model.Exchanges.Where(x => !(x is CrossExchange) && x.Coins.ContainsKey(sym)).ToArray();

					// Create trading pairs between the same currencies on different exchanges
					for (int j = 0; j < exchanges.Length - 1; ++j)
					{
						for (int i = j + 1; i < exchanges.Length; ++i)
						{
							// Check whether the pair already exists
							var exch0 = exchanges[j];
							var exch1 = exchanges[i];
							var pair = Pairs[sym, exch0, exch1];

							// If not, added it
							if (pair == null)
							{
								pair = new TradePair(exch0.Coins[sym], exch1.Coins[sym], this);
								Pairs.Add(pair);

								// Add the coins
								Coins[pair.Base.SymbolWithExchange] = pair.Base;
								Coins[pair.Quote.SymbolWithExchange] = pair.Quote;
							}
						}
					}
				}
			});
			return Misc.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected override Task UpdateData() // Worker thread context
		{
			try
			{
				Model.MarketUpdates.Add(() =>
				{
					// Update the order book for each pair
					#region Order Book
					{
						foreach (var pair in Pairs.Values)
						{
							// Get the coins in the pair
							var coin0 = pair.Base;
							var coin1 = pair.Quote;

							// Each order book as one entry for infinite volume.
							// Available balance is applied after the loop is identified.
							var buys  = new[]{ new Order(1m._(), decimal.MaxValue._(coin0)) };
							var sells = new[]{ new Order(1m._(), decimal.MaxValue._(coin1)) };
							pair.UpdateOrderBook(buys, sells);
						}
					}
					#endregion

					// Update the Balances data
					#region Balance
					{
						using (Model.Balances.PreservePosition())
						{
							foreach (var pair in Pairs.Values)
							{
								Balance[pair.Base ] = pair.Base.Balance;
								Balance[pair.Quote] = pair.Quote.Balance;
							}
						}
					}
					#endregion

					// Update current orders
					#region Current Orders
					{
						// There shouldn't be any of these. Cross-exchange trades
						// are virtual, we only pretend to convert 'Coin' on 'Exchange0'
						// to 'Coin' on 'Exchange1' (or visa versa)
					}
					#endregion
				});

				Status = EStatus.Connected;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "Cryptopia UpdateData() failed");
				Status = EStatus.Error;
			}
			return Misc.CompletedTask;
		}
	}
}

