//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;

namespace pr.common
{
	/// <summary>
	/// A Comparer implementation that is implicitly convertable
	/// from any other comparer or comparison delegate. Implement functions using
	/// this to allow callers to provide any sort of comparer or delegate</summary>
	public class Cmp<T> :Comparer<T> ,IEqualityComparer<T>
	{
		private readonly Func<T,T,int> m_cmp;

		/// <summary>Compares 'lhs' to 'rhs' returning -1,0,1</summary>
		public override int Compare(T lhs, T rhs) { return m_cmp(lhs, rhs); }
		public bool Equals(T lhs, T rhs)          { return m_cmp(lhs, rhs) == 0; }
		public int GetHashCode(T obj)             { return obj.GetHashCode(); }

		public static new Cmp<T> Default { get { return new Cmp<T>(Comparer<T>.Default.Compare); } }

		// Note, it's not possible to make a lambda implicitly convertable because
		// C# requires the lambda to have a type before it resolves overloads and
		// there is no 'any lambda' type. (object, dynamic don't work)
		private Cmp(Func<T,T,int> cmp) { m_cmp = cmp; }
		public static implicit operator Cmp<T>(Func<T,T,int>  c)   { return new Cmp<T>(c); }
		public static implicit operator Cmp<T>(Func<T,T,bool> c)   { return new Cmp<T>((l,r) => c(l,r) ? -1 : c(r,l) ? 1 : 0); }
		public static implicit operator Cmp<T>(Comparison<T>  c)   { return new Cmp<T>((l,r) => c(l,r)); }

		public static implicit operator Func<T,T,int> (Cmp<T> c)   { return c.m_cmp; }
		public static implicit operator Func<T,T,bool>(Cmp<T> c)   { return (l,r) => c.Compare(l,r) == 0; }
		public static implicit operator Eql<T>        (Cmp<T> c)   { return Eql<T>.From(c.m_cmp); }
		
		public static Cmp<T> From(Func<T,T,int>  c) { return (Cmp<T>)c; }
		public static Cmp<T> From(Func<T,T,bool> c) { return (Cmp<T>)c; }
		public static Cmp<T> From(Comparer<T>    c) { return (Cmp<T>)c; }
	}

	/// <summary>
	/// A generic IEqualityComparer implementation that is implicitly convertable
	/// from any other BinaryPredicate delegate or equality comparer. Implement
	/// functions using this to allow callers to provide any sort of comparer or delegate</summary>
	public class Eql<T> :EqualityComparer<T>
	{
		private readonly Func<T,T,bool> m_eql;

		/// <summary>Compares 'lhs' to 'rhs' returning true if equal</summary>
		public override bool Equals(T lhs, T rhs) { return m_eql(lhs,rhs); }
		public override  int GetHashCode(T obj) { return obj.GetHashCode(); }

		public static new Eql<T> Default { get { return new Eql<T>((l,r) => l.Equals(r)); } }

		private Eql(Func<T,T,bool> eql) { m_eql = eql; }
		public static implicit operator Eql<T>(Func<T,T,bool> c) { return new Eql<T>(c); }
		public static implicit operator Eql<T>(Func<T,T,int>  c) { return new Eql<T>((l,r) => c(l,r) == 0); }
		public static implicit operator Eql<T>(Comparer<T>    c) { return new Eql<T>((l,r) => c.Compare(l,r) == 0); }
		public static implicit operator Eql<T>(Comparison<T>  c) { return new Eql<T>((l,r) => c(l,r) == 0); }

		public static implicit operator Func<T,T,bool>(Eql<T> c) { return c.m_eql; }
		
		public static Eql<T> From(Func<T,T,bool> c) { return (Eql<T>)c; }
		public static Eql<T> From(Func<T,T,int>  c) { return (Eql<T>)c; }
		public static Eql<T> From(Comparer<T>    c) { return (Eql<T>)c; }
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestComparer
	{
		[Test] public void TestEql()
		{
			List<int> ints = new List<int>{0,1,2,3,4,5};
			ints.Exists(i=>i == 3);
		}
	}
}
#endif
