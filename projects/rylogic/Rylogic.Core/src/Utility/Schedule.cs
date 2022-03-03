using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Rylogic.Extn;

namespace Rylogic.Utility
{
	[DebuggerDisplay("{Description,nq}")]
	public class Schedule
	{
		// Notes:
		//  - A schedule is a collection of one-off or repeating time ranges.
		//  - The schedule is not sorted because there is little point when events are repeating.
		//  - Remember there can be multiple events at the same time point.

		// Todo: make a schedule user control, basically a table to add/remove ranges

		public Schedule()
		{
			m_events = new List<Event>();
			m_ranges = new List<Range>();
		}
		
		/// <summary>The time-points in the schedule</summary>
		public IReadOnlyList<Event> Events => m_events;
		private List<Event> m_events;

		/// <summary>The entries in the schedule.</summary>
		public IReadOnlyList<Range> Ranges => m_ranges;
		private List<Range> m_ranges;

		/// <summary>Return the events in order after 'time'. Note: this is an infinite series if the schedule contains repeats</summary>
		public IEnumerable<Event> NextEvents(DateTimeOffset? time = null)
		{
			var now = time ?? DateTimeOffset.Now;
			for (; now != DateTimeOffset.MaxValue;)
			{
				var after = now + new TimeSpan(1);
				var next = DateTimeOffset.MaxValue;
				foreach (var evt in m_events)
				{
					// Return all the events that occur at 'now'
					if (evt.Next(now) == now)
						yield return new Event(evt, now);

					// Find the next event to occur after 'now'
					var when = evt.Next(after);
					if (when >= after && when < next)
						next = when;
				}

				// Set the time to the next event time after 'now' and repeat
				now = next;
			}
		}

		/// <summary>Return all ranges that are active at 'time'</summary>
		public IEnumerable<Range> ActiveRanges(DateTimeOffset? time = null)
		{
			var now = time ?? DateTimeOffset.Now;
			foreach (var range in m_ranges)
			{
				// When does the range next end after 'now'?
				var end = range.End.Next(now);
				
				// When does the range first begin before 'end'
				var beg = range.Beg.Prev(end);

				// If the start and end of the range span 'now', return it.
				if (now >= beg && now < end)
					yield return range;
			}
		}

		/// <summary>Reset the schedule to empty</summary>
		public void Clear()
		{
			m_events.Clear();
			m_ranges.Clear();
			ScheduleChanged?.Invoke(this, new ScheduleChangedEventArgs(ScheduleChangedEventArgs.EChg.Cleared, null));
		}

		/// <summary>Add a time range to the schedule</summary>
		public void Add(Range sched)
		{
			m_ranges.Add(sched);
			m_events.Add(sched.Beg);
			m_events.Add(sched.End);
			ScheduleChanged?.Invoke(this, new ScheduleChangedEventArgs(ScheduleChangedEventArgs.EChg.Added, sched));
		}

		/// <summary>Remove a time range from the schedule</summary>
		public void Remove(Range sched)
		{
			m_ranges.Remove(sched);
			m_events.RemoveAll(x => x.Owner == sched);
			ScheduleChanged?.Invoke(this, new ScheduleChangedEventArgs(ScheduleChangedEventArgs.EChg.Removed, sched));
		}

		/// <summary>Raised when a time range is added or removed from the schedule</summary>
		public event EventHandler<ScheduleChangedEventArgs>? ScheduleChanged;

		/// <summary></summary>
		public string Description => string.Join("\n", Ranges.Select(x => x.Description));

		/// <summary>Schedule event type</summary>
		public enum EEventType
		{
			Beg,
			End,
		}

		/// <summary>The repeat types for repeating events</summary>
		public enum ERepeat
		{
			OneOff,
			Minutely,
			Hourly,
			Daily,
			Weekly,
			Monthly,
			Yearly,
		}

		/// <summary>A single event in the schedule</summary>
		[DebuggerDisplay("{Description,nq}")]
		public struct Event :IComparable<Event>, IComparable<DateTimeOffset>
		{
			public Event(EEventType type, ERepeat repeat, DateTimeOffset time, Range? owner)
			{
				Type = type;
				Repeat = repeat;
				Time = time;
				Owner = owner;
			}
			public Event(Event rhs, DateTimeOffset time)
			{
				Type = rhs.Type;
				Repeat = rhs.Repeat;
				Time = time;
				Owner = rhs.Owner;
			}

			/// <summary>Event type</summary>
			public EEventType Type { get; }

			/// <summary>Repeating event type</summary>
			public ERepeat Repeat { get; }

			/// <summary>When the event occurs</summary>
			public DateTimeOffset Time { get; }

			/// <summary>When the event next occurs after 'time'</summary>
			public DateTimeOffset Next(DateTimeOffset? time = null)
			{
				var now = time ?? DateTimeOffset.Now;
				switch (Repeat)
				{
					case ERepeat.OneOff:
					{
						return Time;
					}
					case ERepeat.Minutely:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddMinutes(1);
						return when;
					}
					case ERepeat.Hourly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddHours(1);
						return when;
					}
					case ERepeat.Daily:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddDays(1);
						return when;
					}
					case ERepeat.Weekly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddDays(7);
						return when;
					}
					case ERepeat.Monthly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddMonths(1);
						return when;
					}
					case ERepeat.Yearly:
					{
						var when = new DateTimeOffset(now.Year, Time.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when < now) when = when.AddYears(1);
						return when;
					}
					default:
					{
						throw new Exception($"Unknown repeat type: {Repeat}");
					}
				}
			}

			/// <summary>When the event last occurred before 'time'</summary>
			public DateTimeOffset Prev(DateTimeOffset? time = null)
			{
				var now = time ?? DateTimeOffset.Now;
				switch (Repeat)
				{
					case ERepeat.OneOff:
					{
						return Time;
					}
					case ERepeat.Minutely:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddMinutes(-1);
						return when;
					}
					case ERepeat.Hourly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddHours(-1);
						return when;
					}
					case ERepeat.Daily:
					{
						var when = new DateTimeOffset(now.Year, now.Month, now.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddDays(-1);
						return when;
					}
					case ERepeat.Weekly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddDays(-7);
						return when;
					}
					case ERepeat.Monthly:
					{
						var when = new DateTimeOffset(now.Year, now.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddMonths(-1);
						return when;
					}
					case ERepeat.Yearly:
					{
						var when = new DateTimeOffset(now.Year, Time.Month, Time.Day, Time.Hour, Time.Minute, Time.Second, Time.Millisecond, now.Offset);
						if (when >= now) when = when.AddYears(-1);
						return when;
					}
					default:
					{
						throw new Exception($"Unknown repeat type: {Repeat}");
					}
				}
			}

			/// <summary>The object that this event is associated with</summary>
			public Range? Owner { get; }

			/// <summary>Wait for this event after 'time'. Adding, Removing, or sorting the schedule cancels this task</summary>
			public async Task Wait(CancellationToken cancel, DateTimeOffset? time = null)
			{
				var now = time ?? DateTimeOffset.Now;

				// Block until the event comes due
				var time_till_event = Next(now) - now;
				if (time_till_event <= TimeSpan.Zero)
					return;

				// Wait until the event is due
				await Task.Delay(time_till_event, cancel);
			}

			/// <summary></summary>
			public int CompareTo(DateTimeOffset other)
			{
				return Next().CompareTo(other);
			}
			public int CompareTo(Event other)
			{
				return Next().CompareTo(other.Next());
			}

			/// <summary></summary>
			public string Description => Repeat switch
			{
				ERepeat.OneOff   => $"{Type} {Time}",
				ERepeat.Minutely => $"{Type} {Time:ss} ({Repeat})",
				ERepeat.Hourly   => $"{Type} {Time:mm:ss} ({Repeat})",
				ERepeat.Daily    => $"{Type} {Time:HH:mm} ({Repeat})",
				ERepeat.Weekly   => $"{Type} {Time:ddd HH:mm} ({Repeat})",
				ERepeat.Monthly  => $"{Type} {Time:dd HH:mm} ({Repeat})",
				ERepeat.Yearly   => $"{Type} {Time:MMM dd HH:mm} ({Repeat})",
				_                => $"{Type} {Time} ({Repeat})",
			};
		}

		/// <summary>An entry in a schedule</summary>
		[DebuggerDisplay("{Description,nq}")]
		public class Range
		{
			// Notes:
			//  - Ranges are [Beg, End), so if beg == end, then the range is never active but two events are added to the schedule.

			public Range(string name, ERepeat repeat, DateTimeOffset beg, DateTimeOffset end)
			{
				if (beg > end)
					throw new ArgumentException("Range times must describe a positive time interval");

				Name = name;
				Beg = new Event(EEventType.Beg, repeat, beg, this);
				End = new Event(EEventType.End, repeat, end, this);
			}

			/// <summary>A range to label the range</summary>
			public string Name { get; }

			/// <summary>The event that is the start of the range</summary>
			public Event Beg { get; }

			/// <summary>The event that is the end of the range</summary>
			public Event End { get; }

			/// <summary>Repeating event type</summary>
			public ERepeat Repeat => Beg.Repeat;

			/// <summary>The interval spanned by the time range</summary>
			public TimeSpan Span => End.Time - Beg.Time;

			/// <summary></summary>
			public string Description => $"{Beg.Description} -> {End.Description}";
		}

		#region EventArgs
		public class ScheduleChangedEventArgs :EventArgs
		{
			public ScheduleChangedEventArgs(EChg chg, Range? range)
			{
				Change = chg;
				Range = range;
			}

			/// <summary>The type of change to the schedule</summary>
			public EChg Change { get; }
			public enum EChg
			{
				Cleared,
				Added,
				Removed,
			}

			/// <summary>The range that was added/removed</summary>
			public Range? Range { get; }
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture]
	public class TestSchedule
	{
		[Test]
		public void ScheduleTest()
		{
			var sched = new Schedule();

			var t1200 = new DateTimeOffset(2000, 1, 5, 12, 0, 0, 0, TimeSpan.Zero); // 2000-01-05 12:00
			var t1300 = new DateTimeOffset(2000, 1, 6, 13, 0, 0, 0, TimeSpan.Zero); // 2000-01-06 13:00
			var oneoff = new Schedule.Range("oneoff", Schedule.ERepeat.OneOff, t1200, t1300);
			sched.Add(oneoff);

			var t0230 = new DateTimeOffset(1, 1, 1, 2, 30, 0, TimeSpan.Zero); // From 02:30
			var t0315 = new DateTimeOffset(1, 1, 1, 3, 15, 0, TimeSpan.Zero); // to   03:15
			var daily = new Schedule.Range("daily", Schedule.ERepeat.Daily, t0230, t0315);
			sched.Add(daily);

			var now0 = new DateTimeOffset(2000, 1, 4, 0, 0, 0, TimeSpan.Zero); // 2000-01-04 00:00
			var next0 = sched.NextEvents(now0).Select(x => x.Time.ToString("dd HH:mm")).Take(10).ToList();
			Assert.True(next0.SequenceEqual(new[]
			{
				"04 02:30",
				"04 03:15",
				"05 02:30",
				"05 03:15",
				"05 12:00",
				"06 02:30",
				"06 03:15",
				"06 13:00",
				"07 02:30",
				"07 03:15",
			}));

			var now1 = new DateTimeOffset(2000, 1, 6, 0, 0, 0, TimeSpan.Zero); // 2000-01-06 00:00
			var next1 = sched.NextEvents(now1).Select(x => x.Time.ToString("dd HH:mm")).Take(10).ToList();
			Assert.True(next1.SequenceEqual(new[]
			{
				"06 02:30",
				"06 03:15",
				"06 13:00",
				"07 02:30",
				"07 03:15",
				"08 02:30",
				"08 03:15",
				"09 02:30",
				"09 03:15",
				"10 02:30",
			}));

			var now2 = new DateTimeOffset(2000, 1, 6, 3, 0, 0, TimeSpan.Zero); // 2000-01-06 00:00
			var active2 = sched.ActiveRanges(now2).ToList();
			Assert.True(active2.Count == 2);
			Assert.True(active2.SequenceEqualUnordered(new[] { oneoff, daily }));

			var now3 = new DateTimeOffset(2000, 1, 6, 4, 0, 0, TimeSpan.Zero); // 2000-01-06 04:00
			var active3 = sched.ActiveRanges(now3).ToList();
			Assert.True(active3.Count == 1);
			Assert.True(active3.SequenceEqualUnordered(new[] { oneoff }));

			var now4 = new DateTimeOffset(2000, 1, 6, 13, 0, 0, TimeSpan.Zero); // 2000-01-06 13:00
			var active4 = sched.ActiveRanges(now4).ToList();
			Assert.True(active4.Count == 0);
		}
	}
}
#endif
