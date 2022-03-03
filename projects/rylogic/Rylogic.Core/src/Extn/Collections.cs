using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Rylogic.Extn
{
	public static class Collections_
	{
		/// <summary>Return the new items</summary>
		public static IEnumerable<object> NewItems(this NotifyCollectionChangedEventArgs args)
		{
			return args.NewItems.Cast<object>() ?? Enumerable.Empty<object>();
		}
		public static IEnumerable<T> NewItems<T>(this NotifyCollectionChangedEventArgs args)
		{
			return args.NewItems.OfType<T>() ?? Enumerable.Empty<T>();
		}

		/// <summary>Return the old items</summary>
		public static IEnumerable<object> OldItems(this NotifyCollectionChangedEventArgs args)
		{
			return args.OldItems.Cast<object>() ?? Enumerable.Empty<object>();
		}
		public static IEnumerable<T> OldItems<T>(this NotifyCollectionChangedEventArgs args)
		{
			return args.OldItems.OfType<T>() ?? Enumerable.Empty<T>();
		}
	}
}