using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A mock exchange that replaces a real exchange during back testing</summary>
	public class SimExchange :IDisposable
	{
		// Notes:
		//  - The Exch.Collection members are not used to store the exchange state, the LazyDictionaries are.
		//    The Exch.Collection members represent data queried from the exchange, where as the LazyDictionaries
		//    represent the internal state of the simulated exchange.
		//  - During 'Step' the SimExchange copies data from the Dictionaries to the Collection members,
		//    emulating the behaviour of the 'Exchange.UpdateThreadEntryPoint' function which makes HTTP
		//    requests, storing/overwriting the results in the collection members.
		//  - SimExchange balances only use the 'NoCtx' balance context because real exchanges don't have balance contexts

		private readonly LazyDictionary<long, Order> m_ord;
		private readonly LazyDictionary<long, OrderCompleted> m_his;
		private readonly LazyDictionary<Coin, AccountBalance> m_bal;
		private readonly LazyDictionary<TradePair, MarketDepth> m_depth;

		private Random m_rng;
		private long m_order_id;
		private long m_history_id;
		private RangeF m_order_value_range;
		private double m_spread_frac;
		private int m_orders_per_book;

		public SimExchange(Simulation sim, Exchange exch, PriceDataMap price_data)
		{
			try
			{
				Debug.Assert(Model.BackTesting == true);

				Sim       = sim;
				Exchange  = exch;
				PriceData = price_data;
				m_ord     = new LazyDictionary<long, Order>(k => null);
				m_his     = new LazyDictionary<long, OrderCompleted>(k => null);
				m_bal     = new LazyDictionary<Coin, AccountBalance>(k => new AccountBalance(k, 0.0._(k)));
				m_depth   = new LazyDictionary<TradePair, MarketDepth>(k => new MarketDepth(k.Base, k.Quote));
				m_rng     = null;

				// Cache settings values
				m_order_value_range = SettingsData.Settings.BackTesting.OrderValueRange;
				m_spread_frac       = SettingsData.Settings.BackTesting.SpreadFrac;
				m_orders_per_book   = SettingsData.Settings.BackTesting.OrdersPerBook;

				Exchange.Sim = this;

				Reset();
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Exchange.Sim = null;
			Reset();
		}

		/// <summary>The owning simulation</summary>
		private Simulation Sim { get; }

		/// <summary>The exchange being simulated</summary>
		private Exchange Exchange { get; }

		/// <summary>The store of price data instances</summary>
		private PriceDataMap PriceData { get; }

		/// <summary>Coins associated with this exchange</summary>
		private CoinCollection Coins => Exchange.Coins;

		/// <summary>The pairs associated with this exchange</summary>
		private PairCollection Pairs => Exchange.Pairs;

		/// <summary>The balance of the given coin on this exchange</summary>
		private BalanceCollection Balance => Exchange.Balance;

		/// <summary>Positions by order id</summary>
		private OrdersCollection Orders => Exchange.Orders;

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		private OrdersCompletedCollection History => Exchange.History;

		/// <summary>The deposits/withdrawals</summary>
		private TransfersCollection Transfers => Exchange.Transfers;

		/// <summary>Reset the Sim Exchange</summary>
		public void Reset()
		{
			// Use the same random numbers for each run
			m_rng = new Random(54281);

			// Reset orders
			m_ord.Clear();
			Orders.Clear();
			m_order_id = 0;

			// Reset history
			m_his.Clear();
			History.Clear();
			m_history_id = 0;

			// Reset the balances to the initial values
			m_bal.Clear();
			Balance.Clear();
			Transfers.Clear();
			var exch_data = SettingsData.Settings.BackTesting.AccountBalances[Exchange.Name];
			foreach (var coin in Coins.Values)
			{
				var bal = exch_data[coin.Symbol];
				m_bal.Add(coin, new AccountBalance(coin, bal.Total._(coin)));
				Balance.Add(coin, new Balances(coin, bal.Total._(coin), Sim.Clock));

				// Add a "deposit" for each non-zero balance
				if (bal.Total != 0)
					Transfers.Add(new Transfer($"{bal.Symbol}-BackTesting", ETransfer.Deposit, coin, bal.Total._(coin), Model.UtcNow, Transfer.EStatus.Complete));

				CoinData.NotifyBalanceChanged(coin);
			}

			// Reset the spot price and order books of the pairs
			m_depth.Clear();
			foreach (var pair in Pairs.Values)
			{
				pair.SpotPrice[ETradeType.Q2B] = null;
				pair.SpotPrice[ETradeType.B2Q] = null;
				pair.MarketDepth.UpdateOrderBooks(new Offer[0], new Offer[0]);

				// Set the spot prices from the current candle at the simulation time
				var latest = PriceData[pair, Sim.TimeFrame]?.Current;
				if (latest != null)
				{
					var spot_q2b = latest.Close;
					var spread = spot_q2b * m_spread_frac;
					pair.SpotPrice[ETradeType.Q2B] = spot_q2b._(pair.RateUnits);
					pair.SpotPrice[ETradeType.B2Q] = (spot_q2b - spread)._(pair.RateUnits);
				}
			}
		}

		/// <summary>Step the exchange</summary>
		public void Step()
		{
			// This function emulates the behaviour of the 'Exchange.UpdateThreadEntryPoint'
			// method and the operations that occur on the exchange.

			// Do nothing if not enabled
			if (!Exchange.Enabled)
				return;

			#region Update Market Data
			// Generate and copy the market depth data to each trade pair
			{
				// Update the order book for each pair
				foreach (var pair in Pairs.Values)
				{
					// Get the data source for 'pair'
					var src = PriceData[pair, Sim.TimeFrame];
					if (src == null || src.Count == 0)
						continue;

					// No market data if the source is empty
					Debug.Assert(src.TimeFrame == Sim.TimeFrame);
					var latest = src.Current;
					if (latest == null)
						continue;

					// Generate market depth for 'pair', so that the spot price matches 'src.Current'
					var md = GenerateMarketDepth(pair, latest, Sim.TimeFrame);

					// Update the spot price and order book
					pair.MarketDepth.UpdateOrderBooks(md.B2Q.ToArray(), md.Q2B.ToArray());
					pair.SpotPrice[ETradeType.Q2B] = md.Q2B[0].PriceQ2B;
					pair.SpotPrice[ETradeType.B2Q] = md.B2Q[0].PriceQ2B;

					// Q2B => first price is the minimum, B2Q => first price is a maximum
					Debug.Assert(pair.SpotPrice[ETradeType.Q2B] == latest.Close._(pair.RateUnits));
					Debug.Assert(pair.SpotPrice[ETradeType.B2Q] == (latest.Close - (double)pair.Spread)._(pair.RateUnits));
				}

				// Notify updated
				Pairs.LastUpdated = Model.UtcNow;
			}
			#endregion

			#region Orders / History
			// Fill any orders that can be filled, update 'Orders' from 'm_ord', and 'History' from 'm_his'
			{
				// Try to fill orders
				foreach (var order in m_ord.Values.ToArray())
				{
					// Try to fill 'position'.
					// If 'his' is null, then the position can't be filled can remains unchanged
					// If 'pos' is null, then the position is completely filled.
					// 'order.OrderType' should have a value because the trade cannot be submitted without knowing the spot price
					TryFillOrder(order.Pair, order.Fund, order.OrderId, order.TradeType, order.OrderType, order.AmountIn, order.AmountOut, order.RemainingIn, out var pos, out var his);
					if (his == null)
						continue;

					// Stop the sim for 'RunToTrade' mode
					if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
						Sim.Pause();

					// If 'his' is not null, some or all of the position was filled.
					ApplyToBalance(pos, his);

					// Update the position store
					if (pos != null)
						m_ord[order.OrderId] = pos;
					else
						m_ord.Remove(order.OrderId);

					// Update the history store
					if (his != null)
					{
						var existing = m_his[order.OrderId]?.Trades;
						if (existing != null)
						{
							foreach (var h in existing)
								his.Trades.AddOrUpdate(h);
						}
						m_his[order.OrderId] = his;
					}

					// Sanity check that the partial trade isn't gaining or losing amount
					Debug.Assert(Math_.FEqlRelative(order.AmountBase, (pos?.RemainingBase ?? 0) + (his?.Trades.Sum(x => x.AmountBase) ?? 0), 0.00000001));
				}

				// This is equivalent to the 'DataUpdates' code in 'UpdateOrdersAndHistoryInternal'
				{
					var orders = new List<Order>();
					var timestamp = Model.UtcNow;

					// Update the trade history
					var history_updates = m_his.Values.Where(x => x.Created.Ticks >= Exchange.HistoryInterval.End);
					foreach (var exch_order in history_updates.SelectMany(x => x.Trades))
					{
						// Get/Add the completed order
						var order_completed = History.GetOrAdd(exch_order.OrderId,
							x => new OrderCompleted(x, Exchange.OrderIdToFund(x), exch_order.Pair, exch_order.TradeType));

						// Add the trade to the completed order
						var fill = TradeCompletedFrom(exch_order, order_completed, timestamp);
						order_completed.Trades.AddOrUpdate(fill);

						// Update the history of the completed orders
						Exchange.AddToTradeHistory(order_completed);
					}

					// Update the collection of existing orders
					foreach (var exch_order in m_ord.Values)
					{
						// Add the order to the collection
						orders.Add2(OrderFrom(exch_order, timestamp));
					}
					Exchange.SynchroniseOrders(orders, timestamp);

					// Notify updated. Notify history before positions so that orders don't "disappear" temporarily
					History.LastUpdated = timestamp;
					Orders.LastUpdated = timestamp;
				}
			}
			#endregion

			#region Balance
			{
				// This is equivalent to the 'DataUpdates' code in 'UpdateBalancesInternal'
				{
					var timestamp = Model.UtcNow;

					foreach (var b in m_bal.Values)
					{
						// Find the currency that this balance is for
						var coin = Coins.GetOrAdd(b.Coin.Symbol);

						// Update the balance
						Balance.ExchangeUpdate(coin, b.Total._(coin), b.Held._(coin), timestamp);
					}

					// Notify updated
					Balance.LastUpdated = timestamp;
				}
			}
			#endregion
		}

		/// <summary>Create an order on this simulated exchange</summary>
		public OrderResult CreateOrderInternal(TradePair pair, ETradeType tt, EOrderType ot, Unit<double> amount_in, Unit<double> amount_out)
		{
			// Validate the order
			var bal = m_bal[tt.CoinIn(pair)];
			if (pair.Exchange != Exchange)
				throw new Exception($"SimExchange: Attempt to trade a pair not provided by this exchange");
			if (bal.Available < amount_in)
				throw new Exception($"SimExchange: Submit trade failed. Insufficient balance\n{tt} Pair: {pair.Name}  Amt: {amount_in.ToString(8,true)}  Bal:{bal.Available.ToString(8,true)}");
			if (!pair.AmountRangeIn(tt).Contains(amount_in))
				throw new Exception($"SimExchange: 'In' amount is out of range");
			if (!pair.AmountRangeOut(tt).Contains(amount_out))
				throw new Exception($"SimExchange: 'Out' amount is out of range");

			// Stop the sim for 'RunToTrade' mode
			if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
				Sim.Pause();

			// Allocate a new position Id
			var order_id = ++m_order_id;

			// The order can be filled immediately, filled partially, or not filled and remain as a 'Position'
			TryFillOrder(pair, Fund.Default, order_id, tt, ot, amount_in, amount_out, amount_in, out var pos, out var his);
			if (pos != null) m_ord[order_id] = pos;
			if (his != null) m_his[order_id] = his;

			// Apply changes to the balance
			ApplyToBalance(pos, his);

			// Record the filled orders in the trade result
			return new OrderResult(pair, order_id, pos == null, his?.Trades.Select(x =>
				new OrderResult.Fill(x.TradeId, x.AmountIn, x.AmountOut, x.Commission, x.CommissionCoin)));
		}

		/// <summary>Cancel an existing position</summary>
		public bool CancelOrderInternal(TradePair pair, long order_id)
		{
			// Doesn't exist?
			if (!m_ord.TryGetValue(order_id, out var order))
				return false;

			// Remove 'pos'
			m_ord.Remove(order_id);

			// Remove any hold on the balance for this trade
			var bal = m_bal[order.CoinIn];
			bal.Holds.Remove(order_id);
			return true;
		}

		/// <summary>Return the market depth info for 'pair'</summary>
		public MarketDepth MarketDepthInternal(TradePair pair, int depth)
		{
			return m_depth[pair];
		}

		/// <summary>Attempt to make a trade on 'pair' for the given 'price' and base 'amount'</summary>
		private void TryFillOrder(TradePair pair, Fund fund, long order_id, ETradeType tt, EOrderType ot, Unit<double> amount_in, Unit<double> amount_out, Unit<double> remaining_in, out Order ord, out OrderCompleted his)
		{
			// The order can be filled immediately, filled partially, or not filled and remain as an 'Order'.
			// Also, exchanges use the base currency as the amount to fill, so for Q2B trades it's possible
			// that 'amount_in' is less than the trade asked for.
			var market = m_depth[pair];

			// Consume orders
			var price_q2b = tt.PriceQ2B(amount_out / amount_in);
			var amount_base = tt.AmountBase(price_q2b, amount_in: remaining_in);
			var filled = market.Consume(pair, tt, ot, price_q2b, amount_base, out var remaining_base);

			// The order is partially or completely filled...
			Debug.Assert(Math_.FEqlRelative(amount_base, remaining_base + filled.Sum(x => x.AmountBase), 0.000000001));
			ord = remaining_base != 0 ? new Order(order_id, fund, pair, ot, tt, amount_in, amount_out, tt.AmountIn(remaining_base, price_q2b), Model.UtcNow, Model.UtcNow) : null;
			his = remaining_base != amount_base ? new OrderCompleted(order_id, fund, pair, tt) : null;

			// Add 'TradeCompleted' entries for each order book offer that was filled
			foreach (var fill in filled)
				his.Trades.AddOrUpdate(new TradeCompleted(his, ++m_history_id, fill.AmountIn(tt), fill.AmountOut(tt), Exchange.Fee * fill.AmountOut(tt), tt.CoinOut(pair), Model.UtcNow, Model.UtcNow));
		}

		/// <summary>Apply the implied changes to the current balance by the given order and completed order</summary>
		private void ApplyToBalance(Order ord, OrderCompleted his)
		{
			if (his != null)
			{
				foreach (var trade in his.Trades)
				{
					{// Debt from CoinIn
						var bal = m_bal[trade.CoinIn];
						bal.Total -= trade.AmountIn;
					}
					{// Credit to CoinOut
						var bal = m_bal[trade.CoinOut];
						bal.Total += trade.AmountOut;
					}
					{// Debt the commission
						var bal = m_bal[trade.CommissionCoin];
						bal.Total -= trade.Commission;
					}
				}
				{// Remove the hold
					var bal = m_bal[his.CoinIn];
					bal.Holds.Remove(his.OrderId);
				}
			}

			if (ord != null)
			{
				// Update the hold for the trade
				var bal = m_bal[ord.CoinIn];
				bal.Holds[ord.OrderId] = ord.RemainingIn;
			}
		}

		/// <summary>Generate simulated market depth for 'pair', using 'latest' as the reference for the current spot price</summary>
		private MarketDepth GenerateMarketDepth(TradePair pair, Candle latest, ETimeFrame time_frame)
		{
			// Notes:
			//  - This is an expensive call when back testing is running so minimise allocation, resizing, and sorting.
			//  - Do all calculations using double's for speed.

			// Get the market data for 'pair'.
			// Market data is maintained independently to the pair's market data instance because the
			// rest of the application expects the market data to periodically overwrite the pair's order books.
			var md = m_depth[pair];

			// Get the Q2B (bid) spot price from the candle close. (This is the minimum of the Q2B offers)
			// The B2Q spot price is Q2B - spread, which will be the maximum of the B2Q offers
			var spread = latest.Close * m_spread_frac;
			var best_q2b = latest.Close;
			var best_b2q = latest.Close - spread;
			var base_value = pair.Base.Value;

			md.Q2B.Offers.Resize(m_orders_per_book);
			md.B2Q.Offers.Resize(m_orders_per_book);

			// Generate offers with a normal distribution about 'best'
			var range = 0.2 * 0.5 * (best_q2b + best_b2q);
			for (var i = 0; i != m_orders_per_book; ++i)
			{
				var p = range * Math_.Sqr((double)i / m_orders_per_book);
				md.Q2B.Offers[i] = new Offer((best_q2b + p)._(pair.RateUnits), RandomAmountBase()._(pair.Base));
				md.B2Q.Offers[i] = new Offer((best_b2q - p)._(pair.RateUnits), RandomAmountBase()._(pair.Base));
			}
			return md;

			double RandomAmountBase()
			{
				// Generate an amount to trade in the application common currency (probably USD).
				// Then convert that to base currency using the 'live' value.
				var common_value = Math.Abs(m_rng.Double(m_order_value_range.Beg, m_order_value_range.End));
				var amount_base = Math_.Div(common_value, (double)base_value, common_value);
				return amount_base;
			}
		}

		/// <summary>Convert an exchange order into a CoinFlip order</summary>
		private Order OrderFrom(Order order, DateTimeOffset updated)
		{
			// This is mainly to mirror the behaviour of the actual exchanges
			var order_id = order.OrderId;
			var fund_id = Exchange.OrderIdToFund(order_id);
			var ot = order.OrderType;
			var tt = order.TradeType;
			var pair = Pairs.GetOrAdd(order.Pair.Base, order.Pair.Quote);
			var amount_in = tt.AmountIn(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var amount_out = tt.AmountOut(order.AmountBase._(pair.Base), order.PriceQ2B._(pair.RateUnits));
			var remaining_in = amount_in;
			var created = order.Created;
			return new Order(order_id, fund_id, pair, ot, tt, amount_in, amount_out, remaining_in, created, updated);
		}

		/// <summary>Convert an exchange order into a CoinFlip order completed</summary>
		private TradeCompleted TradeCompletedFrom(TradeCompleted fill, OrderCompleted order_completed, DateTimeOffset updated)
		{
			// This is mainly to mirror the behaviour of the actual exchanges
			var tt = order_completed.TradeType;
			var pair = order_completed.Pair;
			var trade_id = fill.TradeId;
			var amount_in = fill.AmountIn;
			var amount_out = fill.AmountOut;
			var commission = fill.Commission;
			var commission_coin = fill.CommissionCoin;
			var created = fill.Created;
			return new TradeCompleted(order_completed, trade_id, amount_in, amount_out, commission, commission_coin, created, updated);
		}

		/// <summary>Represents the simulated user account on the exchange</summary>
		private class AccountBalance
		{
			public AccountBalance(Coin coin, Unit<double> total)
			{
				Coin = coin;
				Total = total;
				Holds = new Dictionary<long, Unit<double>>();
			}
			public Coin Coin { get; }
			public Unit<double> Total { get; set; }
			public Unit<double> Held => Holds.Values.Sum(x => x);
			public Unit<double> Available => Total - Held;
			public Dictionary<long, Unit<double>> Holds { get; }
		}
	}
}
