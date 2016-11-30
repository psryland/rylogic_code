//***************************************************
// Byte Array Functions
//  Copyright (c) Rylogic Ltd 2010
//***************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace pr.extn
{
	public static class Array_
	{
		/// <summary>Reset the array to default</summary>
		public static void Clear<T>(this T[] arr)
		{
			Array.Clear(arr, 0, arr.Length);
		}
		public static void Clear<T>(this T[] arr, int index, int length)
		{
			Array.Clear(arr, index, length);
		}

		/// <summary>Create a shallow copy of this array</summary>
		public static T[] Dup<T>(this T[] arr)
		{
			return (T[])arr.Clone();
		}
		public static T[] Dup<T>(this T[] arr, int ofs, int count)
		{
			var result = new T[count];
			Array.Copy(arr, ofs, result, 0, count);
			return result;
		}

		/// <summary>Returns the index of 'what' in the array</summary>
		public static int IndexOf<T>(this T[] arr, T what)
		{
			int idx = 0;
			foreach (var i in arr)
			{
				if (!Equals(i,what)) ++idx;
				else return idx;
			}
			return -1;
		}

		/// <summary>Compare sub-ranges within arrays for value equality</summary>
		public static bool SequenceEqual<T>(this T[] lhs, T[] rhs, int len)
		{
			return SequenceEqual(lhs,rhs,0,0,len);
		}

		/// <summary>Compare sub-ranges within arrays for value equality</summary>
		public static bool SequenceEqual<T>(this T[] lhs, T[] rhs, int ofs0, int ofs1, int len)
		{
			for (int i = ofs0, j = ofs1; len-- != 0; ++i, ++j)
				if (!Equals(lhs[i], rhs[i]))
					return false;
			return true;
		}

		/// <summary>Value equality for the contents of two arrays</summary>
		public static bool Equal<T>(T[] lhs, T[] rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;

			var task = Parallel.For(0, lhs.Length, (i, loop_state) =>
			{
				if (Equals(lhs[i], rhs[i])) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
		public static bool Equal<T>(T[,] lhs, T[,] rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;

			var dim0 = lhs.GetLength(0);
			if (dim0 != rhs.GetLength(0))
				return false;

			var task = Parallel.For(0, lhs.Length, (k, loop_state) =>
			{
				var i = k % dim0;
				var j = k / dim0;
				if (Equals(lhs[i,j], rhs[i,j])) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
		public static bool Equal(Array lhs, Array rhs)
		{
			if (lhs == rhs) return true;
			if (lhs == null || rhs == null) return false;
			if (lhs.Length != rhs.Length) return false;
			if (lhs.Rank != rhs.Rank) return false;

			// Get the dimensions of the array and the elements per dimension
			var dim = new int[lhs.Rank];
			var div = new int[lhs.Rank];
			var next = 1;
			for (int i = 0; i != dim.Length; ++i)
			{
				if (lhs.GetLength(i) != rhs.GetLength(i)) return false;
				dim[i] = lhs.GetLength(i);
				div[i] = next;
				next = next * dim[i];
			}

			// Compare all elements in parallel
			var task = Parallel.For(0, lhs.Length, (k, loop_state) =>
			{
				var indices = new int[dim.Length];
				for (int i = 0; i != indices.Length; ++i)
					indices[i] = (k / div[i]) % dim[i];

				if (Equals(lhs.GetValue(indices), rhs.GetValue(indices))) return;
				loop_state.Stop();
			});

			return task.IsCompleted;
		}
	}

	/// <summary>A sub range within an array</summary>
	public struct ArraySlice<T> :IList<T> ,ICollection<T>, IEnumerable<T>, IEnumerable, IReadOnlyList<T>, IReadOnlyCollection<T>
	{
		// This class exists because ArraySegment doesn't provide an indexer.
		// Yes, seriously.. ffs

		public ArraySlice(T[] arr)
			:this(arr,0,arr.Length)
		{}
		public ArraySlice(T[] arr, int offset, int length)
		{
			if (arr == null)
				throw new ArgumentNullException(nameof(arr));
			if (offset < 0 || offset > arr.Length)
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset out of range [0,{0}]".Fmt(arr.Length));
			if (length < 0 || length > arr.Length - offset)
				throw new ArgumentOutOfRangeException(nameof(offset), "Length out of range [0,{0}]".Fmt(arr.Length - offset));

			Array = arr;
			Offset = offset;
			Length = length;
		}

		/// <summary>The original array</summary>
		public T[] Array { get; private set; }

		/// <summary>The index offset into 'Array' for the start of this slice</summary>
		public int Offset { get; private set; }

		/// <summary>The number of elements in this array slice</summary>
		public int Length { get; private set; }
		public int Count { get { return Length; } }

		/// <summary>Get/Set by index</summary>
		public T this[int idx]
		{
			get { return Array[Offset + idx]; }
			set { Array[Offset + idx] = value; }
		}

		/// <summary>Implicit conversion to/from ArraySegment</summary>
		public static implicit operator ArraySegment<T>(ArraySlice<T> s) { return new ArraySegment<T>(s.Array, s.Offset, s.Count); }
		public static implicit operator ArraySlice<T>(ArraySegment<T> s) { return new ArraySlice<T>(s.Array, s.Offset, s.Count); }

		#region IList
		public bool IsReadOnly
		{
			get { return true; }
		}
		public int IndexOf(T item)
		{
			return System.Array.IndexOf(Array, item);
		}
		public void Insert(int index, T item)
		{
			throw new NotSupportedException("Cannot insert items into an array slice");
		}
		public bool Remove(T item)
		{
			throw new NotSupportedException("Cannot remove items from an array slice");
		}
		public void RemoveAt(int index)
		{
			throw new NotSupportedException("Cannot remove items from an array slice");
		}
		public void Add(T item)
		{
			throw new NotSupportedException("Cannot add items to an array slice");
		}
		public void Clear()
		{
			Length = 0;
		}
		public bool Contains(T item)
		{
			return IndexOf(item) != -1;
		}
		public void CopyTo(T[] array, int arrayIndex)
		{
			System.Array.Copy(Array, Offset, array, arrayIndex, Length);
		}
		public IEnumerator<T> GetEnumerator()
		{
			for (int i = Offset, iend = Offset + Length; i != iend; ++i)
				yield return Array[i];
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<T>)this).GetEnumerator();
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using System.Linq;
	using extn;

	[TestFixture] public class TestArrayExtns
	{
		[Test] public void ArrayExtns()
		{
			var a0 = new[]{1,2,3,4};
			var A0 = a0.Dup();

			Assert.AreEqual(typeof(int[]), A0.GetType());
			Assert.True(A0.SequenceEqual(a0));

			Assert.AreEqual(2, A0.IndexOf(3));
			Assert.AreEqual(-1, A0.IndexOf(5));

			for (var err = 0; err != 2; ++err)
			{
				// 1-Dimensional array value equality
				var a1 = new [] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 };
				var A1 = new [] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0 + err };
				Assert.AreEqual(Array_.Equal(a1, A1), err == 0);

				// 2-Dimensional array value equality
				var v2 = 0.0;
				var a2 = new double[3,4];
				var A2 = new double[3,4];
				for (int i = 0; i != a2.GetLength(0); ++i)
					for (int j = 0; j != a2.GetLength(1); ++j)
					{
						a2[i,j] = v2;
						A2[i,j] = v2 + err;
						v2 += 1.0;
					}
				Assert.AreEqual(Array_.Equal(a2, A2), err == 0);

				// N-Dimensional array value equality
				var v3 = 0.0;
				var a3 = new double[2,3,4];
				var A3 = new double[2,3,4];
				for (int i = 0; i != a3.GetLength(0); ++i)
					for (int j = 0; j != a3.GetLength(1); ++j)
						for (int k = 0; k != a3.GetLength(2); ++k)
						{
							a3[i,j,k] = v3;
							A3[i,j,k] = v3 + err;
							v3 += 1.0;
						}
				Assert.AreEqual(Array_.Equal(a3, A3), err == 0);
			}
		}
	}
}
#endif
