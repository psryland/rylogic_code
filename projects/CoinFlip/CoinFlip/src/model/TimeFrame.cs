using System;
using System.ComponentModel;
using System.Diagnostics;

namespace CoinFlip
{
	public enum ETimeFrame
	{
		None,
		[Description("T1 ")]     Tick1  ,
		[Description("M1 ")]     Min1   ,
		[Description("M2 ")]     Min2   ,
		[Description("M3 ")]     Min3   ,
		[Description("M4 ")]     Min4   ,
		[Description("M5 ")]     Min5   ,
		[Description("M6 ")]     Min6   ,
		[Description("M7 ")]     Min7   ,
		[Description("M8 ")]     Min8   ,
		[Description("M9 ")]     Min9   ,
		[Description("M10")]     Min10  ,
		[Description("M15")]     Min15  ,
		[Description("M20")]     Min20  ,
		[Description("M30")]     Min30  ,
		[Description("M45")]     Min45  ,
		[Description("H1 ")]     Hour1  ,
		[Description("H2 ")]     Hour2  ,
		[Description("H3 ")]     Hour3  ,
		[Description("H4 ")]     Hour4  ,
		[Description("H6 ")]     Hour6  ,
		[Description("H8 ")]     Hour8  ,
		[Description("H12")]     Hour12 ,
		[Description("D1 ")]     Day1   ,
		[Description("D2 ")]     Day2   ,
		[Description("D3 ")]     Day3   ,
		[Description("Weekly")]  Weekly ,
		[Description("Monthly")] Monthly,
	}

	/// <summary>A time value in multiples of time frame units</summary>
	public struct TimeFrameTime
	{
		private readonly long m_ticks;
		private readonly ETimeFrame m_time_frame;

		public TimeFrameTime(long ticks, ETimeFrame tf)
		{
			m_ticks      = ticks;
			m_time_frame = tf;
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
			:this(tft.m_ticks, tf)
		{}

		/// <summary>The exact time value in Ticks (not a multiple of the time frame units)</summary>
		public long ExactTicks
		{
			get { return m_ticks; }
		}

		/// <summary>The exact time value in time frame units (includes fractional part)</summary>
		public double ExactTF
		{
			get { return Misc.TicksToTimeFrame(m_ticks, m_time_frame); }
		}

		/// <summary>The exact time value as a UTC DateTimeOffset</summary>
		public DateTimeOffset ExactUTC
		{
			get { return new DateTimeOffset(ExactTicks, TimeSpan.Zero); }
		}

		/// <summary>The time in ticks rounded down to an integer time frame unit</summary>
		public long IntgTicks
		{
			get
			{
				var quanta = Misc.TimeFrameToTicks(1.0, m_time_frame);
				return (m_ticks / quanta) * quanta;
			}
		}

		/// <summary>The time value rounded down to the nearest integer time frame unit</summary>
		public double IntgTF
		{
			get { return Misc.TicksToTimeFrame(IntgTicks, m_time_frame); }
		}

		/// <summary>The time value rounded down to the nearest integer time frame unit as a UTC DateTimeOffset</summary>
		public DateTimeOffset IntgUTC
		{
			get { return new DateTimeOffset(IntgTicks, TimeSpan.Zero); }
		}

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
			return lhs.m_ticks == rhs.m_ticks;
		}
		public static bool operator != (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return !(lhs == rhs);
		}
		public static bool operator <  (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.m_ticks < rhs.m_ticks;
		}
		public static bool operator >  (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.m_ticks > rhs.m_ticks;
		}
		public static bool operator <= (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.m_ticks <= rhs.m_ticks;
		}
		public static bool operator >= (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			return lhs.m_ticks >= rhs.m_ticks;
		}
		public static TimeFrameTime operator + (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			Debug.Assert(lhs.m_time_frame == rhs.m_time_frame);
			return new TimeFrameTime(lhs.m_ticks + rhs.m_ticks, lhs.m_time_frame);
		}
		public static TimeFrameTime operator - (TimeFrameTime lhs, TimeFrameTime rhs)
		{
			Debug.Assert(lhs.m_time_frame == rhs.m_time_frame);
			return new TimeFrameTime(lhs.m_ticks - rhs.m_ticks, lhs.m_time_frame);
		}
		#endregion

		#region Equals
		public bool Equals(TimeFrameTime rhs)
		{
			return m_ticks == rhs.m_ticks && m_time_frame == rhs.m_time_frame;
		}
		public override bool Equals(object obj)
		{
			return obj is TimeFrameTime && Equals((TimeFrameTime)obj);
		}
		public override int GetHashCode()
		{
			return new { m_ticks, m_time_frame }.GetHashCode();
		}
		#endregion
	}
}
