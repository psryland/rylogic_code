using System;
using System.Windows.Forms;

namespace pr.extn
{
	public static class DateTimeExtensions
	{
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

		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTime Clamp(this DateTime time, DateTime min, DateTime max)
		{
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}

		/// <summary>Returns a new DateTimeOffset object clamped to within the given range</summary>
		public static DateTimeOffset Clamp(this DateTimeOffset time, DateTimeOffset min, DateTimeOffset max)
		{
			if (time < min) return min;
			if (time > max) return max;
			return time;
		}

		/// <summary>Set the MinDate, MaxDate, and Value members all at once, avoiding out of range exceptions.</summary>
		public static void Set(this DateTimePicker dtp, DateTime value, DateTime min, DateTime max)
		{
			if (min > max)
				throw new Exception("Minimum date/time value is greater than the maximum date/time value");
			if (value != value.Clamp(min,max))
				throw new Exception("Date/time value is not within the given range of date/time values");

			// Setting to MinimumDateTime/MaximumDateTime first avoids problems if min > MaxDate or max < MinDate
			dtp.MinDate = DateTimePicker.MinimumDateTime;
			dtp.MaxDate = DateTimePicker.MaximumDateTime;

			min = Clamp(min, DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);
			max = Clamp(max, DateTimePicker.MinimumDateTime, DateTimePicker.MaximumDateTime);

			// Setting Value before MinDate/MaxDate avoids setting Value twice when Value < MinDate or Value > MaxDate
			dtp.Value = Clamp(value, min, max);

			dtp.MinDate = min;
			dtp.MaxDate = max;
		}

		/// <summary>Sets the MinDate, MaxDate, and Value members all to Universal time.</summary>
		public static void ToUniversalTime(this DateTimePicker dtp)
		{
			var min = dtp.MinDate.ToUniversalTime();
			var max = dtp.MaxDate.ToUniversalTime();
			var val = dtp.Value.ToUniversalTime();
			dtp.Set(val, min, max);
		}

		/// <summary>Sets the MinDate, MaxDate, and Value members all to Local time.</summary>
		public static void ToLocalTime(this DateTimePicker dtp)
		{
			var min = dtp.MinDate.ToLocalTime();
			var max = dtp.MaxDate.ToLocalTime();
			var val = dtp.Value.ToLocalTime();
			dtp.Set(val, min, max);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestDateTimeExtensions
	{
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