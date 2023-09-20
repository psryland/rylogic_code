using System;
using System.Diagnostics;

namespace CoinFlip
{
	/// <summary>A time value in multiples of time frame units</summary>
	public struct TimeFrameTime
	{
		public TimeFrameTime(long ticks, ETimeFrame tf)
		{
			ExactTicks = ticks;
			TimeFrame = tf;
		}
		public TimeFrameTime(double tf_units, ETimeFrame tf)
			:this(Misc.TimeFrameToTicks(tf_units, tf), tf)
		{}
		public TimeFrameTime(TimeSpan ts, ETimeFrame tf)
			:this(ts.Ticks, tf)
		{}
		public TimeFrameTime(DateTimeOffset dt, ETimeFrame tf)
			:this(dt.Ticks, tf)
		{}
		public TimeFrameTime(DateTime dt, ETimeFrame tf)
			:this(dt.Ticks, tf)
		{
			Debug.Assert(dt.Kind == DateTimeKind.Utc);
		}
		public TimeFrameTime(TimeFrameTime tft, ETimeFrame tf)
			:this(tft.ExactTicks, tf)
		{}

		/// <summary>The exact time value in Ticks (not a multiple of the time frame units)</summary>
		public long ExactTicks { get; }

		/// <summary>The time frame units</summary>
		public ETimeFrame TimeFrame { get; }

		/// <summary>The exact time value in time frame units (includes fractional part)</summary>
		public double ExactTF => Misc.TicksToTimeFrame(ExactTicks, TimeFrame);

		/// <summary>The exact time value as a UTC DateTimeOffset</summary>
		public DateTimeOffset ExactUTC => new(ExactTicks, TimeSpan.Zero);

		/// <summary>The exact time as a time span</summary>
		public TimeSpan ExactTimeSpan => new(ExactTicks);

		/// <summary>The time in ticks rounded down to an integer time frame unit</summary>
		public long IntgTicks
		{
			get
			{
				var quanta = Misc.TimeFrameToTicks(1.0, TimeFrame);
				return (long)(ExactTicks / quanta) * quanta;
			}
		}

		/// <summary>The time value rounded down to the nearest integer time frame unit</summary>
		public double IntgTF => Misc.TicksToTimeFrame(IntgTicks, TimeFrame);

		/// <summary>The time value rounded down to the nearest integer time frame unit as a UTC DateTimeOffset</summary>
		public DateTimeOffset IntgUTC => new(IntgTicks, TimeSpan.Zero);

		/// <summary>Return this time value in a different time frame</summary>
		public TimeFrameTime To(ETimeFrame tf)
		{
			return new TimeFrameTime(this, tf);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return new DateTimeOffset(ExactTicks, TimeSpan.Zero).ToString();
		}

		#region Operators
		public static bool operator == (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.ExactTicks == rhs.ExactTicks;
		}
		public static bool operator != (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return !(lhs == rhs);
		}
		public static bool operator <  (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.ExactTicks < rhs.ExactTicks;
		}
		public static bool operator >  (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.ExactTicks > rhs.ExactTicks;
		}
		public static bool operator <= (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.ExactTicks <= rhs.ExactTicks;
		}
		public static bool operator >= (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.ExactTicks >= rhs.ExactTicks;
		}
		public static TimeFrameTime operator + (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			Debug.Assert(lhs.TimeFrame == rhs.TimeFrame);
			return new TimeFrameTime(lhs.ExactTicks + rhs.ExactTicks, lhs.TimeFrame);
		}
		public static TimeFrameTime operator - (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			Debug.Assert(lhs.TimeFrame == rhs.TimeFrame);
			return new TimeFrameTime(lhs.ExactTicks - rhs.ExactTicks, lhs.TimeFrame);
		}
		#endregion

		#region Equals
		public bool Equals(TimeFrameTime rhs)
		{
			return ExactTicks == rhs.ExactTicks && TimeFrame == rhs.TimeFrame;
		}
		public override bool Equals(object? obj)
		{
			return obj is TimeFrameTime tft && Equals(tft);
		}
		public override int GetHashCode()
		{
			return new { ExactTicks, TimeFrame }.GetHashCode();
		}
		#endregion
	}
}
