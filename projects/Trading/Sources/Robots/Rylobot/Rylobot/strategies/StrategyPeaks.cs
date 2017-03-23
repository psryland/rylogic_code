using System;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyPeaks :Strategy
	{
		// Notes:
		//  - Use the peak detector to identify trends
		//  - fit curves to the peaks
		//  - open trades when price reaches the curve
		//  - close when trades reach the other curve

		public StrategyPeaks(Rylobot bot, double risk)
			:base(bot, "StrategyPeaks", risk)
		{}
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

			// Look for an existing trade for this strategy.
			var position = Positions.FirstOrDefault();

			// Don't open positions while there are pending orders
			if (position == null && !PendingOrders.Any())
			{
				// Look for peak patterns
				TradeType tt;
				QuoteCurrency ep;
				var pattern = Instrument.IsPeakPattern(0, out tt, out ep);
				if (pattern != null)
				{
					Debugging.LogInstrument();
					Debugging.Dump(new SnR(Instrument));
					Debugging.Dump(new PricePeaks(Instrument, 0));
					Debugging.BreakOnPointOfInterest();

					var trade = (Trade)null;
					var order = (Trade)null;

					// Convert the patterns to trades
					switch (pattern.Value)
					{
					case EPeakPattern.BreakOutHigh:
					case EPeakPattern.BreakOutLow:
						{
							var exit = Instrument.ChooseTradeExit(tt, ep);
							trade = new Trade(Instrument, tt, Label, exit.EP, exit.SL, exit.TP, exit.Volume);
							break;
						}
					case EPeakPattern.HighReversal:
					case EPeakPattern.LowReversal:
						{	
							var exit = Instrument.ChooseTradeExit(tt, ep);
							order = new Trade(Instrument, tt, Label, exit.EP, exit.SL, exit.TP, exit.Volume);
							order.Expiration = Instrument.ExpirationTime(1);
							break;
						}
					}

					if (order != null)
						Broker.CreatePendingOrder(order);
					if (trade != null)
						Broker.CreateOrder(trade);
				}
			}

			// For break points
			if (Instrument.NewCandle && (Positions.Any() || PendingOrders.Any()))
			{
				Debugging.LogInstrument();
				Debugging.BreakOnPointOfInterest();
			}
		}

		protected override void OnPositionOpened(Position position)
		{
			base.OnPositionOpened(position);
			PosMgr = new PositionManagerNervious(this, position);
		}
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			if (PosMgr != null && PosMgr.Position == position)
				PosMgr = null;
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				// Call Step when we have active positions
				if (Positions.Any() || PendingOrders.Any())
					return 1.0;

				// Potential price peaks
				var pp = new PricePeaks(Instrument, 0);
				if (pp.PeakCount < 4 || pp.PeakGap < 2.0 * Instrument.MCS)
					return 0.0;

				// Break-outs are top priority
				if (pp.IsBreakOut)
					return 1.0;

				// The peaks indicate a trend
				return pp.Strength;
			}
		}
	}
}
