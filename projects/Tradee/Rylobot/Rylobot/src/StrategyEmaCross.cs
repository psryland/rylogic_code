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
		public StrategyEmaCross(RylobotModel model)
			:base(model)
		{ }

		public override void Step()
		{
		}
	}
}
