//***********************************************
// ListEvt
//  Copyright © Rylogic Ltd 2010
//***********************************************

using System;
using System.Collections;
using System.Collections.Generic;
using pr.extn;

namespace pr.container
{
	// List wrapper with events
	[Obsolete("using binding list instead")] public class ListEvt<T> :List<T> ,IList<T> ,IList
	{
		public ListEvt() {}
		public ListEvt(int capacity) :base(capacity) {}
		public ListEvt(int count, T item) :base(count) { for (int i = 0; i != count; ++i) base.Add(item); }
		public ListEvt(IEnumerable<T> copy) :base(copy) {}

		/// <summary>Occurs whenever an element is changed in the list</summary>
		public event EventHandler<ListChgEventArgs> ListChanged;//(list, chg_type)
		public class ListChgEventArgs :EventArgs
		{
			/// <summary>The type of change made to the list</summary>
			public ListChg ChgType { get; private set; }

			/// <summary>The index of the item that was changed, or first index of a range of changed items</summary>
			public int Index0 { get; private set; }

			/// <summary>The index of a related changed item, or one passed the last index of a range of changed items</summary>
			public int Index1 { get; private set; }

			public ListChgEventArgs(ListChg chg_type, int index0, int index1)
			{
				ChgType = chg_type;
				Index0 = index0;
				Index1 = index1;
			}
		}

		/// <summary>Occurs whenever an element in the list is changed</summary>
		public event EventHandler<ItemChgEventArgs> ItemChanged;//(list, index)
		public class ItemChgEventArgs :EventArgs
		{
			public int Index { get; private set; }
			public ItemChgEventArgs(int index)
			{
				Index = index;
			}
		}

		/// <summary>Allow the ListChanged event to be suspended</summary>
		public bool RaiseListChangedEvents
		{
			get { return !ListChanged.IsSuspended(); }
			set { if (value != RaiseListChangedEvents) {if (value) ListChanged.Resume(); else ListChanged.Suspend();} }
		}

		/// <summary>Remove all items from the list</summary>
		public new void Clear()
		{
			bool notify = Count != 0;
			base.Clear();
			if (notify) ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reset, 0, 0));
		}

		/// <summary>Add an item to the list</summary>
		public new void Add(T item)
		{
			int index = Count;
			base.Add(item);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, index + 1));
		}
		int IList.Add(object value)
		{
			Add((T)value);
			return Count - 1;
		}

		/// <summary>Add a collection to the list</summary>
		public new void AddRange(IEnumerable<T> collection)
		{
			int index = Count;
			base.AddRange(collection);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, Count));
		}

		/// <summary>Insert an item into the list at position 'index'</summary>
		public new void Insert(int index, T item)
		{
			base.Insert(index, item);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, index + 1));
		}
		void IList.Insert(int index, object value)
		{
			Insert(index, (T)value);
		}

		/// <summary>Insert a collection into the list at position 'index'</summary>
		public new void InsertRange(int index, IEnumerable<T> collection)
		{
			int remaining = Count - index;
			base.InsertRange(index, collection);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, Count - remaining));
		}

		/// <summary>Remove an item from the list</summary>
		public new void Remove(T item)
		{
			int index = IndexOf(item);
			if (index >= 0 && index < Count) RemoveAt(index);
		}
		void IList.Remove(object value)
		{
			Remove((T)value);
		}

		/// <summary>Remove items by predicate</summary>
		public new void RemoveAll(Predicate<T> match)
		{
			int count = base.RemoveAll(match);
			if (count != 0)
				ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemRemoved, 0, count));
		}

		/// <summary>Remove an item by index</summary>
		public new void RemoveAt(int index)
		{
			base.RemoveAt(index);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemRemoved, index, index + 1));
		}

		/// <summary>Remove a range of items from the list</summary>
		public new void RemoveRange(int index, int count)
		{
			base.RemoveRange(index, count);
			if (count != 0)
				ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemRemoved, index, index + count));
		}

		/// <summary>Random access to elements in the list. ItemChanged signalled on assignment</summary>
		public new T this[int index]
		{
			get { return base[index]; }
			set { if (!Equals(base[index],value)) {base[index] = value; RaiseItemChanged(index); } }
		}
		object IList.this[int index]
		{
			get { return base[index]; }
			set { this[index] = (T)value; }
		}

		/// <summary>Reverses the order of elements in the collection</summary>
		public new void Reverse()
		{
			base.Reverse();
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}
		public new void Reverse(int index, int count)
		{
			base.Reverse(index, count);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}

		/// <summary>Sort the contents of the container</summary>
		public new void Sort()
		{
			base.Sort();
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}
		public new void Sort(Comparison<T> comparison)
		{
			base.Sort(comparison);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}
		public new void Sort(IComparer<T> comparer)
		{
			base.Sort(comparer);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}
		public new void Sort(int index, int count, IComparer<T> comparer)
		{
			base.Sort(index, count, comparer);
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, 0, Count));
		}

		/// <summary>Force the ListChanged event to be fired</summary>
		public void RaiseListChanged(ListChgEventArgs chg)
		{
			ListChanged.Raise(this, chg);
		}

		/// <summary>Force the ItemChanged event to be fired</summary>
		public void RaiseItemChanged(int index)
		{
			ItemChanged.Raise(this, new ItemChgEventArgs(index));
			ListChanged.Raise(this, new ListChgEventArgs(ListChg.ItemChanged, index, index + 1));
		}

		/// <summary>Swap elements in the list</summary>
		public void Swap(int index0, int index1)
		{
			if (index0 == index1) return;
			bool raise_events = RaiseListChangedEvents;
			try
			{
				RaiseListChangedEvents = false;
				T tmp = base[index0];
				base[index0] = base[index1];
				base[index1] = tmp;
				RaiseListChangedEvents = raise_events;
				ListChanged.Raise(this, new ListChgEventArgs(ListChg.Reordered, index0, index1));
			}
			finally { RaiseListChangedEvents = raise_events; }
		}

		/// <summary>Return the first element in the range</summary>
		public T Front
		{
			get { return base[0]; }
		}

		/// <summary>Return the last element in the range</summary>
		public T Back
		{
			get { return base[Count - 1]; }
		}
	}
}