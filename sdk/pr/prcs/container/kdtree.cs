using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.extn;

namespace pr.container
{
	/// <summary>Provides required methods related to a KD Tree of 'T'</summary>
	public interface IKdTree<T>
	{
		/// <summary>The number of dimensions to sort on</summary>
		int Dimensions { get; }

		/// <summary>Return the value of 'elem' on 'axis'</summary>
		float GetAxisValue(T elem, int axis);

		/// <summary>Save the sort axis generated during BuildTree for 'elem'</summary>
		void SortAxis(T elem, int axis);

		/// <summary>Return the sort axis for 'elem'. Used during a search of the kdtree</summary>
		int SortAxis(T elem);
	}

	public class KdTree<T> :List<T>
	{
		/// <summary>The object that provides the functions necessary for building/searching </summary>
		public IKdTree<T> KdTreeSorter { get; set; }

		/// <summary>True when the KD Tree has been built and can be searched</summary>
		private bool m_sorted;

		public KdTree() :this((IKdTree<T>)Activator.CreateInstance(typeof(T)))       {}
		public KdTree(IKdTree<T> sorter)                           :base()           { KdTreeSorter = sorter; m_sorted = true; }
		public KdTree(IKdTree<T> sorter, int capacity)             :base(capacity)   { KdTreeSorter = sorter; m_sorted = true; }
		public KdTree(IKdTree<T> sorter, IEnumerable<T> collection):base(collection) { KdTreeSorter = sorter; m_sorted = false; }

		/// <summary>Create a KdTree based on a sorter type</summary>
		public static KdTree<T> Create<TSorter>() where TSorter:IKdTree<T>, new()
		{
			return new KdTree<T>(new TSorter());
		}

		/// <summary>
		/// Find in area.
		/// 'AddResult(T elem, float dist_sq)' is called with elements that are within the search area.
		/// 'dist_sq' is the squared distance of the element from the search centre</summary>
		public void FindInArea(float[] centre, float radius, Action<T,float> AddResult)
		{
			if (!m_sorted) throw new Exception("KD Tree requires building first");
			if (centre.Length != KdTreeSorter.Dimensions) throw new Exception("Search centre must be the same dimension as the kd tree");
			FindInArea(0, Count, centre, radius, radius*radius, AddResult);
		}
		private void FindInArea(int first, int last, float[] centre, float radius, float radius_sq, Action<T,float> AddResult)
		{
			if (first == last) return;

			var split_point = first + (last - first) / 2;
			AddIfInRegion(split_point, centre, radius_sq, AddResult);

			// Bottom of the tree? Time to leave
			if (last - first <= 1) return;

			var split_axis  = KdTreeSorter.SortAxis(base[split_point]);
			var split_value = KdTreeSorter.GetAxisValue(base[split_point], split_axis);

			// If the test point is to the right of the split point
			if (centre[split_axis] > split_value)
			{
				var right = split_point + 1;
				FindInArea(right, last, centre, radius, radius_sq, AddResult);

				float distance = centre[split_axis] - split_value;
				if (distance - radius < 0.0f )
					FindInArea(first, split_point, centre, radius, radius_sq, AddResult);
			}

			// Otherwise the test point is to the left of the split point
			else
			{
				FindInArea(first, split_point, centre, radius, radius_sq, AddResult);

				float distance = split_value - centre[split_axis];
				if (distance - radius < 0.0f)
				{
					var right = split_point + 1;
					FindInArea(right, last, centre, radius, radius_sq, AddResult);
				}
			}
		}

		/// <summary>Sort the contents of the list into a KD Tree</summary>
		public void BuildTree()
		{
			BuildTree(0, Count);
			m_sorted = true;
		}
		private void BuildTree(int first, int last)
		{
			if (last - first <= 1) return;

			var split_axis  = LongestAxis(first, last);
			var split_point = MedianSplit(first, last, split_axis);

			KdTreeSorter.SortAxis(base[split_point], split_axis);

			BuildTree(first, split_point);
			++split_point;
			BuildTree(split_point, last);
		}

		/// <summary>Find the axis with the greatest range</summary>
		private int LongestAxis(int first, int last)
		{
			var lower = new float[KdTreeSorter.Dimensions];
			var upper = new float[KdTreeSorter.Dimensions];
			for (var a = 0; a != KdTreeSorter.Dimensions; ++a)
			{
				lower[a] = KdTreeSorter.GetAxisValue(base[first], a);
				upper[a] = lower[a];
			}
			for (++first; first != last; ++first)
			{
				for (var a = 0; a != KdTreeSorter.Dimensions; ++a)
				{
					float value = KdTreeSorter.GetAxisValue(base[first], a);
					lower[a] = Math.Min(lower[a], value);
					upper[a] = Math.Max(upper[a], value);
				}
			}
			var largest = 0;
			var largest_range = upper[0] - lower[0];
			for (var a = 1; a != KdTreeSorter.Dimensions; ++a)
			{
				float range = upper[a] - lower[a];
				if (range > largest_range)
				{
					largest_range = range;
					largest = a;
				}
			}
			return largest;
		}

		/// <summary>
		/// Ensure that the element at the centre of the range has only values less than it on
		/// the left and values greater or equal than it on the right, where the values are the
		/// component of the axis to split on. </summary>
		private int MedianSplit(int first, int last, int split_axis)
		{
			var split_point = first + (last - first) / 2;
			this.NthElement(split_point, (lhs,rhs) => KdTreeSorter.GetAxisValue(lhs, split_axis) < KdTreeSorter.GetAxisValue(rhs, split_axis) ? -1 : 1, first, last);
			return split_point;
		}

		/// <summary>Calls the client if 'elem' is within the search region.</summary>
		private void AddIfInRegion(int elem, float[] centre, float radius_sq, Action<T,float> AddResult)
		{
			float distSq = 0;
			for (var a = 0; a < KdTreeSorter.Dimensions; ++a)
			{
				float dist = KdTreeSorter.GetAxisValue(base[elem], a) - centre[a];
				distSq += dist * dist;
			}
			if (distSq <= radius_sq)
			{
				AddResult(base[elem], distSq);
			}
		}

		public new void Add(T item)
		{
			m_sorted = false;
			base.Add(item);
		}
		public new void AddRange(IEnumerable<T> collection)
		{
			m_sorted = false;
			base.AddRange(collection);
		}
		public new void Clear()
		{
			m_sorted = true;
			base.Clear();
		}
		public new void Insert(int index, T item)
		{
			m_sorted = false;
			base.Insert(index, item);
		}
		public new void InsertRange(int index, IEnumerable<T> collection)
		{
			m_sorted = false;
			base.InsertRange(index, collection);
		}
		public new bool Remove(T item)
		{
			m_sorted = false;
			return base.Remove(item);
		}
		public new int RemoveAll(Predicate<T> match)
		{
			m_sorted = false;
			return base.RemoveAll(match);
		}
		public new void RemoveAt(int index)
		{
			m_sorted = false;
			base.RemoveAt(index);
		}
		public new void RemoveRange(int index, int count)
		{
			m_sorted = false;
			base.RemoveRange(index, count);
		}
		public new void Reverse()
		{
			m_sorted = false;
			base.Reverse();
		}
		public new void Sort()
		{
			m_sorted = false;
			base.Sort();
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using container;
	using maths;

	[TestFixture] internal static partial class UnitTests
	{
		internal static class TestKdTree
		{
			class Sorta :IKdTree<Sorta>
			{
				public v2 pos;
				public int sortaxis;

				int   IKdTree<Sorta>.Dimensions                         { get { return 2; } }
				float IKdTree<Sorta>.GetAxisValue(Sorta elem, int axis) { return elem.pos[axis]; }
				void  IKdTree<Sorta>.SortAxis(Sorta elem, int axis)     { elem.sortaxis = axis; }
				int   IKdTree<Sorta>.SortAxis(Sorta elem)               { return elem.sortaxis; }
				public override string ToString()                       { return pos.ToString(); }
			}

			[Test] public static void KDTree()
			{
				var tree = new KdTree<Sorta>();
				for (int j = 0; j != 10; ++j)
					for (int i = 0; i != 10; ++i)
						tree.Add(new Sorta{pos = new v2(i,j)});

				tree.BuildTree();

				var result = new List<v2>();
				tree.FindInArea(new[]{5f,5f}, 1.1f, (p,rs) => result.Add(p.pos));

				result.Sort((l,r) =>
					{
						if (l.x != r.x) return l.x < r.x ? -1 : 1;
						return l.y < r.y ? -1 : 1;
					});

				Assert.AreEqual(result.Count, 5);
				Assert.AreEqual(result[0], new v2(4f,5f));
				Assert.AreEqual(result[1], new v2(5f,4f));
				Assert.AreEqual(result[2], new v2(5f,5f));
				Assert.AreEqual(result[3], new v2(5f,6f));
				Assert.AreEqual(result[3], new v2(6f,5f));
			}
		}
	}
}
#endif