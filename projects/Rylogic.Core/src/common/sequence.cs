using System;
using System.Collections.Generic;

namespace Rylogic.Common
{
	/// <summary>Methods that operate on sequences</summary>
	public static class Seq
	{
		/// <summary>
		/// Returns the number of times to increment 'lhs' and 'rhs' such that they will both point
		/// to an element that satisfies 'match'. The sum of the increments will be a minimum.<para/>
		/// e.g. Given:<para/>
		///   lhs = a b X c d Y <para/>
		///   rhs = 1 Y 2 X 3 4 <para/>
		///   (X,X) = (2,3) = 5 <para/>
		///   (Y,Y) = (5,1) = 6 <para/>
		///   So (X,X) is the nearest match.
		/// Returns null of no match is found</summary>
		public static Tuple<int,int>? FindNearestMatch<T>(IEnumerable<T> lhs, IEnumerable<T> rhs, Func<T,T,bool> match)
		{
			var L = new List<T>();
			var R = new List<T>();
			var i0 = lhs.GetIterator<T>();
			var i1 = rhs.GetIterator<T>();
			for (int dist = 0; !i0.AtEnd || !i1.AtEnd; ++dist)
			{
				if (!i0.AtEnd) L.Add(i0.CurrentThenNext());
				if (!i1.AtEnd) R.Add(i1.CurrentThenNext());
				for (int i = 0, j = dist; i <= dist; ++i, j = dist - i)
				{
					if (i >= L.Count) continue;
					if (j >= R.Count) continue;
					if (match(L[i], R[j]))
						return Tuple.Create(i, j);
				}
			}
			return null;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Common;

	[TestFixture] public class TestSequence
	{
		[Test] public void NearestMatch()
		{
			var str1 = "abXcdY";
			var str2 = "1Y2X34";
			var d = Seq.FindNearestMatch(str1, str2, (l,r) => l == r);
			Assert.True(d != null && d.Item1 == 2 && d.Item2 == 3); // (X,X)
		}
	}
}
#endif
