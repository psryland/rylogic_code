using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Rylogic.Extn;

namespace Rylogic.Common
{
	public static class EventsSnapshot
	{
		[Flags] public enum Restore
		{
			RemoveAdded    = 1,
			AddRemoved     = 2,
			Both           = RemoveAdded|AddRemoved,
			AssertNoChange = 4 | Both
		}

		/// <summary>Capture the state of the events on 'obj'</summary>
		public static EventsState<T> Capture<T>(T obj, Restore restore = Restore.Both)
		{
			return new EventsState<T>(obj, restore);
		}

		/// <summary>Records and restores the state of multicast delegates on an object</summary>
		public class EventsState<T> :IDisposable
		{
			private readonly Dictionary<FieldInfo,Delegate[]> m_events = new Dictionary<FieldInfo,Delegate[]>();
			private readonly BindingFlags m_binding_flags;
			private readonly Restore m_restore;
			private readonly T m_obj;

			/// <summary>
			/// Reflects on all events of 'obj' and records the states of their invocation lists.
			/// When disposed, the EventsSnapshot restores the invocation list to what it was at the time of capture.
			/// Warning: the order of event handlers is not preserved.</summary>
			public EventsState(T obj, Restore restore = Restore.Both, BindingFlags binding_flags = BindingFlags.Static|BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
			{
				if (obj == null)
					throw new ArgumentNullException(nameof(obj));

				m_obj = obj;
				m_restore = restore;
				m_binding_flags = binding_flags;
				var type = m_obj.GetType();

				var fields = type.AllFields(m_binding_flags).ToDictionary(x => x.Name);
				var events = type.AllEvents(m_binding_flags).ToList();
				foreach (var evt in events)
				{
					if (!fields.TryGetValue(evt.Name, out var field))
						continue;

					// Get the instance of the multicast delegate from 'obj' and save the invocation list
					if (field.GetValue(obj) is MulticastDelegate mcd)
					{
						var delegates = mcd.GetInvocationList();
						m_events.Add(field, delegates);
					}
					else
					{
						m_events.Add(field, new Delegate[0]);
					}
				}
			}
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
			protected virtual void Dispose(bool _)
			{
				// Restore the delegates
				var type = m_obj!.GetType();
				foreach (var evt in m_events)
				{
					var field = evt.Key;
					var mcd = field.GetValue(m_obj) as MulticastDelegate;

					var old_delegates = evt.Value;
					var new_delegates = mcd != null ? mcd.GetInvocationList() : new Delegate[0];

					var event_info = type.GetEvent(field.Name, m_binding_flags);
					if (event_info == null) throw new Exception($"Failed to find event {field.Name} on {type.Name}");

					if ((m_restore & Restore.RemoveAdded) == Restore.RemoveAdded)
					{
						// Get the set of added delegates (allowing for duplicates)
						var added = new List<Delegate>(new_delegates);
						foreach (var d in old_delegates)
							added.Remove(d);

						if ((m_restore & Restore.AssertNoChange) == Restore.AssertNoChange)
						{
							// Report delegates that have been added
							if (added.Count != 0)
							{
								var added_names = string.Join("\n", added.Select(x => $"{x.Target}.{x.Method.Name}"));
								throw new Exception($"Event {type.Name}.{evt.Key.Name} has had handlers added:\n{added_names}");
							}
						}
						else
						{
							// Remove any that were added
							var remove = event_info.GetRemoveMethod(true);
							if (remove == null) throw new Exception($"Remove method not found");
							foreach (var d in added)
								remove.Invoke(m_obj, new object[]{d});
						}
					}
					if ((m_restore & Restore.AddRemoved) == Restore.AddRemoved)
					{
						// Get the set of delegates that have been removed
						var removed = new List<Delegate>(old_delegates);
						foreach (var d in new_delegates)
							removed.Remove(d);

						if ((m_restore & Restore.AssertNoChange) == Restore.AssertNoChange)
						{
							// Report delegates that have been removed
							if (removed.Count != 0)
							{
								var removed_names = string.Join("\n", removed.Select(x => $"{x.Target.ToString()}.{x.Method.Name}"));
								throw new Exception($"Event {type.Name}.{evt.Key.Name} has had handlers removed:\n{removed_names}");
							}
						}
						else
						{
							// Add any that were removed
							var add = event_info.GetAddMethod(true);
							if (add == null) throw new Exception("Add method not found");
							foreach (var d in removed)
								add.Invoke(m_obj, new object[]{d});
						}
					}
				}
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestEventSnapshot
	{
		private class TestBase
		{
			protected event EventHandler<Args>? Event2;
			public class Args :EventArgs {};

			public int Event2HandlerCount => Event2 != null ? Event2.GetInvocationList().Length : 0;

			public virtual void ResetHandlers()
			{
				Event2 = null;
			}
			public virtual void RaiseEvents()
			{
				if (Event2 != null) Event2(this, new Args());
			}
		}
		private sealed class Test :TestBase
		{
			public static string Result = string.Empty;

			public Action? Ignored;
			public event EventHandler? Event1;
			public static event EventHandler? Event3;

			public int Event1HandlerCount { get { return Event1 != null ? Event1.GetInvocationList().Length : 0; } }
			public int Event3HandlerCount { get { return Event3 != null ? Event3.GetInvocationList().Length : 0; } }

			public Test()
			{
				Result = string.Empty;
				Ignored += () => { throw new Exception(); };
				ResetHandlers();
			}
			public override void ResetHandlers()
			{
				Event1 = null;
				base.ResetHandlers();
				Event3 = null;
			}
			public override void RaiseEvents()
			{
				if (Event1 != null) Event1(this, EventArgs.Empty);
				Result += "-";
				base.RaiseEvents();
				Result += "-";
				if (Event3 != null) Event3(this, EventArgs.Empty);
			}
			public void AttachHandlerToEvent2() { Event2 += Handler; }
			public void AttachHandlerToEvent3() { Event3 += Handler; }
			public void Handler(object sender, EventArgs args) { Result += "A"; }
		}

		[Test] public void TestRemoveAdded()
		{
			var test = new Test();

			EventHandler handler = (s,a) => Test.Result += "B";

			test.Event1 += handler;
			test.Event1 += handler;
			test.Event1 += test.Handler;
			test.Event1 += (s,a) => Test.Result += "C";
			Test.Event3 += test.Handler;
			test.RaiseEvents();

			Assert.Equal(4, test.Event1HandlerCount);
			Assert.Equal(0, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BBAC--A", Test.Result);
			Test.Result = string.Empty;

			using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.RemoveAdded))
			{
				test.Event1 += (s,a) => Test.Result += "D";
				test.Event1 += test.Handler;
				test.Event1 += handler;
				test.AttachHandlerToEvent2();
				test.AttachHandlerToEvent3();
				test.RaiseEvents();
				Assert.Equal(7, test.Event1HandlerCount);
				Assert.Equal(1, test.Event2HandlerCount);
				Assert.Equal(2, test.Event3HandlerCount);
				Assert.Equal("BBACDAB-A-AA", Test.Result);
				Test.Result = string.Empty;

				test.Event1 -= handler;
				test.Event1 -= handler;
				test.AttachHandlerToEvent2();
				test.AttachHandlerToEvent3();
				test.RaiseEvents();
				Assert.Equal(5, test.Event1HandlerCount);
				Assert.Equal(2, test.Event2HandlerCount);
				Assert.Equal(3, test.Event3HandlerCount);
				Assert.Equal("BACDA-AA-AAA", Test.Result);
				Test.Result = string.Empty;
			}
			test.RaiseEvents();
			Assert.Equal(3, test.Event1HandlerCount);
			Assert.Equal(0, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BAC--A", Test.Result);
			Test.Result = string.Empty;
		}
		[Test] public void TestAddRemoved()
		{
			var test = new Test();

			EventHandler handler = (s,a) => Test.Result += "B";

			test.Event1 += handler;
			test.Event1 += handler;
			test.Event1 += test.Handler;
			test.Event1 += (s,a) => Test.Result += "C";
			test.AttachHandlerToEvent2();
			Test.Event3 += test.Handler;
			test.RaiseEvents();

			Assert.Equal(4, test.Event1HandlerCount);
			Assert.Equal(1, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BBAC-A-A", Test.Result);
			Test.Result = string.Empty;

			using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.AddRemoved))
			{
				test.ResetHandlers();
				test.RaiseEvents();
				Assert.Equal(0, test.Event1HandlerCount);
				Assert.Equal(0, test.Event2HandlerCount);
				Assert.Equal(0, test.Event3HandlerCount);
				Assert.Equal("--", Test.Result);
				Test.Result = string.Empty;

				test.Event1 += handler;
				test.RaiseEvents();
				Assert.Equal(1, test.Event1HandlerCount);
				Assert.Equal(0, test.Event2HandlerCount);
				Assert.Equal(0, test.Event3HandlerCount);
				Assert.Equal("B--", Test.Result);
				Test.Result = string.Empty;
			}

			test.RaiseEvents();
			Assert.Equal(4, test.Event1HandlerCount);
			Assert.Equal(1, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BBAC-A-A", Test.Result);
			Test.Result = string.Empty;
		}
		[Test] public void TestBoth()
		{
			var test = new Test();

			EventHandler handler = (s,a) => Test.Result += "B";

			test.Event1 += handler;
			test.Event1 += handler;
			test.Event1 += test.Handler;
			test.Event1 += (s,a) => Test.Result += "C";
			Test.Event3 += test.Handler;
			test.RaiseEvents();

			Assert.Equal(4, test.Event1HandlerCount);
			Assert.Equal(0, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BBAC--A", Test.Result);
			Test.Result = string.Empty;

			using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.Both))
			{
				test.Event1 -= handler;
				test.Event1 += test.Handler;
				test.Event1 += test.Handler;
				test.Event1 += (s,a) => Test.Result += "D";
				test.RaiseEvents();
				Assert.Equal(6, test.Event1HandlerCount);
				Assert.Equal(0, test.Event2HandlerCount);
				Assert.Equal(1, test.Event3HandlerCount);
				Assert.Equal("BACAAD--A", Test.Result);
				Test.Result = string.Empty;

				test.Event1 += (s,a) => Test.Result += "E";
				test.AttachHandlerToEvent2();
				Test.Event3 -= test.Handler;
				test.RaiseEvents();
				Assert.Equal(7, test.Event1HandlerCount);
				Assert.Equal(1, test.Event2HandlerCount);
				Assert.Equal(0, test.Event3HandlerCount);
				Assert.Equal("BACAADE-A-", Test.Result);
				Test.Result = string.Empty;
			}

			test.RaiseEvents();
			Assert.Equal(4, test.Event1HandlerCount);
			Assert.Equal(0, test.Event2HandlerCount);
			Assert.Equal(1, test.Event3HandlerCount);
			Assert.Equal("BACB--A", Test.Result);
			Test.Result = string.Empty;
		}
	}
}

#endif
