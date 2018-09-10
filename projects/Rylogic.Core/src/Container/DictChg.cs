using System;
using System.Diagnostics;

namespace Rylogic.Container
{
	public enum DictChg
	{
		/// <summary>Raised before the entire collection is reset</summary>
		PreReset,
		
		/// <summary>Raised after the entire collection is reset</summary>
		Reset,

		/// <summary>Raised after a single item is reset</summary>
		ItemReset,

		/// <summary>Raised just before an item is added to the collection</summary>
		ItemPreAdd,

		/// <summary>Raised just after an item is added to the collection</summary>
		ItemAdded,

		/// <summary>Raised just before an item is removed from the collection</summary>
		ItemPreRemove,

		/// <summary>Raised just after an item is removed from the collection</summary>
		ItemRemoved,
	}

	/// <summary>Args for the event raised whenever the collection is changed</summary>
	public class DictChgEventArgs<TKey,TValue> :EventArgs
	{
		[DebuggerStepThrough]
		public DictChgEventArgs(DictChg chg, TKey key, TValue value)
		{
			ChangeType = chg;
			Key = key;
			Value = value;
			Cancel = false;
		}

		/// <summary>The change this event represents</summary>
		public DictChg ChangeType { get; private set; }

		/// <summary>
		/// The Key of the item in the collection, or where it will be in the collection.
		/// Write-able to allow PreAdd to change the key, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public TKey Key { get; set; }

		/// <summary>
		/// The item added/removed.
		/// Write-able to allow PreAdd to change the item, note however that when
		/// events are suspended PreAdd will not be called.</summary>
		public TValue Value { get; set; }

		/// <summary>On 'Pre' events, can be used to prevent the change</summary>
		public bool Cancel { get; set; }
	}
}
