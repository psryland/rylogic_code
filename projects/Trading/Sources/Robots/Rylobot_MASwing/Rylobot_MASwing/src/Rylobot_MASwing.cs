using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot_MASwing :Rylobot
	{
		#region Parameters
		/// <summary>Overall risk allowed by this bot</summary>
		[Parameter("Risk", DefaultValue = 0.1)]
		public override double Risk { get; set; }

		/// <summary>The maximum number of open position sets</summary>
		[Parameter("Max Position Sets", DefaultValue = 1)]
		public override int MaxPositionSets { get; set; }

		/// <summary>The number of periods for MA0</summary>
		[Parameter("MA Periods", DefaultValue = 19)]
		public int MAPeriods { get; set; }

		/// <summary>The number of candles above or below the MA that signals a bulge</summary>
		[Parameter("Non-Intersecting Count", DefaultValue = 3)]
		public int NonIntersectingCount { get; set; }

		/// <summary>The distance a candle must be from the MA to be considered no intersecting</summary>
		[Parameter("Bulge Distance Tolerance", DefaultValue = 0.3)]
		public double BulgeDistanceTolerance { get; set; }

		/// <summary>The stop loss multiple</summary>
		[Parameter("Stop Loss Fraction", DefaultValue = 1.0)]
		public double SLFrac { get; set; }

		/// <summary>The price movement needed to increase the position size</summary>
		[Parameter("Position Increase Step", DefaultValue = 0.5)]
		public double PositionIncreaseStep { get; set; }

		/// <summary>Close positions after a break of this many periods</summary>
		[Parameter("Close Breakout Periods", DefaultValue = 8)]
		public int CloseBreakoutPeriods { get; set; }

		/// <summary>Close positions if a new profit peak is not set within this many periods</summary>
		[Parameter("Close TopDrop Count", DefaultValue = 8)]
		public int CloseTopDropCount { get; set; }

		/// <summary>Close positions if a sequence of this many adverse candles are detected</summary>
		[Parameter("Close Adverse Count", DefaultValue = 4)]
		public int CloseAdverseCount { get; set; }

		/// <summary>The maximum number of positions per set</summary>
		[Parameter("Max Position Per Set", DefaultValue = 4)]
		public int MaxPositionsPerSet { get; set; }

		/// <summary>The slope threshold for using the slow MA direction as the trade direction</summary>
		[Parameter("Slow MA Trend Slope", DefaultValue = 1)]
		public double MATrendSlope { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			MA0 = Indicator.EMA("MA0", Instrument, MAPeriods, MAPeriods);
			MA1 = Indicator.EMA("MA1", Instrument, 10*MAPeriods, MAPeriods);
		}
		protected override void OnStop()
		{
			MeasureBulgeSequenceProbability();
			base.OnStop();
		}

		/// <summary>The fast MA</summary>
		public Indicator MA0 { get; private set; }

		/// <summary>The slow MA</summary>
		public Indicator MA1 { get; private set; }

		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range: new Range(-100, 1), indicators: new[] { MA0, MA1 });
			Debugging.Dump(new PricePeaks(Instrument, 1));
		}
		protected override void Step()
		{
			// Look for trade entry
			var sets_count = PositionSets.Count + PendingOrders.Count();
			if (sets_count < MaxPositionSets && EntryCooldown == 0)
			{
				// Wait for a sequence of candles entirely above or below the MA
				var price = Instrument.LatestPrice;
				var mcs = Instrument.MCS;

				// Look for a sequence of candles that are entirely above or below the MA
				var bulge = FindBulges(0, MA0).FirstOrDefault();
				if (bulge.Sign != 0 && Instrument.IdxLast - bulge.Range.End <= NonIntersectingCount)
				{
					Debugging.AreaOfInterest(bulge.Range, append: false);

					// Decide the direction
					int sign = 0;

					//// Trade in the direction of the slow MA if it is trending strongly
					//var ma_slope = MA1.FirstDerivative(0) / Instrument.PipSize;
					//if (Math.Abs(ma_slope) > MATrendSlope)
					//{
					//	sign = Math.Sign(ma_slope);
					//}
					//else
					{
						// Using measured stats of bulge sequences, the probabilities are:
						// 0 = below the MA, 1 = above the MA
						var next_bulge_sign = new []
						{
							-1, //  000: -0.168091168091168 (count=351)
							+1, //  001:  0.224489795918367 (count=245)
							-1, //  010: -0.069767441860465 (count=172)
							+1, //  011:  0.158730158730159 (count=252)
							-1, //  100: -0.195121951219512 (count=246)
							+1, //  101:  0.139664804469274 (count=179)
							-1, //  110: -0.217391304347826 (count=253)
							+1, //  111:  0.064102564102564 (count=312)
						};

						// Include the current bulge because the trade triggers when this bulge closes
						var bulge_signs = new List<int>();
						FindBulges(0, MA0).Take(3).ForEach(b => bulge_signs.Insert(0, b.Sign)); // Careful with order
						sign = next_bulge_sign[Bit.SignsToIndex(bulge_signs)];
					}

					{// Create a pending order
						var ep = MA0[0];
						var tt = CAlgo.SignToTradeType(sign);
						var sl = ep - sign * mcs * SLFrac; // Note: the SL needs to be big enough that a paired order is triggered before this trade is closed
						var tp = (QuoteCurrency?)null;
						var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
						var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol, comment:Guid.NewGuid().ToString()) { Expiration = Instrument.ExpirationTime(1) };
						Broker.CreatePendingOrder(trade);
					}

					EntryCooldown = 1;
				}
			}

			// Increase position size on winning positions
			//IncreasePosition();

			// Break point helper
			if (Instrument.NewCandle)
				Dump();
		}
		protected override void OnPositionOpened(Position position)
		{
			// Prevent positions being entered on the same bulge
			EntryCooldown = NonIntersectingCount;

			{// Adjust the stop loss to allow for slip
				var mcs = Instrument.MCS;
				var sign = position.Sign();
				var ep = position.EntryPrice;
				var sl = ep - sign * mcs * SLFrac;
				Broker.ModifyOrder(Instrument, position, sl:sl);
			}

			// If the main position of a position set opens, create a pending order in the opposite direction.
			// This is designed to catch the case when we choose a break-out trade but it's actually a reversal, or visa-versa.
			var id = Guid_.Parse(position.Comment);
			if (id != null && PositionSets[id.Value].Count == 1)
			{
				var mcs = Instrument.MCS;
				var sign = -position.Sign();
				var tt = CAlgo.SignToTradeType(sign);
				var rel = Math.Abs(position.EntryPrice - position.StopLoss.Value) * 0.75;
				var ep = MA0[0] + sign * rel;
				var sl = ep - sign * mcs * SLFrac;
				var tp = (QuoteCurrency?)null;
				var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
				var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol, comment:Guid.NewGuid().ToString()) { Expiration = Instrument.ExpirationTime(2) };
				Broker.CreatePendingOrder(trade);
			}

			// Close positions if there is a breakout in the wrong direction
			PositionManagers.Add(new PositionManagerBreakOut(this, position, CloseBreakoutPeriods, only_if_in_profit: true));

			// Close positions when they fail to make new peaks
			PositionManagers.Add(new PositionManagerTopDrop(this, position, CloseTopDropCount, only_if_in_profit: true));

			// Close positions when there's a steady stream of adverse candles
			PositionManagers.Add(new PositionManagerAdverseCandles(this, position, CloseAdverseCount));
		}
		protected override void OnPositionClosed(Position position)
		{
			CancelAllPendingOrders();
		}

		/// <summary>Look for a sequence of candles that are entirely above or below the MA</summary>
		private IEnumerable<Bulge> FindBulges(Idx idx, Indicator moving_average)
		{
			if (!idx.Within(Instrument.IdxFirst, Instrument.IdxLast))
				yield break;

			var mcs = Instrument.MCS;

			// Find spans of non-intersecting candles
			var bulge = new Bulge { Sign = 0, Range = RangeF.Invalid };
			for (var i = idx; i != Instrument.IdxFirst; --i)
			{
				var candle = Instrument[i];
				var ma = moving_average[i];

				var hi = candle.High - ma;
				var lo = candle.Low  - ma;
				if (Math.Abs(hi) < mcs * BulgeDistanceTolerance) hi = 0;
				if (Math.Abs(lo) < mcs * BulgeDistanceTolerance) lo = 0;
				var intersecting = hi == 0 || lo == 0 || Math.Sign(hi) != Math.Sign(lo);
				if (!intersecting && Math.Sign(hi) != -bulge.Sign)
				{
					// Record the price range spanned
					bulge.Range.Encompass(i);
					bulge.Sign = Math.Sign(hi);
				}
				else
				{
					if (bulge.Range.Size >= NonIntersectingCount)
						yield return bulge;

					// Reset for the next bulge
					bulge = new Bulge { Sign = 0, Range = RangeF.Invalid };
				}
			}
		}
		private struct Bulge
		{
			public int Sign;
			public RangeF Range;
		}

		/// <summary>Increase the position size of winning trades</summary>
		private void IncreasePosition()
		{
			var mcs = Instrument.MCS;
			var price = Instrument.LatestPrice;

			foreach (var set in PositionSets.Values)
			{
				// Base position increases off the initial position
				// The initial position may be closed just before the other positions (hence null is possible)
				var pos0 = Positions.FirstOrDefault(x => x.Id == set[0]);
				if (pos0 == null)
					continue;

				var sign = pos0.Sign();

				// Get the positions at are the increases
				var positions = Positions.Where(x => set.Contains(x.Id) && x.Sign() == sign).ToArray();
				if (positions.Length >= MaxPositionsPerSet)
					continue;

				// Get the initial entry price
				var initial_ep = pos0.EntryPrice;

				// Get the level the price needs to have crossed
				var ep = initial_ep + sign*positions.Length*mcs*PositionIncreaseStep;

				// Add to the position every if 'ep' is crossed
				if (Math.Sign(price.Price(sign) - ep) == sign)
				{
					Dump();

					// Adjust the SL on existing trades
					foreach (var pos in positions)
					{
						var adj_sl = pos.StopLoss + sign*mcs*PositionIncreaseStep;
						Broker.ModifyOrder(Instrument, pos, sl:adj_sl);
					}

					// Add to the position
					var sl = ep - sign * mcs * SLFrac;
					var vol = Broker.ChooseVolume(Instrument, mcs, risk:Risk/MaxPositionsPerSet);
					var trade = new Trade(Instrument, CAlgo.SignToTradeType(sign), Label, ep, sl, pos0.TakeProfit, vol, comment:pos0.Comment);
					Broker.CreateOrder(trade);
				}
			}
		}

		/// <summary>Calculate stats for bulges signs</summary>
		private void MeasureBulgeSequenceProbability()
		{
			// If the bulges are 'a,b,c' what the most likely next bulge
			const int HistoryLength = 2;
			var history = new List<int>();
			var avr = Util.NewArray<AvrVar>(1 << HistoryLength);
			foreach (var bulge in FindBulges(0, MA0)) // going backwards
			{
				// Bulges are in time order, left = oldest, right = newest
				history.Insert(0, bulge.Sign);
				if (history.Count > HistoryLength)
				{
					var idx = Bit.SignsToIndex(history, 0, HistoryLength);
					avr[idx].Add(history[HistoryLength]);
					history.RemoveAt(HistoryLength);
				}
			}

			// Output probabilities for each bulge sequence
			for (int i = 0; i != avr.Length; ++i)
				Debugging.Trace("{0}: {1} (s.d. {2})  (count={3})".Fmt(Bit.ToString((uint)i, 3), avr[i].Mean, avr[i].SamStdDev, avr[i].Count));
		}
	}
}
