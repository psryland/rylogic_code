using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Poloniex.API;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>'Poloniex' Exchange</summary>
	public class Poloniex :Exchange
	{
		public Poloniex(Model model, string key, string secret)
			:base(model, model.Settings.Poloniex)
		{
			Api = new PoloniexApi(key, secret, Model.ShutdownToken);
			TradeHistoryUseful = true;

			// Start the exchange
			if (Model.Settings.Poloniex.Active)
				Model.RunOnGuiThread(() => Active = true);
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
					Util.Dispose(ref m_api);
				}
				m_api = value;
				if (m_api != null)
				{
					m_api.ServerRequestRateLimit = Settings.ServerRequestRateLimit;
					m_api.OnConnectionChanged += HandleConnectionEstablished;
					m_api.OnOrdersChanged += HandleOrdersChanged;
				}
			}
		}
		private PoloniexApi m_api;

		/// <summary>Open a trade</summary>
		protected async override Task<TradeResult> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			try
			{
				// Place the trade order
				var res = await Api.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToPoloniexTT(tt), price, volume);
				return new TradeResult(res.OrderId, res.FilledOrders.Select(x => x.TradeId));
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Poloniex: Submit trade failed. {0}\n".Fmt(ex.Message) +
					"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, price), ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected async override Task CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				await Api.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), order_id);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Poloniex: Cancel trade failed. {0}\n".Fmt(ex.Message) +
					"Order Id: {0}".Fmt(order_id));
			}
		}

		/// <summary>True if this exchange supports retrieving chart data</summary>
		public override IEnumerable<ETimeFrame> ChartDataAvailable(TradePair pair)
		{
			if (pair.Exchange != this) throw new Exception($"Trade pair {pair.NameWithExchange} is not provided by this exchange ({Name})");
			return Enum<MarketPeriod>.Values.Select(x => ToTimeFrame(x));
		}

		/// <summary>Return the chart data for a given pair, over a given time range</summary>
		protected async override Task<List<Candle>> ChartDataInternal(TradePair pair, ETimeFrame timeframe, long time_beg, long time_end)
		{
			// Get the chart data
			var data = await Api.GetChartData(new CurrencyPair(pair.Base, pair.Quote), ToMarketPeriod(timeframe), time_beg, time_end);

			// Convert it to candles (yes, Polo gets the base/quote backwards for 'Volume')
			var candles = data.Select(x => new Candle(x.Time.Ticks, (double)x.Open, (double)x.High, (double)x.Low, (double)x.Close, (double)x.WeightedAverage, (double)x.VolumeQuote)).ToList();
			return candles;
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = await Api.GetTradePairs();
				if (msg == null)
					throw new Exception("Poloniex: Failed to read market data.");

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					// Create the trade pairs and associated coins
					foreach (var p in msg.Where(x => coi.Contains(x.Value.Pair.Base) && coi.Contains(x.Value.Pair.Quote)))
					{
						// Poloniex gives pairs as "Quote_Base"
						var base_ = Coins.GetOrAdd(p.Value.Pair.Base);
						var quote = Coins.GetOrAdd(p.Value.Pair.Quote);

						// Add the trade pair.
						var instr = new TradePair(base_, quote, this, p.Value.Id,
							volume_range_base:new RangeF<Unit<decimal>>(0.0001m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0001m._(quote), 10000000m._(quote)),
							price_range:null);
						Pairs[instr.UniqueKey] = instr;
					}

					// Ensure a 'Balance' object exists for each coin type
					foreach (var c in Coins.Values)
						Balance.GetOrAdd(c);

					// Currently not working on the Poloniex site
					//' // Ensure we're subscribed to the order book streams for each pair
					//' foreach (var p in Pairs.Values)
					//' 	Pub.SubscribePair(new CurrencyPair(p.Base, p.Quote));
				});
			}
			catch (Exception ex)
			{
				HandleUpdateException(nameof(UpdatePairs), ex);
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
				// Poloniex only allows 6 API calls per second, so even though it's more unnecessary data
				// it's better to get all order books in one call.
				var order_book = await Api.GetOrderBook(depth:10);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var pair in Pairs.Values)
					{
						if (!order_book.TryGetValue(new CurrencyPair(pair.Base, pair.Quote).Id, out var orders))
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
				HandleUpdateException(nameof(UpdateData), ex);
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
				var balance_data = await Api.GetBalances();

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the account data and update the balances
					var msg = balance_data;

					// Update the account balance
					using (Model.Balances.PreservePosition())
					{
						foreach (var b in msg.Where(x => x.Value.Available != 0 || Coins.ContainsKey(x.Key)))
						{
							// Find the currency that this balance is for. If it's not a coin of interest, ignore
							var coin = Coins.GetOrAdd(b.Key);

							// Update the balance
							var bal = new Balance(coin, b.Value.HeldForTrades + b.Value.Available, b.Value.Available, 0, b.Value.HeldForTrades, 0, timestamp);
							Balance[coin] = bal;
						}
					}

					// Notify updated
					BalanceUpdatedTime.NotifyAll(timestamp);
				});
			}
			catch (Exception ex)
			{
				HandleUpdateException(nameof(UpdateBalances), ex);
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
				var existing_orders = await Api.GetOpenOrders();

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					Debug.Assert(Model.AssertMainThread());

					// Update the collection of existing orders
					var order_ids = new HashSet<ulong>();
					foreach (var order in existing_orders.Values.Where(x => x.Count != 0).SelectMany(x => x))
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
				HandleUpdateException(nameof(UpdatePositions), ex);
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
				var history = await Api.GetTradeHistory(beg:new DateTimeOffset(HistoryInterval.End, TimeSpan.Zero), end:timestamp);
		
				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					foreach (var order in history.Values.SelectMany(x => x))
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
				HandleUpdateException(nameof(UpdateTradeHistory), ex);
			}
		}

		/// <summary>Set the maximum number of requests per second to the exchange server</summary>
		protected override void SetServerRequestRateLimit(float limit)
		{
			Api.ServerRequestRateLimit = limit;
		}

		/// <summary>Handle an exception during an update call</summary>
		private void HandleUpdateException(string method_name, Exception ex)
		{
			if (ex is AggregateException ae)
			{
				ex = ae.InnerExceptions.First();
			}
			if (ex is OperationCanceledException)
			{
				// Ignore operation cancelled
				return;
			}
			if (ex is HttpResponseException hre)
			{
				if (hre.StatusCode == HttpStatusCode.ServiceUnavailable)
				{
					if (Status != EStatus.Offline)
						Model.Log.Write(ELogLevel.Warn, "Poloniex Service Unavailable");

					Status = EStatus.Offline;
				}
				return;
			}

			// Log all other error types
			Model.Log.Write(ELogLevel.Error, ex, $"Poloniex {method_name} failed");
			Status = EStatus.Error;
		}

		/// <summary>Convert a Poloniex order into a position</summary>
		private Position PositionFrom(global::Poloniex.API.Position pos, DateTimeOffset updated)
		{
			var order_id = pos.OrderId;
			var tt       = Misc.TradeType(pos.Type);
			var pair     = Pairs.GetOrAdd(pos.Pair.Base, pos.Pair.Quote);
			var price    = pos.Price._(pair.RateUnits);
			var volume   = pos.VolumeBase._(pair.Base);
			var created  = pos.Created;
			return new Position(order_id, pair, tt, price, volume, volume, created, updated);
		}

		/// <summary>Convert a Poloniex trade history result into a position object</summary>
		private Historic HistoricFrom(global::Poloniex.API.Historic his, DateTimeOffset updated)
		{
			var order_id   = his.OrderId;
			var trade_id   = his.GlobalTradeId;
			var tt         = Misc.TradeType(his.Type);
			var pair       = Pairs.GetOrAdd(his.Pair.Base, his.Pair.Quote);
			var price      = tt == ETradeType.B2Q ? his.Price._(pair.RateUnits) : (1m / his.Price._(pair.RateUnits));
			var volume_in  = tt == ETradeType.B2Q ? his.Amount._(pair.Base)     : his.Total._(pair.Quote);
			var volume_out = tt == ETradeType.B2Q ? his.Total._(pair.Quote)     : his.Amount._(pair.Base);
			var commission = volume_out * his.Fee;
			var created    = his.Timestamp;
			return new Historic(order_id, trade_id, pair, tt, price, volume_in, volume_out, commission, created, updated);
		}

		/// <summary>Handle connection to Poloniex</summary>
		private void HandleConnectionEstablished(object sender, EventArgs e)
		{
			Status = Api.IsConnected ? EStatus.Connected : EStatus.Offline;
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
			var order = new Order(args.Update.Order.Price._(pair.RateUnits), args.Update.Order.VolumeBase._(pair.Base));

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

		/// <summary>Convert a time frame to a market period</summary>
		private MarketPeriod ToMarketPeriod(ETimeFrame tf)
		{
			switch (tf)
			{
			default:               return MarketPeriod.None;
			case ETimeFrame.Min5:  return MarketPeriod.Minutes5;
			case ETimeFrame.Min15: return MarketPeriod.Minutes15;
			case ETimeFrame.Min30: return MarketPeriod.Minutes30;
			case ETimeFrame.Hour2: return MarketPeriod.Hours2;
			case ETimeFrame.Hour4: return MarketPeriod.Hours4;
			case ETimeFrame.Day1:  return MarketPeriod.Day;
			}
		}

		/// <summary>Convert a market period to a time frame</summary>
		private ETimeFrame ToTimeFrame(MarketPeriod mp)
		{
			switch (mp)
			{
			default:                     return ETimeFrame.None;
			case MarketPeriod.Minutes5:  return ETimeFrame.Min5;
			case MarketPeriod.Minutes15: return ETimeFrame.Min15;
			case MarketPeriod.Minutes30: return ETimeFrame.Min30;
			case MarketPeriod.Hours2:    return ETimeFrame.Hour2;
			case MarketPeriod.Hours4:    return ETimeFrame.Hour4;
			case MarketPeriod.Day:       return ETimeFrame.Day1;
			}
		}
	}
}

