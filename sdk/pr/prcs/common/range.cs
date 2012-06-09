//***********************************************
// Range
//  Copyright © Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.common;

namespace pr.common
{
	// Note about invariants:
	// It is valid for a range to be inside out (i.e. as the initial state when
	// finding a bounding range). However some methods assume that this range has
	// a count >= 0. These functions should contain appropriate asserts.
	
	/// <summary>A range over [m_begin,m_end)</summary>
	public struct Range
	{
		/// <summary>The index of the first element in the range</summary>
		public long m_begin;
		
		/// <summary>The index of one past the last element in the range</summary>
		public long m_end;

		/// <summary>The default empty range</summary>
		public static readonly Range Zero = new Range{m_begin = 0, m_end = 0};

		/// <summary>Construct from an index range</summary>
		public Range(long begin, long end) { m_begin = begin; m_end = end; }
		
		/// <summary>True if the range spans zero elements</summary>
		public bool Empty                  { get { return m_end == m_begin; } }

		/// <summary>Get/Set the number of elements in the range</summary>
		public long Count                 { get { return m_end - m_begin; } set { m_end = m_begin + value; } }

		/// <summary>Gets the first index in the range, or moves the range so that 'm_begin' is at a given index. Note, moves 'm_end' so that 'Count' is unchanged</summary>
		public long First                 { get { return m_begin; } set { long count = Count; m_begin = value; m_end = value + count; } }

		/// <summary>Gets one past the last index in the range, or moves the range so that 'm_end' is at a given index. Note, moves 'm_begin' so that 'Count' is unchanged</summary>
		public long Last                  { get { return m_end; } set { long count = Count; m_begin = value - count; m_end = value; } }

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Count', i.e. 'm_begin' and 'm_end' are both potentially moved</summary>
		public long Mid                   { get { return (m_begin + m_end) / 2; } set { long count = Count; m_begin = value - count/2; m_end = value + (count+1)/2; } }

		/// <summary>Empty the range and reset to the zero'th index</summary>
		public void Clear()               { m_begin = m_end = 0; }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate { get { for (long i = m_begin; i != m_end; ++i) yield return i; } }

		/// <summary>Returns true if 'index' is within this range</summary>
		public bool Contains(long index)  { return m_begin <= index && index < m_end; }

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		public bool Contains(Range rng) 
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			return m_begin <= rng.m_begin && rng.m_end <= m_end;
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompase(Range rng)
		{
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			m_begin = Math.Min(m_begin ,rng.m_begin);
			m_end   = Math.Max(m_end   ,rng.m_end  );
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompase' except this range isn't modified.</summary>
		public Range Union(Range rng)
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			return new Range(Math.Min(m_begin, rng.m_begin), Math.Max(m_end, rng.m_end));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public Range Intersect(Range rng)
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			if (rng.m_end <= m_begin) return new Range(m_begin, m_begin);
			if (rng.m_begin >= m_end) return new Range(m_end, m_end);
			return new Range(Math.Max(m_begin, rng.m_begin), Math.Min(m_end, rng.m_end));
		}
		
		/// <summary>Move the range by an offset</summary>
		public void Shift(long ofs)
		{
			m_begin += ofs;
			m_end   += ofs;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return "["+m_begin+","+m_end+")";
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(Range)) return false;
			return Equals((Range)obj);
		}
		public bool Equals(Range other)
		{
			return other.m_begin == m_begin && other.m_end == m_end;
		}
		public override int GetHashCode()
		{
			unchecked { return (m_begin.GetHashCode()*397) ^ m_end.GetHashCode(); }
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestRange()
		{
			Range a = new Range(-4, -1);
			Range b = new Range(-1,  3);
			Range c = new Range( 0,  5);
			Range d = new Range( 7, 12);
			
			// Intersect
			Assert.AreEqual(a               , a.Intersect(a));
			Assert.AreEqual(new Range(-1,-1), a.Intersect(b));
			Assert.AreEqual(new Range(-1,-1), a.Intersect(c));
			Assert.AreEqual(new Range(-1,-1), b.Intersect(a));
			Assert.AreEqual(new Range( 0, 3), b.Intersect(c));
			Assert.AreEqual(new Range( 3, 3), b.Intersect(d));
			Assert.AreEqual(new Range( 0, 0), c.Intersect(a));
			Assert.AreEqual(new Range( 0, 3), c.Intersect(b));
			Assert.AreEqual(new Range( 5, 5), c.Intersect(d));
		}
	}
}
#endif
