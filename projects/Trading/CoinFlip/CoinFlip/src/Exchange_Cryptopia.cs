using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cryptopia.API;
using Cryptopia.API.DataObjects;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>Cryptopia Exchange</summary>
	public class Cryptopia :Exchange
	{
		private const string ApiKey    = "429162d7138f4275b5e9dd09ff6362fd";
		private const string ApiSecret = "Dt6k0ZC3zqbNCxpDSsHqX7VIR21PEPO7vNeKDbQIKcI=";

		private CryptopiaApiPublic Pub;
		private CryptopiaApiPrivate Priv;
		private List<int> m_pair_ids;

		public Cryptopia(Model model)
			:base(model, 0.002m, model.Settings.Cryptopia.PollPeriod)
		{
			Pub = new CryptopiaApiPublic();
			Priv = new CryptopiaApiPrivate(ApiKey, ApiSecret);
			m_pair_ids = new List<int>();
		}

		/// <summary>Open a trade</summary>
		protected async override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			// Obey the global trade switch
			if (Model.AllowTrades)
			{
				// Place the trade order
				var msg = await Priv.SubmitTrade(new SubmitTradeRequest(pair.TradePairId.Value, tt.ToCryptopiaTT(), volume, rate));
				if (!msg.Success)
					throw new Exception("Cryptopia: Submit trade failed. {0}".Fmt(msg.Error));
			}
		}

		/// <summary>Update the collections of coins and pairs</summary>
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			// Get all available trading pairs
			var msg = await Pub.GetTradePairs();
			if (!msg.Success)
				throw new Exception("Cryptopia: Failed to read available trading pairs. {0}".Fmt(msg.Error));

			// Add an action to add/update the pairs,coins
			Model.MarketUpdates.Add(() =>
			{
				var nue = new HashSet<string>();

				// Create the trade pairs and associated coins
				var pairs = msg.Data.Where(x => coi.Contains(x.SymbolBase) && coi.Contains(x.SymbolQuote));
				foreach (var p in pairs)
				{
					var base_ = Coins.GetOrAdd(p.SymbolBase);
					var quote = Coins.GetOrAdd(p.SymbolQuote);

					// Add the trade pair.
					var instr = new TradePair(base_, quote, this, p.Id,
						volume_range_base:new RangeF<Unit<decimal>>(p.MinimumTradeBase ._(p.SymbolBase ), p.MaximumTradeBase ._(p.SymbolBase )),
						volume_range_quote:new RangeF<Unit<decimal>>(p.MinimumTradeQuote._(p.SymbolQuote), p.MaximumTradeQuote._(p.SymbolQuote)),
						price_range:null);
					Pairs[instr.UniqueKey] = instr;

					// Save the names of the pairs,coins returned
					nue.Add(instr.Name);
					nue.Add(instr.Base.Symbol);
					nue.Add(instr.Quote.Symbol);
				}

				// Remove pairs not in 'nue'
				Pairs.RemoveIf(p => !nue.Contains(p.Name));

				// Remove coins not in 'nue'
				Coins.RemoveIf(c => !nue.Contains(c.Symbol));

				// Ensure a 'Balance' object exists for each coin type
				foreach (var c in Coins.Values)
					Balance.GetOrAdd(c);

				// Record the pair Ids
				lock (m_pair_ids)
				{
					m_pair_ids.Clear();
					m_pair_ids.AddRange(Pairs.Values.Select(x => x.TradePairId.Value));
				}
			});
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateData() // Worker thread context
		{
			try
			{
				// Get the trade pair ids
				int[] ids;
				lock (m_pair_ids)
					ids = m_pair_ids.ToArray();

				// Request the order book data for all of the pairs
				var order_book = ids.Length != 0 
					? Pub.GetMarketOrderGroups(new MarketOrderGroupsRequest(ids))
					: Task.FromResult<MarketOrderGroupsResponse>(null);

				// Request the account data
				var balance_data = Priv.GetBalances(new BalanceRequest());

				// Request the existing orders
				var existing_orders = Priv.GetOpenOrders(new OpenOrdersRequest());

				// Wait for replies for all queries
				await Task.WhenAll(order_book, balance_data, existing_orders);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					#region Order Book
					{
						var msg = order_book.Result;
						if (msg == null || !msg.Success)
							throw new Exception("Cryptopia: Failed to update trading pairs. {0}".Fmt(msg?.Error ?? string.Empty));

						// Update the market orders
						foreach (var orders in msg.Data)
						{
							// Get the currencies involved in the pair
							var coin = orders.Market.Split('_');

							// Find the pair to update.
							// The pair may have been removed between the request and the reply.
							// Pair's may have been added as well, we'll get those next heart beat.
							var pair = Pairs[coin[0], coin[1]];
							if (pair == null)
								continue;

							// Update the depth of market data
							var buys  = orders.Buy .Select(x => new Order(x.Price._(pair.RateUnits), x.Volume._(pair.Base))).ToArray();
							var sells = orders.Sell.Select(x => new Order(x.Price._(pair.RateUnits), x.Volume._(pair.Base))).ToArray();
							pair.UpdateOrderBook(buys, sells);
						}
					}
					#endregion

					// Process the account data and update the balances
					#region Balance
					{
						var msg = balance_data.Result;
						if (!msg.Success)
							throw new Exception("Cryptopia: Failed to update account balances pairs. {0}".Fmt(msg.Error));

						// Update the account balance
						using (Model.Balances.PreservePosition())
						{
							foreach (var b in msg.Data.Where(x => x.Available != 0 || Coins.ContainsKey(x.Symbol)))
							{
								// Find the currency that this balance is for
								var coin = Coins.GetOrAdd(b.Symbol);

								// Update the balance
								var bal = new Balance(coin, b.Total, b.Available, b.Unconfirmed, b.HeldForTrades, b.PendingWithdraw);
								Balance[coin] = bal;
							}
						}
					}
					#endregion

					// Process the existing orders
					#region Current Orders
					{
						var msg = existing_orders.Result;
						if (!msg.Success)
							throw new Exception("Cryptopia: Failed to received existing orders. {0}".Fmt(msg.Error));

						// Update the collection of existing orders
						var order_ids = new HashSet<ulong>();
						using (Model.Positions.PreservePosition())
						{
							foreach (var order in msg.Data)
							{
								// Get the associated trade pair (add the pair if it doesn't exist)
								var id = unchecked((ulong)order.OrderId);
								var sym = order.Market.Split('/');
								var pair = Pairs[sym[0], sym[1]] ?? Pairs.Add2(new TradePair(Coins.GetOrAdd(sym[0]), Coins.GetOrAdd(sym[1]), this, order.TradePairId));
								var rate = order.Rate._(pair.RateUnits);
								var volume = order.Amount._(pair.Base);
								var remaining = order.Remaining._(pair.Base);
								var ts = order.TimeStamp.As(DateTimeKind.Utc);

								// Add the position to the collection
								var pos = new Position(id, pair, Misc.TradeType(order.Type), rate, volume, remaining, ts);
								Positions[id] = pos;
								order_ids.Add(id);
							}

							// Remove any positions that are no longer valid
							foreach (var id in Positions.Keys.Where(x => !order_ids.Contains(x)).ToArray())
								Positions.Remove(id);
						}
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
		}
	}
}

