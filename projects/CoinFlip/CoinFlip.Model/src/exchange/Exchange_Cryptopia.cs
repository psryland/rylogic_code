using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Cryptopia.API;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>Cryptopia Exchange</summary>
	public class Cryptopia :Exchange
	{
		private HashSet<int> m_pair_ids;

		public Cryptopia(Model model, string key, string secret)
			:base(model, model.Settings.Cryptopia)
		{
			m_pair_ids = new HashSet<int>();
			Api = new CryptopiaApi(key, secret, Model.Shutdown);
			TradeHistoryUseful = false;
		}
		public override void Dispose()
		{
			Api = null;
			base.Dispose();
		}

		/// <summary>The API interface</summary>
		private CryptopiaApi Api
		{
			[DebuggerStepThrough] get { return m_api; }
			set
			{
				if (m_api == value) return;
				if (m_api != null)
				{
					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					m_api.ServerRequestRateLimit = Settings.ServerRequestRateLimit;
				}
			}
		}
		private CryptopiaApi m_api;

		/// <summary>Open a trade</summary>
		protected override TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			// Place the trade order
			var msg = Api.SubmitTrade(tt.ToCryptopiaTT(), pair.TradePairId.Value, volume, price);
			if (!msg.Success)
				throw new Exception(
					"Cryptopia: Submit order failed. {0}\n".Fmt(msg.Error) +
					"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, price));

			// Return the result
			// Cryptopia Hack: OrderId will be null if the order is filled immediately
			return new TradeResult(pair, (ulong)(msg.Data.OrderId ?? 0), msg.Data.FilledOrders.Select(x => (ulong)x));
		}

		/// <summary>Cancel an open trade</summary>
		protected override void CancelOrderInternal(TradePair pair, ulong order_id)
		{
			var msg = Api.CancelTrade(ECancelTradeType.Trade, order_id:(int)order_id);
			if (!msg.Success && !Regex.IsMatch(msg.Error, @"Trade #\d+ does not exist"))
				throw new Exception(
					"Cryptopia: Cancel trade failed. {0}\n".Fmt(msg.Error) +
					"Order Id: {0}".Fmt(order_id));
		}

		/// <summary>Update the collections of coins and pairs</summary>
		protected override void UpdatePairsInternal(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = Api.GetTradePairs();
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to read available trading pairs. {0}".Fmt(msg.Error));

				// Add an action to add/update the pairs,coins
				Model.MarketUpdates.Add(() =>
				{
					var ids = new HashSet<int>();

					// Create/Update the trade pairs and associated coins
					var pairs = msg.Data.Where(x => coi.Contains(x.SymbolBase) && coi.Contains(x.SymbolQuote));
					foreach (var p in pairs)
					{
						var base_ = Coins.GetOrAdd(p.SymbolBase);
						var quote = Coins.GetOrAdd(p.SymbolQuote);

						// Update the trade pairs
						var instr = new TradePair(base_, quote, this, p.Id,
							volume_range_base:new RangeF<Unit<decimal>>(p.MinimumTradeBase ._(p.SymbolBase ), p.MaximumTradeBase ._(p.SymbolBase )),
							volume_range_quote:new RangeF<Unit<decimal>>(p.MinimumTradeQuote._(p.SymbolQuote), p.MaximumTradeQuote._(p.SymbolQuote)),
							price_range:null);
						Pairs[instr.UniqueKey] = instr;
						ids.Add(p.Id);
					}

					// Ensure a 'Balance' object exists for each coin type
					foreach (var c in Coins.Values)
						Balance.GetOrAdd(c);

					// Record the pair Ids
					lock (m_pair_ids)
						m_pair_ids.AddRange(ids);
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdatePairs), ex);
			}
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected override void UpdateData() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Get the trade pair ids
				var ids = (int[])null;
				lock (m_pair_ids)
					ids = m_pair_ids.ToArray();

				// Request the order book data for all of the pairs
				var order_book = ids.Length != 0 
					? Api.GetMarketOrderGroups(ids, order_count:Settings.MarketDepth)
					: new MarketOrderGroupsResponse();

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					var msg = order_book;
					if (!msg.Success)
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

					// Notify updated
					MarketDataUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateData), ex);
			}
		}

		/// <summary>Update account balance data</summary>
		protected override void UpdateBalances() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the account data
				var msg = Api.GetBalances();
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to update account balances. {0}".Fmt(msg.Error));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Update the account balance
					using (Model.Balances.PreservePosition())
					{
						foreach (var b in msg.Data.Where(x => x.Available != 0 || Coins.ContainsKey(x.Symbol)))
						{
							// Find the currency that this balance is for
							var coin = Coins.GetOrAdd(b.Symbol);

							// Update the balance
							var bal = new Balance(coin, b.Total, b.Available, b.Unconfirmed, b.HeldForTrades, b.PendingWithdraw, timestamp);
							Balance[coin] = bal;
						}
					}

					// Notify updated
					BalanceUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateBalances), ex);
			}
		}

		/// <summary>Update open positions</summary>
		protected override void UpdatePositions() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the existing orders
				var msg = Api.GetOpenOrders();
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to received existing orders. {0}".Fmt(msg.Error));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					var order_ids = new HashSet<ulong>();
					var pair_ids = new HashSet<int>();

					// Update the collection of existing orders
					foreach (var order in msg.Data)
					{
						// Add the position to the collection
						var pos = PositionFrom(order, timestamp);
						Positions[pos.OrderId] = pos;
						order_ids.Add(pos.OrderId);
						pair_ids.Add(pos.Pair.TradePairId.Value);
					}

					// Remove any positions that are no longer valid.
					RemovePositionsNotIn(order_ids, timestamp);

					// Notify updated
					PositionUpdatedTime.NotifyAll(timestamp);

					// Update the trade pair ids
					lock (m_pair_ids)
						m_pair_ids.AddRange(pair_ids);
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdatePositions), ex);
			}
		}

		/// <summary>Update the trade history</summary>
		protected override void UpdateTradeHistory() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the history
				var msg = Api.GetTradeHistory(count:10);
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to received trade history. {0}".Fmt(msg.Error));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					foreach (var order in msg.Data)
					{
						var his = HistoricFrom(order, timestamp);

						// Hack until history contains original order id
						var fill = History.Values.FirstOrDefault(x => x.Pair == his.Pair && x.Created == his.Created);
						if (fill == null) fill = History.GetOrAdd(his.TradeId, his.TradeType, his.Pair);
						his.OrderIdHACK = fill.OrderId;
						fill.Trades[his.TradeId] = his;

						//var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair);
						//fill.Trades[his.TradeId] = his;
					}

					// Save the history range
					HistoryInterval = new Range(HistoryInterval.Beg, timestamp.Ticks);

					// Notify updated
					TradeHistoryUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateTradeHistory), ex);
			}
		}

		/// <summary>Set the maximum number of requests per second to the exchange server</summary>
		protected override void SetServerRequestRateLimit(float limit)
		{
			Api.ServerRequestRateLimit = limit;
		}

		/// <summary>Convert a Cryptopia open order result into a position object</summary>
		private Position PositionFrom(OpenOrder order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id = unchecked((ulong)order.OrderId);
			var sym = order.Market.Split('/');
			var pair = Pairs.GetOrAdd(sym[0], sym[1], trade_pair_id:order.TradePairId);
			var rate = order.Rate._(pair.RateUnits);
			var volume = order.Amount._(pair.Base);
			var remaining = order.Remaining._(pair.Base);
			var created = order.TimeStamp.As(DateTimeKind.Utc);
			return new Position(order_id, pair, Misc.TradeType(order.Type), rate, volume, remaining, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private Historic HistoricFrom(TradeHistory his, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id   = unchecked((ulong)his.OrderId);
			var trade_id   = unchecked((ulong)his.TradeId);
			var tt         = Misc.TradeType(his.Type);
			var sym        = CurrencyPair.Parse(his.Market);
			var pair       = Pairs.GetOrAdd(sym.Base, sym.Quote, trade_pair_id:his.TradePairId);
			var price      = tt == ETradeType.B2Q ? his.Rate._(pair.RateUnits) : (1m / his.Rate._(pair.RateUnits));
			var volume_in  = tt == ETradeType.B2Q ? his.Amount._(pair.Base)    : (his.Total._(pair.Quote) + his.Fee._(pair.Quote));
			var volume_out = tt == ETradeType.B2Q ? his.Total._(pair.Quote)    : (volume_in * price);
			var commission = tt == ETradeType.B2Q ? his.Fee._(pair.Quote)      : (volume_out - his.Amount._(pair.Base));
			var created    = his.TimeStamp.As(DateTimeKind.Utc);
			return new Historic(order_id, trade_id, pair, tt, price, volume_in, volume_out, commission, created, updated);
		}
	}
}

