using System;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyMain :Strategy
	{
		public StrategyMain(Rylobot bot)
			:base(bot, "StrategyMain")
		{
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// Has a candle just closed?
			// No => return;

			// Is the candle an indecision candle
			// No => return;

			// Get the support and resistance levels for the instrument
			
			// When the price forms an indecision candle on a SnR level, consider a trade on
			// the 
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double Score()
		{
			return 0.0; // not implemented
		}
	}
}
