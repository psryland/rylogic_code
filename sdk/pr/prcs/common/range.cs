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
		public long Begin;
		
		/// <summary>The index of one past the last element in the range</summary>
		public long End;

		/// <summary>The default empty range</summary>
		public static readonly Range Zero = new Range{Begin = 0, End = 0};

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly Range Invalid = new Range{Begin = long.MaxValue, End = long.MinValue};

		/// <summary>Construct from an index range</summary>
		public Range(long begin, long end) { Begin = begin; End = end; }
		
		/// <summary>True if the range spans zero elements</summary>
		public bool Empty                  { get { return End == Begin; } }

		/// <summary>Get/Set the number of elements in the range</summary>
		public long Count                  { get { return End - Begin; } set { End = Begin + value; } }

		/// <summary>Gets the first index in the range, or moves the range so that 'Begin' is at a given index.<para/>Note, moves 'End' so that 'Count' is unchanged</summary>
		public long First                  { get { return Begin; } set { long count = Count; Begin = value; End = value + count; } }

		/// <summary>Gets one past the last index in the range, or moves the range so that 'End' is at a given index.<para/>Note, moves 'Begin' so that 'Count' is unchanged</summary>
		public long Last                   { get { return End; } set { long count = Count; Begin = value - count; End = value; } }

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Count', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public long Mid                    { get { return (Begin + End) / 2; } set { long count = Count; Begin = value - count/2; End = value + (count+1)/2; } }

		/// <summary>Empty the range and reset to the zero'th index</summary>
		public void Clear()                { Begin = End = 0; }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate { get { for (long i = Begin; i != End; ++i) yield return i; } }

		/// <summary>Returns true if 'index' is within this range</summary>
		public bool Contains(long index)   { return Begin <= index && index < End; }

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		public bool Contains(Range rng) 
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			return Begin <= rng.Begin && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompase(Range rng)
		{
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			Begin = Math.Min(Begin ,rng.Begin);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompase' except this range isn't modified.</summary>
		public Range Union(Range rng)
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			return new Range(Math.Min(Begin, rng.Begin), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public Range Intersect(Range rng)
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			if (rng.End <= Begin) return new Range(Begin, Begin);
			if (rng.Begin >= End) return new Range(End, End);
			return new Range(Math.Max(Begin, rng.Begin), Math.Min(End, rng.End));
		}
		
		/// <summary>Move the range by an offset</summary>
		public void Shift(long ofs)
		{
			Begin += ofs;
			End   += ofs;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return "["+Begin+","+End+")";
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(Range)) return false;
			return Equals((Range)obj);
		}
		public bool Equals(Range other)
		{
			return other.Begin == Begin && other.End == End;
		}
		public override int GetHashCode()
		{
			unchecked { return (Begin.GetHashCode()*397) ^ End.GetHashCode(); }
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
