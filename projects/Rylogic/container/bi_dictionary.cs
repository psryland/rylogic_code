using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pr.container
{
	/// <summary>Bi-directional dictionary</summary>
	public class BiDictionary<T0,T1> :IDictionary<T0,T1>
	{
		private readonly Dictionary<T0, T1> m_forward = new Dictionary<T0, T1>();
		private readonly Dictionary<T1, T0> m_reverse = new Dictionary<T1, T0>();

		public BiDictionary()
		{
			m_forward = new Dictionary<T0, T1>();
			m_reverse = new Dictionary<T1, T0>();
		}
		public BiDictionary(IDictionary<T0,T1> dict)
		{
			m_forward = new Dictionary<T0, T1>(dict);
			m_reverse = m_forward.ToDictionary(k => k.Value, v => v.Key);
		}
		public BiDictionary(IDictionary<T1,T0> dict)
		{
			m_reverse = new Dictionary<T1, T0>(dict);
			m_forward = m_reverse.ToDictionary(k => k.Value, v => v.Key);
		}

		public int Count
		{
			get { return m_forward.Count; }
		}

		public Dictionary<T0,T1>.KeyCollection Keys0
		{
			get { return m_forward.Keys; }
		}
		public Dictionary<T1,T0>.KeyCollection Keys1
		{
			get { return m_reverse.Keys; }
		}
		public Dictionary<T0,T1>.ValueCollection Values0
		{
			get { return m_forward.Values; }
		}
		public Dictionary<T1,T0>.ValueCollection Values1
		{
			get { return m_reverse.Values; }
		}

		public T1 this[T0 key]
		{
			get { return m_forward[key]; }
			set
			{
				m_forward[key] = value;
				m_reverse[value] = key;
			}
		}
		public T0 this[T1 key]
		{
			get { return m_reverse[key]; }
			set
			{
				m_reverse[key] = value;
				m_forward[value] = key;
			}
		}

		public bool ContainsKey(T0 key)
		{
			return m_forward.ContainsKey(key);
		}
		public bool ContainsKey(T1 key)
		{
			return m_reverse.ContainsKey(key);
		}

		public void Clear()
		{
			m_forward.Clear();
			m_reverse.Clear();
		}

		public void Add(T0 key, T1 value)
		{
			m_forward.Add(key, value);
			m_reverse.Add(value, key);
		}
		public void Add(T1 key, T0 value)
		{
			m_reverse.Add(key, value);
			m_forward.Add(value, key);
		}

		public bool Remove(T0 key)
		{
			T1 value;
			if (m_forward.TryGetValue(key, out value))
			{
				m_forward.Remove(key);
				m_reverse.Remove(value);
				return true;
			}
			return false;
		}
		public bool Remove(T1 key)
		{
			T0 value;
			if (m_reverse.TryGetValue(key, out value))
			{
				m_reverse.Remove(key);
				m_forward.Remove(value);
				return true;
			}
			return false;
		}

		public bool TryGetValue(T0 key, out T1 value)
		{
			return m_forward.TryGetValue(key, out value);
		}
		public bool TryGetValue(T1 key, out T0 value)
		{
			return m_reverse.TryGetValue(key, out value);
		}

		public Dictionary<T0,T1>.Enumerator GetEnumerator()
		{
			return m_forward.GetEnumerator();
		}

		#region IDictionary<T0,T1>
		ICollection<T0> IDictionary<T0, T1>.Keys
		{
			get { return Keys0; }
		}
		ICollection<T1> IDictionary<T0, T1>.Values
		{
			get { return Values0; }
		}
		int ICollection<KeyValuePair<T0, T1>>.Count
		{
			get { return Count; }
		}
		bool ICollection<KeyValuePair<T0, T1>>.IsReadOnly
		{
			get { return false; }
		}
		T1 IDictionary<T0, T1>.this[T0 key]
		{
			get { return this[key]; }
			set { this[key] = value; }
		}
		bool IDictionary<T0, T1>.ContainsKey(T0 key)
		{
			return ContainsKey(key);
		}
		void IDictionary<T0, T1>.Add(T0 key, T1 value)
		{
			Add(key, value);
		}
		bool IDictionary<T0, T1>.Remove(T0 key)
		{
			return Remove(key);
		}
		bool IDictionary<T0, T1>.TryGetValue(T0 key, out T1 value)
		{
			return TryGetValue(key, out value);
		}
		void ICollection<KeyValuePair<T0, T1>>.Add(KeyValuePair<T0, T1> item)
		{
			((ICollection<KeyValuePair<T0, T1>>)m_forward).Add(item);
			((ICollection<KeyValuePair<T1, T0>>)m_reverse).Add(new KeyValuePair<T1,T0>(item.Value, item.Key));
		}
		bool ICollection<KeyValuePair<T0, T1>>.Remove(KeyValuePair<T0, T1> item)
		{
			return
			((ICollection<KeyValuePair<T1, T0>>)m_reverse).Remove(new KeyValuePair<T1,T0>(item.Value, item.Key)) &&
			((ICollection<KeyValuePair<T0, T1>>)m_forward).Remove(item);
		}
		void ICollection<KeyValuePair<T0, T1>>.Clear()
		{
			Clear();
		}
		bool ICollection<KeyValuePair<T0, T1>>.Contains(KeyValuePair<T0, T1> item)
		{
			return m_forward.Contains(item);
		}
		void ICollection<KeyValuePair<T0, T1>>.CopyTo(KeyValuePair<T0, T1>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<T0, T1>>)m_forward).CopyTo(array, arrayIndex);
		}
		IEnumerator<KeyValuePair<T0, T1>> IEnumerable<KeyValuePair<T0, T1>>.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<T0, T1>>)m_forward).GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
