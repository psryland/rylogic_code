//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.extn;

namespace pr.extn
{
	/// <summary>Extensions for Lists</summary>
	public static class List_
	{
		/// <summary>Return true if the list is empty</summary>
		public static bool Empty(this IList list)
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

		/// <summary>Return the list element at 'index'. i.e '0' is the first item, '-1' is off the beginning</summary>
		public static T Front<T>(this IList<T> list, int index)
		{
			Debug.Assert(list.Count != 0);
			return list[index];
		}

		/// <summary>Return the list element at 'list.Count - index - 1'. i.e '0' is the last item, '-1' is off the end</summary>
		public static T Back<T>(this IList<T> list, int index)
		{
			Debug.Assert(list.Count != 0);
			return list[list.Count - index - 1];
		}

		/// <summary>Return all of the range except the last 'count' items</summary>
		public static IEnumerable<TSource> SubRange<TSource>(this IList<TSource> list, int start, int count)
		{
			for (int i = start; count-- != 0; ++i)
				yield return list[i];
		}

		/// <summary>Return all of the range except the last 'count' items</summary>
		public static IEnumerable<TSource> TakeFrac<TSource>(this IList<TSource> source, float frac)
		{
			return source.Take((int)(source.Count * frac));
		}

		/// <summary>Remove the last item in the list</summary>
		public static void PopBack<T>(this IList<T> list)
		{
			System.Diagnostics.Debug.Assert(list.Count != 0, "Popback on non-empty container");
			list.RemoveAt(list.Count - 1);
		}

		/// <summary>Resize a list default constructing objects to fill</summary>
		public static void Resize<T>(this List<T> list, int newsize)
		{
			list.Resize(newsize, () => default(T));
		}
		public static void Resize<T>(this List<T> list, int newsize, Func<T> factory)
		{
			if      (list.Count > newsize) list.RemoveRange(newsize, list.Count - newsize);
			else if (list.Count < newsize) for (int i = list.Count; i != newsize; ++i) list.Add(factory());
		}

		/// <summary>True if 'index' >= 0 && 'index' < list.Count</summary>
		public static bool Within(this ICollection list, int index)
		{
			return index >= 0 && index < list.Count;
		}

		/// <summary>Add and return the item added to this list</summary>
		public static U Add2<T,U>(this IList<T> list, U item) where U:T
		{
			list.Add(item);
			return item;
		}

		/// <summary>Add a variable list of items</summary>
		public static void Add<T,U>(this IList<T> list, params U[] items) where U:T
		{
			list.AddRange(items.Cast<T>());
		}

		/// <summary>Insert and return the item added to this list</summary>
		public static U Insert2<T,U>(this IList<T> list, int index, U item) where U:T
		{
			list.Insert(index, item);
			return item;
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

		/// <summary>
		/// Add 'item' to the list if 'replace' does not match any elements.
		/// Returns true if 'item' was added, false if it replaced an element.</summary>
		public static bool AddOrReplace<T>(this IList<T> list, T item, Func<T,T,bool> replace, out T replaced)
		{
			for (int i = 0; i != list.Count; ++i)
			{
				if (!replace(list[i], item)) continue;
				replaced = list[i];
				list[i] = item;
				return false;
			}
			replaced = default(T);
			list.Add(item);
			return true;
		}
		public static bool AddOrReplace<T>(this IList<T> list, T item, Func<T,T,bool> replace)
		{
			T replaced;
			return AddOrReplace(list, item, replace, out replaced);
		}

		/// <summary>Add a range of elements to the list</summary>
		public static IList<T> AddRange<T>(this IList<T> list, IEnumerable<T> items)
		{
			foreach (var i in items)
				list.Add(i);
			return list;
		}

		/// <summary>Reset the list with the given items. Equivalent to Clear() followed by AddRange()</summary>
		public static IList<T> Assign<T>(this IList<T> list, IEnumerable<T> items)
		{
			list.Clear();
			return list.AddRange(items);
		}

		/// <summary>Swap elements in the list</summary>
		public static void Swap(this IList list, int index0, int index1)
		{
			if (index0 == index1) return;
			var tmp = list[index0];
			list[index0] = list[index1];
			list[index1] = tmp;
		}
		//public static void Swap<T>(this IList<T> list, int index0, int index1)
		//{
		//	((IList)list).Swap(index0, index1);
		//}
		//public static void Swap<T>(this List<T> list, int index0, int index1)
		//{
		//	((IList)list).Swap(index0, index1);
		//}

		/// <summary>Reverse the order of all elements in this list</summary>
		public static IList Reverse(this IList list)
		{
			list.Reverse(0, list.Count);
			return list;
		}
		public static BindingList<T> Reverse<T>(this BindingList<T> list)
		{
			return list.As<IList>().Reverse().As<BindingList<T>>();
		}

		/// <summary>Reverses the order of the elements in the specified range.</summary>
		public static IList Reverse(this IList list, int index, int count)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", "index must be >= 0");
			if (count < 0) throw new ArgumentOutOfRangeException("count", "count must be >= 0");
			if (list.Count - index < count) throw new ArgumentException("count exceeds the size of the list");

			var index1 = index;
			var index2 = index + count - 1;
			for (; index1 < index2; ++index1, --index2)
			{
				var tmp = list[index1];
				list[index1] = list[index2];
				list[index2] = tmp;
			}
			return list;
		}
		public static BindingList<T> Reverse<T>(this BindingList<T> list, int index, int count)
		{
			return list.As<IList>().Reverse(index, count).As<BindingList<T>>();
		}

		/// <summary>Replaces 'replacee' with 'replacer' in this list. Throws if 'replacee' can't be found.</summary>
		public static void Replace<T>(this IList<T> list, T replacee, T replacer)
		{
			if (ReferenceEquals(replacee, replacer)) return;
			var idx = list.IndexOf(replacee);
			list[idx] = replacer;
		}

		///// <summary>Replaces 'replacee' with 'replacer' in this list. Throws if 'replacee' can't be found.</summary>
		//public static void Replace<T>(this IList<T> list, T replacee, T replacer)
		//{
		//	if (ReferenceEquals(replacee, replacer)) return;
		//	var idx = list.IndexOf(replacee);
		//	list[idx] = replacer;
		//}

		///// <summary>Return the index of the occurrence of an element that causes 'pred' to return true (or -1)</summary>
		//public static int IndexOf<T>(this IList list, Func<T,bool> pred, int start_index, int count)
		//{
		//	int i; for (i = start_index; i != count && !pred((T)list[i]); ++i) {}
		//	return i != count ? i : -1;
		//}

		///// <summary>Return the index of the occurrence of an element that causes 'pred' to return true (or -1)</summary>
		//public static int IndexOf<T>(this IList list, Func<T,bool> pred)
		//{
		//	return list.IndexOf(pred, 0, list.Count);
		//}

		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true (or -1)</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred, int start_index, int count)
		{
			int i; for (i = start_index; i != count && !pred(list[i]); ++i) {}
			return i != count ? i : -1;
		}

		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true (or -1)</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred, int start_index)
		{
			return list.IndexOf(pred, start_index, list.Count);
		}

		/// <summary>Return the index of the occurrence of an element that causes 'pred' to return true (or -1)</summary>
		public static int IndexOf<T>(this IList<T> list, Func<T,bool> pred)
		{
			return list.IndexOf(pred, 0, list.Count);
		}

		/// <summary>
		/// Remove items from a list by predicate.
		/// Returns the number of elements removed.</summary>
		public static int RemoveIf<T>(this List<T> list, Func<T,bool> pred, bool stable)
		{
			return RemoveIf(list, pred, stable, 0, list.Count);
		}
		public static int RemoveIf<T>(this List<T> list, Func<T,bool> pred, bool stable, int start, int count)
		{
			if (start < 0) throw new ArgumentOutOfRangeException("start","Start index {0} out of range [0,{1})".Fmt(start, list.Count));
			if (start + count > list.Count) throw new ArgumentOutOfRangeException("count","Item count {0} out of range [0,{1}]".Fmt(count, list.Count));

			// If stable remove is needed, use the IList version
			if (stable)
				return ((IList<T>)list).RemoveIf(pred, start, count);

			int end = list.Count;
			for (int i = start + count; i-- != start;)
			{
				if (!pred(list[i])) continue;
				list.Swap(i, --end);
			}
			int removed = list.Count - end;
			list.RemoveRange(end, removed);
			return removed;
		}

		/// <summary>
		/// Remove items from a list by predicate.
		/// Note generic list has an unstable faster version of this.
		/// Returns the number of elements removed.</summary>
		public static int RemoveIf<T>(this IList<T> list, Func<T,bool> pred)
		{
			return RemoveIf(list, pred, 0, list.Count);
		}
		public static int RemoveIf<T>(this IList list, Func<T,bool> pred)
		{
			return RemoveIf(list, pred, 0, list.Count);
		}
		public static int RemoveIf<T>(this IList<T> list, Func<T,bool> pred, int start, int count)
		{
			if (start < 0) throw new ArgumentOutOfRangeException("start","Start index {0} out of range [0,{1})".Fmt(start, list.Count));
			if (start + count > list.Count) throw new ArgumentOutOfRangeException("count","Item count {0} out of range [0,{1}]".Fmt(count, list.Count));

			int removed = 0;
			for (int i = start + count; i-- != start;)
			{
				if (!pred(list[i])) continue;
				list.RemoveAt(i);
				++removed;
			}
			return removed;
		}
		public static int RemoveIf<T>(this IList list, Func<T,bool> pred, int start, int count)
		{
			if (start < 0) throw new ArgumentOutOfRangeException("start","Start index {0} out of range [0,{1})".Fmt(start, list.Count));
			if (start + count > list.Count) throw new ArgumentOutOfRangeException("count","Item count {0} out of range [0,{1}]".Fmt(count, list.Count));

			int removed = 0;
			for (int i = start + count; i-- != start;)
			{
				if (!pred((T)list[i])) continue;
				list.RemoveAt(i);
				++removed;
			}
			return removed;
		}

		/// <summary>Remove a range of items from this list</summary>
		public static void RemoveRange<T>(this IList<T> list, int start, int count)
		{
			for (int i = count; i-- != 0;)
				list.RemoveAt(start + i);
		}

		/// <summary>Remove the range of elements from [startIndex, list.Count)</summary>
		public static IList<T> RemoveToEnd<T>(this IList<T> list, int startIndex)
		{
			var diff = list.Count - startIndex;
			if (diff > 0) list.RemoveRange(startIndex, diff);
			return list;
		}

		/// <summary>Remove all items in 'set' from this list. (More efficient that removing one at a time if 'set' is a large fraction of this list)</summary>
		public static void RemoveAll<T>(this IList<T> list, IEnumerable<T> set)
		{
			list.RemoveAll(set.ToHashSet());
		}
		public static void RemoveAll<T>(this IList<T> list, HashSet<T> set)
		{
			for (int i = list.Count; i-- != 0;)
				if (set.Contains(list[i]))
					list.RemoveAt(i);
		}

		/// <summary>
		/// Remove duplicates from this list.
		/// If 'forward' the first instance of each duplicate is retained, otherwise the last (affects resulting order)
		/// Returns the number removed</summary>
		public static int RemoveDuplicates<T>(this IList<T> list, bool forward)
		{
			return RemoveDuplicates(list, 0, list.Count, forward);
		}
		public static int RemoveDuplicates<T>(this IList<T> list, int start, int count, bool forward)
		{
			// No duplicates in 1 or 0 items
			if (count < 2)
				return 0;

			// Start at the front if going forward, back if going backward
			int s = forward ? start : start + count - 1, e = s;
			var set = new HashSet<T>();
			set.Add(list[s]);
			if (forward)
			{
				for (int i = 1; i != count; ++i)
				{
					if (!set.Contains(list[++e]))
					{
						set.Add(list[e]);
						list[++s] = list[e];
					}
				}
				list.RemoveRange(s + 1, e - s);
			}
			else
			{
				for (int i = 1; i != count; ++i)
				{
					if (!set.Contains(list[--s]))
					{
						set.Add(list[s]);
						list[--e] = list[s];
					}
				}
				list.RemoveRange(s, e - s);
			}
			return e - s;
		}

		/// <summary>Remove elements from the start of this list that satisfy 'pred'</summary>
		public static void TrimStart<T>(this IList<T> list, Func<T,bool> pred)
		{
			int i; for (i = 0; i != list.Count && pred(list[i]); ++i) { }
			list.RemoveRange(0, i);
		}

		/// <summary>Remove elements from the end of this list that satisfy 'pred'</summary>
		public static void TrimEnd<T>(this IList<T> list, Func<T,bool> pred)
		{
			int i; for (i = list.Count; i != 0 && pred(list[i-1]); --i) {}
			list.RemoveRange(i, list.Count - i);
		}

		/// <summary>Remove elements from the start and end of this list that satisfy 'pred'</summary>
		public static void Trim<T>(this IList<T> list, Func<T,bool> pred)
		{
			list.TrimEnd(pred);
			list.TrimStart(pred);
		}

		/// <summary>
		/// Binary search using for an element using only a predicate function.
		/// Returns the index of the element if found or the 2s-complement of the first
		/// element larger than the one searched for.
		/// 'cmp' should return -1 if T is less than the target, +1 if greater, or 0 if equal
		/// 'insert_position' should be true to always return positive indices (i.e. when searching to find insert position)</summary>
		public static int BinarySearch<T>(this IList<T> list, Func<T,int> cmp, bool find_insert_position = false)
		{
			var idx = ~0;
			if (list.Count != 0)
			{
				for (int b = 0, e = list.Count;;)
				{
					int m = b + ((e - b) >> 1); // prevent overflow
					int c = cmp(list[m]);       // <0 means list[m] is less than the target element
					if (c == 0) { idx = m; break; }
					if (c <  0) { if (m == b) { idx = ~e; break; } b = m; continue; }
					if (c >  0) { if (m == b) { idx = ~b; break; } e = m; }
				}
			}
			if (find_insert_position && idx < 0) idx = ~idx;
			return idx;
		}
		public static int BinarySearch<T>(this IList<T> list, T item, Cmp<T> cmp, bool find_insert_position = false)
		{
			return list.BinarySearch(x => cmp.Compare(x, item), find_insert_position);
		}

		/// <summary>Binary search for an element in 'list'. Returns the element if found, or default(T) if not.</summary>
		public static T BinarySearchFind<T>(this IList<T> list, Func<T,int> cmp)
		{
			var idx = list.BinarySearch(cmp);
			return idx >= 0 ? list[idx] : default(T);
		}

		/// <summary>
		/// Insert 'item' into this list assuming the list is sorted by 'cmp'.
		/// Returns the index of where 'item' was inserted</summary>
		public static int AddOrdered<T>(this IList<T> list, T item, Cmp<T> cmp)
		{
			var idx = list.BinarySearch(item, cmp);
			if (idx < 0) idx = ~idx;
			list.Insert(idx, item);
			return idx;
		}

		/// <summary>
		/// Partition 'list' within the range [left,right) such that the element at list[left]
		/// is moved to it's correct position within the list if it was sorted.
		/// Returns the index location of where list[left] is moved to.</summary>
		public static int Partition<T>(this IList<T> list, int left, int right, Cmp<T> comparer)
		{
			if (left == right)
				return left;
			if (left > right)
				throw new Exception("invalid range");

			// Swap a "random" element to 'list[left]' to improve performance
			// for the case where list is mostly sorted
			var count = right - left;
			Swap((IList)list, left, left + (137 * (count + 13)) % count);

			// Copy the pivot
			T pivot = list[left];

			// while the indices haven't meet at the pivot index
			int i = left, j = right;
			for (;;)
			{
				// Move the right index left until value < pivot
				for (--j; i != j && comparer.Compare(list[j], pivot) > 0; --j) {}
				if (i == j) break;

				// Copy the right value to the left position
				list[i] = list[j];

				// Move the left index right until value > pivot
				for (++i; i != j && comparer.Compare(list[i], pivot) < 0; ++i) {}
				if (i == j) break;

				// Copy the left value to the right position
				list[j] = list[i];
			}

			// Copy the pivot back into the pivot location
			list[i] = pivot;
			return i;
		}
		public static int Partition<T>(this IList<T> list, Cmp<T> comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;
			return list.Partition(0, list.Count, comparer);
		}

		/// <summary>Sort the list using a lambda</summary>
		public static IList<T> Sort<T>(this IList<T> list, Cmp<T> comparer = null)
		{
			return list.Sort(0, list.Count, comparer);
		}

		/// <summary>Sub range sort using a delegate</summary>
		public static IList<T> Sort<T>(this IList<T> list, int start, int count, Cmp<T> comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;
			list.QuickSort(start, count, comparer);
			return list;
		}

		/// <summary>Sort the list using the quick sort algorithm</summary>
		public static IList<T> QuickSort<T>(this IList<T> list, Cmp<T> comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;
			return list.QuickSort(0, list.Count, comparer);
		}
		public static IList<T> QuickSort<T>(this IList<T> list, int left, int right)
		{
			return list.QuickSort(0, list.Count, Cmp<T>.Default);
		}
		public static IList<T> QuickSort<T>(this IList<T> list, int left, int right, Cmp<T> comparer)
		{
			// pivot and get pivot location
			int pivot = list.Partition(left, right, comparer);

			// if the left index is less than the pivot, sort left side
			if (pivot - left > 1) list.QuickSort(left, pivot, comparer);

			// if right index is greater than pivot, sort right side
			if (right - pivot > 1) list.QuickSort(pivot + 1, right, comparer);
		
			return list;
		}

		/// <summary>Remove adjascent duplicate elements within the range [begin, end).
		/// Returns the end of the unique range (i.e. a value in the range [begin,end]</summary>
		public static int Unique<T>(this IList<T> list, int begin, int end, Eql<T> comparer = null)
		{
			Debug.Assert(begin <= end && end <= list.Count);
			if (list.Count <= 1) return list.Count;
			int w,r,range_end;

			comparer = comparer ?? Eql<T>.Default;
			
			// Find the first equal adjascent elements
			for (r = begin + 1; r < end && !comparer.Equals(list[r-1], list[r]); ++r) {}

			// back copy elements overwriting duplicates
			for (w = r - 1; r < end;)
			{
				for (++r; r != end && comparer.Equals(list[w], list[r]); ++r) {}
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
		public static int Unique<T>(this IList<T> list, Eql<T> comparer = null)
		{
			return list.Unique(0, list.Count, comparer);
		}

		/// <summary>Return the nth element in the list as if the list was sorted</summary>
		public static T NthElement<T>(this IList<T> list, int n, int left, int right, Cmp<T> comparer)
		{
			// This has O(N^2) performance if 'list' is sorted.
			// Todo: make linear
			var pivot = right;
			for (; left != right; )
			{
				pivot = list.Partition(left, right, comparer);
				if      (pivot < n) left = pivot+1;
				else if (pivot > n) right = pivot;
				else break;
			}
			return list[pivot];
		}
		public static T NthElement<T>(this IList<T> list, int n, Cmp<T> comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;
			return list.NthElement(n, 0, list.Count, comparer);
		}

		/// <summary>Merge a collection into this list. Note: this is a linear operation, this list and 'others' are expected to be ordered</summary>
		public static IList<T> Merge<T>(this IList<T> list, IEnumerable<T> others, EMergeType merge_type, int sign = +1, Cmp<T> comparer = null)
		{
			comparer = comparer ?? Cmp<T>.Default;

			var i = 0;
			var j = others.GetIterator();

			for (; i != list.Count && !j.AtEnd; )
			{
				var lhs = list[i];
				var rhs = j.Current;
				var cmp = sign * comparer.Compare(lhs, rhs);

				// lhs < rhs, advance the left iterator
				if (cmp < 0)
				{
					++i;
				}
				// lhs > rhs, insert from 'right' and advance the right iterator
				else if (cmp > 0)
				{
					list.Insert(i, rhs);
					++i; j.MoveNext();
				}
				// lhs == rhs, select one or both
				else
				{
					switch (merge_type)
					{
					case EMergeType.KeepLeft:
						{
							// Keep the lhs in the list, skip over the rhs
							j.MoveNext();
							break;
						}
					case EMergeType.KeepRight:
						{
							// Replace the lhs with the rhs
							list[i] = rhs;
							++i; j.MoveNext();
							break;
						}
					case EMergeType.KeepBoth:
						{
							// Insert 'rhs' after 'lhs'
							list.Insert(i+1, rhs);
							++i; j.MoveNext();
							break;
						}
					}
				}
			}

			// Append the remain from 'rhs'
			for (; !j.AtEnd; j.MoveNext())
				list.Add(j.Current);

			return list;
		}
		public enum EMergeType
		{
			/// <summary>
			/// All elements in the left collection are retained.
			/// Elements in the right collection are only added if they are not in the left collection</summary>
			KeepLeft,

			/// <summary>
			/// All elements in the right collection are retained.
			/// Elements in the left collection are removed if not in the right collection</summary>
			KeepRight,

			/// <summary>
			/// All elements in both collections are retained.
			/// Matching elements in the left collection will be before elements from the right collection.</summary>
			KeepBoth,
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using pr.maths;
	[TestFixture] public class TestListExtns
	{
		[Test] public void List()
		{
			{
				var list0 = new List<int>(new[] { 1, 2, 3, 3, 2, 4, 5, 2, 1, 5, 4 });
				var list1 = new List<int>(new[] { 1, 2, 3, 4, 5 });
				var count = list0.RemoveDuplicates(forward:true);
				Assert.True(count == 6);
				Assert.True(list0.SequenceEqual(list1));
			}
			{
				var list0 = new List<int>(new[] { 1, 2, 3, 3, 2, 4, 5, 2, 1, 5, 4 });
				var list1 = new List<int>(new[] { 3, 2, 1, 5, 4 });
				var count = list0.RemoveDuplicates(forward:false);
				Assert.True(count == 6);
				Assert.True(list0.SequenceEqual(list1));
			}
			{
				var list0 = new List<int>(new[] { 1, 2, 3, 3, 2, 4, 5, 2, 1, 5, 4 });
				var list1 = new List<int>(new[] { 1, 2, 3, 2, 4, 5, 1, 5, 4 });
				var count = list0.RemoveDuplicates(2, 7, forward:true);
				Assert.True(count == 2);
				Assert.True(list0.SequenceEqual(list1));
			}
			{
				var list0 = new List<int>(new[] { 1, 2, 3, 3, 2, 4, 5, 2, 1, 5, 4 });
				var list1 = new List<int>(new[] { 1, 2, 3, 4, 5, 2, 1, 5, 4 });
				var count = list0.RemoveDuplicates(2, 7, forward:false);
				Assert.True(count == 2);
				Assert.True(list0.SequenceEqual(list1));
			}

		}
		[Test] public void ListQuickSort()
		{
			var rng = new Random();
			var list = new List<int>(99);
			for (var i = 0; i != 99; ++i)
				list.Add(rng.Next(10));

			list.Sort();
				
			for (var i = 0; i != list.Count - 1; ++i)
				Assert.True(list[i] <= list[i+1]);
		}
		[Test] public void ListUnique()
		{
			var rng = new Random();
			var list = new List<int>(100);
			for (var i = 0; i != 100; ++i)
				list.Add(rng.Next(10));

			list.Sort();

			int last = list.Unique(0, 50);
			for (var i = 0; i < last; ++i)
			for (var j = i+1; j < last; ++j)
				Assert.AreNotEqual(list[i], list[j]);

			list.Unique();
			for (var i = 0; i < list.Count; ++i)
			for (var j = i+1; j < list.Count; ++j)
				Assert.AreNotEqual(list[i], list[j]);

			Assert.AreEqual(5, list.Add2(5));
		}
		[Test] public void Merge()
		{
			var lhs = new[] { 1, 2, 4, 7, 8 };
			var rhs = new[] { 1, 3, 5, 6, 8, 9, 10 };

			var l0 = lhs.ToList().Merge(rhs, List_.EMergeType.KeepLeft);
			Assert.True(l0.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));

			var l1 = lhs.ToList().Merge(rhs, List_.EMergeType.KeepRight);
			Assert.True(l1.SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));

			var l2 = lhs.ToList().Merge(rhs, List_.EMergeType.KeepBoth);
			Assert.True(l2.SequenceEqual(new[] { 1, 1, 2, 3, 4, 5, 6, 7, 8, 8, 9, 10 }));

			var shl = new[] { 8, 7, 4, 2, 1 };
			var shr = new[] {10, 9, 8, 6, 5, 3, 1 };
			var l3 = shl.ToList().Merge(shr, List_.EMergeType.KeepLeft, sign:-1);
			Assert.True(l3.SequenceEqual(new[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 }));
		}
		[Test] public void NthElement()
		{
			var arr0 = new int[] {0,1,2,3,4,5,6,7,8,9};
			var arr1 = new int[] {0,1,2,3,4,5,6,7,8,9,10};

			Assert.True(arr0.NthElement(0) == 0);
			Assert.True(arr1.NthElement(0) == 0);
			Assert.True(arr0.NthElement(5) == 5);
			Assert.True(arr1.NthElement(5) == 5);
			Assert.True(arr0.NthElement(8) == 8);
			Assert.True(arr1.NthElement(8) == 8);

			var rng = new Random(1);
			Action<int[]> Shuffle = arr =>
			{
				for (int i = 0; i != 1000; ++i)
				{
					var a = rng.Int(0, arr.Length);
					var b = rng.Int(0, arr.Length);
					Maths.Swap(ref arr[a], ref arr[b]);
				}
			};

			Shuffle(arr0);
			Assert.True(arr0.NthElement(0) == 0);
			Assert.True(arr0.NthElement(5) == 5);
			Assert.True(arr0.NthElement(8) == 8);

			Shuffle(arr1);
			Assert.True(arr1.NthElement(0) == 0);
			Assert.True(arr1.NthElement(5) == 5);
			Assert.True(arr1.NthElement(8) == 8);
		}
	}
}
#endif
