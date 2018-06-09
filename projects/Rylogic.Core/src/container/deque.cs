/* Copyright (c) 2006 Leslie Sanford
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy 
 * of this software and associated documentation files (the "Software"), to 
 * deal in the Software without restriction, including without limitation the 
 * rights to use, copy, modify, merge, publish, distribute, sub-license, and/or 
 * sell copies of the Software, and to permit persons to whom the Software is 
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software. 
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN 
 * THE SOFTWARE.
 */
/*
 * Leslie Sanford
 * Email: jabberdabber@hotmail.com
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Rylogic.Container
{
	/// <summary>
	/// Represents a simple double-ended-queue collection of objects.
	/// </summary>
	[Serializable()]
	public class Deque<T> :ICollection, IEnumerable<T>, ICloneable
	{
		/// <summary>Represents a node in the deque.</summary>
		[Serializable()]
		private class Node
		{
			public Node(T value) { Value = value; }
			public T Value       { get; private set;  }
			public Node Previous { get; set; }
			public Node Next     { get; set; }
		}

		// The node at the front of the deque.
		private Node m_front;

		// The node at the back of the deque.
		private Node m_back;

		// The number of elements in the deque.
		private int m_count;

		// The version of the deque.
		private long m_version;

		/// <summary>Initializes a new instance of the Deque class.</summary>
		public Deque()
		{
			m_front   = null;
			m_back    = null;
			m_count   = 0;
			m_version = 0;
		}

		/// <summary>
		/// Initializes a new instance of the Deque class that contains 
		/// elements copied from the specified collection.</summary>
		/// <param name="collection">The collection whose elements are copied to the new Deque.</param>
		public Deque(IEnumerable<T> collection)
		{
			if (collection == null) throw new ArgumentNullException("collection");
			foreach (T item in collection)
				PushBack(item);
		}

		/// <summary>Removes all objects from the Deque.</summary>
		public virtual void Clear()
		{
			m_count = 0;
			m_front = m_back = null;
			m_version++;

			AssertValid();
		}

		/// <summary>Determines whether or not an element is in the Deque.</summary>
		/// <param name="obj">The Object to locate in the Deque.</param>
		/// <returns><b>true</b> if <i>obj</i> if found in the Deque; otherwise, <b>false</b>.</returns>
		public virtual bool Contains(T obj)
		{
			foreach (T o in this)
				if (EqualityComparer<T>.Default.Equals(o, obj))
					return true;

			return false;
		}

		/// <summary>Inserts an object at the front of the Deque.</summary>
		/// <param name="item">The object to push onto the deque;</param>
		public virtual void PushFront(T item)
		{
			// The new node to add to the front of the deque.
			Node new_node = new Node(item);

			// Link the new node to the front node. The current front node at 
			// the front of the deque is now the second node in the deque.
			new_node.Next = m_front;

			// If the deque isn't empty.
			if (Count > 0)
			{
				// Link the current front to the new node.
				m_front.Previous = new_node;
			}

			// Make the new node the front of the deque.
			m_front = new_node;

			// Keep track of the number of elements in the deque.
			m_count++;

			// If this is the first element in the deque.
			if (Count == 1)
			{
				// The front and back nodes are the same.
				m_back = m_front;
			}

			m_version++;
			AssertValid();
		}

		/// <summary>Inserts an object at the back of the Deque.</summary>
		/// <param name="item">The object to push onto the deque;</param>
		public virtual void PushBack(T item)
		{
			// The new node to add to the back of the deque.
			Node new_node = new Node(item);

			// Link the new node to the back node. The current back node at 
			// the back of the deque is now the second to the last node in the deque.
			new_node.Previous = m_back;

			// If the deque is not empty.
			if (Count > 0)
			{
				// Link the current back node to the new node.
				m_back.Next = new_node;
			}

			// Make the new node the back of the deque.
			m_back = new_node;

			// Keep track of the number of elements in the deque.
			m_count++;

			// If this is the first element in the deque.
			if (Count == 1)
			{
				// The front and back nodes are the same.
				m_front = m_back;
			}

			m_version++;
			AssertValid();
		}

		/// <summary>Removes and returns the object at the front of the Deque.</summary>
		/// <returns>The object at the front of the Deque.</returns>
		/// <exception cref="InvalidOperationException">The Deque is empty.</exception>
		public virtual T PopFront()
		{
			if (Count == 0)
				throw new InvalidOperationException("Deque is empty.");

			// Get the object at the front of the deque.
			T item = m_front.Value;

			// Move the front back one node.
			m_front = m_front.Next;

			// Keep track of the number of nodes in the deque.
			m_count--;

			// If the deque is not empty.
			if (Count > 0)
			{
				// Tie off the previous link in the front node.
				m_front.Previous = null;
			}
			// Else the deque is empty.
			else
			{
				// Indicate that there is no back node.
				m_back = null;
			}

			m_version++;
			AssertValid();
			return item;
		}

		/// <summary>Removes and returns the object at the back of the Deque.</summary>
		/// <returns>The object at the back of the Deque.</returns>
		/// <exception cref="InvalidOperationException">The Deque is empty.</exception>
		public virtual T PopBack()
		{
			if (Count == 0)
				throw new InvalidOperationException("Deque is empty.");

			// Get the object at the back of the deque.
			var item = m_back.Value;

			// Move back node forward one node.
			m_back = m_back.Previous;

			// Keep track of the number of nodes in the deque.
			m_count--;

			// If the deque is not empty.
			if (Count > 0)
			{
				// Tie off the next link in the back node.
				m_back.Next = null;
			}
			// Else the deque is empty.
			else
			{
				// Indicate that there is no front node.
				m_front = null;
			}

			m_version++;
			AssertValid();
			return item;
		}

		/// <summary>Returns the object at the front of the Deque without removing it.</summary>
		/// <returns>The object at the front of the Deque.</returns>
		/// <exception cref="InvalidOperationException">The Deque is empty.</exception>
		public virtual T PeekFront()
		{
			if (Count == 0) throw new InvalidOperationException("Deque is empty.");
			return m_front.Value;
		}

		/// <summary>Returns the object at the back of the Deque without removing it.</summary>
		/// <returns>The object at the back of the Deque.</returns>
		/// <exception cref="InvalidOperationException">The Deque is empty.</exception>
		public virtual T PeekBack()
		{
			if (Count == 0) throw new InvalidOperationException("Deque is empty.");
			return m_back.Value;
		}

		/// <summary>Copies the Deque to a new array.</summary>
		/// <returns>A new array containing copies of the elements of the Deque.</returns>
		public virtual T[] ToArray()
		{
			int index = 0;
			var array = new T[Count];
			foreach (T item in this)
			{
				array[index] = item;
				index++;
			}
			return array;
		}

		/// <summary>Returns a synchronized (thread-safe) wrapper for the Deque.</summary>
		/// <param name="deque">The Deque to synchronize.</param>
		/// <returns>A synchronized wrapper around the Deque.</returns>
		public static Deque<T> Synchronized(Deque<T> deque)
		{
			if (deque == null) throw new ArgumentNullException("deque");
			return new SynchronizedDeque(deque);
		}

		/// <summary>Self consistency check</summary>
		[Conditional("DEBUG")] private void AssertValid()
		{
			int n = 0;
			var current = m_front;
			while (current != null)
			{
				n++;
				current = current.Next;
			}

			Debug.Assert(n == Count);

			if (Count > 0)
			{
				Debug.Assert(m_front != null && m_back != null, "Front/Back Null Test - Count > 0");

				Node f = m_front;
				Node b = m_back;

				while (f.Next != null && b.Previous != null)
				{
					f = f.Next;
					b = b.Previous;
				}

				Debug.Assert(f.Next == null && b.Previous == null, "Front/Back Termination Test");
				Debug.Assert(f == m_back && b == m_front, "Front/Back Equality Test");
			}
			else
			{
				Debug.Assert(m_front == null && m_back == null, "Front/Back Null Test - Count == 0");
			}
		}

		#region ICloneable
		/// <summary>Creates a shallow copy of the Deque.</summary>
		/// <returns>A shallow copy of the Deque.</returns>
		public virtual object Clone()
		{
			var clone = new Deque<T>(this);
			clone.m_version = this.m_version;
			return clone;
		}
		#endregion

		#region ICollection
		/// <summary>Gets a value indicating whether access to the Deque is synchronized (thread-safe).</summary>
		public virtual bool IsSynchronized
		{
			get { return false; }
		}

		/// <summary>Gets the number of elements contained in the Deque.</summary>
		public virtual int Count
		{
			get { return m_count; }
		}

		/// <summary>
		/// Copies the Deque elements to an existing one-dimensional Array, 
		/// starting at the specified array index.</summary>
		/// <param name="array">
		/// The one-dimensional Array that is the destination of the elements 
		/// copied from Deque. The Array must have zero-based indexing.</param>
		/// <param name="index">The zero-based index in array at which copying begins.</param>
		public virtual void CopyTo(Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "Index is less than zero.");
			if (array.Rank > 1)
				throw new ArgumentException("Array is multidimensional.");
			if (index >= array.Length)
				throw new ArgumentException("Index is equal to or greater than the length of array.");
			if (Count > array.Length - index)
				throw new ArgumentException(
					"The number of elements in the source Deque is greater " +
					"than the available space from index to the end of the " +
					"destination array.");

			var i = index;
			foreach (object obj in this)
			{
				array.SetValue(obj, i);
				i++;
			}
		}

		/// <summary>Gets an object that can be used to synchronize access to the Deque.</summary>
		public virtual object SyncRoot
		{
			get { return this; }
		}
		#endregion

		#region IEnumerable
		/// <summary>Returns an enumerator that can iterate through the Deque.</summary>
		/// <returns>An IEnumerator for the Deque.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return new Enumerator(this);
		}
		#endregion

		#region IEnumerable<T>
		public virtual IEnumerator<T> GetEnumerator()
		{
			return new Enumerator(this);
		}
		#endregion

		#region SynchronizedDeque
		/// <summary>Implements a synchronization wrapper around a deque.</summary>
		[Serializable()] private class SynchronizedDeque :Deque<T>, IEnumerable
		{
			/// <summary>The wrapped deque.</summary>
			private Deque<T> m_deque;

			/// <summary>The object to lock on.</summary>
			private object m_root;

			public SynchronizedDeque(Deque<T> deque)
			{
				if (deque == null) throw new ArgumentNullException("deque");
				m_deque = deque;
				m_root = deque.SyncRoot;
			}
			public override int Count
			{
				get { lock (m_root) return m_deque.Count; }
			}
			public override bool IsSynchronized
			{
				get { return true; }
			}
			public override void Clear()
			{
				lock (m_root)
					m_deque.Clear();
			}
			public override bool Contains(T item)
			{
				lock (m_root)
					return m_deque.Contains(item);
			}
			public override void PushFront(T item)
			{
				lock (m_root)
					m_deque.PushFront(item);
			}
			public override void PushBack(T item)
			{
				lock (m_root)
					m_deque.PushBack(item);
			}
			public override T PopFront()
			{
				lock (m_root)
					return m_deque.PopFront();
			}
			public override T PopBack()
			{
				lock (m_root)
					return m_deque.PopBack();
			}
			public override T PeekFront()
			{
				lock (m_root)
					return m_deque.PeekFront();
			}
			public override T PeekBack()
			{
				lock (m_root)
					return m_deque.PeekBack();
			}
			public override T[] ToArray()
			{
				lock (m_root)
					return m_deque.ToArray();
			}
			public override object Clone()
			{
				lock (m_root)
					return m_deque.Clone();
			}
			public override void CopyTo(Array array, int index)
			{
				lock (m_root)
					m_deque.CopyTo(array, index);
			}
			public override IEnumerator<T> GetEnumerator()
			{
				lock (m_root)
					return m_deque.GetEnumerator();
			}
			IEnumerator IEnumerable.GetEnumerator()
			{
				lock (m_root)
					return ((IEnumerable)m_deque).GetEnumerator();
			}
		}
		#endregion

		#region Enumerator
		[Serializable()] private class Enumerator :IEnumerator<T>
		{
			private Deque<T> m_owner;
			private Node     m_current_node;
			private T        m_current;
			private bool     m_move_result;
			private long     m_version;
			private bool     m_disposed; // A value indicating whether the enumerator has been disposed.

			public Enumerator(Deque<T> owner)
			{
				m_owner        = owner;
				m_current_node = owner.m_front;
				m_current      = default(T);
				m_move_result  = false;
				m_version      = owner.m_version;
				m_disposed     = false;
			}
			public void Reset()
			{
				if (m_disposed)
					throw new ObjectDisposedException(this.GetType().Name);
				if (m_version != m_owner.m_version)
					throw new InvalidOperationException("The Deque was modified after the enumerator was created.");

				m_current_node = m_owner.m_front;
				m_move_result  = false;
			}
			public object Current
			{
				get
				{
					if (m_disposed)
						throw new ObjectDisposedException(this.GetType().Name);
					if (!m_move_result)
						throw new InvalidOperationException("The enumerator is positioned before the first element of the Deque or after the last element.");

					return m_current;
				}
			}
			public bool MoveNext()
			{
				if (m_disposed)
					throw new ObjectDisposedException(this.GetType().Name);
				if (m_version != m_owner.m_version)
					throw new InvalidOperationException("The Deque was modified after the enumerator was created.");

				if (m_current_node != null)
				{
					m_current      = m_current_node.Value;
					m_current_node = m_current_node.Next;
					m_move_result  = true;
				}
				else
				{
					m_move_result = false;
				}
				return m_move_result;
			}
			T IEnumerator<T>.Current
			{
				get
				{
					if (m_disposed)
						throw new ObjectDisposedException(this.GetType().Name);
					if (!m_move_result)
						throw new InvalidOperationException("The enumerator is positioned before the first element of the Deque or after the last element.");
					return m_current;
				}
			}
			public void Dispose()
			{
				m_disposed = true;
			}
		}
		#endregion
	}
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Container;

	[TestFixture] public class TestDeque
	{
		private const int ElementCount = 100;

		[Test] public void DequeTest()
		{
			var deque = new Deque<int>();

			deque.Clear();
			Assert.Equal(deque.Count, 0);

			PopulateDequePushFront(deque);
			PopulateDequePushBack(deque);
			TestPopFront(deque);
			TestPopBack(deque);
			TestContains(deque);
			TestCopyTo(deque);
			TestToArray(deque);
			TestClone(deque);
			TestEnumerator(deque);
		}
		private static void PopulateDequePushFront(Deque<int> deque)
		{
			deque.Clear();
			for (int i = 0; i < ElementCount; i++)
				deque.PushFront(i);

			Assert.Equal(deque.Count, ElementCount);

			int j = ElementCount - 1;
			foreach (int i in deque)
			{
				Assert.Equal(i, j);
				j--;
			}
		}
		private static void PopulateDequePushBack(Deque<int> deque)
		{
			deque.Clear();
			for (int i = 0; i < ElementCount; i++)
				deque.PushBack(i);

			Assert.Equal(deque.Count, ElementCount);

			int j = 0;
			foreach (int i in deque)
			{
				Assert.Equal(i, j);
				j++;
			}
		}
		private static void TestPopFront(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			int j;
			for (int i = 0; i < ElementCount; i++)
			{
				j = (int)deque.PopFront();
				Assert.Equal(j, i);
			}
			Assert.Equal(deque.Count, 0);
		}
		private static void TestPopBack(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			int j;
			for (int i = 0; i < ElementCount; i++)
			{
				j = (int)deque.PopBack();
				Assert.Equal(j, ElementCount - 1 - i);
			}
			Assert.Equal(deque.Count, 0);
		}
		private static void TestContains(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			for (int i = 0; i < deque.Count; i++)
				Assert.True(deque.Contains(i));

			Assert.False(deque.Contains(ElementCount));
		}
		private static void TestCopyTo(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			var array = new int[deque.Count];

			deque.CopyTo(array, 0);
			foreach (int i in deque)
				Assert.Equal(array[i], i);

			array = new int[deque.Count * 2];
			deque.CopyTo(array, deque.Count);

			foreach (int i in deque)
				Assert.Equal(array[i + deque.Count], i);

			array = new int[deque.Count];
			Assert.Throws(typeof(Exception), () => deque.CopyTo(null, deque.Count));
			Assert.Throws(typeof(Exception), () => deque.CopyTo(array, -1));
			Assert.Throws(typeof(Exception), () => deque.CopyTo(array, deque.Count / 2));
			Assert.Throws(typeof(Exception), () => deque.CopyTo(array, deque.Count));
			Assert.Throws(typeof(Exception), () => deque.CopyTo(new int[10, 10], deque.Count));
		}
		private static void TestToArray(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			int i = 0;
			var array = deque.ToArray();
			foreach (int item in deque)
			{
				Assert.True(item.Equals(array[i]));
				i++;
			}
		}
		private static void TestClone(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			var deque2 = (Deque<int>)deque.Clone();
			Assert.Equal(deque.Count, deque2.Count);

			var d2 = deque2.GetEnumerator();
			d2.MoveNext();
			foreach (int item in deque)
			{
				Assert.True(item.Equals(d2.Current));
				d2.MoveNext();
			}
		}
		private static void TestEnumerator(Deque<int> deque)
		{
			deque.Clear();
			PopulateDequePushBack(deque);

			var e = deque.GetEnumerator();
			Assert.Throws(typeof(Exception), () => { int item = e.Current; });

			foreach (int item in deque) Assert.True(e.MoveNext());
			Assert.False(e.MoveNext());
			Assert.Throws(typeof(Exception), () => { int item = e.Current; });

			e.Reset();
			foreach (int item in deque) Assert.True(e.MoveNext());
			Assert.False(e.MoveNext());
			Assert.Throws(typeof(Exception), () => { int item = e.Current; });
			Assert.Throws(typeof(Exception), () =>
				{
					deque.PushBack(deque.Count);
					e.Reset();
				});
			Assert.Throws(typeof(Exception), () => e.MoveNext());
		}
	}
}
#endif