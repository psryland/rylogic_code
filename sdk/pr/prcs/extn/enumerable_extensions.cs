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
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestExtensions
		{
			[Test] public static void EnumerableExtensions()
			{}
		}
	}
}

#endif
