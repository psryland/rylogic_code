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

		/// <summary>A cached string builder</summary>
		private StringBuilder m_sb;

		public PredictorNeuralNet(Rylobot bot)
			:base(bot, "PredictorNeuralNet")
		{
			m_sb = new StringBuilder();

			// Create network based on a trained model (for CPU evaluation)
			//var m_nnet = new IEvaluateModelManagedD();

			//var modelFilePath = Path_.CombinePath(Environment.CurrentDirectory, "forex.dnn");
			//var config = string.Format("modelPath=\"{0}\"", modelFilePath);
			//m_nnet.CreateNetwork(config, deviceId: -1);

			// Generate random input values in the appropriate structure and size
			//var inputs = GetDictionary("features", 28*28, 255);

			// We can call the evaluate method and get back the results (single layer)...
			// List<float> outputList = model.Evaluate(inputs, "ol.z");

			// ... or we can preallocate the structure and pass it in (multiple output layers)
		//	outputs = GetDictionary("ol.z", 10, 1);
		//	model.Evaluate(inputs, outputs);                    
		}
		public override void Dispose()
		{
	//		Util.Dispose(ref m_nnet);
			base.Dispose();
		}

		/// <summary>Call whenever the instrument data changes</summary>
		protected override void Step(DataEventArgs args)
		{
			// Only make predictions on new candle events
			if (!args.NewCandle)
				return;

			// Get the feature values for the candles leading up to the latest
			var features = FeatureValues(0);

			// Each feature value should be a valid from [-1,+1]
			// Values > 0.5 indicate buy signals, < -0.5 indicate sell signals
			// This can be replaced by a neural net prediction of the features
			var signal = features.Average(x => x.Value);
			Forecast =
				signal > +0.5 ? (TradeType?)TradeType.Buy :
				signal < -0.5 ? (TradeType?)TradeType.Sell :
				null;
		}

		/// <summary>Return a set of feature values for the quality of a trade at 'neg_idx'</summary>
		public List<Feature> FeatureValues(NegIdx neg_idx)
		{
			//// Get the candle in question
			//var candle = Instrument[neg_idx];

			//// Assume 'candle' has just opened, so the current price is the Open price
			//var price = candle.Open;

			//// Get the average candle size over the history window
			//var mcs = Instrument.MeanCandleSize(neg_idx - HistoryWindow, neg_idx);

			// The collection to return
			var features = new List<Feature>();

			// Check the RSI indicator
			RSIFeatures(features, neg_idx);

			return features;
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
			//{// Preceding candle types
			//	for (var j = 3; j-- != 0;)
			//	{
			//		var c = Instrument[neg_idx - j];
			//		var ty = c.Sign * (double)c.Type(mcs);
			//		values.Add(ty);
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

		/// <summary>Get feature details from the RSI indicator</summary>
		private void RSIFeatures(List<Feature> features, NegIdx neg_idx)
		{
			// Value is in the range [0,100] with 30/70 seen as the trigger levels.
			// Entry signals are when the RSI indicator has gone outside the 30/70 level,
			// turned around, then fallen back within the 30/70 range

			var rsi = Bot.Indicators.RelativeStrengthIndex(Bot.MarketSeries.Close, 14);

			// Approximate the RSI curve with a quadratic
			var x = neg_idx - 1;
			var y = rsi.Result[x];
			var dy = rsi.Result.FirstDerivative(x);
			var ddy = rsi.Result.SecondDerivative(x);
			var q = Quadratic.FromDerivatives(x, y, dy, ddy);

			// Estimate where the RSI will cross back into 30-70 range. If that's the next candle
			// then it's a entry signal

			//var d = rsi.Result.LastValue;
			//	var dist = (d - 50.0) / 20.0;
			//	values.Add(dist);
			
		}


		/// <summary>Returns the feature string for the candle at position 'neg_idx'</summary>
		public string Features(NegIdx neg_idx)
		{
			m_sb.Clear();

			// Output feature values
			m_sb.Append("|features ");
			foreach (var v in FeatureValues(neg_idx))
				m_sb.Append(" ").Append(v.Value);

			return m_sb.ToString();
		}

		/// <summary>A signal feature value</summary>
		public class Feature
		{
			public Feature(string label, double value, string comment)
			{
				Label   = label;
				Value   = value;
				Comment = comment;
			}

			/// <summary>A name for the feature</summary>
			public string Label { get; private set; }

			/// <summary>The feature value</summary>
			public double Value { get; private set; }

			/// <summary>Comments about the feature</summary>
			public string Comment { get; set; }
		}
	}
}
