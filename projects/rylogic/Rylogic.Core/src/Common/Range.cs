﻿//***********************************************
// Range
//  Copyright (c) Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Common
{
	// Notes:
	//  - It is valid for a range to be inside out (i.e. as the initial state when
	//    finding a bounding range). However some methods assume that this range has
	//    a count >= 0. These functions should contain appropriate asserts.

	/// <summary>A range over [Begin,End)</summary>
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct RangeI :IEnumerable<long>
	{
		/// <summary>The value of the first element in the range</summary>
		public long Beg;

		/// <summary>The value of one past the last element in the range</summary>
		public long End;

		/// <summary>The default empty range</summary>
		public static readonly RangeI Zero = new() { Beg = 0, End = 0 };

		/// <summary>The default full range</summary>
		public static readonly RangeI Max = new() { Beg = long.MinValue, End = long.MaxValue };

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeI Invalid = new() { Beg = long.MaxValue, End = long.MinValue };

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
				range.Grow(selector(item));

			return range;
		}
		public static RangeI From(IEnumerable<int> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(item);

			return range;
		}
		public static RangeI From(IEnumerable<long> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(item);

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

		/// <summary>True if the range contains a positive interval</summary>
		public bool Positive => End > Beg;

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
		public void Grow(long value)
		{
			Beg = Math.Min(Beg, value);
			End = Math.Max(End, value + 1);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Grow(RangeI rng)
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
		/// (basically the same as 'Grow' except this range isn't modified.</summary>
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
		public override bool Equals(object? obj)
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
	}

	/// <summary>A floating point range over [Begin,End)</summary>
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct RangeF
	{
		// Note:
		//  - Using properties for 'Beg/End' so that WPF binding works

		/// <summary>The value of the first element in the range</summary>
		public double Beg
		{
			get => beg;
			set => beg = value;
		}
		public double beg;

		/// <summary>The value of one past the last element in the range</summary>
		public double End
		{
			get => end;
			set => end = value;
		}
		public double end;

		/// <summary>The default empty range</summary>
		public static readonly RangeF Zero = new() { Beg = 0.0, End = 0.0 };

		/// <summary>The unit range [0,1]</summary>
		public static readonly RangeF Unit = new() { Beg = 0.0, End = 1.0 };

		/// <summary>The default full range</summary>
		public static readonly RangeF Max = new() { Beg = double.MinValue, End = double.MaxValue };

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeF Invalid = new() { Beg = double.MaxValue, End = double.MinValue };

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

		/// <summary>Return the range of values selected by 'selector'</summary>
		public static RangeF From<T>(IEnumerable<T> items, Func<T, double> selector)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(selector(item));

			return range;
		}
		public static RangeF From(IEnumerable<float> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(item);

			return range;
		}
		public static RangeF From(IEnumerable<double> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(item);

			return range;
		}

		/// <summary>Construct from a range</summary>
		public RangeF(double beg, double end)
		{
			this.beg = beg;
			this.end = end;
		}

		/// <summary>True if Beg == End</summary>
		public bool Empty => Equals(Beg, End);

		/// <summary>True if the range contains a positive interval</summary>
		public bool Positive => End > Beg;

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public double Size
		{
			get => End - Beg;
			set => End = Beg + value;
		}

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public double Mid
		{
			get => (Beg + End) * 0.5;
			set
			{
				var hsize = Size * 0.5;
				Beg = value - hsize;
				End = value + hsize;
			}
		}

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear()
		{
			Beg = End = 0.0;
		}

		/// <summary>Casting helpers</summary>
		public float Begf
		{
			get => (float)Beg;
			set => Beg = value;
		}
		public float Endf
		{
			get => (float)End;
			set => End = value;
		}
		public float Sizef
		{
			get => (float)Size;
			set => Size = value;
		}
		public float Midf
		{
			get => (float)Mid;
			set => Mid = value;
		}

		/// <summary>Enumerator for iterating over the range. 'step' is the step size, 'count' is the number of divisions. Use one or the other, not both. Defaults to step == 1.0</summary>
		public IEnumerable<double> Enumerate(double? step = null, double? count = null)
		{
			var d = 1.0;
			if (step != null) d = step.Value;
			if (count != null) d = Size / count.Value;
			for (var i = Beg; i <= End; i += d)
				yield return i;
		}
		public IEnumerable<float> Enumeratef(float? step = null, float? count = null)
		{
			return Enumerate(step, count).Select(x => (float)x);
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		public bool Contains(double value)
		{
			return Beg <= value && value < End;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)</summary>
		public bool ContainsInclusive(double value)
		{
			return Beg <= value && value <= End;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		public bool Contains(RangeF rng)
		{
			Debug.Assert(Size >= 0.0, "this range is inside out");
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			return Beg <= rng.Beg && rng.End <= End;
		}

		/// <summary>Compare this range to the given value</summary>
		public int CompareTo(double value)
		{
			// -1 if this range is less than 'value'
			// +1 if this range is greater than 'value'
			// Otherwise 0.
			return End <= value ? -1 : Beg > value ? +1 : 0;
		}

		/// <summary>Grow the bounds of this range to include 'value'</summary>
		public void Grow(double value)
		{
			Beg = Math.Min(Beg, value);
			End = Math.Max(End, value);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Grow(RangeF rng)
		{
			if (rng == Invalid) return;
			Debug.Assert(rng.Size >= 0.0, "'rng' is inside out");
			Beg = Math.Min(Beg, rng.Beg);
			End = Math.Max(End, rng.End);
		}

		/// <summary>Returns a range inflated (i.e. multiplied) by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		public RangeF Inflate(double scale)
		{
			return new RangeF(Beg, Beg + Size * scale) { Mid = Mid };
		}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Grow' except this range isn't modified.</summary>
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
				Math_.Clamp(x.Beg, range.Beg, range.End),
				Math_.Clamp(x.End, range.Beg, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static RangeF Constrain(RangeF x, RangeF range)
		{
			if (x.Beg < range.Beg) x = x.Shift(range.Beg - x.Beg);
			if (x.End > range.End) x = x.Shift(range.End - x.End);
			if (x.Beg < range.Beg) x.Beg = range.Beg;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString() => $"[{Beg},{End})";

		/// <summary>Allow implicit cast from 'Range'</summary>
		public static implicit operator RangeF(RangeI r)
		{
			return r != RangeI.Invalid ? new RangeF(r.Beg, r.End) : RangeF.Invalid;
		}

		/// <summary>Allow explicit cast to 'Range'</summary>
		public static explicit operator RangeI(RangeF r)
		{
			return r != RangeF.Invalid ? new RangeI((long)r.Beg, (long)r.End) : RangeI.Invalid;
		}

		#region Equals
		public static bool operator ==(RangeF lhs, RangeF rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator !=(RangeF lhs, RangeF rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (obj.GetType() != typeof(RangeF)) return false;
			return Equals((RangeF)obj);
		}
		public bool Equals(RangeF other)
		{
			return Equals(Beg, other.Beg) && Equals(End, other.End);
		}
		public override int GetHashCode()
		{
			return new { Beg, End }.GetHashCode();
		}
		#endregion

		#region Parse

		/// <summary>
		/// Convert a string to a range
		/// Examples: '1 2' '[1:2)' '-1,+1' '[-1,+1]' </summary>
		public static RangeF Parse(string s)
		{
			var v = double_.ParseArray(s, NumberStyles.Float, new[] { " ", "\t", ",", ";", ":", "[", "]", "(", ")" }, StringSplitOptions.RemoveEmptyEntries);
			if (v.Length != 2) throw new FormatException("RangeF.Parse() string argument does not represent a 2 component range");
			return new RangeF(v[0], v[1]);
		}

		#endregion
	}

	/// <summary>A floating point range over [Begin,End) on type 'T'</summary>
	[DebuggerDisplay("{Beg} {End} ({Size})")]
	public struct RangeF<T> where T : struct, IComparable<T>
	{
		/// <summary>The value of the first element in the range</summary>
		public T Beg;

		/// <summary>The value of one past the last element in the range</summary>
		public T End;

		/// <summary>The default empty range</summary>
		public static readonly RangeF<T> Zero = new() { Beg = default, End = default };

		/// <summary>The default full range</summary>
		public static readonly RangeF<T> Max = new() { Beg = Operators<T>.MinValue, End = Operators<T>.MaxValue };

		/// <summary>An invalid range. Used as an initialiser when finding a bounding range</summary>
		public static readonly RangeF<T> Invalid = new() { Beg = Operators<T>.MaxValue, End = Operators<T>.MinValue };
		public static RangeF<T> Reset { get { return Invalid; } }

		/// <summary>Create a range from a Start and Length</summary>
		public static RangeF<T> FromStartLength(T start, T length)
		{
			return new RangeF<T>(start, Operators<T>.Add(start, length));
		}

		/// <summary>Create a range from a centre value and + or - a radius</summary>
		public static RangeF<T> FromCentreRadius(T centre, T radius)
		{
			return new RangeF<T>(Operators<T>.Sub(centre, radius), Operators<T>.Add(centre, radius));
		}

		/// <summary>Return the range of values selected by 'selector'</summary>
		public static RangeF<T> From<U>(IEnumerable<U> items, Func<U, T> selector)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(selector(item));

			return range;
		}
		public static RangeF<T> From(IEnumerable<T> items)
		{
			var range = Invalid;
			foreach (var item in items)
				range.Grow(item);

			return range;
		}

		/// <summary>Construct from a range</summary>
		public RangeF(T begin, T end)
		{
			Beg = begin;
			End = end;
		}

		/// <summary>True if the range spans zero elements</summary>
		public bool Empty => Equals(Beg, End);

		/// <summary>Get/Set the number of elements in the range. Setting changes 'End' only</summary>
		public T Size
		{
			get => Operators<T>.Sub(End, Beg);
			set => End = Operators<T>.Add(Beg, value);
		}

		/// <summary>Get/Set the middle of the range. Setting the middle point does not change 'Size', i.e. 'Begin' and 'End' are both potentially moved</summary>
		public T Mid
		{
			get => Operators<T, double>.Mul(Operators<T>.Add(Beg, End), 0.5);
			set
			{
				var hsize = Operators<T, double>.Mul(Size, 0.5);
				Beg = Operators<T>.Sub(value, hsize);
				End = Operators<T>.Add(value, hsize);
			}
		}

		/// <summary>Empty the range and reset to [0,0)</summary>
		public void Clear()
		{
			Beg = End = default;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End) (i.e. end exclusive)</summary>
		public bool Contains(T value)
		{
			return Beg.CompareTo(value) <= 0 && value.CompareTo(End) < 0;
		}

		/// <summary>Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)</summary>
		public bool ContainsInclusive(T value)
		{
			return Beg.CompareTo(value) <= 0 && value.CompareTo(End) <= 0;
		}

		/// <summary>Returns true if 'rng' is entirely within this range</summary>
		public bool Contains(RangeF<T> rng)
		{
			Debug.Assert(Size.CompareTo(default(T)) >= 0, "this range is inside out");
			Debug.Assert(rng.Size.CompareTo(default(T)) >= 0, "'rng' is inside out");
			return Beg.CompareTo(rng.Beg) <= 0 && rng.End.CompareTo(End) <= 0;
		}

		/// <summary>Return -1 if 'value' is less than Beg, 0 if 'value' in [Beg,End), +1 if >= End</summary>
		public int CompareTo(T value)
		{
			// -1 if this range is less than 'value'
			// +1 if this range is greater than 'value'
			// Otherwise 0.
			return
				End.CompareTo(value) <= 0 ? -1 :
				Beg.CompareTo(value) > 0 ? +1 : 0;
		}

		/// <summary>Grow the bounds of this range to include 'value'</summary>
		public void Grow(T value)
		{
			Beg = Math_.Min(Beg, value);
			End = Math_.Max(End, value);
		}

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Grow(RangeF<T> rng)
		{
			Debug.Assert(rng.Size.CompareTo(default(T)) >= 0, "'rng' is inside out");
			Beg = Math_.Min(Beg, rng.Beg);
			End = Math_.Max(End, rng.End);
		}

		/// <summary>Returns a range inflated (i.e. multiplied) by 'scale'. Begin and End are changed, the mid point of the range is unchanged</summary>
		//	public RangeF<T> Inflate(double scale)
		//	{
		//		return new RangeF(Beg, Beg + Size*scale){Mid = Mid};
		//	}

		/// <summary>
		/// Returns a range that is the union of this range with 'rng'
		/// (basically the same as 'Grow' except this range isn't modified.</summary>
		public RangeF<T> Union(RangeF<T> rng)
		{
			Debug.Assert(Size.CompareTo(default(T)) >= 0, "this range is inside out");
			Debug.Assert(rng.Size.CompareTo(default(T)) >= 0, "'rng' is inside out");
			return new RangeF<T>(Math_.Min(Beg, rng.Beg), Math_.Max(End, rng.End));
		}

		/// <summary>
		/// Returns the intersection of this range with 'rng'.
		/// If there is no intersection, returns [b,b) or [e,e). Note: this means
		/// A.Intersect(B) != B.Intersect(A)</summary>
		public RangeF<T> Intersect(RangeF<T> rng)
		{
			Debug.Assert(Size.CompareTo(default(T)) >= 0, "this range is inside out");
			Debug.Assert(rng.Size.CompareTo(default(T)) >= 0, "'rng' is inside out");
			if (rng.End.CompareTo(Beg) <= 0) return new RangeF<T>(Beg, Beg);
			if (rng.Beg.CompareTo(End) >= 0) return new RangeF<T>(End, End);
			return new RangeF<T>(Math_.Max(Beg, rng.Beg), Math_.Min(End, rng.End));
		}

		/// <summary>Move the range by an offset</summary>
		public RangeF<T> Shift(T ofs)
		{
			return new RangeF<T>(Operators<T>.Add(Beg, ofs), Operators<T>.Add(End, ofs));
		}

		/// <summary>Returns 'x' clamped by 'range'</summary>
		public static RangeF<T> Clamp(RangeF<T> x, RangeF<T> range)
		{
			return new RangeF<T>(
				Math_.Clamp(x.Beg, range.Beg, range.End),
				Math_.Clamp(x.End, range.Beg, range.End));
		}

		/// <summary>Returns 'x' constrained by 'range'. i.e 'x' will be fitted within 'range' and only resized if x.Size > range.Size</summary>
		public static RangeF<T> Constrain(RangeF<T> x, RangeF<T> range)
		{
			if (x.Beg.CompareTo(range.Beg) < 0) x = x.Shift(Operators<T>.Sub(range.Beg, x.Beg));
			if (x.End.CompareTo(range.End) > 0) x = x.Shift(Operators<T>.Sub(range.End, x.End));
			if (x.Beg.CompareTo(range.Beg) < 0) x.Beg = range.Beg;
			return x;
		}

		/// <summary>String representation of the range</summary>
		public override string ToString() => $"[{Beg},{End})";

		//	/// <summary>Allow implicit cast from 'Range'</summary>
		//	public static implicit operator RangeF<T>(Range<T> r)
		//	{
		//		return new RangeF<T>(r.Beg, r.End);
		//	}

		//	/// <summary>Allow explicit cast to 'Range<U>'</summary>
		//	public static explicit operator Range<U>(RangeF<T> r)
		//	{
		//		return new Range<T>((U)r.Beg, (U)r.End);
		//	}

		#region Equals
		public static bool operator ==(RangeF<T> lhs, RangeF<T> rhs)
		{
			return lhs.Equals(rhs);
		}
		public static bool operator !=(RangeF<T> lhs, RangeF<T> rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is RangeF<T> r && Equals(r);
		}
		public bool Equals(RangeF<T> rhs)
		{
			return Equals(Beg, rhs.Beg) && Equals(End, rhs.End);
		}
		public override int GetHashCode()
		{
			return new { Beg, End }.GetHashCode();
		}
		#endregion

		#region Parse

		/// <summary>
		/// Convert a string to a range
		/// Examples: '1 2' '[1:2)' '-1,+1' '[-1,+1]' </summary>
		//	public static RangeF<T> Parse(string s)
		//	{
		//		var v = double_.ParseArray(s, NumberStyles.Float, new[] { " ","\t",",",";",":","[","]","(",")" }, StringSplitOptions.RemoveEmptyEntries);
		//		if (v.Length != 2) throw new FormatException("RangeF.Parse() string argument does not represent a 2 component range");
		//		return new RangeF<T>(v[0], v[1]);
		//	}

		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture]
	public class TestRange
	{
		[Test]
		public void TestFrom()
		{
			{
				var r = RangeI.From(new[] { 1, 4, -2, 5, 7, -3 });
				Assert.Equal(r, new RangeI(-3, 8));
			}
			{
				var r = RangeF.From(new[] { 1f, 4f, -2f, 5f, 7f, -3f });
				Assert.Equal(r, new RangeF(-3f, 7f));
			}
		}

		[Test]
		public void Casting()
		{
			Assert.True((RangeF)new RangeI(10, 20) == new RangeF(10, 20));
			Assert.True((RangeI)new RangeF(-10, 20) == new RangeI(-10, 20));
			Assert.True((RangeF)RangeI.Invalid == RangeF.Invalid);
			Assert.True((RangeI)RangeF.Invalid == RangeI.Invalid);
		}

		[Test]
		public void Intersect()
		{
			var a = new RangeI(-4, -1);
			var b = new RangeI(-1,  3);
			var c = new RangeI( 0,  5);
			var d = new RangeI( 7, 12);

			// Intersect
			Assert.Equal(a               , a.Intersect(a));
			Assert.Equal(new RangeI(-1,-1), a.Intersect(b));
			Assert.Equal(new RangeI(-1,-1), a.Intersect(c));
			Assert.Equal(new RangeI(-1,-1), b.Intersect(a));
			Assert.Equal(new RangeI( 0, 3), b.Intersect(c));
			Assert.Equal(new RangeI( 3, 3), b.Intersect(d));
			Assert.Equal(new RangeI( 0, 0), c.Intersect(a));
			Assert.Equal(new RangeI( 0, 3), c.Intersect(b));
			Assert.Equal(new RangeI( 5, 5), c.Intersect(d));
		}

		[Test]
		public void Bounds()
		{
			var r = RangeI.Invalid;
			r.Grow(4);
			Assert.Equal(4L, r.Beg);
			Assert.Equal(5L, r.End);
			Assert.True(r.Contains(4));

			r.Grow(-2);
			Assert.Equal(-2L, r.Beg);
			Assert.Equal( 5L, r.End);
			Assert.True(r.Contains(-2));
			Assert.True(r.Contains(4));

			r.Grow(new RangeI(1,7));
			Assert.Equal(-2L, r.Beg);
			Assert.Equal( 7L, r.End);
			Assert.True(r.Contains(-2));
			Assert.False(r.Contains(7));

			var r2 = r.Union(new RangeI(-3,2));
			Assert.Equal(-2L, r.Beg);
			Assert.Equal( 7L, r.End);
			Assert.Equal(-3L, r2.Beg);
			Assert.Equal( 7L, r2.End);

			var r3 = r.Intersect(new RangeI(1,10));
			Assert.Equal(-2L, r.Beg);
			Assert.Equal( 7L, r.End);
			Assert.Equal( 1L, r3.Beg);
			Assert.Equal( 7L, r3.End);
		}

		[Test]
		public void IntersectF()
		{
			var a = new RangeF(-4.0, -1.0);
			var b = new RangeF(-1.0,  3.0);
			var c = new RangeF( 0.0,  5.0);
			var d = new RangeF( 7.0, 12.0);

			// Intersect
			Assert.Equal(a                    , a.Intersect(a));
			Assert.Equal(new RangeF(-1.0,-1.0), a.Intersect(b));
			Assert.Equal(new RangeF(-1.0,-1.0), a.Intersect(c));
			Assert.Equal(new RangeF(-1.0,-1.0), b.Intersect(a));
			Assert.Equal(new RangeF( 0.0, 3.0), b.Intersect(c));
			Assert.Equal(new RangeF( 3.0, 3.0), b.Intersect(d));
			Assert.Equal(new RangeF( 0.0, 0.0), c.Intersect(a));
			Assert.Equal(new RangeF( 0.0, 3.0), c.Intersect(b));
			Assert.Equal(new RangeF( 5.0, 5.0), c.Intersect(d));
		}

		[Test]
		public void BoundsF()
		{
			var r = RangeF.Invalid;
			r.Grow(4);
			Assert.Equal(4.0, r.Beg);
			Assert.Equal(4.0, r.End);

			r.Grow(-2);
			Assert.Equal(-2.0, r.Beg);
			Assert.Equal( 4.0, r.End);

			r.Grow(new RangeF(1,7));
			Assert.Equal(-2.0, r.Beg);
			Assert.Equal( 7.0, r.End);

			var r2 = r.Union(new RangeF(-3,2));
			Assert.Equal(-2.0, r.Beg);
			Assert.Equal( 7.0, r.End);
			Assert.Equal(-3.0, r2.Beg);
			Assert.Equal( 7.0, r2.End);

			var r3 = r.Intersect(new RangeF(1,10));
			Assert.Equal(-2.0, r.Beg);
			Assert.Equal( 7.0, r.End);
			Assert.Equal( 1.0, r3.Beg);
			Assert.Equal( 7.0, r3.End);
		}

		[Test]
		public void IntersectFGen()
		{
			var a = new RangeF<decimal>(-4.0m, -1.0m);
			var b = new RangeF<decimal>(-1.0m,  3.0m);
			var c = new RangeF<decimal>( 0.0m,  5.0m);
			var d = new RangeF<decimal>( 7.0m, 12.0m);

			// Intersect
			Assert.Equal(a                    , a.Intersect(a));
			Assert.Equal(new RangeF<decimal>(-1.0m,-1.0m), a.Intersect(b));
			Assert.Equal(new RangeF<decimal>(-1.0m,-1.0m), a.Intersect(c));
			Assert.Equal(new RangeF<decimal>(-1.0m,-1.0m), b.Intersect(a));
			Assert.Equal(new RangeF<decimal>( 0.0m, 3.0m), b.Intersect(c));
			Assert.Equal(new RangeF<decimal>( 3.0m, 3.0m), b.Intersect(d));
			Assert.Equal(new RangeF<decimal>( 0.0m, 0.0m), c.Intersect(a));
			Assert.Equal(new RangeF<decimal>( 0.0m, 3.0m), c.Intersect(b));
			Assert.Equal(new RangeF<decimal>( 5.0m, 5.0m), c.Intersect(d));
		}

		[Test]
		public void BoundsFGen()
		{
			var r = RangeF<decimal>.Invalid;
			r.Grow(4);
			Assert.Equal(4.0m, r.Beg);
			Assert.Equal(4.0m, r.End);

			r.Grow(-2);
			Assert.Equal(-2.0m, r.Beg);
			Assert.Equal( 4.0m, r.End);

			r.Grow(new RangeF<decimal>(1m,7m));
			Assert.Equal(-2.0m, r.Beg);
			Assert.Equal( 7.0m, r.End);

			var r2 = r.Union(new RangeF<decimal>(-3,2));
			Assert.Equal(-2.0m, r.Beg);
			Assert.Equal( 7.0m, r.End);
			Assert.Equal(-3.0m, r2.Beg);
			Assert.Equal( 7.0m, r2.End);

			var r3 = r.Intersect(new RangeF<decimal>(1,10));
			Assert.Equal(-2.0m, r.Beg);
			Assert.Equal( 7.0m, r.End);
			Assert.Equal( 1.0m, r3.Beg);
			Assert.Equal( 7.0m, r3.End);
		}
	}
}
#endif
