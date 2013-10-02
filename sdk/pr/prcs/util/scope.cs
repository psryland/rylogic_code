using System;

namespace pr.util
{
	/// <summary>An general purpose RAII scope</summary>
	public class Scope :IDisposable
	{
		private readonly Action m_on_exit;
		public static Scope Create(Action set, Action restore)
		{
			return new Scope(set, restore);
		}
		public Scope(Action on_enter, Action on_exit)
		{
			m_on_exit = on_exit;
			if (on_enter != null)
				on_enter();
		}
		public void Dispose()
		{
			if (m_on_exit != null)
				m_on_exit();
		}
	}

	/// <summary>An RAII scope with state</summary>
	public class Scope<TState> :IDisposable where TState :class
	{
		private readonly TState m_state;
		private readonly Action<TState> m_on_exit;

		public static Scope<TState> Create(Func<TState> set, Action<TState> restore)
		{
			return new Scope<TState>(set, restore);
		}
		public Scope(Func<TState> on_enter, Action<TState> on_exit)
		{
			m_on_exit = on_exit;
			if (on_enter != null)
				m_state = on_enter();
		}
		public void Dispose()
		{
			if (m_on_exit != null)
				m_on_exit(m_state);
		}
	}
}
