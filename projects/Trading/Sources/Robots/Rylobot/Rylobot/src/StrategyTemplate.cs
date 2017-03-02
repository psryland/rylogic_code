using System;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyTemplate :Strategy
	{
		// Notes:
		//  - Describe what the strategy does

		public StrategyTemplate(Rylobot bot)
			:base(bot, "StrategyTemplate")
		{}
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
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 0.0; }
		}
	}
}
