using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cAlgo.API;
using cAlgo.API.Indicators;
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
		}
		public void Dispose()
		{
			Bot = null;
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

		/// <summary>Raised whenever candles are added/modified in this instrument</summary>
		public event EventHandler<DataEventArgs> DataChanged;
		private void OnDataChanged(DataEventArgs args)
		{
			DataChanged.Raise(this, args);
		}
		private void HandleTick(object sender, EventArgs e)
		{
			// Get the latest candle
			var candle = this[0];
			var now = Bot.UtcNow;

			// Check if this is the start of a new candle
			NewCandle = candle.Timestamp > Latest.Timestamp;

			// Get the fractional CAlgo index for this tick
			var index = IndexAt(now) - IdxFirst;
			var price = new PriceTick(index, now.Ticks, Symbol.Ask, Symbol.Bid);
			Debug.Assert(HighRes.Count == 0 || index > HighRes.Back().Index);

			// Capture the high res price data
			HighRes.Add(price);

			// Apply the data to the latest candle or invalidate the cached Latest
			if (NewCandle)
				Latest = null;
			else
				Latest.Update(candle);

			// Record the last time data was received
			m_last_tick_time = now.Ticks;

			// Invalidate cached, per-candle, properties
			m_mcs = null;

			// Notify data added/changed
			OnDataChanged(new DataEventArgs(this, candle, NewCandle));
		}

		/// <summary>The instrument data source</summary>
		public MarketSeries Data
		{
			get;
			private set;
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

		/// <summary>The instrument time-frame</summary>
		public TimeFrame TimeFrame
		{
			get { return Data.TimeFrame; }
		}

		/// <summary>The high res data for the instrument.</summary>
		public HighResCollection HighRes
		{
			get;
			private set;
		}
		public class HighResCollection :List<PriceTick> ,DataSeries
		{
			private readonly Instrument m_instr;
			public HighResCollection(Instrument instr, int max_length)
			{
				m_instr = instr;
				MaxLength = max_length;
			}

			/// <summary>The nearest price tick value after 'idx'</summary>
			public PriceTick this[Idx idx]
			{
				get
				{
					var i = idx - m_instr.IdxFirst;
					var index = this.BinarySearch(x => x.Index.CompareTo(i), find_insert_position:true);
					index = Maths.Clamp(index, 0, Count-1);

					// If we have high res data at 'index' return it
					if (index != 0)
						return this[index];

					// Otherwise, fall back to candle data, using the current spread
					var candle = m_instr[idx];
					return new PriceTick(i, candle.Timestamp, candle.Close + m_instr.Spread, candle.Close);
				}
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

			/// <summary>
			/// Iterate through the high res price data over the given range with a step size of 'step'.
			/// 'first' and 'last' floating point Idx's (not indices in HighRes)
			/// 'step' is the increment size in indices to move with each returned value</summary>
			public IEnumerable<PriceTick> Range(Idx min, Idx max, double? step = null)
			{
				var first = min - m_instr.IdxFirst;
				var last  = max - m_instr.IdxFirst;

				var istart = this.BinarySearch(x => x.Index.CompareTo(first), find_insert_position:true);
				var iend   = this.BinarySearch(x => x.Index.CompareTo(last), find_insert_position:true);
				if (istart == iend)
					yield break;

				// The point to return
				var pt = this[istart];

				// Loop over the requested range
				var X = this[istart].Index;
				for (var i = istart; i < iend;)
				{
					yield return pt;

					// Get the next point
					if (step == null)
					{
						if (++i < iend)
							pt = this[i];
					}
					// Scan forward to the next price point to output
					else
					{
						X += step.Value;

						// Group all ticks over the step range.
						// If the step is smaller than one tick, advance X by at least 1 each time
						var cnt = 0;
						pt = PriceTick.Invalid;
						for (++i; i < iend; ++i)
						{
							pt.Index += this[i].Index;
							pt.Ask = Math.Max(pt.Ask, this[i].Ask);
							pt.Bid = Math.Min(pt.Bid, this[i].Bid);
							++cnt;

							if (this[i].Index > X)
							{
								pt.Index /= cnt;
								X = this[i].Index;
								break;
							}
						}
					}
				}
			}
			public IEnumerable<PriceTick> Range(RangeF range, double? step = null)
			{
				return Range(range.Beg, range.End, step);
			}

			/// <summary>Create a candle that uses the high res data over the given 'Idx' range</summary>
			public Candle Candle(Idx min, Idx max)
			{
				var data = Range(min, max).ToArray();
				if (!data.Any())
					return new Candle();

				var index     = 0.5 * (data.Front().Index + data.Back().Index);
				var timestamp = data.Front().Timestamp;
				var open      = (double)data.Front().Bid;
				var high      = (double)data.Max(x => x.Bid);
				var low       = (double)data.Min(x => x.Bid);
				var close     = (double)data.Back().Bid;
				var medians   = data.Select(x => x.Bid).ToArray();
				var median    = (double)medians.NthElement(medians.Length/2);
				var volume    = data.Length;
			
				return new Candle(index, timestamp, open, high, low, close, median, volume);
			}

			/// <summary>The number of PriceTick's available in the given candle range</summary>
			public int TickCount(Idx min, Idx max)
			{
				var first = min - m_instr.IdxFirst;
				var last  = max - m_instr.IdxFirst;
				var istart = this.BinarySearch(x => x.Index.CompareTo(first), find_insert_position:true);
				var iend   = this.BinarySearch(x => x.Index.CompareTo(last), find_insert_position:true);
				return iend - istart;
			}

			/// <summary>Extrapolate the high res price data fitting to the last 'count' candles</summary>
			public Extrapolation Extrapolate(int count, Idx? idx_ = null)
			{
				var idx = idx_ ?? 0;
				var points = Range(idx + 1 - count, idx + 1).Select(x => new v2((float)x.Index + m_instr.IdxFirst, (float)x.Mid)).ToArray();
				if (points.Length < 3) return null;
				var curve = Quadratic.FromLinearRegression(points);
				var R = Math.Sqrt(points.Sum(p => Maths.Sqr(curve.F(p.x) - p.y)));
				var confidence = 1.0 - Maths.Sigmoid(R, m_instr.MCS);
				return new Extrapolation(curve, confidence);
			}

			#region DataSeries

			/// <summary>Gets the value in the data series at the specified position.</summary>
			double DataSeries.this[int index]
			{
				get { return this[index].Mid; }
			}

			/// <summary>Gets the total number of elements contained in the DataSeries.</summary>
			int DataSeries.Count
			{
				get { return Count; }
			}

			/// <summary>Gets the last value of this DataSeries.</summary>
			double DataSeries.LastValue
			{
				get { return Count != 0 ? (double)this.Back(0).Mid : 0.0; }
			}

			/// <summary>Access a value in the data series certain bars ago</summary>
			double DataSeries.Last(int index)
			{
				return Count != 0 ? (double)this.Back(index).Mid : 0.0;
			}

			#endregion
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
			[DebuggerStepThrough] get { return new PriceTick(0, Bot.UtcNow.Ticks, CurrentPrice(+1), CurrentPrice(-1)); }
		}

		/// <summary>The total number of data points available</summary>
		public int Count
		{
			[DebuggerStepThrough] get { return Data.OpenTime.Count; }
		}

		/// <summary>Index range (-Count, 0]</summary>
		public Idx IdxFirst
		{
			[DebuggerStepThrough] get { return 1 - Count; }
		}
		public Idx IdxLast
		{
			[DebuggerStepThrough] get { return +1; }
		}
		public Idx IdxNow
		{
			[DebuggerStepThrough] get { return IndexAt(Bot.UtcNow); }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public Candle this[Idx idx]
		{
			get
			{
				Debug.Assert(idx >= IdxFirst && idx < IdxLast);

				// CAlgo uses 0 = oldest, Count = latest
				var i = (int)(idx - IdxFirst);
				return new Candle(
					i,
					Data.OpenTime  [i].Ticks,
					Data.Open      [i],
					Data.High      [i],
					Data.Low       [i],
					Data.Close     [i],
					Data.Median    [i],
					Data.TickVolume[i]);
			}
		}

		/// <summary>The candle with the latest timestamp for the current time frame</summary>
		public Candle Latest
		{
			// Cache the latest candle so that we can detect a new candle starting.
			get
			{
				return m_latest ?? (m_latest = this[0]);
			}
			private set
			{
				m_latest = null;
			}
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

		/// <summary>The age of the current candle normalised to the time frame</summary>
		public double LatestAge
		{
			get { return AgeOf(Latest, false); }
		}

		/// <summary>The timestamp of the last time we received price data (in UTC)</summary>
		public DateTimeOffset LastTickUTC
		{
			[DebuggerStepThrough] get { return new DateTimeOffset(m_last_tick_time, TimeSpan.Zero); }
		}
		private long m_last_tick_time;

		/// <summary>Clamps the given index range to a valid range within the data. i.e. [-Count,0]</summary>
		public Range IndexRange()
		{
			return new Range((int)IdxFirst, (int)IdxLast);
		}
		public Range IndexRange(Idx min, Idx max)
		{
			Debug.Assert(min <= max);
			var mn = Maths.Clamp(min, IdxFirst, IdxLast);
			var mx = Maths.Clamp(max, min, IdxLast);
			return new Range((int)mn, (int)mx);
		}
		public Range IndexRange(RangeF range)
		{
			return IndexRange(range.Beg, range.End);
		}
		public Range IndexRange(Position pos)
		{
			return IndexRange(new Range((int)IndexAt(pos.EntryTime), (int)IdxLast));
		}
		public Range IndexRange(HistoricalTrade pos)
		{
			return IndexRange(new Range((int)IndexAt(pos.EntryTime), (int)IndexAt(pos.ClosingTime)));
		}

		/// <summary>Enumerate the candles within an index range [idx_max,idx_max) (i.e. time-frame units)</summary>
		public IEnumerable<Candle> CandleRange(Idx min, Idx max, bool reverse = false)
		{
			var checked_range = IndexRange(min, max);
			Debug.Assert(checked_range.Begi >= IdxFirst);
			Debug.Assert(checked_range.Endi <= IdxLast);
			if (!reverse)
			{
				for (var i = checked_range.Begi; i != checked_range.Endi; ++i)
					yield return this[i];
			}
			else
			{
				for (var i = checked_range.Endi; i-- != checked_range.Begi;)
					yield return this[i];
			}
		}
		public IEnumerable<Candle> CandleRange(RangeF range, bool reverse = false)
		{
			return CandleRange(range.Beg, range.End, reverse);
		}
		public IEnumerable<Candle> CandleRange(bool reverse = false)
		{
			return CandleRange(IdxFirst, IdxLast, reverse);
		}

		/// <summary>Return candles that have been compressed into groups of 'ts'</summary>
		public IEnumerable<Candle> BatchedCandleRange(Idx min, Idx max, TimeFrame tf, bool reverse = false)
		{
			Debug.Assert(tf >= TimeFrame);

			var checked_range = IndexRange(min, max);
			Debug.Assert(checked_range.Begi >= IdxFirst);
			Debug.Assert(checked_range.Endi <= IdxLast);
			if (checked_range.Count == 0)
				yield break;

			if (!reverse)
			{
				var t = (long)this[checked_range.Begi].TFUnits(tf);
				for (int e, s = checked_range.Begi; s != checked_range.Endi;)
				{
					// Find the range of candles that contribute to one time-frame unit
					for (e = s; e != checked_range.Endi && (long)this[e].TFUnits(tf) == t; ++e) {}

					// Output the candle
					if (e != s)
						yield return Compress(s, e);

					s = e;
					t++;
				}
			}
			else
			{
				var t = (long)this[checked_range.Endi-1].TFUnits(tf);
				for (int s, e = checked_range.Endi; e != checked_range.Begi;)
				{
					// Find the range of candles that contribute to one time-frame unit
					for (s = e; s != checked_range.Begi && (long)this[s-1].TFUnits(tf) == t; --s) {}

					// Output the candle
					if (s != e)
						yield return Compress(s, e);

					e = s;
					t--;
				}
			}
		}
		public IEnumerable<Candle> BatchedCandleRange(RangeF range, TimeFrame tf, bool reverse = false)
		{
			return BatchedCandleRange(range.Beg, range.End, tf, reverse);
		}
		public IEnumerable<Candle> BatchedCandleRange(TimeFrame tf, bool reverse = false)
		{
			return BatchedCandleRange(IdxFirst, IdxLast, tf, reverse);
		}

		/// <summary>Return the index into the candle data for the given time</summary>
		public Idx IndexAt(DateTimeOffset dt)
		{
			// Get the integral CAlgo index
			var idx = Data.OpenTime.GetIndexByTime(dt.UtcDateTime);

			// Find the sub-candle fractional part
			var open = Data.OpenTime[idx];
			var ticks = dt.Ticks - open.Ticks;
			var ticks_per_candle = TimeFrame.ToTicks();
			var frac = Maths.Clamp((double)ticks / ticks_per_candle, 0.0, 1.0);

			return idx + frac + IdxFirst;
		}

		/// <summary>Returns a time 'num_candles' in the future</summary>
		public DateTime ExpirationTime(int num_candles)
		{
			return (Bot.UtcNow + TimeFrame.ToTimeSpan(num_candles)).DateTime;
		}

		/// <summary>Return the max candle size (total length) of the given range</summary>
		public QuoteCurrency MaxCandleSize(Idx min, Idx max)
		{
			var range = IndexRange(min, max);
			if (range.Empty) throw new Exception("Empty range, max candle size is not defined");
			var max_cs = range.Max(x => TrueRange(x));
			return max_cs;
		}
		public QuoteCurrency MaxCandleSize(Range range)
		{
			return MaxCandleSize(range.Begi, range.Endi);
		}

		/// <summary>Return the median candle size (total length) of the given range</summary>
		public QuoteCurrency MedianCandleSize(Idx min, Idx max)
		{
			var lengths = IndexRange(min, max).Select(x => TrueRange(x)).ToArray();
			if (lengths.Length == 0) throw new Exception("Empty range, median candle size is not defined");
			var mcs = lengths.NthElement(lengths.Length/2);
			return mcs;
		}
		public QuoteCurrency MedianCandleSize(Range range)
		{
			return MedianCandleSize(range.Begi, range.Endi);
		}

		/// <summary>A cached median candle size over the last 50 candles</summary>
		public QuoteCurrency MCS
		{
			get { return (m_mcs ?? (m_mcs = MedianCandleSize(IdxLast - 50, IdxLast))).Value; }
		}
		private QuoteCurrency? m_mcs;

		/// <summary>Return the number of ticks in the given range of candles</summary>
		public long TradeVolume(Idx min, Idx max)
		{
			return (long)CandleRange(min, max).Sum(x => x.Volume);
		}

		/// <summary>Return the price range on the interval [min, max)</summary>
		public RangeF PriceRange(Idx min, Idx max)
		{
			var range = RangeF.Invalid;
			foreach (var c in CandleRange(min, max))
			{
				range.Encompass(c.High);
				range.Encompass(c.Low);
			}
			return range;
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
		public QuoteCurrency TrueRange(Idx index)
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

		/// <summary>Return the average true range for candles in [min,max)</summary>
		public QuoteCurrency SMATrueRange(Idx min, Idx max)
		{
			var avr = new Avr();
			for (var i = min; i != max; ++i)
				avr.Add(TrueRange(i));
			return avr.Mean;
		}

		/// <summary>The exponential moving average of the true range for candles in [min, max)</summary>
		public QuoteCurrency EMATrueRange(Idx min, Idx max)
		{
			var avr = new ExpMovingAvr();
			for (var i = min; i != max; ++i)
				avr.Add(TrueRange(i));
			return avr.Mean;
		}

		/// <summary>Compress candles over the given range [idx_min,idx_max) into a single equivalent candle</summary>
		public Candle Compress(Idx idx_min, Idx idx_max)
		{
			var candles = CandleRange(idx_min, idx_max).ToArray();
			if (!candles.Any())
				throw new Exception("Empty range, cannot create an equivalent candle");

			var index     = candles.Front().Index;
			var width     = candles.Back().Index - candles.Front().Index + candles.Back().Width;
			var timestamp = candles.Front().Timestamp;
			var open      = candles.Front().Open;
			var high      = candles.Max(x => x.High);
			var low       = candles.Min(x => x.Low);;
			var close     = candles.Last().Close;
			var medians   = candles.Select(x => x.Median).ToArray();
			var median    = medians.NthElement(medians.Length/2);
			var volume    = candles.Sum(x => x.Volume);
			
			return new Candle(index, timestamp, open, high, low, close, median, volume, width:width);
		}

		/// <summary>Compress the candle at 'idx' with former candles of the same sign</summary>
		public Candle Compress(Idx idx)
		{
			var mcs = MCS;
			var sign = 0;
			Idx i = idx;
			for (; i-- != IdxFirst;)
			{
				var candle = this[i];
				if (candle.Type(mcs).IsIndecision()) continue;
				//if (candle.BodyLength < 4*PipSize) continue;
				if (sign == 0) sign = candle.Sign;
				if (sign == candle.Sign) continue;
				break;
			}
			return Compress(i+1, idx+1);
		}

		/// <summary>
		/// Measures the trend over candles [idx_min, idx_max).
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)
		/// Treat ­0.5 as the limit between trend and no trend.</summary>
		public double MeasureTrendFromCandles(Idx idx_min, Idx idx_max)
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
		public double MeasureTrendFromSlope(double slope)
		{
			// If the slope is greater/less than MCS / N candles classify as a trend
			var run = 20.0;
			var rise = (double)MCS;
			var threshold = rise / run;
			var trend = Maths.Sigmoid(slope, threshold);
			return trend;
		}

		/// <summary>
		/// Classify the trend at the given index using the slope of the slow EMA.
		/// Returns a value on the range [-1,+1] (negative = bearish, positive = bullish)
		/// Scales the range [-inf,+inf] to [-1,+1]. Treat ­0.5 as the limit between trend and no trend.</summary>
		public double MeasureTrend(Idx idx, DataSeries data = null, int? ema_periods = null)
		{
			data = data ?? Data.Close;
			ema_periods = ema_periods ?? Bot.Settings.SlowEMAPeriods;
			var ema = Bot.Indicators.ExponentialMovingAverage(data, ema_periods.Value);
			var slope = ema.Result.FirstDerivative(idx);
			Bot.Debugging.Slope(idx, slope);
			return MeasureTrendFromSlope(slope);
		}

		/// <summary>Classify the trend of the high res data over the last N ticks</summary>
		public double MeasureTrendHighRes(int ticks, Idx? idx = null)
		{
			// Find the index in the high res data to start the search from
			idx = idx ?? IdxNow;
			var calgo_index = idx.Value - IdxFirst;
			var iend = HighRes.BinarySearch(x => x.Index.CompareTo(calgo_index), find_insert_position:true);
			var istart = Math.Max(0, iend - ticks);

			// Do a linear regression on the price ticks
			var corr = new Correlation();
			for (int i = istart; i != iend; ++i)
				corr.Add(HighRes[i].Index, HighRes[i].Mid);

			// Convert the slope to a trend value
			return MeasureTrendFromSlope(corr.LinearRegression.A);
		}

		/// <summary>Return the average slope of all the EMAs over 'period_range'</summary>
		public double EMASlope(Idx index, Range? period_range = null)
		{
			period_range = period_range ?? new Range(1, 101);

			// Integrate the area under the graph of slope vs. EMA period.
			var sum = 0.0;
			foreach (var r in period_range.Value)
			{
				var ema = Bot.Indicators.ExponentialMovingAverage(Data.Close, (int)r);
				var slope = ema.Result.FirstDerivative(index);
				sum += slope;
			}
			return sum / period_range.Value.Size;
		}

		/// <summary>Return the combined EMA slope interpreted as a trend in the range [-1,+1]</summary>
		public double EMATrend(Idx index, Range? period_range = null)
		{
			return MeasureTrendFromSlope(EMASlope(index, period_range));
		}

		/// <summary>Compare a candle to a price value, returning the ratio above or below the value. Returns positive for 'candle' above 'price'</summary>
		public double Compare(Candle candle, QuoteCurrency price, bool wicks)
		{
			var hi = wicks ? candle.High : candle.BodyLimit(+1);
			var lo = wicks ? candle.Low  : candle.BodyLimit(-1);
			var above = Math.Max(0, hi - price);
			var below = Math.Max(0, price - lo);
			if (above == 0) return -1.0;
			if (below == 0) return +1.0;
			return (above - below) / (above + below);
		}

		/// <summary>Compare a candle to a trend line, returning the ratio above or below the line. Returns positive for 'candle' above 'trend'</summary>
		public double Compare(Candle candle, Monic trend, bool wicks)
		{
			return Compare(candle, trend.F(candle.Index + IdxFirst), wicks);
		}

		/// <summary>Compare a candle to a moving average, returning the ratio above or below the line. Returns positive for 'candle' above 'ma'</summary>
		public double Compare(Candle candle, MovingAverage ma, bool wicks)
		{
			return Compare(candle, ma.Result[(int)candle.Index], wicks);
		}

		/// <summary>Return an appropriate stop loss, take profit, and volume for a trade at the given candle an entry price</summary>
		public TradeExit ChooseTradeExit(TradeType tt, QuoteCurrency ep, Idx? idx = null, double? risk = null, int? look_back = null, double? rtr = null)
		{
			var exit = new TradeExit { EP = ep, SL = ep, TP = ep, Volume = Symbol.VolumeMin };
			var sign = tt.Sign();
			idx = idx ?? 0;

			// Get the support and resistance levels
			var snr = new SnR(this, ep, idx+1);
			Bot.Debugging.Dump(snr);

			#region SL

			// Start with a minimum of the MCS
			var sl_rel = MCS;

			{
				// Scan backwards looking for a peak in the stop loss direction.
				look_back = look_back ?? Bot.Settings.LookBackCount;
				foreach (var candle in CandleRange(idx.Value - look_back.Value, idx.Value))
				{
					var limit = candle.WickLimit(-sign);
					var diff = ep - limit;
					if (Math.Sign(diff) != sign) continue;
					if (Math.Abs(diff) < sl_rel) continue;
					sl_rel = Math.Abs(diff);
				}

				// Count the number of SnR levels between 'ep' and 'sl'.
				// If there are two or more strong levels, reduce the SL to just beyond the 2nd level
				var lvls = snr.SnRLevels
					.Where(x => x.Strength > 0.5)                // strong
					.Where(x => Math.Sign(ep - x.Price) == sign) // on the SL side
					.Where(x => Math.Abs(ep - x.Price) < sl_rel) // between 'ep' and 'sl'
					.OrderBy(x => x.Price)                       // sort by increasing price
					.ToArray();
				if (lvls.Length >= 2)
				{
					sl_rel = sign > 0
						? Math.Abs(ep - lvls[lvls.Length-2].Price)
						: Math.Abs(ep - lvls[1].Price);
				}

				//// 
				//// In the case of strong trends this SL loss is too large, limit it to just past a strong SnR level
				//var trend = MeasureTrend(idx);
				//if (Math.Abs(trend) > 0.5)
				//{
				//	foreach (var lvl in snr.SnRLevels.Where(x => x.Strength > 0.5f))
				//	{
				//		var diff = (ep - lvl.Price) * 1.5f;
				//		if (Misc.Sign(diff) != sign) continue;
				//		if (Misc.Abs(diff) > sl_rel) continue;
				//		sl_rel = Misc.Abs(diff);
				//	}
				//}

				// For short trades, add the spread to the SL
				sl_rel += (sign > 0 ? 0 : Spread);

				// Add on a bit as a safety buffer
				sl_rel *= 1.1f;

				// Adjust the volume so that the risk is within the acceptable range.
				// If the risk is too high reduce the volume first, down to the VolumeMin
				// then reduce 'peak'. If the risk is low, increase volume to fit within 'risk'.
				risk = risk ?? 1.0;
				var balance_to_risk = Bot.Broker.BalanceToRisk * risk.Value;

				// Find the account value risked at the current stop loss
				var sl_acct = Symbol.QuoteToAcct(sl_rel);
				var optimal_volume = balance_to_risk / sl_acct;

				// If the risk is too high, reduce the stop loss
				if (optimal_volume < Symbol.VolumeMin)
				{
					exit.Volume = Symbol.VolumeMin;
					sl_rel = Symbol.AcctToQuote(balance_to_risk / exit.Volume);
				}
				// Otherwise, round down to the nearest volume multiple
				else
				{
					exit.Volume = Symbol.NormalizeVolume(optimal_volume, RoundingMode.Down);
				}

				// Find the absolute price for the stop loss level
				exit.SL = ep - sign * sl_rel;
			}
			#endregion
			#region TP
			// Start with a minimum of the MCS
			var tp_rel = MCS;

			{
				// Look for a level on the profit side of the entry price
				// If there are no levels, use a few typical candle sizes
				var nearest = snr.Nearest(ep + sign * tp_rel, sign);
				if (nearest != null)
				{
					tp_rel = Math.Abs(nearest.Price - ep);
				}
				else
				{
					tp_rel = 2 * MCS;
				}

				// Apply the minimum reward to risk ratio
				if (rtr != null)
				{
					tp_rel = Math.Max(tp_rel, sl_rel * rtr.Value);
				}

				// Find the absolute price for the take profit level
				exit.TP = ep + sign * tp_rel;
			}
			#endregion

			return exit;
		}

		/// <summary>Trade exit data</summary>
		[DebuggerDisplay("ep={EP} sl={SL} tp={TP}")]
		public struct TradeExit
		{
			public QuoteCurrency EP;
			public QuoteCurrency SL;
			public QuoteCurrency TP;
			public long Volume;
		}

		/// <summary>Categorise the market state over the range [min,max)</summary>
		public EMarketState MarketState(Idx min, Idx max)
		{
			throw new NotImplementedException();
			//return EMarketState.Unknown;
		}

		/// <summary>True if the candle pattern leading up to 'idx' signals a possible trade entry point.</summary>
		public ECandlePattern? IsCandlePattern(Idx? idx = null)
		{
			CandlePattern cp;
			return IsCandlePattern(out cp, idx);
		}

		/// <summary>True if the candle pattern leading up to 'idx' signals a possible trade entry point.</summary>
		public ECandlePattern? IsCandlePattern(out CandlePattern pattern, Idx? idx_ = null)
		{
			// Notes:
			// - This just indicates candle patterns, *not* trade entries.
			// - Trade entry should also consider general trend, SnR levels, etc
			// - 'A' is treated as the latest candle (potentially not closed).
			// Typically we're looking for 'B' being a price reversal, with 'A' being confirmation.
			// The function is intended to be called on every tick. If 'A' shows a strong trend direction
			// count it as 'closed' for the purposes of candle pattern detection.
			var idx = idx_ ?? 0;
			var mcs = MCS;
			var age = AgeOf(this[idx], clamped:true);
			var typ = this[idx].Type(mcs);

			// Require at least 10 candles
			if (idx - IdxFirst < 10)
			{
				pattern = null;
				return null;
			}

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
			var snr = new SnR(this, A.Close, last);

			// The minimum number of candles to use to determine trend
			const int MeasureTrendCount = 3;

			// Check for all candle patterns and return the most likely
			var patterns = new List<CandlePattern>();

			// Strong trend, Indecision, followed by strong reverse trend
			#region Reversal
			{
				// Determine if the candle at 'indecision_idx' is a reversal pattern
				Func<Idx,bool> IsReversal = (indecision_idx) =>
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
					var preceding_trend = MeasureTrendFromCandles(indecision_idx - MeasureTrendCount, indecision_idx);
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
					patterns.Add(new CandlePattern(
						ECandlePattern.Reversal1,
						CAlgo.SignToTradeType(A.Sign),
						new RangeF(b_idx, last),
						b_idx,
						A.Open + A.Sign * 0.10 * A.BodyLength));
				}
				if (IsReversal(c_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Reversal2,
						CAlgo.SignToTradeType(A.Sign),
						new RangeF(c_idx, last),
						c_idx,
						AB.Open + AB.Sign * 0.10 * AB.BodyLength));
				}
				if (IsReversal(d_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Reversal3,
						CAlgo.SignToTradeType(A.Sign),
						new RangeF(d_idx, last),
						d_idx,
						AC.Open + AC.Sign * 0.10 * AC.BodyLength));
				}
			}
			#endregion

			// Forex4Noobs trigger
			#region Forex4Noobs
			{
				var trade_type = (TradeType?)null;
				Func<Idx,bool> IsStall = (indecision_idx) =>
				{
					// Indecision
					var indecision_candle = this[indecision_idx];
					if (!indecision_candle.Type(mcs).IsIndecision())
						return false;

					// Create the trend candle by compressing from 'indecision_idx' to 'last'
					var trend_candle = Compress(indecision_idx, last);
					if (!CandleRange(indecision_idx, last).AllSame(x => x.Sign))
						return false;

					// There is a significant preceding trend
					var preceding_trend = MeasureTrendFromCandles(indecision_idx - MeasureTrendCount, indecision_idx);
					if (Math.Abs(preceding_trend) < 0.5)
						return false;

					// The trend candle has the opposite direction to the preceding trend
					if (!trend_candle.Type(mcs).IsIndecision() && trend_candle.Sign == Math.Sign(preceding_trend))
						return false;

					// The stall has occurred on a significant SnR level
					var lvl = snr.Nearest(indecision_candle.Close, 0, radius:0.5*mcs);
					if (lvl == null)
						return false;

					trade_type = CAlgo.SignToTradeType(-Math.Sign(preceding_trend));
					return true;
				};

				if (IsStall(b_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Stall1,
						trade_type.Value,
						new RangeF(c_idx, last),
						b_idx,
						ep:B.Median));
				}
				if (IsStall(c_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Stall2,
						trade_type.Value,
						new RangeF(d_idx, last),
						c_idx,
						ep:C.Median));
				}
			}
			#endregion

			// Engulfing pattern
			#region Engulfing
			{
				Func<Idx,bool> IsEngulfing = (engulfed_idx) =>
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
					var preceding_trend = MeasureTrendFromCandles(engulfed_idx - MeasureTrendCount, engulfed_idx+1);
					if (trend_candle.Sign == Math.Sign(preceding_trend) || Math.Abs(preceding_trend) < 0.5)
						return false;

					// Or, the underlying trend is in the same direction as AB and is significant
					//var underlying_trend = MeasureTrend(b_idx);
					//||(AB.Sign == Math.Sign(underlying_trend) && Math.Abs(underlying_trend) > 0.5);

					return true;
				};

				if (IsEngulfing(b_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Engulfing1,
						CAlgo.SignToTradeType(A.Sign),
						new RangeF(b_idx, last),
						a_idx,
						A.Open + A.Sign * 0.8 * A.BodyLength));
				}
				if (IsEngulfing(c_idx))
				{
					patterns.Add(new CandlePattern(
						ECandlePattern.Engulfing2,
						CAlgo.SignToTradeType(A.Sign),
						new RangeF(c_idx, last),
						b_idx,
						AB.Open + AB.Sign * 0.8 * AB.BodyLength));
				}
			}
			#endregion

			// Large price spike
			#region Price Spike
			{
				Func<Idx,bool> IsSpike = (spike_idx) =>
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
					patterns.Add(new CandlePattern(
						ECandlePattern.Spike,
						CAlgo.SignToTradeType(-B.Sign),
						new RangeF(c_idx, last),
						a_idx,
						A.Close));
				}
			}
			#endregion

			patterns.Sort((l,r) => l.Pattern.CompareTo(r.Pattern));

			// Return the most likely pattern
			pattern = patterns.FirstOrDefault();
			return pattern != null ? pattern.Pattern : (ECandlePattern?)null;
		}

		/// <summary>Look for patterns in the price peaks</summary>
		public EPeakPattern? IsPeakPattern(out TradeType tt, out QuoteCurrency ep, Idx? idx_ = null)
		{
			var idx = idx_ ?? 0;
			var pp = new PricePeaks(this, idx+1);

			// If a break out is detected, create a trade now
			if (pp.IsBreakOutHigh)
			{
				tt = TradeType.Buy;
				ep = LatestPrice.Ask;
				return EPeakPattern.BreakOutHigh;
			}
			if (pp.IsBreakOutLow)
			{
				tt = TradeType.Sell;
				ep = LatestPrice.Bid;
				return EPeakPattern.BreakOutLow;
			}

			// If a significant trend is detected, look for a reversal at the trend line
			if (pp.Strength > 0.5)
			{
				Func<Idx,bool,bool> IsReversal = (rev_idx,high) =>
				{
					var A = HighRes.Candle(rev_idx-1.0, rev_idx);
					var B = this[rev_idx-1];
					var sign = high ? +1 : -1;

					// No trend line? no reversal
					var trend = high ? pp.TrendHigh : pp.TrendLow;
					if (trend == null)
						return false;

					// Don't trade against the trend
					if (pp.Sign != 0 && pp.Sign == sign)
						return false;

					// Look for candle patterns that indicate a reversal at the trend line
					var trend_price = trend.F(rev_idx);

					// Look for B.Close within 0.5*MCS of the trend line (on the non-break-out side) or B.Wick within 0.1 * MCS of trend line
					var distB = sign * (trend_price - B.Close);
					if (!distB.Within(0, 0.5*MCS) &&
						Math.Abs(trend_price - B.WickLimit(sign)) >= 0.1*MCS)
						return false;

					// The entry price must be near the trend line (on the non-break-out side)
					var distA = sign * (trend_price - (A.Close + (sign<0?Spread:0)));
					if (!distA.Within(0, 1.0*MCS))
						return false;

					// 'A' should not be an indecision candle, we want to see price reversing
					if (A.Type(MCS).IsIndecision() || A.Sign == sign)
						return false;

					// 'A' must be mostly on the non-break-out side of the trend line
					var ratio = Compare(A, trend, false);
					if (Math.Abs(ratio) < 0.75 || Math.Sign(ratio) == sign)
						return false;

					// If any part of A or B is more than 0.5*MCS on the break-out side of the trend line, then abort
					if (sign * (A.WickLimit(sign) - trend_price) > 0.5*MCS ||
						sign * (B.WickLimit(sign) - trend_price) > 0.5*MCS)
						return false;

					return true;
				};

				if (IsReversal(idx, true))
				{
					tt = TradeType.Sell;
					ep = pp.TrendHigh.F(idx) - MCS;
					return EPeakPattern.HighReversal;
				}
				if (IsReversal(idx, false))
				{
					tt = TradeType.Buy;
					ep = pp.TrendLow.F(idx) + MCS + Spread;
					return EPeakPattern.LowReversal;
				}
			}

			// No pattern detected
			tt = TradeType.Buy;
			ep = 0.0;
			return null;
		}

		/// <summary>Returns the number of sequential higher lows ending at 'idx'</summary>
		public int HigherLows(Idx idx, double tol_pips = 3)
		{
			var count = 0;
			var low = this[idx].Low;
			var tol = tol_pips * PipSize;
			for (var i = idx; i-- != IdxFirst; ++count)
			{
				var lo = this[i].Low;
				if (lo > low + tol) break;
				if (lo < low) low = lo;
			}
			return count;
		}

		/// <summary>Returns the number of sequential lower highs ending at 'idx'</summary>
		public int LowerHighs(Idx idx, double tol_pips = 3)
		{
			var count = 0;
			var high = this[idx].High;
			var tol = tol_pips * PipSize;
			for (var i = idx; i-- != IdxFirst; ++count)
			{
				var hi = this[i].High;
				if (hi < high - tol) break;
				if (hi > high) high = hi;
			}
			return count;
		}

		/// <summary>Return the number of sequential peaks in the direction 'sign'</summary>
		public int SequentialPeaks(int sign, Idx idx, double tol_pips = 3)
		{
			return
				sign > 0 ? HigherLows(idx, tol_pips) :
				sign < 0 ? LowerHighs(idx, tol_pips) :
				Math.Max(HigherLows(idx, tol_pips), LowerHighs(idx, tol_pips));
		}

		/// <summary>Returns a trade type when a likely good trade is identified. Or null</summary>
		public TradeType? FindTradeEntry(out QuoteCurrency? ep, out QuoteCurrency? tp, Idx? idx_ = null)
		{
			// Where in the instrument to look
			var idx = idx_ ?? 0;

			// If Peak.Trend is strong, look for trades when price is at a peak
			// Debug. Forex4NOobs pattern.. it's still waiting for the next candle
			// Pattern Spike: enter when the spike has a clear weakening sign, e.g. wick in spike direction > 20%
			ep = null;
			tp = null;

			// Look for a candle pattern to start the entry point consideration
			CandlePattern patn;
			var pattern = IsCandlePattern(out patn, idx);
			if (pattern == null)
				return null;

			// Compare to the cached last try. If it's the same candle range tested, not including the latest candle, return the previous result
			if (LastTradeEntryRange.Beg == patn.Range.Beg - (long)IdxFirst &&
				LastTradeEntryRange.End == patn.Range.End - (long)IdxFirst &&
				patn.Range.End != 1)
				return LastTradeEntryResult;

			// Update cached result
			LastTradeEntryRange = new Range((long)(patn.Range.Beg - IdxFirst), (long)(patn.Range.End - IdxFirst));
			LastTradeEntryResult = null;

			var candle = this[idx];
			var vote = 0.0;

			// Show the debugging data
			Bot.Debugging.LogInstrument();
			Bot.Debugging.AreaOfInterest(patn.Range);
			Bot.Debugging.Dump(new SnR(this, candle.Close, idx+1));
			Bot.Debugging.Trace("\nCandle Pattern: --- {0} --- ({1})".Fmt(patn.TT, patn.Pattern));

			// Check the price peaks
			#region Price Peak
			{
				var pp = new PricePeaks(this, 0);
				Bot.Debugging.Dump(pp);

				// Vote based on agreement with trend direction
				var v = patn.TT.Sign() * pp.Strength;
				Bot.Debugging.Trace("PP Trend (vote = {0})".Fmt(v));
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
				var v = patn.TT.Sign() * ema_trend;
				Bot.Debugging.Trace("EMA Trend (vote = {0})".Fmt(v));
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
				var v = patn.TT.Sign() * strength;
				Bot.Debugging.Trace("RSI strength (vote = {0})".Fmt(v));
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
			Bot.Debugging.Trace("Final vote for {0}: {1}".Fmt(patn.TT, vote));
			const double VoteThreshold = 0.1;
			LastTradeEntryResult = (vote >= VoteThreshold) ? patn.TT: (TradeType?)null;
			return LastTradeEntryResult;
		}

		/// <summary>The CAlgo index range last used to test for a trade entry</summary>
		public Range LastTradeEntryRange { get; private set; }

		/// <summary>The result of the last test for trade entry</summary>
		public TradeType? LastTradeEntryResult { get; set; }

		/// <summary>Returns non-null if the price at 'idx_' is greater/less than the high/low of the prior 'periods' candles</summary>
		public TradeType? IsBreakOut(int periods, Idx? idx_ = null)
		{
			var idx = idx_ ?? 0;
			var price = idx_ != null ? HighRes[idx] : LatestPrice;
			var price_range = PriceRange(idx - periods, idx);

			if (price.Bid > price_range.End)
				return TradeType.Buy;
			if (price.Ask < price_range.Beg)
				return TradeType.Sell;

			return null;
		}

		/// <summary>
		/// Return this instrument at a higher or lower time frame (e.g. this=h1, ratio=2 => h2. this=h1, ratio=0.5 => m30)).
		/// Note: can't use this with back testing</summary>
		public Instrument RelativeTimeFrame(double ratio)
		{
			var tf = TimeFrame.GetRelativeTimeFrame(ratio);
			return new Instrument(Bot, Symbol, tf);
		}
	}

	/// <summary>States that the market can be in</summary>
	[Flags] public enum EMarketState
	{
		Unknown  = 0,
		Steady   = 1 << 0,
		Volatile = 1 << 1,
		Ranging  = 1 << 2,
		Trending = 1 << 3,
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
