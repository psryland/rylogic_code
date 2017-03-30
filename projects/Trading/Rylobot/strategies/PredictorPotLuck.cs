using System;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	public class PredictorPotLuck :Predictor
	{
		private Random m_rng;

		public PredictorPotLuck(Rylobot bot)
			:base(bot, "PredictorPotLuck")
		{
			m_rng = new Random(1);
		}

		/// <summary>Look for predictions with each new data element</summary>
		protected override void UpdateFeatureValues(DataEventArgs args)
		{
			if (m_cooldown == 0)
			{
				Features.Add(new Feature("PotLuck", m_rng.Double(-1.0, +1.0)));
				m_cooldown = 100;
			}
			else
			{
				--m_cooldown;
			}
		}
		private int m_cooldown;
	}
}
