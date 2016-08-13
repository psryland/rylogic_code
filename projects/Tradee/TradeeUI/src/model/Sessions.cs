using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.common;

namespace Tradee
{
	/// <summary>The trading sessions</summary>
	public class Sessions :IDisposable
	{
		// Forex market opens on Sunday 5 22:00 GMT, closes on Friday 22:00 GMT.
		private static readonly DayOfWeek OpenedDay = DayOfWeek.Sunday;
		private static readonly DayOfWeek ClosedDay = DayOfWeek.Friday;
		private static readonly TimeSpan OpenedTime = new TimeSpan(22,0,0);
		private static readonly TimeSpan ClosedTime = new TimeSpan(22,0,0);

		// Times UTC:
		// Europe:
		//   London:     08:00 to 17:00
		//   Frankfurt:  07:00 to 16:00
		// America:
		//   New York:   13:00 to 22:00
		//   Chicago:    14:00 to 23:00
		// Asia
		//   Tokyo:      00:00 to 09:00
		//   Hong Kong:  01:00 to 10:00
		// Pacific:
		//   Sydney:     22:00 to 07:00
		//   Wellington: 22:00 to 06:00

		public Sessions(MainModel model)
		{
			Model = model;
		}
		public virtual void Dispose()
		{
			Model = null;
		}

		/// <summary>The App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				m_model = value;
			}
		}
		private MainModel m_model;

		/// <summary>Returns time ranges that are within trading hours for the given time interval</summary>
		public IEnumerable<Range> ClipToTradingHours(Range time_range_utc)
		{
			var t0 = new DateTimeOffset(time_range_utc.Begin, TimeSpan.Zero);
			var t1 = new DateTimeOffset(time_range_utc.End  , TimeSpan.Zero);

			// Find the first opening or closing time before 't0'
			var prev_open  = PrecedingDate(t0, OpenedDay, OpenedTime);
			var prev_close = PrecedingDate(t0, ClosedDay, ClosedTime);

			// True if 't0' is within an open trading period
			var open = prev_open > prev_close;
			for (var t = t0; t < t1;)
			{
				var next = open ? NextDate(t, ClosedDay, ClosedTime) : NextDate(t, OpenedDay, OpenedTime);
				if (open)
				{
					var r = new Range(t.Ticks, Math.Min(next.Ticks, t1.Ticks));
					Debug.Assert(r.Size != 0);
					yield return r;
				}
				open = !open;
				t = next;
			}
		}

		/// <summary>Return the next 'week_day' at 'time_of_day' that follows 'dt'</summary>
		private DateTimeOffset NextDate(DateTimeOffset dt, DayOfWeek week_day, TimeSpan time_of_day)
		{
			// Set 'dt.TimeOfDay' forward to the target 'time_of_day'
			if (dt.TimeOfDay > time_of_day)
				dt += (TimeSpan.FromDays(1) - dt.TimeOfDay) + time_of_day;
			else
				dt += time_of_day - dt.TimeOfDay;

			// Set the week day to the next 'week_day'
			if (dt.DayOfWeek > week_day)
				dt = dt.AddDays((7 - (int)dt.DayOfWeek) + (int)week_day);
			else
				dt = dt.AddDays((int)week_day - (int)dt.DayOfWeek);

			return dt;
		}

		/// <summary>Return the preceding 'week_day' at 'time_of_day' that occurs before 'dt'</summary>
		private DateTimeOffset PrecedingDate(DateTimeOffset dt, DayOfWeek week_day, TimeSpan time_of_day)
		{
			// Set 'dt.TimeOfDay' back to the target 'time_of_day'
			if (dt.TimeOfDay < time_of_day)
				dt -= dt.TimeOfDay + (TimeSpan.FromDays(1) - time_of_day);
			else
				dt -= dt.TimeOfDay - time_of_day;

			// Set the week day to the preceding 'week_day'
			if (dt.DayOfWeek < week_day)
				dt = dt.AddDays(-((int)dt.DayOfWeek + (7 - (int)week_day)));
			else
				dt = dt.AddDays(-((int)dt.DayOfWeek - (int)week_day));

			return dt;
		}
	}
}
