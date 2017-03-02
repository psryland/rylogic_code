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
		public Instrument(Rylobot bot)
			:this(bot, bot.MarketSeries)
		{}
		public Instrument(Rylobot bot, Symbol sym, TimeFrame tf)
			:this(bot, bot.GetSeries(sym, tf))
		{}
		public Instrument(Rylobot bot, MarketSeries series)
		{
			Bot = bot;
			Data = series;
			HighRes = new HighResCollection(this, 10000);

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
			var now = Bot.UtcNow;

			// Check if this is the start of a new candle
			NewCandle = candle.Timestamp > Latest.Timestamp;

			// Get the fractional index for this tick
			var index = FractionalIndexAt(now) - (double)IdxFirst;
			var price = new PriceTick(index, Symbol.Ask, Symbol.Bid);
			Debug.Assert(HighRes.Count == 0 || index > HighRes.Back().Index);
			//var idx = HighRes.Count == 0 || index > HighRes.Back().Index ? HighRes.Count : HighRes.BinarySearch(x => x.Index.CompareTo(index));
			//if (idx < 0) idx = ~idx;

			// Capture the high res price data
			HighRes.Add(price);
			//HighRes.Insert(idx, price);
			//if (HighRes.Count > 3 * HighResHistoryLength / 2)
			//	HighRes.RemoveRange(0, HighRes.Count - HighResHistoryLength);

			// Apply the data to the latest candle or invalidate the cached Latest
			if (NewCandle)
				Latest = null;
			else
				Latest.Update(candle);

			// Record the last time data was received
			m_last_tick_time = now.Ticks;

			// Invalidate cached, per-candle, properties
			m_max_cs = null;
			m_median_cs = null;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this, candle, NewCandle));
		}

		/// <summary>
		/// Return this instrument at a higher or lower time frame (e.g. this=h1, ratio=2 => h2. this=h1, ratio=0.5 => m30)).
		/// Note: can't use this with back testing</summary>
		public Instrument RelativeTimeFrame(double ratio)
		{
			var tf = TimeFrame.GetRelativeTimeFrame(ratio);
			return new Instrument(Bot, Symbol, tf);
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
			get;
			private set;
		}

		/// <summary>The high res data for the instrument.</summary>
		public HighResCollection HighRes
		{
			get;
			private set;
		}
		public class HighResCollection :List<PriceTick>
		{
			private readonly Instrument m_instr;
			public HighResCollection(Instrument instr, int max_length)
			{
				m_instr = instr;
				MaxLength = max_length;
			}

			/// <summary>The maximum length the collection will grow to</summary>
			public int MaxLength
			{
				get;
				set;
			}

			/// <summary>Add to the collection</summary>
			public new PriceTick Add(PriceTick price)
			{
				base.Add(price);
				if (Count > 3 * MaxLength / 2) RemoveRange(0, Count - MaxLength);
				return price;
			}
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

		/// <summary>The price spread</summary>
		public QuoteCurrency Spread
		{
			get { return Symbol.Spread; }
		}

		/// <summary>The size of one pip (in quote currency)</summary>
		public QuoteCurrency PipSize
		{
			get { return Symbol.PipSize; }
		}

		/// <summary>The current Ask(+1)/Bid(-1)/Mid(0) price. Remember: Ask > Bid</summary>
		public QuoteCurrency CurrentPrice(int sign)
		{
			return Symbol.CurrentPrice(sign);
		}

		/// <summary>The current Ask(+1)/Bid(-1)/Mid(0) price</summary>
		public PriceTick LatestPrice
		{
			[DebuggerStepThrough] get { return new PriceTick(0, CurrentPrice(+1), CurrentPrice(-1)); }
		}

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			get { return Data.OpenTime.Count; }
		}

		/// <summary>Index range (-Count, 0]</summary>
		public NegIdx IdxFirst
		{
			get { return (NegIdx)(1 - Count); }
		}
		public NegIdx IdxLast
		{
			get { return (NegIdx)(+1); }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public Candle this[NegIdx neg_idx]
		{
			get
			{
				Debug.Assert(neg_idx >= IdxFirst && neg_idx < IdxLast);

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
			get { return this[IdxFirst]; }
		}

		/// <summary>True if the latest update was the start of a new candle (and therefore the old candle just closed)</summary>
		public bool NewCandle
		{
			get;
			private set;
		}

		/// <summary>The timestamp of the last time we received price data (in UTC)</summary>
		public DateTimeOffset LastTickUTC
		{
			[DebuggerStepThrough] get { return new DateTimeOffset(m_last_tick_time, TimeSpan.Zero); }
		}
		private long m_last_tick_time;

		/// <summary>Clamps the given index range to a valid range within the data. i.e. [-Count,0]</summary>
		public Range IndexRange(NegIdx idx_min, NegIdx idx_max)
		{
			Debug.Assert(idx_min <= idx_max);
			var min = Maths.Clamp((int)idx_min, (int)IdxFirst, (int)IdxLast);
			var max = Maths.Clamp((int)idx_max, (int)min, (int)IdxLast);
			return new Range(min, max);
		}
		public Range IndexRange(Range range)
		{
			return IndexRange(range.Begi, range.Endi);
		}
		public Range IndexRange(Position pos)
		{
			return IndexRange(new Range((int)IndexAt(pos.EntryTime), (int)IdxLast));
		}
		public Range IndexRange(HistoricalTrade pos)
		{
			return IndexRange(new Range((int)IndexAt(pos.EntryTime), (int)IndexAt(pos.ClosingTime)));
		}

		/// <summary>Enumerate candles over a range that has been validated</summary>
		private IEnumerable<Candle> CandleRangeInternal(Range checked_range)
		{
			Debug.Assert(checked_range.Begi >= IdxFirst);
			Debug.Assert(checked_range.Endi <= IdxLast);
			for (var i = checked_range.Begi; i != checked_range.Endi; ++i)
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
		public IEnumerable<Candle> CandleRange()
		{
			return CandleRangeInternal(IndexRange(IdxFirst, IdxLast));
		}

		/// <summary>
		/// Iterate through the high res price data over the given range with a step size of 'step'.
		/// 'first' and 'last' floating point NegIdx's (not indices in HighRes)
		/// 'step' is the increment size in indices to move with each returned value</summary>
		public IEnumerable<PriceTick> HighResRange(double idx_min, double idx_max, double? step = null)
		{
			var first = idx_min - (double)IdxFirst;
			var last  = idx_max - (double)IdxFirst;

			var istart = HighRes.BinarySearch(x => x.Index.CompareTo(first));
			var iend   = HighRes.BinarySearch(x => x.Index.CompareTo(last));
			if (istart < 0) istart = ~istart;
			if (iend   < 0) iend = ~iend;
			if (istart == iend)
				yield break;

			// The point to return
			var pt = HighRes[istart];

			// Loop over the requested range
			var X = HighRes[istart].Index;
			for (var i = istart; i < iend;)
			{
				yield return pt;

				// Get the next point
				if (step == null)
				{
					if (++i < iend)
						pt = HighRes[i];
				}
				// Scan forward to the next price point to output
				else
				{
					X += step.Value;

					// Find the range of the ask/bid price for the skipped price values
					// Return the average X position of the skipped values
					var cnt = 0;
					pt = PriceTick.Invalid;
					for (++i; i < iend; ++i)
					{
						pt.Index += HighRes[i].Index;
						pt.Ask = Math.Max((double)pt.Ask, (double)HighRes[i].Ask);
						pt.Bid = Math.Min((double)pt.Bid, (double)HighRes[i].Bid);
						++cnt;
						if (HighRes[i].Index > X)
							break;
					}
					pt.Index /= cnt;
				}
			}
		}
		public IEnumerable<PriceTick> HighResRange(RangeF idx_range, double? step = null)
		{
			return HighResRange(idx_range.Beg, idx_range.End, step);
		}

		/// <summary>Return the index into the candle data for the given time</summary>
		public NegIdx IndexAt(DateTimeOffset dt)
		{
			var idx = Data.OpenTime.GetIndexByTime(dt.UtcDateTime);
			return idx + IdxFirst;
		}

		/// <summary>Return the fractional index into the candle data for the given time</summary>
		public double FractionalIndexAt(DateTimeOffset dt)
		{
			var idx = IndexAt(dt);

			var candle = this[idx];
			var ticks = dt.Ticks - candle.Timestamp;
			var ticks_per_candle = TimeFrame.ToTicks();
			return (double)idx + Maths.Clamp((double)ticks / ticks_per_candle, 0.0, 1.0);
		}

		/// <summary>A cached median candle size over the last 50 candles</summary>
		public QuoteCurrency MaxCS_50
		{
			get { return (m_max_cs ?? (m_max_cs = MaxCandleSize(IdxLast - 50, IdxLast))).Value; }
		}
		private QuoteCurrency? m_max_cs;

		/// <summary>Return the max candle size (total length) of the given range</summary>
		public QuoteCurrency MaxCandleSize(NegIdx idx_min, NegIdx idx_max)
		{
			var range = IndexRange(idx_min, idx_max);
			if (range.Empty) throw new Exception("Empty range, max candle size is not defined");
			var max_cs = range.Max(x => TrueRange((NegIdx)x));
			return max_cs;
		}
		public QuoteCurrency MaxCandleSize(Range range)
		{
			return MaxCandleSize(range.Begi, range.Endi);
		}

		/// <summary>A cached median candle size over the last 50 candles</summary>
		public QuoteCurrency MedianCS_50
		{
			get { return (m_median_cs ?? (m_median_cs = MedianCandleSize(IdxLast - 50, IdxLast))).Value; }
		}
		private QuoteCurrency? m_median_cs;

		/// <summary>Return the median candle size (total length) of the given range</summary>
		public QuoteCurrency MedianCandleSize(NegIdx idx_min, NegIdx idx_max)
		{
			var lengths = IndexRange(idx_min, idx_max).Select(x => TrueRange((NegIdx)x)).ToArray();
			if (lengths.Length == 0) throw new Exception("Empty range, median candle size is not defined");
			var mcs = lengths.NthElement(lengths.Length/2);
			return mcs;
		}
		public QuoteCurrency MedianCandleSize(Range range)
		{
			return MedianCandleSize(range.Begi, range.Endi);
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
		public QuoteCurrency TrueRange(NegIdx index)
		{
			// True Range is defined as the greater of:
			// High of the current period less the low of the current period
			// The high of the current period less the previous period’s closing value
			// The low of the current period less the previous period’s closing value
			if (index < IdxFirst)
				return 0.0;
			if (index < IdxFirst + 1)
				return this[IdxFirst].TotalLength;

			var A = this[index - 0];
			var B = this[index - 1];
			return Maths.Max(A.TotalLength, Math.Abs(A.High - B.Close), Math.Abs(A.Low - B.Close));
		}

		/// <summary>
		/// Measures the trend over candles [idx_min, idx_max).
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)
		/// Treat ­0.5 as the limit between trend and no trend.</summary>
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
			for (var i = range.Endi; i-- != IdxFirst;)
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
					// Initialise on the first candle
					sign = candle.Sign;
					group = candle.BodyRange;
				}

				// Calculate the group candle length
				group.Encompass(candle.BodyLimit(-sign));

				// If the trend changes direction add the group range to the appropriate accumulator
				if (candle.Sign != sign)
				{
					// If this trend has extended beyond 'idx_min', break
					if (i < range.Begi) break;

					// Otherwise accumulate and continue
					if (sign < 0) bears += group.Size;
					else          bulls += group.Size;

					sign = candle.Sign;
					group = candle.BodyRange;
				}
			}
			if (sign < 0) bears += group.Size;
			else          bulls += group.Size;

			// Compare the sum of bulls and the sum of bears to the total range
			var trend_strength = Maths.Div(bulls - bears, bulls + bears);
			var volatility = Maths.Div(body_range.Size, full_range.Size);
			var trend = trend_strength * volatility;
			return trend;
		}

		/// <summary>
		/// Classify a slope in price/candle as a trend direction
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)
		/// Scales the range [-inf,+inf] to [-1,+1]. Treat ­0.5 as the limit between trend and no trend.</summary>
		public double MeasureTrend(double slope)
		{
			// If the slope is greater/less than MCS / N candles classify as a trend
			var run = 20.0;
			var rise = (double)MedianCS_50;
			var threshold = rise / run;
			var trend = Maths.Sigmoid(slope, threshold);
			return trend;
		}

		/// <summary>
		/// Classify the trend at the given index using the slope of the slow EMA.
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)
		/// Scales the range [-inf,+inf] to [-1,+1]. Treat ­0.5 as the limit between trend and no trend.</summary>
		public double MeasureTrend(NegIdx idx, DataSeries data = null, int? ema_periods = null)
		{
			data = data ?? Data.Close;
			ema_periods = ema_periods ?? Bot.Settings.SlowEMAPeriods;
			var ema = Bot.Indicators.ExponentialMovingAverage(data, ema_periods.Value);
			var slope = ema.Result.FirstDerivative(idx);
			Debugging.Slope(idx, slope);
			return MeasureTrend(slope);
		}

		/// <summary>Compress candles over the given range [idx_min,idx_max) into a single equivalent candle</summary>
		public Candle Compress(NegIdx idx_min, NegIdx idx_max)
		{
			var candles = CandleRange(idx_min, idx_max).ToArray();
			if (!candles.Any())
				throw new Exception("Empty range, cannot create an equivalent candle");
			
			var timestamp = candles.First().Timestamp;
			var open      = candles.First().Open;
			var high      = candles.Max(x => x.High);
			var low       = candles.Min(x => x.Low);;
			var close     = candles.Last().Close;
			var medians   = candles.Select(x => x.Median).ToArray();
			var median    = medians.NthElement(medians.Length/2);
			var volume    = candles.Sum(x => x.Volume);
			
			return new Candle(timestamp, open, high, low, close, median, volume);
		}

		/// <summary>Types of candle patterns</summary>
		public enum ECandlePattern
		{
			Reversal1,
			Reversal2,
			Reversal3,
			Engulfing1,
			Engulfing2,
			Spike,
		}

		/// <summary>True if the candle pattern leading up to 'idx' signals a possible trade entry point.</summary>
		public ECandlePattern? IsCandlePattern(NegIdx idx)
		{
			Range range;
			NegIdx index;
			TradeType tt;
			QuoteCurrency? ep, tp;
			return IsCandlePattern(idx, out tt, out range, out index, out ep, out tp);
		}

		/// <summary>True if the candle pattern leading up to 'idx' signals a possible trade entry point.</summary>
		public ECandlePattern? IsCandlePattern(NegIdx idx, out TradeType tt, out Range range, out NegIdx index, out QuoteCurrency? ep, out QuoteCurrency? tp)
		{
			// Notes:
			//  This just indicates candle patterns, *not* trade entries.
			//  Trade entry should also consider general trend, SnR levels, etc

			// Require at least 10 candles
			if (idx - IdxFirst >= 10)
			{
				// 'A' is treated as the latest candle (potentially not closed).
				// Typically we're looking for 'B' being a price reversal, with 'A' being confirmation.
				// The function is intended to be called on every tick. If 'A' shows a strong trend direction
				// count it as 'closed' for the purposes of candle pattern detection.
				var mcs = MedianCS_50;
				var age = AgeOf(this[idx], clamped:true);
				var typ = this[idx].Type(mcs);

				// If the current candle is not old enough, or does not indicate a clear trend, ignore it.
				if (NewCandle || age < 0.9f || !typ.IsTrend())
					--idx;

				// Get the last few candles.
				var last = idx+1;
				var a_idx = idx-0; var A = this[a_idx]; var a_type = A.Type(mcs);
				var b_idx = idx-1; var B = this[b_idx]; var b_type = B.Type(mcs);
				var c_idx = idx-2; var C = this[c_idx]; var c_type = C.Type(mcs);
				var d_idx = idx-3; var D = this[d_idx]; var d_type = D.Type(mcs);
				var AB = Compress(b_idx,last); var ab_type = AB.Type(mcs);
				var AC = Compress(c_idx,last); var ac_type = AC.Type(mcs);

				// Get SnR data
				var snr = new SnR(this, idx, A.Close);

				// The minimum number of candles to use to determine trend
				const int MeasureTrendCount = 3;

				// Strong trend, Indecision, followed by strong reverse trend
				#region Reversal
				{
					// Determine if the candle at 'indecision_idx' is a reversal pattern
					Func<NegIdx,bool> IsReversal = (indecision_idx) =>
					{
						// A hammer, spinning top, or doji
						var indecision_candle = this[indecision_idx];
						if (!indecision_candle.Type(mcs).IsIndecision())
							return false;

						// Create the trend candle by compressing from 'indecision_idx' to 'last'
						var trend_candle = Compress(indecision_idx, last);
						if (!CandleRange(indecision_idx, last).AllSame(x => x.Sign))
							return false;

						// The trend candle is in the opposite direction to the preceding trend and the preceding candle trend is significant.
						var preceding_trend = MeasureTrend(indecision_idx - MeasureTrendCount, indecision_idx);
						if (trend_candle.Sign == Math.Sign(preceding_trend) || Math.Abs(preceding_trend) < 0.5)
							return false;
				
						//var underlying_trend = MeasureTrend(indecision_idx - 1);
						// Or, the underlying trend is in the same direction as A and is significant
						//||(A.Sign == Math.Sign(underlying_trend) && Math.Abs(underlying_trend) > 0.5);

						// Require the trend candle to not be weakening
						if (trend_candle.Weakening)
							return false;

						// 'trend_candle' is a trend sign or 'trend_candle' is a strong candle that has grown significantly beyond the wick of 'indecision_candle'
						var trend_candle_type = trend_candle.Type(mcs);
						if (trend_candle_type != Candle.EType.Marubozu &&
							trend_candle_type != Candle.EType.MarubozuStrengthening &&
							(
								trend_candle.Strength < 0.5 ||
								Math.Abs(trend_candle.Close - indecision_candle.BodyCentre) < 1.2*Math.Abs(indecision_candle.WickLimit(trend_candle.Sign) - indecision_candle.BodyCentre)
							))
							return false;

						// The indecision is near a strong SnR level
						var lvl = snr.Nearest(indecision_candle.Close, sign:0, radius:0.5*mcs);
						if (lvl == null)
							return false;

						return true;
					};

					if (IsReversal(b_idx))
					{
						tt = CAlgo.SignToTradeType(A.Sign);
						range = new Range((int)b_idx, (int)last);
						index = b_idx;
						ep = A.Open + A.Sign * 0.10 * A.BodyLength;
						tp = null;
						return ECandlePattern.Reversal1;
					}
					if (IsReversal(c_idx))
					{
						tt = CAlgo.SignToTradeType(A.Sign);
						range = new Range((int)c_idx, (int)last);
						index = c_idx;
						ep = AB.Open + AB.Sign * 0.10 * AB.BodyLength;
						tp = null;
						return ECandlePattern.Reversal2;
					}
					if (IsReversal(d_idx))
					{
						tt = CAlgo.SignToTradeType(A.Sign);
						range = new Range((int)d_idx, (int)last);
						index = d_idx;
						ep = AC.Open + AC.Sign * 0.10 * AC.BodyLength;
						tp = null;
						return ECandlePattern.Reversal3;
					}
				}
				#endregion

				// Engulfing pattern
				#region Engulfing
				{
					Func<NegIdx,bool> IsEngulfing = (engulfed_idx) =>
					{
						var engulfed_candle = this[engulfed_idx];

						// Create the trend candle by compressing from 'engulf_idx' to 'last'
						var trend_candle = Compress(engulfed_idx+1, last);
						if (!CandleRange(engulfed_idx+1, last).AllSame(x => x.Sign))
							return false;

						// 'trend_candle' must have the opposite direction to 'engulfed_candle'
						if (engulfed_candle.Sign == trend_candle.Sign)
							return false;

						// Both candles need to be a reasonable size
						if (engulfed_candle.Type(mcs).IsIndecision() || trend_candle.Strength < 0.7 || trend_candle.BodyLength < 0.5*mcs)
							return false;

						// 'trend_candle' is bigger than 'engulfed_candle'
						const double EngulfingRatio = 1.2;
						if (trend_candle.BodyLength < EngulfingRatio * engulfed_candle.BodyLength)
							return false;

						// 'trend_candle' is not itself engulfed by any of the previous few candles
						if (CandleRange(engulfed_idx-3, engulfed_idx+1).Any(c => c.BodyLength > EngulfingRatio * trend_candle.BodyLength))
							return false;

						// 'trend_candle.Open' must be fairly close to 'engulfed_candle.Close'
						if (Math.Abs(trend_candle.Open - engulfed_candle.Close) > 0.1 * mcs)
							return false;

						// 'trend_candle' is in the opposite direction to the preceding trend and the preceding trend is strong
						var preceding_trend = MeasureTrend(engulfed_idx - MeasureTrendCount, engulfed_idx+1);
						if (trend_candle.Sign == Math.Sign(preceding_trend) || Math.Abs(preceding_trend) < 0.5)
							return false;

						// Or, the underlying trend is in the same direction as AB and is significant
						//var underlying_trend = MeasureTrend(b_idx);
						//||(AB.Sign == Math.Sign(underlying_trend) && Math.Abs(underlying_trend) > 0.5);

						return true;
					};

					if (IsEngulfing(b_idx))
					{
						tt = CAlgo.SignToTradeType(A.Sign);
						range = new Range((int)b_idx, (int)last);
						index = a_idx;
						ep = A.Open + A.Sign * 0.8 * A.BodyLength;
						tp = null;
						return ECandlePattern.Engulfing1;
					}
					if (IsEngulfing(c_idx))
					{
						tt = CAlgo.SignToTradeType(A.Sign);
						range = new Range((int)c_idx, (int)last);
						index = b_idx;
						ep = AB.Open + AB.Sign * 0.8 * AB.BodyLength;
						tp = null;
						return ECandlePattern.Engulfing2;
					}
				}
				#endregion

				// Large price spike
				#region Price Spike
				{
					Func<NegIdx,bool> IsSpike = (spike_idx) =>
					{
						var spike_candle = this[spike_idx];

						// 'spike_candle' is a huge spike
						if (spike_candle.TotalLength < 6 * mcs)
							return false;

						// Create the candle after the spike
						var trend_candle = Compress(spike_idx+1, last);
						if (!CandleRange(spike_idx+1, last).AllSame(x => x.Sign))
							return false;

						// 'trend_candle' has not exceeded the spike of 'spike_candle'
						if ((spike_candle.Sign > 0 && trend_candle.High > spike_candle.High) ||
							(spike_candle.Sign < 0 && trend_candle.Low  < spike_candle.Low))
							return false;

						// The current price is still near the limit of the spike
						if ((spike_candle.Sign > 0 && trend_candle.Close < spike_candle.High - 0.33 * spike_candle.TotalLength) ||
							(spike_candle.Sign < 0 && trend_candle.Close > spike_candle.Low  + 0.33 * spike_candle.TotalLength))
							return false;

						return true;
					};

					if (IsSpike(b_idx))
					{
						tt = CAlgo.SignToTradeType(-B.Sign);
						range = new Range((int)c_idx, (int)last);
						index = a_idx;
						ep = A.Close;
						tp = B.Open + B.Sign * 0.5 * B.BodyLength;
						return ECandlePattern.Spike;
					}
				}
				#endregion
			}

			// No pattern found
			tt = TradeType.Buy;
			range = new Range((int)idx, (int)idx);
			index = idx;
			ep = null;
			tp = null;
			return null;
		}

		/// <summary>Returns a trade type when a likely good trade is identified. Or null</summary>
		public TradeType? FindTradeEntry(out QuoteCurrency? ep, out QuoteCurrency? tp, NegIdx? idx_ = null)
		{
			// Where in the instrument to look
			var idx = idx_ ?? 0;

			// Look for a candle pattern to start the entry point consideration
			Range range;
			NegIdx pattern_start_index;
			TradeType trade_type;
			var pattern = IsCandlePattern(idx, out trade_type, out range, out pattern_start_index, out ep, out tp);
			if (pattern == null)
				return null;

			// Compare to the cached last try. If it's the same candle range tested, not including the latest candle, return the previous result
			if (LastTradeEntryRange.Beg == range.Beg - (long)IdxFirst &&
				LastTradeEntryRange.End == range.End - (long)IdxFirst &&
				range.End != 1)
				return LastTradeEntryResult;

			// Update cached result
			LastTradeEntryRange = new Range(range.Beg - (long)IdxFirst, range.End - (long)IdxFirst);
			LastTradeEntryResult = null;

			// Show the instrument
			Debugging.LogInstrument();
			Debugging.CandlePattern(range.Begi,range.Endi);
			Debugging.Trace("\nCandle Pattern: --- {0} --- ({1})".Fmt(trade_type, pattern.Value));

			var candle = this[idx];
			var vote = 0.0;

			// Check the price peaks
			#region Price Peak
			{
				var pp = new PricePeaks(this, 0);
				var pp_trend = pp.Trend;
				Debugging.Dump(pp);

				// Vote based on agreement with trend direction
				var v = trade_type.Sign() * pp_trend;
				Debugging.Trace("PP Trend (vote = {0})".Fmt(v));
				vote += v;

				//// Don't trade against strong price peak trends
				//if (Math.Sign(pp_trend) != trade_type.Sign() && Math.Abs(pp_trend) > 0.75)
				//{
				//	Debugging.Trace("Veto - Opposing PP trend (trend = {0})".Fmt(pp_trend));
				//	return null;
				//}
				//// Otherwise, vote for the trade based on agreement with trend
				//else
				//{
				//}
			}
			#endregion

			// Check the slow EMA
			#region Slow EMA
			{
				// Get the slow EMA and ensure we're not trading against the trend
				var trend = MeasureTrend(idx);
				var ema_trend = Maths.Sigmoid(trend, 0.9); // lower the importance of EMA trend

				// Vote based on agreement with trend direction
				var v = trade_type.Sign() * ema_trend;
				Debugging.Trace("EMA Trend (vote = {0})".Fmt(v));
				vote += v;

				// If the trend is strong and opposed to the trade direction, ignore
				//if (Math.Sign(ema_trend) != trade_type.Sign() && Math.Abs(ema_trend) > 0.5)
				//{
				//	Debugging.Trace("Opposing EMA trend (trend = {0})".Fmt(ema_trend));
				//}
			}
			#endregion

			// Check RSI
			#region RSI
			{
				// Check for really over bought or over sold
				var rsi = Bot.Indicators.RelativeStrengthIndex(Data.Close, 14);
				var strength = Maths.Sigmoid(rsi.Result.LastValue - 50, 25);
				var v = trade_type.Sign() * strength;
				Debugging.Trace("RSI strength (vote = {0})".Fmt(v));
				vote += v;

				//if (trade_type == TradeType.Buy)
				//{
				//	if (rsi.Result.LastValue < 25)
				//		Debugging.Trace("Really over sold however (rsi = {0})".Fmt(rsi.Result.LastValue));
				//	else
				//		return null;
				//}				
				//if (trade_type == TradeType.Sell)
				//{
				//	if (rsi.Result.LastValue > 75)
				//		Debugging.Trace("Really over bought however (rsi = {0})".Fmt(rsi.Result.LastValue));
				//	else
				//		return null;
				//}
			}
			#endregion

			// Count the votes
			Debugging.Trace("Final vote for {0}: {1}".Fmt(trade_type, vote));
			const double VoteThreshold = 0.1;
			LastTradeEntryResult = (vote >= VoteThreshold) ? trade_type : (TradeType?)null;
			return LastTradeEntryResult;
		}

		/// <summary>The CAlgo index range last used to test for a trade entry</summary>
		public Range LastTradeEntryRange { get; private set; }

		/// <summary>The result of the last test for trade entry</summary>
		public TradeType? LastTradeEntryResult { get; set; }
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
