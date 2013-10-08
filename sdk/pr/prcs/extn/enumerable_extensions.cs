//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

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
