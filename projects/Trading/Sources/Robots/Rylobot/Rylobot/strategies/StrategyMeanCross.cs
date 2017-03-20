using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyMeanCross :Strategy
	{
		// Notes:
		// This is intended to work in ranging situations in short time frames.
		//  - Spread must be << range
		//  - Buy the lows, sell the highs

		const int MAPeriods0 = 5;
		const int MAPeriods1 = 55;
		const int ProbHistoryCount = 10;
		const int MaxPositionsLimit = 2;

		public StrategyMeanCross(Rylobot bot)
			:base(bot, "StrategyMeanCross")
		{
			PriceDistribution = new Distribution(Instrument.PipSize);
			MA0 = new Indicator(Instrument, bot.Indicators.ExponentialMovingAverage(Instrument.Data.Close, MAPeriods0).Result);
			MA1 = new Indicator(Instrument, bot.Indicators.ExponentialMovingAverage(Instrument.Data.Close, MAPeriods1).Result);
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>A moving distribution of the price</summary>
		private Distribution PriceDistribution { get; set; }

		/// <summary>Moving averages for determining trend sign</summary>
		private Indicator MA0 { get; set; }
		private Indicator MA1 { get; set; }

		/// <summary>Extrapolation of the MA</summary>
		private Extrapolation Future0
		{
			get { return MA0.Extrapolate(2, MAPeriods0); }
		}
		private Extrapolation Future1
		{
			get { return MA1.Extrapolate(2, MAPeriods1); }
		}

		/// <summary>The trend direction</summary>
		private double Trend
		{
			get
			{
				var slope = MA1.FirstDerivative(0);
				return Instrument.MeasureTrend(slope);
			}
		}

		/// <summary>The sign of the trend direction including candle patterns to track fast trend changes</summary>
		private int TrendSignSlow
		{
			get
			{
				// Get the trend sign from slow moving data
				var trend = Trend;
				var diff = MA0[0] - MA1[0];
				var sign =
					Math.Abs(trend) > 0.5 ? Math.Sign(trend) :
					Math.Abs(diff) > Instrument.MCS ? Math.Sign(diff) :
					0;

				return sign;
			}
		}

		/// <summary>The sign of the trend direction including candle patterns to track fast trend changes</summary>
		private int TrendSignFast
		{
			get
			{
				// Get the trend sign from slow moving data
				var sign = TrendSignSlow;

				// Get the previous two candles
				var c0 = Instrument.Compress(0);
				var c1 = Instrument[c0.Index - 1 + Instrument.IdxFirst];
				var c0_type = c0.Type(Instrument.MCS);
				var c1_type = c1.Type(Instrument.MCS);

				// Use candle patterns to detected early trend direction changes
				if (c0_type.IsTrendStrengthening())
				{
					// 'c0' is a strong trend sign
					if (c0.Sign == sign || sign == 0)
					{
						Debugging.Trace("  +Breakout pattern - trend is {0}".Fmt(c0.Sign));
						return c0.Sign;
					}

					// 'c0' is engulfing 'c1'
					if (c0.BodyLength > c1.BodyLength && c0.Sign != c1.Sign)
					{
						Debugging.Trace("  +Engulfing pattern - trend is {0}".Fmt(c0.Sign));
						return c0.Sign;
					}
				}

				// Reversal pattern
				var candle_trend = Instrument.MeasureTrendFromCandles(-5, -1);
				if (c1_type.IsIndecision() && !c0_type.IsIndecision() &&
					Math.Abs(candle_trend) > 0.7 && Math.Sign(candle_trend) == -c0.Sign)
				{
					Debugging.Trace("  +Reversal pattern - trend is {0}".Fmt(c0.Sign));
					return c0.Sign;
				}
				return sign;
			}
		}

		/// <summary>The equity level due to trades created by this strategy the last time a position was opened</summary>
		private AcctCurrency LastEquity { get; set; }

		/// <summary>True if there are open positions within a range of 'price'</summary>
		private bool NearbyPositions(QuoteCurrency price, int sign)
		{
			var min_trade_dist = Instrument.MCS * 0.5;
			return Positions.Any(x => (sign == 0 || x.Sign() == sign) && Math.Abs(x.EntryPrice - price) < min_trade_dist);
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
		private void Dump()
		{
			Debugging.LogInstrument(mas:new[] { MA0, MA1 });
			Debugging.Dump(PriceDistribution, +5, prob:new[] { 0.1, 0.5, 0.9 });
			Debugging.BreakOnPointOfInterest();
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

			// Find the probability of the price being at it's current level. Scaled so that 0 = mean, -1,+1 are the extremes
			var probs = PriceDistribution.Probability(new double[] { price.Bid, price.Mid, price.Ask }).Select(p => 2.0*p - 1.0).ToArray();

			// Add to the price distribution
			UpdatePriceDistribution();
			if (PriceDistribution.Count < 100)
				return;

			// Choose the ask/bid price depending on the sign of the mid price probability. i.e.
			// For opening positions, use Bid when the price is high, Ask when the price is low.
			// For closing positions, use Ask when the price is high, Bit when the price is low
			var prob_sign  = Math.Sign(probs[1]);
			var open_prob  = prob_sign >= 0 ? probs[0] : probs[2];
			var close_prob = prob_sign >= 0 ? probs[2] : probs[0];

			// On a significantly extreme price level, buy/sell
			if (Math.Abs(open_prob) > 0.85)
			{
				for (;;)
				{
					Dump();

					// Buy the lows, Sell the highs
					var sign = -prob_sign;
					var ep = Instrument.CurrentPrice(sign);
					Debugging.Trace("Entry Trigger - {0} - prob={1:N3} trend={2}".Fmt(sign > 0 ? "Buy" : "Sell", open_prob, trend_sign_fast));

					// Don't open positions too close together
					if (NearbyPositions(ep, sign)) break;
					Debugging.Trace("  +No close trades with the same sign ({0})".Fmt(sign));

					// Only open positions in the direction of the trend
					if (prob_sign == trend_sign_fast) break;
					Debugging.Trace("  +With the trend ({0})".Fmt(trend_sign_fast));

					// Don't open a position if the MA's look like they're about to cross
					// in a direction that would make the trade against the trend.
					var next_cross_idx = NextCrossIndex;
					var ma_cross_sign = MA0[0].CompareTo(MA1[0]);
					if (next_cross_idx != null && next_cross_idx.Value < 2.5 && ma_cross_sign != prob_sign) break;
					Debugging.Trace("  +No MA cross predicted soon");

					// Only allow a maximum of N trades in each direction
					if (Positions.Count(x => x.Sign() == sign) >= MaxPositionsLimit) break;
					Debugging.Trace("  +No at maximum positions limit ({0})".Fmt(MaxPositionsLimit));

					var snr = new SnR(Instrument);
					var near = snr.Nearest(ep, -sign, min_dist:mcs*2.0);
					var rel = near != null ? (QuoteCurrency)Math.Abs(ep - near.Price) * 1.2 : 2.0*mcs;
					Debugging.Dump(snr);

					//var exit = Instrument.ChooseTradeExit(tt, ep, risk:risk, rtr:1.0);
					//var levels = PriceDistribution.Values(new [] { 0.1, 0.9 });
					//var rel = Instrument.MCS * 10; //Math.Abs(levels[1] - levels[0]);
					var risk = 0.5/MaxPositionsLimit;

					// Create the positions
					var tt = CAlgo.SignToTradeType(sign);
					//var sl = (QuoteCurrency?)null;
					//var tp = (QuoteCurrency?)null;
					var sl = ep - sign * rel;
					var tp = ep + sign * rel;
					var vol = Broker.ChooseVolume(Instrument, rel, risk:risk);
					//var volume = Instrument.Symbol.VolumeMin;
					//var volume = Broker.ChooseVolume(Instrument, 5.0 * mcs);
					//var vol = volume + (trend_sign == sign ? Instrument.Symbol.VolumeMin : 0); // Bias trades in the trend direction
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol);
					Broker.CreateOrder(trade);
					break;
				}
			}

			// Look for positions to close
			// On a significantly extreme price level, close positions
			//if (Maths.Abs(close_prob) > 0.9)
			// Close longs on high prices, shorts on low prices

			// Get the latest candle (including prior candles with the same sign)
			var latest = Instrument.Compress(0);

			// Look for positions to close
			foreach (var pos in Positions.ToArray())
			{
				//// If a position opposes the trend try to close it at break even
				//if (pos.Sign() != trend_sign_fast && trend_sign_fast != 0)
				//{
				//	Debugging.Trace("  +Position opposes trend (trend={0}), closing at break even".Fmt(trend_sign_fast));
				//	Broker.CloseAt(Instrument, pos, pos.EntryPrice + pos.Sign() * Instrument.Spread);
				//	continue;
				//}

				//// If the direction of the current candle is in profit for 'pos' leave it
				//if (pos.Sign() == latest.Sign)
				//{
				//	continue;
				//}

				//// Choose a minimum profit at RtR = 1.0 (unless the trend sign has changed)
				//var min_profit = Instrument.Symbol.QuoteToAcct(Math.Abs(pos.StopLossRel()) * pos.Volume);
				//if (pos.NetProfit > min_profit)
				//{
				//	Debugging.Trace("  +Close Prob: {0}".Fmt(close_prob));
				//	Broker.ClosePosition(pos);
				//}

				// Close based on probability sign
				if (pos.Sign() == prob_sign)
				{
					// Set the minimum profit as RtR = 1.0 (unless the trend sign has changed)
					var min_profit =
						pos.Sign() != trend_sign_fast ? 0
						: Instrument.Symbol.QuoteToAcct(Math.Abs(pos.StopLossRel()) * pos.Volume);

					// Reduce the min profit, the older the position is
					var entry_index = Instrument.IndexAt(pos.EntryTime);
					var scale = Maths.Sigmoid(0 - entry_index, ProbHistoryCount);
					min_profit *= scale;

					// If the profit is high enough, close
					if (pos.NetProfit > min_profit)
					{
						Broker.ClosePosition(pos);
						Debugging.Trace("  +Close Prob: {0}".Fmt(close_prob));
					}
				}
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
			//Correlator.Track(position, "Trend", Trend);
			//Correlator.Track(position, "TrendAbs", Math.Abs(Trend));
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
			Debugging.Dump(Instrument, mas:new[] { MA0, MA1 });
		}

		/// <summary>Track the average price, returns true if the average is ok to use</summary>
		private void UpdatePriceDistribution()
		{
			// Ensure we get a distribution over the same period of time
			// (rather than number of ticks)

			// Create a distribution from the high res data over the last view candles
			PriceDistribution.Reset();
			foreach (var x in Instrument.HighResRange(-ProbHistoryCount, 1))
				PriceDistribution.Add(x.Mid);
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				// MCS >> Spread
				if (Instrument.MCS < 5.0 * Instrument.Spread)
					return 0.0;

				// // Approximate candle size
				// var mcs = Instrument.MedianCandleSize(-SlowEMAPeriods, 0);
				// 
				// // Check that the spread << the MCS
				// if (Instrument.Spread > 0.3*mcs)
				// 	return 0.0;
				// 
				// // Wait for significant slope
				// var SlopeThreshold = 0.8;
				// var ema_slow = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, SlowEMAPeriods);
				// var slope = ema_slow.Result.FirstDerivative(-2) / Instrument.Symbol.PipSize;
				// if (Math.Abs(slope) < SlopeThreshold) // pips/candle
				// 	return 0.0f;

				//// Find the recent price range
				//var range = RangeF.Invalid;
				//var avr = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, SlowEMAPeriods).Result.LastValue;
				//for (int i = 0; i != SlowEMAPeriods; ++i)
				//{
				//	range.Encompass(Bot.MarketSeries.High.Last(i));
				//	range.Encompass(Bot.MarketSeries.Low.Last(i));
				//}

				//// Check that the MCS << Range
				//if (mcs > 0.5 * range.Size)
				//	return 0.0f;

				// Good to go
				return 1.0;
			}
		}
	}
}
