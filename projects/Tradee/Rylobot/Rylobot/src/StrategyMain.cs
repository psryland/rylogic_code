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
		public StrategyMain(Rylobot bot)
			:base(bot, "StrategyMain")
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

			// Run the position manager
			if (PosMgr != null)
				PosMgr.Step();

			// If there is a pending order, wait for it to trigger or expire
			var pending = PendingOrders.FirstOrDefault();
			if (pending != null)
				return;

			// Look for an existing trade for this strategy.
			var position = Positions.FirstOrDefault();
			if (position == null)
			{
				// Look for indicators to say "enter" and which direction.
				var tt = Instrument.FindTradeEntry();
				if (tt == null)
					return;

				// Create a trade in the suggested direction
				var trade = new Trade(Bot, Instrument, tt.Value, Label, rtr_range:new RangeF(1.0,5.0));
				//var pos = 
					Bot.Broker.CreateOrder(trade);
				//if (pos != null)
				//	PosMgr = new PositionManager(Instrument, pos);
			}
			else
			{
				var tt = SignalMACD();
				if (tt == null)
					return;

				if (tt.Value != position.TradeType)
					Bot.Broker.ClosePosition(position);
			}
		}

		// Trade management
		// - Set SL past recent peaks / 00 levels
		// - Close trade if signals change


		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			if (position == PosMgr.Position)
				PosMgr = null;
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}
	}
}
