using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class CandlePatterns :IDisposable
	{
		public CandlePatterns()
		{}
		public CandlePatterns(XElement node)
		{
			Settings = node.Element(nameof(Settings)).As(Settings);
		}
		public virtual void Dispose()
		{
			Instrument = null;
		}

		/// <summary>Export to XML</summary>
		public XElement ToXml(XElement node)
		{
			node.Add2(nameof(Settings), Settings);
			return node;
		}

		/// <summary>Settings</summary>
		public CandlePatternSettings Settings
		{
			get;
			private set;
		}

		/// <summary>The instrument we're watching candles on</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			set
			{
				if (m_instr == value) return;
				if (m_instr != null)
				{
					m_instr.DataChanged -= HandleInstrumentDataChanged;
				}
				m_instr = value;
				if (m_instr != null)
				{
					m_instr.DataChanged += HandleInstrumentDataChanged;
					HandleInstrumentDataChanged(this, new DataEventArgs(m_instr, m_instr.TimeFrame, m_instr.Latest, false));
				}
			}
		}
		private Instrument m_instr;

		/// <summary>Handle new instrument data</summary>
		private void HandleInstrumentDataChanged(object sender, DataEventArgs args)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(() =>
			{
				CheckSequences();
				CheckTrendLength();
			});
		}

		#region Candle Sequences

		/// <summary>Candle pattern types</summary>
		public enum EType
		{
			/// <summary>Candles indicate that the trend still seems strong</summary>
			Trend,

			/// <summary>Price looks like it has reversed and will go in the current trend direction</summary>
			Reversal,

			/// <summary>Price is probably going to continue in the current trend direction</summary>
			Continuation,

			/// <summary>Candles show no clear trend</summary>
			Consolidation,
		}

		/// <summary>The sequence of candles just prior to the latest</summary>
		public EType CandlePattern
		{
			get;
			private set;
		}

		/// <summary>Trend directions</summary>
		public enum ETrend
		{
			/// <summary>No clear trend</summary>
			None,

			/// <summary>Trending upward</summary>
			Up,

			/// <summary>Trending downward</summary>
			Down,

			/// <summary>Oscillating about a fixed level</summary>
			Ranging,
		}

		/// <summary>The trend direction that the candle pattern predicts</summary>
		public ETrend Trend
		{
			get;
			private set;
		}

		/// <summary>Set the pattern and trend (in one go)</summary>
		private void SetCandlePattern(EType type, ETrend trend)
		{
			// No change?
			if (CandlePattern == type && Trend == trend)
				return;

			// Update both at once, to prevent accidental decisions based on half the info.
			CandlePattern = type;
			Trend = trend;
			OnCandlePatternChanged();
		}

		/// <summary>Look for candle sequences that mean something</summary>
		private void CheckSequences()
		{
			if (Instrument.Count < 3)
				return;

			// Get the last few candles
			// Make decisions based on 'B', the last closed candle.
			var A = Instrument[ 0];
			var B = Instrument[-1];
			var C = Instrument[-2];

			// The age of 'A' (normalised)
			var a_age = A.Age(Instrument);

			// Measure the strength of the trend leading up to 'B' (but not including)
			var preceding_trend = Instrument.MeasureTrend(-5, 4);

			// Opposing trend
			var opposing_trend =
				!C.Type.IsIndecision() && !B.Type.IsIndecision() && // Bodies are a reasonable size
				(C.Sign >= 0) != (B.Sign >= 0);                     // Opposite sign

			// Engulfing: A trend, ending with a reasonable sized body,
			// followed by a bigger body in the opposite direction.
			if ((opposing_trend) &&                                    // Opposing trend directions
				(C.Type.IsTrend() && B.Type.IsTrend()) &&              // Both trend-ish candles
				(B.BodyLength > 1.20 * C.BodyLength) &&                // B is bigger than 120% C
				(Math.Abs(B.Open - C.Close) < 0.05 * B.TotalLength) && // B.Open is fairly close to C.Close
				(Math.Abs(preceding_trend) > 0.8) &&                   // There was a trend leading into B
				(Math.Sign(preceding_trend) != B.Sign))                // The trend was the opposite of B
			{
				SetCandlePattern(EType.Reversal, B.Sign > 0 ? ETrend.Up : ETrend.Down);
				return;
			}

			// Trend, indecision, trend:
			if ((B.Type.IsIndecision()) &&         // A hammer, spinning top, or doji
				(Math.Abs(preceding_trend) > 0.8)) // A trend leading into the indecision
			{
				// This could be a continuation or a reversal. Need to look at 'A' to decide
				if (!A.Type.IsIndecision()) // If A is not an indecision candle as well
				{
					// Use the indecision candle total length to decide established trend.
					// Measure relative to B.BodyCentre.
					// The stronger the indication, the least old 'A' has to be
					var dist = Math.Abs(A.Close - B.BodyCentre);
					var frac = Maths.Frac(B.TotalLength, dist, B.TotalLength * 2.0);
					if (a_age >= 1.0 - Maths.Frac(0.0, dist, 1.0))
					{
						var reversal = Math.Sign(preceding_trend) != A.Sign;
						SetCandlePattern(reversal ? EType.Reversal : EType.Continuation, A.Sign > 0 ? ETrend.Up : ETrend.Down);
						return;
					}
				}
			}

			// Tweezers
			if ((C.Type == Candle.EType.MarubozuWeakening && B.Type == Candle.EType.MarubozuStrengthening) &&
				(Math.Abs(preceding_trend) > 0.8))
			{
				SetCandlePattern(EType.Reversal, B.Sign > 0 ? ETrend.Up : ETrend.Down);
				return;
			}

			// Continuing trend
			if ((Math.Abs(preceding_trend) > 0.8) && // Preceding trend
				(B.Type.IsTrend() && B.Sign == Math.Sign(preceding_trend)))
			{
				SetCandlePattern(EType.Trend, B.Sign > 0 ? ETrend.Up : ETrend.Down);
				return;
			}

			// Consolidation
			if ((Math.Abs(preceding_trend) < 0.5) &&
				!B.Type.IsIndecision() &&
				!B.Type.IsTrend())
			{
				SetCandlePattern(EType.Consolidation, ETrend.None);
				return;
			}
		}

		/// <summary>Raised when the identified candle pattern changes</summary>
		public event EventHandler CandlePatternChanged;
		private void OnCandlePatternChanged()
		{
			CandlePatternChanged.Raise(this);
		}

		#endregion

		#region Trend length

		/// <summary>Find the length of the sequence of continuously rising/falling</summary>
		private void CheckTrendLength()
		{
			var hi_falling = 0;
			var hi_rising = 0;
			var lo_falling = 0;
			var lo_rising = 0;

			// Find the rising/falling sequence lengths
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].High <= Instrument[i+1].High; --i, ++hi_rising ) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].High >= Instrument[i+1].High; --i, ++hi_falling) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].Low  <= Instrument[i+1].Low;  --i, ++lo_rising ) {}
			for (int i = Instrument.LastIdx - 2; i >= Instrument.FirstIdx && Instrument[i+0].Low  >= Instrument[i+1].Low;  --i, ++lo_falling) {}

			// These values can only be equal if the data is flat all the way back to 'FirstIdx'
			HighSeqLength = Math.Max(hi_rising, hi_falling);
			LowSeqLength  = Math.Max(lo_rising, lo_falling);
			HighRising = hi_rising > hi_falling;
			LowRising  = lo_rising > lo_falling;

			// Notify
			OnTrendLengthChanged();
		}

		/// <summary>The number of candles before the latest where the high has been consistently increasing or decreasing.</summary>
		public int HighSeqLength { get; private set; }

		/// <summary>True if the candle highs are rising, false if falling</summary>
		public bool HighRising { get; private set; }

		/// <summary>The number of candles before the latest where the low has been consistently increasing or decreasing.</summary>
		public int LowSeqLength { get; private set; }

		/// <summary>True if the candle lows are rising, false if falling</summary>
		public bool LowRising { get; private set; }

		/// <summary>Raised when either 'RisingLength' or 'FallingLength' changed</summary>
		public event EventHandler TrendLengthChanged;
		private void OnTrendLengthChanged()
		{
			TrendLengthChanged.Raise(this);
		}

		#endregion
	}

	#region Settings

	[TypeConverter(typeof(TyConv))]
	public class CandlePatternSettings :SettingsXml<CandlePatternSettings>
	{
		public CandlePatternSettings()
		{ }

		private class TyConv :GenericTypeConverter<CandlePatternSettings> {}
	}

	#endregion
}
