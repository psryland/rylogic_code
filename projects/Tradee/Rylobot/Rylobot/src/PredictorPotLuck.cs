using System;
using cAlgo.API;

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
		protected override void Step()
		{
			if (m_rng.NextDouble() < 0.001 && Predictions.Count < 10)
				Forecast = m_rng.NextDouble() > 0.5 ? TradeType.Buy : TradeType.Sell;
			else
				Forecast = null;
		}
	}
}
