using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
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
		private const string ApiKey    = "dc2582c37e354244a6056c76f9e8e1f7"; 
		private const string ApiSecret = "feb0ae42fcbf42fa8d42944fda308033";
		private BiDictionary<ulong, Guid> m_order_id_lookup;

		public Bittrex(Model model)
			:base(model, model.Settings.Bittrex)
		{
			Api = new BittrexApi(ApiKey, ApiSecret, Model.ShutdownToken);
			m_order_id_lookup = new BiDictionary<ulong, Guid>();

			// Start the exchange
			if (Model.Settings.Bittrex.Active)
				Model.RunOnGuiThread(() => Active = true);
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
				Util.Dispose(ref m_api);
				m_api = value;
			}
		}
		private BittrexApi m_api;

		/// <summary>Open a trade</summary>
		protected async override Task<TradeResult> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			try
			{
				// Place the trade order
				var res = await Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToBittrexTT(tt), price, volume);
				var order_id = ToOrderId(res.Data.Id);
				return new TradeResult(order_id);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Bittrex: Submit trade failed. {0}\n".Fmt(ex.Message) +
					"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, price), ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected async override Task CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				var uuid = ToUuid(order_id);
				var msg = await Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
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
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = await Api.GetMarkets();
				if (!msg.Success)
					throw new Exception("Bittrex: Failed to read market data. {0}".Fmt(msg.Message));

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					var nue = new HashSet<string>();
					var markets = msg.Data;

					// Create the trade pairs and associated coins
					foreach (var m in markets.Where(x => coi.Contains(x.Pair.Base) && coi.Contains(x.Pair.Quote)))
					{
						var base_ = Coins.GetOrAdd(m.Pair.Base);
						var quote = Coins.GetOrAdd(m.Pair.Quote);

						// Add the trade pair.
						var instr = new TradePair(base_, quote, this,
							trade_pair_id:null,
							volume_range_base: new RangeF<Unit<decimal>>(((decimal)m.MinTradeSize)._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(((decimal)m.MinTradeSize)._(quote), 10000000m._(quote)),
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
				});
			}
			catch (Exception ex)
			{
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Bittrex UpdatePairs() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateData() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request order book data for all of the pairs
				var order_books = new List<Task<OrderBookResponse>>();
				foreach (var pair in Pairs.Values)
					order_books.Add(Api.GetOrderBook(new CurrencyPair(pair.Base, pair.Quote), BittrexApi.EGetOrderBookType.Both, depth:20));

				// Wait for replies for all queries
				await Task.WhenAll(order_books);

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
						// The pair may have been removed between the request and the reply.
						// Pair's may have been added as well, we'll get those next heart beat.
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
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Bittrex UpdateData() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Update account balance data</summary>
		protected async override Task UpdateBalances() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the account data
				var msg = await Api.GetBalances();
				if (!msg.Success)
					throw new Exception("Bittrex: Failed to read account balances. {0}".Fmt(msg.Message));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Update the account balance
					using (Model.Balances.PreservePosition())
					{
						var nue = new HashSet<Coin>();
						foreach (var b in msg.Data.Where(x => x.Available != 0 || Coins.ContainsKey(x.Symbol)))
						{
							// Find the currency that this balance is for
							var coin = Coins.GetOrAdd(b.Symbol);

							// Update the balance
							var bal = new Balance(coin, b.Total, b.Available, b.Pending, b.Total - b.Available, 0, timestamp);
							Balance[coin] = bal;
							nue.Add(coin);
						}

						// Remove balances that aren't of interest
						Balance.RemoveIf(x => !nue.Contains(x.Coin));
					}

					// Notify updated
					BalanceUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Bittrex UpdateBalances() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Update open positions</summary>
		protected async override Task UpdatePositions() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the existing orders
				var msg = await Api.GetOpenOrders();
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
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Bittrex UpdatePositions() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Update the trade history</summary>
		protected async override Task UpdateTradeHistory() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the history
				var msg = await Api.GetTradeHistory();
				if (!msg.Success)
					throw new Exception("Cryptopia: Failed to received trade history. {0}".Fmt(msg.Message));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					foreach (var order in msg.Data)
					{
						var his = PositionFrom(order, timestamp);
						var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair, timestamp);
						fill.Trades[his.TradeId] = his;
					}

					// Notify updated
					TradeHistoryUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Cryptopia UpdateTradeHistory() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Convert a Cryptopia open order result into a position object</summary>
		private Position PositionFrom(global::Bittrex.API.Position order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id = ToOrderId(order.OrderId);
			var pair = Pairs[order.Pair.Base, order.Pair.Quote] ?? Pairs.Add2(new TradePair(Coins.GetOrAdd(order.Pair.Base), Coins.GetOrAdd(order.Pair.Quote), this));
			var price = order.Limit._(pair.RateUnits);
			var volume = order.VolumeBase._(pair.Base);
			var remaining = order.RemainingBase._(pair.Base);
			var created = order.Created;
			return new Position(order_id, 0, pair, Misc.TradeType(order.Type), price, volume, remaining, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private Position PositionFrom(global::Bittrex.API.Historic order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			// Bittrex doesn't use trade ids. Make them up using the Remaining
			// volume so that each trade for a given order is unique (ish).
			var order_id = ToOrderId(order.OrderId);
			var trade_id = unchecked((ulong)(order.RemainingBase * 1_000_000 / order.VolumeBase));
			var pair = Pairs[order.Pair.Base, order.Pair.Quote] ?? Pairs.Add2(new TradePair(Coins.GetOrAdd(order.Pair.Base), Coins.GetOrAdd(order.Pair.Quote), this));
			var price = order.Price._(pair.RateUnits);
			var volume = order.VolumeBase._(pair.Base);
			var created = order.Created;
			return new Position(order_id, trade_id, pair, Misc.TradeType(order.Type), price, volume, 0, created, updated);
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
