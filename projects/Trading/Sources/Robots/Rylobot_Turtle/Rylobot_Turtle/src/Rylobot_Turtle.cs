using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
using cAlgo.API.Internals;
using cAlgo.Indicators;
using pr.common;
using pr.extn;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot_Turtle : Rylobot
	{
		#region Parameters
		[Parameter("Breakout Periods Entry", DefaultValue = 15)]
		public int System1_Entry{ get; set; }

		[Parameter("Breakout Periods Exit", DefaultValue = 15)]
		public int System1_Exit{ get; set; }

		[Parameter("Max Positions", DefaultValue = 4)]
		public int MaxPositions { get; set; }

		[Parameter("Add Position Frac", DefaultValue = 0.5)]
		public double AddPositionFrac { get; set; }

		[Parameter("Stop Loss Frac", DefaultValue = 2.0)]
		public double StopLossFrac { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			BreakoutsHistory = new List<Trade>();
		}

		private List<Trade> BreakoutsHistory { get; set; }
		private Trade ActiveBreakout;

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1));
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
			// MCS = 20-period EMA of the true range.
			// 1-Unit = Volume for a 1xMCS move equalling 1% equity
			// N-period break-out = the price exceeding the high or low of the last N periods.
			// N-period exit = the price making a new N-period low (for long positions) or high (for short positions)
			// System 1:
			//   Enter on 20-period break-out if the prior break-out was not a winner.
			//   Exit on 10-period break-out against the position.
			//   Open 1 unit at entry, and add 1 unit every 0.5*MCS above entry, move all stops to 2*MCS below
			var price = Instrument.LatestPrice;
			var mcs = Instrument.MCS;

			var positions = Positions.ToArray();
			if (positions.Length == 0)
			{
				// System 1 - look for a short period breakout
				var tt = Instrument.IsBreakOut(System1_Entry);
				if (tt != null)
				{
					var sign = tt.Value.Sign();
					var ep = price.Price(sign);
					var sl = ep - sign * mcs * StopLossFrac;
					var vol = Broker.ChooseVolume(Instrument, 1*mcs, risk:1.0/MaxPositions);
					var trade = new Trade(Instrument, tt.Value, Label, ep, sl, null, vol);

					// Enter if the last breakout was not a winner
				//	var last = BreakoutsHistory.LastOrDefault();
				//	if (last == null || last.NetProfit < 0)
					{
						Dump();

						Broker.CreateOrder(trade);
						ActiveBreakout = trade;
					}
				//	else if (ActiveBreakout == null)
				//	{
				//		Dump();
				//
				//		// Save each breakout, so we can tell if they are winners
				//		ActiveBreakout = trade;
				//	}
				}
			}

			// Look to increase the position size
			else if (positions.Length > 0 && positions.Length < MaxPositions)
			{
				var sign = positions[0].Sign();

				// Get the initial entry price
				var initial_ep = sign > 0 ? positions.Min(x => x.EntryPrice) : positions.Max(x => x.EntryPrice);

				// Get the level the price needs to have crossed
				var ep = initial_ep + sign * positions.Length * mcs * AddPositionFrac;
				 
				// Add to the position every AddPositionFrac*N gain
				if (Math.Sign(Instrument.LatestPrice.Price(sign) - ep) == sign)
				{
					Dump();

					var sl = ep - sign * mcs * StopLossFrac;
					var vol = Broker.ChooseVolume(Instrument, 1*mcs, risk:1.0/MaxPositions);

					// Adjust the SL on existing trades
					foreach (var pos in positions)
						Broker.ModifyOrder(Instrument, pos, sl:pos.StopLoss + sign * mcs * AddPositionFrac);

					// Add to the position
					var trade = new Trade(Instrument, CAlgo.SignToTradeType(sign), Label, ep, sl, null, vol);
					Broker.CreateOrder(trade);
				}
			}

			// Simulate each active 'trade'
			if (ActiveBreakout != null)
			{
				ActiveBreakout.Simulate(Instrument.LatestPrice);

				// Look for exits on the simulated trade
				var exit_tt = Instrument.IsBreakOut(System1_Exit);
				if (exit_tt != null && ActiveBreakout.TradeType != exit_tt.Value)
				{
					Dump();

					// Close any trades where 'exit_tt' is opposes the trade direction
					ActiveBreakout.Close();
					BreakoutsHistory.Add(ActiveBreakout);
					ActiveBreakout = null;
				}
			}
		}

		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);
			PositionManagers.Add(new PositionManagerBreakOut(this, position, System1_Exit, only_if_in_profit:false));
		}
	}
}
