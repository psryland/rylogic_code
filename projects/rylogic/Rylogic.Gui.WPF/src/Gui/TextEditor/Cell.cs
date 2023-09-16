using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary></summary>
	[DebuggerDisplay("{ch,nq}")]
	public struct Cell :IEquatable<Cell>
	{
		public Cell(char c)
		{
			ch = c;
			sty = 0;
		}
		public Cell(char c, ushort s)
		{
			ch = c;
			sty = s;
		}

		/// <summary>The character in the cell</summary>
		public char ch;

		/// <summary>The style bitmask</summary>
		public ushort sty;

		/// <summary></summary>
		public static implicit operator char(Cell c) => c.ch;

		#region Equals
		public static bool operator ==(Cell left, Cell right)
		{
			return left.Equals(right);
		}
		public static bool operator !=(Cell left, Cell right)
		{
			return !(left == right);
		}
		public override bool Equals(object? obj)
		{
			return obj is Cell cell && Equals(cell);
		}
		public bool Equals(Cell rhs)
		{
			return ch == rhs.ch && sty == rhs.sty;
		}
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 130108511;
				hash = hash * -1521134295 + ch.GetHashCode();
				hash = hash * -1521134295 + sty.GetHashCode();
				return hash;
			}
		}
		public override string ToString()
		{
			return new string(ch, 1);
		}
		#endregion
	}

	/// <summary>Typedef of a string of cells</summary>
	[DebuggerDisplay("{m_text,nq}")]
	public class CellString :IList<Cell>, IReadOnlyList<Cell>
	{
		// Notes:
		//  - Designed to be a drop in replacement for StringBuilder/string
		//  - Not stored as an array of 'Cell' because interleaved char/style isn't very efficient
		//  - If m_style.Length != m_text.Length, the missing styles are assumed to be 0
		//  - Explicit conversion to string is cheap, use that for string operations.

		private readonly StringBuilder m_text;
		private readonly List<ushort> m_style;

		public CellString()
			:this(16)
		{}
		public CellString(int capacity)
		{
			m_text = new StringBuilder(capacity);
			m_style = new List<ushort>();
		}
		public CellString(string str, ushort style = 0)
			:this(str.Length)
		{
			m_text.Append(str);
			if (style != 0)
				m_style.Resize(str.Length, style);
		}
		public CellString(CellString str)
			: this(str.Length)
		{
			m_text.Append(str.m_text);
			m_style.AddRange(str.m_style);
		}

		/// <summary>Element access</summary>
		public Cell this[int index]
		{
			get
			{
				if (index < m_style.Count)
					return new Cell(m_text[index], m_style[index]);
				else
					return new Cell(m_text[index], 0);
			}
			set
			{
				m_text[index] = value.ch;
				if (index < m_style.Count)
				{
					m_style[index] = value.sty;
				}
				else if (value.sty != 0)
				{
					m_style.Resize(index + 1);
					m_style[index] = value.sty;
				}
			}
		}

		/// <summary>Reserve memory for the string</summary>
		public int Capacity
		{
			get => m_text.Capacity;
			set => m_text.Capacity = value;
		}

		/// <summary>Get/Set the string length</summary>
		public int Length
		{
			get => m_text.Length;
			set
			{
				m_text.Length = value;
				if (value > m_style.Count)
					m_style.Resize(value);
			}
		}

		/// <summary>Get the text in this string</summary>
		public string Text => m_text.ToString();

		/// <summary>Get the text in this string</summary>
		public IReadOnlyList<ushort> Styles => new LazyStyleList(m_style, Length);

		/// <summary>Get ranges within this string that have the same style</summary>
		public IEnumerable<RangeI> Spans
		{
			get
			{
				if (m_style.Count == 0)
				{
					yield return new RangeI(0, Length);
				}
				else
				{
					for (int s = 0, e = 0; s != m_style.Count; s = e)
					{
						// Find the span of characters with the same style
						for (e = s + 1; e != m_style.Count && m_style[e] == m_style[s]; ++e) { }
						yield return new RangeI(s, e);
					}
				}
			}
		}

		/// <summary>Return the character range (exclusive) that contains 'i' and has the same style</summary>
		public RangeI SpanAt(int i)
		{
			if (i < 0 || i >= Length)
				throw new ArgumentOutOfRangeException(nameof(i));
			if (i >= m_style.Count)
				return new RangeI(m_style.Count, Length);

			var span = new RangeI(i, i);
			for (; span.Beg != 0 && m_style[span.Begi] == m_style[span.Begi - 1]; --span.Beg) { }
			for (; ++span.End != m_style.Count && m_style[span.Endi - 1] == m_style[span.Endi];) { }
			return span;
		}

		/// <summary>Add a cell to the string</summary>
		public CellString Append(Cell c)
		{
			m_text.Append(c.ch);
			if (c.sty != 0)
			{
				m_style.Resize(m_text.Length);
				m_style[m_text.Length-1] = c.sty;
			}
			return this;
		}
		public CellString Append(CellString str)
		{
			m_text.Append(str.m_text);
			if (str.m_style.Count != 0)
			{
				m_style.Resize(m_text.Length - str.Length);
				m_style.AddRange(str.m_style);
			}
			return this;
		}
		public CellString Append(CellString str, int start, int length)
		{
			m_text.Append(str.m_text, start, length);
			if (start < str.m_style.Count)
			{
				// Trim the default styles from the end of the range 
				var len = Math.Min(length, Math.Max(0, str.m_style.Count - start));
				for (; len != 0 && str.m_style[start + len - 1] == 0; --len) { }

				m_style.Resize(m_text.Length - length);
				m_style.AddRange(str.m_style.EnumRange(start, len));
			}
			return this;
		}
		public CellString Append(IReadOnlyList<Cell> str)
		{
			return Append(str, 0, str.Count);
		}
		public CellString Append(IReadOnlyList<Cell> str, int start, int length)
		{
			if (str is CellString cstr)
				return Append(cstr, start, length);

			// Fall back to per-character copying
			m_text.EnsureCapacity(m_text.Length + str.Count);
			for (; length-- != 0; ++start)
			{
				m_text.Append(str[start].ch);
				if (str[start].sty != 0)
				{
					m_style.Resize(m_text.Length);
					m_style[start] = str[start].sty;
				}
			}

			return this;
		}
		public CellString Append(string str, ushort style = 0)
		{
			return Append(str, 0, str.Length, style);
		}
		public CellString Append(string str, int start, int length, ushort style = 0)
		{
			m_text.Append(str, start, length);
			if (style != 0)
			{
				m_style.Resize(m_text.Length - length);
				m_style.Resize(m_text.Length, style);
			}
			return this;
		}

		/// <inheritdoc/>
		public void Insert(int index, Cell item)
		{
			m_text.Insert(index, item.ch);
			if (index < m_style.Count)
			{
				m_style.Insert(index, item.sty);
			}
			else if (item.sty != 0)
			{
				m_style.Resize(index + 1);
				m_style[index] = item.sty;
			}
		}
		public void Insert(int index, CellString str)
		{
			m_text.Insert(index, str.m_text);
			if (index < m_style.Count)
			{
				m_style.InsertRange(index, str.m_style.EnumRange<ushort>(0, str.m_text.Length, pad: 0));
			}
			else if (str.m_style.Count != 0)
			{
				m_style.Resize(index);
				m_style.AddRange(str.m_style);
			}
		}
		public void Insert(int index, CellString str, int start, int length)
		{
			m_text.Insert(index, str.m_text.ToString(start, length));
			if (index < m_style.Count)
			{
				m_style.InsertRange(index, str.m_style.EnumRange<ushort>(start, length, pad: 0));
			}
			else if (str.m_style.Count != 0)
			{
				// Trim the default styles from the end of the range 
				var len = Math.Min(length, Math.Max(0, str.m_style.Count - start));
				for (; len != 0 && str.m_style[start + len - 1] == 0; --len) { }

				m_style.Resize(index);
				m_style.AddRange(str.m_style.EnumRange(start, len));
			}
		}

		/// <inheritdoc/>
		public bool Remove(Cell item)
		{
			var idx = IndexOf(item);
			if (idx == -1) return false;
			RemoveAt(idx);
			return true;
		}

		/// <inheritdoc/>
		public void RemoveAt(int index)
		{
			m_text.Remove(index, 1);
			if (index < m_style.Count)
				m_style.RemoveAt(index);
		}

		/// <inheritdoc/>
		public void Remove(int index, int count)
		{
			m_text.Remove(index, count);
			if (index < m_style.Count)
				m_style.RemoveRange(index, Math.Min(count, m_style.Count - index));
		}

		/// <inheritdoc/>
		public int IndexOf(Cell item)
		{
			var i = 0;
			for (; ; )
			{
				i = m_text.IndexOf(item.ch, i, m_text.Length - i);
				if (i != -1) break;
				if (i < m_style.Count && m_style[i] == item.sty) break;
				if (i >= m_style.Count && item.sty == 0) break;
			}
			return i;
		}

		/// <inheritdoc/>
		int IReadOnlyCollection<Cell>.Count => Length;

		/// <inheritdoc/>
		int ICollection<Cell>.Count => Length;

		/// <inheritdoc/>
		void ICollection<Cell>.Add(Cell item)
		{
			Append(item);
		}

		/// <inheritdoc/>
		void ICollection<Cell>.Clear()
		{
			Length = 0;
		}

		/// <inheritdoc/>
		bool ICollection<Cell>.Contains(Cell item)
		{
			return ((IList<Cell>)this).IndexOf(item) != -1;
		}

		/// <inheritdoc/>
		void ICollection<Cell>.CopyTo(Cell[] array, int array_index)
		{
			for (int i = 0, idx = array_index; i != m_text.Length; ++i, ++idx)
				array[idx].ch = m_text[i];
			for (int i = 0, idx = array_index; i != m_style.Count; ++i, ++idx)
				array[idx].sty = m_style[i];
		}

		/// <inheritdoc/>
		public IEnumerator<Cell> GetEnumerator()
		{
			var i = 0;
			for (; i != m_style.Count; ++i)
				yield return new Cell(m_text[i], m_style[i]);
			for (; i != m_text.Length; ++i)
				yield return new Cell(m_text[i], 0);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc/>
		bool ICollection<Cell>.IsReadOnly => false;

		/// <summary>Explicit conversion to string</summary>
		public static explicit operator string(CellString s)
		{
			return s.m_text.ToString();
		}
		public static explicit operator CellString(string s)
		{
			return new CellString(s);
		}

		/// <inheritdoc/>
		public override string ToString() => m_text.ToString();

		/// <summary>Return the string as a char array</summary>
		public char[] ToCharArray() => m_text.ToCharArray();

		/// <summary>Empty cell string</summary>
		public static readonly CellString Empty = new();

		#region Equals
		public static bool operator ==(CellString lhs, CellString rhs) => lhs.Equals(rhs);
		public static bool operator !=(CellString lhs, CellString rhs) => !lhs.Equals(rhs);
		public bool Equals(CellString rhs)
		{
			return
				m_text.Equals(rhs.m_text) &&
				m_style.Equals(rhs.m_style);
		}
		public override bool Equals(object? obj)
		{
			return obj is CellString cstr && Equals(cstr);
		}
		public override int GetHashCode()
		{
			return new { m_text, m_style }.GetHashCode();
		}
		#endregion

		/// <summary>Proxy for a list of length 'Length'</summary>
		private class LazyStyleList :IReadOnlyList<ushort>
		{
			private readonly IList<ushort> m_data;
			private readonly int m_count;

			public LazyStyleList(IList<ushort> data, int count)
			{
				m_data = data;
				m_count = count;
			}
			ushort IReadOnlyList<ushort>.this[int index]
			{
				get
				{
					if (index < m_data.Count) return m_data[index];
					if (index < m_count) return 0;
					throw new ArgumentOutOfRangeException(nameof(index));
				}
			}
			int IReadOnlyCollection<ushort>.Count
			{
				get => m_count;
			}
			IEnumerator<ushort> IEnumerable<ushort>.GetEnumerator()
			{
				var i = 0;
				for (; i != m_data.Count; ++i)
					yield return m_data[i];
				for (; i != m_count; ++i)
					yield return 0;
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<ushort>)this).GetEnumerator();
			}
		}

		//#region Compare
		//
		///// <summary>Compare CellStrings. Ignores styles, just compares strings</summary>
		//public static int Compare(CellString lhs, string rhs) => Compare(lhs, 0, rhs, 0, Math.Max(lhs.Length, rhs.Length), false);
		//public static int Compare(CellString lhs, int indexL, string rhs, int indexR, int length, bool ignore_case)
		//{
		//	return string.Compare(lhs.m_text.ToString(), indexL, rhs, indexR, length, ignore_case);
		//}
		//
		//#endregion
	}
}