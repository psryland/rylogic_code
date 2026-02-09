#if false
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bittrex.API;
using Bittrex.API.DomainObjects;
using CoinFlip.Settings;
using ExchApi.Common;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Bittrex : Exchange
	{
		private readonly HashSet<CurrencyPair> m_pairs;
		private readonly Dictionary<long, Guid> m_order_id_lookup;

		public Bittrex(CoinDataList coin_data, CancellationToken shutdown)
			:this(string.Empty, string.Empty, coin_data, shutdown)
		{
			ExchSettings.PublicAPIOnly = true;
		}
		public Bittrex(string key, string secret, CoinDataList coin_data, CancellationToken shutdown)
			: base(SettingsData.Settings.Bittrex, coin_data, shutdown)
		{
			m_pairs = new HashSet<CurrencyPair>();
			m_order_id_lookup = new Dictionary<long, Guid>();
			Api = new BittrexApi(key, secret, shutdown, Model.Log);
		}
		protected override void Dispose(bool _)
		{
			base.Dispose(_);
			Api = null;
		}

		/// <summary>The API interface</summary>
		private BittrexApi Api
		{
			[DebuggerStepThrough]
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					Util.Dispose(ref field);
				}
				field = value;
				if (field != null)
				{
					field.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
				}
			}
		}
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// Get all available trading pairs
			// Use 'Model.Shutdown' because updating pairs is independent of the Exchange.UpdateThread
			// and we don't want updating pairs to be interrupted by the update thread stopping
			var markets = await Api.GetMarkets(cancel: Shutdown.Token);

			// Remove markets we don't care about
			markets.RemoveAll(x => !(coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)));

			// Add an action to integrate the data
			Model.DataUpdates.Add(() =>
			{
				var pairs = new HashSet<CurrencyPair>();

				// Create the trade pairs and associated coins
				foreach (var m in markets)
				{
					var pair = Pairs.GetOrAdd(m.Pair.Base, m.Pair.Quote);
					pairs.Add(new CurrencyPair(pair.Base, pair.Quote));
				}

				// Ensure a 'Balance' object exists for each coin type
				foreach (var c in Coins)
					Balance.GetOrAdd(c);

				// Record the pairs
				lock (m_pairs)
				{
					m_pairs.Clear();
					m_pairs.AddRange(pairs);
				}
			});
		}

		/// <summary>Update account balance data</summary>
		protected async override Task UpdateBalancesInternal(HashSet<string> coins) // Worker thread context
		{
			// Record the time just before the query to the server
			var last_updated = DateTimeOffset.Now;

			// Request the account data
			var balances = await Api.GetBalances(cancel: Shutdown.Token);

			// Remove balances we don't care about
			var auto_add_coins = SettingsData.Settings.AutoAddCoins;
			balances.RemoveAll(x => !(coins.Contains(x.Symbol) || (auto_add_coins && x.Total != 0)));

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Update the account balance
				foreach (var b in balances)
				{
					// Find the currency that this balance is for
					var coin = Coins.GetOrAdd(b.Symbol);

					// Update the balance
					Balance.ExchangeUpdate(coin, b.Total._(coin), (b.Total - b.Available)._(coin), last_updated);
				}

				// Notify updated
				Balance.LastUpdated = last_updated;
			});
		}

		/// <summary>Update open orders and completed trades</summary>
		protected async override Task UpdateOrdersAndHistoryInternal(HashSet<string> coins) // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Request the existing orders
			var positions = await Api.GetOpenOrders(cancel: Shutdown.Token);

			// Request the history
			var history = await Api.GetTradeHistory(cancel: Shutdown.Token);

			// Remove orders and history we don't care about
			if (!SettingsData.Settings.AutoAddCoins)
			{
				positions.RemoveAll(x => !(coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)));
				history.RemoveAll(x => !(coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)));
			}

			// Record the time that history has been updated to
			m_history_last = timestamp;

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				var orders = new List<Order>();
				var pairs = new HashSet<CurrencyPair>();

				// Update the trade history
				foreach (var exch_order in history)
				{
					// Update the history of completed orders
					var fill = TradeCompletedFrom(exch_order, timestamp);
					AddToTradeHistory(fill);
				}

				// Update the collection of existing orders
				foreach (var exch_order in positions)
				{
					// Add the order to the collection
					var order = orders.Add2(OrderFrom(exch_order, timestamp));
					pairs.Add(exch_order.Pair);
				}
				SynchroniseOrders(orders, timestamp);

				// Update the trade pairs
				lock (m_pairs)
					m_pairs.AddRange(pairs);

				// Notify updated
				History.LastUpdated = timestamp;
				Orders.LastUpdated = timestamp;
			});
		}

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateDataInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Get the array of pairs to query for
			List<CurrencyPair> pairs;
			lock (m_pairs)
				pairs = m_pairs.ToList();

			// For each pair, get the updated market depth info
			var updates = pairs.Select(pair =>
			{
				// Get the latest market data for 'pair'
				var ob = Api.MarketData[pair];

				// Convert to CoinFlip market data format
				var rate_units = $"{pair.Quote}/{pair.Base}";
				var buys = ob.BuyOffers.Select(x => new Offer(x.PriceQ2B._(rate_units), x.AmountBase._(pair.Base))).ToArray();
				var sells = ob.SellOffers.Select(x => new Offer(x.PriceQ2B._(rate_units), x.AmountBase._(pair.Base))).ToArray();

				return new { pair, buys, sells };
			}).ToList();

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Apply the updates
				foreach (var update in updates)
				{
					// Find the pair to update
					var pair = Pairs[update.pair.Base, update.pair.Quote];
					if (pair == null)
						continue;

					// Update the market order book
					pair.MarketDepth.UpdateOrderBooks(update.buys, update.sells);
				}

				// Notify updated
				Pairs.LastUpdated = timestamp;
			});

			await Task.CompletedTask;
		}

		/// <summary>Return the deposits and withdrawals made on this exchange</summary>
		protected async override Task UpdateTransfersInternal(HashSet<string> coins) // worker thread context
		{
			var timestamp = DateTimeOffset.Now;

			// Request the transfers data
			var deposits = await Api.GetDeposits(cancel: Shutdown.Token);
			var withdrawals = await Api.GetWithdrawals(cancel: Shutdown.Token);

			// Remove transfers we don't care about
			if (!SettingsData.Settings.AutoAddCoins)
			{
				deposits.RemoveAll(x => !coins.Contains(x.Currency));
				withdrawals.RemoveAll(x => !coins.Contains(x.Currency));
			}

			// Record the time that transfer history has been updated to
			m_transfers_last = timestamp;

			// Queue integration of the transfer history
			Model.DataUpdates.Add(() =>
			{
				// Update the collection of funds transfers
				foreach (var dep in deposits)
				{
					var deposit = TransferFrom(dep, ETransfer.Deposit);
					AddToTransfersHistory(deposit);
				}
				foreach (var wid in withdrawals)
				{
					var withdrawal = TransferFrom(wid, ETransfer.Withdrawal);
					AddToTransfersHistory(withdrawal);
				}

				// Notify updated
				Transfers.LastUpdated = timestamp;
			});
		}

		/// <summary>Open a trade</summary>
		protected async override Task<OrderResult> CreateOrderInternal(Trade trade, CancellationToken cancel)
		{
			try
			{
				// Place the trade order
				if (trade.OrderType != EOrderType.Limit)
					throw new NotImplementedException();

				var res = await Api.SubmitTrade(new CurrencyPair(trade.Pair.Base, trade.Pair.Quote), trade.TradeType.ToBittrexTT(), trade.PriceQ2B, trade.AmountBase, cancel);

				// Return the order result
				var order_id = ToIdPair(res.Id).OrderId;
				return new OrderResult(trade.Pair, order_id, filled: false);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Submit trade failed. {ex.Message}\n{trade.TradeType} Pair: {trade.Pair.Name}  Amt: {trade.AmountIn.ToString(8, true)} -> {trade.AmountOut.ToString(8, true)} @  {trade.PriceQ2B.ToString(8, true)}", ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected async override Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			try
			{
				// Convert a CoinFlip order id to a Bittrex UUID
				var uuid = m_order_id_lookup[order_id];

				// Cancel the trade
				var result = await Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
				return result.Id == uuid;
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Cancel trade (id={order_id}) failed. {ex.Message}", ex);
			}
		}

		/// <summary>Convert an exchange specific open order result into an order</summary>
		private Order OrderFrom(global::Bittrex.API.DomainObjects.Order order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id = ToIdPair(order.OrderId).OrderId;
			var fund_id = OrderIdToFund(order_id);
			var ot = EOrderType.Limit;
			var tt = Misc.TradeType(order.Type);
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var amount_in = tt.AmountIn(order.AmountBase._(pair.Base), order.LimitQ2B._(pair.RateUnits));
			var amount_out = tt.AmountOut(order.AmountBase._(pair.Base), order.LimitQ2B._(pair.RateUnits));
			var remaining_in = tt.AmountIn(order.RemainingBase._(pair.Base), order.LimitQ2B._(pair.RateUnits));
			var created = order.Created;
			return new Order(order_id, fund_id, pair, ot, tt, amount_in, amount_out, remaining_in, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private TradeCompleted TradeCompletedFrom(global::Bittrex.API.DomainObjects.Trade his, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			// Bittrex doesn't use trade ids. Make them up using the Remaining
			// volume so that each trade for a given order is unique (ish).
			var ids = ToIdPair(his.OrderId);
			var order_id = ids.OrderId;
			var trade_id = ids.TradeId;
			var tt = Misc.TradeType(his.Type);
			var pair = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var amount_in = tt.AmountIn(his.FilledBase._(pair.Base), his.PricePerUnit._(pair.RateUnits));
			var amount_out = tt.AmountOut(his.FilledBase._(pair.Base), his.PricePerUnit._(pair.RateUnits));
			var commission = his.Commission._(pair.Quote);
			var commission_coin = pair.Quote;
			var created = his.Created;
			return new TradeCompleted(order_id, trade_id, pair, tt, amount_in, amount_out, commission, commission_coin, created, updated);
		}

		/// <summary>Convert a poloniex deposit into a 'Transfer'</summary>
		private Transfer TransferFrom(global::Bittrex.API.DomainObjects.Transfer xfr, ETransfer type)
		{
			var id = xfr.TxId;
			var coin = Coins.GetOrAdd(xfr.Currency);
			var amount = xfr.Amount._(coin);
			var timestamp = xfr.Timestamp;
			var status = ToTransferStatus(xfr);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}

		/// <summary>Convert a Bittrex UUID to a CoinFlip order id</summary>
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
		
		/// <summary>Convert boolean in 'xfr' into a transfer status</summary>
		private Transfer.EStatus ToTransferStatus(global::Bittrex.API.DomainObjects.Transfer xfr)
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
				OrderId = BitConverter.ToInt64(guid.ToByteArray(), 0);
				TradeId = BitConverter.ToInt64(guid.ToByteArray(), 8);
			}
			public long OrderId;
			public long TradeId;
		}
	}
}
#endif