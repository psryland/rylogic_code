//***********************************************
// Range
//  Copyright © Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using pr.extn;
using pr.maths;

namespace pr.common
{
	// Note about invariants:
	// It is valid for a range to be inside out (i.e. as the initial state when
	// finding a bounding range). However some methods assume that this range has
	// a count >= 0. These functions should contain appropriate asserts.

	// Note, there is no Range<T> because T cannot be constrained to value types with simple maths operators :-/

	/// <summary>A range over [Begin,End)</summary>
	public struct Range
	{
		/// <summary>The value of the first element in the range</summary>
		public long Begin;

		/// <summary>The value of one past the last element in the range</summary>
		public long End;

		/// <summary>The default empty range</summary>
		public static readonly Range Zero = new Range{Begin = 0, End = 0};

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly Range Invalid = new Range{Begin = long.MaxValue, End = long.MinValue};

		/// <summary>Construct from a range</summary>
		public Range(long begin, long end) { Begin = begin; End = end; }

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty { get { return End == Begin; } }

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public long Size { get { return End - Begin; } set { End = Begin + value; } }

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public long Mid { get { return (Begin + End) / 2; } set { var count = Size; Begin = value - count/2; End = value + (count+1)/2; } }

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear() { Begin = End = 0; }

		// Casting helpers
		public int Begini { get { return (int)Begin; } }
		public int Endi   { get { return (int)End;   } }
		public int Sizei  { get { return (int)Size;  } }
		public int Midi   { get { return (int)Mid;   } }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate
		{
			get
			{
				for (var i = Begin; i != End; ++i)
					yield return i;
			}
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		[Pure] public bool Contains(long value)
		{
			return Begin <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. inclusive)</summary>
		[Pure] public bool ContainsInclusive(long value)
		{
			return Begin <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		[Pure] public bool Contains(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return Begin <= rng.Begin && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'x'</summary>
		public void Encompass(long value)
		{
			Begin = Math.Min(Begin , value);
			End   = Math.Max(End   , value + 1);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompass(Range rng)
		{
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			Begin = Math.Min(Begin ,rng.Begin);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>Returns a range scaled by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public Range Scale(float scale)
		{
			return new Range(Begin, (long)(Begin + Size*scale)){Mid = Mid};
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompass' except this range isn't modified.</summary>
		public Range Union(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return new Range(Math.Min(Begin, rng.Begin), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public Range Intersect(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
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

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
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
			return "[{0},{1})".Fmt(Begin,End);
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

	/// <summary>A floating point range over [Begin,End)</summary>
	public struct RangeF
	{
		/// <summary>The value of the first element in the range</summary>
		public double Begin;

		/// <summary>The value of one past the last element in the range</summary>
		public double End;

		/// <summary>The default empty range</summary>
		public static readonly RangeF Zero = new RangeF{Begin = 0.0, End = 0.0};

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeF Invalid = new RangeF{Begin = double.MaxValue, End = double.MinValue};

		/// <summary>Construct from a range</summary>
		public RangeF(double begin, double end) { Begin = begin; End = end; }

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty { get { return Equals(Begin,End); } }

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public double Size { get { return End - Begin; } set { End = Begin + value; } }

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public double Mid { get { return (Begin + End) * 0.5; } set { var hsize = Size*0.5; Begin = value - hsize; End = value + hsize; } }

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear() { Begin = End = 0.0; }

		// Casting helpers
		public float Beginf { get { return (float)Begin; } }
		public float Endf   { get { return (float)End;   } }
		public float Sizef  { get { return (float)Size;  } }
		public float Midf   { get { return (float)Mid;   } }

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		[Pure] public bool Contains(double value)
		{
			return Begin <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)</summary>
		[Pure] public bool ContainsInclusive(double value)
		{
			return Begin <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		[Pure] public bool Contains(RangeF rng)
		{
			Debug.Assert(Size >= 0.0, "this range is inside out");
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			return Begin <= rng.Begin && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'value'</summary>
		public void Encompass(double value)
		{
			Begin = Math.Min(Begin , value);
			End   = Math.Max(End   , value);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompass(RangeF rng)
		{
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			Begin = Math.Min(Begin ,rng.Begin);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>Returns a range scaled by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public RangeF Scale(double scale)
		{
			return new RangeF(Begin, Begin + Size*scale){Mid = Mid};
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompass' except this range isn't modified.</summary>
		public RangeF Union(RangeF rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return new RangeF(Math.Min(Begin, rng.Begin), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public RangeF Intersect(RangeF rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			if (rng.End <= Begin) return new RangeF(Begin, Begin);
			if (rng.Begin >= End) return new RangeF(End, End);
			return new RangeF(Math.Max(Begin, rng.Begin), Math.Min(End, rng.End));
		}

		/// <summary>Move the range by an offset</summary>
		public RangeF Shift(double ofs)
		{
			return new RangeF(Begin + ofs, End + ofs);
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static RangeF Clamp(RangeF x, RangeF range)
		{
			return new RangeF(
				Maths.Clamp(x.Begin, range.Begin, range.End),
				Maths.Clamp(x.End, range.Begin, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static RangeF Constrain(RangeF x, RangeF range)
		{
			if (x.Begin < range.Begin) x = x.Shift(range.Begin - x.Begin);
			if (x.End   > range.End  ) x = x.Shift(range.End - x.End);
			if (x.Begin < range.Begin) x.Begin = range.Begin;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return "[{0},{1})".Fmt(Begin,End);
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(RangeF)) return false;
			return Equals((RangeF)obj);
		}
		public bool Equals(RangeF other)
		{
			return Equals(Begin,other.Begin) && Equals(End,other.End);
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
	using common;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestRange
		{
			[Test] public static void Intersect()
			{
				var a = new Range(-4, -1);
				var b = new Range(-1,  3);
				var c = new Range( 0,  5);
				var d = new Range( 7, 12);

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
			[Test] public static void Encompass()
			{
				var r = Range.Invalid;
				r.Encompass(4);
				Assert.AreEqual(4, r.Begin);
				Assert.AreEqual(5, r.End);
				Assert.IsTrue(r.Contains(4));

				r.Encompass(-2);
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 5, r.End);
				Assert.IsTrue(r.Contains(-2));
				Assert.IsTrue(r.Contains(4));

				r.Encompass(new Range(1,7));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);
				Assert.IsTrue(r.Contains(-2));
				Assert.IsFalse(r.Contains(7));

				var r2 = r.Union(new Range(-3,2));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);
				Assert.AreEqual(-3, r2.Begin);
				Assert.AreEqual( 7, r2.End);

				var r3 = r.Intersect(new Range(1,10));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);
				Assert.AreEqual( 1, r3.Begin);
				Assert.AreEqual( 7, r3.End);
			}
			[Test] public static void IntersectF()
			{
				var a = new RangeF(-4.0, -1.0);
				var b = new RangeF(-1.0,  3.0);
				var c = new RangeF( 0.0,  5.0);
				var d = new RangeF( 7.0, 12.0);

				// Intersect
				Assert.AreEqual(a                    , a.Intersect(a));
				Assert.AreEqual(new RangeF(-1.0,-1.0), a.Intersect(b));
				Assert.AreEqual(new RangeF(-1.0,-1.0), a.Intersect(c));
				Assert.AreEqual(new RangeF(-1.0,-1.0), b.Intersect(a));
				Assert.AreEqual(new RangeF( 0.0, 3.0), b.Intersect(c));
				Assert.AreEqual(new RangeF( 3.0, 3.0), b.Intersect(d));
				Assert.AreEqual(new RangeF( 0.0, 0.0), c.Intersect(a));
				Assert.AreEqual(new RangeF( 0.0, 3.0), c.Intersect(b));
				Assert.AreEqual(new RangeF( 5.0, 5.0), c.Intersect(d));
			}
			[Test] public static void EncompassF()
			{
				var r = RangeF.Invalid;
				r.Encompass(4);
				Assert.AreEqual(4, r.Begin);
				Assert.AreEqual(4, r.End);

				r.Encompass(-2);
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 4, r.End);

				r.Encompass(new RangeF(1,7));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);

				var r2 = r.Union(new RangeF(-3,2));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);
				Assert.AreEqual(-3, r2.Begin);
				Assert.AreEqual( 7, r2.End);

				var r3 = r.Intersect(new RangeF(1,10));
				Assert.AreEqual(-2, r.Begin);
				Assert.AreEqual( 7, r.End);
				Assert.AreEqual( 1, r3.Begin);
				Assert.AreEqual( 7, r3.End);
			}
		}
	}
}
#endif
