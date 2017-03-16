using System;
using System.Diagnostics;
using pr.common;
using pr.extn;

namespace Rylobot
{
	public class Candle
	{
		public Candle() {}
		public Candle(int index, long timestamp, double open, double high, double low, double close, double median, double volume)
		{
			Index     = index;
			Timestamp = timestamp;
			Open      = open;
			High      = Math.Max(high, Math.Max(open, close));
			Low       = Math.Min(low , Math.Min(open, close));
			Close     = close;
			Median    = median;
			Volume    = volume;
		}
		public Candle(Candle rhs)
		{
			Index     = rhs.Index;
			Timestamp = rhs.Timestamp;
			Open      = rhs.Open;
			High      = rhs.High;
			Low       = rhs.Low;
			Close     = rhs.Close;
			Median    = rhs.Median;
			Volume    = rhs.Volume;
		}

		/// <summary>CAlgo index</summary>
		public int Index { get; private set; }

		/// <summary>The timestamp (in ticks) of when this candle opened</summary>
		public long Timestamp { get; private set; }

		/// <summary>The open price</summary>
		public double Open { get; private set; }

		/// <summary>The price high</summary>
		public double High { get; private set; }

		/// <summary>The price low</summary>
		public double Low  { get; private set; }

		/// <summary>The price close</summary>
		public double Close { get; private set; }

		/// <summary>The median price over the duration of the candle</summary>
		public double Median { get; private set; }

		/// <summary>The number of ticks in this candle</summary>
		public double Volume { get; private set; }

		/// <summary>+1 if bullish, -1 if bearish, 0 if neither</summary>
		public int Sign
		{
			get { return Bullish ? +1 : Bearish ? -1 : 0; }
		}

		/// <summary>True if this is a bullish candle</summary>
		public bool Bullish
		{
			get { return Close > Open; }
		}

		/// <summary>True if this is a bullish candle</summary>
		public bool Bearish
		{
			get { return Close < Open; }
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

		/// <summary>Known candle types</summary>
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

		/// <summary>Single candle type. Relative to the mean candle size (total size)</summary>
		public EType Type(QuoteCurrency mcs)
		{
			// A candle is a doji if the body is very small
			if (BodyLength < 0.03 * mcs)
			{
				return EType.Doji;
			}

			// A candle is a hammer/inverse hammer if it has a small body that is near one end of the candle
			if (BodyLength < 0.125 * mcs || BodyLength < 0.1 * TotalLength)
			{
				if (UpperWickLength < 0.1 * TotalLength) return EType.Hammer;
				if (LowerWickLength < 0.1 * TotalLength) return EType.InvHammer;
				return EType.SpinningTop;
			}

			// A candle is a Marubozu if it has a large body and is a large portion of the total length
			if (BodyLength > 0.9 * mcs && BodyLength > 0.5 * TotalLength)
			{
				// A strengthening or weakening marubozu
				return
					Strengthening ? EType.MarubozuStrengthening :
					Weakening     ? EType.MarubozuWeakening :
					EType.Marubozu;
			}

			// No particular type
			return EType.Unknown;
		}

		/// <summary>The overall length of the candle from high to low</summary>
		public double TotalLength
		{
			get { return High - Low; }
		}

		/// <summary>The length of the body of the candle</summary>
		public double BodyLength
		{
			get { return Math.Abs(Open - Close); }
		}

		/// <summary>The centre of the candle body</summary>
		public double BodyCentre
		{
			get { return (Open + Close) / 2.0; }
		}

		/// <summary>The centre of the whole candle</summary>
		public double Centre
		{
			get { return (High + Low) / 2.0; }
		}

		/// <summary>The ratio of BodyLength to TotalLevel. "Strong" candles are those that show a significant move over the life of the candle</summary>
		public double Strength
		{
			get { return BodyLength / TotalLength; }
		}

		/// <summary>True if the wick of this candle is long on the open side, indicating the price is strengthening in the direction of the candle</summary>
		public bool Strengthening
		{
			get
			{
				return 
					(LowerWickLength > 0.4 * TotalLength && Bullish) ||
					(UpperWickLength > 0.4 * TotalLength && Bearish);
			}
		}

		/// <summary>True if the wick of this candle is long on the close side, indicating the price is weakening in the direction of the candle</summary>
		public bool Weakening
		{
			get
			{
				return 
					(LowerWickLength > 0.4 * TotalLength && Bearish) ||
					(UpperWickLength > 0.4 * TotalLength && Bullish);
			}
		}

		/// <summary>Return the Open/Close as a range</summary>
		public RangeF BodyRange
		{
			get { return new RangeF(Math.Min(Open, Close), Math.Max(Open,Close)); }
		}

		/// <summary>The length of the upper wick</summary>
		public double UpperWickLength
		{
			get { return High - Math.Max(Open, Close); }
		}

		/// <summary>The length of the lower wick</summary>
		public double LowerWickLength
		{
			get { return Math.Min(Open, Close) - Low; }
		}

		/// <summary>Return the High/Low as a range</summary>
		public RangeF WickRange
		{
			get { return new RangeF(Math.Min(High, Low), Math.Max(High, Low)); }
		}

		/// <summary>Replace the data in this candle with 'rhs' (Must have a matching timestamp though)</summary>
		public void Update(Candle rhs)
		{
			Debug.Assert(Timestamp == rhs.Timestamp);
			Open      = rhs.Open;
			High      = rhs.High;
			Low       = rhs.Low;
			Close     = rhs.Close;
			Median    = rhs.Median;
			Volume    = rhs.Volume;
		}

		/// <summary>Incorporate 'price' into this candle</summary>
		public void Update(PriceTick price)
		{
			High  = Math.Max(High, price.Bid);
			Low   = Math.Min(Low , price.Bid);
			Close = price.Bid;
		}

		/// <summary>The time stamp of this candle (in UTC)</summary>
		public DateTimeOffset TimestampUTC
		{
			get { return new DateTimeOffset(Timestamp, TimeSpan.Zero); }
		}

		/// <summary>The time stamp of this candle (in local time)</summary>
		public DateTime TimestampLocal
		{
			get { return TimeZone.CurrentTimeZone.ToLocalTime(TimestampUTC.DateTime); }
		}

		/// <summary>Debugging check for self consistency</summary>
		public bool Valid()
		{
			return
				Timestamp <  (DateTimeOffset.UtcNow + TimeSpan.FromDays(100)).Ticks &&
				High      >= Open && High >= Close &&
				Low       <= Open && Low  <= Close &&
				High      >= Low &&
				Volume    >= 0.0; // empty candles? cTrader sends them tho...
		}

		/// <summary>Friendly print</summary>
		public override string ToString()
		{
			return string.Format("ts:{0} - ohlc:({1:G4} {2:G4} {3:G4} {4:G4}) - vol:{5}", new DateTimeOffset(Timestamp, TimeSpan.Zero).ToString(), Open, High, Low, Close, Volume);
		}

		#region Equals
		public bool Equals(Candle rhs)
		{
			return
				Timestamp == rhs.Timestamp &&
				Open      == rhs.Open      &&
				High      == rhs.High      &&
				Low       == rhs.Low       &&
				Close     == rhs.Close     &&
				Median    == rhs.Median    &&
				Volume    == rhs.Volume    ;
		}
		public override bool Equals(object obj)
		{
			return obj is Candle && Equals((Candle)obj);
		}
		public override int GetHashCode()
		{
			return new { Timestamp, Open, High, Low, Close, Median, Volume }.GetHashCode();
		}
		#endregion
	}

	public static class Candle_
	{
		/// <summary>True if this is a trend indicating candle type</summary>
		public static bool IsTrend(this Candle.EType type)
		{
			return type == Candle.EType.Marubozu || type == Candle.EType.MarubozuStrengthening || type == Candle.EType.MarubozuWeakening;
		}

		/// <summary>True if this is a strengthening trend indicating candle type</summary>
		public static bool IsTrendStrengthening(this Candle.EType type)
		{
			return type == Candle.EType.Marubozu || type == Candle.EType.MarubozuStrengthening;
		}

		/// <summary>True if this is a weakening trend indicating candle type</summary>
		public static bool IsTrendWeakening(this Candle.EType type)
		{
			return type == Candle.EType.Marubozu || type == Candle.EType.MarubozuWeakening;
		}

		/// <summary>True if this is an indecision (reversal or continuation) candle type</summary>
		public static bool IsIndecision(this Candle.EType type)
		{
			return type == Candle.EType.Doji || type == Candle.EType.SpinningTop || type == Candle.EType.Hammer || type == Candle.EType.InvHammer;
		}
	}
}
