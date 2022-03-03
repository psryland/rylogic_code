using System;
using System.Collections;

namespace Rylogic.Container
{
	/// <summary>Simple fixed size ring buffer containing elements of type 'T'</summary>
	public class RingBuffer<T> :IEnumerable
	{
		private int m_begin, m_end;
		public RingBuffer(int capacity)
		{
			Buffer = new T[capacity + 1];
			m_begin = 0;
			m_end = 0;
		}

		public int inc(int i)
		{
			return (i + 1) % Buffer.Length;
		}
		public int dec(int i)
		{
			return (i - 1 + Buffer.Length) % Buffer.Length;
		}
		public int inc(int i, int count)
		{
			return (i + (count % Buffer.Length)) % Buffer.Length;
		}
		public int dec(int i, int count)
		{
			return (i - (count % Buffer.Length) + Buffer.Length) % Buffer.Length;
		}
		public int size(int b, int e)
		{
			return ((e - b) + Buffer.Length) % Buffer.Length;
		}

		/// <summary>Direct access to the buffer. Typically used after Canonicalise, to copy to another location</summary>
		public T[] Buffer { get; private set; }

		/// <summary>Returns true if the ring buffer contains no data</summary>
		public bool Empty => m_begin == m_end;

		/// <summary>Returns true if the ring buffer cannot be added to</summary>
		public bool Full => m_begin == inc(m_end);

		/// <summary>Returns the number of elements in the ring buffer</summary>
		public int Count => size(m_begin, m_end);

		/// <summary>Returns the maximum number of elements that can be added to the ring buffer</summary>
		public int Capacity
		{
			get => Buffer.Length - 1;
			set
			{
				if (Capacity == value) return;
				var size = Math.Min(Count, value);
				Resize(size);
				Buffer = CopyTo(new T[value + 1]);
				m_begin = 0;
				m_end = size;
			}
		}

		/// <summary>Returns the first element</summary>
		public T Front
		{
			get
			{
				if (Empty) throw new Exception("Ring buffer empty");
				return Buffer[m_begin];
			}
		}

		/// <summary>Returns the last element</summary>
		public T Back
		{
			get
			{
				if (Empty) throw new Exception("Ring buffer empty");
				return Buffer[dec(m_end)];
			}
		}

		/// <summary>Adds an element to the end of the ring buffer</summary>
		public void PushBack(T elem)
		{
			if (Full) throw new Exception("Ring buffer full");
			Buffer[m_end] = elem;
			m_end = inc(m_end);
		}

		/// <summary>Adds an element to the start of the ring buffer</summary>
		public void PushFront(T elem)
		{
			if (Full) throw new Exception("Ring buffer full");
			m_begin = dec(m_begin);
			Buffer[m_begin] = elem;
		}

		/// <summary>Removes and returns the last element from the ring buffer</summary>
		public T PopBack()
		{
			if (Empty) throw new Exception("Ring buffer empty");
			m_end = dec(m_end);
			return Buffer[m_end];
		}

		/// <summary>Removes and returns the front element from the ring buffer</summary>
		public T PopFront()
		{
			if (Empty) throw new Exception("Ring buffer empty");
			int begin = m_begin;
			m_begin = inc(m_begin);
			return Buffer[begin];
		}

		/// <summary>Adds an element to the end of the ring buffer, overwriting the first element if the ring buffer is full</summary>
		public void PushBackOverwrite(T elem)
		{
			if (Full) m_begin = inc(m_begin);
			PushBack(elem);
		}

		/// <summary>Adds an element to the start of the ring buffer, overwriting the last element if the ring buffer is full</summary>
		public void PushFrontOverwrite(T elem)
		{
			if (Full) m_end = dec(m_end);
			PushFront(elem);
		}

		/// <summary>Random access to elements in the ring buffer where i == 0 is the front element and i == Length - 1 is the last element</summary>
		public T this[int i]
		{
			get
			{
				if (i >= Count) throw new IndexOutOfRangeException("Ring buffer get accessor. Index '{i}' is out of range [0,{Count})");
				return Buffer[inc(m_begin, i)];
			}
			set
			{
				if (i >= Count) throw new IndexOutOfRangeException($"Ring buffer set accessor. Index '{i}' is out of range [0,{Count})");
				Buffer[inc(m_begin, i)] = value;
			}
		}

		/// <summary>Set the size of the ring buffer within [0,Capacity)</summary>
		public void Resize(int size)
		{
			if (size < 0 || size > Capacity)
				throw new ArgumentOutOfRangeException($"Ring buffer size ({size}) must be within the capacity ({Capacity})");

			m_end = inc(m_begin, size);
		}

		/// <summary>
		/// Move the data in the ring buffer so that the first elements is
		/// at the start of the internal buffer. This guarantees that the data is contiguous</summary>
		public void Canonicalise()
		{
			if (m_end >= m_begin)
			{
				Array.Copy(Buffer, m_begin, Buffer, 0, Count);
			}
			else
			{
				T[] tmp = new T[m_end];
				Array.Copy(Buffer, tmp, m_end);
				Array.Copy(Buffer, m_begin, Buffer, 0, Buffer.Length - m_begin);
				Array.Copy(tmp, Buffer, Buffer.Length - m_begin);
			}

			m_end = Count;
			m_begin = 0;
		}

		/// <summary>Copy to a contiguous buffer</summary>
		public T[] CopyTo(T[] arr)
		{
			if (arr.Length < Count)
				throw new ArgumentException($"CopyTo. Provided array isn't large enough. Provided {arr.Length}. Required {Count}");

			if (m_end >= m_begin)
			{
				Array.Copy(Buffer, m_begin, arr, 0, Count);
			}
			else
			{
				var chunk = Buffer.Length - m_begin;
				Array.Copy(Buffer, m_begin, arr, 0, chunk);
				Array.Copy(Buffer, 0, arr, chunk, m_end);
			}

			return arr;
		}

		// Enumerable interface
		public IEnumerator GetEnumerator()
		{
			for (int i = m_begin; i != m_end; i = inc(i))
				yield return Buffer[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
