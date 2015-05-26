using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>Extension to BindingList that notifies *before* an item is removed</summary>
	[DataContract] public class BindingDictionary<TKey,TValue> :IDictionary<TKey, TValue>
	{
		private Dictionary<TKey,TValue> m_dic;
		public BindingDictionary()
		{
			m_dic = new Dictionary<TKey,TValue>();
			Init();
		}
		public BindingDictionary(IDictionary<TKey, TValue> dictionary)
		{
			m_dic = new Dictionary<TKey,TValue>(dictionary);
			Init();
		}
		public BindingDictionary(IEqualityComparer<TKey> comparer)
		{
			m_dic = new Dictionary<TKey,TValue>(comparer);
			Init();
		}
		public BindingDictionary(int capacity)
		{
			m_dic = new Dictionary<TKey,TValue>(capacity);
			Init();
		}
		public BindingDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
		{
			m_dic = new Dictionary<TKey,TValue>(dictionary, comparer);
			Init();
		}
		public BindingDictionary(int capacity, IEqualityComparer<TKey> comparer)
		{
			m_dic = new Dictionary<TKey,TValue>(capacity, comparer);
			Init();
		}
		private void Init()
		{
			//// ResetBindings and ResetItem aren't overridable.
			//// Attach handlers to ensure we always receive the Reset event.
			//// Calling the 'new' method will cause the Pre events to be raised as well
			//m_dic.Changed += (s,a) =>
			//	{
			//		if (a.ListChangedType == ListChangedType.Reset)
			//			DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.Reset, default(TKey), default(TValue)));
			//		if (a.ListChangedType == ListChangedType.ItemChanged)
			//			DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.ItemReset, a.NewIndex, this[a.NewIndex]));
			//	};
		}

		/// <summary>Gets the System.Collections.Generic.IEqualityComparer<T> that is used to determine equality of keys for the dictionary.</summary>
		public IEqualityComparer<TKey> Comparer
		{
			get { return m_dic.Comparer; }
		}

		/// <summary>Gets the number of key/value pairs contained in the System.Collections.Generic.Dictionary<TKey,TValue></summary>
		public int Count
		{
			get { return m_dic.Count; }
		}

		/// <summary>Gets a collection containing the keys in the System.Collections.Generic.Dictionary<TKey,TValue></summary>
		public Dictionary<TKey, TValue>.KeyCollection Keys
		{
			get { return m_dic.Keys; }
		}

		/// <summary>Gets a collection containing the values in the System.Collections.Generic.Dictionary<TKey,TValue></summary>
		public Dictionary<TKey, TValue>.ValueCollection Values
		{
			get { return m_dic.Values; }
		}

		/// <summary>Gets or sets the value associated with the specified key.</summary>
		public TValue this[TKey key]
		{
			get { return m_dic[key]; }
			set
			{
				var old = m_dic[key];
				if (RaiseDictChangedEvents)
				{
					var args = new DictChgEventArgs<TKey,TValue>(DictChg.ItemPreRemove, key, old);
					DictChanging.Raise(this, args);
					if (args.Cancel)
						return;
				}
				if (RaiseDictChangedEvents)
				{
					var args = new DictChgEventArgs<TKey,TValue>(DictChg.ItemPreAdd, key, value);
					DictChanging.Raise(this, args);
					if (args.Cancel)
						return;
				}

				m_dic[key] = value;

				//if (RaiseDictChangedEvents)
				//	ItemChanged.Raise(this, new ItemChgEventArgs<T>(index, old, item));

				if (RaiseDictChangedEvents)
					DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.ItemRemoved, key, old));
				if (RaiseDictChangedEvents)
					DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.ItemAdded, key, value));
			}
		}

		/// <summary>Adds the specified key and value to the dictionary.</summary>
		public void Add(TKey key, TValue value)
		{
			if (RaiseDictChangedEvents)
			{
				var args = new DictChgEventArgs<TKey,TValue>(DictChg.ItemPreAdd, key, value);
				DictChanging.Raise(this, args);
				if (args.Cancel)
					return;

				// Allow PreAdd to modify 'key' and 'value'
				key   = args.Key;
				value = args.Value;
			}

			m_dic.Add(key, value);

			if (RaiseDictChangedEvents)
				DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.ItemAdded, key, value));
		}

		/// <summary>Removes all keys and values from the System.Collections.Generic.Dictionary<TKey,TValue>.</summary>
		public void Clear()
		{
			if (RaiseDictChangedEvents)
			{
				var args = new DictChgEventArgs<TKey,TValue>(DictChg.PreReset, default(TKey), default(TValue));
				DictChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}

			m_dic.Clear();

			if (RaiseDictChangedEvents)
				DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.Reset, default(TKey), default(TValue)));
		}

		/// <summary>Determines whether the System.Collections.Generic.Dictionary<TKey,TValue> contains the specified key.</summary>
		public bool ContainsKey(TKey key)
		{
			return m_dic.ContainsKey(key);
		}

		/// <summary>Determines whether the System.Collections.Generic.Dictionary<TKey,TValue> contains a specific value.</summary>
		public bool ContainsValue(TValue value)
		{
			return m_dic.ContainsValue(value);
		}

		/// <summary>Returns an enumerator that iterates through the System.Collections.Generic.Dictionary<TKey,TValue>.</summary>
		public Dictionary<TKey, TValue>.Enumerator GetEnumerator()
		{
			return m_dic.GetEnumerator();
		}

		/// <summary>
		/// Implements the System.Runtime.Serialization.ISerializable interface and returns
		/// the data needed to serialize the System.Collections.Generic.Dictionary<TKey,TValue>
		/// instance.</summary>
		[SecurityCritical]
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			m_dic.GetObjectData(info, context);
		}

		/// <summary>
		/// Implements the System.Runtime.Serialization.ISerializable interface and raises
		/// the deserialization event when the deserialization is complete.</summary>
		public virtual void OnDeserialization(object sender)
		{
			m_dic.OnDeserialization(sender);
		}

		/// <summary>Removes the value with the specified key from the System.Collections.Generic.Dictionary<TKey,TValue>.</summary>
		public bool Remove(TKey key)
		{
			var item = m_dic[key];
			if (RaiseDictChangedEvents)
			{
				var args = new DictChgEventArgs<TKey,TValue>(DictChg.ItemPreRemove, key, item);
				DictChanging.Raise(this, args);
				if (args.Cancel)
					return false;
			}

			var r = m_dic.Remove(key);

			if (RaiseDictChangedEvents)
				DictChanging.Raise(this, new DictChgEventArgs<TKey,TValue>(DictChg.ItemRemoved, key, item));

			return r;
		}

		/// <summary>Gets the value associated with the specified key.</summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			return m_dic.TryGetValue(key, out value);
		}
		
		///// <summary>Get/Set readonly for this list</summary>
		//public bool ReadOnly
		//{
		//	get { return !AllowNew && !AllowEdit && !AllowRemove; }
		//	set
		//	{
		//		// For some reason, setting these causes list changed events...
		//		using (SuspendEvents())
		//			AllowNew = AllowEdit = AllowRemove = !value;
		//	}
		//}

		/// <summary>Raised whenever items are added or about to be removed from the collection</summary>
		public event EventHandler<DictChgEventArgs<TKey,TValue>> DictChanging;

		/// <summary>Notify observers of the entire collection changing</summary>
		public void ResetBindings()
		{
			if (RaiseDictChangedEvents)
			{
				var args = new DictChgEventArgs<TKey,TValue>(DictChg.Reset, default(TKey), default(TValue));
				DictChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
		}
		
		/// <summary>Notify observers of a specific item changing</summary>
		public void ResetItem(TKey key)
		{
			if (RaiseDictChangedEvents)
			{
				var args = new DictChgEventArgs<TKey,TValue>(DictChg.ItemReset, key, m_dic[key]);
				DictChanging.Raise(this, args);
				if (args.Cancel)
					return;
			}
		}

		/// <summary>Allow/Disallow Dict changed events</summary>
		public bool RaiseDictChangedEvents { get; set; }

		/// <summary>RAII object for suspending list events</summary>
		public Scope SuspendEvents(bool reset_bindings_on_resume = false)
		{
			return Scope.Create(
				() =>
				{
					RaiseDictChangedEvents = false;
				},
				() =>
				{
					RaiseDictChangedEvents = true;
					if (reset_bindings_on_resume)
						ResetBindings();
				});
		}

		/// <summary>RAII object for suspending list events</summary>
		public Scope SuspendEvents(Func<bool> reset_bindings_on_resume)
		{
			return Scope.Create(
				() =>
				{
					RaiseDictChangedEvents = false;
				},
				() =>
				{
					RaiseDictChangedEvents = true;
					if (reset_bindings_on_resume())
						ResetBindings();
				});
		}

		#region IDictionary<TKey,TValue>
		ICollection<TKey> IDictionary<TKey,TValue>.Keys { get { return Keys; } }
		ICollection<TValue> IDictionary<TKey,TValue>.Values { get { return Values; } }
		void IDictionary<TKey,TValue>.Add(TKey key, TValue value) { Add(key, value); }
		bool IDictionary<TKey,TValue>.ContainsKey(TKey key) { return ContainsKey(key); }
		bool IDictionary<TKey,TValue>.Remove(TKey key) { return Remove(key); }
		bool IDictionary<TKey,TValue>.TryGetValue(TKey key, out TValue value) { return TryGetValue(key, out value); }
		#endregion

		#region ICollection<KeyValuePair<TKey,TValue>>
		int ICollection<KeyValuePair<TKey,TValue>>.Count { get { return Count; } }
		bool ICollection<KeyValuePair<TKey,TValue>>.IsReadOnly { get { return false; } }
		void ICollection<KeyValuePair<TKey,TValue>>.Add(KeyValuePair<TKey,TValue> item) { Add(item.Key, item.Value); }
		void ICollection<KeyValuePair<TKey,TValue>>.Clear() { Clear(); }
		bool ICollection<KeyValuePair<TKey,TValue>>.Contains(KeyValuePair<TKey,TValue> item) { return ContainsKey(item.Key) && ContainsValue(item.Value); }
		void ICollection<KeyValuePair<TKey,TValue>>.CopyTo(KeyValuePair<TKey,TValue>[] array, int arrayIndex) { throw new NotImplementedException(); }
		bool ICollection<KeyValuePair<TKey,TValue>>.Remove(KeyValuePair<TKey,TValue> item) { return Remove(item.Key); }
		#endregion

		#region IEnumerable<KeyValuePair<TKey,TValue>>
		System.Collections.Generic.IEnumerator<KeyValuePair<TKey,TValue>> System.Collections.Generic.IEnumerable<KeyValuePair<TKey,TValue>>.GetEnumerator() { return m_dic.GetEnumerator(); }
		#endregion

		#region IEnumerable
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return m_dic.GetEnumerator(); }
		#endregion IEnumerable
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Collections.Generic;
	using System.Linq;
	using container;

	[TestFixture] public class TestBindingDictionary
	{
		[Test] public void BindingDictionary()
		{
			var a0 = new BindingDictionary<int,double>();
			var a1 = new BindingDictionary<int,double>();

			for (int i = 0; i != 5; ++i)
			{
				a0.Add(i, i * 1.1);
				a1.Add(i, i + 1.1);
			}
		}
	}
}
#endif
