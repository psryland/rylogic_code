//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using pr.common;

namespace pr.extn
{
	/// <summary>Extensions for IEnumerable</summary>
	public static class EnumerableExtensions
	{
		/// <summary>Convert the collection into a hash set</summary>
		public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
		{
			return new HashSet<TSource>(source);
		}

		/// <summary>Apply 'action' to each item in the collection</summary>
		public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> action)
		{
			foreach (var item in source)
				action(item);
		}

		/// <summary>Exactly the same as 'Reverse' but doesn't clash with List.Reverse()</summary>
		public static IEnumerable<TSource> Reversed<TSource>(this IEnumerable<TSource> source)
		{
			return source.Reverse();
		}

		/// <summary>Returns the indices of 'element' within this collection</summary>
		public static IEnumerable<int> IndicesOf<TSource>(this IEnumerable<TSource> source, TSource element, Eql<TSource> comparer = null)
		{
			comparer = comparer ?? Eql<TSource>.Default;
			var i = 0;
			foreach (var s in source)
			{
				if (comparer.Equals(s, element)) yield return i;
				++i;
			}
		}

		/// <summary>Return the indices of the selected elements within the enumerable</summary>
		public static IEnumerable<int> IndicesOf<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> selector)
		{
			var i = 0;
			foreach (var s in source)
			{
				if (selector(s)) yield return i;
				++i;
			}
		}

		/// <summary>Returns true if all elements this collection result in the same result from 'selector'</summary>
		public static bool AllSame<TSource, TRet>(this IEnumerable<TSource> source, Func<TSource,TRet> selector, Eql<TRet> comparer = null)
		{
			comparer = comparer ?? Eql<TRet>.Default;

			if (!source.Any()) return true;
			var first = selector(source.First());
			return source.Skip(1).All(x => comparer.Equals(first, selector(x)));
		}

		/// <summary>Returns the maximum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, Cmp<TKey> comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				var max = src_iter.Current;
				var max_key = selector(max);
				while (src_iter.MoveNext())
				{
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) <= 0) continue;
					max_key = key;
					max = item;
				}
				return max;
			}
		}

		/// <summary>Returns the minimum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, Cmp<TKey> comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				var max = src_iter.Current;
				var max_key = selector(max);
				while (src_iter.MoveNext())
				{
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) >= 0) continue;
					max_key = key;
					max = item;
				}
				return max;
			}
		}

		/// <summary>Returns the index of the maximum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static int IndexOfMaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, Cmp<TKey> comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				int index = 0;
				int index_of_max = 0;
				var max_key = selector(src_iter.Current);
				while (src_iter.MoveNext())
				{
					++index;
					var item = src_iter.Current;
					var key = selector(item);
					if (comparer.Compare(key, max_key) <= 0) continue;
					max_key = key;
					index_of_max = index;
				}
				return index_of_max;
			}
		}

		/// <summary>Returns the index of the minimum element based on 'selector', with comparisons of the selector type made by 'comparer'</summary>
		public static int IndexOfMinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> selector, Cmp<TKey> comparer = null)
		{
			if (source   == null) throw new ArgumentNullException("source");
			if (selector == null) throw new ArgumentNullException("selector");

			comparer = comparer ?? Cmp<TKey>.Default;
			using (var src_iter = source.GetEnumerator())
			{
				if (!src_iter.MoveNext())
					throw new InvalidOperationException("Sequence contains no elements");

				int index = 0;
				int index_of_min = 0;
				var max_key = selector(src_iter.Current);
				while (src_iter.MoveNext())
				{
					++index;
					var key = selector(src_iter.Current);
					if (comparer.Compare(key, max_key) >= 0) continue;
					max_key = key;
					index_of_min = index;
				}
				return index_of_min;
			}
		}

		/// <summary>Counts the number of items in a sequence until 'pred' returns false</summary>
		public static int CountWhile<TSource>(this IEnumerable<TSource> source, Func<TSource,bool> pred)
		{
			int count = 0;
			foreach (var item in source)
			{
				if (pred(item)) ++count;
				else break;
			}
			return count;
		}

		/// <summary>Returns this collection as pairs</summary>
		public static IEnumerable<Tuple<TSource,TSource>> InPairs<TSource>(this IEnumerable<TSource> source)
		{
			var en = source.GetEnumerator();
			while (en.MoveNext())
			{
				var item1 = en.Current;
				var item2 = en.MoveNext() ? en.Current : default(TSource);
				yield return Tuple.Create(item1,item2);
			}
		}

		/// <summary>Zip two collections together in order defined by 'comparer'</summary>
		public static IEnumerable<TSource> Zip<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other, Cmp<TSource> comparer = null)
		{
			comparer = comparer ?? Cmp<TSource>.Default;

			var lhs = source.GetIterator();
			var rhs = other.GetIterator();
			for (;!lhs.AtEnd && !rhs.AtEnd;)
			{
				if (comparer.Compare(lhs.Current,rhs.Current) < 0)
				{
					yield return lhs.Current;
					lhs.MoveNext();
				}
				else
				{
					yield return rhs.Current;
					rhs.MoveNext();
				}
			}
			for (; !lhs.AtEnd; lhs.MoveNext())
			{
				yield return lhs.Current;
			}
			for (; !rhs.AtEnd; rhs.MoveNext())
			{
				yield return rhs.Current;
			}
		}
	
		/// <summary>Compare elements with 'other' returning pairs where the elements are not equal</summary>
		public static IEnumerable<Tuple<TSource,TSource>> Differences<TSource>(this IEnumerable<TSource> source, IEnumerable<TSource> other, Eql<TSource> comparer = null)
		{
			comparer = comparer ?? Eql<TSource>.Default;

			var i0 = source.GetIterator();
			var i1 = other.GetIterator();
			for (; !i0.AtEnd && !i1.AtEnd; i0.MoveNext(), i1.MoveNext())
			{
				if (comparer.Equals(i0.Current, i1.Current)) continue;
				yield return Tuple.Create(i0.Current, i1.Current);
			}
			for (; !i0.AtEnd; i0.MoveNext())
				yield return Tuple.Create(i0.Current, default(TSource));
			for (; !i1.AtEnd; i1.MoveNext())
				yield return Tuple.Create(default(TSource), i1.Current);
		}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using extn;

	[TestFixture] public static partial class UnitTests
	{
		public static partial class TestExtensions
		{
			[Test] public static void InPairs()
			{
				var ints = new[]{1,2,3,4,5,6,7,8,9};
				var sums = new List<int>();
				foreach (var pair in ints.InPairs())
					sums.Add(pair.Item1 + pair.Item2);

				Assert.AreEqual(5  ,sums.Count);
				Assert.AreEqual(3  ,sums[0]);
				Assert.AreEqual(7  ,sums[1]);
				Assert.AreEqual(11 ,sums[2]);
				Assert.AreEqual(15 ,sums[3]);
				Assert.AreEqual(9  ,sums[4]);
			}
			[Test] public static void Merge()
			{
				var a0 = new[]{1,2,4,6,10};
				var a1 = new[]{1,3,4,7,8};
				var r0 = new[]{1,1,2,3,4,4,6,7,8,10};
				var r1 = a0.Zip(a1, Cmp<int>.From((l,r) => l < r));

				Assert.True(r0.SequenceEqual(r1));
			}
			[Test] public static void Differences()
			{
				var a0 = new[]{1,2,3,4,5};
				var a1 = new[]{1,3,3,4,6,7};
				var r0 = new[]{2,3,5,6,0,7};
				var r1 = a0.Differences(a1).SelectMany(x => new[]{x.Item1, x.Item2}).ToArray();

				Assert.True(r0.SequenceEqual(r1));
			}
			[Test] public static void MinMaxBy()
			{
				var a0 = new[]{4,2,7,1,8,3,4,6,8,9};

				Assert.AreEqual(1, a0.MinBy(x => x));
				Assert.AreEqual(2, a0.MinBy(x => (x % 2) == 0 ? x : 1000));
				Assert.AreEqual(9, a0.MinBy(x => 10 - x));

				Assert.AreEqual(9, a0.MaxBy(x => x));
				Assert.AreEqual(8, a0.MaxBy(x => (x % 2) == 0 ? x : -1000));
				Assert.AreEqual(1, a0.MaxBy(x => 10 - x));

				Assert.AreEqual(3, a0.IndexOfMinBy(x => x));
				Assert.AreEqual(1, a0.IndexOfMinBy(x => (x % 2) == 0 ? x : 1000));
				Assert.AreEqual(9, a0.IndexOfMinBy(x => 10 - x));

				Assert.AreEqual(9, a0.IndexOfMaxBy(x => x));
				Assert.AreEqual(4, a0.IndexOfMaxBy(x => (x % 2) == 0 ? x : -1000));
				Assert.AreEqual(3, a0.IndexOfMaxBy(x => 10 - x));
			}
		}
	}
}

#endif