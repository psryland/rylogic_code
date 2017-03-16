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

		const int MeanPeriods = 55;

		public StrategyMeanCross(Rylobot bot)
			:base(bot, "StrategyMeanCross")
		{
			PriceAverage = new MovingAvr(MeanPeriods);
			SMA = new Indicator(Instrument, bot.Indicators.SimpleMovingAverage(Instrument.Data.Close, MeanPeriods).Result);
			LastStdDev = 0;
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>An average of the price</summary>
		private MovingAvr PriceAverage { get; set; }

		/// <summary>The historic simple moving average</summary>
		private Indicator SMA { get; set; }

		/// <summary>Extrapolation of the MA</summary>
		private Extrapolation Future
		{
			get { return SMA.Extrapolate(MeanPeriods); }
		}
		
		/// <summary>The *signed* index of the last standard deviation level crossed</summary>
		private int LastStdDev { get; set; }

		/// <summary>The sign of the trend direction</summary>
		private int TrendSign
		{
			get
			{
				var slope = SMA.FirstDerivative(0);
				var trend = Instrument.MeasureTrend(slope);
				return Math.Abs(trend) > 0.5 ? Math.Sign(trend) : 0;
			}
		}

		/// <summary>The equity level due to trades created by this strategy the last time a position was opened</summary>
		private AcctCurrency LastEquity { get; set; }

		/// <summary>Debugging output</summary>
		private void Dump()
		{
			Debugging.LogInstrument(smas:new[] { MeanPeriods });
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (Instrument.NewCandle)
				Dump();

			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice.Mid;
			var volume = Broker.ChooseVolume(Instrument, 5.0 * mcs);//Instrument.Symbol.VolumeMin;//

			// Track the average price
			PriceAverage.Add(price);
			if (PriceAverage.Count < PriceAverage.WindowSize)
				return;

			// Only allow a maximum of N bad trades
			const int MaxBadCount = 5;
			var BadThreshold = Broker.Balance * 0.01;
			var bad = Positions.Where(x => x.NetProfit < -BadThreshold).ToArray();
			if (bad.Length >= MaxBadCount)
			{
				// Close the worst
				var worst = bad.OrderBy(x => x.NetProfit).First();
			//	Broker.ClosePosition(worst);
			}

			// Find how far the current price is from the mean in multiples of standard deviation
			var mean = PriceAverage.Mean;//Future[MeanPeriods/2]; //
			var stddev = PriceAverage.PopStdDev;
			var dist = (price - mean) / stddev;

			// Find the s.d. band that 'dist' falls within
			var sd_band = (int)dist; // round toward zero

			// On a change of s.d. band by more than 2, buy/sell
			var sd_change = Math.Abs(LastStdDev - sd_band);
			if (sd_change >= 2)
			{
				Dump();

				if (sd_band >= 2)
				{
					// Buy the lows, sell the highs
					var sign = -Math.Sign(sd_band);
					var tt = CAlgo.SignToTradeType(sign);
					var vol = volume;// + (TrendSign == sign ? Instrument.Symbol.VolumeMin : 0); // Bias trades in the trend direction
					var sl = (QuoteCurrency?)null;//price - sign * stddev * 10.0;
					var tp = (QuoteCurrency?)null;//price + sign * stddev * Math.Abs(dist + 1);
					var trade = new Trade(Instrument, tt, Label, price, sl, tp, vol);
					var pos = Broker.CreateOrder(trade);
					Correlator.Track(pos, "With Trend", TrendSign == sign ? +1 : -1);
				}

				// Look for positions to close
				AcctCurrency MinProfit = Broker.Balance * 0.005;
				foreach (var pos in Positions.Where(x => x.NetProfit > MinProfit).ToArray())
					Broker.ClosePosition(pos);

				LastStdDev = sd_band;
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
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
