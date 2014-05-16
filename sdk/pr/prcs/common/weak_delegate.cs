//***********************************************
// Weak Delegate
//  Copyright (c) Rylogic Ltd 2010
//***********************************************

// http://diditwith.net/PermaLink,guid,aacdb8ae-7baa-4423-a953-c18c1c7940ab.aspx

using System;

namespace pr.common
{
	public delegate void UnregisterEventHandler<E>    (EventHandler<E> event_handler) where E: EventArgs;
	public delegate void UnregisterEventHandler       (EventHandler event_handler);
	public delegate void UnregisterAction             (Action action);
	public delegate void UnregisterAction<T1>         (Action<T1> action);
	public delegate void UnregisterAction<T1,T2>      (Action<T1,T2> action);
	public delegate void UnregisterAction<T1,T2,T3>   (Action<T1,T2,T3> action);
	public delegate void UnregisterAction<T1,T2,T3,T4>(Action<T1,T2,T3,T4> action);

	public interface IWeakEventHandler<E> where E: EventArgs { EventHandler<E> Handler {get;} }
	public interface IWeakEventHandler       { EventHandler       Handler {get;} }
	public interface IWeakAction             { Action             Handler {get;} }
	public interface IWeakAction<T1>         { Action<T1>         Handler {get;} }
	public interface IWeakAction<T1,T2>      { Action<T1,T2>      Handler {get;} }
	public interface IWeakAction<T1,T2,T3>   { Action<T1,T2,T3>   Handler {get;} }
	public interface IWeakAction<T1,T2,T3,T4>{ Action<T1,T2,T3,T4>Handler {get;} }

	// Event Handler *****************************************************

	public class WeakEventHandler<T> :IWeakEventHandler where T: class
	{
		private delegate void OpenEventHandler(T @this, object sender, EventArgs args);
		private readonly WeakReference    m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private readonly EventHandler     m_handler;
		private UnregisterEventHandler    m_unregister;

		public WeakEventHandler(EventHandler event_handler, UnregisterEventHandler unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(object sender, EventArgs args)
		{
			var target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_handler.Invoke(target, sender, args); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public EventHandler Handler
		{
			get { return m_handler; }
		}
		public static implicit operator EventHandler(WeakEventHandler<T> weh)
		{
			return weh.m_handler;
		}
	}
	public class WeakEventHandler<T, E> :IWeakEventHandler<E> where T: class where E: EventArgs
	{
		private delegate void OpenEventHandler(T @this, object sender, E e);
		private readonly WeakReference    m_target_ref;
		private readonly OpenEventHandler m_open_handler;
		private readonly EventHandler<E>  m_handler;
		private UnregisterEventHandler<E> m_unregister;

		public WeakEventHandler(EventHandler<E> event_handler, UnregisterEventHandler<E> unregister)
		{
			m_target_ref = new WeakReference(event_handler.Target);
			m_open_handler = (OpenEventHandler)Delegate.CreateDelegate(typeof(OpenEventHandler), null, event_handler.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(object sender, E e)
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_handler.Invoke(target, sender, e); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public EventHandler<E> Handler                                              { get { return m_handler; } }
		public static implicit operator EventHandler<E>(WeakEventHandler<T, E> weh) { return weh.m_handler; }
	}

	// 0 args *****************************************************

	public class WeakAction<T> :IWeakAction where T: class
	{
		private delegate void OpenAction(T @this);
		private readonly WeakReference m_target_ref;
		private readonly OpenAction    m_open_action;
		private readonly Action        m_handler;
		private UnregisterAction       m_unregister;

		public WeakAction(Action action, UnregisterAction unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke()
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_action.Invoke(target); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public Action Handler                                             { get { return m_handler; } }
		public static implicit operator Action(WeakAction<T> weak_action) { return weak_action.m_handler; }
	}

	// 1 arg *****************************************************

	public class WeakAction<T,T1> :IWeakAction<T1> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg);
		private readonly WeakReference m_target_ref;
		private readonly OpenAction    m_open_action;
		private readonly Action<T1>    m_handler;
		private UnregisterAction<T1>   m_unregister;

		public WeakAction(Action<T1> action, UnregisterAction<T1> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg)
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_action.Invoke(target, arg); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public Action<T1> Handler                                                 { get { return m_handler; } }
		public static implicit operator Action<T1>(WeakAction<T, T1> weak_action) { return weak_action.m_handler; }
	}

	// 2 args *****************************************************

	public class WeakAction<T,T1,T2> :IWeakAction<T1,T2> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2);
		private readonly WeakReference    m_target_ref;
		private readonly OpenAction       m_open_action;
		private readonly Action<T1,T2>    m_handler;
		private UnregisterAction<T1,T2>   m_unregister;

		public WeakAction(Action<T1,T2> action, UnregisterAction<T1,T2> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2)
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_action.Invoke(target, arg1, arg2); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public Action<T1,T2> Handler                                                   { get { return m_handler; } }
		public static implicit operator Action<T1,T2>(WeakAction<T,T1,T2> weak_action) { return weak_action.m_handler; }
	}

	// 3 args *****************************************************

	public class WeakAction<T,T1,T2,T3> :IWeakAction<T1,T2,T3> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2, T3 arg3);
		private readonly WeakReference       m_target_ref;
		private readonly OpenAction          m_open_action;
		private readonly Action<T1,T2,T3>    m_handler;
		private UnregisterAction<T1,T2,T3>   m_unregister;

		public WeakAction(Action<T1,T2,T3> action, UnregisterAction<T1,T2,T3> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2, T3 arg3)
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_action.Invoke(target, arg1, arg2, arg3); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public Action<T1,T2,T3> Handler                                                      { get { return m_handler; } }
		public static implicit operator Action<T1,T2,T3>(WeakAction<T,T1,T2,T3> weak_action) { return weak_action.m_handler; }
	}

	// 4 args *****************************************************

	public class WeakAction<T,T1,T2,T3,T4> :IWeakAction<T1,T2,T3,T4> where T: class
	{
		private delegate void OpenAction(T @this, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
		private readonly WeakReference          m_target_ref;
		private readonly OpenAction             m_open_action;
		private readonly Action<T1,T2,T3,T4>    m_handler;
		private UnregisterAction<T1,T2,T3,T4>   m_unregister;

		public WeakAction(Action<T1,T2,T3,T4> action, UnregisterAction<T1,T2,T3,T4> unregister)
		{
			m_target_ref = new WeakReference(action.Target);
			m_open_action = (OpenAction)Delegate.CreateDelegate(typeof(OpenAction), null, action.Method);
			m_handler = Invoke;
			m_unregister = unregister;
		}
		public void Invoke(T1 arg1, T2 arg2, T3 arg3, T4 arg4)
		{
			T target = (T)m_target_ref.Target;
			if      (target != null)       { m_open_action.Invoke(target, arg1, arg2, arg3, arg4); }
			else if (m_unregister != null) { m_unregister(m_handler); m_unregister = null; }
		}
		public Action<T1,T2,T3,T4> Handler                                                         { get { return m_handler; } }
		public static implicit operator Action<T1,T2,T3,T4>(WeakAction<T,T1,T2,T3,T4> weak_action) { return weak_action.m_handler; }
	}
}