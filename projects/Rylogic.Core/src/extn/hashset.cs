using System.Collections.Generic;

namespace Rylogic.Extn
{
	public static class HashSet_
	{
		/// <summary>Add an item to the set and return the set for method chaining</summary>
		public static HashSet<T> Add2<T>(this HashSet<T> set, T item)
		{
			set.Add(item);
			return set;
		}

		/// <summary>Add a range of items to this set. Returns true if all added</summary>
		public static bool AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
		{
			var all = true;
			foreach (var item in items)
				all &= set.Add(item);

			return all;
		}
	}
}
