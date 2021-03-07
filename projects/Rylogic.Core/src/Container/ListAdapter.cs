using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Rylogic.Common;

namespace Rylogic.Container
{
	public class ListAdapter<In, Out> :IList<Out>, IList, IDisposable, INotifyCollectionChanged
		where In : notnull
		where Out : notnull
	{
		// Notes:
		//  - Used to make a list of type 'In' appear like a list of 'Out'
		//  - Intended for Binding where 'Out' is a wrapper of 'In'
		//  - 'In' list can be modified if it is dynamically castable to IList<In>
		//  - 'Update' is required to add/insert items into the list.
		//  - Only need to call Dispose if 'Out' is disposable
		//  - ObservableCollection can cause problems because it notifies during 'Refresh'
		//    This is why I'm using List<Pair> for the Out instances and implementing INotifyCollectionChanged manually.
		//  - Ownership of 'Out' instances is handled by this class. If 'Out' is disposable it will be automatically called.

		public ListAdapter(IReadOnlyList<In> list, Func<In, Out> factory, Func<Out, In>? update = null)
		{
			List = new List<Pair>();
			Factory = factory;
			Update = update;
			Source = list;
			Refresh();
		}
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || !typeof(IDisposable).IsAssignableFrom(typeof(Out)))
				return;
			foreach (var x in List)
				((IDisposable)x.Dst).Dispose();
			List.Clear();
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		~ListAdapter()
		{
			Dispose(false);
		}

		/// <summary>Factory method for creating 'Out's from 'In's</summary>
		private Func<In, Out> Factory { get; }

		/// <summary>Factory method for creating an 'In' from an 'Out'</summary>
		private Func<Out, In>? Update { get; }

		/// <summary>Notification of when this collection is modified</summary>
		public event NotifyCollectionChangedEventHandler? CollectionChanged;

		/// <summary>The source list</summary>
		public IReadOnlyList<In> Source
		{
			get => m_source;
			private set
			{
				if (m_source == value) return;
				m_source = value;

				if (m_source is INotifyCollectionChanged observable)
				{
					observable.CollectionChanged += WeakRef.MakeWeak(HandleSourceCollectionChanged, h => observable.CollectionChanged -= h);
					void HandleSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => Refresh();
				}
			}
		}
		private IReadOnlyList<In> m_source = null!;

		/// <summary>The source collection as an IList (if possible)</summary>
		public IList<In>? EditableSource => Source as IList<In>;

		/// <summary>The collection that holds the 'Out' instances</summary>
		private List<Pair> List { get; }

		/// <summary>Synchronise this list with the source</summary>
		public void Refresh()
		{
			// Find where 'Source' and 'List' are no longer in sync
			int i;
			for (i = 0; i != List.Count; ++i)
			{
				// Not the end of 'Source' and still in sync?
				if (i != Source.Count && Equals(Source[i], List[i].Src))
					continue;

				// Move remaining items from 'List' to the recycler
				for (int j = List.Count; j-- != i;)
				{
					m_recycler.Add(List[j].Src, List[j].Dst);
					List.RemoveAt(j);
				}
				break;
			}

			// Fill 'List' to match 'Source', using objects from the recycler where possible
			var new_items = new List<Out>();
			for (; i != Source.Count; ++i)
			{
				// Look for a pair for 'src' in the recycler
				var src = Source[i];
				if (m_recycler.TryGetValue(src, out var dst))
				{
					// If found, remove it so that what's left is unused.
					m_recycler.Remove(src);
				}
				else
				{
					dst = Factory(src);
					new_items.Add(dst);
				}
				List.Add(new Pair { Src = src, Dst = dst });
			}

			// Notify Collection changed (now that List and Source are in sync)
			// ICollectionView doesn't support range actions, so need to notify once for each item
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

			// Call dispose on left-over 'Out's (if disposable)
			// Remember 'Out' could be object, so only some of the items might be disposable.
			foreach (var x in m_recycler.Values.OfType<IDisposable>())
				x.Dispose();

			// Drop any unused items
			m_recycler.Clear();
		}
		private Dictionary<In, Out> m_recycler = new Dictionary<In, Out>();

		/// <summary>Return the source object associated with 'x'</summary>
		public In Map(Out x) => List.FirstOrDefault(x => Equals(x.Dst, x)).Src;

		/// <summary>Return the destination object associated with 'x'</summary>
		public Out Map(In x) => List.FirstOrDefault(x => Equals(x.Src, x)).Dst;

		/// <inheritdoc/>
		public int Count => List.Count;

		/// <inheritdoc/>
		public bool IsReadOnly => !(EditableSource is IList<In> src) || src.IsReadOnly;
		bool IList.IsFixedSize => !(Source is IList src) || src.IsFixedSize;

		/// <inheritdoc/>
		public Out this[int index]
		{
			get => List[index].Dst;
			set
			{
				if (Update == null || !(EditableSource is IList<In> source))
					throw new NotSupportedException($"No update method has been provided for creating new source instances");

				// Add to 'List' first in case 'Source' is observable.
				// This means 'List' should already be synchronised when 'Source' is changed;
				var src = Update(value);
				List[index] = new Pair { Src = src, Dst = Factory(src) };
				source[index] = src;
			}
		}
		object? IList.this[int index]
		{
			get => this[index];
			set => this[index] = (Out?)value ?? throw new ArgumentNullException("This collection does not allow null values");
		}

		/// <inheritdoc/>
		public int IndexOf(Out item)
		{
			int i;
			for (i = 0; i != List.Count && !Equals(item, List[i].Dst); ++i) { }
			return i != List.Count ? i : -1;
		}
		int IList.IndexOf(object? value)
		{
			return IndexOf((Out?)value ?? throw new ArgumentNullException("This collection does not allow null values"));
		}

		/// <inheritdoc/>
		public void Add(Out item)
		{
			if (!(EditableSource is IList<In> source) || Update == null)
				throw new NotSupportedException($"No update method has been provided for creating new source instances");

			// Add to 'List' first in case 'Source' is observable.
			// This means 'List' should already be synchronised when 'Source' is changed;
			var src = Update(item);
			var pair = new Pair { Src = src, Dst = Factory(src) };
			List.Add(pair);
			source.Add(src);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, pair.Dst));
		}
		int IList.Add(object? value)
		{
			Add((Out?)value ?? throw new ArgumentNullException("This collection does not allow null values"));
			return Count - 1;
		}

		/// <inheritdoc/>
		public void Insert(int index, Out item)
		{
			if (!(EditableSource is IList<In> source) || Update == null)
				throw new NotSupportedException($"No update method has been provided for creating new source instances");


			// Add to 'List' first in case 'Source' is observable.
			// This means 'List' should already be synchronised when 'Source' is changed;
			var src = Update(item);
			var pair = new Pair { Src = src, Dst = Factory(src) };
			List.Insert(index, pair);
			source.Insert(index, src);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, pair.Dst, index));
		}
		void IList.Insert(int index, object? value)
		{
			Insert(index, (Out?)value ?? throw new ArgumentNullException("This collection does not allow null values"));
		}

		/// <inheritdoc/>
		public void RemoveAt(int index)
		{
			if (!(EditableSource is IList<In> source))
				throw new NotSupportedException($"Underlying list is not editable");

			var item = List[index];
			source.RemoveAt(index);
			List.RemoveAt(index);
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item.Dst, index));
			if (item.Dst is IDisposable disposable)
				disposable.Dispose();
		}

		/// <inheritdoc/>
		public bool Remove(Out item)
		{
			var index = IndexOf(item);
			if (index == -1) return false;
			RemoveAt(index);
			return true;
		}
		void IList.Remove(object? value)
		{
			Remove((Out?)value ?? throw new ArgumentNullException("This collection does not allow null values"));
		}

		/// <inheritdoc/>
		public void Clear()
		{
			// Even though 'Update' is not needed here, its absence
			// implies we shouldn't be modifying the source collection.
			if (Update == null || !(EditableSource is IList<In> source))
				throw new NotSupportedException($"No update method has been provided for modifying the source collection");

			source.Clear();
			List.Clear();
			CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <inheritdoc/>
		public bool Contains(Out item)
		{
			return List.Any(x => Equals(item, x.Dst));
		}
		bool IList.Contains(object? value)
		{
			return Contains((Out?)value ?? throw new ArgumentNullException("This collection does not allow null values"));
		}

		/// <inheritdoc/>
		public void CopyTo(Out[] array, int arrayIndex)
		{
			for (int i = 0; i != List.Count; ++i)
				array[arrayIndex + i] = List[i].Dst;
		}
		void ICollection.CopyTo(Array array, int index)
		{
			for (int i = 0; i != List.Count; ++i)
				array.SetValue(List[i].Dst, index + i);
		}

		/// <inheritdoc/>
		public IEnumerator<Out> GetEnumerator()
		{
			return List.Select(x => x.Dst).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <inheritdoc/>
		bool ICollection.IsSynchronized => ((IList)Source).IsSynchronized;

		/// <inheritdoc/>
		object ICollection.SyncRoot => ((IList)Source).SyncRoot;

		/// <summary></summary>
		private struct Pair
		{
			public In Src;
			public Out Dst;
		}
	}
	public static class ListAdapter
	{
		/// <summary>Helper for inferring type parameters</summary>
		public static ListAdapter<In, Out> Create<In, Out>(IReadOnlyList<In> list, Func<In, Out> factory, Func<Out, In>? update = null)
			where In : notnull
			where Out : notnull
		{
			return new ListAdapter<In, Out>(list, factory, update);
		}
	}

#if false // Old version
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
#endif
}
