using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip.Settings;
using ExchApi.Common;
using Poloniex.API;
using Poloniex.API.DomainObjects;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>'Poloniex' Exchange</summary>
	public class Poloniex :Exchange
	{
		private readonly HashSet<CurrencyPair> m_pairs;

		public Poloniex(CoinDataList coin_data, CancellationToken shutdown)
			: this(string.Empty, string.Empty, coin_data, shutdown)
		{
			ExchSettings.PublicAPIOnly = true;
		}
		public Poloniex(string key, string secret, CoinDataList coin_data, CancellationToken shutdown)
			:base(SettingsData.Settings.Poloniex, coin_data, shutdown)
		{
			m_pairs = new HashSet<CurrencyPair>();
			Api = new PoloniexApi(key, secret, shutdown, Model.Log);
		}
		public override void Dispose()
		{
			base.Dispose();
			Api = null;
		}

		/// <summary>The API interface</summary>
		private PoloniexApi Api
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
					m_api.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
				}
			}
		}
		private PoloniexApi m_api;
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// Get all available trading pairs
			// Use 'Shutdown' because updating pairs is independent of the Exchange.UpdateThread
			// and we don't want updating pairs to be interrupted by the update thread stopping
			var trade_pairs = await Api.GetTradePairs(cancel: Shutdown.Token);

			// Remove pairs that don't involve 'coins'
			trade_pairs.RemoveAll(x => !coins.Contains(x.Key));

			// Integrate the data (on the main thread)
			Model.DataUpdates.Add(() =>
			{
				Util.AssertMainThread();
				var pairs = new HashSet<CurrencyPair>();

				// Create the trade pairs and associated coins
				foreach (var p in trade_pairs)
				{
					var base_ = Coins.GetOrAdd(p.Value.Pair.Base);
					var quote = Coins.GetOrAdd(p.Value.Pair.Quote);

					// Create the trade pair
					var pair = new TradePair(base_, quote, this, p.Value.Id,
						amount_range_base: new RangeF<Unit<double>>(0.0001._(base_), 10000000.0._(base_)),
						amount_range_quote: new RangeF<Unit<double>>(0.0001._(quote), 10000000.0._(quote)),
						price_range: null);

					// Add the trade pair.
					Pairs[pair.UniqueKey] = pair;
					pairs.Add(p.Value.Pair);
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
		protected async override Task UpdateBalancesInternal(HashSet<string> coins) // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Request the account data
			var balance_data = await Api.GetBalances(cancel: Shutdown.Token);

			// Remove balances that don't involve 'coins' and don't qualify for auto add
			var auto_add_coins = SettingsData.Settings.AutoAddCoins;
			balance_data.RemoveAll(x => !(coins.Contains(x.Key) || (auto_add_coins && x.Value.Available != 0)));

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Update the account balance
				foreach (var b in balance_data)
				{
					// Find the currency that this balance is for. If it's not a coin of interest, ignore
					var coin = Coins.GetOrAdd(b.Key);

					// Update the balance
					Balance.ExchangeUpdate(coin, (b.Value.HeldForTrades + b.Value.Available)._(coin), b.Value.HeldForTrades._(coin), timestamp);
				}

				// Notify updated
				Balance.LastUpdated = timestamp;
			});
		}

		/// <summary>Update open orders and completed trades</summary>
		protected async override Task UpdateOrdersAndHistoryInternal(HashSet<string> coins) // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Request the existing orders
			var existing_orders = await Api.GetOpenOrders(cancel: Shutdown.Token);

			// Request the history
			var history = await Api.GetTradeHistory(beg: m_history_last, end: timestamp, cancel: Shutdown.Token);

			// Remove orders involving coins we don't care about
			if (!SettingsData.Settings.AutoAddCoins)
			{
				foreach (var kv in existing_orders)
					kv.Value.RemoveAll(x => !(coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)));
				foreach (var kv in history)
					kv.Value.RemoveAll(x => !(coins.Contains(x.Pair.Base) && coins.Contains(x.Pair.Quote)));
			}

			// Record the time that history has been updated to
			m_history_last = timestamp;

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				var orders = new List<Order>();
				var pairs = new HashSet<CurrencyPair>();

				// Update the trade history
				foreach (var exch_order in history.Values.SelectMany(x => x))
				{
					// Get/Add the completed order
					var order_completed = History.GetOrAdd(exch_order.OrderId,
						x => new OrderCompleted(x, OrderIdToFund(x), Pairs.GetOrAdd(exch_order.Pair.Base, exch_order.Pair.Quote), exch_order.Type.TradeType()));

					// Add the trade to the completed order
					var fill = TradeCompletedFrom(exch_order, order_completed, timestamp);
					order_completed.Trades[fill.TradeId] = fill;

					// Update the history of the completed orders
					AddToTradeHistory(order_completed);
				}

				// Update the collection of existing orders
				foreach (var exch_order in existing_orders.Values.Where(x => x.Count != 0).SelectMany(x => x))
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
			HashSet<string> pairs;
			lock (m_pairs)
				pairs = m_pairs.ToHashSet(x => x.Id);

			// Request order book data for all of the pairs
			// Poloniex only allows 6 API calls per second, so even though this returns
			// unnecessary data it's better to get all order books in one call.
			var order_book = await Api.GetOrderBook(depth: ExchSettings.MarketDepth, cancel: Shutdown.Token);

			// Remove the unnecessary data. (don't really need to do this, but it's consistent with the other exchanges)
			var surplus = order_book.Keys.Where(x => !pairs.Contains(x)).ToArray();
			foreach (var k in surplus)
				order_book.Remove(k);

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Process the order book data and update the pairs
				foreach (var pair in Pairs.Values)
				{
					if (!order_book.TryGetValue(new CurrencyPair(pair.Base, pair.Quote).Id, out var orders))
						continue;

					// Update the depth of market data
					var buys = orders.BuyOffers.Select(x => new Offer(x.Price._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
					var sells = orders.SellOffers.Select(x => new Offer(x.Price._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
					pair.MarketDepth.UpdateOrderBooks(buys, sells);
				}

				// Notify updated
				Pairs.LastUpdated = timestamp;
			});
		}

		/// <summary>Return the deposits and withdrawals made on this exchange</summary>
		protected async override Task UpdateTransfersInternal(HashSet<string> coins) // worker thread context
		{
			var timestamp = DateTimeOffset.Now;

			// Request the transfers data
			var transfers = await Api.GetTransfers(beg: m_transfers_last, end: timestamp, cancel: Shutdown.Token);

			// Remove transfers we don't care about
			if (!SettingsData.Settings.AutoAddCoins)
			{
				transfers.Deposits.RemoveAll(x => !coins.Contains(x.Currency));
				transfers.Withdrawals.RemoveAll(x => !coins.Contains(x.Currency));
			}

			// Record the time that transfer history has been updated to
			m_transfers_last = timestamp;

			// Queue integration of the transfer history
			Model.DataUpdates.Add(() =>
			{
				// Update the collection of funds transfers
				foreach (var dep in transfers.Deposits)
				{
					var deposit = TransferFrom(dep);
					Transfers[deposit.TransactionId] = deposit;
				}
				foreach (var wid in transfers.Withdrawals)
				{
					var withdrawal = TransferFrom(wid);
					Transfers[withdrawal.TransactionId] = withdrawal;
				}

				// Save the available range
				TransfersInterval = new Range(TransfersInterval.Beg, timestamp.Ticks);

				// Notify updated
				Transfers.LastUpdated = timestamp;
			});
		}

		/// <summary>Return the order book for 'pair' to a depth of 'count'</summary>
		protected async override Task<MarketDepth> MarketDepthInternal(TradePair pair, int depth) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);
			var orders = await Api.GetOrderBook(cp, depth, cancel: Shutdown.Token);

			// Update the depth of market data
			var market_depth = new MarketDepth(pair.Base, pair.Quote);
			var buys = orders.BuyOffers.Select(x => new Offer(x.Price._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
			var sells = orders.SellOffers.Select(x => new Offer(x.Price._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
			market_depth.UpdateOrderBooks(buys, sells);
			return market_depth;
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected async override Task<List<Candle>> CandleDataInternal(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);

			// Get the chart data
			var data = await Api.GetChartData(cp, ToMarketPeriod(timeframe), time_beg, time_end, cancel);

			// Convert it to candles (yes, Polo gets the base/quote backwards for 'Volume')
			var candles = data.Select(x => new Candle(x.Time.Ticks, x.Open, x.High, x.Low, x.Close, x.WeightedAverage, x.VolumeQuote)).ToList();
			return candles;
		}

		/// <summary>Cancel an open trade</summary>
		protected async override Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			try
			{
				// Cancel the trade
				return await Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), order_id);
			}
			catch (Exception ex)
			{
				throw new Exception($"Poloniex: Cancel trade (id={order_id}) failed. {ex.Message}", ex);
			}
		}

		/// <summary>Open a trade</summary>
		protected async override Task<OrderResult> CreateOrderInternal(TradePair pair, ETradeType tt, EOrderType ot, Unit<double> amount_in, Unit<double> amount_out, CancellationToken cancel)
		{
			try
			{
				// Place the trade order
				if (ot != EOrderType.Limit)
					throw new NotImplementedException();

				var coin_in = tt.CoinIn(pair);
				var coin_out = tt.CoinOut(pair);
				var price_q2b = tt.PriceQ2B(amount_out / amount_in);
				var amount_base = tt.AmountBase(price_q2b, amount_in, amount_out);
				var res = await Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), tt.ToPoloniexTT(), price_q2b, amount_base, cancel);

				// Get the immediate fills
				var fills = new List<OrderResult.Fill>();
				foreach (var fill in res.FilledOrders)
				{
					var fill_in = tt.AmountIn(fill.AmountBase, fill.PriceQ2B)._(coin_in);
					var fill_out = tt.AmountOut(fill.AmountBase, fill.PriceQ2B)._(coin_out);
					fills.Add(new OrderResult.Fill(fill.TradeId, fill_in, fill_out, fill.CommissionQuote, pair.Quote));
				}

				// Return the order result
				return new OrderResult(pair, res.OrderId, false, fills);
			}
			catch (Exception ex)
			{
				throw new Exception($"Poloniex: Submit trade failed. {ex.Message}\n{tt} Pair: {pair.Name}  Amt: {amount_in.ToString(8,true)} @  {(amount_out/amount_in).ToString(8,true)}", ex);
			}
		}

		/// <summary>Enumerate all candle data and time frames provided by this exchange</summary>
		protected override IEnumerable<PairAndTF> EnumAvailableCandleDataInternal(TradePair pair)
		{
			if (pair != null)
			{
				var cp = new CurrencyPair(pair.Base, pair.Quote);
				if (!Pairs.ContainsKey(pair.UniqueKey)) yield break;
				foreach (var mp in Enum<EMarketPeriod>.Values)
					yield return new PairAndTF(pair, ToTimeFrame(mp));
			}
			else
			{
				foreach (var p in Pairs.Values)
					foreach (var mp in Enum<EMarketPeriod>.Values)
						yield return new PairAndTF(p, ToTimeFrame(mp));
			}
		}

		/// <summary>Convert a Poloniex order into an order</summary>
		private Order OrderFrom(global::Poloniex.API.DomainObjects.Order order, DateTimeOffset updated)
		{
			var order_id   = order.OrderId;
			var fund_id    = OrderIdToFund(order_id);
			var ot         = EOrderType.Limit;
			var tt         = Misc.TradeType(order.Type);
			var pair       = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var amount_in  = tt.AmountIn(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var amount_out = tt.AmountOut(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var created    = order.Created;
			return new Order(order_id, fund_id, pair, ot, tt, amount_in, amount_out, amount_in, created, updated);
		}

		/// <summary>Convert a Poloniex trade history result into a completed order</summary>
		private TradeCompleted TradeCompletedFrom(global::Poloniex.API.DomainObjects.TradeCompleted his, OrderCompleted order_completed, DateTimeOffset updated)
		{
			var tt         = Misc.TradeType(his.Type);
			var pair       = order_completed.Pair;
			var trade_id   = his.GlobalTradeId;
			var amount_in  = tt.AmountIn(his.AmountBase._(pair.Base), his.PriceQ2B._(pair.RateUnits));
			var amount_out = tt.AmountOut(his.AmountBase._(pair.Base), his.PriceQ2B._(pair.RateUnits));
			var commission = amount_out * his.Fee;
			var created    = his.Timestamp;
			return new TradeCompleted(order_completed, trade_id, amount_in, amount_out, commission, pair.Quote, created, updated);
		}

		/// <summary>Convert a poloniex deposit into a 'Transfer'</summary>
		private Transfer TransferFrom(global::Poloniex.API.DomainObjects.Deposit dep)
		{
			var id        = dep.TransactionId;
			var type      = ETransfer.Deposit;
			var coin      = Coins.GetOrAdd(dep.Currency);
			var amount    = dep.Amount._(coin);
			var timestamp = dep.Timestamp;
			var status    = ToTransferStatus(dep.Status);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}
		private Transfer TransferFrom(global::Poloniex.API.DomainObjects.Withdrawal wid)
		{
			var id        = wid.WithdrawalNumber.ToString();
			var type      = ETransfer.Withdrawal;
			var coin      = Coins.GetOrAdd(wid.Currency);
			var amount    = wid.Amount._(coin);
			var timestamp = wid.Timestamp;
			var status    = ToTransferStatus(wid.Status);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}

		/// <summary>Convert a market period to a time frame</summary>
		private ETimeFrame ToTimeFrame(EMarketPeriod mp)
		{
			switch (mp)
			{
			default:                     return ETimeFrame.None;
			case EMarketPeriod.Minutes5:  return ETimeFrame.Min5;
			case EMarketPeriod.Minutes15: return ETimeFrame.Min15;
			case EMarketPeriod.Minutes30: return ETimeFrame.Min30;
			case EMarketPeriod.Hours2:    return ETimeFrame.Hour2;
			case EMarketPeriod.Hours4:    return ETimeFrame.Hour4;
			case EMarketPeriod.Day:       return ETimeFrame.Day1;
			}
		}

		/// <summary>Convert a time frame to the nearest market period</summary>
		private EMarketPeriod ToMarketPeriod(ETimeFrame tf)
		{
			switch (tf)
			{
			default:
				return EMarketPeriod.None;
			case ETimeFrame.Tick1:
			case ETimeFrame.Min1:
			case ETimeFrame.Min2:
			case ETimeFrame.Min3:
			case ETimeFrame.Min4:
			case ETimeFrame.Min5:
			case ETimeFrame.Min6:
			case ETimeFrame.Min7:
			case ETimeFrame.Min8:
			case ETimeFrame.Min9:
				return EMarketPeriod.Minutes5;
			case ETimeFrame.Min10:
			case ETimeFrame.Min15:
				return EMarketPeriod.Minutes15;
			case ETimeFrame.Min20:
			case ETimeFrame.Min30:
			case ETimeFrame.Min45:
				return EMarketPeriod.Minutes30;
			case ETimeFrame.Hour1:
			case ETimeFrame.Hour2:
				return EMarketPeriod.Hours2;
			case ETimeFrame.Hour3:
			case ETimeFrame.Hour4:
			case ETimeFrame.Hour6:
			case ETimeFrame.Hour8:
				return EMarketPeriod.Hours4;
			case ETimeFrame.Hour12:
			case ETimeFrame.Day1:
			case ETimeFrame.Day2:
			case ETimeFrame.Day3:
			case ETimeFrame.Week1:
			case ETimeFrame.Week2:
			case ETimeFrame.Month1:
				return EMarketPeriod.Day;
			}
		}

		/// <summary>Convert a poloniex transfer status to a 'Transfer.EStatus'</summary>
		private Transfer.EStatus ToTransferStatus(string status)
		{
			if (status.StartsWith("COMPLETE"))
				return Transfer.EStatus.Complete;
			if (status.StartsWith("PENDING"))
				return Transfer.EStatus.Pending;
			return Transfer.EStatus.Unknown;
		}
	}
}

