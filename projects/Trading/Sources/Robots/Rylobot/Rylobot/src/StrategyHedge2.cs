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
			PositionManagers = new List<PositionManager>();
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			Debugging.BreakOnCandleOfInterest();
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
				var sl = 4.0;
				var exp = 1;
				var step = Instrument.MaxCandleSize(-5,1);

				var sym = Instrument.LatestPrice;
				var trade0 = new Trade(Bot, Instrument, TradeType.Buy , label:Label, ep:sym.Ask + ep*step, sl:sym.Ask - sl*step, tp:sym.Ask + tp*step, risk:0.5f) { Expiration = (Bot.UtcNow + Instrument.TimeFrame.ToTimeSpan(exp)).DateTime };
				var trade1 = new Trade(Bot, Instrument, TradeType.Sell, label:Label, ep:sym.Bid - ep*step, sl:sym.Bid + sl*step, tp:sym.Bid - tp*step, risk:0.5f) { Expiration = (Bot.UtcNow + Instrument.TimeFrame.ToTimeSpan(exp)).DateTime };

				Bot.Broker.CreatePendingOrder(trade0);
				Bot.Broker.CreatePendingOrder(trade1);
			}
			else
			{
				foreach (var pm in PositionManagers)
					pm.Step();
			}
		}

		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);

			// Close pending orders when a position is opened
			foreach (var ord in PendingOrders)
				Bot.Broker.CancelPendingOrder(ord);

			// Create a position manager
			PositionManagers.Add(new PositionManager(Instrument, position));
		}

		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			PositionManagers.RemoveIf(x => x.Position == position);
		}

		/// <summary>Position managers</summary>
		private List<PositionManager> PositionManagers
		{
			get;
			set;
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}
	}
}
