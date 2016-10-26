using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyTail :Strategy
	{
		public StrategyTail(Rylobot bot)
			:base(bot, "StrategyTail")
		{
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// Start with the tail equal to the current price

			// as the price moves, move the tail to follow behind the price, keeping below SnR levels

			// When the price crosses the tail create a trade

		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double Score()
		{
			return 0.0; // not implemented
		}
	}
}
