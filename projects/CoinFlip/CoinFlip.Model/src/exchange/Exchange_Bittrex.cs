using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Bittrex.API;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class Bittrex :Exchange
	{
		private BiDictionary<ulong, Guid> m_order_id_lookup;

		public Bittrex(Model model, string key, string secret)
			:base(model, model.Settings.Bittrex)
		{
			m_order_id_lookup = new BiDictionary<ulong, Guid>();

			Api = new BittrexApi(key, secret, Model.Shutdown);
			TradeHistoryUseful = true;
		}
		public override void Dispose()
		{
			Api = null;
			base.Dispose();
		}

		/// <summary>The public data interface</summary>
		private BittrexApi Api
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
		private BittrexApi m_api;

		/// <summary>Open a trade</summary>
		protected override TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			try
			{
				// Place the trade order
				var res = Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToBittrexTT(tt), price, volume);
				if (!res.Success)
					throw new Exception(res.Message);

				var order_id = ToOrderId(res.Data.Id);
				return new TradeResult(pair, order_id);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Bittrex: Submit trade failed. {0}\n".Fmt(ex.Message) +
					"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, price), ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected override void CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				var uuid = ToUuid(order_id);
				var msg = Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
				if (!msg.Success && msg.Message != "ORDER_NOT_OPEN")
					throw new Exception(msg.Message);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Bittrex: Cancel trade failed. {0}\n".Fmt(ex.Message) +
					"Order Id: {0}".Fmt(order_id));
			}
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override void UpdatePairsInternal(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = Api.GetMarkets();
				if (!msg.Success)
					throw new Exception("Bittrex: Failed to read market data. {0}".Fmt(msg.Message));

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					// Create the trade pairs and associated coins
					var markets = msg.Data;
					foreach (var m in markets.Where(x => coi.Contains(x.Pair.Base) && coi.Contains(x.Pair.Quote)))
					{
						var base_ = Coins.GetOrAdd(m.Pair.Base);
						var quote = Coins.GetOrAdd(m.Pair.Quote);

						// Add/Update the trade pair.
						// Note: m.MinTradeSize is not valid, 50,000 Satoshi is the minimum trade size
						var instr = new TradePair(base_, quote, this,
							trade_pair_id:null,
							volume_range_base: new RangeF<Unit<decimal>>(0.0005m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0005m._(quote), 10000000m._(quote)),
							price_range:null);
						Pairs[instr.UniqueKey] = instr;
					}

					// Ensure a 'Balance' object exists for each coin type
					foreach (var c in Coins.Values)
						Balance.GetOrAdd(c);
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

				// Request order book data for all of the pairs
				var order_books = new List<Task<OrderBookResponse>>();
				foreach (var pair in Pairs.Values)
					order_books.Add(Api.GetOrderBookAsync(new CurrencyPair(pair.Base, pair.Quote), BittrexApi.EGetOrderBookType.Both, depth:Settings.MarketDepth));

				// Wait for replies for all queries
				Task.WhenAll(order_books).Wait();

				// Check the responses
				foreach (var ob in order_books)
				{
					if (!ob.Result.Success)
						throw new Exception("Bittrex: Failed to update trading pairs. {0}".Fmt(ob.Result.Message));
				}

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var ob in order_books)
					{
						Debug.Assert(ob.Result.Success);
						var orders = ob.Result.Data;
						var cp = orders.Pair;

						// Find the pair to update.
						var pair = Pairs[cp.Base, cp.Quote];
						if (pair == null)
							continue;

						// Update the depth of market data
						var buys  = orders.BuyOrders .Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
						var sells = orders.SellOrders.Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
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
					throw new Exception("Bittrex: Failed to read account balances. {0}".Fmt(msg.Message));

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
							var bal = new Balance(coin, b.Total, b.Available, b.Pending, b.Total - b.Available, 0, timestamp);
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
					throw new Exception("Bittrex: Failed to read open orders. {0}".Fmt(msg.Message));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Update the collection of existing orders
					var order_ids = new HashSet<ulong>();
					foreach (var order in msg.Data)
					{
						// Add the position to the collection
						var pos = PositionFrom(order, timestamp);
						Positions[pos.OrderId] = pos;
						order_ids.Add(pos.OrderId);
					}

					// Remove any positions that are no longer valid.
					RemovePositionsNotIn(order_ids, timestamp);

					// Notify updated
					PositionUpdatedTime.NotifyAll(timestamp);
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
				var msg = Api.GetTradeHistory();
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to received trade history. {0}".Fmt(msg.Message));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					foreach (var order in msg.Data)
					{
						var his = HistoricFrom(order, timestamp);
						var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair);
						fill.Trades[his.TradeId] = his;
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
		private Position PositionFrom(global::Bittrex.API.Position order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id = ToOrderId(order.OrderId);
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var price = order.Limit._(pair.RateUnits);
			var volume = order.VolumeBase._(pair.Base);
			var remaining = order.RemainingBase._(pair.Base);
			var created = order.Created;
			return new Position(order_id, pair, Misc.TradeType(order.Type), price, volume, remaining, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private Historic HistoricFrom(global::Bittrex.API.Historic his, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			// Bittrex doesn't use trade ids. Make them up using the Remaining
			// volume so that each trade for a given order is unique (ish).
			var order_id    = ToOrderId(his.OrderId);
			var trade_id    = unchecked((ulong)(his.RemainingBase * 1_000_000 / his.QuantityBase));
			var tt          = Misc.TradeType(his.Type);
			var pair        = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var price       = tt == ETradeType.B2Q ? his.PricePerUnit._(pair.RateUnits) : (1m / his.PricePerUnit._(pair.RateUnits));
			var volume_in   = tt == ETradeType.B2Q ? his.FilledBase._(pair.Base)        : (his.FilledQuote + his.Commission)._(pair.Quote);
			var volume_out  = tt == ETradeType.B2Q ? his.FilledQuote._(pair.Quote)      : (his.FilledBase)._(pair.Base);
			var commission  = tt == ETradeType.B2Q ? his.Commission._(pair.Quote)       : (his.Commission / his.PricePerUnit)._(pair.Base);
			var created     = his.Created;
			return new Historic(order_id, trade_id, pair, tt, price, volume_in, volume_out, commission, created, updated);
		}

		/// <summary>Convert a bittrex UUID to a CoinFlip order id (ULONG)</summary>
		private ulong ToOrderId(Guid guid)
		{
			// Create a new order if for GUID's we haven't seen before
			if (!m_order_id_lookup.TryGetValue(guid, out var order_id))
			{
				// Try to make a number that matches the GUID (for debugging helpfulness)
				order_id = BitConverter.ToUInt32(guid.ToByteArray(), 0);

				// Handle key collisions
				for (; m_order_id_lookup.ContainsKey(order_id); ++order_id) {}
				m_order_id_lookup[guid] = order_id;
			}
			return order_id;
		}

		/// <summary>Convert a CoinFlip order id (ULONG) to a bittrex UUID</summary>
		private Guid ToUuid(ulong order_id)
		{
			return m_order_id_lookup[order_id];
		}
	}
}
