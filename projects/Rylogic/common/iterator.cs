using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pr.common
{
	/// <summary>Iterator wrapper for IEnumerator</summary>
	public class Iterator<T> :IEnumerator<T>
	{
		private IEnumerator<T> m_enumer;
		private int m_index;
		private bool m_last;

		public Iterator(IEnumerator<T> enumer)
		{
			m_enumer = enumer;
			m_index = -1;
			m_last = false;
			MoveNext();
		}
		public Iterator(Iterator<T> iter)
		{
			m_enumer = iter.m_enumer;
			m_index = iter.m_index;
			m_last = iter.m_last;
		}
		public void Dispose()
		{
			m_enumer.Dispose();
			m_last = true;
			m_index = -1;
		}

		/// <summary>The index of the current iteration</summary>
		public int Index { get { return m_index; } }

		/// <summary>The item pointed to by the iterator</summary>
		public T Current
		{
			get
			{
				if (m_last) throw new ArgumentOutOfRangeException();
				return m_enumer.Current;
			}
		}
		object IEnumerator.Current
		{
			get { return Current; }
		}

		/// <summary>Advance to the next item. Throws if called on the end of a range.</summary>
		public bool MoveNext()
		{
			if (m_last) throw new ArgumentOutOfRangeException();
			++m_index;
			return m_last = !m_enumer.MoveNext();
		}

		/// <summary>Reset the iterator to one before the start of the range</summary>
		public void Reset()
		{
			m_enumer.Reset();
			m_last = false;
			m_index = -1;
		}

		/// <summary>True if the iterator is at the end of the range</summary>
		public bool AtEnd
		{
			get { return m_last; }
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
			return new Iterator<T>(x.GetEnumerator());
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using common;
	using util;

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
	}
}
#endif