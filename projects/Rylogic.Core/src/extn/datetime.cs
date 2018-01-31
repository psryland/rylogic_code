using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rylogic.Extn
{
	public static class DateTime_
	{
		// Notes:
		//  If a DateTime has kind == unspecified, calling ToUniversalTime() causes it to assume that the time is
		//  local and returns the time converted to UTC. Calling ToLocalTime() causes it to assume that the time
		//  is UTC and returns the time converted to Local.
		//
		// Rule of thumb: Don't use 'ToLocalTime' or 'ToUniversalTime', use 'To' instead

		/// <summary>Time start from Unix-land</summary>
		public static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		/// <summary>Converts this date time to 'kind'. Note: throws if Kind == Unspecified</summary>
		public static DateTime To(this DateTime dt, DateTimeKind kind)
		{
			// Prefer calling this method instead of 'ToLocalTime' or 'ToUniversalTime' directly
			if (dt.Kind == DateTimeKind.Unspecified) throw new Exception($"Cannot convert an unspecified DateTime to {kind}");
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
			return DateTime.TryParseExact(val, format, null, style, out var result) ? (DateTime?)result : null;
		}
	}

	public static class DateTimeOffset_
	{
		/// <summary>Time start from Unix-land</summary>
		public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
	}

	public static class TimeSpan_
	{
		/// <summary>The total years represented</summary>
		public static double TotalYears(this TimeSpan ts)
		{
			return ts.TotalDays / 365.0;
		}

		/// <summary>The total weeks represented</summary>
		public static double TotalWeeks(this TimeSpan ts)
		{
			return ts.TotalDays / 7.0;
		}

		/// <summary>The years value of this time span</summary>
		public static long Years(this TimeSpan ts)
		{
			return (long)ts.TotalYears();
		}

		/// <summary>The weeks value of this time span</summary>
		public static long Weeks(this TimeSpan ts)
		{
			var x = 365*ts.TotalYears() - 365*ts.Years();
			return (long)(x / 7);
		}

		/// <summary>The days value of this time span (use when displaying years/weeks as well)</summary>
		public static long Days(this TimeSpan ts)
		{
			var x = 365*ts.TotalYears() - 365*ts.Years() - 7*ts.Weeks();
			return (long)(x / 1);
		}

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
			return TimeSpan.TryParse(s, out var ts) ? (TimeSpan?)ts : null;
		}
		public static TimeSpan? TryParseExact(string s, string fmt)
		{
			return TimeSpan.TryParseExact(s, fmt, null, out var ts) ? (TimeSpan?)ts : null;
		}

		/// <summary>
		/// Parse expressions representing time values<para/>
		/// E.g. 10 (seconds implied), 2:30 (2 minutes, 30 seconds), 1:3:10 (1 hour, 3 mins, 10 secs),<para/>
		///  10sec, 2min, 1 hr, 2 min 30sec, 2.5 min (2:30), 30s 10m 1h (01:10:30)</summary>
		public static TimeSpan? TryParseExpr(string val, ETimeUnits default_units = ETimeUnits.Seconds)
		{
			try
			{
				// If the value parses as a double, treat the number as 'default_units'
				if (double.TryParse(val, out var x))
				{
					switch (default_units)
					{
					default: throw new Exception($"unknown time units {default_units}");
					case ETimeUnits.Milliseconds: return TimeSpan.FromMilliseconds(x);
					case ETimeUnits.Seconds: return TimeSpan.FromSeconds(x);
					case ETimeUnits.Minutes: return TimeSpan.FromMinutes(x);
					case ETimeUnits.Hours: return TimeSpan.FromHours(x);
					case ETimeUnits.Days: return TimeSpan.FromDays(x);
					case ETimeUnits.Weeks: return TimeSpan.FromDays(x*7);
					case ETimeUnits.Years: return TimeSpan.FromDays(x*365);
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
				var msec  = val.SubstringRegex(string.Format(num_patn, "msecs|msec|ms"), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'msecs', 'msec', or 'ms'
				var sec   = val.SubstringRegex(string.Format(num_patn, "secs|sec|s"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'secs', 'sec', or 's'
				var min   = val.SubstringRegex(string.Format(num_patn, "mins|min|m"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'mins', 'min', or 'm'
				var hrs   = val.SubstringRegex(string.Format(num_patn, "hrs|hr|h"     ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'hrs', 'hr', 'h'
				var days  = val.SubstringRegex(string.Format(num_patn, "days|day|d"   ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'days', 'day', 'd'
				var weeks = val.SubstringRegex(string.Format(num_patn, "weeks|week|w" ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'weeks', 'week', 'w'
				var years = val.SubstringRegex(string.Format(num_patn, "years|year|y" ), RegexOptions.IgnoreCase).FirstOrDefault(); // A decimal value followed by 'years', 'year', 'y'
				if (msec == null && sec == null && min == null && hrs == null && days == null && weeks == null && years == null)
					return null;

				// Build the time span from parts
				if (msec  != null && double.TryParse(msec , out x)) ts += TimeSpan.FromMilliseconds(x);
				if (sec   != null && double.TryParse(sec  , out x)) ts += TimeSpan.FromSeconds(x);
				if (min   != null && double.TryParse(min  , out x)) ts += TimeSpan.FromMinutes(x);
				if (hrs   != null && double.TryParse(hrs  , out x)) ts += TimeSpan.FromHours(x);
				if (days  != null && double.TryParse(days , out x)) ts += TimeSpan.FromDays(x);
				if (weeks != null && double.TryParse(weeks, out x)) ts += TimeSpan.FromDays(7*x);
				if (years != null && double.TryParse(years, out x)) ts += TimeSpan.FromDays(365*x);
				// Note: Not accounting for leap years because there's no way to know if the time span crosses a leap year
				// (e.g 2y might span a leap year). Users will have to add additional days to the offset as needed

				return ts;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>Labels for time units. Ordered by increasing duration, i.e. Seconds > Milliseconds</summary>
		public enum ETimeUnits
		{
			Milliseconds,
			Seconds,
			Minutes,
			Hours,
			Days,
			Weeks,
			Years,
		}

		/// <summary>Return the time span as a nice looking string</summary>
		public static string ToPrettyString(this TimeSpan ts, bool short_format = true, ETimeUnits min_unit = ETimeUnits.Seconds, ETimeUnits max_unit = ETimeUnits.Days, bool leading_zeros = false, bool trailing_zeros = true)
		{
			// Get each of the parts
			var y = max_unit > ETimeUnits.Years        ? ts.Years()      : (long)ts.TotalYears();
			var w = max_unit > ETimeUnits.Weeks        ? ts.Weeks()      : (long)ts.TotalWeeks();
			var d = max_unit > ETimeUnits.Days         ? ts.Days()       : (long)ts.TotalDays;
			var h = max_unit > ETimeUnits.Hours        ? ts.Hours        : (long)ts.TotalHours;
			var m = max_unit > ETimeUnits.Minutes      ? ts.Minutes      : (long)ts.TotalMinutes;
			var s = max_unit > ETimeUnits.Seconds      ? ts.Seconds      : (long)ts.TotalSeconds;
			var f = max_unit > ETimeUnits.Milliseconds ? ts.Milliseconds : (long)ts.TotalMilliseconds;

			var unit_y = short_format ? "y"  : y != 1 ? "years" : "year";
			var unit_w = short_format ? "w"  : w != 1 ? "weeks" : "week";
			var unit_d = short_format ? "d"  : d != 1 ? "days"  : "day";
			var unit_h = short_format ? "h"  : h != 1 ? "hrs"   : "hr";
			var unit_m = short_format ? "m"  : m != 1 ? "mins"  : "min";
			var unit_s = short_format ? "s"  : s != 1 ? "secs"  : "sec";
			var unit_f = short_format ? "ms" : f != 1 ? "msecs" : "msec";

			// Show if: (unit in range [min_unit,max_unit])  and  ((value != 0) or (showing leading zeros) or (showing trailing zeros and high unit is shown) or (unit = min_unit and 'ts' is smaller than unit))
			var show_y = (min_unit <= ETimeUnits.Years        && ETimeUnits.Years        <= max_unit) && (y != 0 || leading_zeros                               || (min_unit == ETimeUnits.Years        && (long)(ts.TotalDays / 365.0) == 0));
			var show_w = (min_unit <= ETimeUnits.Weeks        && ETimeUnits.Weeks        <= max_unit) && (w != 0 || leading_zeros                               || (min_unit == ETimeUnits.Weeks        && (long)(ts.TotalDays / 7.0)   == 0));
			var show_d = (min_unit <= ETimeUnits.Days         && ETimeUnits.Days         <= max_unit) && (d != 0 || leading_zeros                               || (min_unit == ETimeUnits.Days         && (long)ts.TotalDays           == 0));
			var show_h = (min_unit <= ETimeUnits.Hours        && ETimeUnits.Hours        <= max_unit) && (h != 0 || leading_zeros || (trailing_zeros && show_d) || (min_unit == ETimeUnits.Hours        && (long)ts.TotalHours          == 0));
			var show_m = (min_unit <= ETimeUnits.Minutes      && ETimeUnits.Minutes      <= max_unit) && (m != 0 || leading_zeros || (trailing_zeros && show_h) || (min_unit == ETimeUnits.Minutes      && (long)ts.TotalMinutes        == 0));
			var show_s = (min_unit <= ETimeUnits.Seconds      && ETimeUnits.Seconds      <= max_unit) && (s != 0 || leading_zeros || (trailing_zeros && show_m) || (min_unit == ETimeUnits.Seconds      && (long)ts.TotalSeconds        == 0));
			var show_f = (min_unit <= ETimeUnits.Milliseconds && ETimeUnits.Milliseconds <= max_unit) && (f != 0 || leading_zeros || (trailing_zeros && show_s) || (min_unit == ETimeUnits.Milliseconds && (long)ts.TotalMilliseconds   == 0));

			var sb = new StringBuilder();
			if (show_y) sb.Append($"{y}{unit_y} ");
			if (show_w) sb.Append($"{w}{unit_w} ");
			if (show_d) sb.Append($"{d}{unit_d} ");
			if (show_h) sb.Append($"{h}{unit_h} ");
			if (show_m) sb.Append($"{m}{unit_m} ");
			if (show_s) sb.Append($"{s}{unit_s} ");
			if (show_f) sb.Append($"{f}{unit_f} ");
			return sb.ToString().TrimEnd(' ');
		}

		/// <summary>Return the approximate time for this time span using the least number of characters possible</summary>
		public static string ToMinimalString(this TimeSpan ts)
		{
			if (ts.TotalDays    > 1) return $"{(int)ts.TotalDays   }d";
			if (ts.TotalHours   > 1) return $"{(int)ts.TotalHours  }h";
			if (ts.TotalMinutes > 1) return $"{(int)ts.TotalMinutes}m";
			if (ts.TotalSeconds > 1) return $"{(int)ts.TotalSeconds}s";
			return $"{(int)ts.TotalMilliseconds}ms";
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;
	using Maths;

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
				Assert.True(dto_utc.UtcTicks - dto_loc.UtcTicks < TimeSpan.FromSeconds(1).Ticks);
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

			{
				var ts = TimeSpan.FromSeconds(123456789);
				var s = ts.ToPrettyString();
				Assert.AreEqual(s, expr_short);

				var TS = TimeSpan_.TryParseExpr(expr_long);
				Assert.AreEqual(TS, ts);
			}
			{
				var ts = TimeSpan_.TryParseExpr("1d 12Hrs 1min 100secs 5msec");
				var TS = new TimeSpan(1, 12, 1, 100, 5);
				Assert.AreEqual(ts, TS);
			}
			{
				var ts = TimeSpan_.TryParseExpr("-2mins").Value;
				var s1 = ts.ToPrettyString();
				var s2 = ts.ToPrettyString(short_format:false, min_unit:TimeSpan_.ETimeUnits.Milliseconds, trailing_zeros:true);
				var s3 = ts.ToPrettyString(short_format:false, min_unit:TimeSpan_.ETimeUnits.Milliseconds, trailing_zeros:false);
				Assert.AreEqual(s1, "-2m 0s");
				Assert.AreEqual(s2, "-2mins 0secs 0msecs");
				Assert.AreEqual(s3, "-2mins");
			}
			{
				var ts = TimeSpan_.TryParseExpr("0mins").Value;
				var s1 = ts.ToPrettyString();
				Assert.AreEqual(s1, "0s");
			}
			{
				var ts = TimeSpan_.TryParseExpr("2ms").Value;
				var s1 = ts.ToPrettyString();
				var s2 = ts.ToPrettyString(min_unit:TimeSpan_.ETimeUnits.Milliseconds);
				Assert.AreEqual(s1, "0s");
				Assert.AreEqual(s2, "2ms");
			}
			{
				var ts = TimeSpan_.TryParseExpr("10y 25w 7d").Value;
				var s1 = ts.ToPrettyString(trailing_zeros:false);
				var s2 = ts.ToPrettyString(max_unit:TimeSpan_.ETimeUnits.Years, trailing_zeros:false);
				Assert.True(Math_.FEql(ts.TotalDays, 10*365 + 25*7 + 7));
				Assert.AreEqual(s1, "3832d");
				Assert.AreEqual(s2, "10y 26w");
			}
			{
				var ts = TimeSpan_.TryParseExpr("-10y 25w 7d").Value;
				var s1 = ts.ToPrettyString(trailing_zeros:false);
				var s2 = ts.ToPrettyString(max_unit:TimeSpan_.ETimeUnits.Years, trailing_zeros:false);
				Assert.True(Math_.FEql(ts.TotalDays, -10*365 + 25*7 + 7));
				Assert.AreEqual(s1, "-3468d");
				Assert.AreEqual(s2, "-9y -26w -1d");
			}
		}
	}
}
#endif