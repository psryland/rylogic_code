using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rylogic.Extn;

namespace Rylogic.Container
{
	/// <summary>A list wrapper that represents a set of contiguous lists</summary>
	public class ConcatList<T> :IList<T> ,IList
	{
		public ConcatList(IList<IList<T>>? lists = null)
		{
			m_lists = lists ?? new List<IList<T>>();
			SyncRoot = new object();
		}

		/// <summary>Address within the set of lists</summary>
		public struct Addr
		{
			public Addr(int list_idx, int item_idx)
			{
				ListIdx = list_idx;
				ItemIdx = item_idx;
			}

			/// <summary>The index of the list in 'Lists'</summary>
			public int ListIdx { get; set; }

			/// <summary>The index of the item within the list 'Lists[ListIdx]'</summary>
			public int ItemIdx { get; set; }
		}

		/// <summary>The collection of lists that this container is wrapping</summary>
		public IList<IList<T>> Lists
		{
			get { return m_lists; }
			set
			{
				if (m_lists == value) return;
				m_lists = value ?? new List<IList<T>>();
				if (m_lists.Count == 0)
					m_lists.Add(new List<T>());
			}
		}
		private IList<IList<T>> m_lists;

		/// <summary>Convert a list index and item index to a ConcatList index</summary>
		public int AddrToIndex(Addr addr)
		{
			if (addr.ListIdx == -1 || addr.ItemIdx == -1)
				return -1;

			var index = 0;
			for (int i = 0; i != addr.ListIdx; ++i) index += Lists[i].Count;
			index += addr.ItemIdx;
			return index;
		}

		/// <summary>Convert a ConcatList index to a list index and item index within that list</summary>
		public Addr IndexToAddr(int index)
		{
			// If the set of lists is empty, return the default address
			if (Lists == null)
				return new Addr(-1, -1);

			// Index position -1 is the end of the last list
			if (index == -1)
				return new Addr(Lists.Count - 1, -1);

			// Find the movement set and the row within that set that this row corresponds to
			var addr = new Addr(0, index);
			for (; addr.ListIdx != Lists.Count && addr.ItemIdx >= Lists[addr.ListIdx].Count; ++addr.ListIdx)
				addr.ItemIdx -= Lists[addr.ListIdx].Count;

			// 'index' is outside the range of the list, clamp to the last item
			if (addr.ListIdx == Lists.Count)
			{
				addr.ListIdx = Lists.Count - 1;
				addr.ItemIdx = -1;
			}

			return addr;
		}

		/// <summary>Return the list associated with a ConcatList index</summary>
		public IList<T> IndexToList(int index)
		{
			var addr = IndexToAddr(index);
			return Lists[addr.ListIdx];
		}

		/// <summary>Return the item associated with a ConcatList index</summary>
		public T IndexToItem(int index)
		{
			var addr = IndexToAddr(index);
			return Lists[addr.ListIdx][addr.ItemIdx];
		}

		/// <summary>Get/Set an item by ConcatList index</summary>
		public T this[int index]
		{
			get { return IndexToItem(index); }
			set
			{
				var addr = IndexToAddr(index);
				var list = Lists[addr.ListIdx];
				list[addr.ItemIdx] = value;
			}
		}
		object? IList.this[int index]
		{
			get { return this[index]; }
			set { this[index] = value != null ? (T)value : default!; }
		}

		/// <summary>The number of items in the ConcatList</summary>
		public int Count
		{
			get
			{
				var sum = 0;
				foreach (var list in Lists) sum += list.Count;
				return sum;
			}
		}

		/// <summary>True if the ConcatList is read only</summary>
		public bool IsReadOnly
		{
			get { return Lists.Count == 0 || Lists[0].IsReadOnly; }
		}

		/// <summary>True if the ConcatList cannot be resized</summary>
		public bool IsFixedSize
		{
			get { return Lists.OfType<IList>().Any(x => x.IsFixedSize); }
		}

		/// <summary>Synchronisation object</summary>
		public object SyncRoot
		{
			get; private set;
		}

		/// <summary>True if this collection is thread safe</summary>
		public bool IsSynchronized
		{
			get { return false; }
		}

		/// <summary>Append an item to the last list</summary>
		public void Add(T item)
		{
			Lists.Back().Add(item);
		}
		int IList.Add(object? value)
		{
			Add((T)value!);
			return Count - 1;
		}

		/// <summary>Remove all items from the lists. Note, doesn't clear the Lists collection itself</summary>
		public void Clear()
		{
			foreach (var list in Lists)
				list.Clear();
		}
		public void Clear(bool purge_empty_lists)
		{
			Clear();
			if (purge_empty_lists)
				PurgeEmptyLists();
		}

		/// <summary>True if 'item' exists within one of the 'Lists'</summary>
		public bool Contains(T item)
		{
			foreach (var list in Lists)
				if (list.Contains(item))
					return true;

			return false;
		}
		bool IList.Contains(object? value)
		{
			return Contains((T)value!);
		}

		/// <summary>Populate 'array' with the contents of this ConcatList</summary>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));
			if (arrayIndex >= array.Length)
				throw new ArgumentOutOfRangeException(nameof(arrayIndex), $"Array index is out of range [0,{array.Length})");
			if (array.Length - arrayIndex < Count)
				throw new ArgumentException($"The number of elements in this ConcatList ({Count}) exceeds the available space ({array.Length} - {arrayIndex})", nameof(array));

			foreach (var list in Lists)
			{
				list.CopyTo(array, arrayIndex);
				arrayIndex += list.Count;
			}
		}
		void ICollection.CopyTo(Array array, int index)
		{
			CopyTo((T[])array, index);
		}

		/// <summary>The index of the first occurrence of 'item' in this ConcatList</summary>
		public int IndexOf(T item)
		{
			var ofs = 0;
			foreach (var list in Lists)
			{
				var idx = list.IndexOf(item);
				if (idx != -1) return ofs + idx;
				ofs += list.Count;
			}
			return -1;
		}
		int IList.IndexOf(object? value)
		{
			return IndexOf((T)value!);
		}

		/// <summary>Insert 'item' into the list at index position 'index'</summary>
		public void Insert(int index, T item)
		{
			var addr = IndexToAddr(index);
			if (addr.ListIdx == -1)
				throw new Exception("Cannot insert an item if the Lists collection is empty. At least one list must exist");

			var list = Lists[addr.ListIdx];
			list.Insert(addr.ItemIdx != -1 ? addr.ItemIdx : list.Count, item);
		}
		void IList.Insert(int index, object? value)
		{
			Insert(index, (T)value!);
		}

		/// <summary>Remove the first occurrence of 'item' from this ConcatList</summary>
		public bool Remove(T item)
		{
			foreach (var list in Lists)
				if (list.Remove(item))
					return true;

			return false;
		}
		void IList.Remove(object? value)
		{
			Remove((T)value!);
		}
		public void Remove(T item, bool purge_empty_lists)
		{
			Remove(item);
			if (purge_empty_lists)
				PurgeEmptyLists();
		}

		/// <summary>Remove the item at ConcatList position 'index'</summary>
		public void RemoveAt(int index)
		{
			var addr = IndexToAddr(index);
			Lists[addr.ListIdx].RemoveAt(addr.ItemIdx);
		}
		public void RemoveAt(int index, bool purge_empty_lists)
		{
			RemoveAt(index);
			if (purge_empty_lists)
				PurgeEmptyLists();
		}

		/// <summary>Remove lists from the 'Lists' collection that are empty (excluding the first)</summary>
		public void PurgeEmptyLists()
		{
			for (int i = Lists.Count; i-- > 1;)
			{
				if (Lists[i].Count != 0) continue;
				Lists.RemoveAt(i);
			}
		}

		/// <summary>ConcatList enumerator</summary>
		public IEnumerator<T> GetEnumerator()
		{
			foreach (var list in Lists)
				foreach (var item in list)
					yield return item;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}
	}
}
