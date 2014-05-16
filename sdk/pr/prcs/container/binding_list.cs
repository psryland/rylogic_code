using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using pr.extn;
using pr.util;

namespace pr.container
{
	/// <summary>Extension to BindingList that notifies *before* and item is removed</summary>
	public class BindingListEx<T> :System.ComponentModel.BindingList<T>
	{
		public BindingListEx() :base()
		{
			Init();
		}
		public BindingListEx(IList<T> list) :base(list)
		{
			Init();
		}
		public BindingListEx(int initial_count, T value)
		{
			Init();
			for (;initial_count-- != 0;)
				Add(value);
		}
		private void Init()
		{
			// ResetBindings and ResetItem aren't overridable.
			// Attach handlers to ensure we always receive the Reset event.
			// Calling the 'new' method will cause the Pre events to be raised as well
			ListChanged += (s,a) =>
				{
					if (a.ListChangedType == ListChangedType.Reset)
						ListChanging.Raise(this, new ListChgEventArgs(ListChg.Reset, -1, default(T)));
					if (a.ListChangedType == ListChangedType.ItemChanged)
						ListChanging.Raise(this, new ListChgEventArgs(ListChg.Reset, a.NewIndex, this[a.NewIndex]));
				};
		}

		/// <summary>Raised whenever items are added or about to be removed from the list</summary>
		public event EventHandler<ListChgEventArgs> ListChanging;
		public class ListChgEventArgs :EventArgs
		{
			/// <summary>The change this event represents</summary>
			public ListChg ChangeType { get; private set; }

			/// <summary>The index of the item while in the list, or -1 if not in the list</summary>
			public int Index { get; private set; }

			/// <summary>The item added/remove</summary>
			public T Item { get; private set; }

			public ListChgEventArgs(ListChg chg, int index, T item)
			{
				ChangeType = chg;
				Index      = index;
				Item       = item;
			}
		}

		/// <summary>Raised whenever an element in the list is changed</summary>
		public event EventHandler<ItemChgEventArgs> ItemChanged;
		public class ItemChgEventArgs :EventArgs
		{
			/// <summary>Index position of the item that was changed</summary>
			public int Index { get; private set; }

			/// <summary>The item before it was changed</summary>
			public T OldItem { get; private set; }

			/// <summary>The new item now in position 'Index' in the list</summary>
			public T NewItem { get; private set; }

			public ItemChgEventArgs(int index, T old_item, T new_item)
			{
				Index = index;
				OldItem = old_item;
				NewItem = new_item;
			}
		}

		// Removes all items from the list
		protected override void ClearItems()
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.PreReset, -1, default(T)));

			// Reset event is raised from attached handler
			base.ClearItems();
		}

		// Inserts the specified item in the list at the specified index.
		protected override void InsertItem(int index, T item)
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemPreAdd, -1, item));

			base.InsertItem(index, item);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, item));
		}

		// Removes the item at the specified index.
		protected override void RemoveItem(int index)
		{
			var item = this[index];
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemPreRemove, index, item));
			
			base.RemoveItem(index);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemRemoved, -1, item));
		}
		
		// Replaces the item at the specified index with the specified item.
		protected override void SetItem(int index, T item)
		{
			var old = this[index];

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemPreRemove, index, old));
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemPreAdd, index, item));

			base.SetItem(index, item);

			if (RaiseListChangedEvents)
				ItemChanged.Raise(this, new ItemChgEventArgs(index, old, item));

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemRemoved, index, old));
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.ItemAdded, index, item));
		}

		// Reorders the list
		protected override void ApplySortCore(System.ComponentModel.PropertyDescriptor prop, System.ComponentModel.ListSortDirection direction)
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.PreReordered, -1, default(T)));
			
			base.ApplySortCore(prop, direction);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.Reordered, -1, default(T)));
		}

		// Notify observers of the entire list changing
		public new void ResetBindings()
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.PreReset, -1, default(T)));

			// Reset event is raised from attached handler
			base.ResetBindings();
		}
		
		// Notify observers of a specific item changing
		public new void ResetItem(int position)
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChgEventArgs(ListChg.PreReset, position, this[position]));

			// Reset event is raised from attached handler
			base.ResetItem(position);
		}
	
		/// <summary>RAII object for suspending list events</summary>
		public Scope SuspendEvents()
		{
			return Scope.Create(() => RaiseListChangedEvents = false, () => RaiseListChangedEvents = true);
		}
	}
}
