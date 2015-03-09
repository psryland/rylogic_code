using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace pr.container
{
	public enum ListChg
	{
		/// <summary>Raised before the entire list is reset</summary>
		PreReset,
		
		/// <summary>Raised after the entire list is reset</summary>
		Reset,

		/// <summary>Raised before a single item is reset</summary>
		ItemPreReset,

		/// <summary>Raised after a single item is reset</summary>
		ItemReset,

		/// <summary>Raised just before an item is added to the list</summary>
		ItemPreAdd,

		/// <summary>Raised just after an item is added to the list</summary>
		ItemAdded,

		/// <summary>Raised just before an item is removed from the list</summary>
		ItemPreRemove,

		/// <summary>Raised just after an item is removed from the list</summary>
		ItemRemoved,

		/// <summary>Raised just before the list is reordered</summary>
		PreReordered,

		/// <summary>Raised just after the list is reordered</summary>
		Reordered,

		/// <summary>Raised just before the list is cleared</summary>
		PreClear,

		/// <summary>Raised just after the list is cleared</summary>
		Clear,
	}

	public class ListChgEventArgs<T> :EventArgs
	{
		/// <summary>The change this event represents</summary>
		public ListChg ChangeType { get; private set; }

		/// <summary>The index of the item while in the list, or -1 if not in the list</summary>
		public int Index { get; private set; }

		/// <summary>The item added/remove</summary>
		public T Item { get; private set; }

		/// <summary>On 'Pre' events, can be used to prevent the change</summary>
		public bool Cancel { get; set; }

		[DebuggerStepThrough] public ListChgEventArgs(ListChg chg, int index, T item)
		{
			ChangeType = chg;
			Index      = index;
			Item       = item;
			Cancel     = false;
		}
	}

	public class ItemChgEventArgs<T> :EventArgs
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

	/// <summary>List index position change event</summary>
	public class PositionChgEventArgs :EventArgs
	{
		/// <summary>The previous index (-1 means no previous index)</summary>
		public int OldIndex { get; private set; }
		
		/// <summary>The new current index (-1 means no current index)</summary>
		public int NewIndex { get; private set; }

		public PositionChgEventArgs(int old_index, int new_index)
		{
			OldIndex = old_index;
			NewIndex = new_index;
		}
	}
}
