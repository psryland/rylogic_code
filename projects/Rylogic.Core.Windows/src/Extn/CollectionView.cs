using System.ComponentModel;

namespace Rylogic.Extn
{
	public static class CollectionView_
	{
		public static bool MoveCurrentToOrFirst(this ICollectionView collection, object item)
		{
			return item != null
				? collection.MoveCurrentTo(item)
				: collection.MoveCurrentToFirst();
		}
	}
}
