using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pr.container
{
	public enum ListChg
	{
		/// <summary>
		/// Raised before a single item or the entire list is reset.
		/// If Index == -1, the entire list is reseting.</summary>
		PreReset,
		
		/// <summary>
		/// Raised after a single item or the entire list is reset.
		/// If Index == -1, the entire list was reset.</summary>
		Reset,

		/// <summary>Raised just before an item is added to the list.</summary>
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
	}
}
