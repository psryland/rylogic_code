//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace pr.util
{
	/// <summary>Create a generic IComparer implementation from a Comparison delegate</summary>
	public class Cmp<T> :IComparer<T>
	{
		private readonly Comparison<T> m_cmp;
		public Cmp(Comparison<T> cmp)                       { m_cmp = cmp; }
		public int Compare(T lhs, T rhs)                    { return m_cmp(lhs, rhs); }
		public static Cmp<T> From(Comparison<T> comparison) { return new Cmp<T>(comparison); }
	}

	/// <summary>Create a generic IEqualityComparer implementation from a BinaryPredicate delegate</summary>
	public class Eql<T> :IEqualityComparer<T>
	{
		private readonly Func<T,T,bool> m_eql;
		public Eql(Func<T,T,bool> eql)                { m_eql = eql; }
		public static Eql<T> From(Func<T,T,bool> eql) { return new Eql<T>(eql); }
		public bool Equals(T lhs, T rhs)              { return m_eql(lhs,rhs); }
		public int GetHashCode(T obj)                 { return obj.GetHashCode(); }
	}

	/// <summary>Utils unit tests</summary>
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestEql()
		{
			List<int> ints = new List<int>{0,1,2,3,4,5};
			ints.Exists(i=>i == 3);
		}
	}
}
