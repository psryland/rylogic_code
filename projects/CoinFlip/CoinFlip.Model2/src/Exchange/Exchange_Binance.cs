using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Binance.API;
using Binance.API.DomainObjects;
using CoinFlip.Settings;
using ExchApi.Common;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class Binance : Exchange
	{
		private readonly HashSet<CurrencyPair> m_pairs;

		public Binance(CoinDataList coin_data, CancellationToken shutdown)
			: this(string.Empty, string.Empty, coin_data, shutdown)
		{
			ExchSettings.PublicAPIOnly = true;
		}
		public Binance(string key, string secret, CoinDataList coin_data, CancellationToken shutdown)
			: base(SettingsData.Settings.Binance, coin_data, shutdown)
		{
			m_pairs = new HashSet<CurrencyPair>();
			Api = new BinanceApi(key, secret, shutdown);
			TradeHistoryUseful = true;
		}
		public override void Dispose()
		{
			Api = null;
			base.Dispose();
		}

		/// <summary>The API interface</summary>
		private BinanceApi Api
		{
			[DebuggerStepThrough]
			get { return m_api; }
			set
			{
				if (m_api == value) return;
				if (m_api != null)
				{
					//m_api.OnOrdersChanged -= HandleOrdersChanged;
					//m_api.OnConnectionChanged -= HandleConnectionEstablished;
					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					m_api.RequestThrottle.RequestRateLimit = ExchSettings.ServerRequestRateLimit;
					//m_api.OnConnectionChanged += HandleConnectionEstablished;
					//m_api.OnOrdersChanged += HandleOrdersChanged;
				}

				// Handlers
				//void HandleOrdersChanged(object sender, OrderBookChangedEventArgs args)
				//{
				//	// This is currently broken on the Poloniex site
				//
				//	// Don't care about trade history at this point
				//	if (args.Update.Type == OrderBookUpdate.EUpdateType.NewTrade)
				//		return;
				//
				//	// Look for the associated pair, ignore if not found
				//	var pair = Pairs[args.Pair.Base, args.Pair.Quote];
				//	if (pair == null)
				//		return;
				//
				//	// Create the order
				//	var ty = args.Update.Order.Type;
				//	var order = new Offer(args.Update.Order.Price._(pair.RateUnits), args.Update.Order.VolumeBase._(pair.Base));
				//
				//	// Get the orders that the update applies to
				//	var book = (OrderBook)null;
				//	var sign = 0;
				//	switch (ty)
				//	{
				//	default: throw new Exception(string.Format("Unknown order update type: {0}", ty));
				//	case EOrderType.Sell: book = pair.B2Q; sign = -1; break;
				//	case EOrderType.Buy: book = pair.Q2B; sign = +1; break;
				//	}
				//
				//	// Find the position in 'book.Orders' of where the update applies
				//	var idx = book.Orders.BinarySearch(x => sign * x.Price.CompareTo(order.Price));
				//	if (idx < 0 && ~idx >= book.Orders.Count)
				//		return;
				//
				//	// Apply the update.
				//	switch (args.Update.Type)
				//	{
				//	default: throw new Exception(string.Format("Unknown order book update type: {0}", args.Update.Type));
				//	case OrderBookUpdate.EUpdateType.Modify:
				//		{
				//			// "Modify" means insert or replace the order with the matching price.
				//			if (idx >= 0)
				//				book.Orders[idx] = order;
				//			//else
				//			//	book.Orders.Insert(~idx, order);
				//			break;
				//		}
				//	case OrderBookUpdate.EUpdateType.Remove:
				//		{
				//			// "Remove" means remove the order with the matching price.
				//			if (idx >= 0)
				//				book.Orders.RemoveAt(idx);
				//			break;
				//		}
				//	}
				//
				//	Debug.Assert(pair.AssertOrdersValid());
				//}
				//void HandleConnectionEstablished(object sender, EventArgs e)
				//{
				//	if (Api.IsConnected)
				//		Model.Log.Write(ELogLevel.Info, $"Poloniex connection established");
				//	else
				//		Model.Log.Write(ELogLevel.Info, $"Poloniex connection offline");
				//}
			}
		}
		private BinanceApi m_api;
		protected override IExchangeApi ExchangeApi => Api;

		/// <summary>Update this exchange's set of trading pairs</summary>
		protected async override Task UpdatePairsInternal(HashSet<string> coins) // Worker thread context
		{
			// Get all available trading pairs
			// Use 'Shutdown' because updating pairs is independent of the Exchange.UpdateThread
			// and we don't want updating pairs to be interrupted by the update thread stopping
			var server_rules = await Api.ServerRules(cancel: Shutdown.Token);

			// Add an action to integrate the data
			Model.DataUpdates.Add(() =>
			{
				var pairs = new HashSet<CurrencyPair>();

				// Create the trade pairs and associated coins
				foreach (var p in server_rules.Symbols.Where(x => coins.Contains(x.BaseAsset) && coins.Contains(x.QuoteAsset)))
				{
					var base_ = Coins.GetOrAdd(p.BaseAsset);
					var quote = Coins.GetOrAdd(p.QuoteAsset);

					// Create the trade pair
					var pair = new TradePair(base_, quote, this, null,
						amount_range_base: new RangeF<Unit<decimal>>(0.0001m._(base_), 10000000m._(base_)),
						amount_range_quote: new RangeF<Unit<decimal>>(0.0001m._(quote), 10000000m._(quote)),
						price_range: null);

					// Add the trade pair.
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

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateDataInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Get the array of pairs to query for
			List<CurrencyPair> pairs;
			lock (m_pairs)
				pairs = m_pairs.ToList();

			// Request order book data for all of the pairs
			var order_books = new List<global::Binance.API.DomainObjects.OrderBook>();
			foreach (var pair in pairs)
				order_books.Add(await Api.GetOrderBook(pair, depth: ExchSettings.MarketDepth, cancel: Shutdown.Token));

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Process the order book data and update the pairs
				foreach (var ob in order_books)
				{
					// Find the pair to update.
					var cp = ob.Pair;
					var pair = Pairs[cp.Base, cp.Quote];
					if (pair == null)
						continue;

					// Update the depth of market data
					var buys = ob.BuyOffers.Select(x => new Offer(x.PriceQ2B._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
					var sells = ob.SellOffers.Select(x => new Offer(x.PriceQ2B._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
					pair.MarketDepth.UpdateOrderBook(buys, sells);
				}

				// Notify updated
				Pairs.LastUpdated = timestamp;
			});
		}

		/// <summary>Update account balance data</summary>
		protected async override Task UpdateBalancesInternal() // Worker thread context
		{
			// Request the account data
			var balance_data = await Api.GetBalances(cancel: Shutdown.Token);

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				// Process the account data and update the balances
				var msg = balance_data;

				// Ignore out of date data
				if (msg.UpdateTime < Balance.LastUpdated)
					return;

				// Update the account balance
				foreach (var b in msg.Balances.Where(x => x.Total != 0 || Coins.ContainsKey(x.Asset)))
				{
					// Find the currency that this balance is for
					var coin = Coins.GetOrAdd(b.Asset);

					// Update the balance
					Balance.AssignFundBalance(coin, Fund.Main, b.Total._(coin), b.Locked._(coin), msg.UpdateTime);
				}

				// Notify updated
				Balance.LastUpdated = msg.UpdateTime;
			});
		}

		/// <summary>Update open positions</summary>
		protected async override Task UpdatePositionsAndHistoryInternal() // Worker thread context
		{
			// Record the time just before the query to the server
			var timestamp = DateTimeOffset.Now;

			// Request the existing orders
			var existing_orders = new List<global::Binance.API.DomainObjects.Order>();
			foreach (var pair in m_pairs)
				existing_orders.AddRange(await Api.GetOpenOrders(new CurrencyPair(pair.Base, pair.Quote), cancel: Shutdown.Token));

			// Request the history
			//var history = await Api.GetAllOrders(beg: m_history_last, end: timestamp, cancel: Shutdown.Token);

			// Record the time that history has been updated to
			m_history_last = timestamp;

			// Queue integration of the market data
			Model.DataUpdates.Add(() =>
			{
				var order_ids = new HashSet<long>();
				var pairs = new HashSet<CurrencyPair>();

				// Update the collection of existing orders
				foreach (var order in existing_orders)
				{
					// Add the order to the collection
					var odr = OrderFrom(order, timestamp);
					Orders[odr.OrderId] = odr;
					order_ids.Add(odr.OrderId);
					pairs.Add(order.Pair);
				}
				//	foreach (var order in history.Values.SelectMany(x => x))
				//	{
				//		var his = TradeCompletedFrom(order, timestamp);
				//		var fill = History.GetOrAdd(his.OrderId, his.TradeType, his.Pair);
				//		fill.Trades[his.TradeId] = his;
				//		AddToTradeHistory(fill);
				//	}

				// Update the trade pairs
				lock (m_pairs)
					m_pairs.AddRange(pairs);

				// Remove any positions that are no longer valid.
				RemovePositionsNotIn(order_ids, timestamp);

				//	// Save the history range
				//	HistoryInterval = new Range(HistoryInterval.Beg, timestamp.Ticks);

				// Notify updated
				History.LastUpdated = timestamp;
				Orders.LastUpdated = timestamp;
			});
		}

		/// <summary>Return the order book for 'pair' to a depth of 'count'</summary>
		protected async override Task<MarketDepth> MarketDepthInternal(TradePair pair, int depth) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);
			var orders = await Api.GetOrderBook(cp, depth, cancel: Shutdown.Token);

			// Update the depth of market data
			var market_depth = new MarketDepth(pair.Base, pair.Quote);
			var buys = orders.BuyOffers.Select(x => new Offer(x.PriceQ2B._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
			var sells = orders.SellOffers.Select(x => new Offer(x.PriceQ2B._(pair.RateUnits), x.AmountBase._(pair.Base))).ToArray();
			market_depth.UpdateOrderBook(buys, sells);
			return market_depth;
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected async override Task<List<Candle>> CandleDataInternal(TradePair pair, ETimeFrame timeframe, UnixSec time_beg, UnixSec time_end, CancellationToken? cancel) // Worker thread context
		{
			var cp = new CurrencyPair(pair.Base, pair.Quote);

			// Get the chart data
			var data = await Api.GetChartData(cp, ToMarketPeriod(timeframe), time_beg, time_end, cancel);

			// Convert it to candles
			var candles = data.Select(x => new Candle(x.Time.Ticks, (double)x.Open, (double)x.High, (double)x.Low, (double)x.Close, (double)x.Median, (double)x.Volume)).ToList();
			return candles;
		}

		/// <summary>Cancel an open trade</summary>
		protected override Task<bool> CancelOrderInternal(TradePair pair, long order_id, CancellationToken cancel)
		{
			throw new NotImplementedException();
		}

		/// <summary>Open a trade</summary>
		protected override Task<OrderResult> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume_base, Unit<decimal> price, CancellationToken cancel)
		{
			throw new NotImplementedException();
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
		private Order OrderFrom(global::Binance.API.DomainObjects.Order odr, DateTimeOffset updated)
		{
			var order_id = odr.OrderId;
			var fund_id = OrderIdtoFundId[order_id];
			var tt = Misc.TradeType(odr.Side);
			var pair = Pairs.GetOrAdd(odr.Pair.Base, odr.Pair.Quote);
			var price = odr.Price._(pair.RateUnits);
			var amount = odr.Amount._(pair.Base);
			var created = odr.Created;
			return new Order(fund_id, order_id, tt, pair, price, amount, amount, created, updated);
		}

		/// <summary>Convert a market period to a time frame</summary>
		private ETimeFrame ToTimeFrame(EMarketPeriod mp)
		{
			switch (mp)
			{
			default: return ETimeFrame.None;
			case EMarketPeriod.Minutes1: return ETimeFrame.Min1;
			case EMarketPeriod.Minutes3: return ETimeFrame.Min3;
			case EMarketPeriod.Minutes5: return ETimeFrame.Min5;
			case EMarketPeriod.Minutes15: return ETimeFrame.Min15;
			case EMarketPeriod.Minutes30: return ETimeFrame.Min30;
			case EMarketPeriod.Hours1: return ETimeFrame.Hour1;
			case EMarketPeriod.Hours2: return ETimeFrame.Hour2;
			case EMarketPeriod.Hours4: return ETimeFrame.Hour4;
			case EMarketPeriod.Hours6: return ETimeFrame.Hour6;
			case EMarketPeriod.Hours8: return ETimeFrame.Hour8;
			case EMarketPeriod.Hours12: return ETimeFrame.Hour12;
			case EMarketPeriod.Day1: return ETimeFrame.Day1;
			case EMarketPeriod.Day3: return ETimeFrame.Day3;
			case EMarketPeriod.Week1: return ETimeFrame.Week1;
			case EMarketPeriod.Month1: return ETimeFrame.Month1;
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
				return EMarketPeriod.Minutes1;
			case ETimeFrame.Min2:
			case ETimeFrame.Min3:
				return EMarketPeriod.Minutes3;
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
				return EMarketPeriod.Hours1;
			case ETimeFrame.Hour2:
				return EMarketPeriod.Hours2;
			case ETimeFrame.Hour3:
			case ETimeFrame.Hour4:
				return EMarketPeriod.Hours4;
			case ETimeFrame.Hour6:
				return EMarketPeriod.Hours6;
			case ETimeFrame.Hour8:
				return EMarketPeriod.Hours8;
			case ETimeFrame.Hour12:
				return EMarketPeriod.Hours12;
			case ETimeFrame.Day1:
				return EMarketPeriod.Day1;
			case ETimeFrame.Day2:
			case ETimeFrame.Day3:
				return EMarketPeriod.Day3;
			case ETimeFrame.Week1:
			case ETimeFrame.Week2:
				return EMarketPeriod.Week1;
			case ETimeFrame.Month1:
				return EMarketPeriod.Month1;
			}
		}
	}
}
