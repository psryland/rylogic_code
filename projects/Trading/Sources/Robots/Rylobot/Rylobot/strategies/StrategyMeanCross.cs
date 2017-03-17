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

		const int MAPeriods = 55;

		public StrategyMeanCross(Rylobot bot)
			:base(bot, "StrategyMeanCross")
		{
			PriceDistribution = new Distribution(Instrument.PipSize);
			MA = new Indicator(Instrument, bot.Indicators.ExponentialMovingAverage(Instrument.Data.Close, MAPeriods).Result);
			LastProb = 0;
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>A moving distribution of the price</summary>
		private Distribution PriceDistribution { get; set; }

		/// <summary>The historic simple moving average</summary>
		private Indicator MA { get; set; }

		/// <summary>Extrapolation of the MA</summary>
		private Extrapolation Future
		{
			get { return MA.Extrapolate(1, MAPeriods); }
		}
		
		/// <summary>The last *signed* distance from the mean in units of standard deviation</summary>
		private double LastProb { get; set; }

		/// <summary>The sign of the trend direction</summary>
		private int TrendSign
		{
			get
			{
				var slope = MA.FirstDerivative(0);
				var trend = Instrument.MeasureTrend(slope);
				return Math.Abs(trend) > 0.5 ? Math.Sign(trend) : 0;
			}
		}

		/// <summary>The equity level due to trades created by this strategy the last time a position was opened</summary>
		private AcctCurrency LastEquity { get; set; }

		/// <summary>Debugging output</summary>
		private void Dump()
		{
			var emas = MA.Source is ExponentialMovingAverage ? new [] {MAPeriods } : null;
			var smas = MA.Source is SimpleMovingAverage ? new [] {MAPeriods } : null;
			Debugging.LogInstrument(emas:emas, smas:smas);
			
			{// Show the price distribution
				var vals = PriceDistribution.Values(new[] { 0.1, 0.5, 0.9 });
				var ldr = new pr.ldr.LdrBuilder();
				Debugging.Dump(PriceDistribution, +5, vals[1], ldr);
				ldr.Line("sd", 0xFF8080FF
					,new v4( 5-Instrument.IdxFirst, (float)vals[0], 0, 1)
					,new v4(10-Instrument.IdxFirst, (float)vals[0], 0, 1));
				ldr.Line("sd", 0xFF8080FF
					,new v4( 5-Instrument.IdxFirst, (float)vals[2], 0, 1)
					,new v4(10-Instrument.IdxFirst, (float)vals[2], 0, 1));
				ldr.ToFile(Debugging.FP("distribution.ldr"));
			}

			Debugging.BreakOnPointOfInterest();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (Instrument.NewCandle)
				Dump();

			AcctCurrency MinProfit = Broker.Balance * 0.005;
			AcctCurrency MaxLoss   = Broker.Balance * 0.01;

			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice.Mid;
			var volume = Instrument.Symbol.VolumeMin;
			//var volume = Broker.ChooseVolume(Instrument, 5.0 * mcs);

			// Add to the price distribution
			UpdatePriceDistribution();
			if (PriceDistribution.Count < MAPeriods)
				return; // Require at least one tick per candle

			// Find the probability of the price being at it's current level
			var prob = 2.0 * PriceDistribution.Probability(new double[] { price })[0] - 1.0; // [-1,+1]
			var dist = 1.0 - Maths.Abs(prob);

			// On a change of more than 20%, buy/sell
			var change = Math.Abs(LastProb - dist);
			if (change >= 0.2)
			{
				Dump();

				// If the probability of the current price level is unlikely, open a position
				if (dist < 0.1)
				{
					// Buy the lows, sell the highs
					var sign = -Math.Sign(prob);
					var tt = CAlgo.SignToTradeType(sign);
					var vol = volume;// + (TrendSign == sign ? Instrument.Symbol.VolumeMin : 0); // Bias trades in the trend direction
					var sl = (QuoteCurrency?)null;//price - sign * stddev * 10.0;
					var tp = (QuoteCurrency?)null;//price + sign * stddev * Math.Abs(dist + 1);
					var trade = new Trade(Instrument, tt, Label, price, sl, tp, vol);

					using (Broker.SuspendRiskCheck(ignore_risk: true))
						Broker.CreateOrder(trade);
				}

				// Look for positions to close
				var trend_sign = TrendSign;
				foreach (var pos in Positions.ToArray())
				{
					var min_profit = pos.Sign() == trend_sign ? MinProfit : 0;
					if (pos.NetProfit > min_profit || pos.NetProfit < -MaxLoss)
						Broker.ClosePosition(pos);
				}

				LastProb = dist;
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

		/// <summary>Track the average price, returns true if the average is ok to use</summary>
		private void UpdatePriceDistribution()
		{
			// Ensure we get a distribution over the same period of time
			// (rather than number of ticks)

			// Create a distribution from the high res data over the last view candles
			PriceDistribution.Reset();
			foreach (var x in Instrument.HighResRange(-MAPeriods, 1))
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
