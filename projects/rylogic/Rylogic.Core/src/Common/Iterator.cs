using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rylogic.Common
{
	/// <summary>Iterator wrapper for IEnumerator</summary>
	[DebuggerDisplay("Idx={Index} {Current}")]
	public class Iterator<T> :IEnumerator<T>
	{
		private readonly IEnumerator<T> m_enumer; // The enumerator that keeps track of where we're up to
		private IEnumerable<T> m_enumerable;      // The enumeration source

		public Iterator(IEnumerable<T> enumerable)
		{
			m_enumerable = enumerable;
			m_enumer = m_enumerable.GetEnumerator();
			Index = -1;
			AtEnd = false;
			MoveNext();
		}
		public Iterator(Iterator<T> iter)
		{
			m_enumerable = iter.m_enumerable;
			m_enumer = m_enumerable.Skip(iter.Index).GetEnumerator(); // Advance to the same position as 'iter'
			Index = iter.Index;
			AtEnd = !m_enumer.MoveNext();
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			m_enumerable = null!;
			m_enumer.Dispose();
			AtEnd = true;
			Index = -1;
		}

		/// <summary>The index of the current iteration</summary>
		public int Index { get; private set; }

		/// <summary>True if the iterator is at the end of the range</summary>
		public bool AtEnd { get; private set; }

		/// <summary>The item pointed to by the iterator</summary>
		public T Current
		{
			[DebuggerStepThrough] get
			{
				if (AtEnd) throw new ArgumentOutOfRangeException();
				return m_enumer.Current;
			}
		}
		object? IEnumerator.Current => Current;

		/// <summary>Advance to the next item. Throws if called on the end of a range.</summary>
		public bool MoveNext()
		{
			if (AtEnd) throw new ArgumentOutOfRangeException();
			++Index;
			AtEnd = !m_enumer.MoveNext();
			return !AtEnd;
		}

		/// <summary>Reset the iterator to one before the start of the range</summary>
		public void Reset()
		{
			m_enumer.Reset();
			AtEnd = false;
			Index = -1;
		}

		/// <summary>Equivalent to calling MoveNext(), followed by Current. Basically pre increment and dereference</summary>
		public T NextThenCurrent()
		{
			MoveNext();
			return Current;
		}

		/// <summary>Equivalent to calling Current, followed by MoveNext(). Basically dereference and post increment</summary>
		public T CurrentThenNext()
		{
			var curr = Current;
			MoveNext();
			return curr;
		}

		/// <summary>Return the range of values as a co-routine. (Does not change this iterator)</summary>
		public IEnumerable<T> Enumerate()
		{
			for (var i = new Iterator<T>(this); !i.AtEnd; i.MoveNext())
				yield return i.Current;
		}

		// Be careful if you implement operator ++.
		// C# only allows you to implement pre-increment and supposedly
		// implements post increment for you.. except it doesn't work.
	}

	public static class Iterator
	{
		/// <summary>Return an iterator to the beginning of the range</summary>
		public static Iterator<T> GetIterator<T>(this IEnumerable<T> x)
		{
			return new Iterator<T>(x);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Common;
	using Utility;

	[TestFixture] public class TestIterator
	{
		[Test] public void Iter()
		{
			var items = new[]{1, 2, 3, 4, 5, 6, 7}.ToList();
			var iout = new int[items.Count];

			for (var i = items.GetIterator(); !i.AtEnd; i.MoveNext())
				iout[i.Index] = i.Current;
				
			Assert.True(items.SequenceEqual(iout));
		}
		[Test] public void ValueSemantics()
		{
			var items = new[]{1, 2, 3, 4, 5, 6, 7}.ToList();
			var iout = new int[items.Count];

			var i = items.GetIterator();
			var j = new Iterator<int>(i);

			for (; i.Index != 5; i.MoveNext())
				iout[i.Index] = i.Current;

			Assert.True(i.Index == 5);
			Assert.True(j.Index == 0);

			var k = new Iterator<int>(i);
			Assert.True(k.Index == 5);
			for (; k.Index != 7; k.MoveNext())
				iout[k.Index] = k.Current;

			Assert.True(items.SequenceEqual(iout));

			for (; !j.AtEnd; j.MoveNext())
				Assert.True(j.Current == items[j.Index]);
		}
	}
}
#endif