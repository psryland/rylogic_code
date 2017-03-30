using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot_MAExtreme : Rylobot
	{
		#region Parameters
		[Parameter("Risk Factor", DefaultValue = 1.0, MaxValue = 1.0, MinValue = 0.01)]
		public override double Risk { get; set; }

		[Parameter("Strong Trend Threshold", DefaultValue = 0.8, MaxValue = 1.0, MinValue = 0.0, Step = 0.01)]
		public double StrongTrend { get; set; }

		[Parameter("Open Distance", DefaultValue = 1.0, MaxValue = 5.0, MinValue = 0.0, Step = 0.1)]
		public double OpenDistance { get; set; }

		[Parameter("Close Distance", DefaultValue = 0.0, MaxValue = +3.0, MinValue = -3.0, Step = 0.1)]
		public double CloseDistance { get; set; }

		[Parameter("Fast MA Periods", DefaultValue = 10, MaxValue = 50, MinValue = 1)]
		public int MAPeriods0 { get; set; }

		[Parameter("Slow MA Periods", DefaultValue = 41, MaxValue = 100, MinValue = 5)]
		public int MAPeriods1 { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();

			MA0 = Indicator.EMA("MA0", Instrument, MAPeriods0, 10);
			MA1 = Indicator.EMA("MA1", Instrument, MAPeriods1, 10);
			PriceDistribution = new Distribution(Instrument.PipSize);
		}
		protected override void OnStop()
		{
			// Log the whole instrument
			Debugging.Dump(Instrument, mas:new[] { MA0, MA1 });

			base.OnStop();
		}

		/// <summary>Moving averages for determining trend sign</summary>
		public Indicator MA0 { get; private set; }
		public Indicator MA1 { get; private set; }

		/// <summary>Recent distribution of prices</summary>
		public Distribution PriceDistribution { get; private set; }

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1), mas:new[] { MA0, MA1 });
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			// One position at a time
			if (Positions.Any())
				return;

			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice;

			// Get the trend characteristics
			var trend_sign = Math.Sign(MA0[0].CompareTo(MA1[0]));
			var trend = Instrument.MeasureTrendFromSlope(MA0.FirstDerivative());

			// If this is a strong trend, only enter when price is opposite the trend
			if (Math.Abs(trend) > StrongTrend)
			{

			}
			// Otherwise enter if price is a long way from the MA
			else
			{
				var dist_ask = (MA0[0] - price.Ask) / mcs;
				var dist_bid = (price.Bid - MA0[0]) / mcs;
				if (dist_ask > OpenDistance)
				{
					var exit = Instrument.ChooseTradeExit(TradeType.Buy, price.Ask, risk:Risk);
					var sl = exit.SL;
					var tp = MA0[0] + CloseDistance * mcs;
					var vol = exit.Volume;
					var trade = new Trade(Instrument, TradeType.Buy, Label, price.Ask, sl, tp, vol);
					Broker.CreateOrder(trade);
				}
				if (dist_bid > OpenDistance)
				{
					var exit = Instrument.ChooseTradeExit(TradeType.Sell, price.Bid, risk:Risk);
					var sl = exit.SL;
					var tp = MA0[0] - CloseDistance * mcs;
					var vol = exit.Volume;
					var trade = new Trade(Instrument, TradeType.Sell, Label, price.Bid, sl, tp, vol);
					Broker.CreateOrder(trade);
				}
			}
		}
		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);
			PositionManagers.Add(new PositionManagerCloseAtMA(this, position));
		}
	}

	public class PositionManagerCloseAtMA :PositionManager
	{
		public PositionManagerCloseAtMA(Rylobot_MAExtreme bot, Position position)
			:base(bot, position)
		{}

		public new Rylobot_MAExtreme Bot
		{
			get { return (Rylobot_MAExtreme)base.Bot; }
		}

		protected override void StepCore()
		{
			var mcs = Instrument.MCS;

			// Move the TP to the current MA0 value
			var sign = Position.Sign();
			var tp = Bot.MA0[0] + sign * Bot.CloseDistance * mcs;
			if (Position.TakeProfit == null || Math.Abs(Position.TakeProfit.Value - tp) > Instrument.PipSize)
				Broker.ModifyOrder(Instrument, Position, tp:tp);
		}
	}
}

#if false
		const int ProbHistoryCount = 10;
		const double OpenPositionProb = 0.85;
		const double ClosePositionProb = 0.95;

		/// <summary>Extrapolation of the MA</summary>
		private Extrapolation Future0
		{
			get { return MA0.Future; }
		}
		private Extrapolation Future1
		{
			get { return MA1.Future; }
		}

		/// <summary>A moving distribution of the price</summary>
		private Distribution PriceDistribution { get; set; }

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice;

			// Create a distribution from the high res data over the last view candles
			// Ensure we get a distribution over the same period of time (rather than number of ticks)
			PriceDistribution.Reset();
			foreach (var x in Instrument.HighRes.Range(-ProbHistoryCount, 1)) PriceDistribution.Add(x.Mid);
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

			// The sign of the trend
			var trend_sign = Math.Sign(MA0[0].CompareTo(MA1[0]));

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
#endif