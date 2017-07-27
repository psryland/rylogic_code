using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bittrex.API;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class Bittrex :Exchange
	{
		public Bittrex(Model model)
			:base(model, model.Settings.Bittrex)
		{
			Pub = new BittrexApiPublic(Model.ShutdownToken.Token);

			// Start the exchange
			if (Model.Settings.Bittrex.Active)
				Model.RunOnGuiThread(() => Active = true);
		}
		public override void Dispose()
		{
			Pub = null;
			base.Dispose();
		}

		/// <summary>The public data interface</summary>
		private BittrexApiPublic Pub
		{
			[DebuggerStepThrough] get { return m_pub; }
			set
			{
				if (m_pub == value) return;
				if (m_pub != null)
				{
//					m_pub.OnOrdersChanged -= HandleOrdersChanged;
//					m_pub.OnConnectionChanged -= HandleConnectionEstablished;
					Util.Dispose(ref m_pub);
				}
				m_pub = value;
				if (m_pub != null)
				{
//					m_pub.OnConnectionChanged += HandleConnectionEstablished;
//					m_pub.OnOrdersChanged += HandleOrdersChanged;
				}
			}
		}
		private BittrexApiPublic m_pub;

		/// <summary>Open a trade</summary>
		protected override Task<ulong> CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			throw new NotImplementedException();
			//// Place the trade order
			//var msg = await Priv.SubmitTrade(new SubmitTradeRequest(pair.TradePairId.Value, tt.ToCryptopiaTT(), volume, rate));
			//if (!msg.Success)
			//	throw new Exception(
			//		"Cryptopia: Submit trade failed. {0}\n".Fmt(msg.Error) +
			//		"{0} Pair: {1}  Vol: {2} @ {3}".Fmt(tt, pair.Name, volume, rate));
		}

		/// <summary>Cancel an open trade</summary>
		protected override Task CancelOrderInternal(TradePair pair, ulong order_id)
		{
			throw new NotImplementedException();
			//await Misc.CompletedTask;
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
		{
			try
			{
				// Get all available trading pairs
				var msg = await Pub.GetMarkets();
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
					order_books.Add(Pub.GetOrderBook(new CurrencyPair(pair.Base, pair.Quote), BittrexApiPublic.EGetOrderBookType.Both, depth:20));

				// Wait for replies for all queries
				await Task.WhenAll(order_books);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					foreach (var ob in order_books)
					{
						var msg = ob.Result;
						if (!msg.Success)
							throw new Exception("Bittrex: Failed to update trading pairs. {0}".Fmt(msg.Message));

						var orders = msg.Data;
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
		protected async override Task UpdateBalances()
		{
			try
			{
				// Record the time just before the query to the server
				var timestamp = DateTimeOffset.Now;

				// Request the account data
			//	var balance_data = Priv.GetBalances();
				await Misc.CompletedTask;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
			//		// Process the account data and update the balances
			//		var msg = balance_data.Result;
			//		
			//		// Update the account balance
			//		using (Model.Balances.PreservePosition())
			//		{
			//			foreach (var b in msg.Where(x => x.Value.Available != 0 || Coins.ContainsKey(x.Key)))
			//			{
			//				// Find the currency that this balance is for. If it's not a coin of interest, ignore
			//				var coin = Coins.GetOrAdd(b.Key);
			//		
			//				// Update the balance
			//				var bal = new Balance(coin, b.Value.HeldForTrades + b.Value.Available, b.Value.Available, 0, b.Value.HeldForTrades, 0);
			//				Balance[coin] = bal;
			//			}
			//		}

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
			//	var existing_orders = await Priv.GetOpenOrders();
				await Misc.CompletedTask;

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
			//		// Process the existing orders
			//		var msg = existing_orders;
			//	
			//		// Update the collection of existing orders
			//		var order_ids = new HashSet<ulong>();
			//
			//		// For each trading pair with active orders
			//		foreach (var instr in msg.Where(x => x.Value.Count != 0))
			//		{
			//			// Get the associated trade pair (add the pair if it doesn't exist)
			//			var cp = CurrencyPair.Parse(instr.Key);
			//			var pair = Pairs[cp.Base, cp.Quote] ?? Pairs.Add2(new TradePair(Coins.GetOrAdd(cp.Base), Coins.GetOrAdd(cp.Quote), this));
			//
			//			// Add each order
			//			foreach (var order in instr.Value)
			//			{
			//				var id = order.OrderId;
			//				var type = Misc.TradeType(order.Type);
			//				var rate = order.Price._(pair.RateUnits);
			//				var volume = order.VolumeBase._(pair.Base);
			//				//var remaining = o.Remaining._(sym[0]);
			//				var ts = order.Timestamp;
			//
			//				// Add the position to the collection
			//				var pos = new Position(id, pair, type, rate, volume, volume, ts);
			//				Positions[id] = pos;
			//				order_ids.Add(id);
			//			}
			//		}
			//
			//		// Remove any positions that are no longer valid
			//		foreach (var id in Positions.Keys.Where(x => !order_ids.Contains(x)).ToArray())
			//			Positions.Remove(id);

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
	}
}
