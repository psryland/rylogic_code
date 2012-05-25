using System;
using System.Collections;
using System.Diagnostics;

namespace pr.container
{
	/// <summary>Simple fixed size ring buffer containing elements of type 'T'</summary>
	public class RingBuffer<T> :IEnumerable
	{
		private readonly T[] m_buffer;
		private int m_begin, m_end;
		
		public RingBuffer(int capacity)
		{
			m_buffer = new T[capacity + 1];
			m_begin = 0;
			m_end = 0;
		}

		public int inc(int i)                  { return (i + 1                  ) % m_buffer.Length; }
		public int dec(int i)                  { return (i - 1 + m_buffer.Length) % m_buffer.Length; }
		public int inc(int i, int count)       { return (i + (count%m_buffer.Length)                  ) % m_buffer.Length; }
		public int dec(int i, int count)       { return (i - (count%m_buffer.Length) + m_buffer.Length) % m_buffer.Length; }
		public int size(int b, int e)          { return ((e - b) + m_buffer.Length) % m_buffer.Length; }

		/// <summary>Returns true if the ring buffer contains no data</summary>
		public bool Empty                      { get { return m_begin == m_end; } }
		
		/// <summary>Returns true if the ring buffer cannot be added to</summary>
		public bool Full                       { get { return m_begin == inc(m_end); } }
		
		/// <summary>Returns the number of elements in the ring buffer</summary>
		public int  Length                     { get { return size(m_begin, m_end); } }
		
		/// <summary>Returns the maximum number of elements that can be added to the ring buffer</summary>
		public int  Capacity                   { get { return m_buffer.Length - 1; } }

		/// <summary>Returns the first element</summary>
		public T    Front                      { get { Debug.Assert(!Empty); return m_buffer[m_begin]; } }
		
		/// <summary>Returns the last element</summary>
		public T    Back                       { get { Debug.Assert(!Empty); return m_buffer[dec(m_end)]; } }
		
		/// <summary>Adds an element to the end of the ring buffer</summary>
		public void PushBack(T elem)           { Debug.Assert(!Full); m_buffer[m_end] = elem; m_end = inc(m_end); }

		/// <summary>Adds an element to the start of the ring buffer</summary>
		public void PushFront(T elem)          { Debug.Assert(!Full); m_begin = dec(m_begin); m_buffer[m_begin] = elem; }
		
		/// <summary>Removes and returns the last element from the ring buffer</summary>
		public T    PopBack()                  { Debug.Assert(!Empty); m_end = dec(m_end); return m_buffer[m_end]; }
		
		/// <summary>Removes and returns the front element from the ring buffer</summary>
		public T    PopFront()                 { Debug.Assert(!Empty); int begin = m_begin; m_begin = inc(m_begin); return m_buffer[begin]; }
		
		/// <summary>Adds an element to the end of the ring buffer, overwriting the first element if the ring buffer is full</summary>
		public void PushBackOverwrite(T elem)  { if (Full) {m_begin = inc(m_begin);} PushBack(elem); }

		/// <summary>Adds an element to the start of the ring buffer, overwriting the last element if the ring buffer is full</summary>
		public void PushFrontOverwrite(T elem) { if (Full) {m_end   = dec(m_end);}   PushFront(elem); }

		/// <summary>Random access to elements in the ring buffer where i == 0 is the front element and i == Length - 1 is the last element</summary>
		public T    this[int i]                { get { Debug.Assert(i < Length); return m_buffer[inc(m_begin,i)]; } set { Debug.Assert(i < Length); m_buffer[inc(m_begin,i)] = value; }}

		/// <summary>Set the size of the ring buffer within [0,Capacity)</summary>
		public void Resize(int size)
		{
			Debug.Assert(size >= 0);
			m_end = inc(m_begin, size < Capacity ? size : Capacity);
		}

		/// <summary>Move the data in the ring buffer so that the first elements is
		/// at the start of the internal buffer. This guarantees that the data is contiguous</summary>
		public void Canonicalise()
		{
			if (m_end >= m_begin) { Array.Copy(m_buffer, m_begin, m_buffer, 0, Length); }
			else
			{
				T[] tmp = new T[m_end];
				Array.Copy(m_buffer, tmp, m_end);
				Array.Copy(m_buffer, m_begin, m_buffer, 0, m_buffer.Length - m_begin);
				Array.Copy(tmp, m_buffer, m_buffer.Length - m_begin);
			}
			
			m_end = Length;
			m_begin = 0;
		}

		// Enumerable interface
		public IEnumerator GetEnumerator()
		{
			for (int i = m_begin; i != m_end; i = inc(i))
				yield return m_buffer[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
