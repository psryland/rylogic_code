﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip.Settings;
using ExchApi.Common;
using Rylogic.Utility;

namespace CoinFlip
{
	public class CrossExchange : Exchange
	{
		public CrossExchange(IList<Exchange> trading_exchanges, CoinDataList coin_data, CancellationToken shutdown)
			: base(SettingsData.Settings.CrossExchange, coin_data, shutdown)
		{
			Api = new CrossExchangeApi(shutdown);
			Exchanges = trading_exchanges;
		}

		/// <summary>The other available exchanges</summary>
		private IList<Exchange> Exchanges { get; }

		/// <summary>Api</summary>
		private CrossExchangeApi Api { get; }
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			Model.DataUpdates.Add(() =>
			{
				// Create cross-exchange pairs for each coin of interest
				foreach (var cd in SettingsData.Settings.Coins.Where(x => x.CreateCrossExchangePairs))
				{
					var sym = cd.Symbol;

					// Find the exchanges that have this coin
					var exchanges = Exchanges.Where(x => x.Coins.ContainsKey(sym)).ToArray();

					// Create trading pairs between the same currencies on different exchanges
					for (int j = 0; j < exchanges.Length - 1; ++j)
					{
						for (int i = j + 1; i < exchanges.Length; ++i)
						{
							// Check whether the pair already exists
							var exch0 = exchanges[j];
							var exch1 = exchanges[i];
							var pair = Pairs[sym, exch0, exch1];
							if (pair != null) continue;

							// If not, add it
							var coin0 = exch0.Coins[sym] ?? throw new NullReferenceException($"Coin '{sym}' not found on {exch0.Name}");
							var coin1 = exch1.Coins[sym] ?? throw new NullReferenceException($"Coin '{sym}' not found on {exch1.Name}");
							pair = new TradePair(coin0, coin1, this);
							Pairs.Add(pair);

							// Add the coins
							Coins.Add(pair.Base.SymbolWithExchange, pair.Base);
							Coins.Add(pair.Quote.SymbolWithExchange, pair.Quote);
						}
					}
				}
			});
			return Task.CompletedTask;
		}

		/// <summary>Update account balance data</summary>
		protected override Task UpdateBalancesInternal(HashSet<string> coins) // Worker thread context
		{
			Model.DataUpdates.Add(() =>
			{
				// Update the Balances data
				foreach (var pair in Pairs)
				{
					Balance.Add(pair.Base.Balances);
					Balance.Add(pair.Quote.Balances);
				}

				// Notify updated
				Balance.LastUpdated = DateTimeOffset.Now;
			});
			return Task.CompletedTask;
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected override Task UpdateDataInternal() // Worker thread context
		{
			Model.DataUpdates.Add(() =>
			{
				// Update the order book for each pair
				foreach (var pair in Pairs)
				{
					// Get the coins in the pair
					var coin0 = pair.Base;
					var coin1 = pair.Quote;

					// Each order book as one entry for infinite volume.
					// Available balance is applied after the loop is identified.
					var buys = new[] { new Offer(1m._(pair.RateUnits), decimal.MaxValue._(pair.Base)) };
					var sells = new[] { new Offer(1m._(pair.RateUnits), decimal.MaxValue._(pair.Base)) };
					pair.MarketDepth.UpdateOrderBooks(buys, sells);

					// Notify updated
					Pairs.LastUpdated = DateTimeOffset.Now;
				}
			});
			return Task.CompletedTask;
		}

		/// <summary>Cancel an open trade</summary>
		protected override Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			throw new Exception("Cannot cancel trades on the CrossExchange");
		}

		/// <summary>Open a trade</summary>
		protected override Task<OrderResult> CreateOrderInternal(Trade trade, CancellationToken cancel)
		{
			return Task.FromResult(new OrderResult(trade.Pair, false));
		}

		/// <summary>A mock for the exchange API</summary>
		private class CrossExchangeApi : IExchangeApi
		{
			public CrossExchangeApi(CancellationToken shutdown)
			{
				RequestThrottle = new RequestThrottle();
				Shutdown = shutdown;
			}
			public Task InitAsync()
			{
				return Task.CompletedTask;
			}

			/// <summary></summary>
			public RequestThrottle RequestThrottle { get; }

			/// <summary></summary>
			public CancellationToken Shutdown { get; }

			/// <summary></summary>
			public ClientWebSocket? WebSocket => null;
		}
	}
}
