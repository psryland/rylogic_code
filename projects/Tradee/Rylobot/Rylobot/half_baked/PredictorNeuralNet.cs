using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;
using pr.win32;

namespace Rylobot
{
	public class PredictorNeuralNet :Predictor
	{
		// Notes:
		// This is a predictor that uses a trained neural net to spot trade entry points.

		/// <summary>The maximum number of candles to look back in time</summary>
		public const int HistoryWindow = 100;

		public PredictorNeuralNet(Rylobot bot)
			:base(bot, "PredictorNeuralNet")
		{}
		public override void Dispose()
		{
	//		Util.Dispose(ref m_nnet);
			base.Dispose();
		}

		/// <summary>
		/// Update the 'Features' vector with values for the quality of a trade at 'CurrentIndex'.
		/// Note: args.Candle is the candle at 'CurrentIndex'</summary>
		protected override void UpdateFeatureValues(DataEventArgs args)
		{
			// Only make predictions on new candle events
			if (!args.NewCandle)
				return;

			//// Get the candle in question
			//var candle = Instrument[neg_idx];

			//// Assume 'candle' has just opened, so the current price is the Open price
			//var price = candle.Open;

			//// Get the average candle size over the history window
			//var mcs = Instrument.MeanCandleSize(neg_idx - HistoryWindow, neg_idx);

			Features.Clear();
			RSIFeatures();
			CandlePatterns();
#if false
			//{// Preceding candle data
			//	foreach (var c in Instrument.CandleRange(neg_idx - 3, neg_idx))
			//	{
			//		values.Add(c.Open );
			//		values.Add(c.High );
			//		values.Add(c.Low  );
			//		values.Add(c.Close);
			//	}
			//}
			{// Preceding trend
				var trend = Instrument.MeasureTrend(neg_idx - 5, neg_idx);
				values.Add(trend);
			}
			{// SnR Levels
				// Normalised Distance from the nearest SnR level.
				// 'Near' means within the mean candle size of the SnR level
				var snr = new SnR(Instrument, neg_idx - HistoryWindow, neg_idx);
				var lvl = snr.SnRLevels.MinBy(x => Math.Abs(price - x.Price));

				// Normalised distance based on the mean candle size with the candle centred on the SnR level
				var d = Math.Abs(lvl.Price - price);
				var dist = Math.Min(2*d / mcs, 1.0);
				values.Add(dist);
			}

			{// EMA gradients
				var ema = new[]
				{
					Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, 21),
					Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, 55),
					Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, 144),
				};
				for (var j = 0; j != ema.Length; ++j)
				{
					for (var i = 3; i-- != 0;)
					{
						var index = neg_idx - i - Instrument.FirstIdx;
						var dp  = ema[j].Result.FirstDerivative (index) / mcs;
						var ddp = ema[j].Result.SecondDerivative(index) / mcs;
						values.Add(dp);
						values.Add(ddp);
					}
				}
			}
			{// MACD
				// At Buy points:
				//  - MACD (blue line) is negative
				//  - MACD is a minima
				//  - The histogram has a positive gradient (opposite to the MACD)
				//  - The gap between MACD and the signal line (red line) is large
				// At Sell points:
				//  - inverse of buy
				var macd = Bot.Indicators.MacdCrossOver(Bot.MarketSeries.Close, 26, 12, 9);
				values.Add(macd.MACD.LastValue / mcs);
				values.Add(macd.Histogram.LastValue / mcs);

				var index = neg_idx - 1 - Instrument.FirstIdx;
				var y1 = macd.MACD.FirstDerivative(index) / mcs;
				var y2 = macd.MACD.SecondDerivative(index) / mcs;
				values.Add(y1);
				values.Add(y2);

				var h1 = macd.Histogram.FirstDerivative(index) / mcs;
				var h2 = macd.Histogram.SecondDerivative(index) / mcs;
				values.Add(h1);
				values.Add(h2);
			}
			{// Bollinger Bands
				// Buy signal when:
				//  - price touches the centre line during a rising trend
				// Sell signal when:
				//  - price touches the upper band during a falling trend
				// Also, the distance between upper/lower bands means something
				var bb = Bot.Indicators.BollingerBands(Bot.MarketSeries.Close, 21, 2, MovingAverageType.Exponential);

				var index = neg_idx - 1 - Instrument.FirstIdx;
				var c1 = bb.Main.FirstDerivative(index) / mcs;
				values.Add(c1);

				// Signed distance from the upper/lower bands. Positive means outside the bands
				var dist_upper = (price - bb.Top   .LastValue) / (bb.Top .LastValue - bb.Main  .LastValue);
				var dist_lower = (bb.Bottom.LastValue - price) / (bb.Main.LastValue - bb.Bottom.LastValue);
				values.Add(dist_upper);
				values.Add(dist_lower);

				var dist = (bb.Top.LastValue - bb.Bottom.LastValue) / mcs;
				values.Add(dist);
			}
			return values;
#endif
		}

		/// <summary>Get feature details from candle patterns</summary>
		private void CandlePatterns()
		{
			//{// Preceding candle types
			//	for (var j = 3; j-- != 0;)
			//	{
			//		var c = Instrument[neg_idx - j];
			//		var ty = c.Sign * (double)c.Type(mcs);
			//		values.Add(ty);
			//	}
			//}
		}

		/// <summary>Get feature details from the RSI indicator</summary>
		private void RSIFeatures()
		{
			// Value is in the range [0,100] with 30/70 seen as the trigger levels.
			// Entry signals are when the RSI indicator has gone outside the 30/70 level,
			// turned around, then fallen back within the 30/70 range

			var rsi = Bot.Indicators.RelativeStrengthIndex(Bot.MarketSeries.Close, 14);

			// Approximate the RSI curve with a quadratic
			var x = (int)(CurrentIndex - 1 - Instrument.FirstIdx);
			var y = rsi.Result[x];
			var dy = rsi.Result.FirstDerivative(x);
			var ddy = rsi.Result.SecondDerivative(x);
			var q = Quadratic.FromDerivatives(x, y, dy, ddy);

			// Estimate where the RSI will cross back into 30-70 range. If that's the next candle
			// then it's a entry signal

			//var d = rsi.Result.LastValue;
			//	var dist = (d - 50.0) / 20.0;
			//	values.Add(dist);
			
			// For now, just map high/low values
			var score = Maths.Sigmoid(y - 50.0, 20); // [-0.5 = 30, +0.5 = 70]
			Features.Add(new Feature("RSI", score));
		}

	}
}
