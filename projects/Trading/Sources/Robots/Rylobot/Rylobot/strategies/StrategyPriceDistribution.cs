using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.extn;
using pr.ldr;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyPriceDistribution :Strategy
	{
		// Notes:
		// This is intended to work in ranging situations in short time frames.
		//  - Spread must be << range
		//  - Buy the lows, sell the highs

		public const int MAPeriods0 = 5;
		public const int MAPeriods1 = 41;
		public const int ProbHistoryCount = 10;
		public const double OpenPositionProb = 0.85;
		public const double ClosePositionProb = 0.95;
		public const double MinTradeSeparation = 1.0;

		[Parameter("Test Param", DefaultValue = 0.05, MinValue = 0.001, MaxValue = 0.1, Step = 0.001)]
		public double TestParam { get; set; }

		public StrategyPriceDistribution(Rylobot bot, double risk)
			:base(bot, "StrategyPriceDistribution", risk)
		{
			PriceDistribution = new Distribution(Instrument.PipSize);
			m_prob_price = new double[3];
			MA0 = Indicator.EMA(Instrument, MAPeriods0, 10);
			MA1 = Indicator.EMA(Instrument, MAPeriods1, 10);
			Debugging.DumpInstrument += Dump;
		}
		public override void Dispose()
		{
			Debugging.DumpInstrument -= Dump;
			base.Dispose();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				// Volatility >> Spread
				if (Instrument.PriceRange(-10,1).Size < 5.0*Instrument.Spread)
					return 0.0;

				// Good to go
				return 1.0;
			}
		}

		/// <summary>A moving distribution of the price</summary>
		public Distribution PriceDistribution { get; private set; }
		private double[] m_prob_price;

		/// <summary>Moving averages for determining trend sign</summary>
		public Indicator MA0 { get; private set; }
		public Indicator MA1 { get; private set; }

		/// <summary>Extrapolation of the MA</summary>
		public Extrapolation Future0
		{
			get { return MA0.Future; }
		}
		public Extrapolation Future1
		{
			get { return MA1.Future; }
		}

		/// <summary>The sign of the trend direction</summary>
		public int TrendSignSlow
		{
			get
			{
				// Get the trend sign from slow moving data
				var slope = MA1.FirstDerivative(0);
				var trend = Instrument.MeasureTrendFromSlope(slope);
				var diff = MA0[0] - MA1[0];
				var sign =
					Math.Abs(trend) > 0.5 ? Math.Sign(trend) :
					Math.Abs(diff) > Instrument.MCS ? Math.Sign(diff) :
					0;

				return sign;
			}
		}

		/// <summary>The sign of the trend direction including candle patterns to track fast trend changes</summary>
		public int TrendSignFast
		{
			get
			{
				// Get the trend sign from slow moving data
				var sign = TrendSignSlow;

				// Use candle patterns to detected early trend direction changes
				var c0 = Instrument.Compress(0);
				var i0 = c0.Index + Instrument.IdxFirst;
				var i1 = i0 - 1;
				var c1 = Instrument[i1];
				var candle_trend = Instrument.MeasureTrendFromCandles(-5, -1);
				var mcs = Instrument.MCS;

				// Break-out patterns
				if (c0.BodyLength > 2.0 * mcs)
				{
					// 'c0' is a strong trend sign
					if (c0.Sign == sign || sign == 0)
						return c0.Sign;

					// 'c0' is engulfing 'c1'
					if (c0.BodyLength > c1.BodyLength && c0.Sign != c1.Sign)
						return c0.Sign;
				}

				// Reversal pattern
				//if (c1_type.IsIndecision() && !c0_type.IsIndecision() &&
				//	Math.Abs(candle_trend) > 0.7 && Math.Sign(candle_trend) == -c0.Sign)
				//	return c0.Sign;

				return sign;
			}
		}

		/// <summary>The direction sign of the mid price relative to normal. Positive means above normal, Negative means below normal</summary>
		public int ProbSign
		{
			get { return Math.Sign(m_prob_price[1]); }
		}

		/// <summary>The extremity of the current Ask (+1)/Bid(-1)/Mid(0) price compared to normal</summary>
		public double Prob(int sign)
		{
			return m_prob_price[1+sign];
		}

		/// <summary>The Idx of where the MAs might cross in the future (or null)</summary>
		public FutureIdx? NextCrossIndex
		{
			get
			{
				// Get extrapolations of the EMAs
				var future0 = Future0;
				var future1 = Future1;
				if (future0 == null || future1 == null)
					return null;

				// The number of candles around 0 to check
				const int Fwd = +5;

				// Find where the two curves intersect within the range
				var roots = Maths.Intersection((Quadratic)future0.Curve, (Quadratic)future1.Curve).Where(x => x.Within(0,Fwd)).ToArray();
				if (roots.Length == 0)
					return null;

				// The nearest future cross over
				var cross = roots.Min();

				// Find a confidence level for this cross-over
				var conf = future0.Confidence * future1.Confidence;
				return new FutureIdx(cross, conf);
			}
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1), mas:new[] { MA0, MA1 }, high_res:20.0);
			Debugging.Dump(PriceDistribution, +5, prob:new[] { 0.1, 0.5, 0.9 });
		}

		/// <summary>Called when new data is available. Called before 'Step()'</summary>
		protected override void OnTick()
		{
			base.OnTick();
			
			// Update the distribution from the high res data over the last view candles.
			// Ensure we get a distribution over the same period of time (rather than number of ticks)
			PriceDistribution.Reset();
			foreach (var x in Instrument.HighResRange(-ProbHistoryCount, 1)) PriceDistribution.Add(x.Mid);
			if (PriceDistribution.Count < 100)
				return;

			// Find the probability of the price being at it's current level. Scaled so that 0 = mean, -1,+1 are the extremes
			var price = Instrument.LatestPrice;
			m_prob_price = PriceDistribution.Probability(new double[] { price.Bid, price.Mid, price.Ask }).Select(p => 2.0*p - 1.0).ToArray();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (Instrument.NewCandle)
				Dump();

			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice;
			var trend_sign_slow = TrendSignSlow;
			var trend_sign_fast = TrendSignFast;

			// On a significantly extreme price level, buy/sell
			// For opening positions, use Bid when the price is high, Ask when the price is low.
			var sign = -ProbSign;
			var prob = Prob(sign);
			if (Math.Abs(prob) > OpenPositionProb)
			{
				// Todo:
				// The TrendSignFast is preventing trades being taken
				// If this worked it would offset the bad trades at MA crosses
				Dump();

				// Buy the lows, Sell the highs
				var ep = Instrument.CurrentPrice(sign);
				var risk = Risk / 2; // Divide the risk amongst the number of positions that can be opened
				Debugging.Trace("Entry Trigger - {0} - prob={1:N3}".Fmt(sign > 0 ? "Buy" : "Sell", prob));
				Debugging.Trace(" FastTrend = {0}, SlowTrend = {1}".Fmt(trend_sign_fast, trend_sign_slow));

				for (;;)
				{
					// Only allow one trade in each direction using this method
					if (Positions.Count(x => x.Sign() == sign) != 0)
					{
						Debugging.Trace("  -Position with this sign already exists");
						break;
					}
					Debugging.Trace("  +No existing {0} position".Fmt(sign));

					// Only open positions in the direction of the trend
					if (sign != trend_sign_fast)
					{
						Debugging.Trace("  -Against the trend ({0})".Fmt(trend_sign_fast));
						break;
					}
					Debugging.Trace("  +With the trend ({0})".Fmt(trend_sign_fast));

					// Don't open positions too close together
					if (Broker.NearbyPositions(Label, ep, Instrument.MCS * MinTradeSeparation, sign))
					{
						Debugging.Trace("  -Nearby trades with the same sign ({0})".Fmt(sign));
						break;
					}
					Debugging.Trace("  +No close trades with the same sign ({0})".Fmt(sign));

					// Only open positions when the recent price has been heading in the direction we want to trade
					var price_trend = Instrument.HighRes.FirstDerivative(-1);
					if (Math.Sign(price_trend) == -sign)
					{
						Debugging.Trace("  -Price trend ({0:N3}) against trade sign ({1})".Fmt(price_trend, sign));
						break;
					}
					Debugging.Trace("  +Price trend ({0:N3} pips/tick) matches trade sign ({1})".Fmt(price_trend / Instrument.PipSize, sign));

					// Don't open a position if the MA's look like they're about to cross
					// in a direction that would make the trade against the trend.
					var next_cross_idx = NextCrossIndex;
					var ma_cross_sign = MA0[0].CompareTo(MA1[0]);
					if (next_cross_idx != null && next_cross_idx.Value < 2.5 && ma_cross_sign == sign)
					{
						Debugging.Trace("  -MA cross predicted in {0:N3} candles".Fmt(next_cross_idx.Value));
						break;
					}
					Debugging.Trace("  +No MA cross predicted soon");

					// Choose a SL based on SnR levels and the slow MA
					var rel = Math.Abs(ep - MA1[0]) + mcs;

					// Create the positions
					var tt = CAlgo.SignToTradeType(sign);
					var sl = ep - sign * rel;
					var tp = ep + sign * rel * 1.5;
					var vol = Broker.ChooseVolume(Instrument, rel, risk:risk);
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol);
					Debugging.LogTrade(trade);
					//Broker.CreateOrder(trade);
					break;
				}
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
			PositionManagers.Add(new PositionManagerPriceDistribution(this, position));
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Watch for bot stopped</summary>
		protected override void OnBotStopping()
		{
			base.OnBotStopping();

			// Log the whole instrument
			Debugging.DebuggingEnabled = true;
			Debugging.ReportEdge(100);
			Debugging.Dump(Debugging.AllTrades.Values);
			Debugging.Dump(Instrument, mas:new[] { MA0, MA1 });
			Debugging.DebuggingEnabled = false;
		}
	}

	/// <summary>Manage an active position</summary>
	public class PositionManagerPriceDistribution :PositionManager
	{
		public PositionManagerPriceDistribution(Strategy strat, Position pos)
			:base(strat, pos)
		{
		}

		/// <summary>The strategy that created this position manager</summary>
		public new StrategyPriceDistribution Strategy
		{
			get { return (StrategyPriceDistribution)base.Strategy; }
		}

		/// <summary></summary>
		protected override void StepCore()
		{
			// If the SL is on the winning side, let the trade run its course
			var sl_rel = Position.StopLossRel();
			if (sl_rel < 0)
				return;

			var mcs = Instrument.MCS;
			var sign = Position.Sign();
			var price = Instrument.LatestPrice;
			var trend_sign_slow = Strategy.TrendSignSlow;
			var trend_sign_fast = Strategy.TrendSignFast;

			// If the position opposes the slow trend, try to close it at break even
			if (sign == -trend_sign_slow && sign == -trend_sign_fast)
			{
				var cp = price.Price(-sign);

				// Choose a SL based on SnR levels
				var snr = new SnR(Instrument); Debugging.Dump(snr);
				var near = snr.Nearest(cp, -sign, min_dist:mcs*0.5);
				var sl = near != null ? near.Price : cp - sign*mcs;
				if (Math.Abs(sl - Position.EntryPrice) < sl_rel)
				{
					Broker.ModifyOrder(Instrument, Position, sl:sl);
					Debugging.Trace("  +Position opposes the trend (trend={0}), closing at break even".Fmt(trend_sign_slow));
					return;
				}
			}

			// If the trade is better than RtR=1:1, and the close probability is high close the trade
			// For closing positions, use Ask when the price is high, Bit when the price is low
			var prob_sign = Strategy.ProbSign;
			var prob = Strategy.Prob(prob_sign);
			if (sign == prob_sign && Math.Abs(prob) > StrategyPriceDistribution.ClosePositionProb)
			{
				// If the price gradient is in the direction of the trade, don't close it
				var d = Instrument.HighRes.FirstDerivative(-1);
				if (Math.Sign(d) == sign)
					return;

				var min_profit = Instrument.Symbol.QuoteToAcct(sl_rel * Position.Volume);
				if (Position.NetProfit > min_profit)
				{
					Debugging.Trace("  +Close Prob: {0}".Fmt(prob));
					Broker.ClosePosition(Position);
				}
			}
		}
	}
}
