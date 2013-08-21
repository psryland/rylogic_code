using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using pr.extn;

namespace pr.common
{
	public static class EventsSnapshot
	{
		[Flags] public enum Restore
		{
			RemoveAdded = 1,
			AddRemoved  = 2,
			Both        = RemoveAdded|AddRemoved
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
				m_obj = obj;
				m_restore = restore;
				m_binding_flags = binding_flags;
				var type = m_obj.GetType();
				
				var fields = type.AllFields(m_binding_flags).ToDictionary(x => x.Name);
				var events = type.AllEvents(m_binding_flags).ToList();
				foreach (var evt in events)
				{
					FieldInfo field;
					if (!fields.TryGetValue(evt.Name, out field))
						continue;

					// Get the instance of the multicast delegate from 'obj' and save the invocation list
					var mcd = field.GetValue(obj) as MulticastDelegate;
					if (mcd == null)
					{
						m_events.Add(field, new Delegate[0]);
					}
					else
					{
						var delegates = mcd.GetInvocationList();
						m_events.Add(field, delegates);
					}
				}
			}

			/// <summary>Restores the event handlers to the delegates</summary>
			public void Dispose()
			{
				var type = m_obj.GetType();

				// Restore the delegates
				foreach (var evt in m_events)
				{
					var field = evt.Key;
					var mcd = field.GetValue(m_obj) as MulticastDelegate;

					var old_delegates = evt.Value;
					var new_delegates = mcd != null ? mcd.GetInvocationList() : new Delegate[0];

					var event_info = type.GetEvent(field.Name, m_binding_flags);
					Debug.Assert(event_info != null);

					if ((m_restore & Restore.RemoveAdded) == Restore.RemoveAdded)
					{
						// Get the set of added delegates (allowing for duplicates)
						var added = new List<Delegate>(new_delegates);
						foreach (var d in old_delegates)
							added.Remove(d);

						// Remove any that were added
						var remove = event_info.GetRemoveMethod(true); Debug.Assert(remove != null);
						foreach (var d in added)
							remove.Invoke(m_obj, new object[]{d});
					}
					if ((m_restore & Restore.AddRemoved) == Restore.AddRemoved)
					{
						// Get the set of delegates that have been removed
						var removed = new List<Delegate>(old_delegates);
						foreach (var d in new_delegates)
							removed.Remove(d);

						// Add any that were removed
						var add = event_info.GetAddMethod(true); Debug.Assert(add != null);
						foreach (var d in removed)
							add.Invoke(m_obj, new object[]{d});
					}
				}
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestEventSnapshot
		{
			private class TestBase
			{
				protected event EventHandler<Args> Event2;
				public class Args :EventArgs {};

				public int Event2HandlerCount { get { return Event2 != null ? Event2.GetInvocationList().Length : 0; } }

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

				public Action Ignored;
				public event EventHandler Event1;
				public static event EventHandler Event3;
				
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

			[Test] public static void TestRemoveAdded()
			{
				var test = new Test();

				EventHandler handler = (s,a) => Test.Result += "B";

				test.Event1 += handler;
				test.Event1 += handler;
				test.Event1 += test.Handler;
				test.Event1 += (s,a) => Test.Result += "C";
				Test.Event3 += test.Handler;
				test.RaiseEvents();

				Assert.AreEqual(4, test.Event1HandlerCount);
				Assert.AreEqual(0, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BBAC--A", Test.Result);
				Test.Result = string.Empty;

				using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.RemoveAdded))
				{
					test.Event1 += (s,a) => Test.Result += "D";
					test.Event1 += test.Handler;
					test.Event1 += handler;
					test.AttachHandlerToEvent2();
					test.AttachHandlerToEvent3();
					test.RaiseEvents();
					Assert.AreEqual(7, test.Event1HandlerCount);
					Assert.AreEqual(1, test.Event2HandlerCount);
					Assert.AreEqual(2, test.Event3HandlerCount);
					Assert.AreEqual("BBACDAB-A-AA", Test.Result);
					Test.Result = string.Empty;

					test.Event1 -= handler;
					test.Event1 -= handler;
					test.AttachHandlerToEvent2();
					test.AttachHandlerToEvent3();
					test.RaiseEvents();
					Assert.AreEqual(5, test.Event1HandlerCount);
					Assert.AreEqual(2, test.Event2HandlerCount);
					Assert.AreEqual(3, test.Event3HandlerCount);
					Assert.AreEqual("BACDA-AA-AAA", Test.Result);
					Test.Result = string.Empty;
				}
				test.RaiseEvents();
				Assert.AreEqual(3, test.Event1HandlerCount);
				Assert.AreEqual(0, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BAC--A", Test.Result);
				Test.Result = string.Empty;
			}
			[Test] public static void TestAddRemoved()
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

				Assert.AreEqual(4, test.Event1HandlerCount);
				Assert.AreEqual(1, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BBAC-A-A", Test.Result);
				Test.Result = string.Empty;

				using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.AddRemoved))
				{
					test.ResetHandlers();
					test.RaiseEvents();
					Assert.AreEqual(0, test.Event1HandlerCount);
					Assert.AreEqual(0, test.Event2HandlerCount);
					Assert.AreEqual(0, test.Event3HandlerCount);
					Assert.AreEqual("--", Test.Result);
					Test.Result = string.Empty;

					test.Event1 += handler;
					test.RaiseEvents();
					Assert.AreEqual(1, test.Event1HandlerCount);
					Assert.AreEqual(0, test.Event2HandlerCount);
					Assert.AreEqual(0, test.Event3HandlerCount);
					Assert.AreEqual("B--", Test.Result);
					Test.Result = string.Empty;
				}

				test.RaiseEvents();
				Assert.AreEqual(4, test.Event1HandlerCount);
				Assert.AreEqual(1, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BBAC-A-A", Test.Result);
				Test.Result = string.Empty;
			}
			[Test] public static void TestBoth()
			{
				var test = new Test();

				EventHandler handler = (s,a) => Test.Result += "B";

				test.Event1 += handler;
				test.Event1 += handler;
				test.Event1 += test.Handler;
				test.Event1 += (s,a) => Test.Result += "C";
				Test.Event3 += test.Handler;
				test.RaiseEvents();

				Assert.AreEqual(4, test.Event1HandlerCount);
				Assert.AreEqual(0, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BBAC--A", Test.Result);
				Test.Result = string.Empty;

				using (EventsSnapshot.Capture(test, EventsSnapshot.Restore.Both))
				{
					test.Event1 -= handler;
					test.Event1 += test.Handler;
					test.Event1 += test.Handler;
					test.Event1 += (s,a) => Test.Result += "D";
					test.RaiseEvents();
					Assert.AreEqual(6, test.Event1HandlerCount);
					Assert.AreEqual(0, test.Event2HandlerCount);
					Assert.AreEqual(1, test.Event3HandlerCount);
					Assert.AreEqual("BACAAD--A", Test.Result);
					Test.Result = string.Empty;

					test.Event1 += (s,a) => Test.Result += "E";
					test.AttachHandlerToEvent2();
					Test.Event3 -= test.Handler;
					test.RaiseEvents();
					Assert.AreEqual(7, test.Event1HandlerCount);
					Assert.AreEqual(1, test.Event2HandlerCount);
					Assert.AreEqual(0, test.Event3HandlerCount);
					Assert.AreEqual("BACAADE-A-", Test.Result);
					Test.Result = string.Empty;
				}

				test.RaiseEvents();
				Assert.AreEqual(4, test.Event1HandlerCount);
				Assert.AreEqual(0, test.Event2HandlerCount);
				Assert.AreEqual(1, test.Event3HandlerCount);
				Assert.AreEqual("BACB--A", Test.Result);
				Test.Result = string.Empty;
			}
		}
	}
}
#endif
