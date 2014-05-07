//***********************************************
// System Event Extensions
//  Copyright © Rylogic Ltd 2010
//***********************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using pr.common;
using pr.extn;
using pr.util;

namespace pr.extn
{
	/// <summary>A helper for multicast delegate events</summary>
	[DebuggerStepThrough]
	public static class EventExtensions
	{
		private class SuspendData
		{
			public bool m_raised = false; // true if the event has been raised while suspended
			public int  m_nest   = 1;     // nested suspend count
		}

		/// <summary>A collection of suspended events and whether notify has been called on this suspended event</summary>
		private static readonly Dictionary<object,SuspendData> m_suspended = new Dictionary<object,SuspendData>();

		// Notes:
		//  - Suspend/Resume has no effect on a null event.
		//  - Do not modify events while suspended, doing so will cause leaks

		/// <summary>Add 'evt' to the collection of suspended events</summary>
		private static void SuspendImpl<T>(T evt)
		{
			SuspendData sd = IsSuspendedImpl(evt) ? m_suspended[evt] : null;
			if (sd == null) m_suspended.Add(evt, new SuspendData());
			else            sd.m_nest++;
		}

		/// <summary>Remove 'evt' from the collection of suspended events</summary>
		private static bool ResumeImpl<T>(T evt)
		{
			SuspendData sd = IsSuspendedImpl(evt) ? m_suspended[evt] : null;
			if (sd == null) throw new ArgumentException("Event not suspended. Make sure the event is not modified while suspended");
			if (--sd.m_nest == 0) m_suspended.Remove(evt);
			return sd.m_raised;
		}

		/// <summary>Returns true if 'evt' is suspended</summary>
		private static bool IsSuspendedImpl<T>(T evt)
		{
			return m_suspended.ContainsKey(evt);
		}

		/// <summary>Flag 'evt' as signalled</summary>
		private static void SignalImpl<T>(T evt)
		{
			if (!IsSuspendedImpl(evt)) throw new ArgumentException("Event not suspended. Make sure the event is not modified while suspended");
			m_suspended[evt].m_raised = true;
		}

		// EventHandlers *****************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<TEventArgs>(this EventHandler<TEventArgs> evt, object sender, TEventArgs args) where TEventArgs :EventArgs
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(sender, args);
		}
		public static void Raise(this EventHandler evt, object sender, EventArgs args)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(sender, args);
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt),
				() => Resume(evt));
		}
		public static Scope SuspendScope(this EventHandler evt)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt),
				() => Resume(evt));
		}

		/// <summary>Block this event from firing when Raise() is called, until Resume() is called</summary>
		public static void Suspend<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}
		public static void Suspend(this EventHandler evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}
		public static bool Resume(this EventHandler evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<TEventArgs>(this EventHandler<TEventArgs> evt, object sender, TEventArgs args) where TEventArgs :EventArgs
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(sender, args);
		}
		public static void Resume(this EventHandler evt, object sender, EventArgs args)
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(sender, args);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
		{
			return evt != null && IsSuspendedImpl(evt);
		}
		public static bool IsSuspended(this EventHandler evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// 0 Args *************************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise(this Action evt)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt();
		}

		/// <summary>Block this event from firing when Raise() is called, until Resume() is called</summary>
		public static void Suspend(this Action evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume(this Action evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended and 'raise_if_signalled' is true, will call Raise()</summary>
		public static void Resume(this Action evt, bool raise_if_signalled)
		{
			if (evt == null) return;
			if (ResumeImpl(evt) && raise_if_signalled) evt();
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended(this Action evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// 1 Arg *************************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1>(this Action<T1> evt, T1 arg1)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(arg1);
		}

		/// <summary>Block this event from firing when Raise() is called until Resume() is called</summary>
		public static void Suspend<T1>(this Action<T1> evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume<T1>(this Action<T1> evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<T1>(this Action<T1> evt, T1 arg1)
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(arg1);
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended<T1>(this Action<T1> evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// 2 Args *************************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1,T2>(this Action<T1,T2> evt, T1 arg1, T2 arg2)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(arg1, arg2);
		}

		/// <summary>Block this event from firing when Raise() is called until Resume() is called</summary>
		public static void Suspend<T1,T2>(this Action<T1,T2> evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume<T1,T2>(this Action<T1,T2> evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<T1,T2>(this Action<T1,T2> evt, T1 arg1, T2 arg2)
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(arg1, arg2);
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended<T1,T2>(this Action<T1,T2> evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// 3 Args *************************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1,T2,T3>(this Action<T1,T2,T3> evt, T1 arg1, T2 arg2, T3 arg3)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(arg1, arg2, arg3);
		}

		/// <summary>Block this event from firing when Raise() is called until Resume() is called</summary>
		public static void Suspend<T1,T2,T3>(this Action<T1,T2,T3> evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume<T1,T2,T3>(this Action<T1,T2,T3> evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<T1,T2,T3>(this Action<T1,T2,T3> evt, T1 arg1, T2 arg2, T3 arg3)
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(arg1, arg2, arg3);
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended<T1,T2,T3>(this Action<T1,T2,T3> evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// 4 Args *************************************************

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> evt, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (evt == null) return;
			if (IsSuspendedImpl(evt)) SignalImpl(evt); else evt(arg1, arg2, arg3, arg4);
		}

		/// <summary>Block this event from firing when Raise() is called until Resume() is called</summary>
		public static void Suspend<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> evt)
		{
			if (evt == null) return;
			SuspendImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// Does not fire the event if Raise() was called while suspended.
		/// Returns true however if the event was signalled while suspended</summary>
		public static bool Resume<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> evt)
		{
			if (evt == null) return false;
			return ResumeImpl(evt);
		}

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> evt, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(arg1, arg2, arg3, arg4);
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> evt)
		{
			return evt != null && IsSuspendedImpl(evt);
		}

		// Extension methods *********************************************
		/// <summary>
		/// Usage:
		///   Attach a weak action/event handler to an event
		///     provider.MyEvent += new EventHandler&lt;EventArgs&gt;(MyWeakEventHandler).MakeWeak(eh => provider.MyEvent -= eh);
		///     ...
		///     // Must be a real method, not a lambda
		///     void MyWeakEventHandler(object sender, EventArgs e){}
		///
		/// Usage:
		///   Make all attached event handlers weak
		///   public class EventProvider
		///   {
		///      private EventHandler&lt;EventArgs&gt; m_MyEvent;
		///      public event EventHandler&lt;EventArgs&gt; MyEvent
		///      {
		///          add { m_Event += value.MakeWeak(eh => m_Event -= eh); }
		///          remove {}
		///      }
		///    }
		/// Behaviour:
		///   Gun gun = new Gun();
		///   Target bob = new Target("Bob");
		///   Target fred = new Target("Fred");
		///   gun.Bang += new EventHandler&lt;EventArgs&gt;(bob.OnHit).MakeWeak(h => gun.Bang -= h);
		///   gun.Bang += fred.OnHit;
		///   gun.Bang += new EventHandler&lt;EventArgs&gt;((s,e)=>{MessageBox.Show("Don't do this")}).MakeWeak((h)=>gun.Bang -= h); // see WARNING
		///   gun.Shoot();
		///   bob = null;
		///   fred = null;
		///   GC.Collect();
		///   Thread.Sleep(100); // bob collected here, but not fred
		///   gun.Shoot(); // fred still shot here
		///
		/// WARNING:
		///  Don't attach anonymous delegates as weak delegates. When the delegate goes out of
		///  scope it will be collected and silently remove itself from the event</summary>
		public static EventHandler MakeWeak(this EventHandler handler, UnregisterEventHandler unregister)
		{
			if (handler == null) throw new ArgumentNullException("handler");
			if (handler.Method.IsStatic || handler.Target == null) throw new ArgumentException("Only instance methods are supported.", "handler");

			var weh_type = typeof(WeakEventHandler<>).MakeGenericType(handler.Method.DeclaringType);
			var weh_constructor = weh_type.GetConstructor(new[] {typeof(EventHandler), typeof(UnregisterEventHandler)});
			var weh = (IWeakEventHandler)weh_constructor.Invoke(new object[] {handler, unregister});
			return weh.Handler;
		}
		public static EventHandler<E> MakeWeak<E>(this EventHandler<E> event_handler, UnregisterEventHandler<E> unregister) where E: EventArgs
		{
			if (event_handler == null) throw new ArgumentNullException("event_handler");
			if (event_handler.Method.IsStatic || event_handler.Target == null) throw new ArgumentException("Only instance methods are supported.", "event_handler");

			var weh_type = typeof(WeakEventHandler<,>).MakeGenericType(event_handler.Method.DeclaringType, typeof(E));
			var weh_constructor = weh_type.GetConstructor(new[] {typeof(EventHandler<E>), typeof(UnregisterEventHandler<E>)});
			var weh = (IWeakEventHandler<E>)weh_constructor.Invoke(new object[] {event_handler, unregister});
			return weh.Handler;
		}
		public static Action MakeWeak(this Action action, UnregisterAction unregister)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (action.Method.IsStatic || action.Target == null) throw new ArgumentException("Only instance methods are supported.", "action");

			Type type = typeof(WeakAction<>).MakeGenericType(action.Method.DeclaringType);
			ConstructorInfo cons = type.GetConstructor(new[] {typeof(Action), typeof(UnregisterAction)});
			IWeakAction weak_action = (IWeakAction)cons.Invoke(new object[] {action, unregister});
			return weak_action.Handler;
		}
		public static Action<T1> MakeWeak<T1>(this Action<T1> action, UnregisterAction<T1> unregister)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (action.Method.IsStatic || action.Target == null) throw new ArgumentException("Only instance methods are supported.", "action");

			Type type = typeof(WeakAction<,>).MakeGenericType(action.Method.DeclaringType, typeof(T1));
			ConstructorInfo cons = type.GetConstructor(new[] {typeof(Action<T1>), typeof(UnregisterAction<T1>)});
			IWeakAction<T1> weak_action = (IWeakAction<T1>)cons.Invoke(new object[] {action, unregister});
			return weak_action.Handler;
		}
		public static Action<T1,T2> MakeWeak<T1,T2>(this Action<T1,T2> action, UnregisterAction<T1,T2> unregister)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (action.Method.IsStatic || action.Target == null) throw new ArgumentException("Only instance methods are supported.", "action");

			Type type = typeof(WeakAction<,,>).MakeGenericType(action.Method.DeclaringType, typeof(T1), typeof(T2));
			ConstructorInfo cons = type.GetConstructor(new[] {typeof(Action<T1,T2>), typeof(UnregisterAction<T1,T2>)});
			IWeakAction<T1,T2> weak_action = (IWeakAction<T1,T2>)cons.Invoke(new object[] {action, unregister});
			return weak_action.Handler;
		}
		public static Action<T1,T2,T3> MakeWeak<T1,T2,T3>(this Action<T1,T2,T3> action, UnregisterAction<T1,T2,T3> unregister)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (action.Method.IsStatic || action.Target == null) throw new ArgumentException("Only instance methods are supported.", "action");

			Type type = typeof(WeakAction<,,,>).MakeGenericType(action.Method.DeclaringType, typeof(T1), typeof(T2), typeof(T3));
			ConstructorInfo cons = type.GetConstructor(new[] {typeof(Action<T1,T2,T3>), typeof(UnregisterAction<T1,T2,T3>)});
			IWeakAction<T1,T2,T3> weak_action = (IWeakAction<T1,T2,T3>)cons.Invoke(new object[] {action, unregister});
			return weak_action.Handler;
		}
		public static Action<T1,T2,T3,T4> MakeWeak<T1,T2,T3,T4>(this Action<T1,T2,T3,T4> action, UnregisterAction<T1,T2,T3,T4> unregister)
		{
			if (action == null) throw new ArgumentNullException("action");
			if (action.Method.IsStatic || action.Target == null) throw new ArgumentException("Only instance methods are supported.", "action");

			Type type = typeof(WeakAction<,,,,>).MakeGenericType(action.Method.DeclaringType, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
			ConstructorInfo cons = type.GetConstructor(new[] {typeof(Action<T1,T2,T3,T4>), typeof(UnregisterAction<T1,T2,T3,T4>)});
			IWeakAction<T1,T2,T3,T4> weak_action = (IWeakAction<T1,T2,T3,T4>)cons.Invoke(new object[] {action, unregister});
			return weak_action.Handler;
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using System.Threading;
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestEventExtensions
		{
			private static event Action<int> BooEvent;

			private static readonly List<string> collected = new List<string>();
			private static readonly List<string> hit       = new List<string>();
			private class Gun
			{
				public event EventHandler Firing;
				public event Action<Gun> Bang;
				public event EventHandler<FiredArgs> Fired;
				public class FiredArgs :EventArgs { public string Noise { get; set; } }

				~Gun()              { collected.Add("gun"); }
				public void Shoot()
				{
					Firing.Raise(this, EventArgs.Empty);
					Bang.Raise(this);
					Fired.Raise(this, new FiredArgs{Noise = "Bang!"});
				}
			}
			private class Target
			{
				private readonly string m_name;
				public Target(string name)                     { m_name = name; }
				~Target()                                      { collected.Add(m_name); }
				public void OnHit(Gun gun)                     { hit.Add(m_name); }
				public void OnFiring(object s, EventArgs a)    { hit.Add("Dont Shoot"); }
				public void OnFired(object s, Gun.FiredArgs a) { hit.Add(a.Noise); }
			}

			// ReSharper disable RedundantAssignment
			[Test] public static void SuspendResume()
			{
				// Test event suspend/resume
				int boo_raised = 0;
				BooEvent += (i)=> { ++boo_raised; };
				BooEvent.Suspend();
				BooEvent.Raise(0);
				BooEvent.Raise(1);
				BooEvent.Raise(2);
				BooEvent.Raise(3);
				Assert.IsTrue(BooEvent.Resume());
				Assert.AreEqual(0, boo_raised);
			}
			[Test] public static void WeakActions()
			{
				// Test weak event handlers
				var gun = new Gun();
				var bob = new Target("bob");
				var fred = new Target("fred");
				gun.Bang += new Action<Gun>(bob.OnHit).MakeWeak(h => gun.Bang -= h);
				gun.Bang += fred.OnHit;
				gun.Shoot();
				Assert.IsTrue(hit.Contains("bob"));
				Assert.IsTrue(hit.Contains("fred"));

				hit.Clear();
				bob = null;
				fred = null;
				GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced);
				Thread.Sleep(100); // bob collected here, but not fred
				Assert.IsTrue(collected.Contains("bob"));
				Assert.IsFalse(collected.Contains("fred"));
				gun.Shoot(); // fred still shot here
				Assert.IsFalse(hit.Contains("bob"));
				Assert.IsTrue(hit.Contains("fred"));
			}
			[Test] public static void WeakEventHandlers()
			{
				var gun = new Gun();
				var bob = new Target("bob");
				gun.Firing += new EventHandler(bob.OnFiring).MakeWeak(eh => gun.Firing -= eh);
				gun.Bang += new Action<Gun>(bob.OnHit).MakeWeak(eh => gun.Bang -= eh);
				gun.Fired += new EventHandler<Gun.FiredArgs>(bob.OnFired).MakeWeak(eh => gun.Fired -= eh);
				gun.Shoot();
				Assert.IsTrue(hit.Contains("Dont Shoot"));
				Assert.IsTrue(hit.Contains("bob"));
				Assert.IsTrue(hit.Contains("Bang!"));

				hit.Clear();
				bob = null;
				GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced);
				Thread.Sleep(100); // bob collected here, but not fred
				Assert.IsTrue(collected.Contains("bob"));
				gun.Shoot();
				Assert.IsTrue(hit.Count == 0);
			}
			// ReSharper restore RedundantAssignment
		}
	}
}
#endif