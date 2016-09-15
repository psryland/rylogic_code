using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using pr.win32;

namespace pr.extn
{
	public static class DateTimeEx
	{
		/// <summary>
		/// Set the MinDate, MaxDate, and Value members all at once, avoiding out of range exceptions.
		/// val, min, max must all have a specified 'Kind' value.
		/// Values are clamped to the valid range for a DateTimePicker</summary>
		public static void Set(this DateTimePicker dtp, DateTime val, DateTime min, DateTime max)
		{
			if (val.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Value has an unspecified time-zone");
			if (min.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Minimum Value has an unspecified time-zone");
			if (max.Kind == DateTimeKind.Unspecified) throw new Exception("DateTimePicker Maximum Value has an unspecified time-zone");
			if (min > max)
				throw new Exception("Minimum date/time value is greater than the maximum date/time value");
			if (val != val.Clamp(min,max))
				throw new Exception("Date/time value is not within the given range of date/time values");

			// Clamp the values to the valid range for 'DateTimePicker'
			val = val.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
			min = min.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
			max = max.Clamp(DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);

			// Setting to MinimumDateTime/MaximumDateTime first avoids problems if min > MaxDate or max < MinDate
			dtp.MinDate = DateTimePicker.MinimumDateTime.As(val.Kind);
			dtp.MaxDate = DateTimePicker.MaximumDateTime.As(val.Kind);

			// Setting Value before MinDate/MaxDate avoids setting Value twice when Value < MinDate or Value > MaxDate
			dtp.Value = val;
			dtp.MinDate = min;
			dtp.MaxDate = max;
		}

		/// <summary>Sets the MinDate, MaxDate, and Value members all to 'kind'.</summary>
		public static void To(this DateTimePicker dtp, DateTimeKind kind)
		{
			var min = dtp.MinDate.To(kind);
			var max = dtp.MaxDate.To(kind);
			var val = dtp.Value.To(kind);
			if (dtp is pr.gui.DateTimePicker) ((pr.gui.DateTimePicker)dtp).Kind = kind;
			dtp.Set(val, min, max);
		}
	}
	
	public static class DateTime_
	{
		// Notes:
		//  If a DateTime has kind == unspecified, calling ToUniversalTime() causes it to assume that the time is
		//  local and returns the time converted to UTC. Calling ToLocalTime() causes it to assume that the time
		//  is UTC and returns the time converted to Local.
		//
		// Rule of thumb: Don't use 'ToLocalTime' or 'ToUniversalTime', use 'To' instead

		/// <summary>Converts this date time to 'kind'. Note: throws if Kind == Unspecified</summary>
		public static DateTime To(this DateTime dt, DateTimeKind kind)
		{
			// Prefer calling this method instead of 'ToLocalTime' or 'ToUniversalTime' directly
			if (dt.Kind == DateTimeKind.Unspecified) throw new Exception("Cannot convert an unspecified DateTime to {0}".Fmt(kind));
			if (kind == DateTimeKind.Local) return dt.ToLocalTime();
			if (kind == DateTimeKind.Utc) return dt.ToUniversalTime();
			return dt.As(DateTimeKind.Unspecified);
		}

		/// <summary>
		/// Return a date time reinterpreted as 'kind'.
		/// If the datetime already has a known kind that is different to 'kind' then an exception is raised.</summary>
		public static DateTime As(this DateTime dt, DateTimeKind kind)
		{
			if (kind == dt.Kind)
				return dt;
			if (kind == DateTimeKind.Unspecified)
				return DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
			if (dt.Kind == DateTimeKind.Unspecified)
				return DateTime.SpecifyKind(dt, kind);
			else
				throw new Exception("Reinterpret between UTC/Local, convert to Unspecified first");
		}

		/// <summary>Returns the minimum of two DateTime objects</summary>
		public static DateTime Min(DateTime lhs, DateTime rhs)
		{
			// Convert 'min'/'max' to the same kind as 'time'
			lhs = lhs.Kind == DateTimeKind.Unspecified ? lhs.As(rhs.Kind) : lhs.To(rhs.Kind);
			rhs = rhs.Kind == DateTimeKind.Unspecified ? rhs.As(lhs.Kind) : rhs.To(lhs.Kind);
			return lhs <= rhs ? lhs : rhs;
		}

		/// <summary>Returns the maximum of two DateTime objects</summary>
		public static DateTime Max(DateTime lhs, DateTime rhs)
		{
			// Convert 'min'/'max' to the same kind as 'time'
			lhs = lhs.Kind == DateTimeKind.Unspecified ? lhs.As(rhs.Kind) : lhs.To(rhs.Kind);
			rhs = rhs.Kind == DateTimeKind.Unspecified ? rhs.As(lhs.Kind) : rhs.To(lhs.Kind);
			return lhs >= rhs ? lhs : rhs;
		}

		/// <summary>Returns a new DateTime object clamped to within the given range</summary>
		public static DateTime Clamp(this DateTime time, DateTime min, DateTime max)
		{
			// Convert 'min'/'max' to the same kind as 'time'
			min = min.Kind == DateTimeKind.Unspecified ? min.As(time.Kind) : min.To(time.Kind);
			max = max.Kind == DateTimeKind.Unspecified ? max.As(time.Kind) : max.To(time.Kind);
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}

		/// <summary>Mask all parts of a date time not present in 'format' to zero</summary>
		public static DateTime MaskByFormat(DateTime dt, string format)
		{
			return format.HasValue() ? DateTime.ParseExact(dt.ToString(format), format, null) : dt;
		}

		/// <summary>Parse 'val' as a date time using 'format'</summary>
		public static DateTime Parse(string val, string format, DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces|DateTimeStyles.AssumeUniversal)
		{
			return DateTime.ParseExact(val, format, null, style);
		}

		/// <summary>Try to parse 'val' as a date time using 'format', returns null on parse failure</summary>
		public static DateTime? TryParse(string val, string format, DateTimeStyles style = DateTimeStyles.AllowWhiteSpaces|DateTimeStyles.AssumeUniversal)
		{
			DateTime result;
			return DateTime.TryParseExact(val, format, null, style, out result) ? (DateTime?)result : null;
		}
	}

	public static class DateTimeOffset_
	{
		/// <summary>Return the Date for the current time (in local time) with the TimeOfDay set to 00:00:00</summary>
		public static DateTimeOffset Today
		{
			get
			{
				var now = DateTimeOffset.Now;
				return now - now.TimeOfDay;
			}
		}

		/// <summary>Return the Date for the current time (in UTC) with the TimeOfDay set to 00:00:00</summary>
		public static DateTimeOffset UtcToday
		{
			get
			{
				var now = DateTimeOffset.UtcNow;
				return now - now.TimeOfDay;
			}
		}

		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTime DateTime(this DateTimeOffset time, DateTimeKind kind)
		{
			return time.DateTime.As(kind);
		}

		/// <summary>Returns the minimum of two DateTime objects</summary>
		public static DateTimeOffset Min(DateTimeOffset lhs, DateTimeOffset rhs)
		{
			return lhs <= rhs ? lhs : rhs;
		}

		/// <summary>Returns the maximum of two DateTime objects</summary>
		public static DateTimeOffset Max(DateTimeOffset lhs, DateTimeOffset rhs)
		{
			return lhs >= rhs ? lhs : rhs;
		}

		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTimeOffset Clamp(this DateTimeOffset time, DateTimeOffset min, DateTimeOffset max)
		{
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}

		/// <summary>Convert a win32 system time to a date time offset</summary>
		public static DateTimeOffset FromSystemTime(Win32.SYSTEMTIME st)
		{
			var ft = new Win32.FILETIME();
			if (!Win32.SystemTimeToFileTime(ref st, out ft))
				throw new Exception("Failed to convert system time to file time");
			return DateTimeOffset.FromFileTime(ft.value);
		}
	}

	public static class TimeSpan_
	{
		/// <summary>Return the absolute value of a time span</summary>
		public static TimeSpan Abs(TimeSpan ts)
		{
			return ts >= TimeSpan.Zero ? ts : -ts;
		}
			
		/// <summary>Returns the lesser of lhs,rhs or lhs if they are equal</summary>
		public static TimeSpan Min(TimeSpan lhs, TimeSpan rhs)
		{
			return lhs <= rhs ? lhs : rhs;
		}

		/// <summary>Returns the greater of lhs,rhs or lhs if they are equal</summary>
		public static TimeSpan Max(TimeSpan lhs, TimeSpan rhs)
		{
			return lhs >= rhs ? lhs : rhs;
		}

		/// <summary>Clamps 'x' to the inclusive range [min,max]</summary>
		public static TimeSpan Clamp(TimeSpan x, TimeSpan min, TimeSpan max)
		{
			return x < min ? min : x > max ? max : x;
		}

		/// <summary>Parse a timespan from a string</summary>
		public static TimeSpan? TryParse(string s)
		{
			TimeSpan ts;
			return TimeSpan.TryParse(s, out ts) ? (TimeSpan?)ts : null;
		}
		public static TimeSpan? TryParseExact(string s, string fmt)
		{
			TimeSpan ts;
			return TimeSpan.TryParseExact(s, fmt, null, out ts) ? (TimeSpan?)ts : null;
		}

		/// <summary>
		/// Parse expressions representing time values<para/>
		/// E.g. 10 (seconds implied), 2:30 (2 minutes, 30 seconds), 1:3:10 (1 hour, 3 mins, 10 secs),<para/>
		///  10sec, 2min, 1 hr, 2 min 30sec, 2.5 min (2:30), 30s 10m 1h (01:10:30)</summary>
		public static TimeSpan? TryParseExpr(string val, ETimeUnits default_units = ETimeUnits.Seconds)
		{
			// If the value parses as a double, treat the number as 'default_units'
			double x;
			if (double.TryParse(val, out x))
			{
				switch (default_units)
				{
				default: throw new Exception("unknown time units {0}".Fmt(default_units));
				case ETimeUnits.Milliseconds: return TimeSpan.FromMilliseconds(x);
				case ETimeUnits.Seconds:      return TimeSpan.FromSeconds(x);
				case ETimeUnits.Minutes:      return TimeSpan.FromMinutes(x);
				case ETimeUnits.Hours:        return TimeSpan.FromHours(x);
				case ETimeUnits.Days:         return TimeSpan.FromDays(x);
				}
			}

			// Try standard TimeSpan expressions
			var ts = TimeSpan.Zero;
			if (TimeSpan.TryParse(val, out ts))
				return ts;

			const string num_patn =
				@"(?:^|\s+)"+            // Start of the string or preceded by whitespace
				@"("+                    //
					@"[+-]?"+            // Optional + or - sign
					@"(?:"+              //
						@"(?:\d*\.\d+)"+ // ##.### or .####
						@"|(?:\d+)"+     // or ####
					@")"+                //
				@")\s*"+                 // trailed by optional whitespace
				@"({0})"+                // time unit patterns
				@"(?:$|\s)";             // End of the string or followed by whitespace

			// Extract parts
			var msec = val.SubstringRegex(num_patn.Fmt("msecs|msec|ms"), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'msecs', 'msec', or 'ms'
			var sec  = val.SubstringRegex(num_patn.Fmt("secs|sec|s"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'secs', 'sec', or 's'
			var min  = val.SubstringRegex(num_patn.Fmt("mins|min|m"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'mins', 'min', or 'm'
			var hrs  = val.SubstringRegex(num_patn.Fmt("hrs|hr|h"     ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'hrs', 'hr', 'h'
			var days = val.SubstringRegex(num_patn.Fmt("days|day|d"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'days', 'day', 'd'
			if (msec == null && sec == null && min == null && hrs == null && days == null)
				return null;

			// Build the time span from parts
			if (msec != null && double.TryParse(msec, out x)) ts += TimeSpan.FromMilliseconds(x);
			if (sec  != null && double.TryParse(sec , out x)) ts += TimeSpan.FromSeconds(x);
			if (min  != null && double.TryParse(min , out x)) ts += TimeSpan.FromMinutes(x);
			if (hrs  != null && double.TryParse(hrs , out x)) ts += TimeSpan.FromHours(x);
			if (days != null && double.TryParse(days, out x)) ts += TimeSpan.FromDays(x);
			return ts;
		}

		/// <summary>Labels for time units. Ordered by increasing duration, i.e. Seconds > Milliseconds</summary>
		public enum ETimeUnits
		{
			Milliseconds,
			Seconds,
			Minutes,
			Hours,
			Days,
		}

		/// <summary>Return the time span as a nice looking string</summary>
		public static string ToPrettyString(this TimeSpan ts, bool short_format = true, ETimeUnits min_unit = ETimeUnits.Seconds, ETimeUnits max_unit = ETimeUnits.Days, bool leading_zeros = false, bool trailing_zeros = true)
		{
			// Get each of the parts
			var d = max_unit > ETimeUnits.Days         ? ts.Days         : (long)ts.TotalDays;
			var h = max_unit > ETimeUnits.Hours        ? ts.Hours        : (long)ts.TotalHours;
			var m = max_unit > ETimeUnits.Minutes      ? ts.Minutes      : (long)ts.TotalMinutes;
			var s = max_unit > ETimeUnits.Seconds      ? ts.Seconds      : (long)ts.TotalSeconds;
			var f = max_unit > ETimeUnits.Milliseconds ? ts.Milliseconds : (long)ts.TotalMilliseconds;

			var unit_d = short_format ? "d"  : d != 1 ? "days"  : "day";
			var unit_h = short_format ? "h"  : h != 1 ? "hrs"   : "hr";
			var unit_m = short_format ? "m"  : m != 1 ? "mins"  : "min";
			var unit_s = short_format ? "s"  : s != 1 ? "secs"  : "sec";
			var unit_f = short_format ? "ms" : f != 1 ? "msecs" : "msec";

			// Show if: (unit in range [min_unit,max_unit])  and  ((value != 0) or (showing leading zeros) or (showing trailing zeros and high unit is shown) or (unit = min_unit and 'ts' is smaller than unit))
			var show_d = (min_unit <= ETimeUnits.Days         && ETimeUnits.Days         <= max_unit) && (d != 0 || leading_zeros                               || (min_unit == ETimeUnits.Days         && (long)ts.TotalDays         == 0));
			var show_h = (min_unit <= ETimeUnits.Hours        && ETimeUnits.Hours        <= max_unit) && (h != 0 || leading_zeros || (trailing_zeros && show_d) || (min_unit == ETimeUnits.Hours        && (long)ts.TotalHours        == 0));
			var show_m = (min_unit <= ETimeUnits.Minutes      && ETimeUnits.Minutes      <= max_unit) && (m != 0 || leading_zeros || (trailing_zeros && show_h) || (min_unit == ETimeUnits.Minutes      && (long)ts.TotalMinutes      == 0));
			var show_s = (min_unit <= ETimeUnits.Seconds      && ETimeUnits.Seconds      <= max_unit) && (s != 0 || leading_zeros || (trailing_zeros && show_m) || (min_unit == ETimeUnits.Seconds      && (long)ts.TotalSeconds      == 0));
			var show_f = (min_unit <= ETimeUnits.Milliseconds && ETimeUnits.Milliseconds <= max_unit) && (f != 0 || leading_zeros || (trailing_zeros && show_s) || (min_unit == ETimeUnits.Milliseconds && (long)ts.TotalMilliseconds == 0));

			return Str.Build(
				show_d ? "{0}{1} ".Fmt(d, unit_d) : string.Empty,
				show_h ? "{0}{1} ".Fmt(h, unit_h) : string.Empty,
				show_m ? "{0}{1} ".Fmt(m, unit_m) : string.Empty,
				show_s ? "{0}{1} ".Fmt(s, unit_s) : string.Empty,
				show_f ? "{0}{1} ".Fmt(f, unit_f) : string.Empty).TrimEnd(' ');
		}

		/// <summary>Return the approximate time for this time span using the least number of characters possible</summary>
		public static string ToMinimalString(this TimeSpan ts)
		{
			if (ts.TotalDays    > 1) return "{0}d".Fmt((int)ts.TotalDays);
			if (ts.TotalHours   > 1) return "{0}h".Fmt((int)ts.TotalHours);
			if (ts.TotalMinutes > 1) return "{0}m".Fmt((int)ts.TotalMinutes);
			if (ts.TotalSeconds > 1) return "{0}s".Fmt((int)ts.TotalSeconds);
			return "{0}ms".Fmt((int)ts.TotalMilliseconds);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestDateTimeExtensions
	{
		[Test] public void TestConversion()
		{
			var ofs     = DateTimeOffset.Now.Offset;
			var dto_utc = DateTimeOffset.UtcNow;
			var dto_loc = DateTimeOffset.Now;
			var dt_utc  = DateTime.UtcNow;
			var dt_loc  = DateTime.Now;
			var dt_unk  = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
			Assert.True(dto_utc.Offset == TimeSpan.Zero);
			Assert.True(dto_loc.Offset == ofs);
			Assert.True(dt_utc.Kind == DateTimeKind.Utc);
			Assert.True(dt_loc.Kind == DateTimeKind.Local);
			Assert.True(dt_unk.Kind == DateTimeKind.Unspecified);

			{// The Offset value in DTO is the difference between the DTO value and its value in UTC
				Assert.True(dto_utc.UtcTicks == dto_loc.UtcTicks);
			}

			// Conversion from DTO to DT leaves Kind as unspecified
			{
				var a = dto_utc.DateTime;      Assert.True(a.Kind == DateTimeKind.Unspecified);
				var b = dto_loc.DateTime;      Assert.True(b.Kind == DateTimeKind.Unspecified);
				var c = dto_utc.LocalDateTime; Assert.True(c.Kind == DateTimeKind.Local);
				var d = dto_loc.LocalDateTime; Assert.True(d.Kind == DateTimeKind.Local);
				var e = dto_utc.UtcDateTime;   Assert.True(e.Kind == DateTimeKind.Utc);
				var f = dto_loc.UtcDateTime;   Assert.True(f.Kind == DateTimeKind.Utc);
			}

			// Implicit conversion from DT to DTO sets Offset.
			// If DT.Kind is unspecified, the local time-zone offset is assumed
			{
				DateTimeOffset a = dt_utc; Assert.True(a.Offset == TimeSpan.Zero);
				DateTimeOffset b = dt_loc; Assert.True(b.Offset == ofs);
				DateTimeOffset c = dt_unk; Assert.True(c.Offset == ofs);
			}

		}
		[Test] public void TestClamp()
		{
			var min = new DateTimeOffset(2000,12,1,0,0,0,TimeSpan.Zero);
			var max = new DateTimeOffset(2000,12,31,0,0,0,TimeSpan.Zero);
			DateTimeOffset dt,DT;

			dt = new DateTimeOffset(2000,11,29,0,0,0,TimeSpan.Zero);
			DT = dt.Clamp(min,max);
			Assert.True(!Equals(dt,DT) && dt < min);
			Assert.True(DT >= min && DT <= max);

			dt = new DateTimeOffset(2000,12,20,0,0,0,TimeSpan.Zero);
			DT = dt.Clamp(min,max);
			Assert.True(Equals(dt,DT));
			Assert.True(DT >= min && DT <= max);

			dt = new DateTimeOffset(2000,12,31,23,59,59,TimeSpan.Zero);
			DT = dt.Clamp(min,max);
			Assert.True(!dt.Equals(DT) && dt > max);
			Assert.True(DT >= min && DT <= max);
		}
		[Test] public void TestTimeSpan()
		{
			const string expr_short = "1428d 21h 33m 9s";
			const string expr_long  = "1428days 21hrs 33mins 9secs";

			var ts1 = TimeSpan.FromSeconds(123456789);
			var s1 = ts1.ToPrettyString();
			Assert.AreEqual(s1, expr_short);

			var TS1 = TimeSpan_.TryParseExpr(expr_long);
			Assert.AreEqual(TS1, ts1);

			var ts2 = TimeSpan_.TryParseExpr("1d 12Hrs 1min 100secs 5msec");
			var TS2 = new TimeSpan(1, 12, 1, 100, 5);
			Assert.AreEqual(ts2, TS2);

			var ts3 = TimeSpan_.TryParseExpr("-2mins").Value;
			var s2 = ts3.ToPrettyString();
			var s3 = ts3.ToPrettyString(short_format:false, min_unit:TimeSpan_.ETimeUnits.Milliseconds, trailing_zeros:true);
			var s4 = ts3.ToPrettyString(short_format:false, min_unit:TimeSpan_.ETimeUnits.Milliseconds, trailing_zeros:false);
			Assert.AreEqual(s2, "-2m 0s");
			Assert.AreEqual(s3, "-2mins 0secs 0msecs");
			Assert.AreEqual(s4, "-2mins");

			var ts4 = TimeSpan_.TryParseExpr("0mins").Value;
			var s5 = ts4.ToPrettyString();
			Assert.AreEqual(s5, "0s");

			var ts5 = TimeSpan_.TryParseExpr("2ms").Value;
			var s6 = ts5.ToPrettyString();
			var s7 = ts5.ToPrettyString(min_unit:TimeSpan_.ETimeUnits.Milliseconds);
			Assert.AreEqual(s6, "0s");
			Assert.AreEqual(s7, "2ms");
		}
	}
}
#endif