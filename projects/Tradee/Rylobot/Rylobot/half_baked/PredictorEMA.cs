using cAlgo.API;
using cAlgo.API.Indicators;
using pr.maths;

namespace Rylobot
{
	/// <summary>Uses EMA data to predict price direction</summary>
	public class PredictorEMA :Predictor
	{
		public PredictorEMA(Rylobot bot)
			:base(bot, "PredictorEMA")
		{
			m_ema_lar = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, 144);
			m_ema_med = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close,  55);
			m_ema_sma = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close,  21);
		}

		/// <summary>The large time period EMA</summary>
		public ExponentialMovingAverage EmaLarge { get { return m_ema_lar; } }
		private ExponentialMovingAverage m_ema_lar;

		/// <summary>The medium time period EMA</summary>
		public ExponentialMovingAverage EmaMedium { get { return m_ema_med; } }
		private ExponentialMovingAverage m_ema_med;

		/// <summary>The small time period EMA</summary>
		public ExponentialMovingAverage EmaSmall { get { return m_ema_sma; } }
		private ExponentialMovingAverage m_ema_sma;

		/// <summary>Look for predictions with each new data element</summary>
		protected override void UpdateFeatureValues(DataEventArgs args)
		{
			// Don't bother if there isn't enough data
			if (m_ema_lar.Result.Count < 5 ||
				m_ema_med.Result.Count < 5 ||
				m_ema_sma.Result.Count < 5)
				return;

			// Sample the first and second derivatives of the EMA curves
			var d1_lar = m_ema_lar.Result.FirstDerivative (m_ema_lar.Result.Count - 2);
			var d2_lar = m_ema_lar.Result.SecondDerivative(m_ema_lar.Result.Count - 2);
			
			var d1_med = m_ema_med.Result.FirstDerivative (m_ema_med.Result.Count - 2);
			var d2_med = m_ema_med.Result.SecondDerivative(m_ema_med.Result.Count - 2);

			var d1_sma = m_ema_sma.Result.FirstDerivative (m_ema_sma.Result.Count - 2);
			var d2_sma = m_ema_sma.Result.SecondDerivative(m_ema_sma.Result.Count - 2);

			var latest = Instrument.Latest;

			// Trend up
			if (latest.Low > m_ema_lar.Result.LastValue && // Price above emaLarge
				d1_sma > d1_med && d1_med > d1_lar && d1_lar > 0 && // All gradients rising with small > medium > large
				d2_sma > d2_med && d2_med > d2_lar && d2_lar > 0)   // All curving upward with small > medium > large
			{
				//Forecast = TradeType.Buy;
				return;
			}

			// Trend down
			if (latest.High < m_ema_lar.Result.LastValue && // Price below emaLarge
				d1_sma < d1_med && d1_med < d1_lar && d1_lar < 0 && // All gradients falling with small > medium > large
				d2_sma < d2_med && d2_med < d2_lar && d2_lar < 0)   // All curving downward with small > medium > large
			{
				//Forecast = TradeType.Sell;
				return;
			}

		//	Forecast = null;
		}

		/// <summary>Return an estimate of the current trend direction and strength (normalised [-1,+1])</summary>
		public double TrendStrength
		{
			get
			{
				var i = m_ema_lar.Result.Count - 2;
				if (i < 0) return 0.0;

				var d1_lar = Maths.Sign(m_ema_lar.Result.FirstDerivative(i));
				var d1_med = Maths.Sign(m_ema_med.Result.FirstDerivative(i));
				var d1_sma = Maths.Sign(m_ema_sma.Result.FirstDerivative(i));

				return 0.5*d1_lar + 0.4*d1_med + 0.1*d1_sma;
			}
		}
	}
}
