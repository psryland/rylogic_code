//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using pr.util;
using pr.extn;

namespace pr.extn
{
	/// <summary>Extensions for Lists</summary>
	public static class ListExtensions
	{
		/// <summary>Return true if the list is empty</summary>
		public static bool Empty<T>(this IList<T> list)
		{
			return list.Count == 0;
		}

		/// <summary>Return the first element in the list</summary>
		public static T Front<T>(this IList<T> list)
		{
			Debug.Assert(list.Count != 0);
			return list[0];
		}

		/// <summary>Return the list element in the list</summary>
		public static T Back<T>(this IList<T> list)
		{
			Debug.Assert(list.Count != 0);
			return list[list.Count - 1];
		}

		/// <summary>Resize a list default constructing objects to fill</summary>
		public static void Resize<T>(this List<T> list, int newsize) where T:new()
		{
			list.Resize(newsize,() => new T());
		}
		public static void Resize<T>(this List<T> list, int newsize, Func<T> factory)
		{
			if      (list.Count > newsize) list.RemoveRange(newsize, list.Count - newsize);
			else if (list.Count < newsize) for (int i = list.Count; i != newsize; ++i) list.Add(factory());
		}

		/// <summary>
		/// Add 'item' to the list if it's not already there.
		/// Uses 'are_equal(list[i],item)' to test for uniqueness.
		/// Returns true if 'item' was added, false if it was a duplicate</summary>
		public static bool AddIfUnique<T>(this IList<T> list, T item, Func<T,T,bool> are_equal)
		{
			foreach (var i in list) if (are_equal(i,item)) return false;
			list.Add(item);
			return true;
		}
		
		/// <summary>
		/// Add 'item' to the list if it's not already there.
		/// Uses 'list[i].Equals((item)' to test for uniqueness.
		/// Returns true if 'item' was added, false if it was a duplicate</summary>
		public static bool AddIfUnique<T>(this IList<T> list, T item)
		{
			foreach (var i in list) if (i.Equals(item)) return false;
			list.Add(item);
			return true;
		}

		/// <summary>Swap elements in the list</summary>
		public static void Swap<T>(this IList<T> list, int index0, int index1)
		{
			if (index0 == index1) return;
			T tmp = list[index0];
			list[index0] = list[index1];
			list[index1] = tmp;
		}
		
		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true</summary>
		public static int IndexOf<T>(this System.Collections.IList list, Func<T,bool> pred, int start_index, int count)
		{
			int i; for (i = start_index; i != count && !pred((T)list[i]); ++i) {}
			return i != count ? i : -1;
		}
		
		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true</summary>
		public static int IndexOf<T>(this System.Collections.IList list, Func<T,bool> pred)
		{
			return list.IndexOf(pred, 0, list.Count);
		}
		
		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred, int start_index, int count)
		{
			int i; for (i = start_index; i != count && !pred(list[i]); ++i) {}
			return i != count ? i : -1;
		}
		
		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred, int start_index)
		{
			return list.IndexOf(pred, start_index, list.Count);
		}
		
		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred)
		{
			return list.IndexOf(pred, 0, list.Count);
		}
		
		/// <summary>
		/// Remove items from a list by predicate.
		/// Returns the number of elements removed.</summary>
		public static int RemoveIf<T>(this List<T> list, Func<T,bool> pred, bool stable)
		{
			// If stable remove is needed, use the IList version
			if (stable)
				return ((IList<T>)list).RemoveIf(pred);
			
			int end = list.Count;
			for (int i = list.Count; i-- != 0;)
			{
				if (!pred(list[i])) continue;
				list.Swap(i, --end);
			}
			int count = list.Count - end;
			list.RemoveRange(end, count);
			return count;
		}
		
		/// <summary>
		/// Remove items from a list by predicate.
		/// Note generic list has an unstable faster version of this.
		/// Returns the number of elements removed.</summary>
		public static int RemoveIf<T>(this IList<T> list, Func<T,bool> pred)
		{
			int count = 0;
			for (int i = list.Count; i-- != 0;)
			{
				if (!pred(list[i])) continue;
				list.RemoveAt(i);
				++count;
			}
			return count;
		}
		public static int RemoveIf<T>(this System.Collections.IList list, Func<T,bool> pred)
		{
			int count = 0;
			for (int i = list.Count; i-- != 0;)
			{
				if (!pred((T)list[i])) continue;
				list.RemoveAt(i);
				++count;
			}
			return count;
		}

		/// <summary>Binary search using for an element using only a predicate function.
		/// Returns the index of the element if found or the 2s-complement of the first
		/// element larger than the one searched for.
		/// 'cmp' should return -1 if T is less than the target, +1 if greater, or 0 if equal</summary>
		public static int BinarySearch<T>(this IList<T> list, Func<T,int> cmp)
		{
			if (list.Count == 0) return ~0;
			for (int b = 0, e = list.Count;;)
			{
				int m = b + ((e - b) >> 1); // prevent overflow
				int c = cmp(list[m]);       // <0 means list[m] is less than the target element
				if (c == 0) { return m; }
				if (c <  0) { if (m == b){return ~e;} b = m; continue; }
				if (c >  0) { if (m == b){return ~b;} e = m; continue; }
			}
		}

		/// <summary>Sub range sort using a delegate</summary>
		public static void Sort<T>(this List<T> list, int start, int count, Comparison<T> cmp)
		{
			list.Sort(start, count, Cmp<T>.From(cmp));
		}

		/// <summary>Remove adjascent duplicate elements within the range [begin, end).
		/// Returns the end of the unique range (i.e. a value in the range [begin,end]</summary>
		public static int Unique<T>(this IList<T> list, int begin, int end, Func<T,T,bool> equal)
		{
			Debug.Assert(begin <= end && end <= list.Count);
			if (list.Count <= 1) return list.Count;
			int w,r,range_end;

			// Find the first equal adjascent elements
			for (r = begin + 1; r < end && !equal(list[r-1], list[r]); ++r) {}

			// back copy elements overwriting duplicates
			for (w = r - 1; r < end;)
			{
				for (++r; r != end && equal(list[w], list[r]); ++r) {}
				if (r == end) break;
				list[++w] = list[r];
			}

			range_end = w+1;

			// Copy any remaining elements
			for (; r != list.Count; ++r)
				list[++w] = list[r];
			
			// Resize the container
			for (++w; w != list.Count;)
				list.RemoveAt(list.Count - 1);

			return range_end;
		}
		public static int Unique<T>(this IList<T> list, Func<T,T,bool> equal)
		{
			return list.Unique(0, list.Count, equal);
		}
		public static int Unique<T>(this IList<T> list, int begin, int end)
		{
			return list.Unique(begin, end, (lhs,rhs) => lhs.Equals(rhs));
		}
		public static int Unique<T>(this IList<T> list)
		{
			return list.Unique(0, list.Count, (lhs,rhs) => lhs.Equals(rhs));
		}

		public static int Partition<T>(this IList<T> list, Comparison<T> comparison, int left, int right)
		{
			int i = left;
			int j = right;

			// pick the pivot point and save it
			T pivot = list[left];

			// until the indices cross
			while (i < j)
			{
				// move the right pointer left until value < pivot
				while (comparison(list[j], pivot) > 0 && i < j) j--;

				// move the right value to the left position
				// increment left pointer
				if (i != j) list[i++] = list[j];

				// move the left pointer to the right until value > pivot
				while (comparison(list[i], pivot) < 0 && i < j) i++;

				// move the left value to the right position
				// decrement right pointer
				if (i != j) list[j--] = list[i];
			}

			// put the pivot holder in the left spot
			list[i] = pivot;

			// return pivot location
			return i;
		}
		public static int Partition<T>(this IList<T> list, Comparison<T> comparison)
		{
			return list.Partition(comparison, 0, list.Count - 1);
		}
		public static int Partition<T>(this IList<T> list, IComparer<T> comparer)
		{
			return list.Partition(new Comparison<T>((x,y) => comparer.Compare(x, y)));
		}
		public static int Partition<T>(this IList<T> list) where T : IComparable<T>
		{
			return list.Partition(new Comparison<T>((x, y) => x.CompareTo(y)));
		}
		
		/// <summary>Sort the list using the quick sort algorithm</summary>
		public static void QuickSort<T>(this IList<T> list, Comparison<T> comparison, int left, int right)
		{
			// pivot and get pivot location
			int pivot = list.Partition(comparison, left, right);

			// if the left index is less than the pivot, sort left side
			if (left < pivot) list.QuickSort(comparison, left, pivot - 1);

			// if right index is greated than pivot, sort right side
			if (right > pivot) list.QuickSort(comparison, pivot + 1, right);
		}
	
		/// <summary>Return the k'th element in the list as if the list was sorted</summary>
		public static T QuickSelect<T>(this IList<T> list, int k, Comparison<T> comparison, int left, int right)
		{
			// get pivot position
			int pivot = list.Partition(comparison, left, right);

			// if pivot is less that k, select from the right part
			if (pivot < k) return list.QuickSelect(k, comparison, pivot + 1, right);

			// if pivot is greater than k, select from the left side
			if (pivot > k) return list.QuickSelect(k, comparison, left, pivot - 1);

			// if equal, return the value
			return list[pivot];
		}
		public static T QuickSelect<T>(this IList<T> list, int k, Comparison<T> comparison)
		{
			return list.QuickSelect(k, comparison, 0, list.Count - 1);
		}
		public static T QuickSelect<T>(this IList<T> list, int k, IComparer<T> comparer)
		{
			return list.QuickSelect(k, new Comparison<T>((x, y) => comparer.Compare(x, y)));
		}
		public static T QuickSelect<T>(this IList<T> list, int k) where T : IComparable<T>
		{
			return list.QuickSelect(k, new Comparison<T>((x, y) => x.CompareTo(y)));
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using pr.maths;

	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestListExtensions()
		{
			Random rng = new Random();
			List<int> list = new List<int>(100);

			for (int i = 0; i != 100; ++i)
				list.Add(rng.Next(10));
			
			list.Sort(0, list.Count, (lhs,rhs)=>{return Maths.Compare(lhs,rhs);});

			int last = list.Unique(0, 50);
			for (int i = 0; i < last; ++i)
			for (int j = i+1; j < last; ++j)
				Assert.AreNotEqual(list[i], list[j]);

			list.Unique();
			for (int i = 0; i < list.Count; ++i)
			for (int j = i+1; j < list.Count; ++j)
				Assert.AreNotEqual(list[i], list[j]);
		}
	}
}
#endif
