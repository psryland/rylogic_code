//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Linq;

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
		public static IEnumerable<int> IndicesOf<TSource>(this IEnumerable<TSource> source, TSource element, IEqualityComparer<TSource> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<TSource>.Default;
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
		public static bool AllSame<TSource, TRet>(this IEnumerable<TSource> source, Func<TSource,TRet> selector, IEqualityComparer<TRet> comparer = null)
		{
			comparer = comparer ?? EqualityComparer<TRet>.Default;

			if (!source.Any()) return true;
			var first = selector(source.First());
			return source.Skip(1).All(x => comparer.Equals(first, selector(x)));
		}

		/// <summary>Returns this collection as pairs</summary>
		public static IEnumerable<Tuple<TSource,TSource>> InPairs<TSource>(this IEnumerable<TSource> source)
		{
			var en = source.GetEnumerator();
			while (en.MoveNext())
			{
				var item1 = en.Current;
				var item2 = en.MoveNext() ? en.Current : default(TSource);
				yield return new Tuple<TSource,TSource>(item1,item2);
			}
		}

		///// <summary>Returns:
		/////  (0,1),(2,3),(4,5),...<para/>
		/////  (0,2),(4,6),(8,10),...<para/>
		/////  (
		///// </summary>
		///// <typeparam name="TSource"></typeparam>
		///// <param name="source"></param>
		///// <returns></returns>
		//public static IEnumerable<Tuple<TSource,TSource>> Reduce<TSource>(this IEnumerable<TSource> source)
		//{
		//}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;
	using extn;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestExtensions
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
		}
	}
}

#endif
