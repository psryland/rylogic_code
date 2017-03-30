using System;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategySnR :Strategy
	{
		public StrategySnR(Rylobot bot, double risk)
			:base(bot, "StrategySnR", risk)
		{}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}

		/// <summary>Debugging, output current state</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100,1));
			Debugging.Dump(new SnR(Instrument, tf:TimeFrame.Hour8));
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (!Instrument.NewCandle)
				return;

			var position = Positions.FirstOrDefault();
			if (position == null)
			{
				var snr = new SnR(Instrument, tf:TimeFrame.Hour8);
				var mcs = Instrument.MCS;

				// Look for price being near an SnR level
				var price = Instrument.LatestPrice;
				var near = snr.Nearest(price.Mid, 0);
				if (near == null || Math.Abs(near.Price - price.Mid) > 0.5*mcs)
					return;

				// Get the preceding trend
				var trend = Instrument.MeasureTrendFromCandles(-5, 1);
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Watch for bot stopped</summary>
		protected override void OnBotStopping()
		{
			base.OnBotStopping();

			// Log the whole instrument
			Debugging.Dump(Instrument);
		}
	}

	/// <summary>Close positions at stalls on SnR levels</summary>
	public class PositionManagerStopAtSnR :PositionManager
	{
		public PositionManagerStopAtSnR(Strategy strat, Position position)
			:base(strat, position)
		{ }

		protected override void StepCore()
		{
			
		}
	}
}
