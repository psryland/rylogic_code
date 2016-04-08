//***********************************************
// System Event Extensions
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using pr.util;

namespace pr.extn
{
	/// <summary>A helper for multicast delegate events</summary>
	[DebuggerStepThrough]
	public static class EventEx
	{
		// Notes:
		//  - Suspend/Resume has no effect on a null event.
		//  - Do not modify events while suspended, doing so will cause leaks
		private class SuspendData
		{
			public bool m_raised = false; // true if the event has been raised while suspended
			public int  m_nest   = 0;     // nested suspend count
		}

		/// <summary>A collection of suspended events and whether notify has been called on this suspended event</summary>
		private static readonly Dictionary<object,SuspendData> m_suspended = new Dictionary<object,SuspendData>();

		#region Implementation
		private static class Impl<T>
		{
			/// <summary>Add 'evt' to the collection of suspended events. Returns true if the event has been raised since suspended</summary>
			public static bool Suspend(T evt, bool suspend)
			{
				// Find the suspend data associated with this event
				SuspendData sd = IsSuspended(evt) ? m_suspended[evt] : null;
				if (sd == null)
				{
					// If no data, add it, or throw if this is a resume
					if (suspend)
						m_suspended.Add(evt, sd = new SuspendData());
					else
						throw new ArgumentException("Event not suspended. Make sure the event is not modified while suspended");
				}

				// Suspend/Resume ref count
				if (suspend)
					++sd.m_nest;
				else
					--sd.m_nest;

				// If this was the last resume, remove 'evt' from the suspending events collection
				if (sd.m_nest == 0)
				{
					m_suspended.Remove(evt);
				}

				// Return the 'Raised' of the event
				return sd.m_raised;
			}

			/// <summary>Returns true if 'evt' is suspended</summary>
			[DebuggerStepThrough]
			public static bool IsSuspended(T evt)
			{
				return m_suspended.ContainsKey(evt);
			}

			/// <summary>Flag 'evt' as signalled</summary>
			public static void Signal(T evt)
			{
				if (!IsSuspended(evt)) throw new ArgumentException("Event not suspended. Make sure the event is not modified while suspended");
				m_suspended[evt].m_raised = true;
			}
		}
		#endregion

		#region EventHandler / EventHandler<>

		/// <summary>Fire the event if not suspended</summary>
		[DebuggerStepThrough]
		public static TEventArgs Raise<TEventArgs>(this EventHandler<TEventArgs> evt, object sender = null, TEventArgs args = null) where TEventArgs :EventArgs
		{
			Debug.Assert(!(sender is EventArgs), "Don't pass the event args as the 'sender' parameter");
			if (evt == null) return args;
			if (Impl<EventHandler<TEventArgs>>.IsSuspended(evt))
				Impl<EventHandler<TEventArgs>>.Signal(evt);
			else
				evt(sender, args);
			return args;
		}

		/// <summary>Fire the event if not suspended</summary>
		[DebuggerStepThrough]
		public static EventArgs Raise(this EventHandler evt, object sender = null, EventArgs args = null)
		{
			Debug.Assert(!(sender is EventArgs), "Don't pass the event args as the 'sender' parameter");
			if (evt == null) return args;
			if (Impl<EventHandler>.IsSuspended(evt))
				Impl<EventHandler>.Signal(evt);
			else
				evt(sender, args);
			return args;
		}

		/// <summary>Post a message to the thread queue to fire this event (if not suspended)</summary>
		[DebuggerStepThrough]
		public static void QueueRaise<TEventArgs>(this EventHandler<TEventArgs> evt, object sender = null, TEventArgs args = null) where TEventArgs :EventArgs
		{
			Debug.Assert(!(sender is EventArgs), "Don't pass the event args as the 'sender' parameter");
			Dispatcher.CurrentDispatcher.BeginInvoke(() => evt.Raise(sender, args));
		}

		/// <summary>Post a message to the thread queue to fire this event (if not suspended)</summary>
		[DebuggerStepThrough]
		public static void QueueRaise(this EventHandler evt, object sender = null, EventArgs args = null)
		{
			Debug.Assert(!(sender is EventArgs), "Don't pass the event args as the 'sender' parameter");
			Dispatcher.CurrentDispatcher.BeginInvoke(() => evt.Raise(sender, args));
		}

		/// <summary>
		/// Returns an RAII object for suspending events.
		/// The event will be raised if 'raise_on_resume' is true and it was signalled while suspended</summary>
		public static Scope SuspendScope<TEventArgs>(this EventHandler<TEventArgs> evt, bool raise_if_signalled = false, object sender = null, TEventArgs args = null) where TEventArgs :EventArgs
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt, true),
				() =>
				{
					if (Suspend(evt, false) && raise_if_signalled)
						evt.Raise(sender, args);
				});
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope(this EventHandler evt, bool raise_if_signalled = false, object sender = null, EventArgs args = null)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt, true),
				() =>
				{
					if (Suspend(evt, false) && raise_if_signalled)
						evt.Raise(sender, args);
				});
		}

		/// <summary>
		/// Block/Unblock this event from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the event has been 'Raise'd while being suspended.</summary>
		public static bool Suspend<TEventArgs>(this EventHandler<TEventArgs> evt, bool suspend) where TEventArgs :EventArgs
		{
			if (evt == null) return false;
			return Impl<EventHandler<TEventArgs>>.Suspend(evt, suspend);
		}

		/// <summary>
		/// Block/Unblock this event from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the event has been 'Raise'd while being suspended.</summary>
		public static bool Suspend(this EventHandler evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<EventHandler>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended<TEventArgs>(this EventHandler<TEventArgs> evt) where TEventArgs :EventArgs
		{
			return evt != null && Impl<EventHandler<TEventArgs>>.IsSuspended(evt);
		}
		public static bool IsSuspended(this EventHandler evt)
		{
			return evt != null && Impl<EventHandler>.IsSuspended(evt);
		}

		#endregion

		#region INotifyPropertyChanged

		/// <summary>Fire the event if not suspended</summary>
		[DebuggerStepThrough]
		public static PropertyChangedEventArgs Raise(this PropertyChangedEventHandler evt, object sender = null, PropertyChangedEventArgs args = null)
		{
			if (evt == null) return args;
			if (Impl<PropertyChangedEventHandler>.IsSuspended(evt))
				Impl<PropertyChangedEventHandler>.Signal(evt);
			else
				evt(sender, args);
			return args;
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope(this PropertyChangedEventHandler evt, bool raise_if_signalled = false, object sender = null, PropertyChangedEventArgs args = null)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt, true),
				() =>
				{
					if (Suspend(evt, false) && raise_if_signalled)
						evt.Raise(sender, args);
				});
		}

		/// <summary>
		/// Block/Unblock this event from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the event has been 'Raise'd while being suspended.</summary>
		public static bool Suspend(this PropertyChangedEventHandler evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<PropertyChangedEventHandler>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended(this PropertyChangedEventHandler evt)
		{
			return evt != null && Impl<PropertyChangedEventHandler>.IsSuspended(evt);
		}

		#endregion

		#region INotifyPropertyChanging

		/// <summary>Fire the event if not suspended</summary>
		[DebuggerStepThrough]
		public static PropertyChangingEventArgs Raise(this PropertyChangingEventHandler evt, object sender = null, PropertyChangingEventArgs args = null)
		{
			if (evt == null) return args;
			if (Impl<PropertyChangingEventHandler>.IsSuspended(evt))
				Impl<PropertyChangingEventHandler>.Signal(evt);
			else
				evt(sender, args);
			return args;
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope(this PropertyChangingEventHandler evt, bool raise_if_signalled = false, object sender = null, PropertyChangingEventArgs args = null)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => Suspend(evt, true),
				() =>
				{
					if (Suspend(evt, false) && raise_if_signalled)
						evt.Raise(sender, args);
				});
		}

		/// <summary>
		/// Block/Unblock this event from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the event has been 'Raise'd while being suspended.</summary>
		public static bool Suspend(this PropertyChangingEventHandler evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<PropertyChangingEventHandler>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended(this PropertyChangingEventHandler evt)
		{
			return evt != null && Impl<PropertyChangingEventHandler>.IsSuspended(evt);
		}

		#endregion
		
		#region Action

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise(this Action evt)
		{
			if (evt == null) return;
			if (Impl<Action>.IsSuspended(evt))
				Impl<Action>.Signal(evt);
			else
				evt();
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope(this Action evt, bool raise_if_signalled = false)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(
				() => evt.Suspend(true),
				() =>
				{
					if (evt.Suspend(false) && raise_if_signalled)
						evt.Raise();
				});
		}

		/// <summary>
		/// Block/Unblock this action from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the action has been 'Raise'd while being suspended.</summary>
		public static bool Suspend(this Action evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<Action>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended(this Action evt)
		{
			return evt != null && Impl<Action>.IsSuspended(evt);
		}

		#endregion

		#region Action<T1>

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1>(this Action<T1> evt, T1 arg1)
		{
			if (evt == null) return;
			if (Impl<Action<T1>>.IsSuspended(evt))
				Impl<Action<T1>>.Signal(evt);
			else
				evt(arg1);
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope<T1>(this Action<T1> evt)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(() => evt.Suspend(true), () => evt.Suspend(false));
		}

		/// <summary>
		/// Block/Unblock this action from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the action has been 'Raise'd while being suspended.</summary>
		public static bool Suspend<T1>(this Action<T1> evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<Action<T1>>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended<T1>(this Action<T1> evt)
		{
			return evt != null && Impl<Action<T1>>.IsSuspended(evt);
		}

		#endregion

		#region Action<T1,T2>

		/// <summary>Fire the event if not suspended</summary>
		public static void Raise<T1,T2>(this Action<T1,T2> evt, T1 arg1, T2 arg2)
		{
			if (evt == null) return;
			if (Impl<Action<T1,T2>>.IsSuspended(evt))
				Impl<Action<T1,T2>>.Signal(evt);
			else
				evt(arg1, arg2);
		}

		/// <summary>Returns an RAII object for suspending events</summary>
		public static Scope SuspendScope<T1,T2>(this Action<T1,T2> evt)
		{
			if (evt == null) return Scope.Create(null,null);
			return Scope.Create(() => evt.Suspend(true), () => evt.Suspend(false));
		}

		/// <summary>
		/// Block/Unblock this action from firing when Raise() is called.
		/// Calls to suspend/resume must be matched.
		/// Returns true if the action has been 'Raise'd while being suspended.</summary>
		public static bool Suspend<T1,T2>(this Action<T1,T2> evt, bool suspend)
		{
			if (evt == null) return false;
			return Impl<Action<T1,T2>>.Suspend(evt, suspend);
		}

		/// <summary>Returns true if this event is currently suspended</summary>
		public static bool IsSuspended<T1,T2>(this Action<T1,T2> evt)
		{
			return evt != null && Impl<Action<T1,T2>>.IsSuspended(evt);
		}

		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestEventExtns
	{
		private static event Action<int> BooEvent;

		[Test] public void SuspendResume()
		{
			// Test event suspend/resume
			int boo_raised = 0;
			BooEvent += i => ++boo_raised;
			BooEvent.Suspend(true);
			BooEvent.Raise(0);
			BooEvent.Raise(1);
			BooEvent.Raise(2);
			BooEvent.Raise(3);
			Assert.True(BooEvent.Suspend(false));
			Assert.AreEqual(0, boo_raised);
		}
	}
}
#endif