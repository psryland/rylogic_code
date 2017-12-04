using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>A mock exchange that replaces a real exchange during back testing</summary>
	public class SimExchange :IDisposable
	{
		// Notes:
		//  - The Exch.Collection members are not used to store the exchange state, the LazyDictionaries are.
		//  - During 'Step' the SimExchange copies data from the Dictionaries to the Collection members,
		//    emulating the behaviour of the 'Exchange.UpdateThreadEntryPoint' function which makes HTTP
		//    requests, storing/overwriting the results in the collection members.
		//  - SimExchange balances only use the 'NoCtx' balance context because real exchanges don't have balance contexts

		private readonly LazyDictionary<TradePair, PriceData> m_src;
		private readonly LazyDictionary<TradePair, MarketDepth> m_depth;
		private readonly LazyDictionary<ulong, Position> m_pos;
		private readonly LazyDictionary<ulong, PositionFill> m_his;
		private readonly LazyDictionary<Coin, FundBalance> m_bal;
		private readonly LazyDictionary<string, Unit<decimal>> m_initial_bal;
		private Random m_rng;
		private ulong m_position_id;
		private ulong m_history_id;
		private RangeF m_order_value_range;
		private double m_spread_frac;
		private int m_orders_per_book;

		public SimExchange(Simulation sim, Exchange exch)
		{
			try
			{
				Sim     = sim;
				Exch    = exch;
				m_src   = new LazyDictionary<TradePair, PriceData>(k => null);
				m_depth = new LazyDictionary<TradePair, MarketDepth>(k => new MarketDepth(k.Base, k.Quote));
				m_pos   = new LazyDictionary<ulong, Position>(k => null);
				m_his   = new LazyDictionary<ulong, PositionFill>(k => null);
				m_bal   = new LazyDictionary<Coin, FundBalance>(k => new FundBalance(Fund.Main, k, m_initial_bal[k], 0m._(k)));
				m_rng   = null;

				// Take a copy of the initial balances (for performance)
				m_initial_bal = new LazyDictionary<string, Unit<decimal>>(k => 1m._(k));
				foreach (var cd in Model.Settings.Coins)
					m_initial_bal.Add(cd.Symbol, cd.BackTestingInitialBalance._(cd.Symbol));

				// Locate data sources
				FindDataSources();

				// Cache settings values
				m_order_value_range = Sim.Settings.OrderValueRange;
				m_spread_frac       = Sim.Settings.SpreadFrac;
				m_orders_per_book   = Sim.Settings.OrdersPerBook;

				// Don't reset here. Wait for 'Model.BackTesting' to be true first.
			}
			catch
			{
				Dispose();
				throw;
			}
		}
		public void Dispose()
		{
			Reset();
		}
		
		/// <summary>The owning simulation</summary>
		private Simulation Sim { get; set; }

		/// <summary>The exchange being simulated</summary>
		private Exchange Exch { get; set; }

		/// <summary>App logic</summary>
		public Model Model { get { return Sim.Model; } }

		/// <summary>Coins associated with this exchange</summary>
		private CoinCollection Coins { get { return Exch.Coins; } }

		/// <summary>The pairs associated with this exchange</summary>
		private PairCollection Pairs { get { return Exch.Pairs; } }

		/// <summary>The balance of the given coin on this exchange</summary>
		private BalanceCollection Balance { get { return Exch.Balance; } }

		/// <summary>Positions by order id</summary>
		private PositionsCollection Positions { get { return Exch.Positions; } }

		/// <summary>Trade history on this exchange, keyed on order ID</summary>
		private HistoryCollection History { get { return Exch.History; } }

		/// <summary>Set up the map from pairs to data sources for each pair</summary>
		public void FindDataSources()
		{
			// Don't need source data for the cross exchange
			if (Exch == Model.CrossExchange)
				return;

			// Find a PriceData instance to use for each pair.
			// Ideally we'd have the source data provided by 'm_exch', but we can use any exchange's data.
			m_src.Clear();
			foreach (var pair in Pairs.Values)
			{
				var time_frame = Sim.TimeFrame;
				var db_filepath = Misc.CandleDBFilePath(pair.NameWithExchange);

				// Assume no data available
				m_src.Add(pair, null);

				if (Path_.FileExists(db_filepath))
				{
					// There is data available from this exchange
					m_src[pair] = Model.PriceData[pair, time_frame];
				}
				else
				{
					// Look for price data from a different exchange
					var pair_name = pair.Name.Replace('/','_');
					var fd = Path_.EnumFileSystem(Path_.Directory(db_filepath), regex_filter:$@"{pair_name}\s*-\s*(?<exchange>.*)\.db").FirstOrDefault();
					if (fd == null)
						continue;

					// Find the corresponding pair on the exchange that the database file is for
					var exch_name = fd.RegexMatch.Groups["exchange"].Value;
					var src_exch = Model.TradingExchanges.FirstOrDefault(x => x.Name == exch_name);
					if (src_exch == null)
						continue;

					// Find the pair on the source exchange
					var src_pair = src_exch.Pairs[pair.Name];
					if (src_pair == null)
						continue;

					// Use the data from 'src_exch'
					m_src[pair] = Model.PriceData[src_pair, time_frame];
				}
			}
		}

		/// <summary>Reset the Sim Exchange</summary>
		public void Reset()
		{
			// Use the same random numbers for each run
			m_rng = new Random(54281);

			// Reset positions
			m_pos.Clear();
			Positions.Clear();
			m_position_id = 0;

			// Reset History
			m_his.Clear();
			History.Clear();
			m_history_id = 0;

			// Reset the balances
			using (Balance.BatchChange())
			{
				m_bal.Clear();
				Balance.Clear();
				foreach (var cd in Model.Settings.Coins)
				{
					m_initial_bal[cd.Symbol] = cd.BackTestingInitialBalance._(cd.Symbol);
				}
				foreach (var coin in Coins.Values)
				{
					m_bal.Add(coin, new FundBalance(Fund.Main, coin, m_initial_bal[coin], 0m._(coin)));
					Balance.Add(coin, new Balances(coin, m_initial_bal[coin], Sim.Clock));
				}
			}

			// Reset the order books of the pairs
			m_depth.Clear();
			foreach (var pair in Pairs.Values)
				pair.MarketDepth.UpdateOrderBook(Enumerable.Empty<Order>(), Enumerable.Empty<Order>());
		}

		/// <summary>Step the exchange</summary>
		public void Step()
		{
			// This function emulates the behaviour of the 'Exchange.UpdateThreadEntryPoint' method
			// and the operations that occur on the exchange.

			// Do nothing if not enabled
			if (!Exch.Enabled)
				return;

			#region Update Market Data
			// Generate and copy the market depth data to each trade pair
			{
				// Update the order book for each pair
				foreach (var pair in Pairs.Values)
				{
					// Get the data source for 'pair'
					var src = m_src[pair];
					if (src == null || src.Count == 0)
						continue;

					// Generate market depth for 'pair', so that the spot price matches 'src.Current'
					var latest = src.Current;
					Debug.Assert(src.TimeFrame == Sim.TimeFrame);
					var md = GenerateMarketDepth(pair, latest, Sim.TimeFrame);

					// Update the order book
					pair.MarketDepth.UpdateOrderBook(md.B2Q.ToArray(), md.Q2B.ToArray());

					// Check. Remember: Q2B spot price = B2Q[0].Price and visa versa
					Debug.Assert(pair.SpotPrice(ETradeType.B2Q) >= ((decimal)latest.Close + (decimal)pair.Spread)._(pair.RateUnits));
					Debug.Assert(pair.SpotPrice(ETradeType.Q2B) <= ((decimal)latest.Close                       )._(pair.RateUnits));
				}

				// Notify updated
				Pairs.LastUpdated = Model.UtcNow;
			}
			#endregion

			#region Positions / History
			// Fill any positions that can be filled, update 'Positions' from 'm_pos', and 'History' from 'm_his'
			{
				// Try to fill orders
				foreach (var position in m_pos.Values.ToArray())
				{
					// Try to fill 'position'.
					// If 'his' is null, then the position can't be filled can remains unchanged
					// If 'pos' is null, then the position is completely filled.
					TryFillOrder(position.Pair, position.OrderId, position.TradeType, position.PriceQ2B, position.VolumeBase, position.RemainingBase, out var pos, out var his);
					if (his == null)
						continue;

					// Stop the sim for 'RunToTrade' mode
					if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
						Sim.Pause();

					// If 'his' is not null, some or all of the position was filled.
					ReverseBalance(position);
					ApplyToBalance(pos, his);

					// Update the position store
					if (pos != null)
						m_pos[position.OrderId] = pos;
					else
						m_pos.Remove(position.OrderId);

					// Update the history store
					if (his != null)
					{
						var existing = m_his[position.OrderId]?.Trades.Values;
						if (existing != null)
							foreach (var h in existing)
								his.Trades[h.TradeId] = h;

						m_his[position.OrderId] = his;
					}

					// Sanity check that the partial trade isn't gaining or losing volume
					Debug.Assert(position.VolumeBase == (pos?.RemainingBase ?? 0m) + (his?.Trades.Values.Sum(x => x.VolumeBase) ?? 0m));
				}

				using (Positions.BatchChange())
				{
					// Update (overwrite) 'Positions' from 'm_pos'
					Positions.RemoveIf(x => !m_pos.ContainsKey(x.OrderId));
					foreach (var pos in m_pos.Values)
					{
						if (Equals(pos, Positions[pos.OrderId])) continue;
						var position = new Position(pos);
						Positions[pos.OrderId] = position;
					}
				}

				// Update (override) 'History' from 'm_history'
				using (History.BatchChange())
				{
					History.RemoveIf(x => !m_his.ContainsKey(x.OrderId));
					foreach (var his in m_his.Values)
					{
						if (Equals(his, History[his.OrderId])) continue;
						var fill = new PositionFill(his);
						History[his.OrderId] = fill;
						Exch.AddToTradeHistory(fill);
					}
				}

				// Notify updated. Notify history before positions so that orders don't "disappear" temporarily
				History.LastUpdated = Model.UtcNow;
				Positions.LastUpdated = Model.UtcNow;
			}
			#endregion

			#region Balance
			{
				// Update (overwrite) 'Balance' from 'm_bal'
				using (Balance.BatchChange())
				{
					// Differential update to prevent unnecessary ListChanging events
					Balance.RemoveIf(x => !m_bal.ContainsKey(x.Coin));
					foreach (var bal in m_bal.Values)
					{
						if (Equals(bal, Balance[bal.Coin])) continue;
						Balance[bal.Coin].Update(bal);
					}
				}

				// Notify updated
				Balance.LastUpdated = Model.UtcNow;
			}
			#endregion
		}

		/// <summary>Create an order on this simulated exchange</summary>
		public TradeResult CreateOrderInternal(TradePair pair, ETradeType tt, Unit<decimal> volume, Unit<decimal> price)
		{
			// Validate the order
			if (pair.Exchange != Exch)
				throw new Exception($"SimExchange: Attempt to trade a pair not provided by this exchange");
			if (m_bal[tt.CoinIn(pair)].Available < tt.VolumeIn(volume, price))
				throw new Exception($"SimExchange: Submit trade failed. Insufficient balance\n{tt} Pair: {pair.Name}  Vol: {volume.ToString("G8",true)} @  {price.ToString("G8",true)}");
			if (!pair.VolumeRangeBase.Contains(volume))
				throw new Exception($"SimExchange: Volume in base currency is out of range");
			if (!pair.VolumeRangeQuote.Contains(price * volume))
				throw new Exception($"SimExchange: Volume in quote currency is out of range");

			// Stop the sim for 'RunToTrade' mode
			if (Sim.RunMode == Simulation.ERunMode.RunToTrade)
				Sim.Pause();

			// Allocate a new position Id
			var order_id = ++m_position_id;

			// The order can be filled immediately, filled partially, or not filled and remain as a 'Position'
			TryFillOrder(pair, order_id, tt, price, volume, volume, out var pos, out var his);
			if (pos != null) m_pos[order_id] = pos;
			if (his != null) m_his[order_id] = his;

			// Apply changes to the balance
			ApplyToBalance(pos, his);

			// Record the filled orders in the trade result
			return new TradeResult(pair, order_id, pos == null, his?.Trades.Values.Select(x => x.TradeId));
		}

		/// <summary>Cancel an existing position</summary>
		public bool CancelOrderInternal(TradePair pair, ulong order_id)
		{
			// Doesn't exist?
			if (!m_pos.TryGetValue(order_id, out var pos))
				return false;

			// Remove 'pos'
			m_pos.Remove(order_id);

			// Reverse the balance changes due to 'pos'
			ReverseBalance(pos);
			return true;
		}

		/// <summary>Return the market depth info for 'pair'</summary>
		public MarketDepth MarketDepthInternal(TradePair pair, int depth)
		{
			return m_depth[pair];
		}

		/// <summary>Attempt to make a trade on 'pair' for the given 'price' and base 'volume'</summary>
		private void TryFillOrder(TradePair pair, ulong order_id, ETradeType tt, Unit<decimal> price, Unit<decimal> initial_volume, Unit<decimal> current_volume, out Position pos, out PositionFill his)
		{
			// The order can be filled immediately, filled partially, or not filled and remain as a 'Position'
			var orders = m_depth[pair][tt];

			// Consume orders
			var filled = orders.Consume(pair, price, current_volume, out var remaining);
			Debug.Assert(current_volume == remaining + filled.Sum(x => x.VolumeBase));

			// The order is partially or wholly filled...
			pos = remaining != 0              ? new Position(Fund.Main, order_id, pair, tt, price, initial_volume, remaining, Model.UtcNow, Model.UtcNow) : null;
			his = remaining != current_volume ? new PositionFill(order_id, tt, pair) : null;
			foreach (var fill in filled)
				his.Trades.Add(new Historic(order_id, ++m_history_id, pair, tt, fill.Price, fill.VolumeBase, Exch.Fee * fill.VolumeQuote, Model.UtcNow, Model.UtcNow));
		}

		/// <summary>Apply the implied changes to the current balance by the given position and position fill</summary>
		private void ApplyToBalance(Position pos, PositionFill his)
		{
			var coins = new HashSet<Coin>();
			if (his != null)
			{
				foreach (var fill in his.Trades.Values)
				{
					{// Debt from CoinIn
						var bal0 = m_bal[fill.CoinIn];
						bal0.Update(Model.UtcNow, total: bal0.Total - fill.VolumeIn);
						coins.Add(fill.CoinIn);
					}
					{// Credit to CoinOut
						var bal0 = m_bal[fill.CoinOut];
						bal0.Update(Model.UtcNow, total: bal0.Total + fill.VolumeNett);
						coins.Add(fill.CoinOut);
					}
				}
			}
			if (pos != null)
			{
				// Hold for trade
				var bal0 = m_bal[pos.CoinIn];
				bal0.Update(Model.UtcNow, held_on_exch: bal0.HeldOnExch + pos.Remaining);
				coins.Add(pos.CoinIn);
			}
		}

		/// <summary>Reverse the balance changes due to 'pos'</summary>
		private void ReverseBalance(Position pos)
		{
			var bal0 = m_bal[pos.CoinIn];
			bal0.Update(Model.UtcNow, held_on_exch: bal0.HeldOnExch - pos.Remaining);
		}

		/// <summary>Generate fake market depth for 'pair', using 'latest' as the reference for the current spot price</summary>
		private MarketDepth GenerateMarketDepth(TradePair pair, Candle latest, ETimeFrame time_frame)
		{
			// Get the market data for 'pair'.
			// Market data is maintained independently to pair.B2Q/Q2B because the rest of the
			// application expects the market data to periodic overwrite the pair's order books.
			var md = m_depth[pair];

			// Get the spot price from the candle close
			var spread = latest.Close * m_spread_frac;
			var best_q2b = ((decimal)(latest.Close + spread))._(pair.RateUnits);
			var best_b2q = ((decimal)(latest.Close         ))._(pair.RateUnits);

			// Make one collection and remove orders within the spread
			var orders = Enumerable
				.Concat(md.Q2B.Take(m_orders_per_book), md.B2Q.Take(m_orders_per_book))
				.Where(x => x.Price < best_b2q || x.Price > best_q2b)
				.ToList();

			// Add the spot price orders
			orders.Add(CreateRandomOrder(best_q2b));
			orders.Add(CreateRandomOrder(best_b2q));

			// Add random orders to grow the collection up to size
			for (; orders.Count < 2*m_orders_per_book; )
				orders.Add(CreateRandomOrder());

			// Sort by price, lowest to highest.
			orders.Sort((l,r) => +l.Price.CompareTo(r.Price));

			// Populate the market data
			var idx = orders.BinarySearch(x => x.Price.CompareTo(best_q2b));
			Debug.Assert(idx > 0 && orders[idx].Price == best_q2b && orders[idx-1].Price == best_b2q);

			var b2q = orders.GetRange(0, idx).ReverseInPlace();
			var q2b = orders.GetRange(idx, orders.Count - idx);
			md.UpdateOrderBook(b2q, q2b);
			return md;

			// Create an order
			Order CreateRandomOrder(Unit<decimal>? price_ = null)
			{
				Debug.Assert(price_ == null || price_.Value <= best_b2q || price_.Value >= best_q2b);
				for (;;)
				{
					// Generate a price if not given
					var mean = (double)(decimal)(best_q2b + best_b2q) / 2.0;
					var price = price_ ?? ((decimal)Math.Max(m_rng.GaussianDouble(mean, mean/100.0), Maths.TinyD))._(pair.RateUnits);
					if (price > best_b2q && price < best_q2b)
						continue; // Invalid price, try again

					// Generate a volume to trade. Convert a value in USD to Base currency using it's approximate value
					var value = (decimal)Math.Abs(m_rng.Double(m_order_value_range.Beg, m_order_value_range.End));
					var scale = pair.Base.AssignedValue;
					var volume = Maths.Div(value, scale, value)._(pair.Base);

					// If the generated order is valid, return it otherwise, try again.
					var order = new Order(price, volume);
					if (order.Validate(pair))
						return order;
				}
			}
		}
	}
}
