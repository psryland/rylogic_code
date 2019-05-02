using System;
using System.Collections.Generic;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.Container
{
	public static class KDTree_
	{
		public interface IAccessors<T>
		{
			// Notes:
			//  - If 'T' implements this interface, then accessors don't need to
			//    be passed to the KD Tree functions.
			//  - 'SortAxisSet' should return 'elem' because if 'T' is a struct type
			//    changing the provided 'elem' parameter has no effect.

			/// <summary>Get/Set the sort axis for an element</summary>
			int SortAxisGet(T elem);
			T SortAxisSet(T elem, int axis);

			/// <summary>Return the value for the given axis</summary>
			double AxisValueGet(T elem, int axis);
		}

		/// <summary>Sort 'list' into a KD Tree</summary>
		/// <param name="list">The list to be sorted into a KD tree</param>
		/// <param name="dimensions">The number of dimensions to sort on (typically 2 or 3)</param>
		/// <param name="AxisValueGet">Callback that returns the value of the given element on the given dimension</param>
		/// <param name="SortAxisSet">Callback that sets the axis to sort on for the given element</param>
		public static void Build<T>(IList<T> list, int dimensions, Func<T, int, double> AxisValueGet, Func<T, int, T> SortAxisSet)
		{
			BuildTree(0, list.Count);

			// Helpers
			void BuildTree(int beg, int end)
			{
				if (end - beg <= 1)
					return;

				// Partition the range at the mid point of the longest axis
				var split_axis = LongestAxis(beg, end);
				var split_point = MedianSplit(beg, end, split_axis);

				// SortAxisSet must return the updated element because it may be a struct.
				list[split_point] = SortAxisSet(list[split_point], split_axis);

				// Recursively build each half of the remaining data
				BuildTree(beg, split_point);
				BuildTree(split_point + 1, end);
			}
			int LongestAxis(int beg, int end)
			{
				// Found the bounds on each axes of the range [beg, end)
				var lower = Array_.New(dimensions, i => double.MaxValue);
				var upper = Array_.New(dimensions, i => double.MinValue);
				for (var i = beg; i != end; ++i)
				{
					for (var a = 0; a != dimensions; ++a)
					{
						var value = AxisValueGet(list[i], a);
						lower[a] = Math.Min(lower[a], value);
						upper[a] = Math.Max(upper[a], value);
					}
				}

				// Return the axis with the greatest range
				var largest = 0;
				for (var a = 1; a != dimensions; ++a)
				{
					if (upper[a] - lower[a] < upper[largest] - lower[largest]) continue;
					largest = a;
				}
				return largest;
			}
			int MedianSplit(int first, int last, int split_axis)
			{
				// Ensure that the element at the centre of the range has only values less than it on
				// the left and values greater or equal than it on the right, where the values are the
				// component of the axis to split on.
				var split_point = first + (last - first) / 2;
				var cmp = Cmp<T>.From((lhs, rhs) => AxisValueGet(lhs, split_axis) < AxisValueGet(rhs, split_axis) ? -1 : 1);
				list.NthElement(split_point, first, last, cmp);
				return split_point;
			}
		}
		public static void Build<T>(IList<T> list, int dimensions, IAccessors<T> accessors)
		{
			Build(list, dimensions, accessors.AxisValueGet, accessors.SortAxisSet);
		}
		public static void Build<T>(IList<T> list, int dimensions) where T : IAccessors<T>
		{
			Build(list, dimensions, (x, i) => x.AxisValueGet(x, i), (x, i) => x.SortAxisSet(x, i));
		}

		/// <summary>Search for elements within the given radius of 'centre'</summary>
		/// <param name="list">The KD Tree sorted data structure</param>
		/// <param name="dimensions">The number of dimensions that the KD tree is sorted on</param>
		/// <param name="centre">The centre of the search area</param>
		/// <param name="radius">The radius of the search area</param>
		/// <param name="AddResult">Callback called for each element that is within the search volume</param>
		/// <param name="AxisValueGet">Callback that returns the value of the given element on the given dimension</param>
		/// <param name="SortAxisGet">Callback that returns the sort axis for the given element</param>
		public static IEnumerable<T> Search<T>(IList<T> list, int dimensions, double[] centre, double radius, Func<T, int, double> AxisValueGet, Func<T, int> SortAxisGet)
		{
			if (centre.Length < dimensions)
				throw new Exception("Search centre must have a value for each dimension of the KD tree");

			var radius_sq = radius * radius;
			return Search(0, list.Count);

			// Helpers
			IEnumerable<T> Search(int beg, int end)
			{
				if (beg == end)
					yield break;

				var split_point = beg + (end - beg) / 2;
				if (IsInSearchVolume(list[split_point]))
					yield return list[split_point];

				// Bottom of the tree? Time to leave
				if (end - beg <= 1)
					yield break;

				var split_axis = SortAxisGet(list[split_point]);
				var split_value = AxisValueGet(list[split_point], split_axis);

				// If the test point is to the left of the split point
				if (centre[split_axis] < split_value)
				{
					foreach (var x in Search(beg, split_point))
						yield return x;

					var distance = split_value - centre[split_axis];
					if (distance - radius < 0)
						foreach (var x in Search(split_point + 1, end))
							yield return x;
				}

				// Otherwise the test point is to the right of the split point
				else
				{
					foreach (var x in Search(split_point + 1, end))
						yield return x;

					var distance = centre[split_axis] - split_value;
					if (distance - radius < 0)
						foreach (var x in Search(beg, split_point))
							yield return x;
				}
			}
			bool IsInSearchVolume(T elem)
			{
				var dist_sq = 0.0;
				for (var a = 0; a != dimensions; ++a)
				{
					var dist = AxisValueGet(elem, a) - centre[a];
					dist_sq += dist * dist;
				}
				return dist_sq <= radius_sq;
			}
		}
		public static IEnumerable<T> Search<T>(IList<T> list, int dimensions, double[] centre, double radius, IAccessors<T> accessors)
		{
			return Search(list, dimensions, centre, radius, accessors.AxisValueGet, accessors.SortAxisGet);
		}
		public static IEnumerable<T> Search<T>(IList<T> list, int dimensions, double[] centre, double radius) where T : IAccessors<T>
		{
			return Search(list, dimensions, centre, radius, (x, i) => x.AxisValueGet(x, i), (x) => x.SortAxisGet(x));
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Diagnostics;
	using System.Linq;
	using Container;
	using Maths;

	[TestFixture]
	public class TestKdTree
	{
		[DebuggerDisplay("{sortaxis} | {pos}")]
		struct Sorta : KDTree_.IAccessors<Sorta>
		{
			public v2 pos;
			public int sortaxis;

			// When the accessors are implemented in the element type itself,
			// the 'elem' parameter should always equal 'this'.
			int KDTree_.IAccessors<Sorta>.SortAxisGet(Sorta elem)
			{
				Assert.Equal(this, elem);
				return sortaxis;
			}
			Sorta KDTree_.IAccessors<Sorta>.SortAxisSet(Sorta elem, int axis)
			{
				Assert.Equal(this, elem);
				sortaxis = axis;
				return this;
			}
			double KDTree_.IAccessors<Sorta>.AxisValueGet(Sorta elem, int axis)
			{
				Assert.Equal(this, elem);
				return pos[axis];
			}
		}

		[Test]
		public void KDTree()
		{
			var tree = new List<Sorta>();
			for (int j = 0; j != 10; ++j)
				for (int i = 0; i != 10; ++i)
					tree.Add(new Sorta { pos = new v2(i, j) });

			KDTree_.Build(tree, 2);

			var result = KDTree_.Search(tree, 2, new[] { 5.0, 5.0 }, 1.1).Select(x => x.pos).ToList();
			result.Sort((l, r) =>
			{
				if (l.x != r.x) return l.x < r.x ? -1 : 1;
				return l.y < r.y ? -1 : 1;
			});

			Assert.Equal(result.Count, 5);
			Assert.Equal(result[0], new v2(4f, 5f));
			Assert.Equal(result[1], new v2(5f, 4f));
			Assert.Equal(result[2], new v2(5f, 5f));
			Assert.Equal(result[3], new v2(5f, 6f));
			Assert.Equal(result[4], new v2(6f, 5f));
		}
	}
}
#endif