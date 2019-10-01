using System;
using System.Collections.Generic;
using Rylogic.Extn;

namespace Rylogic.Container
{
	/// <summary>A sorted list with an interface similar to Dictionary</summary>
	public class ListMap<K,V> :List<V>
	{
		private readonly Comparer m_cmp;
		
		/// <summary>A comparer that returns 0 if key == value, negative if key &lt; value, or positive if key &gt; value</summary>
		public delegate int Comparer(K key, V value);

		public ListMap(Comparer cmp)
		{
			m_cmp = cmp;
		}
		public ListMap(ListMap<K,V> rhs) :base(rhs)
		{
			m_cmp = rhs.m_cmp;
		}
		public void Add(K key, V value)
		{
			int idx = FindIdx(key);
			if (idx >= 0) throw new ArgumentException("An element with "+key+" already exists");
			Insert(~idx, value);
		}
		public void Remove(K key)
		{
			int idx = FindIdx(key);
			if (idx < 0) return;
			RemoveAt(idx);
		}
		public bool ContainsKey(K key)
		{
			return FindIdx(key) >= 0;
		}
		public V this[K key]
		{
			get { return base[GetIdx(key)]; }
			set { base[GetIdx(key)] = value; }
		}
		public bool TryGetValue(K key, out V value)
		{
			int idx = FindIdx(key);
			if (idx < 0) { value = default!; return false; }
			value = base[idx];
			return true;
		}

		private int FindIdx(K key)
		{
			return this.BinarySearch(v => m_cmp(key, v));
		}
		private int GetIdx(K key)
		{
			int idx = FindIdx(key);
			if (idx < 0) throw new KeyNotFoundException(key + " not found");
			return idx;
		}
	}
}
