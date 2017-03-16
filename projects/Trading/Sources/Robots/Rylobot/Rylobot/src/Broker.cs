using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary>Manages the account and current risk</summary>
	public class Broker :INotifyPropertyChanged ,IDisposable
	{
		public Broker(Rylobot bot, IAccount acct)
		{
			Bot = bot;
			Update(acct);
		}
		public void Dispose()
		{
			Bot = null;
		}

		/// <summary>The App logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Returns a collection of all positions and pending orders</summary>
		public IEnumerable<Order> AllOrders
		{
			get
			{
				foreach (var pos in Bot.Positions)
					yield return new Order(pos);
				foreach (var ord in Bot.PendingOrders)
					yield return new Order(ord);
			}
		}

		/// <summary>Raised when the AccountId is about to change</summary>
		public event EventHandler AccountChanging;

		/// <summary>Raised after the AccountId has changed</summary>
		public event EventHandler AccountChanged;

		/// <summary>Raised on property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void SetProp<T>(ref T prop, T value, params string[] prop_names)
		{
			if (Equals(prop, value)) return;
			prop = value;

			// Raise notification for each affected property
			foreach (var name in prop_names)
				PropertyChanged.Raise(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>Account Id</summary>
		public int AccountId
		{
			get { return m_account_id; }
			set { SetProp(ref m_account_id, value, R<Broker>.Name(x => x.AccountId)); }
		}
		private int m_account_id;

		/// <summary>True if this account uses real money</summary>
		public bool IsLive
		{
			get { return m_is_live; }
			set { SetProp(ref m_is_live, value, R<Broker>.Name(x => x.IsLive)); }
		}
		private bool m_is_live;

		/// <summary>Currency of the account</summary>
		public string Currency
		{
			get { return m_currency ?? string.Empty; }
			set { SetProp(ref m_currency, value, R<Broker>.Name(x => x.Currency)); }
		}
		private string m_currency;

		/// <summary>Account balance (in units of account currency)</summary>
		public AcctCurrency Balance
		{
			get { return Math.Max((double)m_balance, 0.0); }
			set { SetProp(ref m_balance, value, R<Broker>.Name(x => x.Balance)); }
		}
		private AcctCurrency m_balance;

		/// <summary>Unrealised account balance</summary>
		public AcctCurrency Equity
		{
			get { return m_equity; }
			set { SetProp(ref m_equity, value, R<Broker>.Name(x => x.Equity)); }
		}
		private AcctCurrency m_equity;

		/// <summary>The account leverage</summary>
		public double Leverage
		{
			get { return m_leverage; }
			set { SetProp(ref m_leverage, value, R<Broker>.Name(x => x.Leverage)); }
		}
		private double m_leverage;

		/// <summary>The total risk due to active and pending orders</summary>
		public AcctCurrency TotalRisk
		{
			get { return CurrentRisk + PendingRisk; }
		}

		/// <summary>The risk due to open positions</summary>
		public AcctCurrency CurrentRisk
		{
			get { return m_position_risk; }
			set { SetProp(ref m_position_risk, value, R<Broker>.Name(x => x.CurrentRisk), R<Broker>.Name(x => x.TotalRisk)); }
		}
		private AcctCurrency m_position_risk;

		/// <summary>The risk due to pending orders</summary>
		public AcctCurrency PendingRisk
		{
			get { return m_pending_risk; }
			set { SetProp(ref m_pending_risk, value, R<Broker>.Name(x => x.PendingRisk), R<Broker>.Name(x => x.TotalRisk)); }
		}
		private AcctCurrency m_pending_risk;

		/// <summary>The total risk due to active and pending orders (as a percent of balance)</summary>
		public double TotalRiskPC
		{
			get { return 100.0 * (double)TotalRisk / (double)Balance; }
		}

		/// <summary>The risk due to open positions (as a percent of balance)</summary>
		public double CurrentRiskPC
		{
			get { return 100.0 * (double)CurrentRisk / (double)Balance; }
		}

		/// <summary>The risk due to pending orders (as a percent of balance)</summary>
		public double PendingRiskPC
		{
			get { return 100.0 * (double)PendingRisk / (double)Balance; }
		}

		/// <summary>The maximum allowed risk at any one point in time</summary>
		public AcctCurrency AllowedRisk
		{
			get
			{
				// Find the percentage of balance available to risk
				var max_risk_pc = Bot.Settings.MaxRiskPC;
				return Balance * max_risk_pc * 0.01;
			}
		}

		/// <summary>Returns the maximum amount (in account currency) to risk given the current balance and current existing risk</summary>
		public AcctCurrency BalanceToRisk
		{
			get
			{
				// Find the percentage of balance available to risk
				var max_risk_pc = Bot.Settings.MaxRiskPC;

				// Determine how much room is left given existing risk
				var available_risk_pc = max_risk_pc - TotalRiskPC;
				if (available_risk_pc <= 0)
					return 0.0;

				// Find the account currency value of the available risk
				var balance_to_risk = Balance * available_risk_pc * 0.01;
				return balance_to_risk;
			}
		}

		/// <summary>Temporarily suspend risk checks</summary>
		public Scope SuspendRiskCheck(bool ignore_risk = false)
		{
			return Scope.Create(
				() => ++m_suspend_risk_check,
				() =>
				{
					if (--m_suspend_risk_check != 0) return;
					Debug.Assert(m_suspend_risk_check == 0);
					if (!ignore_risk && MaxRisk > AllowedRisk)
						throw new Exception("Maximum risk exceeded");
				});
		}
		private int m_suspend_risk_check;

		/// <summary>Creates a pending order. Throws if insufficient risk available</summary>
		public PendingOrder CreatePendingOrder(Trade trade, bool rethrow)
		{
			try
			{
				// Handle invalid 'trade's by returning null
				if (trade.Error != null)
				{
					Bot.Print(trade.Error.Message);
					return null;
				}

				// Check that placing this order will not exceed the maximum risk
				if (m_suspend_risk_check == 0)
				{
					var trades = AllOrders.Concat(new Order(trade, active:true)).ToList();
					var max_risk = FindMaxRisk(trades);
					if (AllowedRisk - max_risk < -Maths.TinyD)
						throw new Exception("Trade disallowed, exceeds allowable risk");
				}

				// Place the pending order.
				var r = (TradeResult)null;

				// Buy from below or Sell from above is a limit order
				if ((trade.TradeType == TradeType.Buy && trade.EP < trade.Instrument.CurrentPrice(+1)) ||
					(trade.TradeType == TradeType.Sell && trade.EP > trade.Instrument.CurrentPrice(-1)))
				{
					r = Bot.PlaceLimitOrder(trade.TradeType, trade.Instrument.Symbol, trade.Volume, trade.EP, trade.Label, trade.SL_pips, trade.TP_pips, trade.Expiration);
					if (!r.IsSuccessful)
						throw new Exception("Place limit order failed: {0}".Fmt(r.Error));
				}

				// Buy from above or Sell from below is a stop order
				if ((trade.TradeType == TradeType.Buy && trade.EP > trade.Instrument.CurrentPrice(+1)) ||
					(trade.TradeType == TradeType.Sell && trade.EP < trade.Instrument.CurrentPrice(-1)))
				{
					r = Bot.PlaceStopOrder(trade.TradeType, trade.Instrument.Symbol, trade.Volume, trade.EP, trade.Label, trade.SL_pips, trade.TP_pips, trade.Expiration);
					if (!r.IsSuccessful)
						throw new Exception("Place stop order failed: {0}".Fmt(r.Error));
				}

				// Neither condition valid? not sure this is possible
				if (r == null)
					throw new Exception("Pending order not placed");

				// Update the order
				Debugging.LogOrder(r.PendingOrder, true);

				// Update the account balance
				Update();

				// Return the created order
				return r.PendingOrder;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return null;
			}
		}
		public PendingOrder CreatePendingOrder(Trade trade)
		{
			return CreatePendingOrder(trade, false);
		}

		/// <summary>Update a pending order</summary>
		public bool ModifyPendingOrder(PendingOrder ord, Trade trade, bool rethrow)
		{
			try
			{
				// Check that changing this order will not exceed the maximum risk
				if (m_suspend_risk_check == 0)
				{
					var trades = AllOrders.Where(x => x.Id != ord.Id).Concat(new Order(trade, active:true)).ToList();
					var max_risk = FindMaxRisk(trades);
					if (max_risk > AllowedRisk)
						throw new Exception("Modification disallowed, exceeds allowable risk");
				}

				// Modify the pending order
				var r = Bot.ModifyPendingOrder(ord, trade.EP, trade.SL_pips, trade.TP_pips, trade.Expiration);
				if (!r.IsSuccessful)
					throw new Exception("Modifying pending order failed: {0}".Fmt(r.Error));

				// Replace the order with updated info
				Debugging.LogOrder(ord, update_instrument:true);

				// Update the account balance
				Update();

				return true;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return false;
			}
		}
		public bool ModifyPendingOrder(PendingOrder ord, Trade trade)
		{
			return ModifyPendingOrder(ord, trade, false);
		}

		/// <summary>Cancel a pending order</summary>
		public PendingOrder CancelPendingOrder(PendingOrder ord, bool rethrow)
		{
			try
			{
				// Allow closing null
				if (ord == null)
					return null;

				// Update the order
				Debugging.LogOrder(ord, true);

				var r = Bot.CancelPendingOrder(ord);
				if (!r.IsSuccessful)
					throw new Exception("Close position failed: {0}".Fmt(r.Error));

				// Update the account balance
				Update();
				return null;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return null;
			}
		}
		public PendingOrder CancelPendingOrder(PendingOrder ord)
		{
			return CancelPendingOrder(ord, false);
		}

		/// <summary>Cancel a collection of pending orders</summary>
		public void CancelPendingOrders(IEnumerable<PendingOrder> orders)
		{
			foreach (var ord in orders.ToArray())
				CancelPendingOrder(ord);
		}

		/// <summary>Close all positions with the given label</summary>
		public void CancelAllPendingOrders(string label)
		{
			CancelPendingOrders(Bot.PendingOrders.Where(x => x.Label == label));
		}

		/// <summary>Creates a market order. Throws if insufficient risk available</summary>
		public Position CreateOrder(Trade trade, bool rethrow)
		{
			try
			{
				// Handle invalid 'trade's by returning null
				if (trade.Error != null)
				{
					Bot.Print(trade.Error.Message);
					return null;
				}

				// Check that placing this order will not exceed the maximum risk
				if (m_suspend_risk_check == 0)
				{
					var trades = AllOrders.Concat(new Order(trade, active:true)).ToList();
					var max_risk = FindMaxRisk(trades);
					if (AllowedRisk - max_risk < -Maths.TinyD)
						throw new Exception("Trade disallowed, exceeds allowable risk");
				}

				// Place the order
				var r = Bot.ExecuteMarketOrder(trade.TradeType, trade.Instrument.Symbol, trade.Volume, trade.Label, (double?)trade.SL_pips, (double?)trade.TP_pips);
				if (!r.IsSuccessful)
					throw new Exception("Execute market order failed: {0}".Fmt(r.Error));

				// Output the order that was placed
				Debugging.LogTrade(r.Position, live:true, update_instrument:true);

				// Update the account balance
				Update();

				// Return the created position
				return r.Position;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return null;
			}
		}
		public Position CreateOrder(Trade trade)
		{
			return CreateOrder(trade, false);
		}

		/// <summary>Change the values of an existing order</summary>
		public bool ModifyOrder(Position pos, Trade trade, bool rethrow = false)
		{
			try
			{
				// Check that changing this order will not exceed the maximum risk
				if (m_suspend_risk_check == 0)
				{
					var trades = AllOrders.Where(x => x.Id != pos.Id).Concat(new Order(trade, active:true)).ToList();
					var max_risk = FindMaxRisk(trades);
					if (max_risk > AllowedRisk)
						throw new Exception("Modification disallowed, exceeds allowable risk");
				}

				// Modify the order
				var r = Bot.ModifyPosition(pos, (double?)trade.SL, (double?)trade.TP);
				if (!r.IsSuccessful)
					throw new Exception("Modifying market order failed: {0}".Fmt(r.Error));

				// Replace the order with updated info
				Debugging.LogTrade(pos, live:true, update_instrument:true);

				// Update the account balance
				Update();

				return true;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return false;
			}
		}

		/// <summary>Change the SL or TP on a position</summary>
		public bool ModifyOrder(Instrument instr, Position position, QuoteCurrency? sl = null, QuoteCurrency? tp = null)
		{
			// 'tp' is optional, it set it replaces the TP on 'position'
			Debug.Assert(instr.SymbolCode == position.SymbolCode);

			var trade = new Trade(instr, position);
			if (sl != null) trade.SL = sl;
			if (tp != null) trade.TP = tp;
			return ModifyOrder(position, trade);
		}

		/// <summary>Close an open position</summary>
		public Position ClosePosition(Position pos, bool rethrow)
		{
			try
			{
				// Allow closing null
				if (pos == null)
					return null;

				// Update the position
				Debugging.LogTrade(pos, live:false, update_instrument:true);

				var r = Bot.ClosePosition(pos);
				if (!r.IsSuccessful)
					throw new Exception("Close position failed: {0}".Fmt(r.Error));

				// Update the account balance
				Update();
				return null;
			}
			catch (Exception ex)
			{
				Bot.Print(ex.Message);
				if (rethrow) throw;
				return null;
			}
		}
		public Position ClosePosition(Position pos)
		{
			return ClosePosition(pos, false);
		}

		/// <summary>Close a range of positions</summary>
		public void ClosePositions(IEnumerable<Position> positions)
		{
			foreach (var pos in positions.ToArray())
				ClosePosition(pos);
		}

		/// <summary>Close all positions with the given label</summary>
		public void CloseAllPositions(string label)
		{
			ClosePositions(Bot.Positions.Where(x => x.Label == label));
		}

		/// <summary>
		/// Set the SL/TP on an existing position so that there's a chance it'll close in profit.
		/// Sets the SL to the wick limit of the latest and previous candle.
		/// This will only ever reduce the SL distance from it's current value.
		/// Set 'tp' to chance the current TP value as well (only if the SL is changed tho).
		/// Returns true if the SL was changed</summary>
		public bool CloseByStopLoss(Instrument instr, Position position, QuoteCurrency? tp = null)
		{
			// 'tp' is optional, it set it replaces the TP on 'position'
			Debug.Assert(instr.SymbolCode == position.SymbolCode);

			// Set the SL to just past the last candle
			var price = instr.CurrentPrice(-position.Sign());
			var sl = position.Sign() > 0
				? Math.Min(instr[-1].WickLimit(-1), instr[0].WickLimit(-1)) - instr.Spread
				: Math.Max(instr[-1].WickLimit(+1), instr[0].WickLimit(+1)) + instr.Spread;

			// Only make the SL smaller
			var sl_rel = position.Sign() * (position.EntryPrice - sl);
			if (sl_rel > position.StopLossRel())
				return false;

			// Move the stop loss to just past the current price
			var trade = new Trade(instr, position) { SL = sl, TP = tp ?? position.TakeProfit };
			if (!ModifyOrder(position, trade))
				ClosePosition(position); // Something went wrong, just close now

			return true;
		}

		/// <summary>Set the SL or TP for a position such that it will close when the price hits 'price'</summary>
		public bool CloseAt(Instrument instr, Position position, QuoteCurrency price)
		{
			// 'tp' is optional, it set it replaces the TP on 'position'
			Debug.Assert(instr.SymbolCode == position.SymbolCode);

			// If the position already has a TP/SL at 'price' then don't change anything
			if ((position.StopLoss.HasValue && Maths.FEql(position.StopLoss.Value, price)) ||
				(position.TakeProfit.HasValue && Maths.FEql(position.TakeProfit.Value, price)))
				return false;

			var trade = (Trade)null;
			if (position.TradeType == TradeType.Buy)
			{
				// If the current price is not between 'price' and 'position.StopLoss' move the SL to 'price'
				if (!instr.CurrentPrice(-1).Within(position.StopLoss ?? -double.MaxValue, price))
					trade = new Trade(instr, position) { SL = price - instr.Spread };

				// If the current price is not between 'price' and 'position.TakeProfit' move the TP to 'price'
				else if (!instr.CurrentPrice(+1).Within(price, position.TakeProfit ?? +double.MaxValue))
					trade = new Trade(instr, position) { TP = price };

				// If the current price spans 'price', set the TP and SL one spread distance on either side of 'price'
				else
					trade = null;//new Trade(instr, position) { SL = price - instr.Spread, TP = price + instr.Spread };
			}
			else
			{
				// If the current price is not between 'price' and 'position.StopLoss' move the SL to 'price'
				if (!instr.CurrentPrice(+1).Within(price, position.StopLoss ?? +double.MaxValue))
					trade = new Trade(instr, position) { SL = price + instr.Spread };

				// If the current price is not between 'price' and 'position.TakeProfit' move the TP to 'price'
				else if (!instr.CurrentPrice(-1).Within(position.TakeProfit ?? -double.MaxValue, price))
					trade = new Trade(instr, position) { TP = price };

				// If the current price spans 'price', set the TP and SL one spread distance on either side of 'price'
				else
					trade = null;//new Trade(instr, position) { SL = price + instr.Spread, TP = price - instr.Spread };
			}
			if (trade != null)
			{
				// Move the SL/TP
				ModifyOrder(position, trade);
			}
			else
			{
				// Close immediately if the price spread spans 'price'
				ClosePosition(position);
			}
			return true;
		}

		/// <summary>Move the SL so that position can only close at break even</summary>
		public bool MoveToBreakEven(Instrument instr, Position position, AcctCurrency? profit = null)
		{
			profit = profit ?? 0;
			if (position.NetProfit < profit)
				return false;

			var sl = position.EntryPrice + position.Sign() * instr.Symbol.AcctToQuote(profit.Value / position.Volume);
			var trade = new Trade(instr, position) { SL = sl };
			ModifyOrder(position, trade);
			return true;
		}

		/// <summary>Look for positions that cancel out and close them, ensuring a net profit</summary>
		public void CancelOutPositions(string label, int trend_sign)
		{
			// Get the winning and losing positions
			var winners = Bot.Positions.Where(x => x.Label == label && x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToList();                           // From biggest to smallest
			var losers  = Bot.Positions.Where(x => x.Label == label && x.Sign() != trend_sign && x.NetProfit < 0).OrderBy(x => +x.NetProfit).ToList(); // From worst to best
			if (winners.Count != 0 && losers.Count != 0)
			{
				// Find the totals of winning and losing positions
				var win_total = winners.Sum(x => x.NetProfit);
				var los_total = losers.Sum(x => -x.NetProfit);

				// More winners than losers, close as few winners as needed
				if (win_total > los_total)
				{
					for (int i = winners.Count; i-- != 0;)
					{
						if (los_total > win_total - winners[i].NetProfit) break;
						win_total -= winners[i].NetProfit;
						winners.RemoveAt(i);
					}
				}

				// More losers than winners, close as many losers as we can
				else if (los_total > win_total)
				{
					los_total = 0;
					for (int i = 0; i != losers.Count; ++i)
					{
						// Find the first loser less than 'win_total'
						for (;losers.Count != i;)
						{
							if (los_total + -losers[i].NetProfit < win_total) break;
							losers.RemoveAt(i);
						}
						if (losers.Count == i) break;
						los_total += -losers[i].NetProfit;
					}
				}

				// Anything cancel out?
				if (winners.Count != 0 && losers.Count != 0)
				{
					var bal = winners.Sum(x => x.NetProfit) - losers.Sum(x => -x.NetProfit);
					Debugging.Trace("Cancelling positions! Net: {0}".Fmt(bal));
					Debug.Assert(bal >= 0.0);

					ClosePositions(winners);
					ClosePositions(losers);
				}
			}
		}

		/// <summary>Return the maximum SL value (in quote currency) for the given volume</summary>
		public QuoteCurrency MaxSL(Instrument instr, long volume)
		{
			// Find the amount we're allowed to risk
			// Scale down a little bit to prevent issues comparing the returned SL to BalanceToRisk
			var balance_to_risk = BalanceToRisk * 0.99;
			if (balance_to_risk == 0)
				return 0.0;

			// Get this amount in quote currency
			var sl = instr.Symbol.AcctToQuote(balance_to_risk / volume);
			return sl;
		}

		/// <summary>Return the maximum SL value (in pips) for the given volume</summary>
		public Pips MaxSL_pips(Instrument instr, long volume)
		{
			// Convert to pips
			var sl = MaxSL(instr, volume);
			return instr.Symbol.QuoteToPips(sl);
		}

		/// <summary>Return the maximum volume to trade given a relative stop loss value</summary>
		public long ChooseVolume(Instrument instr, QuoteCurrency sl, double? risk = null)
		{
			// Find the amount we're allowed to risk
			// Scale down a little bit to prevent issues comparing the returned SL to BalanceToRisk
			var balance_to_risk = BalanceToRisk * 0.99;
			if (balance_to_risk == 0)
				return 0;

			// Get this amount in quote currency
			var volume = (risk ?? 1.0) * balance_to_risk / instr.Symbol.QuoteToAcct(sl);
			return instr.Symbol.NormalizeVolume(volume, RoundingMode.Down);
		}

		/// <summary>Calculate the maximum risk given the current positions/orders (in account currency). (positive is risk, negative is guaranteed win)</summary>
		public AcctCurrency MaxRisk
		{
			get { return FindMaxRisk(AllOrders); }
		}

		/// <summary>Find the maximum risk from the given positions, orders, and virtual trades. (positive is risk, negative is guaranteed win)</summary>
		public AcctCurrency FindMaxRisk(IEnumerable<Order> trades)
		{
			// Determine the risk from the given positions/orders/virtual trades (assuming ideal liquidity).
			// This is not as simple as just adding up SL amounts because of hedging and trades without SL/TP values.
			// Each trade within the same symbol is a straight line on a profit vs. price plot.
			// The line is a ray or line segment if one or both of SL/TP are defined.

			// These lines define a binary space partition tree. Starting at the current price,
			// we can determine a gradient of the profit/price. Price can then either go up or
			// down to the nearest trade event (SL/TP/order fill). We need to do a tree search
			// to test all possible outcomes.

			// The combined maximum profit/loss
			var total_risk = (AcctCurrency)0;

			// Find all the unique symbols
			var symbols = trades.Select(x => x.SymbolCode).Distinct();

			// Handle each symbol separately. Since each symbol is assumed to
			// be independent the risk/profit from each symbol can just be added.
			foreach (var sc in symbols)
			{
				var sym = Bot.GetSymbol(sc);
				var sym_trades = trades.Where(x => x.SymbolCode == sc).ToList();

				// Quick case if there is only one position/order for this symbol
				if (sym_trades.Count == 1)
				{
					var trade = sym_trades[0];
					var risk = trade.StopLossRel * trade.Volume;
					total_risk += sym.QuoteToAcct(risk);
				}
				else
				{
					// Return the smallest distance from 'price' to a trade event associated with 'ord', assuming price moves in 'direction'.
					Func <Order, QuoteCurrency, int, QuoteCurrency> Distance = (Order ord, QuoteCurrency price, int direction) =>
					{
						var dist = (QuoteCurrency)double.PositiveInfinity;
						if (ord.TreatAsActive)
						{
							// Test the SL and TP to see which is the correct side of 'price' and is the nearest
							var d_sl = direction * (ord.SL - price);
							if (d_sl != null && d_sl >= 0 && d_sl < dist)
								dist = d_sl.Value;

							var d_tp = direction * (ord.TP - price);
							if (d_tp != null && d_tp >= 0 && d_tp < dist)
								dist = d_tp.Value;
						}
						else
						{
							// If this is a pending order, look at the entry price only
							var d_ep = direction * (ord.EP - price);
							if (d_ep >= 0 && d_ep < dist)
								dist = d_ep;
						}
						return dist;
					};

					// Recursively traverse the set of orders exploring all possible outcomes.
					// 'price' is the price level from the last recursion
					// 'pl' is the current profit/loss down this branch of the tree (loss = min, profit = max)
					// 'orders' is the collection of remaining orders
					Func<QuoteCurrency, QuoteCurrency, List<Order>, QuoteCurrency> MaxR = null;
					MaxR = (QuoteCurrency price, QuoteCurrency max_risk, List<Order> orders) =>
					{
						// Find the next SL/TP/Fill to the left and right of 'price'
						var next_l = (Order)null; var dist_l = (QuoteCurrency)double.PositiveInfinity;
						var next_r = (Order)null; var dist_r = (QuoteCurrency)double.PositiveInfinity;
						foreach (var ord in orders)
						{
							var d_l = Distance(ord, price, -1);
							var d_r = Distance(ord, price, +1);
							if (d_l < dist_l) { next_l = ord; dist_l = d_l; }
							if (d_r < dist_r) { next_r = ord; dist_r = d_r; }
						}

						// Consider what the found trade events on the left and right sides do to the maximum risk.
						// Continue the search recursively from these price values.
						if (next_l != null)
						{
							// Find the risk at 'price_l',
							var price_l = price - dist_l;
							var r = (QuoteCurrency)orders.Sum(x => (double)x.ValueAt(price_l, false, false));
							max_risk = Math.Max(max_risk, -r);

							// Recursively search the remaining orders.
							var remaining = orders.ToList();
							if (!next_l.TreatAsActive)
								next_l.TreatAsActive = true; // If the next order is a pending order, 'Fill' it.
							else
								remaining.Remove(next_l);

							max_risk = MaxR(price_l, max_risk, remaining);
						}
						if (next_r != null)
						{
							// Find the risk at 'price_r'
							var price_r = price + dist_r;
							var r = (QuoteCurrency)orders.Sum(x => (double)x.ValueAt(price_r, false, false));
							max_risk = Math.Max(max_risk, -r);

							// Recursively search the remaining orders.
							var remaining = orders.ToList();
							if (!next_r.TreatAsActive)
								next_r.TreatAsActive = true;
							else
								remaining.Remove(next_l);

							max_risk = MaxR(price_r, max_risk, remaining);
						}
						return max_risk;
					};

					// Find the maximum profit/loss for 'sym'
					var max_r = MaxR(sym.CurrentPrice(0), (QuoteCurrency)double.NegativeInfinity, sym_trades);
					total_risk += sym.QuoteToAcct(max_r);
				}
			}
			return total_risk;
		}

		/// <summary>Return the signed net volume for positions tagged with 'label'</summary>
		public long NetVolume(string label)
		{
			var net_volume = 0L;
			foreach (var pos in Bot.Positions.Where(x => x.Label == label))
				net_volume += pos.TradeType.Sign() * pos.Volume;

			return net_volume;
		}

		/// <summary>Return the net profit of positions tagged with 'label'</summary>
		public AcctCurrency NetProfit(string label)
		{
			var net_profit = 0.0;
			foreach (var pos in Bot.Positions.Where(x => x.Label == label))
				net_profit += pos.NetProfit;

			return net_profit;
		}

		/// <summary>Update the state of the account</summary>
		public void Update()
		{
			Update(Bot.Account);
			Update(Bot.Positions);
			Update(Bot.PendingOrders);
		}

		/// <summary>Update the state of the account from received account info</summary>
		private void Update(IAccount acct)
		{
			var acct_change = AccountId != acct.Number;
			if (acct_change)
				AccountChanging.Raise(this);

			AccountId = acct.Number;
			IsLive    = acct.IsLive;
			Currency  = acct.Currency;
			Balance   = acct.Balance;
			Equity    = acct.Equity;
			Leverage  = acct.PreciseLeverage;

			if (acct_change)
				AccountChanged.Raise(this);
		}

		/// <summary>Update the risk level from currently active positions</summary>
		private void Update(Positions positions)
		{
			// Add up all the potential losses from current positions
			var risk = (AcctCurrency)0.0;
			foreach (var position in positions.Select(x => new Order(x)))
			{
				// The stop loss will be negative if it is on the 'win' side of the entry price, regardless of trade type.
				var sym = Bot.GetSymbol(position.SymbolCode);
				risk += sym.QuoteToAcct(position.StopLossRel) * position.Volume;
			}

			// Update the current risk from active positions
			CurrentRisk = risk;
		}

		/// <summary>Update the risk level from pending orders positions</summary>
		private void Update(PendingOrders orders)
		{
			// Add up all potential losses from pending orders
			var risk = (AcctCurrency)0.0;
			foreach (var order in orders.Select(x => new Order(x)))
			{
				// The stop loss will be negative if it is on the 'win' side
				// of the entry price, regardless of trade type.
				var sym = Bot.GetSymbol(order.SymbolCode);
				risk += sym.QuoteToAcct(order.StopLossRel) * order.Volume;
			}

			PendingRisk = risk;
		}
	}
}

	
			///// <summary>The free margin of the current account.</summary>
			//public double FreeMargin { get; set; }


			///// <summary>Represents the margin of the current account.</summary>
			//public double Margin { get; set; }

			///// <summary>
			///// Represents the margin level of the current account.
			///// Margin level (in %) is calculated using this formula: Equity / Margin * 100.</summary>
			//public double? MarginLevel { get; set; }

			///// <summary>Unrealised gross profit</summary>
			//public double UnrealizedGrossProfit { get; set; }

			///// <summary>Unrealised net profit</summary>
			//public double UnrealizedNetProfit { get; set; }
