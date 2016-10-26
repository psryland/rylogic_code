using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylobot
{
	public class StrategyEmaCross :Strategy
	{
		// Look for crosses of the EMA.
		// When a candle closes on compl
		public StrategyEmaCross(Rylobot bot)
			:base(bot, "StrategyEmaCross")
		{ }

		public override void Step()
		{
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double Score()
		{
			return 0.0; // not implemented
		}
	}
}
