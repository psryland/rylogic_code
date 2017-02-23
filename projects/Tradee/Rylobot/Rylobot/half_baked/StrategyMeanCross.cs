using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
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
		//  - Wait for significant slopes in the slow EMA
		//  - Enter when price pulls back to the EMA
		const int SlowEMAPeriods = 10;
		const int FastEMAPeriods = 3;

		const double TP_Ratio = 1;
		const double SL_Ratio = 3;

		public StrategyMeanCross(Rylobot bot)
			:base(bot, "StrategyMeanCross")
		{
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				// Approximate candle size
				var mcs = Instrument.MedianCandleSize(-SlowEMAPeriods, 0);

				// Check that the spread << the MCS
				if (Instrument.Spread > 0.3*mcs)
					return 0.0;

				// Wait for significant slope
				var SlopeThreshold = 0.8;
				var ema_slow = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, SlowEMAPeriods);
				var slope = ema_slow.Result.FirstDerivative(-2) / Instrument.Symbol.PipSize;
				if (Math.Abs(slope) < SlopeThreshold) // pips/candle
					return 0.0f;

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
				return 1.0f;
			}
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			// If there is a pending order, wait for it to trigger or expire
			var pending = PendingOrders.FirstOrDefault();
			if (pending != null)
				return;

			// Look for an existing trade for this strategy.
			var position = Positions.FirstOrDefault();
			if (position != null)
				return;

			// If none, look for a good entry point
			var ema_fast = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, FastEMAPeriods);
			var ema_slow = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, SlowEMAPeriods);

			// Look for the price to cross the slow EMA
			var slow = ema_slow.Result.LastValue;
			var fast = ema_fast.Result.LastValue;
			var slope = ema_slow.Result.FirstDerivative(0) / Instrument.Symbol.PipSize;
			var mcs = Instrument.MedianCandleSize(-SlowEMAPeriods, 0);
			var p0 = Instrument.CurrentPrice(Math.Sign(slope));

			var sign = 0;
			var Tolerance = mcs * 0.2;
			if (slope > 0 && fast > slow && p0 < slow + Tolerance) sign = +1;
			if (slope < 0 && fast < slow && p0 > slow - Tolerance) sign = -1;
			if (sign != 0)
			{
				// Create an order
				var sl = 
					sign > 0 ? Instrument.CandleRange(-SlowEMAPeriods,0).Min(x => x.Low) :
					sign < 0 ? Instrument.CandleRange(-SlowEMAPeriods,0).Max(x => x.High) : 0.0;

				var tp = slow + sign * TP_Ratio * mcs;
				var trade = new Trade(Instrument, CAlgo.SignToTradeType(sign), Label, slow, sl, tp, Bot.Broker.MaxVolume(Instrument, SL_Ratio*mcs));
				Bot.Broker.CreateOrder(trade);
			}
		}

		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
		}
	}
}
