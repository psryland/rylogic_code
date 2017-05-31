using System.Collections.Generic;

namespace pr.extn
{
	public static class HashSet_
	{
		/// <summary>Add an item to the set and return the set for method chaining</summary>
		public static HashSet<T> Add2<T>(this HashSet<T> set, T item)
		{
			set.Add(item);
			return set;
		}
	}
}
