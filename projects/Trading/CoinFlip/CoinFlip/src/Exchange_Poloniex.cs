using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			:base(model, 0.0025m, model.Settings.Poloniex.PollPeriod)
		{
			Pub = new PoloniexApiPublic();
			Priv = new PoloniexApiPrivate(ApiKey, ApiSecret);
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
		protected async override Task CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> rate)
		{
			// Obey the global trade switch
			if (Model.AllowTrades)
			{
				// Place the trade order
				await Priv.SubmitTrade(new CurrencyPair(pair.Base, pair.Quote), Misc.ToPoloniexTT(tt), rate, volume);
			}
		}

		/// <summary>Update this exchange's set of trading pairs</summary>
		public async override Task UpdatePairs(HashSet<string> coi) // Worker thread context
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

		/// <summary>Update the market data, balances, and open positions</summary>
		protected async override Task UpdateData() // Worker thread context
		{
			try
			{
				// Request order book data for all of the pairs
				var order_book = Pub.GetOrderBook(depth:50);

				// Request the account data
				var balance_data = Priv.GetBalances();

				// Request the existing orders
				var existing_orders = Priv.GetOpenOrders();

				// Wait for replies for all queries
				await Task.WhenAll(order_book, balance_data, existing_orders);

				// Queue integration of the market data
				Model.MarketUpdates.Add(() =>
				{
					// Process the order book data and update the pairs
					#region Order Book
					if (order_book != null)
					{
						var msg = order_book.Result;
						foreach (var orders in msg)
						{
							var cp = CurrencyPair.Parse(orders.Key);

							// Find the pair to update.
							// The pair may have been removed between the request and the reply.
							// Pair's may have been added as well, we'll get those next heart beat.
							var pair = Pairs[cp.Base, cp.Quote];
							if (pair == null)
								continue;

							// Update the depth of market data
							var buys  = orders.Value.BuyOrders .Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
							var sells = orders.Value.SellOrders.Select(x => new Order(x.Price._(pair.RateUnits), x.VolumeBase._(pair.Base))).ToArray();
							pair.UpdateOrderBook(buys, sells);
						}
					}
					#endregion

					// Process the account data and update the balances
					#region Balance
					{
						var msg = balance_data.Result;

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
					}
					#endregion

					// Process the existing orders
					#region Current Orders
					if (existing_orders != null)
					{
						var msg = existing_orders.Result;
				
						// Update the collection of existing orders
						var order_ids = new HashSet<ulong>();
						using (Model.Positions.PreservePosition())
						{
							// For each trading pair with active orders
							foreach (var instr in msg.Where(x => x.Value.Count != 0))
							{
								// Get the associated trade pair (add the pair if it doesn't exist)
								var cp = CurrencyPair.Parse(instr.Key);
								var pair = Pairs[cp.Base, cp.Quote] ?? Pairs.Add2(new TradePair(Coins.GetOrAdd(cp.Base), Coins.GetOrAdd(cp.Quote), this));
			
								// Add each order
								foreach (var order in instr.Value)
								{
									var id = order.OrderId;
									var type = Misc.TradeType(order.Type);
									var rate = order.Price._(pair.RateUnits);
									var volume = order.VolumeBase._(pair.Base);
									//var remaining = o.Remaining._(sym[0]);
									var ts = order.Timestamp;
			
									// Add the position to the collection
									var pos = new Position(id, pair, type, rate, volume, volume, ts);
									Positions[id] = pos;
									order_ids.Add(id);
								}
							}
			
							// Remove any positions that are no longer valid
							foreach (var id in Positions.Keys.Where(x => !order_ids.Contains(x)).ToArray())
								Positions.Remove(id);
						}
					}
					#endregion
				});

				Status = EStatus.Connected;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, "Poloniex UpdateData() failed");
				Status = EStatus.Error;
			}
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
			case EOrderType.Sell: book = pair.Bid; sign = -1; break;
			case EOrderType.Buy:  book = pair.Ask; sign = +1; break;
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

