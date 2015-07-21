using System;

namespace pr.util
{
	/// <summary>An general purpose RAII scope</summary>
	public class Scope :IDisposable
	{
		private readonly Action m_on_exit;

		/// <summary>Construct the scope object</summary>
		public static Scope Create(Action set, Action restore)
		{
			return new Scope(set, restore);
		}

		/// <summary>Use 'Create'</summary>
		private Scope(Action on_enter, Action on_exit)
		{
			// Note: important to save 'on_exit' before calling 'on_enter' in case it throws
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
	public class Scope<TState> :IDisposable
	{
		private readonly Action<TState> m_on_exit;

		/// <summary>The state object created on construction</summary>
		public TState State { get; private set; }

		/// <summary>Allow implicit conversion to the stored state type</summary>
		public static implicit operator TState(Scope<TState> s) { return s.State; }

		/// <summary>Construct the scope object</summary>
		public static Scope<TState> Create(Func<TState> set, Action<TState> restore)
		{
			return new Scope<TState>(set, restore);
		}

		/// <summary>Use 'Create'</summary>
		private Scope(Func<TState> on_enter, Action<TState> on_exit)
		{
			m_on_exit = on_exit;
			if (on_enter != null)
				State = on_enter();
		}
		public void Dispose()
		{
			if (m_on_exit != null)
				m_on_exit(State);
		}
	}
}
