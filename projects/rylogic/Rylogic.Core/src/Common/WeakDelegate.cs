//***********************************************
// Weak Delegate
//  Copyright (c) Rylogic Ltd 2010
//***********************************************
// http://diditwith.net/PermaLink,guid,aacdb8ae-7baa-4423-a953-c18c1c7940ab.aspx

using System;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Rylogic.Common
{
	public static class WeakRef
	{
		// Usage:
		//   Attach a weak action/event handler to an event
		//     provider.MyEvent += WeakRef.MakeWeak(MyWeakEventHandler, eh => provider.MyEvent -= eh);
		//     ...
		//     // Must be a real method, not a lambda. Can be a local method though.
		//     void MyWeakEventHandler(object sender, EventArgs e){}
		//
		// Usage:
		//   Make all attached event handlers weak
		//   public class EventProvider
		//   {
		//      private EventHandler&lt;EventArgs&gt; m_MyEvent;
		//      public event EventHandler&lt;EventArgs&gt; MyEvent
		//      {
		//          add { m_Event += WeakRef.MakeWeak(value, eh => m_Event -= eh); }
		//          remove {}
		//      }
		//    }
		//
		// WARNING:
		//  Don't attach anonymous delegates as weak delegates. When the delegate goes out of
		//  scope it will be collected and silently remove itself from the event</summary>

		/// <summary>Returns the target of the reference or null</summary>
		public static T Target<T>(this WeakReference<T> wref)
			where T : class
		{
			return wref.TryGetTarget(out var x) ? x : null!;
		}

		/// <summary></summary>
		public static EventHandler MakeWeak(this EventHandler handler, Action<EventHandler> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var weh_type = typeof(WeakHandlerImpl<>).MakeGenericType(method_type);
			var cons = weh_type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weh = (IWeakHandler)cons.Invoke(new object[] { handler, unregister });
			return weh.Handler;
		}
		public static EventHandler<E> MakeWeak<E>(this EventHandler<E> handler, Action<EventHandler<E>> unregister) where E : EventArgs
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var weh_type = typeof(WeakHandlerImpl<,>).MakeGenericType(method_type, typeof(E));
			var cons = weh_type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weh = (IWeakHandler<E>)cons.Invoke(new object[] { handler, unregister });
			return weh.Handler;
		}
		public static PropertyChangedEventHandler MakeWeak(this PropertyChangedEventHandler handler, Action<PropertyChangedEventHandler> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var weh_type = typeof(WeakPropChangedHandlerImpl<>).MakeGenericType(method_type);
			var cons = weh_type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weh = (IWeakPropChangedHandler)cons.Invoke(new object[] { handler, unregister });
			return weh.Handler;
		}
		public static NotifyCollectionChangedEventHandler MakeWeak(this NotifyCollectionChangedEventHandler handler, Action<NotifyCollectionChangedEventHandler> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var weh_type = typeof(WeakCollectionChangedHandlerImpl<>).MakeGenericType(method_type);
			var cons = weh_type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weh = (IWeakCollectionChangedHandler)cons.Invoke(new object[] { handler, unregister });
			return weh.Handler;
		}

		/// <summary></summary>
		public static Action MakeWeak(this Action handler, Action<Action> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var type = typeof(WeakActionImpl<>).MakeGenericType(method_type);
			var cons = type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weak_action = (IWeakAction)cons.Invoke(new object[] { handler, unregister });
			return weak_action.Handler;
		}
		public static Action<T1> MakeWeak<T1>(this Action<T1> handler, Action<Action<T1>> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var type = typeof(WeakActionImpl<,>).MakeGenericType(method_type, typeof(T1));
			var cons = type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weak_action = (IWeakAction<T1>)cons.Invoke(new object[] { handler, unregister });
			return weak_action.Handler;
		}
		public static Action<T1, T2> MakeWeak<T1, T2>(this Action<T1, T2> handler, Action<Action<T1, T2>> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var type = typeof(WeakActionImpl<,,>).MakeGenericType(method_type, typeof(T1), typeof(T2));
			var cons = type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weak_action = (IWeakAction<T1, T2>)cons.Invoke(new object[] { handler, unregister });
			return weak_action.Handler;
		}
		public static Action<T1, T2, T3> MakeWeak<T1, T2, T3>(this Action<T1, T2, T3> handler, Action<Action<T1, T2, T3>> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var type = typeof(WeakActionImpl<,,,>).MakeGenericType(method_type, typeof(T1), typeof(T2), typeof(T3));
			var cons = type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weak_action = (IWeakAction<T1, T2, T3>)cons.Invoke(new object[] { handler, unregister });
			return weak_action.Handler;
		}
		public static Action<T1, T2, T3, T4> MakeWeak<T1, T2, T3, T4>(this Action<T1, T2, T3, T4> handler, Action<Action<T1, T2, T3, T4>> unregister)
		{
			Validate(handler, unregister);
			var method_type = handler.Method.DeclaringType ?? throw new Exception($"method handler declaring type is null");
			var type = typeof(WeakActionImpl<,,,,>).MakeGenericType(method_type, typeof(T1), typeof(T2), typeof(T3), typeof(T4));
			var cons = type.GetConstructor(new[] { handler.GetType(), unregister.GetType() }) ?? throw new Exception($"weak event handler constructor not found");
			var weak_action = (IWeakAction<T1, T2, T3, T4>)cons.Invoke(new object[] { handler, unregister });
			return weak_action.Handler;
		}

		/// <summary>Validate the handler parameters</summary>
		private static void Validate(Delegate handler, Delegate unregister)
		{
			if (handler == null)
				throw new ArgumentNullException(nameof(handler));
			if (handler.Method.IsStatic || handler.Target == null)
				throw new ArgumentException("Only instance methods are supported.", nameof(handler));
			if (unregister == null)
				throw new ArgumentNullException(nameof(unregister), "An unregister action is required to remove the weak handler");
		}
	}

	#region IWeak interfaces

	public interface IWeakAction
	{
		Action Handler { get; }
	}
	public interface IWeakAction<T1>
	{
		Action<T1> Handler { get; }
	}
	public interface IWeakAction<T1, T2>
	{
		Action<T1, T2> Handler { get; }
	}
	public interface IWeakAction<T1, T2, T3>
	{
		Action<T1, T2, T3> Handler { get; }
	}
	public interface IWeakAction<T1, T2, T3, T4>
	{
		Action<T1, T2, T3, T4> Handler { get; }
	}
	public interface IWeakHandler
	{
		EventHandler Handler { get; }
	}
	public interface IWeakHandler<E> where E : EventArgs
	{
		EventHandler<E> Handler { get; }
	}
	public interface IWeakPropChangedHandler
	{
		PropertyChangedEventHandler Handler { get; }
	}
	public interface IWeakCollectionChangedHandler
	{
		NotifyCollectionChangedEventHandler Handler { get; }
	}

	#endregion

	#region Weak Handlers

	// Event Handlers
	public class WeakHandlerImpl<T> : IWeakHandler
		where T : class
	{
		private delegate void OpenEventHandler(T @this, object? sender, EventArgs args);

		private readonly WeakReference m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private Action<EventHandler>? m_unregister;

		public WeakHandlerImpl(EventHandler event_handler, Action<EventHandler> unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_unregister = unregister;
			Handler = Invoke;
		}

		/// <summary></summary>
		public EventHandler Handler { get; }

		/// <summary></summary>
		public void Invoke(object? sender, EventArgs args)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null)
			{
				m_open_handler.Invoke(target, sender, args);
			}
			else if (m_unregister != null)
			{
				m_unregister(Handler);
				m_unregister = null;
			}
		}

		/// <summary></summary>
		public static implicit operator EventHandler(WeakHandlerImpl<T> weh) { return weh.Handler; }
	}
	public class WeakHandlerImpl<T, E> :IWeakHandler<E>
		where T : class
		where E : EventArgs
	{
		private delegate void OpenEventHandler(T @this, object? sender, E e);

		private readonly WeakReference m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private Action<EventHandler<E>>? m_unregister;

		public WeakHandlerImpl(EventHandler<E> event_handler, Action<EventHandler<E>> unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_unregister = unregister;
			Handler = Invoke;
		}

		/// <summary></summary>
		public EventHandler<E> Handler { get; }

		/// <summary></summary>
		public void Invoke(object? sender, E e)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null)
			{
				m_open_handler.Invoke(target, sender, e);
			}
			else if (m_unregister != null)
			{
				m_unregister(Handler);
				m_unregister = null;
			}
		}

		/// <summary></summary>
		public static implicit operator EventHandler<E>(WeakHandlerImpl<T, E> weh) { return weh.Handler; }
	}
	public class WeakPropChangedHandlerImpl<T> : IWeakPropChangedHandler
		where T : class
	{
		private delegate void OpenEventHandler(T @this, object? sender, PropertyChangedEventArgs args);

		private readonly WeakReference m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private Action<PropertyChangedEventHandler>? m_unregister;

		public WeakPropChangedHandlerImpl(PropertyChangedEventHandler event_handler, Action<PropertyChangedEventHandler> unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_unregister = unregister;
			Handler = Invoke;
		}

		/// <summary></summary>
		public PropertyChangedEventHandler Handler { get; }

		/// <summary></summary>
		public void Invoke(object? sender, PropertyChangedEventArgs args)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null)
			{
				m_open_handler.Invoke(target, sender, args);
			}
			else if (m_unregister != null)
			{
				m_unregister(Handler);
				m_unregister = null;
			}
		}

		/// <summary></summary>
		public static implicit operator PropertyChangedEventHandler(WeakPropChangedHandlerImpl<T> weh) { return weh.Handler; }
	}
	public class WeakCollectionChangedHandlerImpl<T> :IWeakCollectionChangedHandler
		where T : class
	{
		private delegate void OpenEventHandler(T @this, object? sender, NotifyCollectionChangedEventArgs args);

		private readonly WeakReference m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private Action<NotifyCollectionChangedEventHandler>? m_unregister;

		public WeakCollectionChangedHandlerImpl(NotifyCollectionChangedEventHandler event_handler, Action<NotifyCollectionChangedEventHandler> unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_unregister = unregister;
			Handler = Invoke;
		}

		/// <summary></summary>
		public NotifyCollectionChangedEventHandler Handler { get; }

		/// <summary></summary>
		public void Invoke(object? sender, NotifyCollectionChangedEventArgs args)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null)
			{
				m_open_handler.Invoke(target, sender, args);
			}
			else if (m_unregister != null)
			{
				m_unregister(Handler);
				m_unregister = null;
			}
		}

		/// <summary></summary>
		public static implicit operator NotifyCollectionChangedEventHandler(WeakCollectionChangedHandlerImpl<T> weh) { return weh.Handler; }
	}

	#endregion

	#region Weak Actions

	// Actions
	public class WeakActionImpl<T> :IWeakAction where T: class
	{
		private delegate void OpenAction(T @this);
		private readonly WeakReference m_target_ref;
		private readonly OpenAction    m_open_action;
		private Action<Action>?        m_unregister;

		public WeakActionImpl(Action action, Action<Action> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			Handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke()
		{
			var target = (T?)m_target_ref.Target;
			if (target != null) { m_open_action.Invoke(target); }
			else if (m_unregister != null) { m_unregister(Handler); m_unregister = null; }
		}
		public Action Handler { get; }
		public static implicit operator Action(WeakActionImpl<T> weak_action) { return weak_action.Handler; }
	}
	public class WeakActionImpl<T,T1> :IWeakAction<T1> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg);
		private readonly WeakReference m_target_ref;
		private readonly OpenAction    m_open_action;
		private Action<Action<T1>>?    m_unregister;

		public WeakActionImpl(Action<T1> action, Action<Action<T1>> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			Handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null) { m_open_action.Invoke(target, arg); }
			else if (m_unregister != null) { m_unregister(Handler); m_unregister = null; }
		}
		public Action<T1> Handler { get; }
		public static implicit operator Action<T1>(WeakActionImpl<T, T1> weak_action) { return weak_action.Handler; }
	}
	public class WeakActionImpl<T,T1,T2> :IWeakAction<T1,T2> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2);
		private readonly WeakReference    m_target_ref;
		private readonly OpenAction       m_open_action;
		private Action<Action<T1,T2>>?    m_unregister;

		public WeakActionImpl(Action<T1,T2> action, Action<Action<T1,T2>> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			Handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null) { m_open_action.Invoke(target, arg1, arg2); }
			else if (m_unregister != null) { m_unregister(Handler); m_unregister = null; }
		}
		public Action<T1, T2> Handler { get; }
		public static implicit operator Action<T1,T2>(WeakActionImpl<T,T1,T2> weak_action) { return weak_action.Handler; }
	}
	public class WeakActionImpl<T,T1,T2,T3> :IWeakAction<T1,T2,T3> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2, T3 arg3);
		private readonly WeakReference       m_target_ref;
		private readonly OpenAction          m_open_action;
		private Action<Action<T1,T2,T3>>?    m_unregister;

		public WeakActionImpl(Action<T1,T2,T3> action, Action<Action<T1,T2,T3>> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			Handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2, T3 arg3)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null) { m_open_action.Invoke(target, arg1, arg2, arg3); }
			else if (m_unregister != null) { m_unregister(Handler); m_unregister = null; }
		}
		public Action<T1, T2, T3> Handler { get; }
		public static implicit operator Action<T1,T2,T3>(WeakActionImpl<T,T1,T2,T3> weak_action) { return weak_action.Handler; }
	}
	public class WeakActionImpl<T,T1,T2,T3,T4> :IWeakAction<T1,T2,T3,T4> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
		private readonly WeakReference          m_target_ref;
		private readonly OpenAction             m_open_action;
		private Action<Action<T1,T2,T3,T4>>?    m_unregister;

		public WeakActionImpl(Action<T1,T2,T3,T4> action, Action<Action<T1,T2,T3,T4>> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			Handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			var target = (T?)m_target_ref.Target;
			if (target != null) { m_open_action.Invoke(target, arg1, arg2, arg3, arg4); }
			else if (m_unregister != null) { m_unregister(Handler); m_unregister = null; }
		}
		public Action<T1, T2, T3, T4> Handler { get; }
		public static implicit operator Action<T1, T2, T3, T4>(WeakActionImpl<T, T1, T2, T3, T4> weak_action) => weak_action.Handler;
	}

	#endregion
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Runtime.CompilerServices;
	using System.Threading;
	using Common;

	[TestFixture]
	public class TestWeakRef
	{
		// Notes:
		//  - These methods need to not be inlined so that the JIT doesn't
		//    have the option to extend the lifetimes of 'bob' and 'fred'.
		//  - These tests fail when run by 'ncrunch' when instrumentation is turned on
		//    because it does things to the GC that prevent collection

		private class Gun
		{
			private readonly List<string> m_collected;
			public Gun(List<string> collected)
			{
				m_collected = collected;
			}
			~Gun()
			{
				m_collected.Add("gun");
			}
			public event EventHandler? Firing;
			public event Action<Gun>? Bang;
			public event EventHandler<FiredArgs>? Fired;
			public void Shoot()
			{
				Firing?.Invoke(this, EventArgs.Empty);
				Bang?.Invoke(this);
				Fired?.Invoke(this, new FiredArgs("Bang!"));
			}
			public class FiredArgs :EventArgs
			{
				public FiredArgs(string noise)
				{
					Noise = noise;
				}
				public string Noise { get; }
			}
		}
		private class Target :INotifyPropertyChanged
		{
			private readonly List<string> m_collected;
			private readonly List<string> m_hit;
			public Target(string name, List<string> collected, List<string> hit)
			{
				m_collected = collected;
				m_hit = hit;
				Name = name;
				Dead = false;
			}
			~Target()
			{
				m_collected.Add(Name);
			}
			public string Name { get; }
			public bool Dead { get; private set; }
			public void OnHit(Gun _)
			{
				Dead = true;
				m_hit.Add(Name);
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Dead)));
			}
			public void OnFiring(object? s, EventArgs a)
			{
				m_hit.Add("Don't Shoot");
			}
			public void OnFired(object? s, Gun.FiredArgs a)
			{
				m_hit.Add(a.Noise);
			}
			public event PropertyChangedEventHandler? PropertyChanged;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void WeakActionsShootTargets(Gun gun, List<string> collected, List<string> hit)
		{
			Target? bob = new Target("bob", collected, hit);
			Target? fred = new Target("fred", collected, hit);
			gun.Bang += WeakRef.MakeWeak<Gun>(bob.OnHit, h => gun.Bang -= h);
			gun.Bang += fred.OnHit;
			gun.Shoot();
			Assert.True(hit.Contains("bob"));
			Assert.True(hit.Contains("fred"));

			hit.Clear();
			bob = null;
			fred = null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void WeakEventHandlersShootTargets(Gun gun, List<string> collected, List<string> hit)
		{
			Target? bob = new Target("bob", collected, hit);
			gun.Firing += WeakRef.MakeWeak(bob.OnFiring, eh => gun.Firing -= eh);
			gun.Bang += WeakRef.MakeWeak<Gun>(bob.OnHit, eh => gun.Bang -= eh);
			gun.Fired += WeakRef.MakeWeak<Gun.FiredArgs>(bob.OnFired, eh => gun.Fired -= eh);
			gun.Shoot();
			Assert.True(hit.Contains("Don't Shoot"));
			Assert.True(hit.Contains("bob"));
			Assert.True(hit.Contains("Bang!"));

			hit.Clear();
			bob = null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		void WeakEventPropChangedHandlersShootTargets(Gun gun, List<string> collected, List<string> hit, List<string> prop_change)
		{
			Target? bob = new Target("bob", collected, hit);
			bob.PropertyChanged += WeakRef.MakeWeak(HandlePropChanged, h => bob.PropertyChanged -= h);
			void HandlePropChanged(object? sender, PropertyChangedEventArgs e) => prop_change.Add($"{bob?.Name} {e.PropertyName}");

			gun.Bang += WeakRef.MakeWeak<Gun>(bob.OnHit, eh => gun.Bang -= eh);
			gun.Shoot();
			Assert.True(hit.Contains("bob"));
			Assert.True(prop_change.Contains("bob Dead"));

			hit.Clear();
			prop_change.Clear();
			bob = null;
		}

		[Test]
		public void WeakActions()
		{
			var collected = new List<string>();
			var hit = new List<string>();

			var gun = new Gun(collected);
			WeakActionsShootTargets(gun, collected, hit);

			// bob collected here, but not fred
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			Thread.Sleep(100);
			Assert.True(collected.Contains("bob"));
			Assert.False(collected.Contains("fred"));

			// fred still shot here
			gun.Shoot();
			Assert.False(hit.Contains("bob"));
			Assert.True(hit.Contains("fred"));
		}
		[Test]
		public void WeakEventHandlers()
		{
			var collected = new List<string>();
			var hit = new List<string>();

			var gun = new Gun(collected);
			WeakEventHandlersShootTargets(gun, collected, hit);

			// bob collected here
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			Thread.Sleep(100);
			Assert.True(collected.Contains("bob"));

			// No one shot here
			gun.Shoot();
			Assert.True(hit.Count == 0);
		}
		[Test]
		public void WeakPropertyChangedEventHandlers()
		{
			var collected = new List<string>();
			var hit = new List<string>();
			var prop_change = new List<string>();

			var gun = new Gun(collected);
			WeakEventPropChangedHandlersShootTargets(gun, collected, hit, prop_change);

			// bob collected here
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			Thread.Sleep(100);
			Assert.True(collected.Contains("bob"));

			// No one shot here
			gun.Shoot();
			Assert.True(hit.Count == 0);
			Assert.True(prop_change.Count == 0);
		}
		[Test]
		public void WeakCollectionChangedEventHandlers()
		{
			int count = 0;
			var list = new List<ObservableCollection<string>>();
			list.Add(new ObservableCollection<string>());
			list.Add(new ObservableCollection<string>());
			list.Add(new ObservableCollection<string>());
			foreach (var obs in list)
			{
				obs.CollectionChanged += WeakRef.MakeWeak(HandleCollectionChanged, h => obs.CollectionChanged -= h);
				void HandleCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) { ++count; }
			}

			list[0].Add("Zero");
			list[1].Add("One");
			list[2].Add("Two");
			Assert.True(count == 3);

			// Observables collected here
			list.Clear();
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
			Assert.True(count == 3);
		}
	}
}
#endif