using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Rylogic.Common;

namespace Rylogic.Container
{
	public class ListAdapter<In, Out> :IList<Out>, IReadOnlyList<Out>, IList, IDisposable, INotifyCollectionChanged
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

		public ListAdapter(IList<In> list, Func<In, Out> factory, Func<Out, In>? update = null)
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
			foreach (var x in List.OfType<IDisposable>())
				x.Dispose();
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
		public IList<In> Source
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
		private IList<In> m_source = null!;

		/// <summary>The source collection as an IList (if possible)</summary>
		public IList<In>? EditableSource => m_source as IList<In>;

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
		private Dictionary<In, Out> m_recycler = new();

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
			if (EditableSource is not IList<In> source || Update == null)
				throw new NotSupportedException($"No update method has been provided for creating new source instances");

			var src = Update(item);
			var pair = new Pair { Src = src, Dst = Factory(src) };

			// Add to 'List' first in case 'Source' is 'INotifyCollectionChanged'.
			// This means 'List' should already be synchronised when 'Source' is changed;
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
			if (EditableSource is not IList<In> source || Update == null)
				throw new NotSupportedException($"No update method has been provided for creating new source instances");

			var src = Update(item);
			var pair = new Pair { Src = src, Dst = Factory(src) };

			// Add to 'List' first in case 'Source' is 'INotifyCollectionChanged'.
			// This means 'List' should already be synchronised when 'Source' is changed;
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
			if (EditableSource is not IList<In> source)
				throw new NotSupportedException($"Underlying list is not editable");

			var item = List[index];

			// Remove from 'List' first in case 'Source' is 'INotifyCollectionChanged'.
			// This means 'List' should already be synchronised when 'Source' is changed;
			List.RemoveAt(index);
			source.RemoveAt(index);
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
			if (Update == null || EditableSource is not IList<In> source)
				throw new NotSupportedException($"No update method has been provided for modifying the source collection");

			// Clear 'List' first in case 'Source' is 'INotifyCollectionChanged'.
			// This means 'List' should already be synchronised when 'Source' is changed;
			List.Clear();
			source.Clear();
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
		public static ListAdapter<In, Out> Create<In, Out>(IList<In> list, Func<In, Out> factory, Func<Out, In>? update = null)
			where In : notnull
			where Out : notnull
		{
			return new ListAdapter<In, Out>(list, factory, update);
		}
	}
}
