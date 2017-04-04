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
		[Parameter("Breakout Periods Entry", DefaultValue = 20)]
		public int System1_Entry{ get; set; }

		[Parameter("Breakout Periods Exit", DefaultValue = 10)]
		public int System1_Exit{ get; set; }

		[Parameter("Max Positions", DefaultValue = 4)]
		public int MaxPositions { get; set; }
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
			BreakoutsHistory = new List<Trade>();
			ActiveBreakouts = new List<Trade>();
		}
		protected override void OnStop()
		{
			base.OnStop();
		}

		private List<Trade> BreakoutsHistory { get; set; }
		private List<Trade> ActiveBreakouts { get; set; }

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
			var N = Instrument.EMATrueRange(-20, 1);

			var positions = Positions.ToArray();
			if (positions.Length == 0)
			{
				// System 1 - look for a short period breakout
				var tt = Instrument.IsBreakOut(System1_Entry);
				if (tt != null)
				{
					Dump();

					var sign = tt.Value.Sign();
					var ep = price.Price(sign);
					var sl = ep - sign * 2*N;
					var vol = Broker.ChooseVolume(Instrument, 1*N, risk:0.1f);
					var trade = new Trade(Instrument, tt.Value, Label, ep, sl, null, vol);

					// Enter if the last breakout was not a winner
					var last = BreakoutsHistory.LastOrDefault();
					if (!Positions.Any() && (last == null || last.NetProfit < 0))
					{
						Broker.CreateOrder(trade);
					}

					// Save each breakout, so we can tell if they are winners
					ActiveBreakouts.Add(trade);
				}
			}

			// Look to increase the position size
			else if (positions.Length > 0 && positions.Length < MaxPositions)
			{
				var sign = positions[0].Sign();

				// Get the initial entry price
				var initial_ep = sign > 0 ? positions.Min(x => x.EntryPrice) : positions.Max(x => x.EntryPrice);

				// Get the level the price needs to have crossed
				var ep = initial_ep + sign * positions.Length * N / 2.0;
				 
				// Add to the position every 0.5*N gain
				if (Math.Sign(Instrument.LatestPrice.Price(sign) - ep) == sign)
				{
					Dump();

					var sl = ep - sign * 2*N;
					var vol = Broker.ChooseVolume(Instrument, 1*N, risk:0.1f);

					// Adjust the SL on existing trades
					foreach (var pos in positions)
						Broker.ModifyOrder(Instrument, pos, sl:pos.StopLoss + sign * N/2.0);

					// Add to the position
					var trade = new Trade(Instrument, CAlgo.SignToTradeType(sign), Label, ep, sl, null, vol);
					Broker.CreateOrder(trade);
				}
			}

			// Simulate each active 'trade'
			foreach (var trade in ActiveBreakouts)
				trade.Simulate(Instrument.LatestPrice);

			// Look for exits on simulated trades
			var exit_tt = Instrument.IsBreakOut(System1_Exit);
			if (exit_tt != null)
			{
				// Close any trades where 'exit_tt' is opposes the trade direction
				foreach (var trade in ActiveBreakouts.Where(x => x.TradeType != exit_tt.Value).ToArray())
				{
					Dump();

					trade.Close();
					BreakoutsHistory.Add(trade);
					ActiveBreakouts.Remove(trade);
				}
			}
		}

		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);
			PositionManagers.Add(new PositionManagerTurtle(this, position));
		}
	}

	/// <summary>Position manager for the Turtle trading strategy</summary>
	public class PositionManagerTurtle :PositionManager
	{
		public PositionManagerTurtle(Rylobot bot, Position position)
			:base(bot, position)
		{}

		public new Rylobot_Turtle Bot
		{
			get { return (Rylobot_Turtle)base.Bot; }
		}

		protected override void StepCore()
		{
			// Look for an exit signal
			var exit_tt = Instrument.IsBreakOut(Bot.System1_Exit);
			if (exit_tt != null && exit_tt.Value != Position.TradeType)
				Broker.ClosePosition(Position);
		}
	}
}
