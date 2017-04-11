using System;
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
	public class Rylobot_CandlePatterns :Rylobot
	{
		#region Parameters
		/// <summary>The risk level to use for this bot</summary>
		[Parameter("Account Risk", DefaultValue = 1.0, MaxValue = 1.0, MinValue = 0.01)]
		public override double Risk { get; set; }

		/// <summary>The maximum number of open position sets</summary>
		[Parameter("Max Position Sets", DefaultValue = 1, MinValue = 1)]
		public override int MaxPositionSets { get; set; }

		/// <summary>Fast MA</summary>
		[Parameter("Fast MA Periods", DefaultValue = 10, MaxValue = 50, MinValue = 1)]
		public int MAPeriods0 { get; set; }

		/// <summary>Slow MA</summary>
		[Parameter("Slow MA Periods", DefaultValue = 41, MaxValue = 100, MinValue = 5)]
		public int MAPeriods1 { get; set; }

		/// <summary>The stop loss multiple</summary>
		[Parameter("Stop Loss Fraction", DefaultValue = 2.0)]
		public double SLFrac { get; set; }

		/// <summary>Close positions after a break of this many periods</summary>
		[Parameter("Close Breakout Periods", DefaultValue = 8)]
		public int CloseBreakoutPeriods { get; set; }

		/// <summary>Close positions if a new profit peak is not set within this many periods</summary>
		[Parameter("Close TopDrop Count", DefaultValue = 8)]
		public int CloseTopDropCount { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			MA0 = Indicator.EMA("MA0", Instrument, MAPeriods0, 10);
			MA1 = Indicator.EMA("MA1", Instrument, MAPeriods1, 10);
		}

		/// <summary>Moving averages for determining trend sign</summary>
		public Indicator MA0 { get; private set; }
		public Indicator MA1 { get; private set; }

		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1), indicators:new[] { MA0, MA1 });
			Debugging.Dump(new SnR(Instrument));
		}
		protected override void Step()
		{
			if (!Instrument.NewCandle)
				return;

			// Look for trade entry
			var sets_count = PositionSets.Count + PendingOrders.Count();
			if (sets_count < MaxPositionSets && EntryCooldown == 0)
			{
				// Wait for a Doji candle
				var mcs = Instrument.MCS;
				var A = Instrument[-1];
				var a_type = A.Type(mcs);
				if (!a_type.IsIndecision())
					return;

				// Look for strong trade direction indications
				var trade_sign = (int?)null;
				for (;;)
				{
					// Divergent extrapolation
					var q0 = (Quadratic)MA0.Future.Curve;
					var q1 = (Quadratic)MA1.Future.Curve;
					var intersect = Maths.Intersection(q0,q1);
					if (Math.Sign(q0.A) != Math.Sign(q1.A) || intersect.Length == 0)// || intersect.FirstOrDefault() >= 0)
						break;

					trade_sign = Math.Sign(q0.A);
					break;
				}

				// Look for Marubozu followed by doji
				for (;;)
				{
					// Find the preceding non-doji
					var i = -2;
					var B = Instrument[i];
					var b_type = B.Type(mcs);
					for (; i > Instrument.IdxFirst && b_type.IsIndecision(); --i, B = Instrument[i], b_type = B.Type(mcs)) { }
					if (!b_type.IsTrend())
						break;

					{
						var sign = trade_sign ?? -B.Sign;
						var ep = A.Close;
						var tt = CAlgo.SignToTradeType(sign);
						var sl = ep - sign * mcs * SLFrac;
						var tp = ep + sign * mcs * SLFrac * 4;
						//var tp = (QuoteCurrency?)null;
						var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
						var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol) { Expiration = Instrument.ExpirationTime(1) };
						Broker.CreateOrder(trade);
						Debugging.Trace("  -Doji followed by Marubozu");
						return;
					}
				}

				// Look for a strong candle trend followed by a hammer or inverted hammer
				for (;;)
				{
					// Get the candle trend leading up to 'A'
					var candle_trend = Instrument.MeasureTrendFromCandles(-3, -1);
					if (Math.Abs(candle_trend) < 0.8)
						break;

					// Look for a hammer pattern
					if (a_type == Candle.EType.Hammer && Math.Sign(candle_trend) > 0)
						break;
					if (a_type == Candle.EType.InvHammer && Math.Sign(candle_trend) < 0)
						break;

					{
						var sign = trade_sign ?? -Math.Sign(candle_trend);
						var ep = A.Close;
						var tt = CAlgo.SignToTradeType(sign);
						var sl = ep - sign * mcs * SLFrac;
						var tp = ep + sign * mcs * SLFrac * 4;
						//var tp = (QuoteCurrency?)null;
						var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
						var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol) { Expiration = Instrument.ExpirationTime(1) };
						Broker.CreateOrder(trade);
						Debugging.Trace("  -{0} pattern".Fmt(a_type));
						return;
					}
				}

				//var trend_sign = Math.Sign(MA0[0].CompareTo(MA1[0]));
				//var price_range = Instrument.PriceRange(i, 1).Inflate(1.05);
			}
		}

		/// <summary>Position opened</summary>
		protected override void OnPositionOpened(Position position)
		{
			// Close positions if there is a breakout in the wrong direction
			PositionManagers.Add(new PositionManagerBreakOut(this, position, CloseBreakoutPeriods, only_if_in_profit: true));

			// Close positions when they fail to make new peaks
			PositionManagers.Add(new PositionManagerTopDrop(this, position, CloseTopDropCount, only_if_in_profit: true));

			EntryCooldown = 5;
		}
	}
}
