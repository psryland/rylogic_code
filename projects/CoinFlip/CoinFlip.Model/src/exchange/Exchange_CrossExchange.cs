﻿using System;
using System.Collections.Generic;
using System.Linq;
using pr.util;

namespace CoinFlip
{
	/// <summary>A meta exchange that facilitates converting currency on one exchange to the same currency on another exchange</summary>
	public class CrossExchange :Exchange
	{
		public CrossExchange(Model model)
			:base(model, model.Settings.CrossExchange)
		{}

		/// <summary>Open a trade</summary>
		protected override TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			return new TradeResult(pair);
		}

		/// <summary>Cancel an open trade</summary>
		protected override bool CancelOrderInternal(TradePair pair, ulong order_id)
		{
			throw new Exception("Cannot cancel trades on the CrossExchange");
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override void UpdatePairsInternal(HashSet<string> coi) // Worker thread context
		{
			Model.MarketUpdates.Add(() =>
			{
				// Create cross-exchange pairs for each coin of interest
				foreach (var cd in Model.Coins.Where(x => x.OfInterest))
				{
					var sym = cd.Symbol;

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
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected override void UpdateData() // Worker thread context
		{
			try
			{
				Model.MarketUpdates.Add(() =>
				{
					// Update the order book for each pair
					foreach (var pair in Pairs.Values)
					{
						// Get the coins in the pair
						var coin0 = pair.Base;
						var coin1 = pair.Quote;

						// Each order book as one entry for infinite volume.
						// Available balance is applied after the loop is identified.
						var buys  = new[]{ new Order(1m._(), decimal.MaxValue._(coin0)) };
						var sells = new[]{ new Order(1m._(), decimal.MaxValue._(coin1)) };
						pair.MarketDepth.UpdateOrderBook(buys, sells);

						// Notify updated
						Pairs.LastUpdated = DateTimeOffset.Now;
					}
				});
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "CrossExchange UpdateData() failed");
			}
		}

		/// <summary>Update account balance data</summary>
		protected override void UpdateBalances() // Worker thread context
		{
			try
			{
				Model.MarketUpdates.Add(() =>
				{
					// Update the Balances data
					foreach (var pair in Pairs.Values)
					{
						Balance[pair.Base ] = pair.Base.Balance;
						Balance[pair.Quote] = pair.Quote.Balance;
					}

					// Notify updated
					Balance.LastUpdated = DateTimeOffset.Now;
				});
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "CrossExchange UpdateBalances() failed");
			}
		}

		/// <summary>Update open positions</summary>
		protected override void UpdatePositionsAndHistory() // Worker thread context
		{
			// There shouldn't be any of these. Cross-exchange trades
			// are virtual, we only pretend to convert 'Coin' on 'Exchange0'
			// to 'Coin' on 'Exchange1' (or visa versa)
			base.UpdatePositionsAndHistory();
		}

		/// <summary>Set the maximum number of requests per second to the exchange server</summary>
		protected override void SetServerRequestRateLimit(float limit)
		{
		}
	}
}
