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
				m_bal     = new LazyDictionary<Coin, AccountBalance>(k => new AccountBalance(k, 0m._(k), 0m._(k)));
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

		/// <summary>Reset the Sim Exchange</summary>
		public void Reset()
		{
			// Use the same random numbers for each run
			m_rng = new Random(54281);

			// Reset positions
			m_ord.Clear();
			Orders.Clear();
			m_order_id = 0;

			// Reset History
			m_his.Clear();
			History.Clear();
			m_history_id = 0;

			// Reset the balances to the initial values
			m_bal.Clear();
			Balance.Clear();
			var exch_data = SettingsData.Settings.BackTesting.AccountBalances[Exchange.Name];
			foreach (var coin in Coins.Values)
			{
				var bal = exch_data[coin.Symbol];
				m_bal.Add(coin, new AccountBalance(coin, bal.Total._(coin), bal.Held._(coin)));
				Balance.Add(coin, new Balances(coin, bal.Total._(coin), Sim.Clock));
			}

			// Reset the spot price and order books of the pairs
			m_depth.Clear();
			foreach (var pair in Pairs.Values)
			{
				pair.SpotPrice[ETradeType.Q2B] = null;
				pair.SpotPrice[ETradeType.B2Q] = null;
				pair.MarketDepth.UpdateOrderBook(new Offer[0], new Offer[0]);

				// Set the spot prices from the current candle at the simulation time
				var latest = PriceData[pair, Sim.TimeFrame]?.Current;
				if (latest != null)
				{
					var spot_q2b = latest.Close;
					var spread = spot_q2b * m_spread_frac;
					pair.SpotPrice[ETradeType.Q2B] = ((decimal)(spot_q2b         ))._(pair.RateUnits);
					pair.SpotPrice[ETradeType.B2Q] = ((decimal)(spot_q2b - spread))._(pair.RateUnits);
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
					pair.MarketDepth.UpdateOrderBook(md.B2Q.ToArray(), md.Q2B.ToArray());
					pair.SpotPrice[ETradeType.Q2B] = ((decimal)md.Q2B[0].Price)._(pair.RateUnits);
					pair.SpotPrice[ETradeType.B2Q] = ((decimal)md.B2Q[0].Price)._(pair.RateUnits);

					// Q2B => first price is the minimum, B2Q => first price is a maximum
					Debug.Assert(pair.SpotPrice[ETradeType.Q2B] == ((decimal)latest.Close                       )._(pair.RateUnits));
					Debug.Assert(pair.SpotPrice[ETradeType.B2Q] == ((decimal)latest.Close - (decimal)pair.Spread)._(pair.RateUnits));
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
					TryFillOrder(order.Pair, order.FundId, order.OrderId, order.TradeType, order.OrderType.Value, order.PriceQ2B, order.AmountBase, order.RemainingBase, out var pos, out var his);
					if (his == null)
						continue;

					// Stop the sim for 'RunToTrade' mode
					if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
						Sim.Pause();

					// If 'his' is not null, some or all of the position was filled.
					ReverseBalance(order);
					ApplyToBalance(pos, his);

					// Update the position store
					if (pos != null)
						m_ord[order.OrderId] = pos;
					else
						m_ord.Remove(order.OrderId);

					// Update the history store
					if (his != null)
					{
						var existing = m_his[order.OrderId]?.Trades.Values;
						if (existing != null)
							foreach (var h in existing)
								his.Trades[h.TradeId] = h;

						m_his[order.OrderId] = his;
					}

					// Sanity check that the partial trade isn't gaining or losing amount
					Debug.Assert(order.AmountBase == (pos?.RemainingBase ?? 0m) + (his?.Trades.Values.Sum(x => x.AmountBase) ?? 0m));
				}

				// This is equivalent to the 'DataUpdates' code in 'UpdateOrdersAndHistoryInternal'
				{
					var order_ids = new HashSet<long>();
					var timestamp = Model.UtcNow;

					// Update the collection of existing orders
					foreach (var exch_order in m_ord.Values)
					{
						// Add the order to the collection
						var order = new Order(exch_order);
						Orders[exch_order.OrderId] = order;
						order_ids.Add(order.OrderId);
					}
					var history_updates = m_his.Values.Where(x => x.Created.Ticks >= Exchange.HistoryInterval.End);
					foreach (var exch_order in history_updates.SelectMany(x => x.Trades.Values))
					{
						// Get/Add the completed order
						var order_completed = History.GetOrAdd(exch_order.OrderId, x => new OrderCompleted(x, Exchange.OrderIdToFundId(x),
							exch_order.TradeType, exch_order.Pair));

						// Add the trade to the completed order
						var fill = new TradeCompleted(order_completed, exch_order.TradeId, exch_order.PriceQ2B, exch_order.AmountBase, exch_order.CommissionQuote, exch_order.Created, timestamp);
						order_completed.Trades[fill.TradeId] = fill;

						// Update the history of the completed orders
						Exchange.AddToTradeHistory(order_completed);
					}

					// Remove any orders that are no longer valid
					Exchange.SynchroniseOrders(order_ids, timestamp);

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
						Balance.AssignFundBalance(coin, Fund.Main, b.Total._(coin), b.Held._(coin), timestamp);
					}

					// Notify updated
					Balance.LastUpdated = timestamp;
				}
			}
			#endregion
		}

		/// <summary>Create an order on this simulated exchange</summary>
		public OrderResult CreateOrderInternal(TradePair pair, ETradeType tt, EPlaceOrderType ot, Unit<decimal> amount, Unit<decimal> price)
		{
			// Validate the order
			if (pair.Exchange != Exchange)
				throw new Exception($"SimExchange: Attempt to trade a pair not provided by this exchange");
			if (m_bal[tt.CoinIn(pair)].Available < tt.AmountIn(amount, price))
				throw new Exception($"SimExchange: Submit trade failed. Insufficient balance\n{tt} Pair: {pair.Name}  Vol: {amount.ToString("G8",true)} @  {price.ToString("G8",true)}");
			if (!pair.AmountRangeBase.Contains(amount))
				throw new Exception($"SimExchange: Amount in base currency is out of range");
			if (!pair.AmountRangeQuote.Contains(price * amount))
				throw new Exception($"SimExchange: Amount in quote currency is out of range");

			// Stop the sim for 'RunToTrade' mode
			if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
				Sim.Pause();

			// Allocate a new position Id
			var order_id = ++m_order_id;

			// The order can be filled immediately, filled partially, or not filled and remain as a 'Position'
			TryFillOrder(pair, Fund.Main, order_id, tt, ot, price, amount, amount, out var pos, out var his);
			if (pos != null) m_ord[order_id] = pos;
			if (his != null) m_his[order_id] = his;

			// Apply changes to the balance
			ApplyToBalance(pos, his);

			// Record the filled orders in the trade result
			return new OrderResult(pair, order_id, pos == null, his?.Trades.Values.Select(x =>
				new OrderResult.Fill(x.TradeId, x.PriceQ2B, x.AmountBase, x.CommissionQuote)));
		}

		/// <summary>Cancel an existing position</summary>
		public bool CancelOrderInternal(TradePair pair, long order_id)
		{
			// Doesn't exist?
			if (!m_ord.TryGetValue(order_id, out var pos))
				return false;

			// Remove 'pos'
			m_ord.Remove(order_id);

			// Reverse the balance changes due to 'pos'
			ReverseBalance(pos);
			return true;
		}

		/// <summary>Return the market depth info for 'pair'</summary>
		public MarketDepth MarketDepthInternal(TradePair pair, int depth)
		{
			return m_depth[pair];
		}

		/// <summary>Attempt to make a trade on 'pair' for the given 'price' and base 'amount'</summary>
		private void TryFillOrder(TradePair pair, string fund_id, long order_id, ETradeType tt, EPlaceOrderType ot, Unit<decimal> price, Unit<decimal> initial_amount, Unit<decimal> current_amount, out Order pos, out OrderCompleted his)
		{
			// The order can be filled immediately, filled partially, or not filled and remain as an 'Order'
			var offers = m_depth[pair][tt];

			// Consume orders
			var filled = offers.Consume(pair, ot, price, current_amount, out var remaining);
			Debug.Assert(current_amount == remaining + ((decimal)filled.Sum(x => x.AmountBase))._(pair.Base));

			// The order is partially or completely filled...
			pos = remaining != 0              ? new Order(fund_id, order_id, tt, pair, price, initial_amount, remaining, Model.UtcNow, Model.UtcNow) : null;
			his = remaining != current_amount ? new OrderCompleted(order_id, fund_id, tt, pair) : null;
			foreach (var fill in filled)
				his.Trades.Add(new TradeCompleted(his, ++m_history_id, ((decimal)fill.Price)._(pair.RateUnits), ((decimal)fill.AmountBase)._(pair.Base), (Exchange.Fee * (decimal)fill.AmountQuote)._(pair.Quote), Model.UtcNow, Model.UtcNow));
		}

		/// <summary>Apply the implied changes to the current balance by the given order and completed order</summary>
		private void ApplyToBalance(Order ord, OrderCompleted his)
		{
			var coins = new HashSet<Coin>();
			if (his != null)
			{
				foreach (var fill in his.Trades.Values)
				{
					{// Debt from CoinIn
						var bal = m_bal[fill.CoinIn];
						bal.Total -= fill.AmountIn;
						coins.Add(fill.CoinIn);
					}
					{// Credit to CoinOut
						var bal = m_bal[fill.CoinOut];
						bal.Total += fill.AmountNett;
						coins.Add(fill.CoinOut);
					}
				}
			}
			if (ord != null)
			{
				// Hold for trade
				var bal = m_bal[ord.CoinIn];
				bal.Held += ord.Remaining;
				coins.Add(ord.CoinIn);
			}
		}

		/// <summary>Reverse the balance changes due to 'order'</summary>
		private void ReverseBalance(Order order)
		{
			var bal = m_bal[order.CoinIn];
			bal.Held -= order.Remaining;
		}

		/// <summary>Generate fake market depth for 'pair', using 'latest' as the reference for the current spot price</summary>
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
			var base_value = (double)(decimal)pair.Base.Value;
			var rate_units = pair.RateUnits;

			md.Q2B.Offers.Resize(m_orders_per_book);
			md.B2Q.Offers.Resize(m_orders_per_book);

			// Generate offers with a normal distribution about 'best'
			var range = 0.2 * 0.5 * (best_q2b + best_b2q);
			for (var i = 0; i != m_orders_per_book; ++i)
			{
				// Normal distribution: y = A.e^-(x/B)^2, A = amplitude, B = 'width'
				// As 'i' increases, 'p' is the difference in price from the best price.
				// When 'i' is 0, price is the spot price.
				//var p = ((decimal)(range * (1 - Math.Exp(-Math_.Sqr(2.5 * i / m_orders_per_book)))))._(pair.RateUnits);
				var p = range * Math_.Sqr((double)i / m_orders_per_book);
				md.Q2B.Offers[i] = new Offer(best_q2b + p, RandomAmountBase());
				md.B2Q.Offers[i] = new Offer(best_b2q - p, RandomAmountBase());
			}
			return md;

			double RandomAmountBase()
			{
				// Generate an amount to trade in the application common currency (probably USD).
				// Then convert that to base currency using the 'live' value.
				var common_value = Math.Abs(m_rng.Double(m_order_value_range.Beg, m_order_value_range.End));
				var amount_base = Math_.Div(common_value, base_value, common_value);
				return amount_base;
			}
		}

		/// <summary>Represents the simulated user account on the exchange</summary>
		private class AccountBalance
		{
			public AccountBalance(Coin coin, Unit<decimal> total, Unit<decimal> held)
			{
				Coin = coin;
				Total = total;
				Held = held;
			}
			public Coin Coin { get; }
			public Unit<decimal> Total { get; set; }
			public Unit<decimal> Held { get; set; }
			public Unit<decimal> Available => Total - Held;
		}
	}
}
