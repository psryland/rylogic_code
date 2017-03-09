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

			if (Instrument.NewCandle)
			{
				Debugging.LogInstrument();
				Debugging.BreakOnCandleOfInterest();
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

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 0.0; }
		}
	}
}
