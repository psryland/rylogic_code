using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
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
		private const string ApiKey    = "ORQL9DAZ-HEPQ1HIY-NFIXELSG-0T4DW2VA";
		private const string ApiSecret = "c7ebc4258332a994f80ced1ec3501a7705426c5fa9bd022d640c0c5e67513335babb453963102a25541c51a6ac4a6e827bf69dd831506c2a5b5d0bf9bfd58729";

		public Poloniex(Model model)
			:base(model, model.Settings.Poloniex)
		{
			Pub = new PoloniexApiPublic(Model.ShutdownToken.Token);
			Priv = new PoloniexApiPrivate(ApiKey, ApiSecret, Model.ShutdownToken.Token);

			// Start the exchange
			if (Model.Settings.Poloniex.Active)
				Model.RunOnGuiThread(() => Active = true);
		}
		public override void Dispose()
		{
			Pub = null;
			Priv = null;
			base.Dispose();
		}

		/// <summary>The public data interface</summary>
		private PoloniexApiPublic Pub
		{
			[DebuggerStepThrough] get { return m_pub; }
			set
			{
				if (m_pub == value) return;
				if (m_pub != null)
				{
					m_pub.OnOrdersChanged -= HandleOrdersChanged;
					m_pub.OnConnectionChanged -= HandleConnectionEstablished;
					Util.Dispose(ref m_pub);
				}
				m_pub = value;
				if (m_pub != null)
				{
					m_pub.OnConnectionChanged += HandleConnectionEstablished;
					m_pub.OnOrdersChanged += HandleOrdersChanged;
				}
			}
		}
		private PoloniexApiPublic m_pub;

		/// <summary>The private data interface</summary>
		private PoloniexApiPrivate Priv
		{
			[DebuggerStepThrough] get { return m_priv; }
			set
			{
				if (m_priv == value) return;
				Util.Dispose(ref m_priv);
				m_priv = value;
			}
		}
		private PoloniexApiPrivate m_priv;

		/// <summary>Open a trade</summary>
		protected async override Task<ulong> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			try
			{
				// Place the trade order
				var order_number = await Priv.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToPoloniexTT(tt), rate, volume);
				return order_number;
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Poloniex: Submit trade failed. {0}\n".Fmt(ex.Message) +
					"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, rate), ex);
			}
		}

		/// <summary>Cancel an open trade</summary>
		protected async override Task CancelOrderInternal(TradePair pair, ulong order_id)
		{
			try
			{
				// Cancel the trade
				await Priv.CancelTrade(new CurrencyPair(pair.Base, pair.Quote), order_id);
			}
			catch (Exception ex)
			{
				throw new Exception(
					"Poloniex: Cancel trade failed. {0}\n".Fmt(ex.Message) +
					"Order Id: {0}".Fmt(order_id));
			}
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = await Pub.GetTradePairs();

				// Add an action to integrate the data
				Model.MarketUpdates.Add(() =>
				{
					var nue = new HashSet<string>();

					// Create the trade pairs and associated coins
					var pairs = msg.Select(x => new { Id = x.Value.Id, Coins = x.Key.Split('_') }).Where(x => coi.Contains(x.Coins[0]) && coi.Contains(x.Coins[1]));
					foreach (var p in pairs)
					{
						// Poloniex gives pairs as "Quote_Base"
						var base_ = Coins.GetOrAdd(p.Coins[1]);
						var quote = Coins.GetOrAdd(p.Coins[0]);

						// Add the trade pair.
						var instr = new TradePair(base_, quote, this, p.Id,
							volume_range_base:new RangeF<Unit<decimal>>(0.0001m._(base_), 10000000m._(base_)),
							volume_range_quote:new RangeF<Unit<decimal>>(0.0001m._(quote), 10000000m._(quote)),
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

					// Currently not working on the Poloniex site
					//' // Ensure we're subscribed to the order book streams for each pair
					//' foreach (var p in Pairs.Values)
					//' 	Pub.SubscribePair(new CurrencyPair(p.Base, p.Quote));
				});
			}
			catch (Exception ex)
			{
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdatePairs() failed");
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
				// Poloniex only allows 6 API calls per second, so even though it's more unnecessary data
				// it's better to get all order books in one call.
				var order_book = await Pub.GetOrderBook(depth:10);

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
				if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
				if (ex is OperationCanceledException) {}
				else
				{
					Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdateData() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Update account balance data</summary>
		protected async override Task UpdateBalances()
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the account data
				var balance_data = await Priv.GetBalances();

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
							var bal = new Balance(coin, b.Value.HeldForTrades + b.Value.Available, b.Value.Available, 0, b.Value.HeldForTrades, 0);
							Balance[coin] = bal;
						}
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
					Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdateBalances() failed");
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
				var existing_orders = await Priv.GetOpenOrders();

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					Debug.Assert(Model.AssertMainThread());

					// Update the collection of existing orders
					var order_ids = new HashSet<ulong>();
					foreach (var order in existing_orders.Values.Where(x => x.Count != 0).SelectMany(x => x))
					{
						// Add the position to the collection
						var pos = PositionFrom(order);
						Positions[pos.OrderId] = pos;
						order_ids.Add(pos.OrderId);
					}

					// Remove any positions that are no longer valid.
					foreach (var pos in Positions.Values.Where(x => !order_ids.Contains(x.OrderId)).ToArray())
						Positions.Remove(pos.OrderId);

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
					Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdatePositions() failed");
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
				var history = await Priv.GetTradeHistory(beg:DateTimeOffset.Now - TimeSpan.FromDays(7), end:DateTimeOffset.Now);
		
				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					foreach (var order in history.Values.SelectMany(x => x))
					{
						var pos = PositionFrom(order);
						var fill = History.GetOrAdd(pos.OrderId, pos.TradeType, pos.Pair);
						fill.Trades[pos.TradeId] = pos;
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
					Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdateTradeHistory() failed");
					Status = EStatus.Error;
				}
			}
		}

		/// <summary>Convert a Poloniex order into a position</summary>
		private Position PositionFrom(global::Poloniex.API.Position pos)
		{
			var pair = Pairs[pos.Pair.Base, pos.Pair.Quote] ??
				Pairs.Add2(new TradePair(
					Coins.GetOrAdd(pos.Pair.Base),
					Coins.GetOrAdd(pos.Pair.Quote),
					this));

			var order_id = pos.OrderId;
			var type = Misc.TradeType(pos.Type);
			var rate = pos.Price._(pair.RateUnits);
			var volume = pos.VolumeBase._(pair.Base);
			var ts = pos.Timestamp;
			return new Position(order_id, 0, pair, type, rate, volume, volume, ts);
		}

		/// <summary>Convert a Poloniex trade history result into a position object</summary>
		private Position PositionFrom(global::Poloniex.API.Historic his)
		{
			var pair = Pairs[his.Pair.Base, his.Pair.Quote] ??
				Pairs.Add2(new TradePair(
					Coins.GetOrAdd(his.Pair.Base),
					Coins.GetOrAdd(his.Pair.Quote),
					this));

			var order_id = his.OrderId;
			var trade_id = his.GlobalTradeId;
			var type = Misc.TradeType(his.Type);
			var rate = his.Price._(pair.RateUnits);
			var volume = his.VolumeBase._(pair.Base);
			var ts = his.Timestamp;
			return new Position(order_id, trade_id, pair, type, rate, volume, 0, ts);
		}

		/// <summary>Handle connection to Poloniex</summary>
		private void HandleConnectionEstablished(object sender, EventArgs e)
		{
			Status = Pub.IsConnected ? EStatus.Connected : EStatus.Offline;
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
	}
}

