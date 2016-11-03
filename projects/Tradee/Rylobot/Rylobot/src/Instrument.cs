using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	public class Instrument :IDisposable
	{
		/// <summary>Construction is fairly cheap</summary>
		public Instrument(Rylobot bot, string symbol_code)
		{
			Bot = bot;

			// Ensure the instrument settings directory exists
			if (!Path_.DirExists(Bot.Settings.InstrumentSettingsDir))
				Directory.CreateDirectory(Bot.Settings.InstrumentSettingsDir);

			// Load settings for this instrument
			var instr_settings_filepath = Util.ResolveAppDataPath("Rylogic", "Rylobot", ".\\Instruments\\{0}.xml".Fmt(SymbolCode));
			Settings = new InstrumentSettings(instr_settings_filepath);
		}
		public void Dispose()
		{
			Settings = null;
			Bot = null;
		}

		/// <summary>Settings for the current instrument</summary>
		public InstrumentSettings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The App logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Tick -= HandleTick;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Tick += HandleTick;
				}
			}
		}
		private Rylobot m_bot;

		/// <summary>Called when the instrument data has been updated</summary>
		private void HandleTick(object sender, EventArgs e)
		{
			// Get the latest candle
			var candle = this[0];

			// Check if this is the start of a new candle
			NewCandle = candle.Timestamp > Latest.Timestamp;

			// Apply the data to the latest candle or invalidate the cached Latest
			if (NewCandle)
				Latest = null;
			else
				Latest.Update(candle);

			// Record the last time data was received
			LastUpdatedUTC = DateTimeOffset.UtcNow;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this, candle, NewCandle));
		}

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		private void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}

		/// <summary>The instrument time-frame</summary>
		public TimeFrame TimeFrame
		{
			get { return Data.TimeFrame; }
		}

		/// <summary>The instrument data source</summary>
		public MarketSeries Data
		{
			get { return Bot.MarketSeries; }
		}

		/// <summary>The CAlgo Symbol interface for this instrument</summary>
		public Symbol Symbol
		{
			get { return Bot.GetSymbol(SymbolCode); }
		}

		/// <summary>The instrument symbol code</summary>
		public string SymbolCode
		{
			get { return Data.SymbolCode; }
		}

		/// <summary>The size of one pip (in quote currency)</summary>
		public double PipSize
		{
			get { return Symbol.PipSize; }
		}

		/// <summary>The current Ask(+1)/Bid(-1) price</summary>
		public double CurrentPrice(int sign)
		{
			return Symbol.CurrentPrice(sign);
		}

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			get { return Data.OpenTime.Count; }
		}

		/// <summary>Index range (-Count, 0]</summary>
		public NegIdx FirstIdx
		{
			get { return (NegIdx)(1 - Count); }
		}
		public NegIdx LastIdx
		{
			get { return (NegIdx)(+1); }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public Candle this[NegIdx neg_idx]
		{
			get
			{
				Debug.Assert(neg_idx >= FirstIdx && neg_idx < LastIdx);

				// CAlgo uses 0 = oldest, Count = latest
				var idx = (int)(Data.OpenTime.Count + neg_idx - 1);
				return new Candle(
					Data.OpenTime  [idx].Ticks,
					Data.Open      [idx],
					Data.High      [idx],
					Data.Low       [idx],
					Data.Close     [idx],
					Data.Median    [idx],
					Data.TickVolume[idx]);
			}
		}

		/// <summary>The candle with the latest timestamp for the current time frame</summary>
		public Candle Latest
		{
			// Cache the latest candle so that we can detect a new candle starting.
			get { return m_latest ?? (m_latest = this[0]); }
			private set { m_latest = null; }
		}
		private Candle m_latest;

		/// <summary>The candle with the oldest timestamp for the current time frame</summary>
		public Candle Oldest
		{
			get { return this[FirstIdx]; }
		}

		/// <summary>True if the latest update was the start of a new candle (and therefore the old candle just closed)</summary>
		public bool NewCandle
		{
			get;
			private set;
		}

		/// <summary>The timestamp of the last time we received data (not necessarily an update to 'Latest) (in UTC)</summary>
		public DateTimeOffset LastUpdatedUTC
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Clamps the given index range to a valid range within the data. i.e. [-Count,0]</summary>
		public Range IndexRange(NegIdx idx_min, NegIdx idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Maths.Clamp(idx_min, FirstIdx, LastIdx);
			var max = Maths.Clamp(idx_max, min, LastIdx);
			return new Range(min, max);
		}
		public Range IndexRange(Range range)
		{
			return IndexRange(range.Begini, range.Endi);
		}
		public Range IndexRange(Position pos)
		{
			return IndexRange(new Range(IndexAt(pos.EntryTime), LastIdx));
		}
		public Range IndexRange(HistoricalTrade pos)
		{
			return IndexRange(new Range(IndexAt(pos.EntryTime), IndexAt(pos.ClosingTime)));
		}

		/// <summary>Enumerate candles over a range that has been validated</summary>
		private IEnumerable<Candle> CandleRangeInternal(Range checked_range)
		{
			Debug.Assert(checked_range.Begini >= FirstIdx);
			Debug.Assert(checked_range.Endi <= LastIdx);
			for (var i = checked_range.Begini; i != checked_range.Endi; ++i)
				yield return this[i];
		}

		/// <summary>Enumerate the candles within an index range [idx_max,idx_max) (i.e. time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(NegIdx idx_min, NegIdx idx_max)
		{
			return CandleRangeInternal(IndexRange(idx_min, idx_max));
		}
		public IEnumerable<Candle> CandleRange(Range idx_range)
		{
			return CandleRangeInternal(IndexRange(idx_range));
		}

		/// <summary>Return the index into the candle data for the given time</summary>
		public NegIdx IndexAt(DateTimeOffset dt)
		{
			var idx = Data.OpenTime.GetIndexByTime(dt.UtcDateTime);
			return idx + FirstIdx;
		}

		/// <summary>Return the average candle size (total length) of the given range</summary>
		public double MeanCandleSize(NegIdx idx_min, NegIdx idx_max)
		{
			var r = IndexRange(idx_min, idx_max);
			var mean_candle_size = r.Size != 0 ? CandleRangeInternal(r).Average(x => x.TotalLength) : 0;
			return mean_candle_size;
		}
		public double MeanCandleSize(Range range)
		{
			return MeanCandleSize(range.Begini, range.Endi);
		}

		/// <summary>Return the age of 'candle' normalised to the time frame</summary>
		public double AgeOf(Candle candle, bool clamped)
		{
			var ticks = Bot.UtcNow.Ticks - candle.TimestampUTC.Ticks;
			var age = (double)ticks / TimeFrame.ToTicks();
			if (clamped) age = Maths.Clamp(age, 0.0, 1.0);
			return age;
		}

		/// <summary>Return the true range for the candle at index position 'index'</summary>
		public double TrueRange(NegIdx index)
		{
			// True Range is defined as the greater of:
			// High of the current period less the low of the current period
			// The high of the current period less the previous period’s closing value
			// The low of the current period less the previous period’s closing value
			if (index < FirstIdx)
				return 0.0;
			if (index < FirstIdx + 1)
				return this[FirstIdx].TotalLength;

			var A = this[index - 0];
			var B = this[index - 1];
			return Maths.Max(A.TotalLength, Math.Abs(A.High - B.Close), Math.Abs(A.Low - B.Close));
		}

		/// <summary>
		/// Measures the trend over candles [idx_min, idx_max).
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)</summary>
		public double MeasureTrend(NegIdx idx_min, NegIdx idx_max)
		{
			// Compress adjacent candles with the same sign into "group candles"
			// Scan backwards from 'idx_max-1'
			// Scan back to the start of a group, even if it is earlier than 'idx_min'
			// so that strong trends are fairly represented.
			// Scale by the ratio of body range to full range so that high volatility
			// reduces trend strength
			var range = IndexRange(idx_min, idx_max);
			if (range.Empty)
				return 0.0;

			// The price range covered by the trend
			var body_range = RangeF.Invalid;
			var full_range = RangeF.Invalid;
			var group = RangeF.Invalid;

			var sign  = 0;   // The sign of the current block
			var bulls = 0.0; // The net sum of the bullish blocks
			var bears = 0.0; // The net sum of the bearish blocks
			for (var i = range.Endi; i-- != FirstIdx;)
			{
				var candle = this[i];

				// Find the range spanned.
				body_range.Encompass(candle.Open);
				body_range.Encompass(candle.Close);
				full_range.Encompass(candle.High);
				full_range.Encompass(candle.Low);

				// Use candle bodies rather than wicks because wicks represent
				// price changes that have been cancelled out therefore they don't
				// contribute to the trend
				if (sign == 0)
				{
					// Initialise on first candle
					sign = candle.Sign;
					group = candle.BodyRange;
				}

				// Calculate the group candle length
				group.Encompass(candle.BodyLimit(-sign));

				// If the trend changes direction add the group range to the appropriate accumulator
				if (candle.Sign != sign)
				{
					// If this trend has extended beyond 'idx_min', break
					if (i < range.Begini) break;

					// Otherwise accumulate and continue
					if (sign < 0) bears += group.Size;
					else          bulls += group.Size;

					sign = candle.Sign;
					group = candle.BodyRange;
				}
			}
			if (sign < 0) bears += group.Size;
			else          bulls += group.Size;

			var trend_strength = Maths.Div(bulls - bears, bulls + bears);
			var volatility = Maths.Div(body_range.Size, full_range.Size);
			return trend_strength * volatility;
		}
	}

	// "typedef" for negative instrument indices
	[DebuggerDisplay("{value}")]
	public struct NegIdx
	{
		private int value;
		[DebuggerStepThrough] private NegIdx(int v) { value = v; }
		[DebuggerStepThrough] public static implicit operator int(NegIdx neg_idx) { return neg_idx.value; }
		[DebuggerStepThrough] public static implicit operator NegIdx(int neg_idx) { return new NegIdx(neg_idx); }
		[DebuggerStepThrough] public override string ToString() { return value.ToString(); }
	}

	#region EventArgs

	/// <summary>Event args for when data is changed</summary>
	public class DataEventArgs :EventArgs
	{
		public DataEventArgs(Instrument instr, Candle candle, bool new_candle)
		{
			Instrument = instr;
			Candle     = candle;
			NewCandle  = new_candle;
		}

		/// <summary>The instrument containing the changes</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>The candle that was added to 'Table'</summary>
		public Candle Candle { get; private set; }

		/// <summary>True if 'Candle' is a new candle and the previous candle as just closed</summary>
		public bool NewCandle { get; private set; }
	}

	#endregion
}
