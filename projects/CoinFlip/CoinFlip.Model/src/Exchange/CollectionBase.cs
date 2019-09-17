using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Rylogic.Container;
using Rylogic.Utility;

namespace CoinFlip
{
	public class CollectionBase<TKey, TValue> :IEnumerable<TValue>, IList, INotifyCollectionChanged, IListChanging<TValue>
	{
		protected readonly BindingDict<TKey, TValue> m_data;
		public CollectionBase(Exchange exchange, Func<TValue, TKey> key_from)
		{
			Exchange = exchange;
			Updated = new ConditionVariable<DateTimeOffset>(DateTimeOffset.MinValue);
			m_data = new BindingDict<TKey, TValue> { KeyFrom = key_from };
			m_data.CollectionChanged += (s, a) => CollectionChanged?.Invoke(this, a);
			m_data.ListChanging += (s, a) => ListChanging?.Invoke(this, a);
		}

		/// <summary>The owning exchange</summary>
		public Exchange Exchange { get; }

		/// <summary></summary>
		public int Count => m_data.Count;

		/// <summary>True if 'key' is in this collection</summary>
		public bool ContainsKey(TKey key)
		{
			return m_data.ContainsKey(key);
		}

		/// <summary>Associative lookup</summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			return m_data.TryGetValue(key, out value);
		}

		/// <summary>Wait-able object for update notification</summary>
		public ConditionVariable<DateTimeOffset> Updated { get; }

		/// <summary>The time when data in this collection was last updated. Note: *NOT* when collection changed, when the elements in the collection changed</summary>
		public DateTimeOffset LastUpdated
		{
			get { return m_last_updated; }
			set
			{
				m_last_updated = value;
				Updated.NotifyAll(value);
			}
		}
		private DateTimeOffset m_last_updated;

		/// <summary></summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		/// <summary></summary>
		public event EventHandler<ListChgEventArgs<TValue>> ListChanging;
		public bool RaiseListChangedEvents
		{
			get => m_data.RaiseListChangedEvents;
			set => m_data.RaiseListChangedEvents = value;
		}

		#region IList
		bool IList.IsReadOnly => true;
		bool IList.IsFixedSize => ((IList)m_data).IsFixedSize;
		object ICollection.SyncRoot => ((IList)m_data).SyncRoot;
		bool ICollection.IsSynchronized => ((IList)m_data).IsSynchronized;
		object IList.this[int index]
		{
			get => ((IList)m_data)[index];
			set => throw new NotSupportedException();
		}
		int IList.Add(object value)
		{
			throw new NotSupportedException();
		}
		bool IList.Contains(object value)
		{
			return ((IList)m_data).Contains(value);
		}
		void IList.Clear()
		{
			throw new NotSupportedException();
		}
		int IList.IndexOf(object value)
		{
			return ((IList)m_data).IndexOf(value);
		}
		void IList.Insert(int index, object value)
		{
			throw new NotSupportedException();
		}
		void IList.Remove(object value)
		{
			throw new NotSupportedException();
		}
		void IList.RemoveAt(int index)
		{
			throw new NotSupportedException();
		}
		void ICollection.CopyTo(Array array, int index)
		{
			((IList)m_data).CopyTo(array, index);
		}
		#endregion

		#region IEnumerable
		public IEnumerator<TValue> GetEnumerator()
		{
			return m_data.Values.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
