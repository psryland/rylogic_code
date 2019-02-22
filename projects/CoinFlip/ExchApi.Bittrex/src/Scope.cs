using System;

namespace Bittrex.API
{
	/// <summary>An general purpose RAII scope</summary>
	public class Scope :IDisposable
	{
		private Action m_on_exit;

		/// <summary>Construct the scope object</summary>
		public static Scope Create(Action on_enter, Action on_exit)
		{
			var s = new Scope();
			s.Init(on_enter, on_exit);
			return s;
		}

		/// <summary>Create a scope around a value</summary>
		public static Scope<T> Create<T>(Func<T> on_enter, Action<T> on_exit)
		{
			var s = new Scope<T>();
			s.Init(() => s.Value = on_enter(), () => on_exit(s.Value));
			return s;
		}

		/// <summary>Record 'on_exit' and call 'on_enter'</summary>
		protected void Init(Action on_enter, Action on_exit)
		{
			// Note: important to save 'on_exit' before calling 'on_enter' in case it throws
			m_on_exit = on_exit;
			if (on_enter != null)
				on_enter();
		}

		/// <summary>Allow subclasses to inherit without having to forward the on_enter/on_exit constructor</summary>
		public Scope() {}
		
		/// <summary>Use 'Create'</summary>
		protected Scope(Action on_enter, Action on_exit)
		{
			Init(on_enter, on_exit);
		}
		public void Dispose()
		{
			if (m_on_exit != null)
				m_on_exit();
		}
	}

	/// <summary>Scope for a generic type 'T'</summary>
	public class Scope<T> :Scope
	{
		public T Value;

		/// <summary>Allow implicit conversion to T</summary>
		public static implicit operator T(Scope<T> x) { return x.Value; }
	}
}
