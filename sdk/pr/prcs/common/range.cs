//***********************************************
// Range
//  Copyright © Rylogic Ltd 2008
//***********************************************

using System;
using System.Collections.Generic;

namespace pr.common
{
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

		/// <summary>Returns true if 'index' is within this range</summary>
		public bool Contains(long index)  { return m_begin <= index && index < m_end; }

		/// <summary>Enumerator for iterating over the range</summary>
		public IEnumerable<long> Enumerate { get { for (long i = m_begin; i != m_end; ++i) yield return i; } }

		/// <summary>String representation of the range</summary>
		public override string ToString() { return "["+m_begin+","+m_end+")"; }

		/// <summary>Grow the bounds of this range to include 'range'</summary>
		public void Encompase(Range range)
		{
			m_begin = Math.Min(m_begin ,range.m_begin);
			m_end   = Math.Max(m_end   ,range.m_end  );
		}

		/// <summary>Move the range by an offset</summary>
		public void Shift(long ofs)
		{
			m_begin += ofs;
			m_end   += ofs;
		}
	}
}