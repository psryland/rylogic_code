//***********************************************
// System Event Extensions
//  Copyright © Rylogic Ltd 2010
//***********************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using pr.common;

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

		/// <summary>Block this event from firing when Raise() is called, until Resume() is called</summary>
		public static void Suspend<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
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

		/// <summary>Resume firing this event when Raise() is called.
		/// if the event was signalled while suspended, will call Raise()</summary>
		public static void Resume<TEventArgs>(this EventHandler<TEventArgs> evt, object sender, TEventArgs args) where TEventArgs :EventArgs
		{
			if (evt == null) return;
			if (ResumeImpl(evt)) evt(sender, args);
		}

		/// <summary>Returns true if this event is currently suppended</summary>
		public static bool IsSuspended<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
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
		public static EventHandler<E> MakeWeak<E>(this EventHandler<E> event_handler, UnregisterEventHandler<E> unregister) where E: EventArgs
		{
			if (event_handler == null) throw new ArgumentNullException("event_handler");
			if (event_handler.Method.IsStatic || event_handler.Target == null) throw new ArgumentException("Only instance methods are supported.", "event_handler");

			Type weh_type = typeof(WeakEventHandler<,>).MakeGenericType(event_handler.Method.DeclaringType, typeof(E));
			ConstructorInfo weh_constructor = weh_type.GetConstructor(new[] {typeof(EventHandler<E>), typeof(UnregisterEventHandler<E>)});
			IWeakEventHandler<E> weh = (IWeakEventHandler<E>)weh_constructor.Invoke(new object[] {event_handler, unregister});
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

	
	/// <summary>String extension unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		private static event Action<int> BooEvent;
		
		private static readonly List<string> collected = new List<string>();
		private static readonly List<string> hit       = new List<string>();
		private class Gun
		{
			public event Action<Gun> Bang;
			~Gun()              { collected.Add("gun"); }
			public void Shoot() { Bang.Raise(this); }
		}
		private class Target
		{
			private readonly string m_name;
			public Target(string name) { m_name = name; }
			~Target()                  { collected.Add(m_name); }
			public void OnHit(Gun gun) { hit.Add(m_name); }
		}
		
		[Test] public static void TestEventExtensions()
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
			
			// ReSharper disable RedundantAssignment
			// Test weak event handlers
			Gun gun = new Gun();
			Target bob = new Target("bob");
			Target fred = new Target("fred");
			gun.Bang += new Action<Gun>(bob.OnHit).MakeWeak((h)=>gun.Bang -= h);
			gun.Bang += fred.OnHit;
			gun.Shoot();
			Assert.Contains("bob", hit);
			Assert.Contains("fred", hit);
			
			hit.Clear();
			bob = null;
			fred = null;
			GC.Collect(); Thread.Sleep(100); // bob collected here, but not fred
			Assert.IsTrue(collected.Contains("bob"));
			Assert.IsFalse(collected.Contains("fred"));
			gun.Shoot(); // fred still shot here
			Assert.IsFalse(hit.Contains("bob"));
			Assert.IsTrue(hit.Contains("fred"));
			// ReSharper restore RedundantAssignment
		}
	}
}
