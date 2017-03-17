using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyHedge2 :Strategy
	{
		// Notes:
		//  - Open hedged positions 1*MCS apart
		//  - Set SL to 1*MCS
		//  - Set TP to 2*MCS

		public StrategyHedge2(Rylobot bot)
			:base(bot, "StrategyHedge2")
		{
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			Debugging.BreakOnPointOfInterest();
			if (Instrument.NewCandle)
			{
				Debugging.LogInstrument();
			}

			if (PendingOrders.Any())
				return;

			// Look for an existing trade for this strategy.
			var position = Positions.FirstOrDefault();
			if (position == null)
			{
				// 'ep' must be lower than 'tp'
				var ep = 0.2;
				var tp = 0.6;
				var sl = 0.8;
				var exp = 1;
				var step = Instrument.MCS;//MedianCandleSize(-5,1);

				var sym = Instrument.LatestPrice;
				var trade0 = new Trade(Instrument, TradeType.Buy , Label, ep:sym.Ask + ep*step, sl:sym.Ask - sl*step, tp:sym.Ask + tp*step, risk:0.5f) { Expiration = (Bot.UtcNow + Instrument.TimeFrame.ToTimeSpan(exp)).DateTime };
				var trade1 = new Trade(Instrument, TradeType.Sell, Label, ep:sym.Bid - ep*step, sl:sym.Bid + sl*step, tp:sym.Bid - tp*step, risk:0.5f) { Expiration = (Bot.UtcNow + Instrument.TimeFrame.ToTimeSpan(exp)).DateTime };

				Broker.CreatePendingOrder(trade0);
				Broker.CreatePendingOrder(trade1);
			}
			else
			{
			}
		}

		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);

			// Close pending orders when a position is opened
			foreach (var ord in PendingOrders)
				Broker.CancelPendingOrder(ord);

			// Create a position manager
			PositionManagers.Add(new PositionManager(Instrument, position));
		}

		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}
	}
}
