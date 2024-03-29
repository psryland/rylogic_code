﻿using System;
using System.Diagnostics;
using Rylogic.Common;
using Rylogic.Maths;

namespace CoinFlip
{
	/// <summary>The core data of a single candle</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Candle
	{
		// Notes:
		// - Candle prices are the Q2B spot price

		public Candle() {}
		public Candle(long timestamp, double open, double high, double low, double close, double median, double volume)
		{
			Timestamp = timestamp;
			Open      = open;
			High      = Math_.Max(high, open, close);
			Low       = Math_.Min(low , open, close);
			Close     = close;
			Median    = median;
			Volume    = volume;
		}
		public Candle(Candle rhs)
		{
			Timestamp = rhs.Timestamp;
			Open      = rhs.Open;
			High      = rhs.High;
			Low       = rhs.Low;
			Close     = rhs.Close;
			Median    = rhs.Median;
			Volume    = rhs.Volume;
		}

		/// <summary>The timestamp (in ticks) of when this candle opened</summary>
		public long Timestamp { [DebuggerStepThrough] get; }

		/// <summary>The open price</summary>
		public double Open { [DebuggerStepThrough] get; private set; }

		/// <summary>The price high</summary>
		public double High { [DebuggerStepThrough] get; private set; }

		/// <summary>The price low</summary>
		public double Low  { [DebuggerStepThrough] get; private set; }

		/// <summary>The price close</summary>
		public double Close { [DebuggerStepThrough] get; private set; }

		/// <summary>The median price over the duration of the candle</summary>
		public double Median { [DebuggerStepThrough] get; private set; }

		/// <summary>The number of ticks in this candle</summary>
		public double Volume { [DebuggerStepThrough] get; private set; }

		/// <summary>+1 if bullish, -1 if bearish, 0 if neither</summary>
		public int Sign => Bullish ? +1 : Bearish ? -1 : 0;

		/// <summary>Return the Open/Close as a range</summary>
		public RangeF BodyRange => new(Math.Min(Open, Close), Math.Max(Open, Close));

		/// <summary>Return the High/Low as a range</summary>
		public RangeF WickRange => new(Math.Min(High, Low), Math.Max(High, Low));

		/// <summary>True if this is a bullish candle</summary>
		public bool Bullish => Close > Open;

		/// <summary>True if this is a bullish candle</summary>
		public bool Bearish => Close < Open;

		/// <summary>Returns the age of this candle as a normalised fraction of the time frame</summary>
		public double Age(Instrument instr)
		{
			var one = Misc.TimeFrameToTicks(1.0, instr.TimeFrame);
			var age_ticks = Model.UtcNow.Ticks - Timestamp;
			if (age_ticks < 0) return 0.0;
			if (age_ticks > one) return 1.0;
			return Math_.Frac(0.0, age_ticks, one);
		}

		/// <summary>Return the High or Low of this candle, based on 'sign'</summary>
		public double WickLimit(int sign)
		{
			return sign == +1 ? High : sign == -1 ? Low : Median;
		}

		/// <summary>Return the highest or lowest of Open or Close for this candle, based on 'sign'</summary>
		public double BodyLimit(int sign)
		{
			return sign == +1 ? Math.Max(Open,Close) : sign == -1 ? Math.Min(Open,Close) : Median;
		}

		/// <summary>The overall length of the candle from high to low</summary>
		public double TotalLength => High - Low;

		/// <summary>The length of the body of the candle</summary>
		public double BodyLength => Math.Abs(Open - Close);

		/// <summary>The centre of the candle body</summary>
		public double BodyCentre => (Open + Close) / 2.0;

		/// <summary>The length of the upper wick</summary>
		public double UpperWickLength => High - Math.Max(Open, Close);

		/// <summary>The length of the lower wick</summary>
		public double LowerWickLength => Math.Min(Open, Close) - Low;

		/// <summary>Single candle type</summary>
		public EType Type
		{
			get
			{
				if (BodyLength < 0.03 * TotalLength)
				{
					return EType.Doji;
				}
				if (BodyLength < 0.25 * TotalLength)
				{
					if (UpperWickLength < 0.1 * TotalLength) return EType.Hammer;
					if (LowerWickLength < 0.1 * TotalLength) return EType.InvHammer;
					return EType.SpinningTop;
				}
				if (BodyLength > 0.75 * TotalLength)
				{
					return EType.Marubozu;
				}
				if (UpperWickLength < 0.1 * TotalLength)
				{
					return
						Bullish ? EType.MarubozuStrengthening :
						Bearish ? EType.MarubozuWeakening :
						EType.Unknown;
				}
				if (LowerWickLength < 0.1 * TotalLength)
				{
					return
						Bullish ? EType.MarubozuWeakening :
						Bearish ? EType.MarubozuStrengthening :
						EType.Unknown;
				}
				return EType.Unknown;
			}
		}

		/// <summary>Replace the data in this candle with 'rhs' (Must have a matching timestamp though)</summary>
		public void Update(Candle rhs)
		{
			Debug.Assert(Timestamp == rhs.Timestamp);
			Open   = rhs.Open;
			High   = rhs.High;
			Low    = rhs.Low;
			Close  = rhs.Close;
			Median = rhs.Median;
			Volume = rhs.Volume;
		}

		/// <summary>The time stamp of this candle (in UTC)</summary>
		public DateTimeOffset TimestampUTC => new(Timestamp, TimeSpan.Zero);

		/// <summary>The time stamp of this candle (in local time)</summary>
		public DateTimeOffset TimestampLocal => TimestampUTC.ToLocalTime();

		/// <summary>Return the end time for this candle given 'time_frame'</summary>
		public DateTimeOffset CloseTime(ETimeFrame time_frame)
		{
			return TimestampUTC + Misc.TimeFrameToTimeSpan(1.0, time_frame);
		}

		/// <summary>Interpolate this candle between it's known values. Used to simulate higher resolution of price movement</summary>
		public Candle SubCandle(double t)
		{
			// Bullish candles, return: Open, Low, High, Close
			// Bearish candles, return: Open, High, Low, Close
			var vol = Math_.Lerp(t, 0, Volume);
			var t0 = Bullish ? 0.6667 : 0.3333;
			var t1 = Bullish ? 0.3333 : 0.6667;
			var close = Bullish
				? Math_.Lerp(t, Open, Low, High, Close)
				: Math_.Lerp(t, Open, High, Low, Close);

			return new Candle(Timestamp, Open,
				t < t0 ? Math.Max(Open, close) : High,
				t < t1 ? Math.Min(Open, close) : Low,
				close, Median, vol);
		}
		public Candle SubCandle(DateTimeOffset now, ETimeFrame time_frame)
		{
			// Interpolate the latest candle to determine the spot price
			var t = Math_.Frac(Timestamp, now.Ticks, Timestamp + Misc.TimeFrameToTicks(1.0, time_frame));
			return SubCandle(Math_.Clamp(t, 0.0, 1.0));
		}

		/// <summary>Debugging check for self consistency</summary>
		public bool Valid()
		{
			return
				Timestamp <  (DateTimeOffset.UtcNow + TimeSpan.FromDays(100)).Ticks &&
				High      >= Open && High >= Close &&
				Low       <= Open && Low  <= Close &&
				High      >= Low &&
				Volume    >= 0; // empty candles?
		}

		/// <summary>A nice string description of the candle</summary>
		public string Description => $"Time={new DateTimeOffset(Timestamp, TimeSpan.Zero)} OHLC=({Open:G4} {High:G4} {Low:G4} {Close:G4}) Vol={Volume}";

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return Description;
		}

		#region Equals
		public bool Equals(Candle rhs)
		{
			return
				rhs       != null &&
				Timestamp == rhs.Timestamp &&
				Open      == rhs.Open      &&
				High      == rhs.High      &&
				Low       == rhs.Low       &&
				Close     == rhs.Close     &&
				Median    == rhs.Median    &&
				Volume    == rhs.Volume    ;
		}
		public override bool Equals(object? obj)
		{
			return obj is Candle candle && Equals(candle);
		}
		public override int GetHashCode()
		{
			return new { Timestamp, Open, High, Low, Close, Median, Volume }.GetHashCode();
		}
		#endregion

		public enum EType
		{
			/// <summary>
			/// A candle with a reasonably large body and wicks on either side.
			/// Can mean a trend continuation if part of a trend, or consolidation
			/// if preceded by similar candles of opposite trend.</summary>
			Unknown,

			/// <summary>Indicates indecision. Could be a reversal or simply low volume</summary>
			Doji,

			/// <summary>
			/// Indecision candle. Can mean reversal or consolidation</summary>
			SpinningTop,

			/// <summary>
			/// Small body near the top of the candle. Can mean reversal</summary>
			Hammer,

			/// <summary>
			/// Small body near the bottom of the candle. Can mean reversal</summary>
			InvHammer,

			/// <summary>
			/// The wick on the opening side is very short but longer on the closing side.
			/// Indicates the trend is weakening because the close pulled back.</summary>
			MarubozuWeakening,

			/// <summary>
			/// The wick on the closing side is very short but longer on the opening side.
			/// Indicates the trend is strengthening because the open pulled back but the
			/// close finished with strong trend.</summary>
			MarubozuStrengthening,

			/// <summary>
			/// Body of the candle is almost all of the candle (strong trend)</summary>
			Marubozu,
		}
	}
}
