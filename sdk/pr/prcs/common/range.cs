//***********************************************
// Range
//  Copyright © Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using pr.common;
using pr.maths;

namespace pr.common
{
	// Note about invariants:
	// It is valid for a range to be inside out (i.e. as the initial state when
	// finding a bounding range). However some methods assume that this range has
	// a count >= 0. These functions should contain appropriate asserts.

	// Note, there is no Range<T> because T cannot be constrained to value types with simple maths operators :-/

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

		// Casting helpers
		public int Begini                  { get { return (int)Begin; } }
		public int Endi                    { get { return (int)End;   } }
		public int Counti                  { get { return (int)Count; } }
		public int Firsti                  { get { return (int)First; } }
		public int Lasti                   { get { return (int)Last;  } }
		public int Midi                    { get { return (int)Mid;   } }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate { get { for (long i = Begin; i != End; ++i) yield return i; } }

		/// <summary>Returns true if 'index' is within the range [Begin,End) (i.e. end exclusive)</summary>
		[Pure] public bool Contains(long index)
		{
			return Begin <= index && index < End;
		}

		/// <summary>Returns true if 'index' is within the range [Begin,End] (i.e. inclusive)</summary>
		[Pure] public bool ContainsInclusive(long index)
		{
			return Begin <= index && index <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		[Pure] public bool Contains(Range rng)
		{
			Debug.Assert(Count >= 0, "this range is inside out");
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			return Begin <= rng.Begin && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'x'</summary>
		public void Encompase(long x)
		{
			Begin = Math.Min(Begin , x);
			End   = Math.Max(End   , x);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompase(Range rng)
		{
			Debug.Assert(rng.Count >= 0, "'rng' is inside out");
			Begin = Math.Min(Begin ,rng.Begin);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>Returns a range scaled by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public Range Scale(float scale)
		{
			var count = Count * scale;
			return new Range(Begin, (long)(Begin + Count*scale)){Mid = Mid};
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
		public Range Shift(long ofs)
		{
			return new Range(Begin + ofs, End + ofs);
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static Range Clamp(Range x, Range range)
		{
			return new Range(
				Maths.Clamp(x.Begin, range.Begin, range.End),
				Maths.Clamp(x.End, range.Begin, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Count > range.Count</summary>
		public static Range Constrain(Range x, Range range)
		{
			if (x.Begin < range.Begin) x = x.Shift(range.Begin - x.Begin);
			if (x.End   > range.End  ) x = x.Shift(range.End - x.End);
			if (x.Begin < range.Begin) x.Begin = range.Begin;
			return x;
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
