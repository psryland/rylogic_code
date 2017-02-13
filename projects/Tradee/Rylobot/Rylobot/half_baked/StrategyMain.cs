using System;
using System.Diagnostics;
using System.Linq;
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

		/// <summary>The position managed by this strategy</summary>
		public Position Position
		{
			get { return m_position; }
			protected set
			{
				if (m_position == value) return;
				m_position = value;
			}
		}
		private Position m_position;

		/// <summary>A predictor used for entry point and direction signalling</summary>
		public PredictorNeuralNet NNet
		{
			[DebuggerStepThrough] get { return m_pred_nnet; }
			private set
			{
				if (m_pred_nnet == value) return;
				if (m_pred_nnet != null)
				{
					Util.Dispose(ref m_pred_nnet);
				}
				m_pred_nnet = value;
				if (m_pred_nnet != null)
				{
					m_pred_nnet.TrackForecasts = true;
				}
			}
		}
		private PredictorNeuralNet m_pred_nnet;

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// One trade at a time
			Position = FindLivePosition(Position);
			if (Position != null)
				return;

			// Wait for the predictor to predict
			if (NNet.Forecast == null)
				return;

			// Create a trade
			var trade = new Trade(Bot, Instrument, NNet.Forecast.Value, Label, NNet.CurrentIndex);
			Position = Bot.Broker.CreateOrder(trade);
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.0; // not implemented 
		}
	}
}
