﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Container
{
	/// <summary>Extension to BindingList that notifies *before* an item is removed</summary>
	[DataContract] public class BindingListEx<T> :BindingList<T>, IEnumerable<T>, IListChanging<T>, IItemChanged<T>, IBatchChanges
	{
		public BindingListEx() :base()
		{
			Init();
		}
		
		/// <summary>Careful: this constructor doesn't copy 'list' it wraps it. So sort/readonly/etc depend on the given list</summary>
		public BindingListEx(IList<T> list) :base(list)
		{
			Init();
		}
		
		/// <summary>Construct from a range</summary>
		public BindingListEx(IEnumerable<T> collection) :base(collection.ToList())
		{
			Init();
		}
		
		/// <summary>Construct using a generator function</summary>
		public BindingListEx(int initial_count, Func<int,T> gen) :base(int_.Range(initial_count).Select(i => gen(i)).ToList())
		{
			Init();
		}

		/// <summary>Construct from a single value repeated 'initial_count' times</summary>
		public BindingListEx(int initial_count, T value = default(T)) :base(Enumerable.Repeat(value, initial_count).ToList())
		{
			Init();
		}

		/// <summary>Construction</summary>
		private void Init()
		{
			PerItem = false;
			AllowSort = true;
			IsSorted = false;
			SortDirection = ListSortDirection.Ascending;
			SortComparer = Comparer.DefaultInvariant;

			// ResetBindings and ResetItem aren't override-able.
			// Attach handlers to ensure we always receive the Reset event.
			// Calling the 'new' method will cause the Pre events to be raised as well
			ListChanged += InternalHandleListChanged;
		}

		/// <summary>Map ListChanged events to ListChanging events</summary>
		private void InternalHandleListChanged(object sender, ListChangedEventArgs a)
		{
			if (a.ListChangedType == ListChangedType.Reset)
			{
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.Reset, -1, default(T)));
			}
			if (a.ListChangedType == ListChangedType.ItemChanged)
			{
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.ItemReset, a.NewIndex, this[a.NewIndex]));
			}
		}

		/// <summary>Get/Set readonly for this list</summary>
		public bool ReadOnly
		{
			get { return !AllowNew && !AllowEdit && !AllowRemove; }
			set
			{
				// For some reason, setting these causes list changed events...
				using (SuspendEvents())
					AllowNew = AllowEdit = AllowRemove = !value;
			}
		}

		/// <summary>Get/Set whether sorting is allowed on this list</summary>
		public bool AllowSort { get; set; }

		/// <summary>Get/Set whether events for each item are generated for Clear, Reset, etc, or just PreReset/Reset</summary>
		public bool PerItem { get; set; }

		/// <summary>Raised whenever items are added or about to be removed from the list</summary>
		public event EventHandler<ListChgEventArgs<T>> ListChanging;

		/// <summary>Raised whenever an element in the list is replaced (i.e. by SetItem)</summary>
		[Obsolete("This is badly defined, stop using it")] public event EventHandler<ItemChgEventArgs<T>> ItemChanged;

		/// <summary>Removes all items from the list</summary>
		protected override void ClearItems()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.PreReset, -1, default(T));
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			// Remove items one at a time so that events are generated for each
			if (PerItem)
				for (;Count != 0;) RemoveAt(Count - 1);

			// Call ClearItems even if 'PerItem' is true so that the ListChg.Reset event is raised
			// Reset event is raised from ListChanged handler
			base.ClearItems();
			m_hash_set?.Clear();

			// Zero items, sorted..
			IsSorted = true;
		}

		/// <summary>Inserts the specified item in the list at the specified index.</summary>
		protected override void InsertItem(int index, T item)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.ItemPreAdd, index, item);
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;

				// Allow PreAdd to modify 'item' and 'index'
				item = args.Item;
				index = args.Index;
			}

			// Note: you can get first chance exceptions here when the list is bound to a BindingSource
			// that is bound to a combo box or list box. It happens when the list goes to/from empty
			// and is just shoddiness in the windows controls.
			base.InsertItem(index, item);
			m_hash_set?.Add(item);
			IsSorted = false;
	
			if (RaiseListChangedEvents)
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.ItemAdded, index, item));
		}

		/// <summary>Removes the item at the specified index.</summary>
		protected override void RemoveItem(int index)
		{
			var item = this[index];
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.ItemPreRemove, index, item);
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			base.RemoveItem(index);
			m_hash_set?.Remove(item);
			IsSorted = false;
	
			if (RaiseListChangedEvents)
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.ItemRemoved, -1, item));
		}
		
		/// <summary>Replaces the item at the specified index with the specified item.</summary>
		protected override void SetItem(int index, T item)
		{
			var old = this[index];

			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.ItemPreRemove, index, old);
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.ItemPreAdd, index, item);
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			base.SetItem(index, item);
			IsSorted = false;

			if (RaiseListChangedEvents)
				ItemChanged?.Invoke(this, new ItemChgEventArgs<T>(index, old, item));

			if (RaiseListChangedEvents)
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.ItemRemoved, index, old));
			if (RaiseListChangedEvents)
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.ItemAdded, index, item));
		}

		/// <summary>Optimised 'Contains'</summary>
		public new bool Contains(T item)
		{
			return m_hash_set?.Contains(item) ?? base.Contains(item);
		}
		public bool UseHashSet
		{
			get { return m_hash_set != null; }
			set { m_hash_set = value ? this.ToHashSet() : null; }
		}
		private HashSet<T> m_hash_set;

		/// <summary>Enumerable</summary>
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			foreach (var item in (IEnumerable)this)
				yield return (T)item;
		}

		#region Sorting

		/// <summary>True if the list supports sorting; otherwise, false. The default is false.</summary>
		public bool SupportsSorting
		{
			get { return AllowSort && SortComparer != null; }
		}
		protected override bool SupportsSortingCore
		{
			get { return SupportsSorting; }
		}

		/// <summary>The comparer used for sorting</summary>
		public IComparer SortComparer
		{
			get { return m_impl_cmp; }
			set
			{
				if (m_impl_cmp == value) return;
				m_impl_cmp = value;
			}
		}
		private IComparer m_impl_cmp;

		/// <summary>Get/Set the direction the list is sorted. The default is ListSortDirection.Ascending.</summary>
		public ListSortDirection SortDirection { get; set; }
		protected override ListSortDirection SortDirectionCore
		{
			get { return SortDirection; }
		}

		/// <summary>Get/Set the property descriptor that is used for sorting the list.</summary>
		public PropertyDescriptor SortProperty { get; set; }
		protected override PropertyDescriptor SortPropertyCore
		{
			get { return SortProperty; }
		}

		/// <summary>true if the list is sorted; otherwise, false. The default is false.</summary>
		public bool IsSorted { get; private set; }
		protected override bool IsSortedCore
		{
			get { return IsSorted; }
		}

		/// <summary>Reorders the list</summary>
		protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
		{
			// Don't allow sort on non-comparable properties
			if (!prop.PropertyType.Inherits(typeof(IComparable)))
				return;

			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.PreReset, -1, default(T));
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			using (SuspendEvents())
			{
				var sign = direction == ListSortDirection.Ascending ? +1 : -1;
				var cmp = Cmp<T>.From((lhs,rhs) =>
				{
					var l = prop.GetValue(lhs);
					var r = prop.GetValue(rhs);
					return sign * SortComparer.Compare(l,r);
				});
				this.QuickSort(cmp);

				SortDirection = direction;
				IsSorted = true;
			}

			// Sort should raise ListChanged Reset
			base.ResetBindings();

			if (RaiseListChangedEvents)
				ListChanging?.Invoke(this, new ListChgEventArgs<T>(this, ListChg.Reset, -1, default(T)));
		}

		/// <summary>Removes any sort applied with ApplySortCore()</summary>
		protected override void RemoveSortCore()
		{
			IsSorted = false;
		}

		#endregion

		/// <summary>Notify observers of the entire list changing</summary>
		public new void ResetBindings()
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.PreReset, -1, default(T));
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			// Reset event is raised from attached handler
			base.ResetBindings();
		}
		
		/// <summary>Notify observers of a specific item changing</summary>
		public new void ResetItem(int position)
		{
			if (RaiseListChangedEvents)
			{
				var args = new ListChgEventArgs<T>(this, ListChg.ItemPreReset, position, this[position]);
				ListChanging?.Invoke(this, args);
				if (args.Cancel)
					return;
			}

			// Reset event is raised from attached handler
			base.ResetItem(position);
		}
	
		/// <summary>Notify observers of a specific item changing</summary>
		public void ResetItem(T item, bool optional = false)
		{
			var idx = IndexOf(item);
			if (idx.Within(0, Count))
				ResetItem(idx);
			else if (!optional)
				throw new Exception("Item is not within this container");
		}

		/// <summary>
		/// RAII object for suspending list events
		/// WARNING: Be careful using this. If list events are used to attach/detach handlers,
		/// then suspending events will prevent that from happening! If you're trying to prevent
		/// lots of UI updates due to a big change to the collection, try SuspendLayout, or use Invalidate</summary>
		public Scope<bool> SuspendEvents(bool reset_bindings_on_resume = false)
		{
			// Returns Scope<bool> rather than Scope so that callers can change the state of 'raise'
			var initial_raise = RaiseListChangedEvents;
			return Scope.Create(
				() =>
				{
					RaiseListChangedEvents = false;
					return reset_bindings_on_resume;
				},
				raise =>
				{
					RaiseListChangedEvents = true;
					if (raise && initial_raise) ResetBindings();
				});
		}

		/// <summary>Raised when the caller signals a batch of changes are about to happen</summary>
		public event EventHandler<PrePostEventArgs> BatchChanges;
		public Scope BatchChange()
		{
			return Scope.Create(
				() => BatchChanges?.Invoke(this, new PrePostEventArgs(after:false)),
				() => BatchChanges?.Invoke(this, new PrePostEventArgs(after:true)));
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Linq;
	using Container;

	[TestFixture] public class TestBindingListEx
	{
		[Test] public void BindingList()
		{
			var a0 = new BindingListEx<double>(5, i => 2.0);
			var a1 = new BindingListEx<double>(5, i =>
				{
					return i + 1.0;
				});
			Assert.True(a0.SequenceEqual(new[]{2.0, 2.0, 2.0, 2.0, 2.0}));
			Assert.True(a1.SequenceEqual(new[]{1.0, 2.0, 3.0, 4.0, 5.0}));
		}
	}
}
#endif