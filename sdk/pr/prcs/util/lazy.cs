//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using NUnit.Framework;

namespace pr.util
{
	/// <summary>Replacement for System.Lazy that has implicit conversion to T</summary>
	[DebuggerStepThrough] public class Lazy<T>
	{
		private readonly Func<T> m_func;
		private readonly bool    m_has_value;
		private T                m_result;
		
		public Lazy(Func<T> func) { m_func = func; m_has_value = false; }
		private Lazy(T value)     { m_result = value; m_has_value = true; }
		public T Value            { get { return m_has_value ? m_result : m_result = m_func(); } }
		public bool HasValue      { get { return m_has_value; } }
		
		// Values are implicitly convertable to Lazy<T> but the Lazy(T) constructor
		// is private to prevent accidently use such as: Lazy.New(ExpensiveFunction())
		public static implicit operator Lazy<T>(T value) { return new Lazy<T>(value); }
		public static implicit operator T(Lazy<T> value) { return value.Value; }
	}
	public static class Lazy
	{
		/// <summary>Helper for constructing Lazy expressions</summary>
		[DebuggerStepThrough] public static Lazy<T> New<T>(Func<T> func) { return new Lazy<T>(func); }
	}
	
	/// <summary>Utils unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		internal class Thing
		{
			public bool m_evaluated;
			public bool m_expect_call;
			
			public string GenerateString()
			{
				m_evaluated = true;
				return "<insert lots of work here>";
			}
			public void LazyCall(Lazy<string> str)
			{
				Assert.False(m_evaluated);
				string s = str;
				Assert.True(!m_expect_call || m_evaluated);
				m_evaluated = false;
			}
		}

		[Test] public static void TestLazy()
		{
			Thing thing = new Thing();
			
			thing.m_expect_call = true;
			thing.LazyCall(Lazy.New(thing.GenerateString));
			
			thing.m_expect_call = false;
			thing.LazyCall("immediate");
		}
	}
}
