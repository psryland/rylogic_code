using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using pr.extn;

namespace pr.container
{
	/// <summary>Extension to BindingList that notifies *before* and item is removed</summary>
	public class BindingListEx<T> :System.ComponentModel.BindingList<T>
	{
		public BindingListEx() :base() {}
		public BindingListEx(IList<T> list) :base(list) {}

		/// <summary>Raised whenever items are added or about to be removed from the list</summary>
		public event EventHandler<ListChangingEventArgs> ListChanging;
		public class ListChangingEventArgs :EventArgs
		{
			/// <summary>The change this event represents</summary>
			public ListChg ChangeType { get; private set; }

			/// <summary>The index of the item while in the list, or -1 if not in the list</summary>
			public int Index { get; private set; }

			/// <summary>The item added/remove</summary>
			public T Item { get; private set; }

			public ListChangingEventArgs(ListChg chg, int index, T item)
			{
				ChangeType = chg;
				Index      = index;
				Item       = item;
			}
		}

		protected override void ClearItems()
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.PreReset, -1, default(T)));

			base.ClearItems();

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.Reset, -1, default(T)));
		}

		// Inserts the specified item in the list at the specified index.
		protected override void InsertItem(int index, T item)
		{
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemPreAdd, -1, item));

			base.InsertItem(index, item);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemAdded, index, item));
		}

		// Removes the item at the specified index.
		protected override void RemoveItem(int index)
		{
			var item = this[index];
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemPreRemove, index, item));
			
			base.RemoveItem(index);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemRemoved, -1, item));
		}
		
		// Replaces the item at the specified index with the specified item.
		protected override void SetItem(int index, T item)
		{
			var old = this[index];
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemPreAdd, -1, item));
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemPreRemove, index, old));
		
			base.SetItem(index, item);

			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemRemoved, -1, old));
			if (RaiseListChangedEvents)
				ListChanging.Raise(this, new ListChangingEventArgs(ListChg.ItemAdded, index, item));
		}
	}
}
