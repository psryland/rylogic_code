using System;

namespace Rylogic.Utility
{
	public sealed class Ref<T>
	{
		// Usage:
		//   Ref<string> M() 
		//   {
		//       string x = "hello";
		//       Ref<string> rx = new Ref<string>(()=>x, v=>{x=v;});
		//       rx.Value = "goodbye";
		//       Console.WriteLine(x); // goodbye
		//       return rx;
		//   }
		private readonly Func<T>?   m_get;
		private readonly Action<T>? m_set;
		private T m_value;

		public Ref(T t)
		{
			m_value = t;
			m_get = null;
			m_set = null;
		}
		public Ref(Func<T> get, Action<T> set)
		{
			m_value = default!;
			m_get = get;
			m_set = set;
		}
		public T Value
		{
			get { return m_get != null ? m_get() : m_value; }
			set { if (m_set != null) { m_set(value); } else { m_value = value; } }
		}
		
		// Allows:
		//  Ref<int> ri =...;
		//  int i = ri;
		public static implicit operator T(Ref<T> v)
		{
			return v.Value;
		}

		// Allows:
		//  int i = ...;
		//  Ref<int> ri = i;
		public static implicit operator Ref<T>(T v)
		{
			return new Ref<T>(v);
		}
	}
}
