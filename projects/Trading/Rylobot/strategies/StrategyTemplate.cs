using System;
using cAlgo.API;
using pr.common;
using pr.extn;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyTemplate :Strategy
	{
		// Notes:
		//  - Describe what the strategy does

		public StrategyTemplate(Rylobot bot, double risk)
			:base(bot, "StrategyTemplate", risk)
		{}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 0.0; }
		}

		/// <summary>Debugging, output current state</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100,1));
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
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
}
