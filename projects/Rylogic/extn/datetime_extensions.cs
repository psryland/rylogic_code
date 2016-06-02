using System;
using System.Globalization;
using System.Windows.Forms;

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
		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTime DateTime(this DateTimeOffset time, DateTimeKind kind)
		{
			return time.DateTime.As(kind);
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
	}
}
#endif