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
			HighRes = new List<PriceTick>();

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

			// Capture the high res price data
			var index = FractionalIndexAt(Bot.UtcNow) - (double)IdxFirst;
			var idx = HighRes.Count != 0 && HighRes.Back().m_index > index ? HighRes.BinarySearch(x => x.m_index.CompareTo(index)) : HighRes.Count;
			if (idx < 0) idx = ~idx;
			HighRes.Insert(idx, new PriceTick(index, Symbol.Ask, Symbol.Bid));

			// Apply the data to the latest candle or invalidate the cached Latest
			if (NewCandle)
				Latest = null;
			else
				Latest.Update(candle);

			// Record the last time data was received
			LastUpdatedUTC = DateTimeOffset.UtcNow;

			// Invalidate cached, per-candle, properties
			m_msc = null;

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

		/// <summary>The high res data for the instrument. 'x' is the CAlgo index, 'y' is the ask price, 'z' is the bid price, 'w' is unused</summary>
		public List<PriceTick> HighRes
		{
			get;
			private set;
		}
		public struct PriceTick
		{
			/// <summary>The fractional CAlgo index</summary>
			public double m_index;

			/// <summary>The Ask price</summary>
			public QuoteCurrency m_ask;

			/// <summary>The Ask price</summary>
			public QuoteCurrency m_bid;

			public PriceTick(double index, QuoteCurrency ask, QuoteCurrency bid)
			{
				m_index = index;
				m_ask = ask;
				m_bid = bid;
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

		/// <summary>The current Ask(+1)/Bid(-1)/Mid(0) price</summary>
		public QuoteCurrency CurrentPrice(int sign)
		{
			return Symbol.CurrentPrice(sign);
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
			var min = Maths.Clamp((int)idx_min, (int)IdxFirst, (int)IdxLast);
			var max = Maths.Clamp((int)idx_max, (int)min, (int)IdxLast);
			return new Range(min, max);
		}
		public Range IndexRange(Range range)
		{
			return IndexRange(range.Begini, range.Endi);
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
			Debug.Assert(checked_range.Begini >= IdxFirst);
			Debug.Assert(checked_range.Endi <= IdxLast);
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

			var istart = HighRes.BinarySearch(x => x.m_index.CompareTo(first));
			var iend   = HighRes.BinarySearch(x => x.m_index.CompareTo(last));
			if (istart < 0) istart = ~istart;
			if (iend   < 0) iend = ~iend;
			if (istart == iend)
				yield break;

			// The point to return
			var pt = HighRes[istart];

			// Loop over the requested range
			var X = HighRes[istart].m_index;
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
					pt = new PriceTick(0.0, double.MinValue, double.MaxValue);
					for (++i; i < iend; ++i)
					{
						pt.m_index += HighRes[i].m_index;
						pt.m_ask = Math.Max((double)pt.m_ask, (double)HighRes[i].m_ask);
						pt.m_bid = Math.Min((double)pt.m_bid, (double)HighRes[i].m_bid);
						++cnt;
						if (HighRes[i].m_index > X)
							break;
					}
					pt.m_index /= cnt;
				}
			}
		}
		public IEnumerable<PriceTick> HighResRange(RangeF idx_range, double? step = null)
		{
			return HighResRange(idx_range.Begin, idx_range.End, step);
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
		public QuoteCurrency MCS_50
		{
			get { return (m_msc ?? (m_msc = MedianCandleSize(IdxLast - 50, IdxLast))).Value; }
		}
		private QuoteCurrency? m_msc;

		/// <summary>Return the average candle size (total length) of the given range</summary>
		public QuoteCurrency MedianCandleSize(NegIdx idx_min, NegIdx idx_max)
		{
			var lengths = CandleRange(idx_min, idx_max).Select(x => x.TotalLength).ToArray();
			if (lengths.Length == 0) throw new Exception("Empty range, median candle size is not defined");
			var mcs = lengths.NthElement(lengths.Length/2);
			return mcs;
		}
		public QuoteCurrency MedianCandleSize(Range range)
		{
			return MedianCandleSize(range.Begini, range.Endi);
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
			for (var i = range.Endi; i-- != IdxFirst;)
			{
				var candle = this[i];

				// Find the range spanned.
				body_range.Encompass((double)candle.Open);
				body_range.Encompass((double)candle.Close);
				full_range.Encompass((double)candle.High);
				full_range.Encompass((double)candle.Low);

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
				group.Encompass((double)candle.BodyLimit(-sign));

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

		/// <summary>Compress candles over the given range into a single equivalent candle</summary>
		public Candle Compress(NegIdx idx_min, NegIdx idx_max)
		{
			var candles = CandleRange(idx_min, idx_max);
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

		/// <summary>True if the candle pattern leading up to 'idx' indicates a price reversal.</summary>
		public bool IsCandlePattern(NegIdx idx, out int forecast_direction, out QuoteCurrency target_entry)
		{
			// Not enough data, assume not
			// Require at least 3 candles
			if (idx - IdxFirst < 3)
			{
				forecast_direction = 0;
				target_entry = 0;
				return false;
			}

			// Measure the age of the latest candle (normalised)
			var a_age = AgeOf(this[idx], clamped:true);
			if (a_age < 0.8) --idx; // include the latest candle if mostly done

			var mcs = MCS_50;

			// Get the last few candles
			// 'A' is treated as the latest closed candle.
			var A = this[idx-0]; var a_type = A.Type(mcs);
			var B = this[idx-1]; var b_type = B.Type(mcs);
			var C = this[idx-2]; var c_type = C.Type(mcs);

			// Measure the strength of the trend leading up to 'B' (but not including)
			var preceding_trend = MeasureTrend(idx - 5, idx - 1);

			// Strong trend, Indecision, followed by strong reverse trend
			if ((b_type.IsIndecision()) &&                                                                                      // A hammer, spinning top, or doji
				(Math.Abs(preceding_trend) > 0.5) &&                                                                            // The preceding trend is strong
				(A.Sign != Math.Sign(preceding_trend)) &&                                                                       // 'A' is in the opposite direction to the preceding trend
				(a_type == Candle.EType.Marubozu || a_type == Candle.EType.MarubozuStrengthening ||                             // 'A' is a trend sign
				(A.Strength > 0.7 && Math.Abs(A.Close - B.BodyCentre) > 1.2 * Math.Abs(B.WickLimit(A.Sign) - B.BodyCentre))) && // 'A' is a trend sign
				true)
			{
				forecast_direction = A.Sign;
				target_entry = B.Median;
				return true;
			}

			// Indecision, followed by two semi strong candles in the same direction
			if ((c_type.IsIndecision()) &&                  // A hammer, spinning top, or doji
				(Math.Abs(preceding_trend) > 0.5) &&        // The preceding trend is strong
				(A.Sign != Math.Sign(preceding_trend)) &&   // 'A' is in the opposite direction to the preceding trend
				(B.Sign == A.Sign) &&                       // 'A' and 'B' in the same direction
				(Math.Abs(A.Open - B.Close) < 0.1 * mcs) && // 'A' and 'B' join nose to tail
				(Math.Abs(A.Close - B.Open) > mcs) &&       // 'A' and 'B' combined is a significant trend indication
				true)
			{
				forecast_direction = A.Sign;
				target_entry = C.Median;
				return true;
			}

			// Engulfing pattern
			const double EngulfingRatio = 1.2;
			if ((A.Sign != B.Sign) &&                             // Candles need opposite directions
				(!b_type.IsIndecision()) &&                       // Both candles need to be a reasonable size
				(A.Strength > 0.7 && A.BodyLength > 0.5*mcs) &&   // Both candles need to be a reasonable size
				(A.BodyLength > EngulfingRatio * B.BodyLength) && // 'A' is bigger than 'B'
				(Math.Abs(A.Open - B.Close) < 0.1 * mcs) &&       // A.Open must be fairly close to B.Close
				(Math.Abs(preceding_trend) > 0.5) &&              // The preceding trend is strong
				(A.Sign != Math.Sign(preceding_trend)) &&         // 'A' is in the opposite direction to the preceding trend
				true)
			{
				// Engulfing patterns tend to take off, so set the target entry at A.Close
				forecast_direction = A.Sign;
				target_entry = A.Close;
				return true;
			}

			// Engulfing pattern, two semi strong candles in the same direction engulfing 'C'
			var AB = Compress(idx-1,idx-0);
			if ((A.Sign == B.Sign && A.Sign != C.Sign) &&          // 'A' and 'B' are in the same direction, and are opposite to 'C'
				(!c_type.IsIndecision()) &&                        // Both candles need to be a reasonable size
				(AB.Strength > 0.7 && AB.BodyLength > 0.5*mcs) &&  // Both candles need to be a reasonable size
				(AB.BodyLength > EngulfingRatio * C.BodyLength) && // 'A+B' is bigger than 'C'
				(Math.Abs(B.Open - C.Close) < 0.1 * mcs) &&        // B.Open must be fairly close to C.Close
				(Math.Abs(preceding_trend) > 0.5) &&               // The preceding trend is strong
				(AB.Sign != Math.Sign(preceding_trend)) &&         // 'A' is in the opposite direction to the preceding trend
				true)
			{
				// Engulfing patterns tend to take off, so set the target entry at A.Close
				forecast_direction = A.Sign;
				target_entry = A.Close;
				return true;
			}
			forecast_direction = 0;
			target_entry = 0;
			return false;
		}

		/// <summary>Returns a trade type when a likely good trade is identified. Or null</summary>
		public TradeType? FindTradeEntry(NegIdx? idx_ = null)
		{
			// Where in the instrument to look
			var idx = idx_ ?? 0;

			// Need some sort of voting system.
			// Each method adds a weighted vote of whether to buy or sell or skip.
			// Need some way to see what each vote is for each candle
			//  -> Step through each new candle, write the votes to a text file

			int dir;
			QuoteCurrency entry;
			if (IsCandlePattern(idx, out dir, out entry))
				return
					dir > 0 ? TradeType.Buy :
					dir < 0 ? TradeType.Sell :
					(TradeType?)null;
			
			return null;

			// Get the current price
			var curr_price = idx_ != null ? this[idx].Close : (double)CurrentPrice(0);

			// Get the gradient of the long period EMA
			var ema100 = Bot.Indicators.ExponentialMovingAverage(Data.Close, 100);
			var grad100 = ema100.Result.FirstDerivative(idx) / MCS_50;

			// Get the gradient of the short period EMA
			var ema14 = Bot.Indicators.ExponentialMovingAverage(Data.Close, 14);
			var grad14 = ema14.Result.FirstDerivative(idx) / MCS_50;

			// Is price is trending...
			var price_trending = 0;
			{
				// Price is trending if:
				//  - the long EMA has a large slope
				//  - the short EMA is on the same side as the curvature of the long EMA
				const double ema100_trending = 0.1; // pips per index
				price_trending =
					grad100 > +ema100_trending ? +1 :
					grad100 < -ema100_trending ? -1 :
					0;

			}

			{// Look for price volatility >> MCS
				// If price isn't trending but is moving up and down significantly more
				// than the mean candle size, then price is ranging
			}

			// Is the instrument is over-bought or over-sold?
			{
				// Look for the price over or under 70/30 and having just crossed back into the normal range
				var rsi = Bot.Indicators.RelativeStrengthIndex(Data.Close, 14);
				var rsi_sum = rsi.Result.Integrate(idx - 5, idx - 0) / 5.0;

				//if (tt == TradeType.Buy && rsi.Result.LastValue > 70.0)
				//	return null; // Over bought
				//if (tt == TradeType.Sell && rsi.Result.LastValue < 30.0)
				//	return null; // Over sold
			}

			// Is price showing a known candle pattern on a strong SnR level
			{
			}

			// Has price spiked a long way over a few candles?
			{
				// Expect the price to retrace back by a third of the price jump
			}
		}
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
