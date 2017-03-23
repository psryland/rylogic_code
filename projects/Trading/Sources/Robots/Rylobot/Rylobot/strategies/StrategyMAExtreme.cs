using System;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyMAExtreme :Strategy
	{
		// Notes:
		//  - Describe what the strategy does

		const int MAPeriods0 = 5;
		const int MAPeriods1 = 41;
		const int ProbHistoryCount = 10;
		const double OpenPositionProb = 0.85;
		const double ClosePositionProb = 0.95;

		public StrategyMAExtreme(Rylobot bot, double risk)
			:base(bot, "StrategyMAExtreme", risk)
		{
			PriceDistribution = new Distribution(Instrument.PipSize);
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
		private Distribution PriceDistribution { get; set; }

		/// <summary>Moving averages for determining trend sign</summary>
		private Indicator MA0 { get; set; }
		private Indicator MA1 { get; set; }

		/// <summary>Extrapolation of the MA</summary>
		private Extrapolation Future0
		{
			get { return MA0.Future; }
		}
		private Extrapolation Future1
		{
			get { return MA1.Future; }
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1), mas:new[] { MA0, MA1 }, high_res:20.0);
			Debugging.Dump(PriceDistribution, +5, prob:new[] { 0.1, 0.5, 0.9 });
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice;

			// Create a distribution from the high res data over the last view candles
			// Ensure we get a distribution over the same period of time (rather than number of ticks)
			PriceDistribution.Reset();
			foreach (var x in Instrument.HighResRange(-ProbHistoryCount, 1)) PriceDistribution.Add(x.Mid);
			if (PriceDistribution.Count < 100)
				return;

			// Find the probability of the price being at it's current level. Scaled so that 0 = mean, -1,+1 are the extremes
			var probs = PriceDistribution.Probability(new double[] { price.Bid, price.Mid, price.Ask }).Select(p => 2.0*p - 1.0).ToArray();

			// Choose the ask/bid price depending on the sign of the mid price probability. i.e.
			// For opening positions, use Bid when the price is high, Ask when the price is low.
			// For closing positions, use Ask when the price is high, Bit when the price is low
			var prob_sign  = Math.Sign(probs[1]);
			var open_prob  = prob_sign >= 0 ? probs[0] : probs[2];
			var close_prob = prob_sign >= 0 ? probs[2] : probs[0];

			// Look for positions to close
			#region Close Positions
			foreach (var pos in Positions.ToArray())
			{
				// If the SL is on the winning side, let the trade run its course
				var sl_rel = pos.StopLossRel();
				if (sl_rel < 0)
					continue;

				// If the trade is better than RtR=1:1, and showing signs of reversing, close the trade
				var min_profit = Instrument.Symbol.QuoteToAcct(sl_rel * pos.Volume);
				if (pos.NetProfit > min_profit)
				{
					// If the price gradient is in the direction of the trade, don't close it
					var d = Instrument.HighRes.FirstDerivative(-1);
					if (Math.Sign(d) == pos.Sign())
						continue;

					var f = Maths.Frac(MA1[0], price.Price(-pos.Sign()), MA0[0]);
					if (f < 0.5)
					{
				//		Broker.ClosePosition(pos);
					}
				}

			}
			#endregion

			// On a significantly extreme price level, buy/sell
			if (Math.Abs(open_prob) > OpenPositionProb)
			{
				Dump();

				// Buy the lows, Sell the highs
				var sign = -prob_sign;
				var ep = Instrument.CurrentPrice(sign);
				var risk = Risk / 1; // Divide the risk amongst the number of positions that can be opened
				Debugging.Trace("Entry Trigger - {0} - prob={1:N3}".Fmt(sign > 0 ? "Buy" : "Sell", open_prob));

				for (;;)
				{
					// Only allow one trade at a time using this method
					if (Positions.Any())
					{
						Debugging.Trace("  -Position with this sign already exists");
						break;
					}
					Debugging.Trace("  +No existing {0} position".Fmt(sign));

					// Require MA0 to be a significant distance from MA1
					var ma0_dist = MA0[0] - MA1[1];
					if (Math.Abs(ma0_dist) < mcs)
					{
						Debugging.Trace("  -MA are not separated enough ({0:N3} pips)".Fmt(ma0_dist / Instrument.PipSize));
						break;
					}
					Debugging.Trace("  +MAs separated by {0:N3} pips".Fmt(ma0_dist / Instrument.PipSize));

					// Require the price to be a significant distance further than MA0
					var price_dist = price.Price(sign) - MA1[0];
					if (Math.Sign(price_dist) != Math.Sign(ma0_dist) || Math.Abs(price_dist) < Math.Abs(ma0_dist) || Math.Abs(price_dist - ma0_dist) < 0.3*mcs)
					{
						Debugging.Trace("  -Price not extreme enough ({0:N3} pips)".Fmt(price_dist / Instrument.PipSize));
						break;
					}
					Debugging.Trace("  +Price is separated by {0:N3} pips".Fmt(price_dist / Instrument.PipSize));

					// Only open positions when the recent price has been heading in the direction we want to trade
					var price_trend = Instrument.HighRes.FirstDerivative(-1);
					if (Math.Sign(price_trend) == -sign)
					{
						Debugging.Trace("  -Price trend ({0:N3}) against trade sign ({1})".Fmt(price_trend, sign));
						break;
					}
					Debugging.Trace("  +Price trend ({0:N3} pips/tick) matches trade sign ({1})".Fmt(price_trend / Instrument.PipSize, sign));

					// Don't open trades in the same direction as the preceding trend
					//var candle_trend = Instrument.MeasureTrendFromCandles(-3,0);
					//var high_res_candle = Instrument.HighResCandle(-0.5, 1.0);
					//if (high_res_candle.Sign != -Math.Sign(candle_trend))
					//{
					//	Debugging.Trace("  -Latest candle is in the same direction as the preceding trend");
					//	break;
					//}
					//Debugging.Trace("  +Latest candle is opposite to preceding trend");

					// Wait for a sign that price is reversing
					// The wick in the -sign direction is > a percentage of the total length

					// Set the SL at double the extreme distance
					var sl_rel = 1.0 * Math.Abs(price_dist - ma0_dist);
					var tp_rel = 1.0 * sl_rel;

					// Create the positions
					var tt = CAlgo.SignToTradeType(sign);
					var sl = ep - sign * sl_rel;
					var tp = ep + sign * tp_rel;
					var vol = Broker.ChooseVolume(Instrument, sl_rel, risk:risk);
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol);
					Broker.CreateOrder(trade);
					break;
				}
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

		/// <summary>Watch for bot stopped</summary>
		protected override void OnBotStopping()
		{
			base.OnBotStopping();

			// Log the whole instrument
			Debugging.DebuggingEnabled = true;
			Debugging.Dump(Instrument, mas:new[] { MA0, MA1 });
			Debugging.Dump(Debugging.AllTrades.Values);
			Debugging.DebuggingEnabled = false;
		}
	}
}
