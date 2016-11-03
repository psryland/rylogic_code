using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	public abstract class Predictor :IDisposable
	{
		// This is a base class for a signal trigger.
		// Derived types use various methods to guess where price is going
		// and their accuracy is tested.

		public Predictor(Rylobot bot, string name)
		{
			Bot         = bot;
			Name        = name;
			Instrument  = new Instrument(bot, bot.Symbol.Code);
			Predictions = new List<Prediction>();
			Results     = Util.NewArray(Bot.Settings.PredictionForecastLength, i => new RtR(i));
		}
		public virtual void Dispose()
		{
			Instrument = null;
			Bot = null;
		}

		/// <summary>A name for this predictor</summary>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>Application logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{}
				m_bot = value;
				if (m_bot != null)
				{}
			}
		}
		private Rylobot m_bot;

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			get { return m_instr; }
			private set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					m_instr.DataChanged -= HandleDataChanged;
					Util.Dispose(ref m_instr);
				}
				m_instr = value;
				if (m_instr != null)
				{
					m_instr.DataChanged += HandleDataChanged;
				}
			}
		}
		private Instrument m_instr;

		/// <summary>The predicted best trade direction, or null if no prediction</summary>
		public TradeType? Forecast
		{
			get { return m_forecast; }
			protected set
			{
				if (m_forecast == value) return;
				m_forecast = value;
				Comments = string.Empty;

				// If a trade direction is specified, add it as a prediction
				if (m_forecast != null)
				{
					var tt = m_forecast.Value;
					var price = Bot.Symbol.CurrentPrice(tt.Sign());
					Predictions.Add(new Prediction(tt, price, 0));
				}

				ForecastChange.Raise(this);
			}
		}
		private TradeType? m_forecast;

		/// <summary>Notes about the logic that lead to the current forecast. Set 'Forecast' first</summary>
		public string Comments
		{
			get;
			protected set;
		}

		/// <summary>Raised whenever the predictor has a guess at the future</summary>
		public event EventHandler ForecastChange;

		/// <summary>A collection of predictions</summary>
		protected List<Prediction> Predictions
		{
			get;
			private set;
		}

		/// <summary>The Reward to Risk verses candle index after the prediction was made</summary>
		public RtR[] Results
		{
			get;
			set;
		}

		/// <summary>Called when new data is added to the instrument</summary>
		private void HandleDataChanged(object sender, DataEventArgs e)
		{
			// Allow derived types to make predictions
			Step(e);

			// Whenever a new candle is added, the start index moves down one
			if (e.NewCandle)
			{
				foreach (var pred in Predictions)
					--pred.StartIndex;

				// Drop predictions older than the forecast length
				Predictions.RemoveIf(x => -x.StartIndex >= Bot.Settings.PredictionForecastLength);
			}

			// Use the current price to determine new max profit/loss
			foreach (var pred in Predictions)
			{
				var profit = 0.0;
				var loss = 0.0;
				if (pred.PredictedTrend == TradeType.Buy)
				{
					var cpr = Instrument.CurrentPrice(-1);     // Current price assuming profit
					var lmt = Instrument.Latest.WickLimit(+1); // Candle wick high limit
					profit = Math.Max(cpr,lmt) - pred.EntryPrice;

					cpr = Instrument.CurrentPrice(+1);     // Current price assuming loss
					lmt = Instrument.Latest.WickLimit(-1); // Candle wick low limit
					loss = pred.EntryPrice - Maths.Min(cpr,lmt);
				}
				if (pred.PredictedTrend == TradeType.Sell)
				{
					var cpr = Instrument.CurrentPrice(+1); // Current price assuming profit
					var lmt = Instrument.Latest.WickLimit(-1) + Instrument.Symbol.Spread; // Candle wick low
					profit = pred.EntryPrice - Math.Min(cpr,lmt);

					cpr = Instrument.CurrentPrice(-1);; // Current price assuming loss
					lmt = Instrument.Latest.WickLimit(+1) + Instrument.Symbol.Spread; // Candle wick high
					loss = Math.Max(cpr,lmt) - pred.EntryPrice;
				}

				pred.MaxProfit = Math.Max(pred.MaxProfit, profit);
				pred.MaxLoss   = Math.Max(pred.MaxLoss, loss);

				// Accumulate the results
				Results[-pred.StartIndex].TP.Add(pred.MaxProfit);
				Results[-pred.StartIndex].SL.Add(pred.MaxLoss);
				Results[-pred.StartIndex].RR.Add(pred.MaxProfit / pred.MaxLoss);
			}
		}

		/// <summary>Look for predictions with each new data element</summary>
		protected abstract void Step(DataEventArgs e);

		/// <summary>The reward to risk for a particular distance after a prediction</summary>
		public class RtR
		{
			public RtR(int index)
			{
				Index = index;
				TP = new AvrVar();
				SL = new AvrVar();
				RR = new AvrVar();
			}

			public int Index { get; private set; }

			/// <summary>The maximum number of pips on the winning side of the prediction at 'Index'</summary>
			public AvrVar TP { get; set; }

			/// <summary>The maximum number of pips on the losing side of the prediction at 'Index'</summary>
			public AvrVar SL { get; set; }

			/// <summary>The reward to risk ratio at 'Index'</summary>
			public AvrVar RR { get; set; }
		}

		/// <summary>Records what happens to the price over time</summary>
		protected class Prediction
		{
			public Prediction(TradeType tt, double price, NegIdx index)
			{
				PredictedTrend = tt;
				EntryPrice = price;
				StartIndex = index;
				MaxProfit = -double.MaxValue;
				MaxLoss = -double.MaxValue;
			}

			/// <summary>The predicted price direction</summary>
			public TradeType PredictedTrend { get; private set; }

			/// <summary>The candle index (NegIdx) at the time the prediction was made</summary>
			public NegIdx StartIndex { get; set; }

			/// <summary>The price at the time the prediction was made</summary>
			public double EntryPrice { get; private set; }

			/// <summary>The maximum distance (in quote currency) that the price has moved in the profit direction</summary>
			public double MaxProfit { get; set; }

			/// <summary>The maximum distance (in quote currency) that the price has moved in the loss direction</summary>
			public double MaxLoss { get; set; }
		}
	}
}
