using System;
using System.ComponentModel;

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

	public static class TimeFrame
	{
		/// <summary>
		/// Convert a time value into units of a time frame.
		/// e.g if 'time_in_ticks' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned</summary>
		public static double TicksToTimeFrame(long time_in_ticks, ETimeFrame time_frame)
		{
			// Use 1 second for all tick time-frames
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1  : return new TimeSpan(time_in_ticks).TotalSeconds;
			case ETimeFrame.Min1   : return new TimeSpan(time_in_ticks).TotalMinutes;
			case ETimeFrame.Min2   : return new TimeSpan(time_in_ticks).TotalMinutes / 2.0;
			case ETimeFrame.Min3   : return new TimeSpan(time_in_ticks).TotalMinutes / 3.0;
			case ETimeFrame.Min4   : return new TimeSpan(time_in_ticks).TotalMinutes / 4.0;
			case ETimeFrame.Min5   : return new TimeSpan(time_in_ticks).TotalMinutes / 5.0;
			case ETimeFrame.Min6   : return new TimeSpan(time_in_ticks).TotalMinutes / 6.0;
			case ETimeFrame.Min7   : return new TimeSpan(time_in_ticks).TotalMinutes / 7.0;
			case ETimeFrame.Min8   : return new TimeSpan(time_in_ticks).TotalMinutes / 8.0;
			case ETimeFrame.Min9   : return new TimeSpan(time_in_ticks).TotalMinutes / 9.0;
			case ETimeFrame.Min10  : return new TimeSpan(time_in_ticks).TotalMinutes / 10.0;
			case ETimeFrame.Min15  : return new TimeSpan(time_in_ticks).TotalMinutes / 15.0;
			case ETimeFrame.Min20  : return new TimeSpan(time_in_ticks).TotalMinutes / 20.0;
			case ETimeFrame.Min30  : return new TimeSpan(time_in_ticks).TotalMinutes / 30.0;
			case ETimeFrame.Min45  : return new TimeSpan(time_in_ticks).TotalMinutes / 45.0;
			case ETimeFrame.Hour1  : return new TimeSpan(time_in_ticks).TotalHours;
			case ETimeFrame.Hour2  : return new TimeSpan(time_in_ticks).TotalHours / 2.0;
			case ETimeFrame.Hour3  : return new TimeSpan(time_in_ticks).TotalHours / 3.0;
			case ETimeFrame.Hour4  : return new TimeSpan(time_in_ticks).TotalHours / 4.0;
			case ETimeFrame.Hour6  : return new TimeSpan(time_in_ticks).TotalHours / 6.0;
			case ETimeFrame.Hour8  : return new TimeSpan(time_in_ticks).TotalHours / 8.0;
			case ETimeFrame.Hour12 : return new TimeSpan(time_in_ticks).TotalHours / 12.0;
			case ETimeFrame.Day1   : return new TimeSpan(time_in_ticks).TotalDays;
			case ETimeFrame.Day2   : return new TimeSpan(time_in_ticks).TotalDays / 2.0;
			case ETimeFrame.Day3   : return new TimeSpan(time_in_ticks).TotalDays / 3.0;
			case ETimeFrame.Weekly : return new TimeSpan(time_in_ticks).TotalDays / 7.0;
			case ETimeFrame.Monthly: return new TimeSpan(time_in_ticks).TotalDays / 30.0;
			}
		}
		public static double TimeSpanToTimeFrame(TimeSpan ts, ETimeFrame time_frame)
		{
			return TicksToTimeFrame(ts.Ticks, time_frame);
		}

		/// <summary>
		/// Convert time-frame units to ticks.
		/// e.g. if 'units' is 4.3 hours, then TimeZone.FromHours(4.3).Ticks is returned</summary>
		public static long TimeFrameToTicks(double units, ETimeFrame time_frame)
		{
			// Use 1 second for all tick time-frames
			switch (time_frame)
			{
			default: throw new Exception("Unknown time frame");
			case ETimeFrame.Tick1  : return TimeSpan.FromSeconds(units).Ticks;
			case ETimeFrame.Min1   : return TimeSpan.FromMinutes(units).Ticks;
			case ETimeFrame.Min2   : return TimeSpan.FromMinutes(units * 2.0).Ticks;
			case ETimeFrame.Min3   : return TimeSpan.FromMinutes(units * 3.0).Ticks;
			case ETimeFrame.Min4   : return TimeSpan.FromMinutes(units * 4.0).Ticks;
			case ETimeFrame.Min5   : return TimeSpan.FromMinutes(units * 5.0).Ticks;
			case ETimeFrame.Min6   : return TimeSpan.FromMinutes(units * 6.0).Ticks;
			case ETimeFrame.Min7   : return TimeSpan.FromMinutes(units * 7.0).Ticks;
			case ETimeFrame.Min8   : return TimeSpan.FromMinutes(units * 8.0).Ticks;
			case ETimeFrame.Min9   : return TimeSpan.FromMinutes(units * 9.0).Ticks;
			case ETimeFrame.Min10  : return TimeSpan.FromMinutes(units * 10.0).Ticks;
			case ETimeFrame.Min15  : return TimeSpan.FromMinutes(units * 15.0).Ticks;
			case ETimeFrame.Min20  : return TimeSpan.FromMinutes(units * 20.0).Ticks;
			case ETimeFrame.Min30  : return TimeSpan.FromMinutes(units * 30.0).Ticks;
			case ETimeFrame.Min45  : return TimeSpan.FromMinutes(units * 45.0).Ticks;
			case ETimeFrame.Hour1  : return TimeSpan.FromHours(units).Ticks;
			case ETimeFrame.Hour2  : return TimeSpan.FromHours(units * 2.0).Ticks;
			case ETimeFrame.Hour3  : return TimeSpan.FromHours(units * 3.0).Ticks;
			case ETimeFrame.Hour4  : return TimeSpan.FromHours(units * 4.0).Ticks;
			case ETimeFrame.Hour6  : return TimeSpan.FromHours(units * 6.0).Ticks;
			case ETimeFrame.Hour8  : return TimeSpan.FromHours(units * 8.0).Ticks;
			case ETimeFrame.Hour12 : return TimeSpan.FromHours(units * 12.0).Ticks;
			case ETimeFrame.Day1   : return TimeSpan.FromDays(units).Ticks;
			case ETimeFrame.Day2   : return TimeSpan.FromDays(units * 2.0).Ticks;
			case ETimeFrame.Day3   : return TimeSpan.FromDays(units * 3.0).Ticks;
			case ETimeFrame.Weekly : return TimeSpan.FromDays(units * 7.0).Ticks;
			case ETimeFrame.Monthly: return TimeSpan.FromDays(units * 30.0).Ticks;
			}
		}
		public static TimeSpan TimeFrameToTimeSpan(double units, ETimeFrame time_frame)
		{
			return TimeSpan.FromTicks(TimeFrameToTicks(units, time_frame));
		}
	}
}
