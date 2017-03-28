using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyEmaCross :Strategy
	{
		// Notes:
		//  - Open 1K positions
		//  - No SL/TP
		//  - Ensure the direction of profit for the net position is the same as trend direction
		//  - Set SL to break even + amount
		
		private const int EmaPeriods0 = 5;
		private const int EmaPeriods1 = 15;
		private const int EmaPeriodsGlobal = 200;

		public StrategyEmaCross(Rylobot bot, double risk)
			:base(bot, "StrategyEmaCross", risk)
		{
			EMA0 = Indicator.EMA(Instrument, EmaPeriods0);
			EMA1 = Indicator.EMA(Instrument, EmaPeriods1);
			EMAGlobal = Indicator.EMA(Instrument, EmaPeriodsGlobal);
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>The cross over EMA's</summary>
		private Indicator EMA0 { get; set; }
		private Indicator EMA1 { get; set; }
		private Indicator EMAGlobal { get; set; }

		/// <summary>A prediction of the future EMA values</summary>
		private Extrapolation Future0
		{
			get { return EMA0.Future; }
		}
		private Extrapolation Future1
		{
			get { return EMA1.Future; }
		}

		/// <summary>The sign of the trend direction</summary>
		private int TrendSign
		{
			// Use the intersections of two EMAs for trend direction signals
			get { return EMA0[0].CompareTo(EMA1[0]); }
		}

		/// <summary>True if there are positions within 'MinPositionDist' of the current price</summary>
		private bool NearbyPositions
		{
			get
			{
				var min_trade_dist = Instrument.MCS * 0.5;
				var price = Instrument.LatestPrice.Mid;
				return Positions.Any(x => Math.Abs(x.EntryPrice - price) < min_trade_dist);
			}
		}

		/// <summary>The Idx of where the EMAs last crossed (or null)</summary>
		public Idx? LastCrossIndex
		{
			get
			{
				// The number of candles around 0 to check
				const int Bck = -5;

				// Get the current direction
				var dir0 = TrendSign;

				// Look backwards from the latest value for the first cross over
				for (Idx i = -1; i != Bck; --i)
				{
					var dir1 = EMA0[i].CompareTo(EMA1[i]);
					if (dir0 == dir1) continue;
					return i;
				}
				return null;
			}
		}

		/// <summary>The Idx of where the EMAs might cross in the future (or null)</summary>
		public FutureIdx? NextCrossIndex
		{
			get
			{
				// Get extrapolations of the EMAs
				var ema0 = Future0;
				var ema1 = Future1;
				if (ema0 == null || ema1 == null)
					return null;

				// The number of candles around 0 to check
				const int Fwd = +5;

				// Find where the two curves intersect within the range
				var roots = Maths.Intersection((Quadratic)ema0.Curve, (Quadratic)ema1.Curve).Where(x => x.Within(0,Fwd)).ToArray();
				if (roots.Length == 0)
					return null;

				// The nearest future cross over
				var cross = roots.Min();

				// Find a confidence level for this cross-over
				var conf = ema0.Confidence * ema1.Confidence;
				return new FutureIdx(cross, conf);
			}
		}

		/// <summary>The direction of the next cross (set by a call to 'NextCrossIndex')</summary>
		public int NextCrossSign
		{
			get; private set;
		}

		/// <summary>Calculate how close together the EMAs are on the interval [min,max)</summary>
		public double Coincidence(Idx min, Idx max)
		{
			// RMS of difference between the EMAs
			var div = new Avr();
			for (int i = (int)min, iend = (int)Math.Min(1, max); i != iend; ++i)
				div.Add(Maths.Sqr(EMA0[i] - EMA1[i]));

			if (max > 1)
			{
				var future0 = Future0;
				var future1 = Future1;
				if (future0 != null && future1 != null)
				{
					for (int i = 1, iend = (int)max; i != iend; ++i)
						div.Add(Maths.Sqr(future0[i] - future1[i]));
				}
			}

			return Math.Sqrt(div.Mean);
		}

		public override void Dump()
		{
			// When there's a change of trend direction, allow 3 candles for a good close of positions
			Debugging.Dump(Future0.Curve, "ema0", Colour32.Green, new RangeF(-5.0, 5.0));
			Debugging.Dump(Future1.Curve, "ema1", Colour32.Red  , new RangeF(-5.0, 5.0));
			Debugging.LogInstrument();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			Debugging.BreakOnPointOfInterest();
			if (Instrument.NewCandle)
				Dump();

			// The trend direction. Positive if EMA0 > EMA1
			var trend_sign = TrendSign;

			// EMA predictions
			var future0 = Future0;
			var future1 = Future1;

			// Look for crosses of the EMAs
			var prev_cross = LastCrossIndex;
			var next_cross = NextCrossIndex;

			// The direction to open a trade in
			var trade_sign = 0;

			const double CoincidenceLimit = 0.4;

			#region Create Positions

			for (;;)
			{
				// No entry if pending orders
				if (PendingOrders.Any())
					break;

				Debugging.Trace("Idx={0},Tick={1} - Looking for entry - Trend sign: {2}".Fmt(Instrument.Count, Bot.TickNumber, trend_sign));
				var immediate = false;

				// No entry if no prediction of EMAs available
				if (future0 == null || future1 == null)
				{
					Debugging.Trace("   No Entry - EMA prediction not available)");
					break;
				}
				Debugging.Trace("   - EMA prediction confidence: EMA0={0:N3} EMA1={1:N3}".Fmt(future0.Confidence, future1.Confidence));

				// No entry if the predicted EMAs curve in opposing directions
				var curve0 = future0.ddF(0);
				var curve1 = future1.ddF(0);
				if (Math.Sign(curve0) != Math.Sign(curve1))
				{
					Debugging.Trace("   No Entry - EMA1 and EMA0 have opposing curvature");
					break;
				}
				Debugging.Trace("   - EMA1 and EMA0 have agreeing curvature ({0})".Fmt(Math.Sign(curve0) > 0 ? "up" : "down"));
				trade_sign = Math.Sign(curve0);

				// If the latest candle is a Marubozu in the direction of the trend
				// direction after a recent cross, don't worry about coincidence
				var latest = Instrument.HighResCandle(-0.25, +1);
				if (latest.Sign != trend_sign || !latest.Type(Instrument.MCS).IsTrendStrengthening())
				{
					// No entry if EMAs are coincident
					var coincidence = Coincidence(-4, +1) / Instrument.MCS;
					if (coincidence < CoincidenceLimit)
					{
						Debugging.Trace("   No Entry - Coincident ({0} / {1})".Fmt(coincidence, CoincidenceLimit));
						break;
					}
					Debugging.Trace("   - Not coincident: ({0:N3} / {1:N3})".Fmt(coincidence, CoincidenceLimit));
				}
				else
				{
					immediate = true;
					Debugging.Trace("   - Immediate entry! - marubozu in trend direction detected");
				}

				// If the trade sign is opposite to the current trend sign, require an up-coming cross over
				if (trade_sign != trend_sign)
				{
					if (next_cross == null || !next_cross.Value.Within(0,2))
					{
						Debugging.Trace("   No Entry - No EMA cross predicted");
						break;
					}
					Debugging.Trace("   - EMA cross predicted at {0:N3} ({1:N3})".Fmt(next_cross.Value.Index, next_cross.Value.Confidence));
				}
				// If the trade sign is in the same direction, require the EMAs to be divergent
				else
				{
					var slope0 = future0.dF(0);
					var slope1 = future1.dF(0);
					if (Math.Sign(slope0) != Math.Sign(slope1) || Math.Abs(slope0) < Math.Abs(slope1))
					{
						Debugging.Trace("   No Entry - EMAs not divergent");
						break;
					}
					Debugging.Trace("   - EMAs divergent");
				}

				// Require the EMA1 slope to match the trade direction
				if (trade_sign != Math.Sign(EMA1.FirstDerivative(0)))
				{
					Debugging.Trace("   No Entry - EMA1 slope opposes trade direction ({0})".Fmt(-trade_sign));
					break;
				}
				Debugging.Trace("   - EMA1 slope agrees with the trade direction ({0})".Fmt(trade_sign));

				// No entry if last candle opposes the trade direction
				if (!latest.Type(Instrument.MCS).IsIndecision() && latest.Sign != trade_sign)
				{
					Debugging.Trace("   No Entry - Last candle opposes the trade direction");
					break;
				}
				Debugging.Trace("   - Last candle agrees with the trade direction");

				// No entry if the suggested trade is in the same direction as an existing position
				if (Positions.Any(x => x.Sign() == trade_sign))
				{
					Debugging.Trace("   No Entry - All ready holding a position in this direction ({0})".Fmt(trade_sign));
					break;
				}
				Debugging.Trace("   - No existing positions in this direction ({0})".Fmt(trade_sign));

				Dump();

				// Get the trade direction and current price
				var tt = CAlgo.SignToTradeType(trade_sign);
				var price = Instrument.CurrentPrice(trade_sign);

				// Enter at 'EMA1' because EMA0 typically retraces back to EMA1 and the retraces trigger losing trades.
				// Don't enter at the current price because price often spikes quickly initially and then falls back.
				// Scale the entry based on the slope of the EMA1, steep slopes don't break through EMA1 as often.
				var ep = EMA1[0];

				// If the current price is better than the entry price, or we're in a strong trend, enter immediately.
				if (immediate || Math.Sign(ep - price) == trade_sign)
				{
					var exit = Instrument.ChooseTradeExit(tt, price, risk:0.2);
					//if (strong_trend) exit.SL = trade_sign > 0 ? Math.Min(exit.SL, Instrument[-1].High) : Math.Max(exit.SL, Instrument[-1].Low);
					var trade = new Trade(Instrument, tt, Label, price, exit.SL, null, exit.Volume);
					Broker.CreateOrder(trade);
					Debugging.Trace("   Creating position at current price ({0})".Fmt(trade_sign));
				}
				// otherwise, use a pending order to get a good entry
				else
				{
					var exit = Instrument.ChooseTradeExit(tt, 0, ep, risk:0.2);
					var trade = new Trade(Instrument, tt, Label, ep, exit.SL, null, exit.Volume);
					trade.Expiration = Instrument.ExpirationTime(1);
					Broker.CreatePendingOrder(trade);
					Debugging.Trace("   Creating pending order near EMA ({0})".Fmt(trade_sign));
				}
				break;
			}

			#endregion

			#region Close Positions

			// Check each position
			// Aim to close trades at the cross-over price
			foreach (var position in Positions.ToArray())
			{
				// A cross occurred recently that signals time to get out
				if (prev_cross != null && position.Sign() != trend_sign)
				{
					Dump();
					Debugging.Trace("Closing position - Cross occurred at {0:N3}".Fmt(prev_cross.Value));
					var price = EMA1[prev_cross.Value];
					Broker.CloseAt(Instrument, position, price);
				}
				//// A cross is predicted that will signal time to get out
				//else if (next_cross != null && position.Sign() == trend_sign && Math.Sign(future0.ddF(0)) == Math.Sign(future1.ddF(0)))
				//{
				//	Dump();
				//	Debugging.Trace("Closing position - Cross predicted at {0:N3}".Fmt(next_cross.Value));
				//	var price = future1[next_cross.Value];
				//	Broker.CloseAt(Instrument, position, price);
				//}
				// The EMAs crossed without being detected (somehow), or we entered on a prediction and the
				// EMAs haven't yet crossed. Get out if the prediction no longer holds
				else if (position.Sign() != trend_sign && (next_cross == null || !next_cross.Value.Within(0,3)))
				{
					// Allow a grace period of a few candles
					var entry_index = Instrument.IndexAt(position.EntryTime);
					if (entry_index < -1)
					{
						Dump();
						Debugging.Trace("Closing position - No recent cross and position against trend");
						Broker.ClosePosition(position);
					}
					else
					{
						Dump();
						Debugging.Trace("Closing position - No recent cross and position against trend - Stayed due to grace period");
					}
				}
			}
			#endregion
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Return the number of times the EMAs have crossed on the candle range '[idx_min, idx_max)'</summary>
		private int CrossCount(Idx idx_min, Idx idx_max)
		{
			var count = 0;
			for (var i = idx_max - 1; i-- > idx_min;)
			{
				var dir0 = EMA0[i+0].CompareTo(EMA1[i+0]);
				var dir1 = EMA0[i+1].CompareTo(EMA1[i+1]);
				count += dir0 != dir1 ? 1 : 0;
			}
			return count;
		}

		/// <summary>The indices of EMA crosses going backward in time</summary>
		private IEnumerable<Idx> HistoricCrosses()
		{
			for (Idx i = 0; i-- > EMA0.IdxFirst;)
			{
				var dir0 = EMA0[i+0].CompareTo(EMA1[i+0]);
				var dir1 = EMA0[i+1].CompareTo(EMA1[i+1]);
				if (dir0 != dir1)
					yield return i + 1;
			}
		}

	}
}

#if false

			// Close on large equity jumps
			#region Scalp Equity Spikes
			{
				// Track the peak equity since the last position was opened.
				// If a peak is detected, close when it pulls back to 90%
				const double SpikeRatio = 1.01;

				var was_peak = PeakEquity > BaseEquity * SpikeRatio;
				if (Broker.Equity > PeakEquity)
				{
					PeakEquity = Broker.Equity;
					TicksSinceLastPeakEquity = 0;
				}

				if (PeakEquity > BaseEquity * SpikeRatio)
				{
					if (!was_peak)
						Debugging.Trace("Equity Spike detected! {0} / {1}".Fmt(PeakEquity, BaseEquity));

					var frac = Maths.Lerp(0.5, 1.0, ++TicksSinceLastPeakEquity / 100.0);
					if (Broker.Equity < frac * PeakEquity)
					{
						Debugging.Trace("Scalping equity spike: {0} / {1}".Fmt(Broker.Equity, BaseEquity));
						Broker.CloseAllPositions(Label);
						profit_sign = ProfitSign;
					}
				}
			}
			#endregion

			// Look for positions that cancel out
			#region Cancel-Outs
			{
				// Get the winning and losing positions
				var winners = Positions.Where(x => x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToList();                           // From biggest to smallest
				var losers  = Positions.Where(x => x.Sign() != trend_sign && x.NetProfit < 0).OrderBy(x => +x.NetProfit).ToList(); // From worst to best
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

						Broker.ClosePositions(winners);
						Broker.ClosePositions(losers);
						profit_sign = ProfitSign;
					}
				}
			}
			#endregion
			{

				// If there was a recent cross, or there are no positions and no
				// pending orders, try to get in at a good entry.
				// Don't enter if:
				//  - EMA1 does not have a significant slope
				//  - There has been more than one cross in the last few candles
				//  - There is no predicted cross in the next
				if (profit_sign == 0 && !PendingOrders.Any() &&                        // No profit direction and not waiting on a pending order
					CrossCount(-5,+1) < 2 &&                                           // Maximum of one cross in the recent history
					Math.Abs(Instrument.MeasureTrend(EMA1.FirstDerivative())) > 0.5 && // The slow EMA has a trend direction
				//	next_cross == null &&                                              // No predicted upcoming cross
					true)
				{
					// Enter at 'EMA1' because EMA0 typically retraces back to EMA1 and the
					// retraces trigger losing trades
					var ep = EMA1[0];
					var tt = CAlgo.SignToTradeType(trend_sign);
					var price = Instrument.CurrentPrice(trend_sign);

					// If the current price is better than the entry price, enter immediately,
					if (Math.Sign(ep - price) == trend_sign)
					{
						var exit = Instrument.ChooseTradeExit(tt, 0, price, risk:0.5);
						var trade = new Trade(Instrument, tt, Label, price, exit.SL, null, exit.Volume);
						Broker.CreateOrder(trade);
						profit_sign = ProfitSign;
					}
					// otherwise, use a pending order to get a good entry
					else
					{
						var exit = Instrument.ChooseTradeExit(tt, 0, ep, risk:0.5);
						var trade = new Trade(Instrument, tt, Label, ep, exit.SL, null, exit.Volume);
						trade.Expiration = Instrument.ExpirationTime(1);
						Broker.CreatePendingOrder(trade);
						profit_sign = ProfitSign;
					}
				}
			}

			#region Ensure Profit Direction

			// Ensure the current profit direction is zero or with the trend direction
			if (profit_sign != 0 && profit_sign != trend_sign)
			{
				// Try to close positions so that the profit direction matches the trend direction.
				for (;profit_sign != 0 && profit_sign != trend_sign;)
				{
					// The trade directions we want to close
					var tt = trend_sign != 0
						? CAlgo.SignToTradeType(-trend_sign)
						: CAlgo.SignToTradeType(+profit_sign);

					// Close positions in the wrong direction
					var pos = Positions.Where(x => x.TradeType == tt && x.NetProfit > 0).OrderBy(x => -x.NetProfit).FirstOrDefault();
					if (pos == null) break; // nothing to close
					Broker.ClosePosition(pos);
					profit_sign = ProfitSign;
				}

				//// If still in the wrong direction, hedge the current position
				//if (profit_sign != 0 && profit_sign != trend_sign)
				//{
				//	// The trade direction to create
				//	var tt = trend_sign != 0
				//		? CAlgo.SignToTradeType(+trend_sign)
				//		: CAlgo.SignToTradeType(-profit_sign);
				//
				//	// Hedge the current volume
				//	var volume = Math.Abs(NetVolume);
				//	if (volume != 0)
				//	{
				//		// We need to hedge the position for safety
				//		using (Broker.SuspendRiskCheck(true))
				//		{
				//			// Get entry/exit prices for the hedge trade
				//			var exit = Instrument.ChooseTradeExit(tt, 0, Instrument.CurrentPrice(tt.Sign()));
				//
				//			// Use a TP/SL on the hedged positions
				//			foreach (var position in Positions.Where(x => x.Sign() != tt.Sign()))
				//			{
				//				var ord = new Trade(Instrument, position);
				//				ord.TP = ord.EP + ord.Sign * 2*Instrument.Spread;
				//				ord.SL = exit.TP;
				//				Broker.ModifyOrder(position, ord);
				//
				//				// Record the worst TP of the wrong direction trades
				//				exit.SL = tt.Sign() > 0 ? Math.Min(exit.SL, ord.TP.Value) : Math.Max(exit.SL, ord.TP.Value);
				//			}
				//
				//			// Hedge the wrong direction trades
				//			var trade = new Trade(Instrument, tt, Label, exit.EP, exit.SL, exit.TP, volume);
				//			Broker.CreateOrder(trade);
				//
				//			profit_sign = ProfitSign;
				//		}
				//	}
				//}
			}
			#endregion
#endif
