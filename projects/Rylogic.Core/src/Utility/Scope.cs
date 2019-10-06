using System;
using System.Threading.Tasks;

namespace Rylogic.Utility
{
	/// <summary>An general purpose RAII scope</summary>
	public class Scope :IDisposable
	{
		/// <summary>Allow subclasses to inherit without having to forward the on_enter/on_exit constructor</summary>
		public Scope() { }
		protected Scope(Action on_enter, Action on_exit) // Use 'Create'
		{
			Init(on_enter, on_exit);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			if (m_on_exit != null)
				m_on_exit();
		}

		/// <summary>Clean up delegate</summary>
		private Action? m_on_exit;

		/// <summary>Construct the scope object</summary>
		public static Scope Create(Action? on_enter, Action? on_exit)
		{
			var s = new Scope();
			s.Init(on_enter, on_exit);
			return s;
		}
		public static async Task<Scope> CreateAsync(Func<Task> on_enter, Action on_exit)
		{
			var s = new Scope();
			await s.InitAsync(on_enter, on_exit);
			return s;
		}

		/// <summary>Create a scope around a value</summary>
		public static Scope<T> Create<T>(Func<T> on_enter, Action<T>? on_exit)
		{
			var s = new Scope<T>();
			s.Init(() => s.Value = on_enter(), () => on_exit?.Invoke(s.Value));
			return s;
		}
		public static async Task<Scope<T>> CreateAsync<T>(Func<Task<T>> on_enter, Action<T> on_exit)
		{
			var s = new Scope<T>();
			await s.InitAsync(async () => s.Value = await on_enter(), () => on_exit(s.Value));
			return s;
		}

		/// <summary>Record 'on_exit' and call 'on_enter'</summary>
		protected void Init(Action? on_enter, Action? on_exit)
		{
			// Note: important to save 'on_exit' before calling 'on_enter' in case it throws
			m_on_exit = on_exit;
			if (on_enter != null)
				on_enter();
		}
		protected async Task<Scope> InitAsync(Func<Task> on_enter, Action on_exit)
		{
			// Note: important to save 'on_exit' before calling 'on_enter' in case it throws
			m_on_exit = on_exit;
			if (on_enter != null) await on_enter();
			return this;
		}
	}

	/// <summary>Scope for a generic type 'T'</summary>
	public class Scope<T> :Scope
	{
		public Scope()
		{
			Value = default!;
		}

		/// <summary>Stored value for the duration of the scope</summary>
		public T Value;

		/// <summary>Allow implicit conversion to T</summary>
		public static implicit operator T(Scope<T> x) { return x.Value; }
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Utility;

	[TestFixture] public class ScopeTests
	{
		[Test] public void TestScope_Simple()
		{
			var flag = new bool[1];
			
			Assert.True(flag[0] == false);
			using (Scope.Create(() => flag[0] = true, () => flag[0] = false))
				Assert.True(flag[0] == true);
			Assert.True(flag[0] == false);
		}
		[Test] public void TestScope_WrappedValue()
		{
			using (var s = Scope.Create(() => (IntPtr)0x0123beef, p => p = IntPtr.Zero))
			{
				Assert.True(s.Value == (IntPtr)0x0123beef);
			}
		}
	}
}
#endif
