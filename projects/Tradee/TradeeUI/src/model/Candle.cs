using System;
using System.Diagnostics;

namespace Tradee
{
	public class Candle :PriceCandle
	{
		public new static readonly Candle Default = new Candle(PriceCandle.Default);

		public Candle() {}
		public Candle(PriceCandle rhs)
			:base(rhs)
		{}

		/// <summary>Classify the candle type</summary>
		public ECandleType Type
		{
			get
			{
				return ECandleType.Other;
			}
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
	}
}
