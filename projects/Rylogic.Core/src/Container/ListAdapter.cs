using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Rylogic.Container
{
	public class ListAdapter<TIn, TOut> :IList<TOut>, IList, INotifyCollectionChanged
	{
		// Notes:
		//  - Used to make a list of type 'TIn' appear like a list of 'TOut'
		//  - Uses function delegates to "Adapt" a 'TIn' to a 'TOut', and "Conform" a 'TOut' back to a 'TIn'.
		//  - *WARNING* if you're trying to use this class for binding it probably won't work because 'Adapt'
		//    typically returns a new instance for each list item. Binding will only attached to the first
		//    instance.

		public ListAdapter(IList<TIn> list, Func<TIn, TOut> adapt, Func<TOut, TIn>? conform = null)
		{
			Source = list;
			Adapt = adapt;
			Conform = conform;
		}

		/// <summary>The list being adapted</summary>
		public IList<TIn> Source { get; }

		/// <summary>Adapter function</summary>
		public Func<TIn, TOut> Adapt { get; }

		/// <summary>Inverse of the Adapter function</summary>
		public Func<TOut, TIn>? Conform { get; }

		/// <summary></summary>
		public int Count => Source.Count;

		/// <summary></summary>
		public bool IsReadOnly => Source.IsReadOnly || Conform == null;
		bool IList.IsFixedSize => Source is IList list && list.IsFixedSize;
		bool ICollection.IsSynchronized => Source is ICollection col && col.IsSynchronized;
		object ICollection.SyncRoot => Source is ICollection col ? col.SyncRoot : throw new NotSupportedException();

		/// <summary></summary>
		public TOut this[int index]
		{
			get { return Adapt(Source[index]); }
			set
			{
				if (IsReadOnly || Conform == null)
					throw new InvalidOperationException("Collection is read-only");
				
				Source[index] = Conform(value)!;
			}
		}
		object? IList.this[int index]
		{
			get => this[index]!;
			set => this[index] = (TOut)value!;
		}

		/// <summary></summary>
		public void Add(TOut item)
		{
			if (IsReadOnly|| Conform == null)
				throw new InvalidOperationException("Collection is read-only");

			Source.Add(Conform(item));
		}
		int IList.Add(object? value)
		{
			Add((TOut)value!);
			return Count - 1;
		}

		/// <summary></summary>
		public void Clear()
		{
			if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
			Source.Clear();
		}

		/// <summary></summary>
		public bool Contains(TOut item)
		{
			if (Conform != null)
				return Source.Contains(Conform(item));
			else
				return IndexOf(item) != -1;
		}
		bool IList.Contains(object? value)
		{
			return Contains((TOut)value!);
		}

		/// <summary></summary>
		public void CopyTo(TOut[] array, int arrayIndex)
		{
			if (arrayIndex + array.Length < Count)
				throw new InsufficientMemoryException("Provided array is too small");
			foreach (var item in Source)
				array[arrayIndex++] = Adapt(item);
		}
		void ICollection.CopyTo(Array array, int index)
		{
			if (Source is ICollection list)
				list.CopyTo(array, index);
			else
				throw new NotSupportedException();
		}

		/// <summary>The index of an element</summary>
		public int IndexOf(TIn item)
		{
			return Source.IndexOf(item);
		}
		public int IndexOf(TOut item)
		{
			if (Conform != null)
				return Source.IndexOf(Conform(item));

			var index = 0;
			for (; index != Count && !Equals(item, Adapt(Source[index])); ++index) {}
			return index != Count ? index : -1;
		}
		int IList.IndexOf(object? value)
		{
			return IndexOf((TOut)value!);
		}

		/// <summary></summary>
		public void Insert(int index, TOut item)
		{
			if (IsReadOnly || Conform == null)
				throw new InvalidOperationException("Collection is read-only");
			Source.Insert(index, Conform(item));
		}
		void IList.Insert(int index, object? value)
		{
			Insert(index, (TOut)value!);
		}

		/// <summary></summary>
		public bool Remove(TIn item)
		{
			if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
			var index = IndexOf(item);
			if (index != -1) RemoveAt(index);
			return index != -1;
		}
		public bool Remove(TOut item)
		{
			if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
			var index = IndexOf(item);
			if (index != -1) RemoveAt(index);
			return index != -1;
		}
		void IList.Remove(object? value)
		{
			Remove((TOut)value!);
		}

		/// <summary></summary>
		public void RemoveAt(int index)
		{
			if (IsReadOnly) throw new InvalidOperationException("Collection is read-only");
			Source.RemoveAt(index);
		}

		/// <summary></summary>
		public IEnumerator<TOut> GetEnumerator()
		{
			foreach (var item in Source)
				yield return Adapt(item);
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>True if the underlying list supports collection changed notification</summary>
		public bool SupportsCollectionChanged => Source is INotifyCollectionChanged;

		/// <summary>If the source list supports collection changed notification, allows forwarding of the event</summary>
		public event NotifyCollectionChangedEventHandler CollectionChanged
		{
			add
			{
				if (m_collection_changed == null && Source is INotifyCollectionChanged ncc) ncc.CollectionChanged += HandleCollectionChanged;
				m_collection_changed += value;
			}
			remove
			{
				m_collection_changed -= value;
				if (m_collection_changed == null && Source is INotifyCollectionChanged ncc) ncc.CollectionChanged -= HandleCollectionChanged;
			}
		}
		private event NotifyCollectionChangedEventHandler? m_collection_changed;
		private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (m_collection_changed == null)
				return;

			// Map the changed objects to adapted changed objects
			var args = (NotifyCollectionChangedEventArgs?)null;
			switch (e.Action)
			{
			case NotifyCollectionChangedAction.Reset:
				{
					args = new NotifyCollectionChangedEventArgs(e.Action);
					break;
				}
			case NotifyCollectionChangedAction.Add:
				{
					var nue = e.NewItems.Cast<TIn>().Select(x => Adapt(x)).ToList();
					args = new NotifyCollectionChangedEventArgs(e.Action, nue, e.NewStartingIndex);
					break;
				}
			case NotifyCollectionChangedAction.Remove:
				{
					var old = e.OldItems.Cast<TIn>().Select(x => Adapt(x)).ToList();
					args = new NotifyCollectionChangedEventArgs(e.Action, old, e.OldStartingIndex);
					break;
				}
			case NotifyCollectionChangedAction.Replace:
				{
					var nue = e.NewItems.Cast<TIn>().Select(x => Adapt(x)).ToList();
					var old = e.OldItems.Cast<TIn>().Select(x => Adapt(x)).ToList();
					args = new NotifyCollectionChangedEventArgs(e.Action, nue, old, e.NewStartingIndex);
					break;
				}
			case NotifyCollectionChangedAction.Move:
				{
					var nue = e.NewItems.Cast<TIn>().Select(x => Adapt(x)).ToList();
					args = new NotifyCollectionChangedEventArgs(e.Action, nue, e.NewStartingIndex, e.OldStartingIndex);
					break;
				}
			}
			if (args != null)
				m_collection_changed.Invoke(this, args);
		}
	}

	public static class ListAdapter
	{
		/// <summary>Helper for inferring type parameters</summary>
		public static ListAdapter<TIn, TOut> Create<TIn, TOut>(IList<TIn> list, Func<TIn, TOut> adapt, Func<TOut, TIn>? conform = null)
		{
			return new ListAdapter<TIn, TOut>(list, adapt, conform);
		}
	}
}
