using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using pr.common;
using pr.util;

namespace pr.container
{
	public enum ListChg
	{
		/// <summary>Raised before the entire list is reset</summary>
		PreReset = Pre | Reset,
		
		/// <summary>Raised after the entire list is reset, or reordered (sorted)</summary>
		Reset = 1,

		/// <summary>Raised before a single item is reset</summary>
		ItemPreReset = Pre | ItemReset,

		/// <summary>Raised after a single item is reset</summary>
		ItemReset = 2,

		/// <summary>Raised just before an item is added to the list</summary>
		ItemPreAdd = Pre | ItemAdded,

		/// <summary>Raised just after an item is added to the list</summary>
		ItemAdded = 3,

		/// <summary>Raised just before an item is removed from the list</summary>
		ItemPreRemove = Pre | ItemRemoved,

		/// <summary>Raised just after an item is removed from the list</summary>
		ItemRemoved = 4,

		/// <summary>Flags the change type as 'pre' the change</summary>
		Pre = 0x8000,
	}

	/// <summary>Supports the ListChanging event</summary>
	public interface IListChanging<T>
	{
		/// <summary>Gets/Sets a value indicating whether adding or removing items within the collection raises ListChanged events.</summary>
		bool RaiseListChangedEvents { get; set; }

		/// <summary>Raised whenever items are added or about to be removed from a collection</summary>
		event EventHandler<ListChgEventArgs<T>> ListChanging;
	}
	public interface IItemChanged<T>
	{
		/// <summary>Raised whenever an element in the collection is changed</summary>
		event EventHandler<ItemChgEventArgs<T>> ItemChanged;
	}

	/// <summary>Support for ListChanging event on untyped collections</summary>
	public interface IListChanging
	{
		/// <summary>Gets/Sets a value indicating whether adding or removing items within the collection raises ListChanged events.</summary>
		bool RaiseListChangedEvents { get; set; }

		/// <summary>Raised whenever items are added or about to be removed from a collection</summary>
		event EventHandler<ListChgEventArgs> ListChanging;
	}
	public interface IItemChanged
	{
		/// <summary>Raised whenever an element in the collection is changed</summary>
		event EventHandler<ItemChgEventArgs> ItemChanged;
	}

	/// <summary>Supports notification of batched changes</summary>
	public interface IBatchChanges
	{
		/// <summary>Raised before and after a batch of changes</summary>
		event EventHandler<PrePostEventArgs> BatchChanges;

		/// <summary>Create a new batch of changes</summary>
		Scope BatchChange();
	}

	/// <summary>Args for the event raised whenever the list is changed</summary>
	public class ListChgEventArgs :EventArgs
	{
		[DebuggerStepThrough] public ListChgEventArgs(IList list)
			:this(list, ListChg.Reset, -1, null)
		{}
		[DebuggerStepThrough] public ListChgEventArgs(IList list, ListChg chg, int index, object item)
		{
			List       = list;
			ChangeType = chg;
			Index      = index;
			Item       = item;
			Cancel     = false;
		}

		/// <summary>The list this event is associated with</summary>
		public IList List { get; private set; }

		/// <summary>The change this event represents</summary>
		public ListChg ChangeType { get; private set; }

		/// <summary>
		/// The index of the item in the list, or where it will be in the list.
		/// Writeable to allow PreAdd to change the index, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public int Index { get; set; }

		/// <summary>
		/// The item added/removed.
		/// Writeable to allow PreAdd to change the item, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public object Item { get; set; }

		/// <summary>On 'Pre' events, can be used to prevent the change</summary>
		public bool Cancel { get; set; }

		/// <summary>True if this is the event before the list change</summary>
		public bool IsPreEvent
		{
			get { return ChangeType.HasFlag(ListChg.Pre); }
		}

		/// <summary>True if this is the event after the list change</summary>
		public bool IsPostEvent
		{
			get { return !ChangeType.HasFlag(ListChg.Pre); }
		}

		/// <summary>True if data in the collection has possibly changed, not just the collection order</summary>
		public bool IsDataChanged
		{
			get { return IsPostEvent; }
		}
	}

	/// <summary>Args for the event raised whenever the list is changed</summary>
	public class ListChgEventArgs<T> :EventArgs
	{
		[DebuggerStepThrough] public ListChgEventArgs(IList<T> list)
			:this(list, ListChg.Reset, -1, default(T))
		{}
		[DebuggerStepThrough] public ListChgEventArgs(IList<T> list, ListChg chg, int index, T item)
		{
			List       = list;
			ChangeType = chg;
			Index      = index;
			Item       = item;
			Cancel     = false;
		}

		/// <summary>The list this event is associated with</summary>
		public IList<T> List { get; private set; }

		/// <summary>The change this event represents</summary>
		public ListChg ChangeType { get; private set; }

		/// <summary>
		/// The index of the item in the list, or where it will be in the list.
		/// Writeable to allow PreAdd to change the index, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public int Index { get; set; }

		/// <summary>
		/// The item added/removed.
		/// Writeable to allow PreAdd to change the item, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public T Item { get; set; }

		/// <summary>On 'Pre' events, can be used to prevent the change</summary>
		public bool Cancel { get; set; }

		/// <summary>True if this is the event before the list change</summary>
		public bool IsPreEvent
		{
			get { return ChangeType.HasFlag(ListChg.Pre); }
		}

		/// <summary>True if this is the event after the list change</summary>
		public bool IsPostEvent
		{
			get { return !ChangeType.HasFlag(ListChg.Pre); }
		}

		/// <summary>True if data in the collection has possibly changed, not just the collection order</summary>
		public bool IsDataChanged
		{
			get { return IsPostEvent; }
		}
	}

	/// <summary>Event args for the event raised whenever an item in the list is changed</summary>
	public class ItemChgEventArgs :EventArgs
	{
		public ItemChgEventArgs(int index, object old_item, object new_item)
		{
			Index = index;
			OldItem = old_item;
			NewItem = new_item;
		}
		/// <summary>Index position of the item that was changed</summary>
		public int Index { get; private set; }

		/// <summary>The item before it was changed</summary>
		public object OldItem { get; private set; }

		/// <summary>The new item now in position 'Index' in the list</summary>
		public object NewItem { get; private set; }
	}

	/// <summary>Event args for the event raised whenever an item in the list is changed</summary>
	public class ItemChgEventArgs<T> :EventArgs
	{
		public ItemChgEventArgs(int index, T old_item, T new_item)
		{
			Index = index;
			OldItem = old_item;
			NewItem = new_item;
		}
		/// <summary>Index position of the item that was changed</summary>
		public int Index { get; private set; }

		/// <summary>The item before it was changed</summary>
		public T OldItem { get; private set; }

		/// <summary>The new item now in position 'Index' in the list</summary>
		public T NewItem { get; private set; }
	}

	/// <summary>List index position change event</summary>
	public class PositionChgEventArgs :EventArgs
	{
		public PositionChgEventArgs(int old_index, int new_index)
		{
			OldIndex = old_index;
			NewIndex = new_index;
		}

		/// <summary>The previous index (-1 means no previous index)</summary>
		public int OldIndex { get; private set; }
		
		/// <summary>The new current index (-1 means no current index)</summary>
		public int NewIndex { get; private set; }
	}
}
