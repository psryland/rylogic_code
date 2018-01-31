using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Bitfinex.API;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Bitfinex :Exchange
	{
		private readonly Dictionary<ulong, Guid> m_order_id_lookup;

		public Bitfinex(Model model, string key, string secret)
			:base(model, model.Settings.Bitfinex)
		{
			m_order_id_lookup = new Dictionary<ulong, Guid>();
			Api = new BitfinexApi(key, secret, Model.Shutdown.Token);
			TradeHistoryUseful = true;
		}
		public override void Dispose()
		{
			Api = null;
			base.Dispose();
		}

		/// <summary>The public data interface</summary>
		private BitfinexApi Api
		{
			[DebuggerStepThrough] get { return m_api; }
			set
			{
				if (m_api == value) return;
				if (m_api != null)
				{
					m_api.Stop();
					m_api.WalletChanged -= HandleWalletChanged;
					m_api.OnError -= HandleError;
					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					m_api.ServerRequestRateLimit = Settings.ServerRequestRateLimit;
					m_api.OnError += HandleError;
					m_api.WalletChanged += HandleWalletChanged;
					m_api.Start();

					// Add subscriptions
					m_api.Subscriptions.Add(new SubscriptionAccount());
				}

				// Handlers
				void HandleError(object sender, ErrorEventArgs e)
				{
					Model.Log.Write(ELogLevel.Error, e.GetException(), "Bitfinex API error");
				}
				void HandleWalletChanged(object sender, EventArgs e)
				{
					
				}
			}
		}
		private BitfinexApi m_api;

		/// <summary>Open a trade</summary>
		protected override TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			try
			{
				throw new NotImplementedException();
			//	// Place the trade order
			//	var res = Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToBittrexTT(tt), price, volume);
			//	if (!res.Success)
			//		throw new Exception(res.Message);
			//
			//	var order_id = ToIdPair(res.Data.Id).OrderId;
			//	return new TradeResult(pair, order_id, filled:false);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bitfinex: Submit trade failed. {ex.Message}\n{tt} Pair: {pair.Name}  Vol: {volume.ToString("G8",true)} @  {price.ToString("G8",true)}", ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected override bool CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				throw new NotImplementedException();
			//	// Cancel the trade
			//	var uuid = ToUuid(order_id);
			//	var msg = Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), uuid);
			//	if (msg.Success) return true;
			//	if (msg.Message == "ORDER_NOT_OPEN") return false;
			//	throw new Exception(msg.Message);
			}
			catch (Exception ex)
			{
				throw new Exception($"Bitfinex: Cancel trade (id={order_id}) failed. {ex.Message}");
			}
		}

		/// <summary>Enumerate all candle data and time frames provided by this exchange</summary>
		protected override IEnumerable<PairAndTF> EnumAvailableCandleDataInternal(TradePair pair)
		{
			if (pair != null)
			{
				var cp = new CurrencyPair(pair.Base, pair.Quote);
				if (!Pairs.ContainsKey(pair.UniqueKey)) yield break;
				foreach (var mp in Enum<EMarketTimeFrames>.Values)
					yield return new PairAndTF(pair, ToTimeFrame(mp));
			}
			else
			{
				foreach (var p in Pairs.Values)
					foreach (var mp in Enum<EMarketTimeFrames>.Values)
						yield return new PairAndTF(p, ToTimeFrame(mp));
			}
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected override List<Candle> CandleDataInternal(TradePair pair, ETimeFrame time_frame, long time_beg, long time_end, CancellationToken? cancel) // Worker thread context
		{
			return new List<Candle>();
			//var cp = new CurrencyPair(pair.Base, pair.Quote);
			//var tf = ToMarketTimeFrame(time_frame);

			//var data = (List<global::Bitfinex.API.Candle>)null;

			//// If the chart data does not yet contain 'cp', add a subscription for it
			//if (!Api.Candles.Contains(cp, tf))
			//{
			//	// Add a subscription for the candle data
			//	Model.MarketUpdates.Add(() =>
			//	{
			//		Api.Subscriptions.Add(new SubscriptionCandleData(cp, tf));
			//	});
				
			//	// In the mean time, make a REST call
			//	data = Api.GetChartData(cp, tf, time_beg, time_end, cancel);
			//}
			//else
			//{
			//	data = Api.Candles[cp, tf];
			//}

			//// Convert it to candles
			//var candles = data.Select(x => new Candle(x.Timestamp.Ticks, x.Open, x.High, x.Low, x.Close, Maths.Median(x.Open, x.High, x.Low, x.Close), x.Volume)).ToList();
			//return candles;
		}

		/// <summary>Return the order book for 'pair' to a depth of 'count'</summary>
		protected override MarketDepth MarketDepthInternal(TradePair pair, int depth) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);
			var order_book = Api.GetOrderBook(cp, depth, EPrecision.P0, cancel:Shutdown.Token);

			// Update the depth of market data
			var market_depth = new MarketDepth(pair.Base, pair.Quote);
			var buys  = order_book.BuyOrders .Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
			var sells = order_book.SellOrders.Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
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
				var msg = Api.GetMarkets(cancel:Model.Shutdown.Token);
			
				// Get the pairs of interest
				var markets = msg.Where(x => coi.Contains(x.Base) && coi.Contains(x.Quote)).ToHashSet();

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					// Synchronise the order book subscriptions with 'markets'
					Api.Subscriptions.RemoveIf(x => x is SubscriptionOrderBook ob && !markets.Contains(ob.Pair));
					foreach (var pair in markets)
						Api.Subscriptions.Add(new SubscriptionOrderBook(pair));

					// Create the trade pairs and associated coins
					foreach (var m in markets)
					{
						var base_ = Coins.GetOrAdd(m.Base);
						var quote = Coins.GetOrAdd(m.Quote);

						// Create a trade pair. Note: m.MinTradeSize is not valid, 50,000 Satoshi is the minimum trade size
						var pair = new TradePair(base_, quote, this,
							trade_pair_id:null,
							volume_range_base: new RangeF<Unit<decimal>>(0.0005m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0005m._(quote), 10000000m._(quote)),
							price_range:null);

						// Update the pairs collection
						Pairs[pair.UniqueKey] = pair;
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

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var pair in Pairs.Values)
					{
						var order_book = Api.Market[new CurrencyPair(pair.Base, pair.Quote)];
				
						// Update the depth of market data
						var buys  = order_book.BuyOrders .Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
						var sells = order_book.SellOrders.Select(x => new OrderBook.Offer(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
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
				// Record the time
				var timestamp = DateTimeOffset.Now;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Update the account balance
					foreach (var b in Api.Wallet.Where(x => x.Total != 0m || Coins.ContainsKey(x.Symbol)))
					{
						var coin = Coins.GetOrAdd(b.Symbol);

						// Update the balance
						Balance[coin] = new Balances(coin, b.Total._(coin), (b.Total - b.Available)._(coin), timestamp);
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

				// Record the time that history has been updated to
				m_history_last = timestamp;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					var order_ids = new HashSet<ulong>();

					// Update the collection of existing orders
					foreach (var order in Api.Orders.Where(x => x.Status == EOrderStatus.Active || x.Status == EOrderStatus.PartiallyFilled))
					{
						// Add the order to the collection
						var odr = OrderFrom(order, timestamp);
						Orders[odr.OrderId] = odr;
						order_ids.Add(odr.OrderId);
					}
					foreach (var order in Api.History)
					{
						var his = HistoricFrom(order, timestamp);
						var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair);
						fill.Trades[his.TradeId] = his;
						AddToTradeHistory(fill);
					}

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
			//	var timestamp = DateTimeOffset.Now;
			//
			//	// Request the transfers data
			//	var transfers = Api.GetTransfers(beg:m_transfers_last, end:timestamp, cancel:Shutdown.Token);
			//
			//	// Record the time that transfer history has been updated to
			//	m_transfers_last = timestamp;
			//
			//	// Queue integration of the transfer history
			//	Model.MarketUpdates.Add(() =>
			//	{
			//		// Update the collection of funds transfers
			//		foreach (var dep in transfers.Deposits)
			//		{
			//			var deposit = TransferFrom(dep);
			//			Transfers[deposit.TransactionId] = deposit;
			//		}
			//		foreach (var wid in transfers.Withdrawals)
			//		{
			//			var withdrawal = TransferFrom(wid);
			//			Transfers[withdrawal.TransactionId] = withdrawal;
			//		}
			//
			//		// Save the available range
			//		TransfersInterval = new Range(TransfersInterval.Beg, timestamp.Ticks);
			//
			//		// Notify updated
			//		Transfers.LastUpdated = timestamp;
			//	});
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

		/// <summary>Convert a Bitfinex order into an order</summary>
		private Order OrderFrom(global::Bitfinex.API.Order odr, DateTimeOffset updated)
		{
			var order_id  = odr.OrderId;
			var fund_id   = OrderIdtoFundId[order_id];
			var tt        = Misc.TradeType(odr.TradeType);
			var pair      = Pairs.GetOrAdd(odr.Pair.Base, odr.Pair.Quote);
			var price     = odr.Price._(pair.RateUnits);
			var volume    = Math.Abs(odr.AmountInitial)._(pair.Base);
			var remaining = Math.Abs(odr.Amount)._(pair.Base);
			var created   = odr.Created;
			return new Order(fund_id, order_id, pair, tt, price, volume, remaining, created, updated);
		}

		/// <summary>Convert a Bitfinex filled order into a historic</summary>
		private Historic HistoricFrom(global::Bitfinex.API.Trade his, DateTimeOffset updated)
		{
			var order_id         = his.OrderId;
			var trade_id         = his.TradeId;
			var tt               = Misc.TradeType(his.TradeType);
			var pair             = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var price            = his.Price._(pair.RateUnits);
			var volume_base      = Math.Abs(his.Amount)._(pair.Base);
			var commission_quote = his.Fee._(his.FeeCurrency);
			var created          = his.Created;
			return new Historic(order_id, trade_id, pair, tt, price, volume_base, commission_quote, created, updated);
		}

		/// <summary>Convert a Bitfinex time frame to a time frame</summary>
		private ETimeFrame ToTimeFrame(EMarketTimeFrames mtf)
		{
			switch (mtf)
			{
			default:                         return ETimeFrame.None;
			case EMarketTimeFrames.Minute1:  return ETimeFrame.Min1;
			case EMarketTimeFrames.Minute5:  return ETimeFrame.Min5;
			case EMarketTimeFrames.Minute15: return ETimeFrame.Min15;
			case EMarketTimeFrames.Minute30: return ETimeFrame.Min30;
			case EMarketTimeFrames.Hour1:    return ETimeFrame.Hour1;
			case EMarketTimeFrames.Hour3:    return ETimeFrame.Hour3;
			case EMarketTimeFrames.Hour6:    return ETimeFrame.Hour6;
			case EMarketTimeFrames.Hour12:   return ETimeFrame.Hour12;
			case EMarketTimeFrames.Day1:     return ETimeFrame.Day1;
			case EMarketTimeFrames.Week1:    return ETimeFrame.Week1;
			case EMarketTimeFrames.Week2:    return ETimeFrame.Week2;
			case EMarketTimeFrames.Month1:   return ETimeFrame.Month1;
			}
		}

		/// <summary>Convert a time frame to the nearest market period</summary>
		private EMarketTimeFrames ToMarketTimeFrame(ETimeFrame tf)
		{
			switch (tf)
			{
			default:
				return EMarketTimeFrames.None;
			case ETimeFrame.Tick1:
			case ETimeFrame.Min1:
			case ETimeFrame.Min2:
				return EMarketTimeFrames.Minute1;
			case ETimeFrame.Min3:
			case ETimeFrame.Min4:
			case ETimeFrame.Min5:
			case ETimeFrame.Min6:
			case ETimeFrame.Min7:
			case ETimeFrame.Min8:
			case ETimeFrame.Min9:
				return EMarketTimeFrames.Minute5;
			case ETimeFrame.Min10:
			case ETimeFrame.Min15:
				return EMarketTimeFrames.Minute15;
			case ETimeFrame.Min20:
			case ETimeFrame.Min30:
			case ETimeFrame.Min45:
				return EMarketTimeFrames.Minute30;
			case ETimeFrame.Hour1:
				return EMarketTimeFrames.Hour1;
			case ETimeFrame.Hour2:
			case ETimeFrame.Hour3:
				return EMarketTimeFrames.Hour3;
			case ETimeFrame.Hour4:
			case ETimeFrame.Hour6:
				return EMarketTimeFrames.Hour6;
			case ETimeFrame.Hour8:
			case ETimeFrame.Hour12:
				return EMarketTimeFrames.Hour12;
			case ETimeFrame.Day1:
			case ETimeFrame.Day2:
			case ETimeFrame.Day3:
				return EMarketTimeFrames.Day1;
			case ETimeFrame.Week1:
				return EMarketTimeFrames.Week1;
			case ETimeFrame.Week2:
				return EMarketTimeFrames.Week2;
			case ETimeFrame.Month1:
				return EMarketTimeFrames.Month1;
			}
		}
	}
}
