//#define USE_WAMP
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Poloniex.API;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>'Poloniex' Exchange</summary>
	public class Poloniex :Exchange
	{
		private readonly HashSet<CurrencyPair> m_pairs;

		public Poloniex(Model model, string key, string secret)
			:base(model, model.Settings.Poloniex)
		{
			m_pairs = new HashSet<CurrencyPair>();
			Api = new PoloniexApi(key, secret, Model.Shutdown.Token);
			TradeHistoryUseful = true;
		}
		public override void Dispose()
		{
			Api = null;
			base.Dispose();
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
					m_api.OnOrdersChanged -= HandleOrdersChanged;
					m_api.OnConnectionChanged -= HandleConnectionEstablished;
					#if USE_WAMP
					m_api.Stop();
					#endif

					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					#if USE_WAMP
					m_api.Start();
					#endif
					m_api.ServerRequestRateLimit = Settings.ServerRequestRateLimit;
					m_api.OnConnectionChanged += HandleConnectionEstablished;
					m_api.OnOrdersChanged += HandleOrdersChanged;
				}
			}
		}
		private PoloniexApi m_api;

		/// <summary>Open a trade</summary>
		protected override TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			try
			{
				// Place the trade order
				var res = Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToPoloniexTT(tt), price, volume);
				return new TradeResult(pair, res.OrderId, false, res.FilledOrders.Select(x => x.TradeId));
			}
			catch (Exception ex)
			{
				throw new Exception($"Poloniex: Submit trade failed. {ex.Message}\n{tt} Pair: {pair.Name}  Vol: {volume.ToString("G8",true)} @  {price.ToString("G8",true)}", ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected override bool CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				return Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), order_id);
			}
			catch (Exception ex)
			{
				throw new Exception($"Poloniex: Cancel trade (id={order_id}) failed. {ex.Message}");
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

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected override List<Candle> CandleDataInternal(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end, CancellationToken? cancel) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);

			// Get the chart data
			var data = Api.GetChartData(cp, ToMarketPeriod(timeframe), time_beg, time_end, cancel);

			// Convert it to candles (yes, Polo gets the base/quote backwards for 'Volume')
			var candles = data.Select(x => new Candle(x.Time.Ticks, (double)x.Open, (double)x.High, (double)x.Low, (double)x.Close, (double)x.WeightedAverage, (double)x.VolumeQuote)).ToList();
			return candles;
		}

		/// <summary>Return the order book for 'pair' to a depth of 'count'</summary>
		protected override MarketDepth MarketDepthInternal(TradePair pair, int depth) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);
			var orders = Api.GetOrderBook(cp, depth, cancel:Shutdown.Token);

			// Update the depth of market data
			var market_depth = new MarketDepth(pair.Base, pair.Quote);
			var buys  = orders.BuyOrders .Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
			var sells = orders.SellOrders.Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
			market_depth.UpdateOrderBook(buys, sells);
			return market_depth;
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected override void UpdatePairsInternal(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				// Use 'Model.Shutdown' because updating pairs is independent of the Exchange.UpdateThread
				// and we don't want updating pairs to be interrupted by the update thread stopping
				var msg = Api.GetTradePairs(cancel:Model.Shutdown.Token);
				if (msg == null)
					throw new Exception("Poloniex: Failed to read market data.");

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					var pairs = new HashSet<CurrencyPair>();

					// Create the trade pairs and associated coins
					foreach (var p in msg.Where(x => coi.Contains(x.Value.Pair.Base) && coi.Contains(x.Value.Pair.Quote)))
					{
						// Poloniex gives pairs as "Quote_Base"
						var base_ = Coins.GetOrAdd(p.Value.Pair.Base);
						var quote = Coins.GetOrAdd(p.Value.Pair.Quote);

						// Create the trade pair
						var pair = new TradePair(base_, quote, this, p.Value.Id,
							volume_range_base:new RangeF<Unit<decimal>>(0.0001m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0001m._(quote), 10000000m._(quote)),
							price_range:null);

						// Add the trade pair.
						Pairs[pair.UniqueKey] = pair;
						pairs.Add(p.Value.Pair);
					}

					// Ensure a 'Balance' object exists for each coin type
					foreach (var c in Coins.Values)
						Balance.GetOrAdd(c);

					// Currently not working on the Poloniex site
					#if USE_WAMP
					// Ensure we're subscribed to the order book streams for each pair
					foreach (var p in Pairs.Values)
						Api.SubscribePair(new CurrencyPair(p.Base, p.Quote));
					#endif

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
				HashSet<string> pairs;
				lock (m_pairs)
					pairs = m_pairs.ToHashSet(x => x.Id);

				// Request order book data for all of the pairs
				// Poloniex only allows 6 API calls per second, so even though this returns
				// unnecessary data it's better to get all order books in one call.
				var order_book = Api.GetOrderBook(depth:Settings.MarketDepth, cancel:Shutdown.Token);

				// Remove the unnecessary data. (don't really need to do this, but it's consistent with the other exchanges)
				var surplus = order_book.Keys.Where(x => !pairs.Contains(x)).ToArray();
				foreach (var k in surplus)
					order_book.Remove(k);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var pair in Pairs.Values)
					{
						if (!order_book.TryGetValue(new CurrencyPair(pair.Base, pair.Quote).Id, out var orders))
							continue;

						// Update the depth of market data
						var buys  = orders.BuyOrders .Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
						var sells = orders.SellOrders.Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
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
				var timestamp = DateTimeOffset.Now;

				// Request the account data
				var balance_data = Api.GetBalances(cancel:Shutdown.Token);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the account data and update the balances
					var msg = balance_data;

					// Update the account balance
					foreach (var b in msg.Where(x => x.Value.Available != 0 || Coins.ContainsKey(x.Key)))
					{
						// Find the currency that this balance is for. If it's not a coin of interest, ignore
						var coin = Coins.GetOrAdd(b.Key);

						// Update the balance
						Balance[coin] = new Balances(coin, (b.Value.HeldForTrades + b.Value.Available)._(coin), b.Value.HeldForTrades._(coin), timestamp);
					}

					// Notify updated
					Balance.LastUpdated = timestamp;
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
				var existing_orders = Api.GetOpenOrders(cancel:Shutdown.Token);

				// Request the history
				var history = Api.GetTradeHistory(beg:m_history_last, end:timestamp, cancel:Shutdown.Token);

				// Record the time that history has been updated to
				m_history_last = timestamp;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					var order_ids = new HashSet<ulong>();
					var pairs = new HashSet<CurrencyPair>();

					// Update the collection of existing orders
					foreach (var order in existing_orders.Values.Where(x => x.Count != 0).SelectMany(x => x))
					{
						// Add the order to the collection
						var odr = OrderFrom(order, timestamp);
						Orders[odr.OrderId] = odr;
						order_ids.Add(odr.OrderId);
						pairs.Add(order.Pair);
					}
					foreach (var order in history.Values.SelectMany(x => x))
					{
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
					Orders.LastUpdated = timestamp;
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
				var transfers = Api.GetTransfers(beg:m_transfers_last, end:timestamp, cancel:Shutdown.Token);

				// Record the time that transfer history has been updated to
				m_transfers_last = timestamp;

				// Queue integration of the transfer history
				Model.MarketUpdates.Add(() =>
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

		/// <summary>Convert a Poloniex order into an order</summary>
		private Order OrderFrom(global::Poloniex.API.Order odr, DateTimeOffset updated)
		{
			var order_id = odr.OrderId;
			var fund_id  = OrderIdtoFundId[order_id];
			var tt       = Misc.TradeType(odr.Type);
			var pair     = Pairs.GetOrAdd(odr.Pair.Base, odr.Pair.Quote);
			var price    = odr.Price._(pair.RateUnits);
			var volume   = odr.VolumeBase._(pair.Base);
			var created  = odr.Created;
			return new Order(fund_id, order_id, pair, tt, price, volume, volume, created, updated);
		}

		/// <summary>Convert a Poloniex trade history result into a position object</summary>
		private Historic HistoricFrom(global::Poloniex.API.Historic his, DateTimeOffset updated)
		{
			var order_id         = his.OrderId;
			var trade_id         = his.GlobalTradeId;
			var tt               = Misc.TradeType(his.Type);
			var pair             = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var price            = his.Price._(pair.RateUnits);
			var volume_base      = his.Amount._(pair.Base);
			var commission_quote = price * volume_base * his.Fee;
			var created          = his.Timestamp;
			return new Historic(order_id, trade_id, pair, tt, price, volume_base, commission_quote, created, updated);
		}

		/// <summary>Convert a poloniex deposit into a 'Transfer'</summary>
		private Transfer TransferFrom(global::Poloniex.API.Deposit dep)
		{
			var id        = dep.TransactionId;
			var type      = ETransfer.Deposit;
			var coin      = Coins.GetOrAdd(dep.Currency);
			var amount    = dep.Amount._(coin);
			var timestamp = dep.Timestamp.Ticks;
			var status    = ToTransferStatus(dep.Status);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}
		private Transfer TransferFrom(global::Poloniex.API.Withdrawal wid)
		{
			var id        = wid.WithdrawalNumber.ToString();
			var type      = ETransfer.Withdrawal;
			var coin      = Coins.GetOrAdd(wid.Currency);
			var amount    = wid.Amount._(coin);
			var timestamp = wid.Timestamp.Ticks;
			var status    = ToTransferStatus(wid.Status);
			return new Transfer(id, type, coin, amount, timestamp, status);
		}

		/// <summary>Handle connection to Poloniex</summary>
		private void HandleConnectionEstablished(object sender, EventArgs e)
		{
			if (Api.IsConnected)
				Model.Log.Write(ELogLevel.Info, $"Poloniex connection established");
			else
				Model.Log.Write(ELogLevel.Info, $"Poloniex connection offline");
		}

		/// <summary>Handle market data updates</summary>
		private void HandleOrdersChanged(object sender, OrderBookChangedEventArgs args)
		{
			// This is currently broken on the Poloniex site

			// Don't care about trade history at this point
			if (args.Update.Type == OrderBookUpdate.EUpdateType.NewTrade)
				return;

			// Look for the associated pair, ignore if not found
			var pair = Pairs[args.Pair.Base, args.Pair.Quote];
			if (pair == null)
				return;

			// Create the order
			var ty = args.Update.Order.Type;
			var order = new OrderBook.Offer(args.Update.Order.Price._(pair.RateUnits), args.Update.Order.VolumeBase._(pair.Base));

			// Get the orders that the update applies to
			var book = (OrderBook)null;
			var sign = 0;
			switch (ty) {
			default: throw new Exception(string.Format("Unknown order update type: {0}", ty));
			case EOrderType.Sell: book = pair.B2Q; sign = -1; break;
			case EOrderType.Buy: book = pair.Q2B; sign = +1; break;
			}

			// Find the position in 'book.Orders' of where the update applies
			var idx = book.Orders.BinarySearch(x => sign * x.Price.CompareTo(order.Price));
			if (idx < 0 && ~idx >= book.Orders.Count)
				return;

			// Apply the update.
			switch (args.Update.Type)
			{
			default: throw new Exception(string.Format("Unknown order book update type: {0}", args.Update.Type));
			case OrderBookUpdate.EUpdateType.Modify:
				{
					// "Modify" means insert or replace the order with the matching price.
					if (idx >= 0)
						book.Orders[idx] = order;
					//else
					//	book.Orders.Insert(~idx, order);
					break;
				}
			case OrderBookUpdate.EUpdateType.Remove:
				{
					// "Remove" means remove the order with the matching price.
					if (idx >= 0)
						book.Orders.RemoveAt(idx);
					break;
				}
			}

			Debug.Assert(pair.AssertOrdersValid());
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

