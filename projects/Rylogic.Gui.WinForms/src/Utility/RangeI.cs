using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Rylogic.Extn;
using Rylogic.Maths;

#if true // Set this to false to see if using the Rylogic.Common.RangeI version works yet...
namespace Rylogic.Gui.WinForms.Workaround
{
	/// <summary>A range over [Begin,End)</summary>
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct RangeI :IEnumerable<long>
	{
		// Notes:
		//  - This is a copy of Rylogic.Common.RangeI to work around an issue with the WinForms
		//    designer not being about to load this type. If I ever figure out why, it can be removed
		//    and usages should fallback to the Rylogic.Common.RangeI version

		/// <summary>The value of the first element in the range</summary>
		public long Beg;

		/// <summary>The value of one past the last element in the range</summary>
		public long End;

		/// <summary>The default empty range</summary>
		public static readonly RangeI Zero = new RangeI { Beg = 0, End = 0 };

		/// <summary>The default full range</summary>
		public static readonly RangeI Max = new RangeI { Beg = long.MinValue, End = long.MaxValue };

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeI Invalid = new RangeI { Beg = long.MaxValue, End = long.MinValue };

		/// <summary>Create a range from a Start and Length</summary>
		public static RangeI FromStartLength(long start, long length)
		{
			return new RangeI(start, start + length);
		}

		/// <summary>Create a range from a centre value and + or - a radius</summary>
		public static RangeI FromCentreRadius(long centre, long radius)
		{
			return new RangeI(centre - radius, centre + radius);
		}

		/// <summary>Return the range of values selected by 'selector'</summary>
		public static RangeI From<T>(IEnumerable<T> items, Func<T, long> selector)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Encompass(selector(item));

			return range;
		}
		public static RangeI From(IEnumerable<int> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Encompass(item);

			return range;
		}
		public static RangeI From(IEnumerable<long> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Encompass(item);

			return range;
		}

		/// <summary>Construct from a range</summary>
		public RangeI(long beg, long end)
		{
			Beg = beg;
			End = end;
		}

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty => End == Beg;

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public long Count
		{
			get => End - Beg;
			set => End = Beg + value;
		}
		public long Size
		{
			get => Count;
			set => Count = value;
		}

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public long Mid
		{
			get => (Beg + End) / 2;
			set
			{
				var count = Size;
				Beg = value - count / 2;
				End = value + (count + 1) / 2;
			}
		}

		/// <summary>Exact mid point</summary>
		public double Midf => 0.5 * (Beg + End);

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear()
		{
			Beg = End = 0;
		}

		// Casting helpers
		public int Begi => (int)Beg;
		public int Endi => (int)End;
		public int Counti => (int)Count;
		public int Sizei => (int)Size;
		public int Midi => (int)Mid;

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
		public bool Contains(long value)
		{
			return Beg <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. inclusive)</summary>
		public bool ContainsInclusive(long value)
		{
			return Beg <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		public bool Contains(RangeI rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return Beg <= rng.Beg && rng.End <= End;
		}

		/// <summary>Return -1 if 'value' is less than Beg, 0 if 'value' in [Beg,End), +1 if >= End</summary>
		public int CompareTo(long value)
		{
			// -1 if this range is less than 'value'
			// +1 if this range is greater than 'value'
			// Otherwise 0.
			return End <= value ? -1 : Beg > value ? +1 : 0;
		}

		/// <summary>Grow the bounds of this range to include 'x'</summary>
		public void Encompass(long value)
		{
			Beg = Math.Min(Beg, value);
			End = Math.Max(End, value + 1);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompass(RangeI rng)
		{
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			Beg = Math.Min(Beg, rng.Beg);
			End = Math.Max(End, rng.End);
		}

		/// <summary>Returns a range scaled by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public RangeI Scale(float scale)
		{
			return new RangeI(Beg, (long)(Beg + Size * scale)) { Mid = Mid };
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Encompass' except this range isn't modified.</summary>
		public RangeI Union(RangeI rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			return new RangeI(Math.Min(Beg, rng.Beg), Math.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e) of *this* range.
		/// Note: this means A.Intersect(B) != B.Intersect(A)</summary>
		public RangeI Intersect(RangeI rng)
		{
			Debug.Assert(Size >= 0, "this range is inside out");
			Debug.Assert(rng.Size >= 0, "'rng' is inside out");
			if (rng.End <= Beg) return new RangeI(Beg, Beg);
			if (rng.Beg >= End) return new RangeI(End, End);
			return new RangeI(Math.Max(Beg, rng.Beg), Math.Min(End, rng.End));
		}

		/// <summary>Move the range by an offset</summary>
		public RangeI Shift(long ofs)
		{
			return new RangeI(Beg + ofs, End + ofs);
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static RangeI Clamp(RangeI x, RangeI range)
		{
			return new RangeI(
				Math_.Clamp(x.Beg, range.Beg, range.End),
				Math_.Clamp(x.End, range.Beg, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static RangeI Constrain(RangeI x, RangeI range)
		{
			if (x.Beg < range.Beg) x = x.Shift(range.Beg - x.Beg);
			if (x.End > range.End) x = x.Shift(range.End - x.End);
			if (x.Beg < range.Beg) x.Beg = range.Beg;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString()
		{
			return $"[{Beg},{End})";
		}

#region Equals
		public static bool operator ==(RangeI lhs, RangeI rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator !=(RangeI lhs, RangeI rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object obj)
		{
			return obj is RangeI && Equals((RangeI)obj);
		}
		public bool Equals(RangeI other)
		{
			return other.Beg == Beg && other.End == End;
		}
		public override int GetHashCode()
		{
			return new { Beg, End }.GetHashCode();
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

		/// <summary>Convert a string to a range. Examples: '1 2' '[1:2)' '-1,+1' '[-1,+1]' </summary>
		public static RangeI Parse(string s)
		{
			var v = long_.ParseArray(s, NumberStyles.Integer, new[] { " ", "\t", ",", ";", ":", "[", "]", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
			if (v.Length != 2) throw new FormatException("Range.Parse() string argument does not represent a 2 component range");
			return new RangeI(v[0], v[1]);
		}

#endregion

		public static implicit operator Common.RangeI(RangeI r) { return new Common.RangeI(r.Beg, r.End); }
		public static implicit operator RangeI(Common.RangeI r) { return new RangeI(r.Beg, r.End); }
	}
}
#endif
