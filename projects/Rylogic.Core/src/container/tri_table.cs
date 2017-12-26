using System.Collections;
using System.Collections.Generic;

namespace Rylogic.Container
{
	// Helper functions for triangular tables
	// Inclusive = A table with entries for N vs. 0 -> N
	// Exclusive = A table with entries for N vs. 0 -> N - 1
	//
	// An Exclusive Triangular table looks like this:
	//	Exc_|_0_|_1_|_2_|_..._|_N_|
	//	__1_|_X_|___  
	//	__2_|_X_|_X_|___
	//	__3_|_X_|_X_|_X_|__
	//	_.._|_X_|_X_|_X_|_..._
	//	_N-1|_X_|_X_|_X_|_..._|
	//
	//	(Inclusive has an extra row at the bottom and a diagonal

	// Usage:
	//	bool flag[pr::TriTable<10>::SizeExc];
	//	flag[tri_table::IndexExc(indexA, indexB)] = true;
	// or
	//	bool flag[TriTable<10>::SizeInc];
	//	flag[TriTableIndexInc<4, 6>::Index] = true;

	public static class TriTable
	{
		public enum EType { Inclusive = 1, Exclusive = -1 }

		/// <summary>Inclusive/exclusive tri-table count</summary>
		public static int Size(int num_elements, EType type)
		{
			return num_elements * (num_elements + (int)type) / 2;
		}
		
		/// <summary>Inclusive/exclusive tri-table index</summary>
		public static int Index(int indexA, int indexB, EType type)
		{
			return (indexA < indexB)
				? indexB * (indexB + (int)type) / 2 + indexA
				: indexA * (indexA + (int)type) / 2 + indexB;
		}
	}

	/// <summary>A sorted list with an interface similar to Dictionary</summary>
	public class TriTable<Elem> :IEnumerable<Elem> ,IEnumerable
	{
		private readonly Elem[] m_array;
		private readonly TriTable.EType m_type;
		private readonly int m_item_count;

		public TriTable(int item_count, TriTable.EType type)
		{
			m_array = new Elem[TriTable.Size(item_count, type)];
			m_type = type;
			m_item_count = item_count;
		}

		/// <summary>The number of elements stored in the tri-table</summary>
		public int Count
		{
			get { return m_array.Length; }
		}

		/// <summary>The length of one side of the tri-table</summary>
		public int ItemCount
		{
			get { return m_item_count; }
		}

		/// <summary>Access an element in the table given it's row,column. Note, index order is irrelevant</summary>
		public Elem this[int idxA, int idxB]
		{
			get { return m_array[TriTable.Index(idxA, idxB, m_type)]; }
			set { m_array[TriTable.Index(idxA, idxB, m_type)] = value; }
		}

		/// <summary>Return all elements for which 'index' is one of the indices</summary>
		public IEnumerable<Elem> Row(int index)
		{
			int i;
			for (i = 0; i != index; ++i)
				yield return this[i, index];
			
			if (m_type == TriTable.EType.Inclusive)
				yield return this[index,index];

			for (++i; i != m_item_count; ++i)
				yield return this[index, i];
		}

		public IEnumerator<Elem> GetEnumerator()
		{
			foreach (var i in m_array)
				yield return i;
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
