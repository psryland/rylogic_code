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
		public override void Dispose()
		{
			base.Dispose();
			Api = null;
		}

		/// <summary>The API interface</summary>
		private BittrexApi Api
		{
			[DebuggerStepThrough]
			get { return m_api; }
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
					m_api.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
				}
			}
		}
		private BittrexApi m_api;
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// Get all available trading pairs
			// Use 'Model.Shutdown' because updating pairs is independent of the Exchange.UpdateThread
			// and we don't want updating pairs to be interrupted by the update thread stopping
			var markets = await Api.GetMarkets(cancel: Shutdown.Token);

			// Add an action to integrate the data
			Model.DataUpdates.Add(() =>
			{
				var pairs = new HashSet<CurrencyPair>();

				// Create the trade pairs and associated coins
				foreach (var m in markets.Where(x => coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)))
				{
					var base_ = Coins.GetOrAdd(m.Pair.Base);
					var quote = Coins.GetOrAdd(m.Pair.Quote);

					// Create a trade pair. Note: m.MinTradeSize is not valid, 50,000 Satoshi is the minimum trade size
					var pair = new TradePair(base_, quote, this,
						trade_pair_id: null,
						amount_range_base: new RangeF<Unit<double>>(0.0005._(base_), 10000000.0._(base_)),
						amount_range_quote: new RangeF<Unit<double>>(0.0005._(quote), 10000000.0._(quote)),
						price_range: null);

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

		/// <summary>Update account balance data</summary>
		protected async override Task UpdateBalancesInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var last_updated = DateTimeOffset.Now;

			// Request the account data
			var balances = await Api.GetBalances(cancel: Shutdown.Token);

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Update the account balance
				foreach (var b in balances.Where(x => x.Available != 0 || Coins.ContainsKey(x.Symbol)))
				{
					// Find the currency that this balance is for
					var coin = Coins.GetOrAdd(b.Symbol);

					// Update the balance
					Balance.AssignFundBalance(coin, Fund.Main, b.Total._(coin), (b.Total - b.Available)._(coin), last_updated);
				}

				// Notify updated
				Balance.LastUpdated = last_updated;
			});
		}

		/// <summary>Update open orders and completed trades</summary>
		protected async override Task UpdateOrdersAndHistoryInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Request the existing orders
			var positions = await Api.GetOpenOrders(cancel: Shutdown.Token);

			// Request the history
			var history = await Api.GetTradeHistory(cancel: Shutdown.Token);

			// Record the time that history has been updated to
			m_history_last = timestamp;

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				var order_ids = new HashSet<long>();
				var pairs = new HashSet<CurrencyPair>();

				// Update the collection of existing orders
				foreach (var exch_order in positions)
				{
					// Add the order to the collection
					var order = OrderFrom(exch_order, timestamp);
					Orders[order.OrderId] = order;
					order_ids.Add(order.OrderId);
					pairs.Add(exch_order.Pair);
				}
				foreach (var exch_order in history)
				{
					// Get/Add the completed order
					var ids = ToIdPair(exch_order.OrderId);
					var order_completed = History.GetOrAdd(ids.OrderId, x => new OrderCompleted(x, OrderIdToFundId(x),
						Misc.TradeType(exch_order.Type), Pairs.GetOrAdd(exch_order.Pair.Base, exch_order.Pair.Quote)));

					// Add the trade to the completed order
					var fill = TradeCompletedFrom(exch_order, order_completed, timestamp);
					order_completed.Trades[fill.TradeId] = fill;

					// Update the history of completed orders
					AddToTradeHistory(order_completed);
				}

				// Update the trade pairs
				lock (m_pairs)
					m_pairs.AddRange(pairs);

				// Remove any positions that are no longer valid.
				SynchroniseOrders(order_ids, timestamp);

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
					pair.MarketDepth.UpdateOrderBook(update.buys, update.sells);
				}

				// Notify updated
				Pairs.LastUpdated = timestamp;
			});

			await Task.CompletedTask;
		}

		/// <summary>Return the deposits and withdrawals made on this exchange</summary>
		protected async override Task UpdateTransfersInternal() // worker thread context
		{
			var timestamp = DateTimeOffset.Now;

			// Request the transfers data
			var deposits = await Api.GetDeposits(cancel: Shutdown.Token);
			var withdrawals = await Api.GetWithdrawals(cancel: Shutdown.Token);

			// Record the time that transfer history has been updated to
			m_transfers_last = timestamp;

			// Queue integration of the transfer history
			Model.DataUpdates.Add(() =>
			{
				// Update the collection of funds transfers
				foreach (var dep in deposits)
				{
					var deposit = TransferFrom(dep, ETransfer.Deposit);
					Transfers[deposit.TransactionId] = deposit;
				}
				foreach (var wid in withdrawals)
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

		/// <summary>Cancel an open trade</summary>
		protected async override Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			try
			{
				// Cancel the trade
				var uuid = ToUuid(order_id);
				var result = await Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
				return result.Id == uuid;
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Cancel trade (id={order_id}) failed. {ex.Message}", ex);
			}
		}

		/// <summary>Open a trade</summary>
		protected async override Task<OrderResult> CreateOrderInternal(TradePair pair, ETradeType tt, EPlaceOrderType ot, Unit<double> amount, Unit<double> price, CancellationToken cancel)
		{
			try
			{
				// Place the trade order
				if (ot != EPlaceOrderType.Limit) throw new NotImplementedException();
				var res = await Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), tt.ToBittrexTT(), price, amount);
				var order_id = ToIdPair(res.Id).OrderId;
				return new OrderResult(pair, order_id, filled: false);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bittrex: Submit trade failed. {ex.Message}\n{tt} Pair: {pair.Name}  Vol: {amount.ToString("F8", true)} @  {price.ToString("F8", true)}", ex);
			}
		}

		/// <summary>Convert an exchange specific open order result into an order</summary>
		private Order OrderFrom(global::Bittrex.API.DomainObjects.Order order, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			var order_id = ToIdPair(order.OrderId).OrderId;
			var fund_id = OrderIdToFundId(order_id);
			var tt = Misc.TradeType(order.Type);
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var price = order.Limit._(pair.RateUnits);
			var amount = order.VolumeBase._(pair.Base);
			var remaining = order.RemainingBase._(pair.Base);
			var created = order.Created;
			return new Order(fund_id, order_id, tt, pair, price, amount, remaining, created, updated);
		}

		/// <summary>Convert a Cryptopia trade history result into a position object</summary>
		private TradeCompleted TradeCompletedFrom(global::Bittrex.API.DomainObjects.Trade his, OrderCompleted order_completed, DateTimeOffset updated)
		{
			// Get the associated trade pair (add the pair if it doesn't exist)
			// Bittrex doesn't use trade ids. Make them up using the Remaining
			// volume so that each trade for a given order is unique (ish).
			var pair = order_completed.Pair;
			var ids = ToIdPair(his.OrderId);
			var trade_id = ids.TradeId;
			var price = his.PricePerUnit._(pair.RateUnits);
			var amount_base = his.FilledBase._(pair.Base);
			var commission_quote = his.Commission._(pair.Quote);
			var created = his.Created;
			return new TradeCompleted(order_completed, trade_id, price, amount_base, commission_quote, created, updated);
		}

		/// <summary>Convert a poloniex deposit into a 'Transfer'</summary>
		private Transfer TransferFrom(global::Bittrex.API.DomainObjects.Transfer xfr, ETransfer type)
		{
			var id = xfr.TxId;
			var coin = Coins.GetOrAdd(xfr.Currency);
			var amount = xfr.Amount._(coin);
			var timestamp = xfr.Timestamp.Ticks;
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

		/// <summary>Convert a CoinFlip order id to a Bittrex UUID</summary>
		private Guid ToUuid(long order_id)
		{
			return m_order_id_lookup[order_id];
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
