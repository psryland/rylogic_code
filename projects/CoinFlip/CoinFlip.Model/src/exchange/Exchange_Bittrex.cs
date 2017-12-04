using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Bittrex.API;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class Bittrex :Exchange
	{
		private readonly HashSet<CurrencyPair> m_pairs;
		private readonly Dictionary<ulong, Guid> m_order_id_lookup;

		public Bittrex(Model model, string key, string secret)
			:base(model, model.Settings.Bittrex)
		{
			m_pairs = new HashSet<CurrencyPair>();
			m_order_id_lookup = new Dictionary<ulong, Guid>();
			Api = new BittrexApi(key, secret, Model.Shutdown.Token);
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

				var order_id = ToIdPair(res.Data.Id).OrderId;
				return new TradeResult(pair, order_id, filled:false);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Submit trade failed. {ex.Message}\n{tt} Pair: {pair.Name}  Vol: {volume.ToString("G8",true)} @  {price.ToString("G8",true)}", ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected override bool CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				var uuid = ToUuid(order_id);
				var msg = Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
				if (msg.Success) return true;
				if (msg.Message == "ORDER_NOT_OPEN") return false;
				throw new Exception(msg.Message);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Cancel trade (id={order_id}) failed. {ex.Message}");
			}
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override void UpdatePairsInternal(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				// Use 'Model.Shutdown' because updating pairs is independent of the Exchange.UpdateThread
				// and we don't want updating pairs to be interrupted by the update thread stopping
				var msg = Api.GetMarkets(cancel:Model.Shutdown.Token);
				if (!msg.Success)
					throw new Exception("Bittrex: Failed to read market data. {0}".Fmt(msg.Message));

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					var pairs = new HashSet<CurrencyPair>();

					// Create the trade pairs and associated coins
					var markets = msg.Data;
					foreach (var m in markets.Where(x => coi.Contains(x.Pair.Base) && coi.Contains(x.Pair.Quote)))
					{
						var base_ = Coins.GetOrAdd(m.Pair.Base);
						var quote = Coins.GetOrAdd(m.Pair.Quote);

						// Create a trade pair. Note: m.MinTradeSize is not valid, 50,000 Satoshi is the minimum trade size
						var pair = new TradePair(base_, quote, this,
							trade_pair_id:null,
							volume_range_base: new RangeF<Unit<decimal>>(0.0005m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0005m._(quote), 10000000m._(quote)),
							price_range:null);

						// Update the pairs collection
						Pairs[pair.UniqueKey] = pair;
						pairs.Add(new CurrencyPair(base_, quote));
					}

					// Ensure a 'Balance' object exists for each coin type
					foreach (var c in Coins.Values)
						Balance.GetOrAdd(c);

					// Record the pairs
					lock (m_pairs)
					{
						m_pairs.Clear();
						m_pairs.AddRange(pairs);
					}
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

				// Get the array of pairs to query for
				CurrencyPair[] pairs;
				lock (m_pairs)
					pairs = m_pairs.ToArray();

				// Request order book data for all of the pairs
				var order_books = new List<OrderBookResponse>();
				foreach (var cp in pairs)
					order_books.Add(Api.GetOrderBook(cp, BittrexApi.EGetOrderBookType.Both, depth:Settings.MarketDepth, cancel:Shutdown.Token));

				// Check the responses
				foreach (var ob in order_books)
				{
					if (!ob.Success)
						throw new Exception("Bittrex: Failed to update trading pairs. {0}".Fmt(ob.Message));
				}

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var ob in order_books)
					{
						Debug.Assert(ob.Success);
						var orders = ob.Data;
						var cp = orders.Pair;

						// Find the pair to update.
						var pair = Pairs[cp.Base, cp.Quote];
						if (pair == null)
							continue;

						// Update the depth of market data
						var buys  = orders.BuyOrders .Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
						var sells = orders.SellOrders.Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
						pair.MarketDepth.UpdateOrderBook(buys, sells);
					}

					// Notify updated
					Pairs.LastUpdated = timestamp;
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
				var last_updated = DateTimeOffset.Now;

				// Request the account data
				var msg = Api.GetBalances(cancel:Shutdown.Token);
				if (!msg.Success)
					throw new Exception("Bittrex: Failed to read account balances. {0}".Fmt(msg.Message));

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Update the account balance
					foreach (var b in msg.Data.Where(x => x.Available != 0 || Coins.ContainsKey(x.Symbol)))
					{
						// Find the currency that this balance is for
						var coin = Coins.GetOrAdd(b.Symbol);

						// Update the balance
						Balance[coin] = new Balances(coin, b.Total._(coin), (b.Total - b.Available)._(coin), last_updated);
					}

					// Notify updated
					Balance.LastUpdated = last_updated;
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateBalances), ex);
			}
		}

		/// <summary>Update open positions</summary>
		protected override void UpdatePositionsAndHistory() // Worker thread context
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the existing orders
				var positions = Api.GetOpenOrders(cancel:Shutdown.Token);
				if (!positions.Success)
					throw new Exception("Bittrex: Failed to read open orders. {0}".Fmt(positions.Message));

				// Request the history
				var history = Api.GetTradeHistory(cancel:Shutdown.Token);
				if (!history.Success)
					throw new Exception("Cryptopia: Failed to received trade history. {0}".Fmt(history.Message));

				// Record the time that history has been updated to
				m_history_last = timestamp;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					var order_ids = new HashSet<ulong>();
					var pairs = new HashSet<CurrencyPair>();

					// Update the collection of existing orders
					foreach (var order in positions.Data)
					{
						// Add the position to the collection
						var pos = PositionFrom(order, timestamp);
						Positions[pos.OrderId] = pos;
						order_ids.Add(pos.OrderId);
						pairs.Add(order.Pair);
					}
					foreach (var order in history.Data)
					{
						// Add the filled positions to the collection
						var his = HistoricFrom(order, timestamp);
						var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair);
						fill.Trades[his.TradeId] = his;
						AddToTradeHistory(fill);
					}

					// Update the trade pairs
					lock (m_pairs)
						m_pairs.AddRange(pairs);

					// Remove any positions that are no longer valid.
					RemovePositionsNotIn(order_ids, timestamp);

					// Save the history range
					HistoryInterval = new Range(HistoryInterval.Beg, timestamp.Ticks);

					// Notify updated
					History.LastUpdated = timestamp;
					Positions.LastUpdated = timestamp;
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdatePositionsAndHistory), ex);
			}
		}

		/// <summary>Return the deposits and withdrawals made on this exchange</summary>
		protected override void UpdateTransfers() // worker thread context
		{
			try
			{
				var timestamp = DateTimeOffset.Now;

				// Request the transfers data
				var deposits = Api.GetDeposits(cancel:Shutdown.Token);
				if (!deposits.Success)
					throw new Exception("Bittrex: Failed to update deposits history. {0}".Fmt(deposits.Message));

				var withdrawals = Api.GetWithdrawals(cancel:Shutdown.Token);
				if (!withdrawals.Success)
					throw new Exception("Bittrex: Failed to update withdrawals history. {0}".Fmt(withdrawals.Message));

				// Record the time that transfer history has been updated to
				m_transfers_last = timestamp;

				// Queue integration of the transfer history
				Model.MarketUpdates.Add(() =>
				{
					// Update the collection of funds transfers
					foreach (var dep in deposits.Data)
					{
						var deposit = TransferFrom(dep, ETransfer.Deposit);
						Transfers[deposit.TransactionId] = deposit;
					}
					foreach (var wid in withdrawals.Data)
					{
						var withdrawal = TransferFrom(wid, ETransfer.Withdrawal);
						Transfers[withdrawal.TransactionId] = withdrawal;
					}

					// Save the available range
					TransfersInterval = new Range(TransfersInterval.Beg, timestamp.Ticks);

					// Notify updated
					Transfers.LastUpdated = timestamp;
				});
			}
			catch (Exception ex)
			{
				HandleException(nameof(UpdateTransfers), ex);
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
			var order_id = ToIdPair(order.OrderId).OrderId;
			var fund_id = OrderIdtoFundId[order_id];
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var price = order.Limit._(pair.RateUnits);
			var volume = order.VolumeBase._(pair.Base);
			var remaining = order.RemainingBase._(pair.Base);
			var created = order.Created;
			return new Position(fund_id, order_id, pair, Misc.TradeType(order.Type), price, volume, remaining, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private Historic HistoricFrom(global::Bittrex.API.Historic his, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			// Bittrex doesn't use trade ids. Make them up using the Remaining
			// volume so that each trade for a given order is unique (ish).
			var ids              = ToIdPair(his.OrderId);
			var order_id         = ids.OrderId;
			var trade_id         = ids.TradeId;
			var tt               = Misc.TradeType(his.Type);
			var pair             = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var price            = his.PricePerUnit._(pair.RateUnits);
			var volume_base      = his.FilledBase._(pair.Base);
			var commission_quote = his.Commission._(pair.Quote);
			var created          = his.Created;
			return new Historic(order_id, trade_id, pair, tt, price, volume_base, commission_quote, created, updated);
		}

		/// <summary>Convert a poloniex deposit into a 'Transfer'</summary>
		private Transfer TransferFrom(global::Bittrex.API.Transfer xfr, ETransfer type)
		{
			var id        = xfr.TxId;
			var coin      = Coins.GetOrAdd(xfr.Currency);
			var amount    = xfr.Amount._(coin);
			var timestamp = xfr.Timestamp.Ticks;
			var status    = ToTransferStatus(xfr);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}

		/// <summary>Convert a Bittrex UUID to a CoinFlip order id (ULONG)</summary>
		private TradeIdPair ToIdPair(Guid guid)
		{
			// Convert the guid into a order id/trade id pair and check if it's already in the map.
			// Handle Id collisions to ensure each order id is unique
			var ids = new TradeIdPair(guid);
			for (; m_order_id_lookup.TryGetValue(ids.OrderId, out var g) && g != guid;)
				++ids.OrderId;

			// Add the order id to the lookup table
			m_order_id_lookup[ids.OrderId] = guid;
			return ids;
		}

		/// <summary>Convert a CoinFlip order id (ULONG) to a Bittrex UUID</summary>
		private Guid ToUuid(ulong order_id)
		{
			return m_order_id_lookup[order_id];
		}

		/// <summary>Convert boolean in 'xfr' into a transfer status</summary>
		private Transfer.EStatus ToTransferStatus(global::Bittrex.API.Transfer xfr)
		{
			if (xfr.PendingPayment)
				return Transfer.EStatus.Pending;
			if (xfr.Cancelled)
				return Transfer.EStatus.Cancelled;
			if (xfr.Authorized)
				return Transfer.EStatus.Complete;
			return Transfer.EStatus.Unknown;
		}

		/// <summary>Convert a Guid into a trade id/order id pair</summary>
		private struct TradeIdPair
		{
			public TradeIdPair(Guid guid)
			{
				OrderId = BitConverter.ToUInt64(guid.ToByteArray(), 0);
				TradeId = BitConverter.ToUInt64(guid.ToByteArray(), 8);
			}
			public ulong OrderId;
			public ulong TradeId;
		}
	}
}
