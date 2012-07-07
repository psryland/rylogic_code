using System.Collections.Generic;

namespace pr.extn
{
	public static class DictExtensions
	{
		/// <summary>Value compare of a dictionary without regard to order</summary>
		public static bool EqualUnordered<TKey, TValue>(this IDictionary<TKey, TValue> lhs, IDictionary<TKey, TValue> rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Count != rhs.Count) return false;

			var comparer = EqualityComparer<TValue>.Default;
			foreach (KeyValuePair<TKey, TValue> kvp in lhs)
			{
				TValue secondValue;
				if (!rhs.TryGetValue(kvp.Key, out secondValue)) return false;
				if (!comparer.Equals(kvp.Value, secondValue)) return false;
			}
			return true;
		}
	}
}
