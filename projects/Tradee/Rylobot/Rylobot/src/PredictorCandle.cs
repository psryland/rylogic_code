using System;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary>Uses candle patterns to predict price direction</summary>
	public class PredictorCandle :Predictor
	{
		public PredictorCandle(Rylobot bot)
			:base(bot, "PredictorCandle")
		{ }

		/// <summary>Look for predictions with each new data element</summary>
		protected override void Step()
		{
			// Require at least 3 candles
			if (Instrument.Count < 3)
				return;

			// Find the approximate candle size
			var mean_candle_size = Instrument.MeanCandleSize(-100,0);

			// Get the last few candles
			// Make decisions based on 'B', the last closed candle.
			var A = Instrument[ 0]; var a_type = A.Type(mean_candle_size);
			var B = Instrument[-1]; var b_type = A.Type(mean_candle_size);
			var C = Instrument[-2]; var c_type = A.Type(mean_candle_size);

			// The age of 'A' (normalised)
			var a_age = Instrument.AgeOf(A, clamped:true);

			// Measure the strength of the trend leading up to 'B' (but not including)
			var preceding_trend = Instrument.MeasureTrend(-5, -1);

			// Opposing trend
			var opposing_trend =
				!c_type.IsIndecision() && !b_type.IsIndecision() && // Bodies are a reasonable size
				(C.Sign >= 0) != (B.Sign >= 0);       // Opposite sign

			// Engulfing: A trend, ending with a reasonable sized body,
			// followed by a bigger body in the opposite direction.
			if ((opposing_trend) &&                                    // Opposing trend directions
				(c_type.IsTrend() && b_type.IsTrend()) &&                            // Both trend-ish candles
				(B.BodyLength > 1.20 * C.BodyLength) &&                // B is bigger than 120% C
				(Math.Abs(B.Open - C.Close) < 0.05 * B.TotalLength) && // B.Open is fairly close to C.Close
				(Math.Abs(preceding_trend) > 0.8) &&                   // There was a trend leading into B
				(Math.Sign(preceding_trend) != B.Sign))                // The trend was the opposite of B
			{
				Forecast = B.Sign > 0 ? TradeType.Buy : TradeType.Sell;
				Comments = Str.Build(
					"Engulfing: A trend, ending with a reasonable sized body, followed by a bigger body in the opposite direction.\n",
					" C:{0}  B:{1}  A:{2}\n".Fmt(c_type, b_type, a_type),
					" Preceding trend: {0}\n".Fmt(preceding_trend),
					"");
					
				return;
			}

			// Trend, indecision, trend:
			if ((b_type.IsIndecision()) &&                // A hammer, spinning top, or doji
				(Math.Abs(preceding_trend) > 0.8)) // A trend leading into the indecision
			{
				// This could be a continuation or a reversal. Need to look at 'A' to decide
				if (!a_type.IsIndecision()) // If A is not an indecision candle as well
				{
					// Use the indecision candle total length to decide established trend.
					// Measure relative to B.BodyCentre.
					// The stronger the indication, the least old 'A' has to be
					var dist = Math.Abs(A.Close - B.BodyCentre);
					var frac = Maths.Frac(B.TotalLength, dist, B.TotalLength * 2.0);
					if (a_age >= 1.0 - Maths.Frac(0.0, dist, 1.0))
					{
						var reversal = Math.Sign(preceding_trend) != A.Sign;
						Forecast = A.Sign > 0 ? TradeType.Buy : TradeType.Sell;
						Comments = Str.Build(
							"Hammer, Spinning top, or doji:  and a trend leading into the indecision\n",
							" C:{0}  B:{1}  A:{2}\n".Fmt(c_type, b_type, a_type),
							" Preceding trend: {0}\n".Fmt(preceding_trend),
							"");
						return;
					}
				}
			}

			//// Tweezers
			//if ((C.Type == Candle.EType.MarubozuWeakening && B.Type == Candle.EType.MarubozuStrengthening) &&
			//	(Math.Abs(preceding_trend) > 0.8))
			//{
			//	Forecast = B.Sign > 0 ? TradeType.Buy : TradeType.Sell;
			//	Comments = Str.Build(
			//		"Tweezers pattern",
			//		" C:{0}  B:{1}  A:{2}\n".Fmt(c_type, b_type, a_type),
			//		" Preceding trend: {0}\n".Fmt(preceding_trend),
			//		"");
			//	return;
			//}

			//// Continuing trend
			//if ((Math.Abs(preceding_trend) > 0.8) && // Preceding trend
			//	(B.IsTrend && B.Sign == Math.Sign(preceding_trend)))
			//{
			//	Forecast = B.Sign > 0 ? TradeType.Buy: TradeType.Sell;
			//	Comments = "Continuing trend";
			//	return;
			//}

			//// Consolidation
			//if ((Math.Abs(preceding_trend) < 0.5) &&
			//	!B.IsIndecision &&
			//	!B.IsTrend)
			//{
			//	Forecast = null;
			//	Comments = "Consolidation, no trend";
			//	return;
			//}

			// no idea
			Forecast = null;
		}
	}
}
