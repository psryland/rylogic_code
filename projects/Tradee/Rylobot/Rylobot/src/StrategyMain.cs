using System;
using System.Diagnostics;
using cAlgo.API;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyMain :Strategy
	{
		public StrategyMain(Rylobot bot)
			:base(bot, "StrategyMain")
		{
			NNet = new PredictorNeuralNet(bot);
		}
		public override void Dispose()
		{
			NNet = null;
			base.Dispose();
		}

		/// <summary>A predictor used for entry point and direction signalling</summary>
		public PredictorNeuralNet NNet
		{
			[DebuggerStepThrough] get { return m_pred_nnet; }
			private set
			{
				if (m_pred_nnet == value) return;
				if (m_pred_nnet != null)
				{
					m_pred_nnet.ForecastChange -= HandleEntryTrigger;
					Util.Dispose(ref m_pred_nnet);
				}
				m_pred_nnet = value;
				if (m_pred_nnet != null)
				{
					m_pred_nnet.ForecastChange += HandleEntryTrigger;
				}
			}
		}
		private PredictorNeuralNet m_pred_nnet;

		/// <summary>Called when the predictor </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void HandleEntryTrigger(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// One trade at a time
			if (Position != null)
				return;

			// Wait for predictor to signal an entry point and direction

			// Create a trade

			// Output all of the feature values used to make the signal

			// Review each trade, figure out how to adjust the feature values



			// Only on new candles
			//if (!Instrument.NewCandle)
			//	return;

			// If the candle pattern indicator a trade entry...
			if (m_pred_candle_patterns.Forecast != null)
			{
				Debugging.Trading.Begin();
				Debugging.Trading.Comment(m_pred_candle_patterns.Comments);

				// Get the direction from the candle patterns
				TradeType tt;

				// If the EMA predictor shows a strong trend, use that instead of the candle pattern
		//		var trend_strength = m_pred_ema.TrendStrength;
		//		if (Math.Abs(trend_strength) > 0.5)
		//		{
		//			tt = Maths.Sign(trend_strength) > 0 ? TradeType.Buy : TradeType.Sell;
		//		}
		//		else
				{
					tt = m_pred_candle_patterns.Forecast.Value;
				}

				// Place an order
				var trade = new Trade(Bot, Instrument, tt, Label);
				Debugging.Trading.Trade(trade);
				Position = Bot.Broker.CreateOrder(trade);
			}
		}

		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);

			Debugging.Trading.Position(position, Bot);
			Debugging.Trading.Instrument(Instrument, position, diagnostic:false);
			Debugging.Trading.End();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.0; // not implemented 
		}
	}
}
