using System.ComponentModel;

namespace Rylogic.Extn
{
	public static class CollectionView_
	{
		/// <summary>Move the current position to 'item' or the first item if 'item' is not found</summary>
		public static bool MoveCurrentToOrFirst(this ICollectionView collection, object item)
		{
			return item != null
				? collection.MoveCurrentTo(item)
				: collection.MoveCurrentToFirst();
		}

		/// <summary>Access the current item (or null)</summary>
		public static T CurrentAs<T>(this ICollectionView collection) where T:class
		{
			return (T)collection.CurrentItem;
		}
	}
}
