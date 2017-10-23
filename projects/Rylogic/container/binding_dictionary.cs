using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using pr.common;
using pr.extn;
using pr.util;

namespace pr.container
{
	[DebuggerDisplay("Count={Count}")]
	public class BindingDict<TKey, TValue>
		:IDictionary<TKey, TValue>
		,IBindingList, IList<TValue>, IList, IReadOnlyList<TValue>, IListChanging<TValue>, IItemChanged<TValue>
		,ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
		,ICancelAddNew , IBatchChanges
	{
		// Notes:
		//  - Hybrid container:
		//     Conceptually, this is a dictionary with an array that defines the order of values.
		//     Dictionary provides O(NLogN) Insert/Remove/Lookup
		//     List provides Order/Sorting/Binding events. Basically maps index to TKey.
		//  - IBindingList, IList is a list of 'TValue'
		//  - AddNew functionality requires the 'KeyFrom' member to be valid
		//  - This is analogous to BindingList, *NOT* BindingSource. i.e. there's not concept of 'Current'

		private Dictionary<TKey, TValue> m_dict;
		private List<TKey> m_keys;

		public BindingDict()
		{
			m_dict = new Dictionary<TKey, TValue>();
			m_keys = new List<TKey>();
			Init();
		}
		public BindingDict(BindingDict<TKey, TValue> rhs)
		{
			m_dict = new Dictionary<TKey, TValue>(rhs.m_dict);
			m_keys = new List<TKey>(rhs.m_keys);
			Init(rhs);
		}
		public BindingDict(int capacity)
		{
			m_dict = new Dictionary<TKey, TValue>(capacity);
			m_keys = new List<TKey>(capacity);
			Init();
		}
		public BindingDict(IDictionary<TKey, TValue> dictionary)
		{
			m_dict = new Dictionary<TKey, TValue>(dictionary);
			m_keys = new List<TKey>(dictionary.Keys);
			Init();
		}
		public BindingDict(IEqualityComparer<TKey> comparer)
		{
			m_dict = new Dictionary<TKey, TValue>(comparer);
			m_keys = new List<TKey>();
			Init();
		}
		public BindingDict(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
		{
			m_dict = new Dictionary<TKey, TValue>(dictionary, comparer);
			m_keys = new List<TKey>(dictionary.Keys);
			Init();
		}
		public BindingDict(int capacity, IEqualityComparer<TKey> comparer)
		{
			m_dict = new Dictionary<TKey, TValue>(capacity, comparer);
			m_keys = new List<TKey>(capacity);
			Init();
		}
		private void Init()
		{
			SupportsSorting        = true;
			RaiseListChangedEvents = true;
			KeyFrom                = null;
			IsReadOnly             = false;
			IsSorted               = false;
			SortProperty           = null;
			SortDirection          = ListSortDirection.Ascending;
		}
		private void Init(BindingDict<TKey, TValue> rhs)
		{
			SupportsSorting        = rhs.SupportsSorting;
			RaiseListChangedEvents = rhs.RaiseListChangedEvents;
			KeyFrom                = rhs.KeyFrom;
			IsReadOnly             = rhs.IsReadOnly;
			IsSorted               = rhs.IsSorted;
			SortProperty           = rhs.SortProperty;
			SortDirection          = rhs.SortDirection;
		}

		/// <summary>A function to return the key from a value</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public Func<TValue, TKey> KeyFrom
		{
			get;
			set;
		}

		/// <summary>The number of items in the collection</summary>
		public int Count
		{
			get { return m_dict.Count; }
		}

		/// <summary>Reset the collection</summary>
		public void Clear()
		{
			ClearItems();
		}

		/// <summary>Associative access</summary>
		public virtual TValue this[TKey key]
		{
			get { return m_dict[key]; }
			set
			{
				// If 'key' is already in the dictionary, just update the value.
				// Otherwise, insert 'key:value' at the end of the collection
				if (m_dict.ContainsKey(key))
					SetItem(key, value, m_keys.IndexOf(key));
				else
					InsertItem(key, value, m_keys.Count);
			}
		}

		/// <summary>Random access</summary>
		public virtual TValue At(int index)
		{
			// Can't create overload this[int index] because TKey can be 'int', which is ambiguous
			return this[m_keys[index]];
		}

		/// <summary>The keys collection</summary>
		public ICollection<TKey> Keys
		{
			get { return m_dict.Keys; }
		}

		/// <summary>The values collection</summary>
		public ICollection<TValue> Values
		{
			get { return m_dict.Values; }
		}

		/// <summary>List changed notification</summary>
		public event ListChangedEventHandler ListChanged;
		protected virtual void OnListChanged(ListChangedEventArgs args)
		{
			ListChanged?.Invoke(this, args);
		}

		/// <summary>Raised whenever items are added or about to be removed from the list</summary>
		public event EventHandler<ListChgEventArgs<TValue>> ListChanging;
		protected virtual void OnListChanging(ListChgEventArgs<TValue> args)
		{
			ListChanging?.Invoke(this, args);
		}

		/// <summary>Raised whenever an element in the list is changed</summary>
		public event EventHandler<ItemChgEventArgs<TValue>> ItemChanged;
		protected virtual void OnItemChanged(ItemChgEventArgs<TValue> args)
		{
			ItemChanged?.Invoke(this, args);
		}

		/// <summary>Gets/Sets a value indicating whether adding or removing items within the list raises ListChanged events.</summary>
		public bool RaiseListChangedEvents { get; set; }
		public Scope SuspendEvents(bool reset_on_resume)
		{
			return Scope.Create(
				() => { var r = RaiseListChangedEvents; RaiseListChangedEvents = false; return r; },
				re => { RaiseListChangedEvents = re; if (reset_on_resume) ResetBindings(); });
		}

		/// <summary>True if the collection is read only</summary>
		public bool IsReadOnly
		{
			get;
			set;
		}

		/// <summary>True if 'key' is a key in this collection</summary>
		public bool ContainsKey(TKey key)
		{
			return m_dict.ContainsKey(key);
		}

		/// <summary>True if 'value' is a value in this collection</summary>
		public bool ContainsValue(TValue value)
		{
			return m_dict.ContainsValue(value);
		}

		/// <summary>The index of 'item' if found in the collection, otherwise -1.</summary>
		public int IndexOf(TValue value)
		{
			for (var i = 0; i != m_keys.Count; ++i)
			{
				var key = m_keys[i];
				if (!Equals(m_dict[key], value)) continue;
				return i;
			}
			return -1;
		}

		/// <summary>Add 'value' to the end of the collection. Requires 'KeyFrom'</summary>
		public void Add(TValue value)
		{
			AssertListAccess();
			Insert(KeyFrom(value), value, m_keys.Count);
		}

		/// <summary>Add 'key:value' to the end of the collection</summary>
		public void Add(TKey key, TValue value)
		{
			Insert(key, value, m_keys.Count);
		}

		/// <summary>Add 'key:value' to the collection with index position 'index'</summary>
		public void Insert(TKey key, TValue value, int index)
		{
			InsertItem(key, value, index);
		}

		/// <summary>Remove the object at index position 'index'</summary>
		public void RemoveAt(int index)
		{
			RemoveItem(m_keys[index], index);
		}

		/// <summary>Remove the object associated with 'key'</summary>
		public bool Remove(TKey key)
		{
			if (!ContainsKey(key))
				return false;

			var idx = m_keys.IndexOf(key);
			RemoveItem(key, idx);
			return true;
		}

		/// <summary>Remove the object associated with 'key'</summary>
		public bool Remove(TValue value)
		{
			var idx = IndexOf(value);
			if (idx == -1) return false;
			RemoveItem(m_keys[idx], idx);
			return true;
		}

		/// <summary>Test for 'key' in the collection and, if so, return the associated value</summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			return m_dict.TryGetValue(key, out value);
		}

		/// <summary>Get/Set if sorting is supported</summary>
		public bool SupportsSorting
		{
			get;
			set;
		}

		/// <summary>True if sorted</summary>
		public bool IsSorted
		{
			get;
			private set;
		}

		/// <summary>The property that sort is done on</summary>
		public PropertyDescriptor SortProperty
		{
			get;
			set;
		}

		/// <summary>The direction to sort</summary>
		public ListSortDirection SortDirection
		{
			get;
			set;
		}

		/// <summary>Notify observers of the entire list changing</summary>
		public void ResetBindings()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.PreReset, -1, default(TValue));
				OnListChanging(args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.Reset, -1, default(TValue)));
				OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
		}

		/// <summary>Notify observers of a specific item changing</summary>
		public void ResetItem(TKey key)
		{
			var index = m_keys.IndexOf(key);
			ResetItem(key, index);
		}
	
		/// <summary>Notify observers of a specific item changing</summary>
		public void ResetItem(TValue value)
		{
			var idx = IndexOf(value);
			if (idx < 0 || idx >= Count)
				throw new Exception("Item is not within this container");

			ResetItem(m_keys[idx], idx);
		}

		/// <summary>Notify observers of a specific item changing</summary>
		private void ResetItem(TKey key, int index)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.ItemPreReset, index, m_dict[key]);
				OnListChanging(args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.ItemReset, index, m_dict[key]));
				OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
			}
		}

		/// <summary>Reset the collection</summary>
		private void ClearItems()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.PreReset, -1, default(TValue));
				OnListChanging(args);
				if (args.Cancel)
					return;
			}

			ClearItemsCore();

			if (RaiseListChangedEvents)
			{
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.Reset, -1, default(TValue)));
				OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
		}
		protected virtual void ClearItemsCore()
		{
			m_dict.Clear();
			m_keys.Clear();
		}

		/// <summary>Insert a key:value pair into the collection</summary>
		private void InsertItem(TKey key, TValue value, int index)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.ItemPreAdd, index, value);
				OnListChanging(args);
				if (args.Cancel)
					return;

				// Allow PreAdd to modify 'item' and 'index'
				value = args.Item;
				index = args.Index;
			}

			InsertItemCore(key, value, index);

			if (RaiseListChangedEvents)
			{
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.ItemAdded, index, value));
				OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, index));
			}
		}
		protected virtual void InsertItemCore(TKey key, TValue value, int index)
		{
			if (ContainsKey(key))
				throw new InvalidOperationException(string.Format("Key:Value pair ({0},{1}) is already in the collection", key, value));

			m_dict[key] = value;
			m_keys.Insert(index, key);
		}

		/// <summary>Remove the item associated with 'key' with the expected index 'index' in 'm_keys'</summary>
		private void RemoveItem(TKey key, int index)
		{
			var item = m_dict[key];
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.ItemPreRemove, index, item);
				OnListChanging(args);
				if (args.Cancel)
					return;
			}

			RemoveItemCore(key, index);
	
			if (RaiseListChangedEvents)
			{
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.ItemRemoved, -1, item));
				OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, index));
			}
		}
		protected virtual void RemoveItemCore(TKey key, int index)
		{
			Debug.Assert(Equals(m_keys[index], key));
			m_keys.RemoveAt(index);
			m_dict.Remove(key);
		}
		
		/// <summary>Replace an existing item in the collection</summary>
		private void SetItem(TKey key, TValue value, int index)
		{
			var old = m_dict[key];
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.ItemPreRemove, index, old);
				OnListChanging(args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<TValue>(this, ListChg.ItemPreAdd, index, value);
				OnListChanging(args);
				if (args.Cancel)
					return;
			}

			// Replace the item
			SetItemCore(key, value);

			if (RaiseListChangedEvents)
			{
				OnItemChanged(new ItemChgEventArgs<TValue>(index, old, value));
				OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, index));
			}
			if (RaiseListChangedEvents)
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.ItemRemoved, index, old));
			if (RaiseListChangedEvents)
				OnListChanging(new ListChgEventArgs<TValue>(this, ListChg.ItemAdded, index, value));
		}
		protected virtual void SetItemCore(TKey key, TValue value)
		{
			m_dict[key] = value;
		}

		/// <summary>Test for support for accessing this container like a list</summary>
		private void AssertListAccess()
		{
			if (KeyFrom != null) return;
			throw new NotSupportedException("List-like access requires the 'KeyFrom' member to be valid");
		}

		/// <summary>Raised when the caller signals a batch of changes are about to happen</summary>
		public event EventHandler<PrePostEventArgs> BatchChanges;
		public Scope BatchChange()
		{
			return Scope.Create(
				() => BatchChanges.Raise(this, new PrePostEventArgs(after:false)),
				() => BatchChanges.Raise(this, new PrePostEventArgs(after:true)));
		}

		#region IBindingList

		/// <summary>Allow items to be added to the collection</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IBindingList.AllowNew
		{
			get { return KeyFrom != null; }
		}

		/// <summary>Allow items in the collection to be changed</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IBindingList.AllowEdit
		{
			get { return true; }
		}

		/// <summary>Allow items to be removed from the collection</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IBindingList.AllowRemove
		{
			get { return true; }
		}

		/// <summary>True because this is a binding collection</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IBindingList.SupportsChangeNotification
		{
			get { return true; }
		}

		/// <summary>True, can be searched</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IBindingList.SupportsSearching
		{
			get { return true; }
		}

		/// <summary></summary>
		void IBindingList.AddIndex(PropertyDescriptor property)
		{
			// todo
			throw new NotImplementedException();
		}

		/// <summary>Add a new default item to the collection</summary>
		object IBindingList.AddNew()
		{
			AssertListAccess();

			// Can only add new items if we can generate the key from the value
			var value = Activator.CreateInstance<TValue>();
			var key = KeyFrom(value);

			// Add the new item to the collection
			Add(key, value);
			return value;
		}

		/// <summary>Sort the collection based on 'property'</summary>
		void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
		{
			m_keys.Sort(new Comparison<TKey>((l,r) =>
			{
				var v0 = property.GetValue(m_dict[l]);
				var v1 = property.GetValue(m_dict[r]);
				return direction == ListSortDirection.Ascending ? Comparer.Default.Compare(v0, v1) : Comparer.Default.Compare(v1, v0);
			}));
		}

		/// <summary></summary>
		int IBindingList.Find(PropertyDescriptor property, object key)
		{
			// todo
			throw new NotImplementedException();
		}

		/// <summary></summary>
		void IBindingList.RemoveIndex(PropertyDescriptor property)
		{
			// todo
			throw new NotImplementedException();
		}

		/// <summary></summary>
		void IBindingList.RemoveSort()
		{
			// todo
			throw new NotImplementedException();
		}

		#endregion

		#region ICancelAddNew

		/// <summary>Discards a pending new item from the collection.</summary>
		void ICancelAddNew.CancelNew(int itemIndex)
		{
			RemoveItem(m_keys[itemIndex], itemIndex);
		}

		/// <summary>Commits a pending new item to the collection.</summary>
		void ICancelAddNew.EndNew(int itemIndex)
		{
			// Nothing to do, it's already in there
		}

		#endregion

		#region IList

		/// <summary>Array access</summary>
		object IList.this[int index]
		{
			get { return ((IList<TValue>)this)[index]; }
			set { ((IList<TValue>)this)[index] = (TValue)value; }
		}

		/// <summary>False, dynamically resizing</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool IList.IsFixedSize
		{
			get { return false; }
		}

		/// <summary>Add an object to the collection</summary>
		int IList.Add(object value)
		{
			Add((TValue)value);
			return m_keys.Count - 1;
		}

		/// <summary>True if 'value' is in this collection</summary>
		bool IList.Contains(object value)
		{
			if (value is TValue)
				return ContainsValue((TValue)value);
			if (value is TKey)
				return ContainsKey((TKey)value);
			if (value is KeyValuePair<TKey,TValue>)
				return ((ICollection<KeyValuePair<TKey, TValue>>)this).Contains((KeyValuePair<TKey,TValue>)value);
			return false;
		}

		/// <summary>The index of the given value (O(N²LogN) search)</summary>
		int IList.IndexOf(object value)
		{
			return IndexOf((TValue)value);
		}

		/// <summary>Insert an object at 'index' into the collection</summary>
		void IList.Insert(int index, object value)
		{
			((IList<TValue>)this).Insert(index, (TValue)value);
		}

		/// <summary>Remove an object from the collection</summary>
		void IList.Remove(object value)
		{
			var idx = ((IList)this).IndexOf(value);
			RemoveAt(idx);
		}

		#endregion

		#region IList<TValue>

		/// <summary>Array access</summary>
		TValue IList<TValue>.this[int index]
		{
			get { return At(index); }
			set { this[m_keys[index]] = value; }
		}

		/// <summary>Insert 'item' into the collection. Requires 'KeyFrom'</summary>
		void IList<TValue>.Insert(int index, TValue value)
		{
			AssertListAccess();

			var key = KeyFrom(value);
			Insert(key, value, index);
		}

		#endregion

		#region IReadOnlyList<TValue>

		/// <summary>Array access</summary>
		TValue IReadOnlyList<TValue>.this[int index]
		{
			get { return At(index); }
		}

		#endregion

		#region ICollection

		/// <summary>Gets an object that can be used to synchronize access to the System.Collections.ICollection.</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object ICollection.SyncRoot
		{
			get { return ((ICollection)m_dict).SyncRoot; }
		}

		/// <summary>Gets a value indicating whether access to the System.Collections.ICollection is synchronized (thread safe).</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		bool ICollection.IsSynchronized
		{
			get { return ((ICollection)m_dict).IsSynchronized; }
		}

		/// <summary>Copies the elements of the System.Collections.ICollection to an System.Array, starting at a particular System.Array index.</summary>
		void ICollection.CopyTo(Array array, int index)
		{
			((ICollection)m_dict).CopyTo(array, index);
		}

		#endregion

		#region ICollection<TValue>

		/// <summary>True if 'value' is in this collection</summary>
		bool ICollection<TValue>.Contains(TValue value)
		{
			return ContainsValue(value);
		}

		/// <summary>Copies the elements of the System.Collections.ICollection to an System.Array, starting at a particular System.Array index.</summary>
		void ICollection<TValue>.CopyTo(TValue[] array, int index)
		{
			for (int i = index; i != m_keys.Count; ++i)
				array[i] = m_dict[m_keys[i]];
		}

		#endregion

		#region ICollection<KeyValuePair<TKey, TValue>>

		/// <summary>Add a 'key:value' pair</summary>
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>Remove 'item' from the collection</summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			if (!((ICollection<KeyValuePair<TKey, TValue>>)this).Contains(item))
				return false;

			return Remove(item.Key);
		}

		/// <summary>True if the 'key:value' pair is in the collection</summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
		{
			return ContainsKey(item.Key) && Equals(m_dict[item.Key], item.Value);
		}

		/// <summary>Copy to buffer</summary>
		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, TValue>>)m_dict).CopyTo(array, arrayIndex);
		}

		#endregion

		#region IEnumerable

		/// <summary>Enumerate values</summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_dict.Values.GetEnumerator();
		}

		#endregion

		#region IEnumerable<TValue>

		/// <summary>Enumerate values</summary>
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
		{
			foreach (var key in m_keys)
				yield return m_dict[key];
		}

		#endregion

		#region IEnumerable<KeyValuePair<TKey, TValue>>

		/// <summary>Enumerate pairs</summary>
		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<TKey, TValue>>)this).GetEnumerator();
		}

		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Collections.Generic;
	using container;

	[TestFixture] public class TestBindingDictionary
	{
		private class Stats
		{
			public int Adds;
		}

		[Test] public void BindingDictionary()
		{
			var stats = new Stats();

			var a0 = new BindingDict<int,double>();
			var s0 = new BindingSource<double>{ DataSource = a0 };

			s0.ListChanging += (s,a) =>
			{
				if (a.ChangeType == ListChg.ItemAdded)
					++stats.Adds;
			};

			for (int i = 0; i != 5; ++i)
				a0.Add(i, i * 1.1);

			Assert.AreEqual(stats.Adds, 5);
		}
	}
}
#endif
