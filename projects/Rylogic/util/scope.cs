using System;

namespace pr.util
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

		/// <summary>Construct a subclassed scope. Handy for allocated memory, etc</summary>
		public static TScope Create<TScope>(Action<TScope> on_enter, Action<TScope> on_exit) where TScope:Scope, new()
		{
			var s = new TScope();
			s.Init(() => on_enter(s), () => on_exit(s));
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
		protected Scope()
		{}
		
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

	/// <summary>Scope for an IntPtr</summary>
	public class IntPtrScope :Scope
	{
		public IntPtr Ptr;
	
		/// <summary>Allow implicit conversion to IntPtr</summary>
		public static implicit operator IntPtr(IntPtrScope s) { return s.Ptr; }
	}

	/// <summary>Scope for flags</summary>
	public class FlagScope :Scope
	{
		public bool Flag;

		/// <summary>Allow implicit conversion to IntPtr</summary>
		public static implicit operator bool(FlagScope s) { return s.Flag; }
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using util;

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

		internal class MyScope :Scope
		{
			public int[] handle;
		}
		[Test] public void TestScope_Subclassed()
		{
			// Pretend 'handle' is some CoTask memory or something. Scope.Create<MyScope> can used
			// to pass out a specialised Scope with convenient fields.
			using (var myscope = Scope.Create<MyScope>(s => s.handle = new []{42}, s => s.handle = null))
			{
				Assert.True(myscope.handle[0] == 42);
			}
		}
	}
}
#endif
