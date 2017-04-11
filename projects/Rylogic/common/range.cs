//***********************************************
// Range
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
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
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct Range :IEnumerable<long>
	{
		/// <summary>The value of the first element in the range</summary>
		public long Beg;

		/// <summary>The value of one past the last element in the range</summary>
		public long End;

		/// <summary>The default empty range</summary>
		public static readonly Range Zero = new Range{Beg = 0, End = 0};

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly Range Invalid = new Range{Beg = long.MaxValue, End = long.MinValue};

		/// <summary>Create a range from a Start and Length</summary>
		public static Range FromStartLength(long start, long length)
		{
			return new Range(start, start + length);
		}

		/// <summary>Create a range from a centre value and + or - a radius</summary>
		public static Range FromCentreRadius(long centre, long radius)
		{
			return new Range(centre - radius, centre + radius);
		}

		/// <summary>Construct from a range</summary>
		public Range(long begin, long end)
		{
			Beg = begin;
			End = end;
		}

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty
		{
			get { return End == Beg; }
		}

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public long Count
		{
			get { return End - Beg; }
			set { End = Beg + value; }
		}
		public long Size
		{
			get { return Count; }
			set { Count = value; }
		}

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public long Mid
		{
			get { return (Beg + End) / 2; }
			set
			{
				var count = Size;
				Beg = value - count/2;
				End = value + (count+1)/2;
			}
		}

		/// <summary>Exact mid point</summary>
		public double Midf
		{
			get { return 0.5 * (Beg + End);  }
		}

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear()
		{
			Beg = End = 0;
		}

		// Casting helpers
		public int Begi   { get { return (int)Beg;   } }
		public int Endi   { get { return (int)End;   } }
		public int Counti { get { return (int)Count; } }
		public int Sizei  { get { return (int)Size;  } }
		public int Midi   { get { return (int)Mid;   } }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate
		{
			get
			{
				for (var i = Beg; i != End; ++i)
					yield return i;
			}
		}
		public IEnumerable<int> Enumeratei
		{
			get
			{
				for (var i = Begi; i != Endi; ++i)
					yield return i;
			}
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		[Pure] public bool Contains(long value)
		{
			return Beg <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. inclusive)</summary>
		[Pure] public bool ContainsInclusive(long value)
		{
			return Beg <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		[Pure] public bool Contains(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return Beg <= rng.Beg && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'x'</summary>
		public void Encompass(long value)
		{
			Beg = Math.Min(Beg , value);
			End   = Math.Max(End   , value + 1);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompass(Range rng)
		{
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			Beg = Math.Min(Beg ,rng.Beg);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>Returns a range scaled by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public Range Scale(float scale)
		{
			return new Range(Beg, (long)(Beg + Size*scale)){Mid = Mid};
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompass' except this range isn't modified.</summary>
		public Range Union(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return new Range(Math.Min(Beg, rng.Beg), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public Range Intersect(Range rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			if (rng.End <= Beg) return new Range(Beg, Beg);
			if (rng.Beg >= End) return new Range(End, End);
			return new Range(Math.Max(Beg, rng.Beg), Math.Min(End, rng.End));
		}

		/// <summary>Move the range by an offset</summary>
		public Range Shift(long ofs)
		{
			return new Range(Beg + ofs, End + ofs);
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static Range Clamp(Range x, Range range)
		{
			return new Range(
				Maths.Clamp(x.Beg, range.Beg, range.End),
				Maths.Clamp(x.End, range.Beg, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static Range Constrain(Range x, Range range)
		{
			if (x.Beg < range.Beg) x = x.Shift(range.Beg - x.Beg);
			if (x.End   > range.End  ) x = x.Shift(range.End - x.End);
			if (x.Beg < range.Beg) x.Beg = range.Beg;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return "[{0},{1})".Fmt(Beg,End);
		}

		#region Equals
		public static bool operator == (Range lhs, Range rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator != (Range lhs, Range rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(Range)) return false;
			return Equals((Range)obj);
		}
		public bool Equals(Range other)
		{
			return other.Beg == Beg && other.End == End;
		}
		public override int GetHashCode()
		{
			unchecked { return (Beg.GetHashCode()*397) ^ End.GetHashCode(); }
		}
		#endregion

		#region IEnumerable

		public IEnumerator<long> GetEnumerator()
		{
			for (var i = Beg; i != End; ++i)
				yield return i;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<long>)this).GetEnumerator();
		}

		#endregion

		#region Parse

		/// <summary>
		/// Convert a string to a range
		/// Examples: '1 2' '[1:2)' '-1,+1' '[-1,+1]' </summary>
		public static Range Parse(string s)
		{
			var v = long_.ParseArray(s, NumberStyles.Integer, new[] { " ","\t",",",";",":","[","]","(",")" }, StringSplitOptions.RemoveEmptyEntries);
			if (v.Length != 2) throw new FormatException("Range.Parse() string argument does not represent a 2 component range");
			return new Range(v[0], v[1]);
		}

		#endregion
	}

	/// <summary>A floating point range over [Begin,End)</summary>
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct RangeF
	{
		/// <summary>The value of the first element in the range</summary>
		public double Beg;

		/// <summary>The value of one past the last element in the range</summary>
		public double End;

		/// <summary>The default empty range</summary>
		public static readonly RangeF Zero = new RangeF{Beg = 0.0, End = 0.0};

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeF Invalid = new RangeF{Beg = double.MaxValue, End = double.MinValue};

		/// <summary>Create a range from a Start and Length</summary>
		public static RangeF FromStartLength(double start, double length)
		{
			return new RangeF(start, start + length);
		}

		/// <summary>Create a range from a centre value and + or - a radius</summary>
		public static RangeF FromCentreRadius(double centre, double radius)
		{
			return new RangeF(centre - radius, centre + radius);
		}

		/// <summary>Construct from a range</summary>
		public RangeF(double begin, double end)
		{
			Beg = begin;
			End = end;
		}

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty
		{
			get { return Equals(Beg,End); }
		}

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public double Size
		{
			get { return End - Beg; }
			set { End = Beg + value; }
		}

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public double Mid
		{
			get { return (Beg + End) * 0.5; }
			set
			{
				var hsize = Size*0.5;
				Beg = value - hsize;
				End = value + hsize;
			}
		}

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear()
		{
			Beg = End = 0.0;
		}

		// Casting helpers
		public float Begf  { get { return (float)Beg;  } }
		public float Endf  { get { return (float)End;  } }
		public float Sizef { get { return (float)Size; } }
		public float Midf  { get { return (float)Mid;  } }

		/// <summary>Enumerator for iterating over the range. 'step' is the step size, 'count' is the number of divisions. Use one or the other, not both. Defaults to step == 1.0</summary>
		public IEnumerable<double> Enumerate(double? step = null, double? count = null)
		{
			var d = 1.0;
			if (step  != null) d = step.Value;
			if (count != null) d = Size / count.Value;
			for (var i = Beg; i <= End; i += d)
				yield return i;
		}
		public IEnumerable<float> Enumeratef(float? step = null, float? count = null)
		{
			return Enumerate(step, count).Select(x => (float)x);
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		[Pure] public bool Contains(double value)
		{
			return Beg <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)</summary>
		[Pure] public bool ContainsInclusive(double value)
		{
			return Beg <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		[Pure] public bool Contains(RangeF rng)
		{
			Debug.Assert(Size >= 0.0, "this range is inside out");
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			return Beg <= rng.Beg && rng.End <= End;
		}

		/// <summary>Grow the bounds of this range to include 'value'</summary>
		public void Encompass(double value)
		{
			Beg = Math.Min(Beg , value);
			End   = Math.Max(End   , value);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompass(RangeF rng)
		{
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			Beg = Math.Min(Beg ,rng.Beg);
			End   = Math.Max(End   ,rng.End  );
		}

		/// <summary>Returns a range inflated (i.e. multiplied) by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public RangeF Inflate(double scale)
		{
			return new RangeF(Beg, Beg + Size*scale){Mid = Mid};
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompass' except this range isn't modified.</summary>
		public RangeF Union(RangeF rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return new RangeF(Math.Min(Beg, rng.Beg), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public RangeF Intersect(RangeF rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			if (rng.End <= Beg) return new RangeF(Beg, Beg);
			if (rng.Beg >= End) return new RangeF(End, End);
			return new RangeF(Math.Max(Beg, rng.Beg), Math.Min(End, rng.End));
		}

		/// <summary>Move the range by an offset</summary>
		public RangeF Shift(double ofs)
		{
			return new RangeF(Beg + ofs, End + ofs);
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static RangeF Clamp(RangeF x, RangeF range)
		{
			return new RangeF(
				Maths.Clamp(x.Beg, range.Beg, range.End),
				Maths.Clamp(x.End, range.Beg, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static RangeF Constrain(RangeF x, RangeF range)
		{
			if (x.Beg < range.Beg) x = x.Shift(range.Beg - x.Beg);
			if (x.End   > range.End  ) x = x.Shift(range.End - x.End);
			if (x.Beg < range.Beg) x.Beg = range.Beg;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return "[{0},{1})".Fmt(Beg,End);
		}

		/// <summary>Allow implicit cast from 'Range'</summary>
		public static implicit operator RangeF(Range r)
		{
			return new RangeF(r.Beg, r.End);
		}

		/// <summary>Allow explicit cast to 'Range'</summary>
		public static explicit operator Range(RangeF r)
		{
			return new Range((long)r.Beg, (long)r.End);
		}

		#region Equals
		public static bool operator == (RangeF lhs, RangeF rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator != (RangeF lhs, RangeF rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(RangeF)) return false;
			return Equals((RangeF)obj);
		}
		public bool Equals(RangeF other)
		{
			return Equals(Beg,other.Beg) && Equals(End,other.End);
		}
		public override int GetHashCode()
		{
			unchecked { return (Beg.GetHashCode()*397) ^ End.GetHashCode(); }
		}
		#endregion

		#region Parse

		/// <summary>
		/// Convert a string to a range
		/// Examples: '1 2' '[1:2)' '-1,+1' '[-1,+1]' </summary>
		public static RangeF Parse(string s)
		{
			var v = double_.ParseArray(s, NumberStyles.Float, new[] { " ","\t",",",";",":","[","]","(",")" }, StringSplitOptions.RemoveEmptyEntries);
			if (v.Length != 2) throw new FormatException("RangeF.Parse() string argument does not represent a 2 component range");
			return new RangeF(v[0], v[1]);
		}

		#endregion
	}
}

#if PR_UNITTESTS

namespace pr.unittests
{
	using common;

	[TestFixture] public class TestRange
	{
		[Test] public void Intersect()
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
		[Test] public void Encompass()
		{
			var r = Range.Invalid;
			r.Encompass(4);
			Assert.AreEqual(4L, r.Beg);
			Assert.AreEqual(5L, r.End);
			Assert.True(r.Contains(4));

			r.Encompass(-2);
			Assert.AreEqual(-2L, r.Beg);
			Assert.AreEqual( 5L, r.End);
			Assert.True(r.Contains(-2));
			Assert.True(r.Contains(4));

			r.Encompass(new Range(1,7));
			Assert.AreEqual(-2L, r.Beg);
			Assert.AreEqual( 7L, r.End);
			Assert.True(r.Contains(-2));
			Assert.False(r.Contains(7));

			var r2 = r.Union(new Range(-3,2));
			Assert.AreEqual(-2L, r.Beg);
			Assert.AreEqual( 7L, r.End);
			Assert.AreEqual(-3L, r2.Beg);
			Assert.AreEqual( 7L, r2.End);

			var r3 = r.Intersect(new Range(1,10));
			Assert.AreEqual(-2L, r.Beg);
			Assert.AreEqual( 7L, r.End);
			Assert.AreEqual( 1L, r3.Beg);
			Assert.AreEqual( 7L, r3.End);
		}
		[Test] public void IntersectF()
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
		[Test] public void EncompassF()
		{
			var r = RangeF.Invalid;
			r.Encompass(4);
			Assert.AreEqual(4.0, r.Beg);
			Assert.AreEqual(4.0, r.End);

			r.Encompass(-2);
			Assert.AreEqual(-2.0, r.Beg);
			Assert.AreEqual( 4.0, r.End);

			r.Encompass(new RangeF(1,7));
			Assert.AreEqual(-2.0, r.Beg);
			Assert.AreEqual( 7.0, r.End);

			var r2 = r.Union(new RangeF(-3,2));
			Assert.AreEqual(-2.0, r.Beg);
			Assert.AreEqual( 7.0, r.End);
			Assert.AreEqual(-3.0, r2.Beg);
			Assert.AreEqual( 7.0, r2.End);

			var r3 = r.Intersect(new RangeF(1,10));
			Assert.AreEqual(-2.0, r.Beg);
			Assert.AreEqual( 7.0, r.End);
			Assert.AreEqual( 1.0, r3.Beg);
			Assert.AreEqual( 7.0, r3.End);
		}
	}
}
#endif
