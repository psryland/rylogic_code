//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;

namespace Rylogic.Common
{

	/// <summary>
	/// A Comparer implementation that is implicitly convertible
	/// from any other comparer or comparison delegate.</summary>
	public class Cmp<T> :IComparer<T>, IEqualityComparer<T>, IComparer, IEqualityComparer
	{
		// Notes:
		// - Use 'IComparer<T>' or 'IEqualityComparer<T>' for function parameters.
		//   Then use 'cmp = cmp ?? Cmp<T>.Default'
		// - Use Func<T,T,int> in preference to Comparison<T>
		// - This doesn't work, because 'T' is unknown in a lambda function:
		//   public static class Cmp
		//   {
		//       public static Cmp<T> From<T>(Func<T,T,bool> c) { return Cmp<T>.From(c); }
		//   }
		// - It's not possible to make a lambda implicitly convertible because C# requires
		//   the lambda to have a type before it resolves overloads and there is no
		//   'any lambda' type. (object, dynamic, Func<T,T,int> don't work)
		// - Do not make this implicitly convertible to Func<T,T,int> because then you can't
		//   overload with IComparer<T>
		// - Careful with LessThan/EqualTo, they're ambiguous. i.e. both are Func<T,T,bool>

		private readonly Func<T,T,int> m_cmp;
		private Cmp(Func<T,T,int> cmp)
		{
			m_cmp = cmp ?? Default.m_cmp;
		}
		public static Cmp<T> Default
		{
			get { return new Cmp<T>(Comparer<T>.Default.Compare); }
		}

		/// <summary>Compares 'lhs' to 'rhs' returning -1,0,1</summary>
		public int Compare(T lhs, T rhs)
		{
			return m_cmp(lhs, rhs);
		}
		public bool Equals(T lhs, T rhs)
		{
			return m_cmp(lhs, rhs) == 0;
		}
		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
		int IComparer.Compare(object x, object y)
		{
			return Compare((T)x, (T)y);
		}
		bool IEqualityComparer.Equals(object x, object y)
		{
			return Equals((T)x, (T)y);
		}
		int IEqualityComparer.GetHashCode(object obj)
		{
			return GetHashCode((T)obj);
		}

		// Convert To
		public static implicit operator Cmp<T>(Func<T,T,int> c) { return new Cmp<T>(c); }
		public static implicit operator Cmp<T>(Comparer<T>   c) { return new Cmp<T>(c.Compare); }

		// Construct From
		public static Cmp<T> From(Func<T,T,int> c)  { return c; }
		public static Cmp<T> From(IComparer<T> c)   { return new Cmp<T>(c.Compare); }
		public static Cmp<T> From(Func<T,T,bool> c) { return new Cmp<T>((l,r) => c(l,r) ? -1 : c(r,l) ? 1 : 0); } // c = Less than
	}
	public class Eql<T> :IEqualityComparer<T>, IEqualityComparer
	{
		// Same as: Func<T,T,int>
		private readonly Func<T,T,bool> m_eql;
		private Eql(Func<T,T,bool> eql)
		{
			m_eql = eql ?? Default.m_eql;
		}
		public static Eql<T> Default
		{
			get { return new Eql<T>(EqualityComparer<T>.Default.Equals); }
		}

		/// <summary>Equates 'lhs' to 'rhs' returning true,false</summary>
		public bool Equals(T lhs, T rhs)
		{
			return m_eql(lhs, rhs);
		}
		public int GetHashCode(T obj)
		{
			return obj.GetHashCode();
		}
		bool IEqualityComparer.Equals(object x, object y)
		{
			return Equals((T)x, (T)y);
		}
		int IEqualityComparer.GetHashCode(object obj)
		{
			return GetHashCode((T)obj);
		}

		// Convert To
		public static implicit operator Eql<T>(Func<T,T,bool> c) { return new Eql<T>(c); }
		public static implicit operator Eql<T>(Comparer<T> c)    { return new Eql<T>((l,r) => c.Compare(l,r) == 0); }

		// Construct From
		public static Eql<T> From(Func<T,T,bool> c) { return new Eql<T>(c); } // c = Equal
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestComparer
	{
		private IEnumerable<T> Func<T>(IEnumerable<T> lhs, IEnumerable<T> rhs, IComparer<T> cmp = null)
		{
			cmp = cmp ?? Cmp<T>.Default;
			var i = lhs.GetIterator();
			var j = rhs.GetIterator();
			for (;!i.AtEnd && !j.AtEnd;)
			{
				var c = cmp.Compare(i.Current, j.Current);
				if (c <= 0)
				{
					yield return i.Current;
					i.MoveNext();
				}
				if (c >= 0)
				{
					yield return j.Current;
					j.MoveNext();
				}
			}
		}

		[Test] public void Comparer()
		{
			var lhs = new[]{1,2,4,6,7,9};
			var rhs = new[]{1,3,4,5,8,10};
			Func(lhs, rhs, Cmp<int>.From((l,r) => l.CompareTo(r)));
		}
		[Test] public void TestEql()
		{
			var ints = new List<int>{0,1,2,3,4,5};
			ints.Exists(i=>i == 3);
		}
	}
}
#endif
