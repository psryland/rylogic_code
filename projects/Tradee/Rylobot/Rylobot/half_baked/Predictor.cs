using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	public abstract class Predictor :IDisposable
	{
		// Notes:
		// This is a base class for an entry signal trigger.
		// Derived types use various methods to guess where price is going.
		// Derived types add 'Feature' objects to the 'Features' collection, these are basically
		// values in the range [-1.0,+1.0] where -1.0 = strong sell, +1.0 = strong buy.

		public Predictor(Rylobot bot, string name)
		{
			Bot          = bot;
			Name         = name;
			CurrentIndex = 0;
			Instrument   = new Instrument(bot);
			Features     = new List<Feature>();
			LogFilepath  = Path_.CombinePath(@"P:\projects\Tradee\Rylobot\Rylobot\net\Data", "{0}.predictions.csv".Fmt(name));
		}
		public virtual void Dispose()
		{
			TrackForecasts = false;
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

		/// <summary>
		/// The index to treat as the 'latest' candle.
		/// This is typically 0, but can be used to run a predictor behind the latest when
		/// measuring success rate.</summary>
		public NegIdx CurrentIndex
		{
			[DebuggerStepThrough] get { return m_current_index; }
			set
			{
				if (m_current_index == value) return;
				m_current_index = value;
				OnCurrentIndexChanged();
			}
		}
		private NegIdx m_current_index;

		/// <summary>Track forecasts to measure success rate</summary>
		public bool TrackForecasts
		{
			get { return m_track_forecasts; }
			set
			{
				if (m_track_forecasts == value) return;
				if (m_track_forecasts)
				{
					Predictions = null;
				}
				m_track_forecasts = value;
				if (m_track_forecasts)
				{
					Predictions = new List<Prediction>();
					m_reset_log = true;
				}
			}
		}
		private bool m_track_forecasts;

		/// <summary>Raised when the current index changes, meaning the "now" position has moved</summary>
		public event EventHandler CurrentIndexChanged;
		protected virtual void OnCurrentIndexChanged()
		{
			CurrentIndexChanged.Raise(this);
		}

		/// <summary>The predicted best trade direction, or null if no prediction</summary>
		public TradeType? Forecast
		{
			get { return m_forecast; }
			private set
			{
				if (m_forecast == value) return;
				m_forecast = value;
				OnForecastChange();
			}
		}
		private TradeType? m_forecast;

		/// <summary>Raised whenever the predictor has a guess at the future</summary>
		public event EventHandler ForecastChange;
		protected virtual void OnForecastChange()
		{
			ForecastChange.Raise(this);

			// If track forecasts is enabled, create a trade that we'll watch
			if (TrackForecasts && Forecast != null)
			{
				// Create a prediction with this forecast
				var trade = new Trade(Bot, Instrument, Forecast.Value, Name, CurrentIndex);
				var features = new List<Feature>(Features);
				Predictions.Add(new Prediction(trade, features));
			}
		}

		/// <summary>
		/// A vector of signal values (features)
		/// Each feature value should be a value from [-1,+1]
		/// Values &gt; 0.5 indicate buy signals, &lt; -0.5 indicate sell signals.summary>
		public List<Feature> Features
		{
			get;
			private set;
		}

		/// <summary>A collection of predictions</summary>
		private List<Prediction> Predictions
		{
			get;
			set;
		}

		/// <summary>
		/// Update the 'Features' vector with values for the quality of a trade at 'CurrentIndex'.
		/// Note: args.Candle is the candle at 'CurrentIndex'</summary>
		protected abstract void UpdateFeatureValues(DataEventArgs args);
		public void UpdateFeatureValues()
		{
			var candle = Instrument[CurrentIndex];
			UpdateFeatureValues(new DataEventArgs(Instrument, candle, false));
		}

		/// <summary>Called when new data is added to the instrument</summary>
		private void HandleDataChanged(object sender, DataEventArgs args)
		{
			// Get the "latest" candle at 'CurrentIndex'
			var candle = args.Instrument[CurrentIndex];

			// Get derived types to update the state of the features vector
			args = new DataEventArgs(args.Instrument, candle, args.NewCandle);
			UpdateFeatureValues(args);

			// No features == no Forecast 
			if (Features.Count == 0)
				return;

			// Make a forecast based on the features
			// This can be replaced by a neural net prediction of the features
			var signal = Features.Average(x => x.Value);
			Forecast =
				signal > +0.5 ? (TradeType?)TradeType.Buy :
				signal < -0.5 ? (TradeType?)TradeType.Sell :
				null;

			// Track predictions
			if (TrackForecasts)
			{
				// Advance the predictions and log the results
				foreach (var pred in Predictions)
				{
					// When a trade closes record the result
					pred.Trade.AddCandle(candle, CurrentIndex);
					if (pred.Trade.Result != Trade.EResult.Open)
						LogPrediction(pred);
				}

				// Remove closed predictions
				Predictions.RemoveIf(x => x.Trade.Result != Trade.EResult.Open);
			}
		}

		/// <summary>Where to write the prediction log to</summary>
		public string LogFilepath { get; set; }

		/// <summary>Reset the log file</summary>
		private void ResetLog()
		{
			using (var log = new StreamWriter(new FileStream(LogFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)))
			{
				log.WriteLine(Str.Build(
					"Succeeded, ",
					"TradeType, ",
					"Profit, ",
					"RtR, ",
					string.Join(", ", Features.Select(x => x.Label)),
					string.Empty));
			}
			m_reset_log = false;
		}
		private bool m_reset_log;

		/// <summary>Add the results of a virtual trade to the log</summary>
		private void LogPrediction(Prediction pred)
		{
			if (m_reset_log)
				ResetLog();

			var trade = pred.Trade;
			var features = pred.Features;
			using (var log = new StreamWriter(new FileStream(LogFilepath, FileMode.Append, FileAccess.Write, FileShare.Read)))
			{
				log.WriteLine(Str.Build(
					trade.Result == Trade.EResult.HitTP ? 1 : 0, ", ",
					trade.TradeType, ", ",
					(trade.Result == Trade.EResult.HitTP ? trade.PeakProfit : -trade.PeakLoss) * trade.Volume, ", ",
					trade.RtR, ", ",
					string.Join(", ", features.Select(x => x.Value)),
					string.Empty));
			}
		}

		/// <summary>Returns the feature string for the candle at position 'neg_idx'</summary>
		public string FeaturesString(NegIdx neg_idx)
		{
			m_sb = m_sb ?? new StringBuilder();
			m_sb.Clear();

			// Output feature values
			m_sb.Append("|features ");
			foreach (var v in Features)
				m_sb.Append(" ").Append(v.Value);

			return m_sb.ToString();
		}
		private StringBuilder m_sb;

		/// <summary>A signal feature value</summary>
		[DebuggerDisplay("{Label} {Value} {Comment}")]
		public class Feature
		{
			public Feature(string label, double value, string comment = null)
			{
				Label   = label;
				Value   = value;
				Comment = comment;
			}

			/// <summary>A name for the feature</summary>
			public string Label { get; private set; }

			/// <summary>The feature value</summary>
			public double Value { get; private set; }

			/// <summary>Comments about the feature</summary>
			public string Comment { get; set; }
		}

		/// <summary>A prediction is a virtual trade plus state that triggered the trade</summary>
		[DebuggerDisplay("{Trade}")]
		private class Prediction
		{
			public Prediction(Trade trade, List<Feature> features)
			{
				Trade    = trade;
				Features = features;
			}

			/// <summary>The virtual trade made using the forecast</summary>
			public Trade Trade { get; private set; }

			/// <summary>State data used to make the forecast</summary>
			public List<Feature> Features { get; private set; }
		}
	}
}
