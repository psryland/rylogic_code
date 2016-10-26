using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rylobot
{
	/// <summary>Selects a strategy given the current market conditions</summary>
	public class StrategySelector
	{
		// Notes:
		// This class should choose the strategy to use given basic heuristics of
		// the market, e.g. ranging, falling, rising.
		public StrategySelector(Rylobot bot)
		{
			Bot = bot;
		}

		/// <summary></summary>
		public Rylobot Bot { get; private set; }

		/// <summary>
		/// Called on each data tick.
		/// Checks whether 'strat' is still an appropriate strategy for the current conditions.</summary>
		public Strategy Step(Strategy strat)
		{
			// Ask each strategy to rate itself over the recent data.
			// Choose the strategy with the best score. Use hysteresis to prevent bouncing.
			if (strat == null)
				return new StrategyPotLuck(Bot);

			// Nope, current one is still fine
			return strat;
		}
	}
}
