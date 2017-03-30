using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyMain :Strategy
	{
		// Notes:
		//  - Simple traditional strategy.
		//  - Use technical analysis to find good entry points
		//  - Require a reward to risk > 1.0
		//  - Aim for > 50% success
		//  - No hedging etc...
		//  
		public StrategyMain(Rylobot bot, double risk)
			:base(bot, "StrategyMain", risk)
		{
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>A helper for monitoring active positions</summary>
		private PositionManager PosMgr
		{
			get;
			set;
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			if (Instrument.NewCandle)
			{
				Debugging.LogInstrument();
				Debugging.BreakOnPointOfInterest();
			}

			// Run the position manager
			if (PosMgr != null)
				PosMgr.Step();

			// If there is a pending order, wait for it to trigger or expire
			var pending = PendingOrders.FirstOrDefault();
			if (pending != null)
			{
				// If the price is more than the SL/TP distance from the EP then give up on the order
				var dist = Math.Abs(Instrument.LatestPrice.Mid - pending.TargetPrice);
				if (dist > Math.Abs(pending.TakeProfitRel()) ||
					dist > Math.Abs(pending.StopLossRel()))
					Broker.CancelPendingOrder(pending);
				else
					return;
			}

			// Look for an existing trade for this strategy.
			var position = Positions.FirstOrDefault();
			if (position == null)
			{
				// Look for indicators to say "enter" and which direction.
				QuoteCurrency? ep, tp;
				var tt = Instrument.FindTradeEntry(out ep, out tp);
				if (tt == null)
					return;

				// Create a trade in the suggested direction
				var trade = new Trade(Instrument, tt.Value, Label, ep:ep, tp:tp);
				trade.Expiration = (Bot.UtcNow + Instrument.TimeFrame.ToTimeSpan(10)).DateTime;
				if (trade.RtR < 0.2)
					return;

				// Create a pending order
				Broker.CreatePendingOrder(trade);
				//Broker.CreateOrder(trade);
			}
		}

		// Trade management
		// - Set SL past recent peaks / 00 levels
		// - Close trade if signals change

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
			PosMgr = new PositionManagerNervious(this, position);
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
			if (PosMgr != null && position == PosMgr.Position)
				PosMgr = null;
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}
	}
}
